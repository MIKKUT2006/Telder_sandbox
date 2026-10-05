using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Fluids;
using Game.Save;
using Game.World.Dimensions;
using Game.World.Lighting;

namespace Game.World.Fluids
{
    /// <summary>
    /// Minecraft-like 2D liquid runtime.
    ///
    /// V5 rules:
    /// - only explicitly placed/generated cells are source blocks,
    /// - flowing cells NEVER become source blocks,
    /// - a source has limited horizontal spread (8 by default),
    /// - downward flow is unlimited,
    /// - after a waterfall reaches a floor it starts a fresh secondary run
    ///   (10 blocks by default) that becomes gradually shallower,
    /// - liquid propagation is time stepped (3 cells per second by default),
    /// - flowing cells disappear again when their feeder disappears,
    /// - state works across loaded chunk borders and is saved,
    /// - water/ice-water waterfalls create white foam particles on impact,
    /// - liquid rendering samples the same world light as terrain,
    /// - emissive liquids (lava) participate in RGB light propagation.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    public sealed class LiquidRuntime : MonoBehaviour
    {
        private const int SaveVersion = 5;
        private const byte FullAmount = 8;
        private const byte WaterfallBranchFlag = 0x80;
        private const byte FlowDistanceMask = 0x7F;
        private const float DefaultFlowCellsPerSecond = 3f;
        private const int LightBorder = 1;
        private const int LightTextureWidth = Chunk.SizeX + LightBorder * 2;
        private const int LightTextureHeight = Chunk.SizeY + LightBorder * 2;

        [Serializable]
        private class CellSave
        {
            public int X;
            public int Y;
            public string Fluid;
            public int Amount;
            public bool Source;
            public int FlowDistance;
            public bool WaterfallBranch;
            public bool Falling;
        }

        [Serializable]
        private class LiquidSaveFile
        {
            public int Version = SaveVersion;
            public List<CellSave> Cells = new List<CellSave>();
        }

        private sealed class LiquidChunkVisual
        {
            public GameObject Root;
            public Mesh Mesh;
            public Texture2D LightTexture;
            public Color32[] LightPixels;
            public MaterialPropertyBlock Properties;
        }

        private struct FlowCandidate
        {
            public bool Valid;
            public ushort ID;
            public int Distance;
            public bool WaterfallBranch;

            public FlowCandidate(bool valid, ushort id, int distance, bool waterfallBranch)
            {
                Valid = valid;
                ID = id;
                Distance = distance;
                WaterfallBranch = waterfallBranch;
            }
        }

        private struct ScheduledActivation
        {
            public long Key;
            public float ReadyAt;

            public ScheduledActivation(long key, float readyAt)
            {
                Key = key;
                ReadyAt = readyAt;
            }
        }

        private struct FoamPoint
        {
            public Vector3 Position;
            public Color Color;

            public FoamPoint(Vector3 position, Color color)
            {
                Position = position;
                Color = color;
            }
        }

        private static LiquidRuntime instance;

        private World world;

        private readonly Queue<long> active = new Queue<long>();
        private readonly HashSet<long> activeSet = new HashSet<long>();
        private readonly Queue<ScheduledActivation> scheduledActive =
            new Queue<ScheduledActivation>();
        private readonly HashSet<long> scheduledSet = new HashSet<long>();

        private readonly Dictionary<Vector2Int, LiquidChunkVisual> visuals =
            new Dictionary<Vector2Int, LiquidChunkVisual>();

        private readonly Dictionary<long, CellSave> saved =
            new Dictionary<long, CellSave>();

        private readonly HashSet<Vector2Int> seenChunks =
            new HashSet<Vector2Int>();

        private readonly Dictionary<Vector2Int, List<FoamPoint>> foamPoints =
            new Dictionary<Vector2Int, List<FoamPoint>>();

        private float tickTimer;
        private float saveTimer;
        private float hazardTimer;
        private float lightTextureTimer;
        private float foamTimer;

        private string loadedDimensionKey = string.Empty;
        private int loadedSaveVersion = SaveVersion;

        private Material lightingMaterial;
        private Material foamMaterial;
        private ParticleSystem foamSystem;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("LiquidRuntime");
            instance = go.AddComponent<LiquidRuntime>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            Shader shader = Shader.Find("Game/ChunkLitSprite");
            if (shader == null)
            {
                Debug.LogWarning(
                    "LIQUID: shader Game/ChunkLitSprite not found. " +
                    "Falling back to Sprites/Default; liquid lighting will be unavailable."
                );
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                lightingMaterial = new Material(shader);
                lightingMaterial.name = "Runtime Liquid Lighting";
            }

            CreateFoamSystem();
        }

        private void Update()
        {
            WorldManager wm = WorldManager.Instance;
            if (wm == null || !wm.IsReady)
                return;

            world = wm.GetWorld();
            if (world == null)
                return;

            EnsureDimensionLoaded();
            DiscoverChunks();
            PumpScheduledActivations();

            tickTimer += Time.deltaTime;
            if (tickTimer >= 0.07f)
            {
                tickTimer = 0f;
                Simulate(1600);
            }

            // Terrain light textures are updated by ChunkRenderer. Liquids own a
            // matching light texture, so refresh it independently as lighting/day
            // changes; this also avoids depending on renderer execution order.
            lightTextureTimer += Time.deltaTime;
            if (lightTextureTimer >= 0.10f)
            {
                lightTextureTimer = 0f;
                RefreshAllLightTextures();
            }

            hazardTimer += Time.deltaTime;
            if (hazardTimer >= 0.12f)
            {
                hazardTimer = 0f;
                UpdatePlayerHazard();
            }

            foamTimer += Time.deltaTime;
            if (foamTimer >= 0.08f)
            {
                foamTimer = 0f;
                EmitWaterfallFoam();
            }

            saveTimer += Time.deltaTime;
            if (saveTimer >= 5f)
            {
                saveTimer = 0f;
                Flush();
            }
        }

        private void EnsureDimensionLoaded()
        {
            DimensionDefinition dimension = DimensionTravelRuntime.Current;
            string key = dimension == null
                ? string.Empty
                : dimension.Name + "_" + dimension.Seed;

            if (key == loadedDimensionKey)
                return;

            Flush();

            loadedDimensionKey = key;
            loadedSaveVersion = SaveVersion;

            saved.Clear();
            seenChunks.Clear();
            ClearVisuals();
            active.Clear();
            activeSet.Clear();
            scheduledActive.Clear();
            scheduledSet.Clear();
            foamPoints.Clear();

            if (!SaveGameRuntime.HasActiveSave || string.IsNullOrWhiteSpace(key))
                return;

            string path = SavePath();
            if (!File.Exists(path))
                return;

            try
            {
                LiquidSaveFile file =
                    JsonUtility.FromJson<LiquidSaveFile>(File.ReadAllText(path));

                if (file == null)
                    return;

                loadedSaveVersion = file.Version;

                if (file.Cells == null)
                    return;

                for (int i = 0; i < file.Cells.Count; i++)
                {
                    CellSave cell = file.Cells[i];
                    if (cell != null)
                        saved[Pack(cell.X, cell.Y)] = cell;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("LIQUID: save load failed: " + e.Message);
            }
        }

        private void DiscoverChunks()
        {
            foreach (var pair in world.GetLoadedChunks())
            {
                Vector2Int chunkKey = new Vector2Int(pair.Key.x, pair.Key.y);
                if (!seenChunks.Add(chunkKey))
                    continue;

                Chunk chunk = pair.Value;
                if (chunk == null)
                    continue;

                int startX = chunk.X * Chunk.SizeX;
                int startY = chunk.Y * Chunk.SizeY;

                for (int x = 0; x < Chunk.SizeX; x++)
                {
                    for (int y = 0; y < Chunk.SizeY; y++)
                    {
                        int worldX = startX + x;
                        int worldY = startY + y;
                        long packed = Pack(worldX, worldY);

                        if (saved.TryGetValue(packed, out CellSave state))
                        {
                            ApplySavedState(chunk, x, y, state);
                        }

                        if (chunk.GetLiquidAmount(x, y) > 0)
                        {
                            ushort liquidID = chunk.GetLiquidID(x, y);
                            ScheduleAround(
                                worldX,
                                worldY,
                                GetFlowStepDelay(liquidID)
                            );
                        }
                    }
                }

                // If this chunk loaded next to an already-loaded source, there may
                // be no liquid inside the new chunk yet. Waking both sides of every
                // border lets flow continue seamlessly across the new chunk edge.
                WakeChunkBoundary(chunk.X, chunk.Y);

                RebuildChunkVisual(chunk.X, chunk.Y);
            }

            List<Vector2Int> remove = null;

            foreach (var pair in visuals)
            {
                if (world.GetChunk(pair.Key.x, pair.Key.y) != null)
                    continue;

                if (remove == null)
                    remove = new List<Vector2Int>();

                remove.Add(pair.Key);
            }

            if (remove != null)
            {
                for (int i = 0; i < remove.Count; i++)
                {
                    Vector2Int key = remove[i];
                    DestroyVisual(key);
                    seenChunks.Remove(key);
                }
            }
        }

        private void ApplySavedState(
            Chunk chunk,
            int localX,
            int localY,
            CellSave savedState
        )
        {
            ushort id = string.IsNullOrWhiteSpace(savedState.Fluid)
                ? (ushort)0
                : FluidIDRegistry.GetOrRegister(savedState.Fluid);

            byte amount = (byte)Mathf.Clamp(savedState.Amount, 0, FullAmount);
            bool source = savedState.Source;
            int distance = Mathf.Clamp(savedState.FlowDistance, 0, FlowDistanceMask);
            bool waterfallBranch = loadedSaveVersion >= 5 && savedState.WaterfallBranch;
            bool falling = savedState.Falling;

            // V1/V2 stored only amount. Preserve those old worlds by interpreting
            // full old cells as sources and partial cells as ordinary source-flow.
            if (loadedSaveVersion < 3 && id != 0 && amount > 0)
            {
                source = amount >= FullAmount;
                falling = false;
                waterfallBranch = false;

                if (source)
                {
                    distance = 0;
                    amount = FullAmount;
                }
                else
                {
                    distance = Mathf.Clamp(8 - amount, 1, 8);
                }
            }

            if (falling)
            {
                distance = 0;
                waterfallBranch = false;
            }

            chunk.SetLiquidState(
                localX,
                localY,
                id,
                amount,
                source,
                EncodeFlowDistance(distance, waterfallBranch),
                falling
            );
        }

        // =====================================================
        // MINECRAFT-LIKE FLOW
        // =====================================================

        private void Simulate(int budget)
        {
            HashSet<Vector2Int> dirtyVisualChunks = new HashSet<Vector2Int>();
            HashSet<Vector2Int> dirtyEmissiveChunks = new HashSet<Vector2Int>();

            while (budget-- > 0 && active.Count > 0)
            {
                long key = active.Dequeue();
                activeSet.Remove(key);

                Unpack(key, out int x, out int y);

                if (GetChunkFor(x, y) == null)
                    continue;

                RecomputeCell(
                    x,
                    y,
                    dirtyVisualChunks,
                    dirtyEmissiveChunks
                );
            }

            foreach (Vector2Int chunk in dirtyVisualChunks)
                RebuildChunkVisual(chunk.x, chunk.y);

            RebuildEmissiveLighting(dirtyEmissiveChunks);
        }

        private void RecomputeCell(
            int x,
            int y,
            HashSet<Vector2Int> dirtyVisualChunks,
            HashSet<Vector2Int> dirtyEmissiveChunks
        )
        {
            ushort currentID = world.GetLiquidID(x, y);
            byte currentAmount = world.GetLiquidAmount(x, y);
            bool currentSource = world.GetLiquidIsSource(x, y);

            if (world.GetBlock(x, y) != 0)
            {
                if (currentID != 0 || currentAmount != 0)
                {
                    SetState(
                        x, y,
                        0, 0,
                        false, 0, false, false,
                        dirtyVisualChunks,
                        dirtyEmissiveChunks
                    );
                }

                return;
            }

            // Only a cell explicitly placed/generated as a source is permanent.
            if (currentSource && currentID != 0)
            {
                if (currentAmount != FullAmount ||
                    world.GetLiquidFlowDistance(x, y) != 0 ||
                    world.GetLiquidIsFalling(x, y))
                {
                    SetState(
                        x, y,
                        currentID, FullAmount,
                        true, 0, false, false,
                        dirtyVisualChunks,
                        dirtyEmissiveChunks
                    );
                }

                return;
            }

            // Vertical flow has unlimited range. It is always a FLOW cell, never a
            // source. Horizontal distance is deliberately cleared while falling:
            // once the column reaches a floor it starts a fresh waterfall branch.
            ushort aboveID = world.GetLiquidID(x, y + 1);
            byte aboveAmount = world.GetLiquidAmount(x, y + 1);

            if (aboveID != 0 &&
                aboveAmount > 0 &&
                (currentID == 0 || currentID == aboveID))
            {
                SetState(
                    x, y,
                    aboveID, FullAmount,
                    false, 0, false, true,
                    dirtyVisualChunks,
                    dirtyEmissiveChunks
                );

                return;
            }

            FlowCandidate best = default;

            ConsiderHorizontalFeeder(x - 1, y, currentID, ref best);
            ConsiderHorizontalFeeder(x + 1, y, currentID, ref best);

            if (!best.Valid)
            {
                if (currentID != 0 || currentAmount != 0)
                {
                    SetState(
                        x, y,
                        0, 0,
                        false, 0, false, false,
                        dirtyVisualChunks,
                        dirtyEmissiveChunks
                    );
                }

                return;
            }

            int maxDistance = GetHorizontalFlowDistance(best.ID, best.WaterfallBranch);
            byte amount = AmountForDistance(best.Distance, maxDistance);

            SetState(
                x, y,
                best.ID, amount,
                false, best.Distance, best.WaterfallBranch, false,
                dirtyVisualChunks,
                dirtyEmissiveChunks
            );
        }

        private void ConsiderHorizontalFeeder(
            int feederX,
            int feederY,
            ushort currentID,
            ref FlowCandidate best
        )
        {
            if (GetChunkFor(feederX, feederY) == null)
                return;

            ushort id = world.GetLiquidID(feederX, feederY);
            byte amount = world.GetLiquidAmount(feederX, feederY);

            if (id == 0 || amount == 0)
                return;

            if (currentID != 0 && currentID != id)
                return;

            bool source = world.GetLiquidIsSource(feederX, feederY);
            bool falling = world.GetLiquidIsFalling(feederX, feederY);
            bool waterfallBranch = false;
            int distance;

            if (source)
            {
                // A real source starts the normal source branch.
                distance = 1;
                waterfallBranch = false;
            }
            else if (falling)
            {
                // Open-air waterfall cells may not spread sideways. The lowest
                // falling cell begins a fresh 10-cell (configurable) branch only
                // after it has reached a solid floor.
                if (!FlowCellHasSolidFloor(feederX, feederY))
                    return;

                distance = 1;
                waterfallBranch = true;
            }
            else
            {
                // Ordinary horizontal flow also has strict downward priority: at
                // the edge of a ledge it falls instead of bridging across the gap.
                if (!FlowCellHasSolidFloor(feederX, feederY))
                    return;

                byte encoded = world.GetLiquidFlowDistance(feederX, feederY);
                int feederDistance = DecodeFlowDistance(encoded);
                waterfallBranch = IsWaterfallBranch(encoded);

                if (feederDistance <= 0)
                    return;

                distance = feederDistance + 1;
            }

            int maxDistance = GetHorizontalFlowDistance(id, waterfallBranch);
            if (distance > maxDistance)
                return;

            if (!best.Valid ||
                distance < best.Distance ||
                (distance == best.Distance &&
                 waterfallBranch && !best.WaterfallBranch) ||
                (distance == best.Distance &&
                 waterfallBranch == best.WaterfallBranch && id < best.ID))
            {
                best = new FlowCandidate(true, id, distance, waterfallBranch);
            }
        }

        private bool FlowCellHasSolidFloor(int x, int y)
        {
            if (GetChunkFor(x, y - 1) == null)
                return false;

            return world.GetBlock(x, y - 1) != 0;
        }

        private int GetHorizontalFlowDistance(ushort id, bool waterfallBranch)
        {
            string key = FluidIDRegistry.GetString(id);

            if (!string.IsNullOrWhiteSpace(key) &&
                FluidRegistry.TryGet(key, out FluidDefinition definition) &&
                definition != null)
            {
                int configured = waterfallBranch
                    ? definition.WaterfallHorizontalFlowDistance
                    : definition.HorizontalFlowDistance;

                return Mathf.Clamp(configured, 1, 32);
            }

            return waterfallBranch ? 10 : 8;
        }

        private float GetFlowStepDelay(ushort id)
        {
            float cellsPerSecond = DefaultFlowCellsPerSecond;
            string key = FluidIDRegistry.GetString(id);

            if (!string.IsNullOrWhiteSpace(key) &&
                FluidRegistry.TryGet(key, out FluidDefinition definition) &&
                definition != null &&
                definition.FlowSpeed > 0f)
            {
                cellsPerSecond = definition.FlowSpeed;
            }

            return 1f / Mathf.Clamp(cellsPerSecond, 0.25f, 30f);
        }

        private static byte AmountForDistance(int distance, int maxDistance)
        {
            maxDistance = Mathf.Max(1, maxDistance);
            distance = Mathf.Clamp(distance, 1, maxDistance);

            // 7/8 at the first flowing block (almost full), then a smooth stepped
            // slope down to 1/8 at the end of the branch.
            int reduction = Mathf.CeilToInt(distance * 7f / maxDistance);
            int amount = 8 - reduction;
            return (byte)Mathf.Clamp(amount, 1, 7);
        }

        private static byte EncodeFlowDistance(int distance, bool waterfallBranch)
        {
            int clamped = Mathf.Clamp(distance, 0, FlowDistanceMask);
            return (byte)(clamped | (waterfallBranch ? WaterfallBranchFlag : 0));
        }

        private static int DecodeFlowDistance(byte encoded)
        {
            return encoded & FlowDistanceMask;
        }

        private static bool IsWaterfallBranch(byte encoded)
        {
            return (encoded & WaterfallBranchFlag) != 0;
        }

        // =====================================================
        // STATE / WAKEUP
        // =====================================================

        private void SetState(
            int x,
            int y,
            ushort id,
            byte amount,
            bool isSource,
            int flowDistance,
            bool waterfallBranch,
            bool isFalling,
            HashSet<Vector2Int> dirtyVisualChunks,
            HashSet<Vector2Int> dirtyEmissiveChunks
        )
        {
            ushort oldID = world.GetLiquidID(x, y);
            byte oldAmount = world.GetLiquidAmount(x, y);
            bool oldSource = world.GetLiquidIsSource(x, y);
            byte oldEncodedDistance = world.GetLiquidFlowDistance(x, y);
            bool oldFalling = world.GetLiquidIsFalling(x, y);

            amount = amount > FullAmount ? FullAmount : amount;

            if (id == 0 || amount == 0)
            {
                id = 0;
                amount = 0;
                isSource = false;
                flowDistance = 0;
                waterfallBranch = false;
                isFalling = false;
            }
            else if (isSource)
            {
                amount = FullAmount;
                flowDistance = 0;
                waterfallBranch = false;
                isFalling = false;
            }
            else if (isFalling)
            {
                // Falling columns are not horizontal branches. Their landing will
                // create a fresh waterfall branch with distance 1.
                flowDistance = 0;
                waterfallBranch = false;
                amount = FullAmount;
            }

            byte encodedDistance = EncodeFlowDistance(flowDistance, waterfallBranch);

            if (oldID == id &&
                oldAmount == amount &&
                oldSource == isSource &&
                oldEncodedDistance == encodedDistance &&
                oldFalling == isFalling)
            {
                return;
            }

            if (!world.SetLiquidState(
                x,
                y,
                id,
                amount,
                isSource,
                encodedDistance,
                isFalling
            ))
            {
                return;
            }

            Vector2Int chunk = new Vector2Int(
                FloorDiv(x, Chunk.SizeX),
                FloorDiv(y, Chunk.SizeY)
            );

            dirtyVisualChunks.Add(chunk);

            if (IsEmissive(oldID) || IsEmissive(id))
                dirtyEmissiveChunks.Add(chunk);

            saved[Pack(x, y)] = new CellSave
            {
                X = x,
                Y = y,
                Fluid = amount == 0 ? string.Empty : FluidIDRegistry.GetString(id),
                Amount = amount,
                Source = isSource,
                FlowDistance = DecodeFlowDistance(encodedDistance),
                WaterfallBranch = IsWaterfallBranch(encodedDistance),
                Falling = isFalling
            };

            // The state itself changes immediately, but the wave can only advance
            // to the next cell after one flow step. At FlowSpeed=3 this is 0.333s.
            ushort timingID = id != 0 ? id : oldID;
            ScheduleAround(x, y, GetFlowStepDelay(timingID));
        }

        private bool IsEmissive(ushort id)
        {
            if (id == 0)
                return false;

            string key = FluidIDRegistry.GetString(id);
            if (string.IsNullOrWhiteSpace(key) ||
                !FluidRegistry.TryGet(key, out FluidDefinition definition) ||
                definition == null)
            {
                return false;
            }

            return definition.LightEmissionR > 0 ||
                   definition.LightEmissionG > 0 ||
                   definition.LightEmissionB > 0;
        }

        private Chunk GetChunkFor(int x, int y)
        {
            int chunkX = FloorDiv(x, Chunk.SizeX);
            int chunkY = FloorDiv(y, Chunk.SizeY);
            return world.GetChunk(chunkX, chunkY);
        }

        private void Activate(int x, int y)
        {
            long key = Pack(x, y);
            if (activeSet.Add(key))
                active.Enqueue(key);
        }

        private void WakeAroundImmediate(int x, int y)
        {
            Activate(x, y);
            Activate(x, y - 1);
            Activate(x, y + 1);
            Activate(x - 1, y);
            Activate(x + 1, y);
        }

        private void ScheduleActivate(int x, int y, float delay)
        {
            long key = Pack(x, y);

            if (activeSet.Contains(key) || scheduledSet.Contains(key))
                return;

            scheduledSet.Add(key);
            scheduledActive.Enqueue(
                new ScheduledActivation(
                    key,
                    Time.time + Mathf.Max(0f, delay)
                )
            );
        }

        private void ScheduleAround(int x, int y, float delay)
        {
            ScheduleActivate(x, y, delay);
            ScheduleActivate(x, y - 1, delay);
            ScheduleActivate(x, y + 1, delay);
            ScheduleActivate(x - 1, y, delay);
            ScheduleActivate(x + 1, y, delay);
        }

        private void PumpScheduledActivations()
        {
            // FlowSpeed for built-in liquids is identical (3 cells/s), so this
            // insertion-ordered queue is also time ordered. It avoids scanning all
            // pending liquid cells every frame.
            int safety = 10000;

            while (scheduledActive.Count > 0 && safety-- > 0)
            {
                ScheduledActivation next = scheduledActive.Peek();
                if (next.ReadyAt > Time.time)
                    break;

                scheduledActive.Dequeue();
                scheduledSet.Remove(next.Key);

                Unpack(next.Key, out int x, out int y);
                Activate(x, y);
            }
        }

        private void WakeChunkBoundary(int chunkX, int chunkY)
        {
            int startX = chunkX * Chunk.SizeX;
            int startY = chunkY * Chunk.SizeY;
            int endX = startX + Chunk.SizeX - 1;
            int endY = startY + Chunk.SizeY - 1;

            for (int x = startX; x <= endX; x++)
            {
                WakeAroundImmediate(x, startY);
                WakeAroundImmediate(x, endY);
            }

            for (int y = startY; y <= endY; y++)
            {
                WakeAroundImmediate(startX, y);
                WakeAroundImmediate(endX, y);
            }
        }

        public static void NotifyCellChanged(int x, int y)
        {
            if (instance == null)
                return;

            instance.WakeAroundImmediate(x, y);

            if (instance.world != null)
            {
                instance.RebuildChunkVisual(
                    FloorDiv(x, Chunk.SizeX),
                    FloorDiv(y, Chunk.SizeY)
                );
            }
        }

        public static void RemoveLiquidAt(int x, int y)
        {
            if (instance == null || instance.world == null)
                return;

            HashSet<Vector2Int> dirtyVisual = new HashSet<Vector2Int>();
            HashSet<Vector2Int> dirtyLight = new HashSet<Vector2Int>();

            instance.SetState(
                x,
                y,
                0,
                0,
                false,
                0,
                false,
                false,
                dirtyVisual,
                dirtyLight
            );

            foreach (Vector2Int chunk in dirtyVisual)
                instance.RebuildChunkVisual(chunk.x, chunk.y);

            instance.RebuildEmissiveLighting(dirtyLight);
        }

        public static bool PlaceSource(int x, int y, string fluidID)
        {
            if (instance == null ||
                instance.world == null ||
                string.IsNullOrWhiteSpace(fluidID) ||
                instance.world.GetBlock(x, y) != 0)
            {
                return false;
            }

            ushort id = FluidIDRegistry.GetOrRegister(fluidID);

            HashSet<Vector2Int> dirtyVisual = new HashSet<Vector2Int>();
            HashSet<Vector2Int> dirtyLight = new HashSet<Vector2Int>();

            instance.SetState(
                x,
                y,
                id,
                FullAmount,
                true,
                0,
                false,
                false,
                dirtyVisual,
                dirtyLight
            );

            foreach (Vector2Int chunk in dirtyVisual)
                instance.RebuildChunkVisual(chunk.x, chunk.y);

            instance.RebuildEmissiveLighting(dirtyLight);
            return true;
        }

        /// <summary>
        /// True only when the supplied world-space point is below the actual
        /// liquid surface. Horizontal flow uses its 1..7/8 visual height while
        /// source and falling cells occupy the full cell height.
        /// </summary>
        public static bool IsPointSubmerged(Vector2 worldPoint)
        {
            World queryWorld = instance != null ? instance.world : null;

            if (queryWorld == null)
            {
                WorldManager manager = WorldManager.Instance;
                if (manager != null && manager.IsReady)
                    queryWorld = manager.GetWorld();
            }

            if (queryWorld == null)
                return false;

            int x = Mathf.FloorToInt(worldPoint.x);
            int y = Mathf.FloorToInt(worldPoint.y);
            ushort id = queryWorld.GetLiquidID(x, y);
            byte amount = queryWorld.GetLiquidAmount(x, y);

            if (id == 0 || amount == 0)
                return false;

            float height = queryWorld.GetLiquidIsSource(x, y) ||
                           queryWorld.GetLiquidIsFalling(x, y)
                ? 1f
                : Mathf.Clamp01(amount / 8f);

            float localY = worldPoint.y - y;
            return localY >= 0f && localY < height - 0.001f;
        }

        // =====================================================
        // WATERFALL FOAM
        // =====================================================

        private void CreateFoamSystem()
        {
            GameObject go = new GameObject("LiquidWaterfallFoam");
            go.transform.SetParent(transform, false);

            foamSystem = go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = foamSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            main.maxParticles = 900;

            ParticleSystem.EmissionModule emission = foamSystem.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = foamSystem.shape;
            shape.enabled = false;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
                foamSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;

            Gradient foamFade = new Gradient();
            foamFade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.72f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(foamFade);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 7;

            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                foamMaterial = new Material(shader);
                foamMaterial.name = "Runtime Waterfall Foam";

                if (foamMaterial.HasProperty("_MainTex"))
                    foamMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);

                if (foamMaterial.HasProperty("_Color"))
                    foamMaterial.SetColor("_Color", Color.white);

                renderer.sharedMaterial = foamMaterial;
            }

            foamSystem.Play();
        }

        private void EmitWaterfallFoam()
        {
            if (foamSystem == null || foamPoints.Count == 0)
                return;

            Camera camera = Camera.main;
            Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
            int emitted = 0;
            const int maxPerBurst = 72;

            foreach (var pair in foamPoints)
            {
                List<FoamPoint> points = pair.Value;
                if (points == null)
                    continue;

                for (int i = 0; i < points.Count; i++)
                {
                    if (emitted >= maxPerBurst)
                        return;

                    FoamPoint point = points[i];

                    if (camera != null && Mathf.Abs(point.Position.x - cameraPosition.x) > 28f)
                        continue;

                    int count = UnityEngine.Random.value < 0.48f ? 2 : 1;

                    for (int p = 0; p < count && emitted < maxPerBurst; p++)
                    {
                        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
                        emit.position = point.Position + new Vector3(
                            UnityEngine.Random.Range(-0.40f, 0.40f),
                            UnityEngine.Random.Range(0.00f, 0.16f),
                            -0.03f
                        );
                        emit.velocity = new Vector3(
                            UnityEngine.Random.Range(-1.55f, 1.55f),
                            UnityEngine.Random.Range(1.15f, 2.65f),
                            0f
                        );
                        emit.startLifetime = UnityEngine.Random.Range(0.32f, 0.62f);
                        emit.startSize = UnityEngine.Random.Range(0.07f, 0.14f);
                        emit.rotation = 45f;
                        emit.startColor = Color.Lerp(
                            Color.white,
                            point.Color,
                            UnityEngine.Random.Range(0.08f, 0.24f)
                        );

                        foamSystem.Emit(emit, 1);
                        emitted++;
                    }
                }
            }
        }

        // =====================================================
        // DAMAGE
        // =====================================================

        private void UpdatePlayerHazard()
        {
            Game.PlayerStats.PlayerStats stats =
                FindFirstObjectByType<Game.PlayerStats.PlayerStats>();

            if (stats == null)
                return;

            Vector3 position = stats.transform.position;

            if (!IsPointSubmerged(new Vector2(position.x, position.y)))
                return;

            int x = Mathf.FloorToInt(position.x);
            int y = Mathf.FloorToInt(position.y);

            byte amount = world.GetLiquidAmount(x, y);
            ushort id = world.GetLiquidID(x, y);

            if (amount == 0 || id == 0)
                return;

            string key = FluidIDRegistry.GetString(id);

            if (!FluidRegistry.TryGet(key, out FluidDefinition definition) ||
                definition == null ||
                definition.Damage <= 0f)
            {
                return;
            }

            float interval = Mathf.Max(0.05f, definition.DamageInterval);
            stats.TakeDamage(definition.Damage * (0.12f / interval));
        }

        // =====================================================
        // LIQUID RENDERING + WORLD LIGHT
        // =====================================================

        private void RebuildChunkVisual(int chunkX, int chunkY)
        {
            if (world == null)
                return;

            Chunk chunk = world.GetChunk(chunkX, chunkY);
            Vector2Int key = new Vector2Int(chunkX, chunkY);

            if (chunk == null)
            {
                DestroyVisual(key);
                return;
            }

            DestroyVisual(key);

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();
            List<Vector2> uvs = new List<Vector2>();
            List<FoamPoint> chunkFoam = new List<FoamPoint>();
            int worldBaseX = chunkX * Chunk.SizeX;
            int worldBaseY = chunkY * Chunk.SizeY;

            for (int x = 0; x < Chunk.SizeX; x++)
            {
                for (int y = 0; y < Chunk.SizeY; y++)
                {
                    byte amount = chunk.GetLiquidAmount(x, y);
                    ushort id = chunk.GetLiquidID(x, y);

                    if (amount == 0 || id == 0)
                        continue;

                    string fluidKey = FluidIDRegistry.GetString(id);
                    Color color = new Color(0.2f, 0.55f, 1f, 0.72f);
                    FluidDefinition definition = null;

                    if (FluidRegistry.TryGet(fluidKey, out definition) &&
                        definition != null)
                    {
                        Color parsed;
                        if (ColorUtility.TryParseHtmlString(definition.Color, out parsed))
                            color = parsed;
                    }

                    if (chunk.GetLiquidIsFalling(x, y) &&
                        definition != null &&
                        definition.WaterfallFoam)
                    {
                        int worldX = worldBaseX + x;
                        int worldY = worldBaseY + y;
                        bool solidImpact = world.GetBlock(worldX, worldY - 1) != 0;
                        bool liquidImpact =
                            world.GetLiquidAmount(worldX, worldY - 1) > 0 &&
                            !world.GetLiquidIsFalling(worldX, worldY - 1);

                        if (solidImpact || liquidImpact)
                        {
                            Color foamColor = Color.Lerp(Color.white, color, 0.15f);
                            foamColor.a = 1f;
                            chunkFoam.Add(new FoamPoint(
                                new Vector3(worldX + 0.5f, worldY + 0.10f, -0.20f),
                                foamColor
                            ));
                        }
                    }

                    float height = chunk.GetLiquidIsFalling(x, y)
                        ? 1f
                        : Mathf.Clamp01(amount / 8f);
                    int start = vertices.Count;

                    vertices.Add(new Vector3(x, y, 0f));
                    vertices.Add(new Vector3(x + 1f, y, 0f));
                    vertices.Add(new Vector3(x + 1f, y + height, 0f));
                    vertices.Add(new Vector3(x, y + height, 0f));

                    colors.Add(color);
                    colors.Add(color);
                    colors.Add(color);
                    colors.Add(color);

                    float u0 = x / (float)Chunk.SizeX;
                    float u1 = (x + 1f) / Chunk.SizeX;
                    float v0 = y / (float)Chunk.SizeY;
                    float v1 = (y + height) / Chunk.SizeY;

                    uvs.Add(new Vector2(u0, v0));
                    uvs.Add(new Vector2(u1, v0));
                    uvs.Add(new Vector2(u1, v1));
                    uvs.Add(new Vector2(u0, v1));

                    triangles.Add(start);
                    triangles.Add(start + 2);
                    triangles.Add(start + 1);
                    triangles.Add(start);
                    triangles.Add(start + 3);
                    triangles.Add(start + 2);
                }
            }

            if (chunkFoam.Count > 0)
                foamPoints[key] = chunkFoam;
            else
                foamPoints.Remove(key);

            if (vertices.Count == 0)
                return;

            LiquidChunkVisual visual = new LiquidChunkVisual();

            visual.Root = new GameObject(
                "Liquids_" + chunkX + "_" + chunkY
            );

            visual.Root.transform.position = new Vector3(
                chunkX * Chunk.SizeX,
                chunkY * Chunk.SizeY,
                -0.15f
            );

            visual.Mesh = new Mesh();
            visual.Mesh.name = "LiquidMesh_" + chunkX + "_" + chunkY;
            visual.Mesh.SetVertices(vertices);
            visual.Mesh.SetTriangles(triangles, 0);
            visual.Mesh.SetColors(colors);
            visual.Mesh.SetUVs(0, uvs);
            visual.Mesh.RecalculateBounds();

            MeshFilter filter = visual.Root.AddComponent<MeshFilter>();
            filter.sharedMesh = visual.Mesh;

            MeshRenderer renderer = visual.Root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = lightingMaterial;
            renderer.sortingOrder = 5;

            visual.LightTexture = new Texture2D(
                LightTextureWidth,
                LightTextureHeight,
                TextureFormat.RGB24,
                false
            );

            visual.LightTexture.name =
                "LiquidLight_" + chunkX + "_" + chunkY;

            visual.LightTexture.filterMode = FilterMode.Bilinear;
            visual.LightTexture.wrapMode = TextureWrapMode.Clamp;

            visual.LightPixels =
                new Color32[LightTextureWidth * LightTextureHeight];

            visual.Properties = new MaterialPropertyBlock();

            if (lightingMaterial != null &&
                lightingMaterial.shader != null &&
                lightingMaterial.shader.name == "Game/ChunkLitSprite")
            {
                float scaleX = (float)Chunk.SizeX / LightTextureWidth;
                float scaleY = (float)Chunk.SizeY / LightTextureHeight;
                float offsetX = (float)LightBorder / LightTextureWidth;
                float offsetY = (float)LightBorder / LightTextureHeight;

                visual.Properties.SetTexture("_MainTex", Texture2D.whiteTexture);
                visual.Properties.SetTexture("_LightTex", visual.LightTexture);
                visual.Properties.SetVector(
                    "_LightUVScaleOffset",
                    new Vector4(scaleX, scaleY, offsetX, offsetY)
                );
                visual.Properties.SetFloat("_LayerBrightness", 1f);
                visual.Properties.SetFloat("_Ambient", 0f);
                visual.Properties.SetFloat("_LightGamma", 0.72f);
                visual.Properties.SetColor("_Color", Color.white);

                renderer.SetPropertyBlock(visual.Properties);
            }

            visuals[key] = visual;
            UpdateVisualLightTexture(chunk, visual);
        }

        private void RefreshAllLightTextures()
        {
            if (world == null)
                return;

            foreach (var pair in visuals)
            {
                Chunk chunk = world.GetChunk(pair.Key.x, pair.Key.y);
                if (chunk != null && pair.Value != null)
                    UpdateVisualLightTexture(chunk, pair.Value);
            }
        }

        private void UpdateVisualLightTexture(
            Chunk chunk,
            LiquidChunkVisual visual
        )
        {
            if (chunk == null ||
                visual == null ||
                visual.LightTexture == null ||
                visual.LightPixels == null)
            {
                return;
            }

            int startWorldX = chunk.X * Chunk.SizeX;
            int startWorldY = chunk.Y * Chunk.SizeY;
            int index = 0;

            for (int lightY = -LightBorder;
                 lightY < Chunk.SizeY + LightBorder;
                 lightY++)
            {
                for (int lightX = -LightBorder;
                     lightX < Chunk.SizeX + LightBorder;
                     lightX++)
                {
                    LightNode node = world.GetLight(
                        startWorldX + lightX,
                        startWorldY + lightY
                    );

                    float sunlight = node.Sun / 15f;
                    float red = Mathf.Max(sunlight, node.R / 15f);
                    float green = Mathf.Max(sunlight, node.G / 15f);
                    float blue = Mathf.Max(sunlight, node.B / 15f);

                    visual.LightPixels[index++] = new Color(
                        red,
                        green,
                        blue,
                        1f
                    );
                }
            }

            visual.LightTexture.SetPixels32(visual.LightPixels);
            visual.LightTexture.Apply(false, false);
        }

        private void RebuildEmissiveLighting(HashSet<Vector2Int> chunks)
        {
            if (world == null || chunks == null || chunks.Count == 0)
                return;

            WorldManager manager = WorldManager.Instance;
            WorldSettings settings = manager == null ? null : manager.GetSettings();
            int worldHeight = settings == null ? 256 : settings.WorldHeight;

            LightPropagationEngine engine = world.GetLightEngine();
            if (engine == null)
                return;

            foreach (Vector2Int chunk in chunks)
            {
                int minX = chunk.x * Chunk.SizeX;
                int minY = chunk.y * Chunk.SizeY;

                engine.RebuildAfterLiquidRegionChanged(
                    minX,
                    minY,
                    minX + Chunk.SizeX - 1,
                    minY + Chunk.SizeY - 1,
                    worldHeight
                );
            }
        }

        private void DestroyVisual(Vector2Int key)
        {
            foamPoints.Remove(key);

            if (!visuals.TryGetValue(key, out LiquidChunkVisual visual))
                return;

            visuals.Remove(key);

            if (visual == null)
                return;

            if (visual.Root != null)
                Destroy(visual.Root);

            if (visual.Mesh != null)
                Destroy(visual.Mesh);

            if (visual.LightTexture != null)
                Destroy(visual.LightTexture);
        }

        private void ClearVisuals()
        {
            List<Vector2Int> keys = new List<Vector2Int>(visuals.Keys);
            for (int i = 0; i < keys.Count; i++)
                DestroyVisual(keys[i]);

            visuals.Clear();
            foamPoints.Clear();
        }

        // =====================================================
        // SAVE
        // =====================================================

        public void Flush()
        {
            if (!SaveGameRuntime.HasActiveSave ||
                saved.Count == 0 ||
                string.IsNullOrWhiteSpace(loadedDimensionKey))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(
                    SavePaths.GetSaveFolder(SaveGameRuntime.CurrentSaveId)
                );

                LiquidSaveFile file = new LiquidSaveFile();

                foreach (CellSave cell in saved.Values)
                    file.Cells.Add(cell);

                File.WriteAllText(
                    SavePath(),
                    JsonUtility.ToJson(file, true)
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning("LIQUID: save failed: " + e.Message);
            }
        }

        private string SavePath()
        {
            string safe = loadedDimensionKey
                .Replace('/', '_')
                .Replace('\\', '_')
                .Replace(':', '_');

            return Path.Combine(
                SavePaths.GetSaveFolder(SaveGameRuntime.CurrentSaveId),
                "liquids_" + safe + ".json"
            );
        }

        private void OnApplicationQuit()
        {
            Flush();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            if (lightingMaterial != null)
                Destroy(lightingMaterial);

            if (foamMaterial != null)
                Destroy(foamMaterial);
        }

        private static long Pack(int x, int y)
        {
            unchecked
            {
                return ((long)x << 32) ^ (uint)y;
            }
        }

        private static void Unpack(long key, out int x, out int y)
        {
            x = (int)(key >> 32);
            y = (int)(uint)key;
        }

        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            int r = a % b;

            if (r != 0 && ((r < 0) != (b < 0)))
                q--;

            return q;
        }
    }
}

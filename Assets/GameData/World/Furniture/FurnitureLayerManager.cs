
using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Inventory.UI;
using Game.Save;
using Game.World.Dimensions;
using Game.World.Items;
using Game.GameplaySystems.Multiblock;
using Game.GameplaySystems.Respawn;
using Game.GameplaySystems.Interactions;
using Game.World.Rendering;
using Game.World.Lighting;


namespace Game.World.Furniture
{

    [Serializable]
    public class FurnitureSaveEntry
    {

        public int X;

        public int Y;

        public string BlockId;

        public byte Transform;

    }


    [Serializable]
    public class GeneratedFurnitureRemovalEntry
    {

        public int X;

        public int Y;

        public string BlockId;

    }


    [Serializable]
    public class FurnitureSaveFile
    {

        public List<FurnitureSaveEntry> Entries =
            new List<FurnitureSaveEntry>();


        // Only tombstones for procedurally generated furniture are saved.
        // The generated grass/flowers themselves are regenerated from the seed
        // every time their chunk is loaded.
        public List<GeneratedFurnitureRemovalEntry> RemovedGenerated =
            new List<GeneratedFurnitureRemovalEntry>();

    }


    public class FurnitureLayerManager :
        MonoBehaviour
    {

        public static FurnitureLayerManager Instance
        {
            get;
            private set;
        }


        public static event Action<int, int, string>
            FurnitureRemoved;


        public static event Action<int, int, string>
            FurniturePlaced;


        private readonly Dictionary<
            long,
            FurnitureSaveEntry
        > data =
            new Dictionary<
                long,
                FurnitureSaveEntry
            >();


        private readonly Dictionary<
            long,
            GameObject
        > visuals =
            new Dictionary<
                long,
                GameObject
            >();


        private readonly HashSet<long> dynamicVisualKeys =
            new HashSet<long>();


        private readonly Dictionary<long, string> dynamicVisualTextureKeys =
            new Dictionary<long, string>();


        private readonly List<long> dynamicVisualScratch =
            new List<long>(64);


        private float nextDynamicVisualUpdate;
        private float nextFurnitureLightingUpdate;
        private Camera furnitureCamera;

        // Reuse the exact chunk-light material instead of repeatedly tinting the
        // SpriteRenderer on the CPU. Each visual owns only a tiny property block.
        private sealed class FurnitureLightingBinding
        {
            public SpriteRenderer Renderer;
            public MaterialPropertyBlock Properties;
            public Texture2D BoundLightTexture;
            public bool BoundFallback;
            public int BoundVineRootY;
        }
        private readonly Dictionary<long, FurnitureLightingBinding>
            furnitureLightingBindings = new Dictionary<long, FurnitureLightingBinding>();
        private Material furnitureLightingMaterial;
        private Material furnitureWindMaterial;
        private bool furnitureWindShaderMissingLogged;
        private bool furnitureShaderMissingLogged;



        // Active procedural furniture exists only while its chunk is loaded.
        // It is deliberately NOT written to the furniture save file.
        private readonly HashSet<long> generatedKeys =
            new HashSet<long>();


        // A removed generated plant must not respawn when the chunk reloads.
        // Only these small tombstones are persisted.
        private readonly Dictionary<long, string> removedGenerated =
            new Dictionary<long, string>();


        // RGB block light has a maximum level of 15.
        // +2 matches the lighting propagation safety radius.



        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void EnsureCreated()
        {

            EnsureInstance();

        }


        public static FurnitureLayerManager EnsureInstance()
        {

            if (
                Instance !=
                null
            )
            {

                return Instance;

            }


            if (
                WorldManager.Instance ==
                null
            )
            {

                return null;

            }


            GameObject gameObject =
                new GameObject(
                    "FurnitureLayer"
                );


            gameObject.transform.SetParent(
                WorldManager.Instance.transform,
                false
            );


            return
                gameObject.AddComponent<
                    FurnitureLayerManager
                >();

        }


        private void Awake()
        {

            if (
                Instance != null
                &&
                Instance != this
            )
            {

                Destroy(
                    gameObject
                );


                return;

            }


            Instance =
                this;


            Load();

        }


        private void OnDestroy()
        {
            furnitureLightingBindings.Clear();
            if (furnitureLightingMaterial != null)
            {
                Destroy(furnitureLightingMaterial);
                furnitureLightingMaterial = null;
            }
            if (furnitureWindMaterial != null)
            {
                Destroy(furnitureWindMaterial);
                furnitureWindMaterial = null;
            }
            if (Instance == this)
                Instance = null;
        }


        // =====================================================
        // ACCESS
        // =====================================================

        public string GetFurniture(
            int x,
            int y
        )
        {

            return
                data.TryGetValue(
                    Pack(
                        x,
                        y
                    ),
                    out FurnitureSaveEntry entry
                )
                    ? entry.BlockId
                    : null;

        }

        public byte GetFurnitureTransform(
            int x,
            int y
        )
        {
            return
                data.TryGetValue(
                    Pack(
                        x,
                        y
                    ),
                    out FurnitureSaveEntry entry
                )
                    ? entry.Transform
                    : (byte)0;
        }



        public bool HasFurniture(
            int x,
            int y
        )
        {

            return
                !string.IsNullOrWhiteSpace(
                    GetFurniture(
                        x,
                        y
                    )
                );

        }


        public bool TryGetDefinition(
            int x,
            int y,
            out BlockDefinition definition
        )
        {

            definition =
                null;


            string id =
                GetFurniture(
                    x,
                    y
                );


            if (
                string.IsNullOrWhiteSpace(
                    id
                )
            )
            {

                return false;

            }


            try
            {

                ContentID contentID =
                    ContentID.Parse(
                        id
                    );


                if (
                    !BlockRegistry.Contains(
                        contentID
                    )
                )
                {

                    return false;

                }


                definition =
                    BlockRegistry.Get(
                        contentID
                    );


                return
                    definition !=
                    null;

            }
            catch
            {

                return false;

            }

        }


        // =====================================================
        // PLACE
        // =====================================================

        public bool SetFurniture(
            int x,
            int y,
            string blockId
        )
        {
            return
                SetFurniture(
                    x,
                    y,
                    blockId,
                    0
                );
        }


        public bool SetFurniture(int x, int y, string blockId, byte transform)
        {
            return SetFurniture(x, y, blockId, transform, false);
        }

        public bool SetFurniture(
            int x,
            int y,
            string blockId,
            byte transform,
            bool structurePlacement
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
                ||
                HasFurniture(
                    x,
                    y
                )
            )
            {

                return false;

            }


            if (!structurePlacement && FurnitureSupportRuntime.IsFurnitureOnlyBlock(blockId) &&
                !FurnitureSupportRuntime.HasSupport(x, y, blockId)) return false;

            InteractiveBlockRuntime.ClearStateAt(
                x,
                y,
                BlockVisualLayer.Furniture,
                true
            );


            FurnitureSaveEntry entry =
                new FurnitureSaveEntry
                {
                    X =
                        x,

                    Y =
                        y,

                    BlockId =
                        blockId,

                    Transform =
                        transform
                };


            data[
                Pack(
                    x,
                    y
                )
            ] =
                entry;


            CreateVisual(
                entry
            );


            Save();


            FurniturePlaced?.Invoke(
                x,
                y,
                blockId
            );


            RebuildLightingAndVisuals(
                x,
                y
            );


            return true;

        }


        // =====================================================
        // PROCEDURAL FURNITURE
        // =====================================================

        public bool SetGeneratedFurniture(
            int x,
            int y,
            string blockId
        )
        {
            return
                SetGeneratedFurniture(
                    x,
                    y,
                    blockId,
                    0
                );
        }


        public bool SetGeneratedFurniture(
            int x,
            int y,
            string blockId,
            byte transform
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
                ||
                HasFurniture(
                    x,
                    y
                )
                ||
                WasGeneratedFurnitureRemoved(
                    x,
                    y,
                    blockId
                )
            )
            {

                return false;

            }


            long key =
                Pack(
                    x,
                    y
                );


            FurnitureSaveEntry entry =
                new FurnitureSaveEntry
                {
                    X =
                        x,

                    Y =
                        y,

                    BlockId =
                        blockId,

                    Transform =
                        transform
                };


            data[
                key
            ] =
                entry;


            generatedKeys.Add(
                key
            );


            CreateVisual(
                entry
            );


            return true;

        }


        public bool WasGeneratedFurnitureRemoved(
            int x,
            int y,
            string blockId
        )
        {

            long key =
                Pack(
                    x,
                    y
                );


            if (
                !removedGenerated.TryGetValue(
                    key,
                    out string removedBlockId
                )
            )
            {

                return false;

            }


            return
                string.Equals(
                    removedBlockId,
                    blockId,
                    StringComparison.OrdinalIgnoreCase
                );

        }


        public void UnloadGeneratedFurnitureChunk(
            int chunkX,
            int chunkY
        )
        {

            if (
                generatedKeys.Count ==
                0
            )
            {

                return;

            }


            List<long> remove =
                new List<long>();


            foreach (
                long key
                in generatedKeys
            )
            {

                int x =
                    UnpackX(
                        key
                    );


                int y =
                    UnpackY(
                        key
                    );


                int entryChunkX =
                    Mathf.FloorToInt(
                        x /
                        (float)Chunk.SizeX
                    );


                int entryChunkY =
                    Mathf.FloorToInt(
                        y /
                        (float)Chunk.SizeY
                    );


                if (
                    entryChunkX ==
                    chunkX
                    &&
                    entryChunkY ==
                    chunkY
                )
                {

                    remove.Add(
                        key
                    );

                }

            }


            for (
                int i = 0;
                i < remove.Count;
                i++
            )
            {

                long key =
                    remove[i];


                generatedKeys.Remove(
                    key
                );


                data.Remove(
                    key
                );


                if (
                    visuals.TryGetValue(
                        key,
                        out GameObject visual
                    )
                    &&
                    visual !=
                    null
                )
                {

                    Destroy(
                        visual
                    );

                }


                visuals.Remove(
                    key
                );
                furnitureLightingBindings.Remove(key);

            }

        }


        // =====================================================
        // REMOVE
        // =====================================================

        public bool RemoveFurniture(
            int x,
            int y
        )
        {
            return RemoveFurnitureInternal(
                x,
                y,
                false
            );
        }


        private bool RemoveFurnitureInternal(
            int x,
            int y,
            bool deferExpensiveWork
        )
        {

            long key =
                Pack(
                    x,
                    y
                );


            if (
                !data.TryGetValue(
                    key,
                    out FurnitureSaveEntry entry
                )
            )
            {

                return false;

            }


            bool wasGenerated =
                generatedKeys.Remove(
                    key
                );


            if (
                wasGenerated
            )
            {

                removedGenerated[
                    key
                ] =
                    entry.BlockId;

            }


            MultiBlockRecord multiBlockRecord =
                MultiBlockStore.FindAnchor(
                    x,
                    y
                );


            data.Remove(
                key
            );


            if (
                multiBlockRecord != null &&
                (
                    string.Equals(
                        multiBlockRecord.Kind,
                        "bed",
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    MultiBlockMetadataRegistry.HasTag(
                        multiBlockRecord.BlockId,
                        "bed"
                    )
                )
            )
            {
                RespawnPointService.InvalidateIfBedAnchor(
                    x,
                    y,
                    entry.BlockId
                );
            }


            MultiBlockStore.RemoveAnchorAt(
                x,
                y,
                entry.BlockId
            );


            InteractiveBlockRuntime.ClearStateAt(
                x,
                y,
                BlockVisualLayer.Furniture,
                true
            );


            dynamicVisualKeys.Remove(key);
            dynamicVisualTextureKeys.Remove(key);


            if (
                visuals.TryGetValue(
                    key,
                    out GameObject visual
                ) &&
                visual != null
            )
            {

                Destroy(
                    visual
                );

            }


            visuals.Remove(
                key
            );
            furnitureLightingBindings.Remove(key);


            if (!deferExpensiveWork)
            {
                Save();
            }


            // Events stay immediate even in batch mode. Systems such as chests,
            // furnaces and multiblocks still see the removal on the exact frame
            // where the visual disappears.
            FurnitureRemoved?.Invoke(
                x,
                y,
                entry.BlockId
            );
            FurnitureSupportRuntime.SupportChanged(x, y);


            if (!deferExpensiveWork)
            {
                RebuildLightingAndVisuals(
                    x,
                    y
                );
            }


            return true;

        }


        // =====================================================
        // REMOVE + DROP BLOCK ITEM
        // =====================================================

        public bool BreakFurniture(
            int x,
            int y
        )
        {
            return BreakFurnitureInternal(
                x,
                y,
                false
            );
        }


        // Tree cascade fast path. Saving and light-texture refresh are deferred
        // until the collapse job finishes, instead of writing the same furniture
        // save file and rebuilding lighting once for every leaf/branch cell.
        public bool BreakFurnitureDeferred(
            int x,
            int y
        )
        {
            return BreakFurnitureInternal(
                x,
                y,
                true
            );
        }


        private bool BreakFurnitureInternal(
            int x,
            int y,
            bool deferExpensiveWork
        )
        {

            string blockId =
                GetFurniture(
                    x,
                    y
                );


            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
            )
            {

                return false;

            }


            BlockDefinition definition = null;
            TryGetDefinition(x, y, out definition);

            var plantDrops = Game.Gameplay.Plants.PlantRuntime.Harvest(definition,x,y);

            if (
                !RemoveFurnitureInternal(
                    x,
                    y,
                    deferExpensiveWork
                )
            )
            {

                return false;

            }


            Vector2 dropPosition = new Vector2(x + 0.5f, y + 0.65f);
            if (plantDrops == null) BlockLootDropper.SpawnDrops(definition, blockId, dropPosition);
            else foreach (var drop in plantDrops) ItemDropSpawner.EnsureInstance().SpawnFromBlock(drop.ItemId,drop.Count,dropPosition);

            return true;

        }


        /// <summary>
        /// Completes a deferred tree-furniture batch. The save file is written once
        /// and lighting is sampled across the affected bounds instead of being fully
        /// rebuilt for every single destroyed furniture cell.
        /// </summary>
        public void FlushDeferredTreeChanges(
            int minX,
            int minY,
            int maxX,
            int maxY
        )
        {
            Save();

            if (
                minX > maxX ||
                minY > maxY
            )
            {
                return;
            }

            const int LightingSampleStep = 12;

            for (
                int y = minY;
                y <= maxY;
                y += LightingSampleStep
            )
            {
                for (
                    int x = minX;
                    x <= maxX;
                    x += LightingSampleStep
                )
                {
                    RebuildLightingAndVisuals(
                        x,
                        y
                    );
                }
            }

            // Always include the far corner when bounds are not exact multiples
            // of the sample step. This keeps large authored trees correct too.
            if (
                maxX != minX ||
                maxY != minY
            )
            {
                RebuildLightingAndVisuals(
                    maxX,
                    maxY
                );
            }
        }


        // =====================================================
        // VISUAL
        // =====================================================

        private void CreateVisual(
            FurnitureSaveEntry entry
        )
        {

            long key =
                Pack(
                    entry.X,
                    entry.Y
                );


            dynamicVisualKeys.Remove(key);
            dynamicVisualTextureKeys.Remove(key);


            if (
                visuals.TryGetValue(
                    key,
                    out GameObject old
                )
                &&
                old !=
                null
            )
            {

                Destroy(
                    old
                );

            }


            GameObject gameObject =
                new GameObject(
                    "Furniture_" +
                    entry.BlockId +
                    "_" +
                    entry.X +
                    "_" +
                    entry.Y
                );


            gameObject.transform.SetParent(
                transform,
                false
            );


            gameObject.transform.position =
                new Vector3(
                    entry.X +
                    0.5f,

                    entry.Y +
                    0.5f,

                    0f
                );


            SpriteRenderer spriteRenderer =
                gameObject.AddComponent<
                    SpriteRenderer
                >();


            // Background = 0
            // Furniture = 1
            // Foreground = 2
            spriteRenderer.sortingOrder =
                1;


            BlockDefinition definition = null;
            bool hasDefinition =
                TryGetDefinition(
                    entry.X,
                    entry.Y,
                    out definition
                );


            Sprite visualSprite = null;

            if (hasDefinition)
            {
                visualSprite =
                    ResolveFurnitureSprite(
                        entry.BlockId,
                        definition,
                        entry.X,
                        entry.Y
                    );

                if (BlockVisualResolver.IsDynamic(definition))
                {
                    dynamicVisualKeys.Add(key);
                    dynamicVisualTextureKeys[key] =
                        ResolveFurnitureTextureName(
                            definition,
                            entry.X,
                            entry.Y
                        );
                }
            }


            spriteRenderer.sprite =
                visualSprite != null
                    ? visualSprite
                    : ItemIconProvider.GetIcon(
                        entry.BlockId
                    );


            // The base sprite tint must remain white: the shared chunk-light
            // shader applies world lighting on the GPU, including day/night.
            spriteRenderer.color = Color.white;


            if (hasDefinition)
            {
                Vector2 multiBlockOffset =
                    MultiBlockVisualProvider.GetAnchorToCenterOffset(
                        definition
                    );


                gameObject.transform.position +=
                    new Vector3(
                        multiBlockOffset.x + definition.VisualOffsetX,
                        multiBlockOffset.y + definition.VisualOffsetY,
                        0f
                    );


                gameObject.transform.localScale =
                    Vector3.one *
                    Mathf.Max(
                        0.01f,
                        definition.VisualScale
                    );


                if (
                    HasTag(
                        definition,
                        "torch"
                    )
                    ||
                    HasTag(
                        definition,
                        "fire"
                    )
                )
                {

                    CreateFireParticles(
                        gameObject.transform
                    );

                }

            }


            ApplyFurnitureTransform(
                gameObject.transform,
                entry.Transform
            );


            visuals[
                key
            ] =
                gameObject;

            InitializeFurnitureLighting(key, entry, spriteRenderer);
        }


        public void RefreshVisual(
            int x,
            int y
        )
        {
            long key = Pack(x, y);

            if (!data.TryGetValue(key, out FurnitureSaveEntry entry))
                return;

            if (!visuals.TryGetValue(key, out GameObject visual) || visual == null)
                return;

            if (!TryGetDefinition(x, y, out BlockDefinition definition) || definition == null)
                return;

            // Resolve the CURRENT state/frame. The previous implementation always
            // returned the base multi-block sprite here, so StateChanged("burning")
            // immediately re-applied furnace.png and then incorrectly marked the
            // burning texture key as already rendered.
            Sprite sprite = ResolveFurnitureSprite(
                entry.BlockId,
                definition,
                x,
                y
            );

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer != null && sprite != null)
                renderer.sprite = sprite;

            if (BlockVisualResolver.IsDynamic(definition))
            {
                dynamicVisualKeys.Add(key);
                dynamicVisualTextureKeys[key] =
                    ResolveFurnitureTextureName(
                        definition,
                        x,
                        y
                    );
            }
            else
            {
                dynamicVisualKeys.Remove(key);
                dynamicVisualTextureKeys.Remove(key);
            }
        }


        private void Update()
        {
            RefreshDayNightFurnitureLighting();
            if (dynamicVisualKeys.Count == 0 || Time.time < nextDynamicVisualUpdate)
                return;

            nextDynamicVisualUpdate = Time.time + 1f / 60f;

            dynamicVisualScratch.Clear();
            foreach (long key in dynamicVisualKeys)
                dynamicVisualScratch.Add(key);

            for (int i = 0; i < dynamicVisualScratch.Count; i++)
            {
                long key = dynamicVisualScratch[i];

                if (
                    !data.TryGetValue(key, out FurnitureSaveEntry entry) ||
                    !visuals.TryGetValue(key, out GameObject visual) ||
                    visual == null ||
                    !TryGetDefinition(entry.X, entry.Y, out BlockDefinition definition) ||
                    definition == null
                )
                {
                    dynamicVisualKeys.Remove(key);
                    dynamicVisualTextureKeys.Remove(key);
                    continue;
                }

                if (!BlockVisualResolver.IsDynamic(definition))
                {
                    dynamicVisualKeys.Remove(key);
                    dynamicVisualTextureKeys.Remove(key);
                    continue;
                }

                string currentKey =
                    ResolveFurnitureTextureName(
                        definition,
                        entry.X,
                        entry.Y
                    );

                dynamicVisualTextureKeys.TryGetValue(key, out string previousKey);
                if (string.Equals(previousKey, currentKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                Sprite sprite =
                    ResolveFurnitureSprite(
                        entry.BlockId,
                        definition,
                        entry.X,
                        entry.Y
                    );

                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                if (renderer != null && sprite != null)
                    renderer.sprite = sprite;

                dynamicVisualTextureKeys[key] = currentKey;
            }
        }


        private bool EnsureFurnitureWindMaterial()
        {
            if (furnitureWindMaterial != null)
                return true;

            Shader shader = UnityEngine.Resources.Load<Shader>(
                "Shaders/PixelatedWaveLitSprite"
            );

            if (shader == null)
                shader = Shader.Find("Game/PixelatedWaveLitSprite");

            if (shader == null)
            {
                if (!furnitureWindShaderMissingLogged)
                {
                    furnitureWindShaderMissingLogged = true;
                    Debug.LogWarning(
                        "FURNITURE WIND: Game/PixelatedWaveLitSprite shader missing. " +
                        "Wave furniture will use the regular lighting material."
                    );
                }

                return false;
            }

            furnitureWindMaterial = new Material(shader);
            furnitureWindMaterial.name = "Runtime Furniture Wind Lighting";

            furnitureWindMaterial.SetFloat("_WindStrength", 5f);
            furnitureWindMaterial.SetFloat("_WindSpeed", 1f);
            furnitureWindMaterial.SetFloat("_WindFrequency", 1.1f);
            furnitureWindMaterial.SetFloat("_BlockPixelSize", 16f);

            return true;
        }

        private bool EnsureFurnitureLightingMaterial()
        {
            if (furnitureLightingMaterial != null)
                return true;
            Shader shader = Shader.Find("Game/ChunkLitSprite");
            if (shader == null)
            {
                if (!furnitureShaderMissingLogged)
                {
                    furnitureShaderMissingLogged = true;
                    Debug.LogWarning("FURNITURE: Game/ChunkLitSprite shader missing; " +
                        "using unlit sprites until the shader is available.");
                }
                return false;
            }
            furnitureLightingMaterial = new Material(shader);
            furnitureLightingMaterial.name = "Runtime Furniture Chunk Lighting";
            return true;
        }


        private void InitializeFurnitureLighting(
            long key,
            FurnitureSaveEntry entry,
            SpriteRenderer renderer)
        {
            if (renderer == null)
                return;

            bool hasDefinition = TryGetDefinition(
                entry.X,
                entry.Y,
                out BlockDefinition definition
            );

            bool hasWaveTag = hasDefinition && HasTag(definition, "wave");

            if (hasWaveTag && EnsureFurnitureWindMaterial())
            {
                renderer.sharedMaterial = furnitureWindMaterial;
            }
            else
            {
                if (!EnsureFurnitureLightingMaterial())
                    return;

                renderer.sharedMaterial = furnitureLightingMaterial;
            }

            renderer.color = Color.white;

            FurnitureLightingBinding binding = new FurnitureLightingBinding
            {
                Renderer = renderer,
                Properties = new MaterialPropertyBlock(),
                BoundLightTexture = null,
                BoundFallback = false
            };

            furnitureLightingBindings[key] = binding;
            BindFurnitureLight(entry, binding, true);
        }


        private void BindFurnitureLight(
            FurnitureSaveEntry entry, FurnitureLightingBinding binding, bool force)
        {
            if (entry == null || binding == null || binding.Renderer == null)
                return;

            WorldManager manager = WorldManager.Instance;
            ChunkRenderer chunkRenderer = manager != null ? manager.GetChunkRenderer() : null;

            // The anchor of a multi-block uses its own chunk; a one-block border
            // in the texture covers adjacent edge pixels as well.
            int chunkX = Mathf.FloorToInt(entry.X / (float)Chunk.SizeX);
            int chunkY = Mathf.FloorToInt(entry.Y / (float)Chunk.SizeY);
            Texture2D texture = null;
            bool hasTexture = chunkRenderer != null &&
                chunkRenderer.TryGetChunkLightTexture(chunkX, chunkY, out texture);
            if (!hasTexture)
                texture = Texture2D.whiteTexture;

            bool hanging = FurnitureSupportRuntime.IsHanging(entry.BlockId);
            int rootY = entry.Y + 1;
            if (hanging)
                for (int up = 1; up < 9 && FurnitureSupportRuntime.IsHanging(GetFurniture(entry.X, entry.Y + up)); up++)
                    rootY++;
            bool fallback = !hasTexture;
            if (!force && binding.BoundLightTexture == texture &&
                binding.BoundFallback == fallback && binding.BoundVineRootY == rootY)
                return;

            // A single full-screen shader also uses the same scale and border
            // as ChunkRenderer: 32 playable cells + one texel of border each side.
            float width = Chunk.SizeX + 2f;
            float height = Chunk.SizeY + 2f;
            Vector4 worldUV = new Vector4(
                1f / width, 1f / height,
                (1f - chunkX * Chunk.SizeX) / width,
                (1f - chunkY * Chunk.SizeY) / height);


            binding.Properties.Clear();
            binding.Properties.SetTexture("_LightTex", texture);
            binding.Properties.SetVector("_FurnitureWorldLightUV", worldUV);
            binding.Properties.SetFloat("_FurnitureWorldLightMode", 1f);
            binding.Properties.SetFloat("_FurnitureFallbackMode", fallback ? 1f : 0f);
            binding.Properties.SetFloat("_Ambient", .05f);
            binding.Properties.SetFloat("_LayerBrightness", 1f);
            binding.Properties.SetFloat("_LightGamma", .72f);

            // Stable wind phase: neighbouring furniture uses world coordinates,
            // not its position in the hierarchy.
            binding.Properties.SetVector(
                "_WindWorldOffset",
                new Vector4(entry.X, entry.Y, 0f, 0f)
            );

            binding.Properties.SetFloat("_WindHanging", hanging ? 1f : 0f);
            binding.Properties.SetFloat("_WindRootY", rootY);
            binding.Properties.SetFloat("_WindPlantMode", 1f);
            binding.Renderer.SetPropertyBlock(binding.Properties);

            binding.BoundLightTexture = texture;
            binding.BoundFallback = fallback;
            binding.BoundVineRootY = rootY;
        }

        // 5Hz only checks for replacement/unloaded chunk textures. Light intensity,
        // day/night, torch emission and moving light changes render directly on the
        // GPU from the live light texture; there is no CPU color sampling at all.
        private void RefreshDayNightFurnitureLighting()
        {
            if (Time.time < nextFurnitureLightingUpdate) return;
            nextFurnitureLightingUpdate = Time.time + .20f;
            if (
    furnitureLightingMaterial == null &&
    furnitureWindMaterial == null
)
            {
                return;
            }

            foreach (KeyValuePair<long, FurnitureLightingBinding> pair
                in furnitureLightingBindings)
            {
                FurnitureLightingBinding binding = pair.Value;
                if (binding.Renderer == null || !binding.Renderer.isVisible)
                    continue;
                FurnitureSaveEntry entry;
                if (!data.TryGetValue(pair.Key, out entry))
                    continue;
                BindFurnitureLight(entry, binding, false);
            }
        }

        private static Sprite ResolveFurnitureSprite(
            string blockId,
            BlockDefinition definition,
            int x,
            int y
        )
        {
            if (MultiBlockVisualProvider.IsMultiBlock(definition))
            {
                Sprite multiBlockSprite =
                    MultiBlockVisualProvider.ResolveSprite(
                        blockId,
                        definition,
                        x,
                        y,
                        BlockVisualLayer.Furniture
                    );

                if (multiBlockSprite != null)
                    return multiBlockSprite;
            }

            return BlockVisualResolver.ResolveSprite(
                definition,
                x,
                y,
                BlockVisualLayer.Furniture
            );
        }


        private static string ResolveFurnitureTextureName(
            BlockDefinition definition,
            int x,
            int y
        )
        {
            if (MultiBlockVisualProvider.IsMultiBlock(definition))
            {
                string multiBlockName =
                    MultiBlockVisualProvider.ResolveTextureName(
                        definition,
                        x,
                        y,
                        BlockVisualLayer.Furniture
                    );

                if (!string.IsNullOrWhiteSpace(multiBlockName))
                    return "multiblock:" + multiBlockName;
            }

            return "block:" + (
                BlockVisualResolver.ResolveTextureName(
                    definition,
                    x,
                    y,
                    BlockVisualLayer.Furniture
                ) ?? string.Empty
            );
        }


        private static void ApplyFurnitureTransform(
            Transform target,
            byte transform
        )
        {
            if (
                target ==
                null
            )
            {
                return;
            }


            int rotation =
                transform &
                0x03;


            bool mirror =
                (
                    transform &
                    0x04
                )
                !=
                0;


            target.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -rotation *
                    90f
                );


            Vector3 scale =
                target.localScale;


            scale.x =
                Mathf.Abs(
                    scale.x
                )
                *
                (
                    mirror
                        ? -1f
                        : 1f
                );


            target.localScale =
                scale;
        }


        private void CreateFireParticles(Transform parent)
        {
            // PixelWorldEffects owns one bounded particle buffer for all visible fires.
            // Leave this hook for older furniture callers; avoid a duplicate emitter per torch.
        }


        private bool HasTag(
            BlockDefinition definition,
            string tag
        )
        {

            if (
                definition ==
                null
                ||
                definition.Tags ==
                null
            )
            {

                return false;

            }


            for (
                int i = 0;
                i < definition.Tags.Count;
                i++
            )
            {

                if (
                    string.Equals(
                        definition.Tags[i],
                        tag,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {

                    return true;

                }

            }


            return false;

        }


        // =====================================================
        // LIGHTING
        // =====================================================

        private void RebuildLightingAndVisuals(int x,int y)
        {
            var manager=WorldManager.Instance;
            if(manager==null||manager.GetWorld()==null||manager.GetSettings()==null)return;
            // Furniture has its own sprite layer. Only the light needs rebuilding;
            // the engine publishes dirty tiles and WorldManager uploads their light
            // textures after completion, without redrawing surrounding terrain.
            manager.GetWorld().GetLightEngine().RebuildAfterBlockChanged(
                x,y,manager.GetSettings().WorldHeight,false);
        }





        // =====================================================
        // SAVE
        // =====================================================

        private string GetFile()
        {

            if (
                !SaveGameRuntime.HasActiveSave
                ||
                DimensionTravelRuntime.Current ==
                null
            )
            {

                return null;

            }


            string dimensionFile =
                SavePaths.GetDimensionFile(
                    SaveGameRuntime.CurrentSaveId,
                    DimensionTravelRuntime.Current.Name,
                    DimensionTravelRuntime.Current.Seed
                );


            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    dimensionFile
                )
            );


            return
                dimensionFile +
                ".furniture.json";

        }


        private void Load()
        {

            data.Clear();

            generatedKeys.Clear();

            removedGenerated.Clear();


            foreach (
                KeyValuePair<long, GameObject> pair
                in visuals
            )
            {

                if (
                    pair.Value !=
                    null
                )
                {

                    Destroy(
                        pair.Value
                    );

                }

            }


            visuals.Clear();
            furnitureLightingBindings.Clear();

            dynamicVisualKeys.Clear();
            dynamicVisualTextureKeys.Clear();


            string file =
                GetFile();


            if (
                string.IsNullOrWhiteSpace(
                    file
                )
                ||
                !File.Exists(
                    file
                )
            )
            {

                return;

            }


            try
            {

                FurnitureSaveFile save =
                    JsonUtility.FromJson<
                        FurnitureSaveFile
                    >(
                        File.ReadAllText(
                            file
                        )
                    );


                if (
                    save ==
                    null
                    ||
                    save.Entries ==
                    null
                )
                {

                    return;

                }


                if (
                    save.RemovedGenerated !=
                    null
                )
                {

                    for (
                        int i = 0;
                        i < save.RemovedGenerated.Count;
                        i++
                    )
                    {

                        GeneratedFurnitureRemovalEntry removed =
                            save.RemovedGenerated[i];


                        if (
                            removed ==
                            null
                            ||
                            string.IsNullOrWhiteSpace(
                                removed.BlockId
                            )
                        )
                        {

                            continue;

                        }


                        removedGenerated[
                            Pack(
                                removed.X,
                                removed.Y
                            )
                        ] =
                            removed.BlockId;

                    }

                }


                for (
                    int i = 0;
                    i < save.Entries.Count;
                    i++
                )
                {

                    FurnitureSaveEntry entry =
                        save.Entries[i];


                    if (
                        entry ==
                        null
                        ||
                        string.IsNullOrWhiteSpace(
                            entry.BlockId
                        )
                    )
                    {

                        continue;

                    }


                    data[
                        Pack(
                            entry.X,
                            entry.Y
                        )
                    ] =
                        entry;


                    CreateVisual(
                        entry
                    );

                }

                // Remove occupancy records whose real furniture anchor no longer
                // exists. Older builds could leave these behind after a bed was broken.
                MultiBlockStore.PruneAgainstFurniture(this);

            }
            catch (
                Exception exception
            )
            {

                Debug.LogError(
                    "FURNITURE LOAD: " +
                    exception
                );

            }

        }


        public void Flush()
        {
            Save();
        }


        private void Save()
        {

            string file =
                GetFile();


            if (
                string.IsNullOrWhiteSpace(
                    file
                )
            )
            {

                return;

            }


            try
            {

                FurnitureSaveFile save =
                    new FurnitureSaveFile();


                foreach (
                    KeyValuePair<long, FurnitureSaveEntry> pair
                    in data
                )
                {

                    if (
                        generatedKeys.Contains(
                            pair.Key
                        )
                    )
                    {

                        continue;

                    }


                    save.Entries.Add(
                        pair.Value
                    );

                }


                foreach (
                    KeyValuePair<long, string> pair
                    in removedGenerated
                )
                {

                    save.RemovedGenerated.Add(
                        new GeneratedFurnitureRemovalEntry
                        {
                            X =
                                UnpackX(
                                    pair.Key
                                ),

                            Y =
                                UnpackY(
                                    pair.Key
                                ),

                            BlockId =
                                pair.Value
                        }
                    );

                }


                File.WriteAllText(
                    file,
                    JsonUtility.ToJson(
                        save,
                        true
                    )
                );

            }
            catch (
                Exception exception
            )
            {

                Debug.LogError(
                    "FURNITURE SAVE: " +
                    exception
                );

            }

        }


        private static long Pack(
            int x,
            int y
        )
        {

            return
                (
                    (long)x <<
                    32
                )
                ^
                (uint)y;

        }


        private static int UnpackX(
            long key
        )
        {

            return
                (int)(
                    key >>
                    32
                );

        }


        private static int UnpackY(
            long key
        )
        {

            return
                unchecked(
                    (int)(uint)key
                );

        }

    }

}

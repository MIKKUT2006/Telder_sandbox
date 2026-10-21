using Game.Blocks;
using Game.Content;
using Game.World.Collision;
using Game.World.Dimensions;
using Game.World.Generation;
using Game.World.Lighting;
using Game.World.Loading;
using Game.World.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;
using Game.World.Effects;
using Game.Save;
using Game.GameplaySystems;
using Game.GameplaySystems.Furnace;
using Game.GameplaySystems.Interactions;
using Game.World.Weather;
using Game.Networking;
using UnityEngine;

namespace Game.World
{
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance;
        private Game.World.Voxel3D.VoxelWorld3DRuntime voxelRuntime;


        // =====================================================
        // PLAYER
        // =====================================================

        [SerializeField]
        private Transform player;


        // =====================================================
        // SYSTEMS
        // =====================================================

        private World world;

        private WorldSettings settings;

        private WorldCollision worldCollision;

        private ChunkCollision chunkCollision;

        // Deduplicated post-edit collision rebuilds: a whole mining burst in one
        // chunk requests ONE rebuild rather than a rebuild for every cell.
        private readonly Queue<UnityEngine.Vector2Int> pendingCollisionQueue =
            new Queue<UnityEngine.Vector2Int>();
        private readonly HashSet<UnityEngine.Vector2Int> pendingCollisionSet =
            new HashSet<UnityEngine.Vector2Int>();

        private WorldGenerator generator;

        private ChunkLoader loader;

        private ChunkRenderer renderer;

        private LightPropagationEngine lighting;


        // =====================================================
        // STATE
        // =====================================================

        private bool worldGenerated;
        private bool initialLightingReady;
        // Movement and opening visuals must wait for collision-ready surface terrain.
        private bool initialSpawnReady;
        private int plannedSpawnX;
        private int plannedSurfaceY;
        private Vector2 initialSpawnFeet;

        // WorldLoadingOverlay reads this state on-device rather than waiting
        // forever without showing which initialization step failed.
        private string startupStatus = "Ожидание данных мира...";
        private string startupError;
        private float startupProgress;
        public string StartupStatus { get { return startupStatus; } }
        public string StartupError { get { return startupError; } }
        public float StartupProgress { get { return startupProgress; } }

        private void ReportStartupFailure(string stage, Exception exception)
        {
            startupError = stage + ": " + exception.Message;
            startupStatus = "Ошибка загрузки мира";
            Debug.LogError("TELDER WORLD STARTUP: " + startupError, this);
            Debug.LogException(exception, this);
        }

        private void ReportStartupFailure(string message)
        {
            startupError = message;
            startupStatus = "Ошибка загрузки мира";
            Debug.LogError("TELDER WORLD STARTUP: " + message, this);
        }

        public bool InitialSpawnReady { get { return initialSpawnReady; } }
        public Vector2 InitialSpawnFeet { get { return initialSpawnFeet; } }
        public bool InitialSpawnVisualReady
        {
            get
            {
                if (voxelRuntime != null) return voxelRuntime.Ready;
                if (!initialSpawnReady || renderer == null) return false;
                int chunkX = Mathf.FloorToInt(initialSpawnFeet.x / Chunk.SizeX);
                int chunkY = Mathf.FloorToInt((initialSpawnFeet.y - 0.1f) / Chunk.SizeY);
                return renderer.HasChunkVisual(chunkX, chunkY);
            }
        }


        public bool InitialLightingReady
        {
            get { return initialLightingReady; }
        }


        public bool IsReady
        {
            get
            {
                return
                    worldGenerated &&
                    loader != null &&
                    world != null;
            }
        }


        // =====================================================
        // AWAKE
        // =====================================================

        private void Awake()
        {
            // Explicit runtime reference also protects the touch HUD from
            // IL2CPP managed code stripping and bootstrap order differences.
            Game.InputSupport.Mobile.MobileGameplayControls.EnsureInstalled();
            Instance = this;
            startupStatus = "Ожидание игровых ресурсов...";
            if (!MobileGameDataBootstrap.IsReady)
            {
                StartCoroutine(InitializeWhenGameDataReady());
                return;
            }
            TryInitializeWorldSystems();
        }

        private IEnumerator InitializeWhenGameDataReady()
        {
            while (!MobileGameDataBootstrap.IsReady)
            {
                if (MobileGameDataBootstrap.HasError)
                {
                    ReportStartupFailure("GameData: " + MobileGameDataBootstrap.ErrorMessage);
                    yield break;
                }
                yield return null;
            }
            TryInitializeWorldSystems();
        }

        private void TryInitializeWorldSystems()
        {
            try
            {
                startupStatus = "Инициализация блоков, предметов и генератора...";
                InitializeWorldSystems();
                startupStatus = "Подготовка чанков точки появления...";
                startupProgress = 0.08f;
            }
            catch (Exception exception)
            {
                ReportStartupFailure("Ошибка инициализации мира", exception);
            }
        }

        private void InitializeWorldSystems()
        {

            string gameplayContext =
                GameplayContextKey.Get();

            BlockVisualStateRuntime.SetContext(
                gameplayContext
            );

            InteractiveBlockRuntime.SetContext(
                gameplayContext
            );

            BlockVisualStateRuntime.StateChanged +=
                HandleBlockVisualStateChanged;


            if (!DimensionTravelRuntime.Current.Is3D && GetComponent<Game.Entities.Spawn.EntitySpawnManager>() == null)
            {
                gameObject.AddComponent<Game.Entities.Spawn.EntitySpawnManager>();
            }


            // =====================================================
            // CONTENT
            // =====================================================

            ContentManager.Initialize();


            settings = new WorldSettings();

            settings.Seed = DimensionTravelRuntime.Current.Seed;
            if (DimensionTravelRuntime.Current.Is3D)
            {
                world = new World(); world.SetWorldHeight(settings.WorldHeight);
                lighting = world.GetLightEngine(); worldCollision = new WorldCollision(world);
                voxelRuntime = GetComponent<Game.World.Voxel3D.VoxelWorld3DRuntime>();
                if (voxelRuntime == null) voxelRuntime = gameObject.AddComponent<Game.World.Voxel3D.VoxelWorld3DRuntime>();
                voxelRuntime.Initialize(player); worldGenerated = true;
                return;
            }

            
            // =====================================================
            // WORLD
            // =====================================================

            world = new World();

            world.SetWorldHeight(
                settings.WorldHeight
            );


            // ���������� ������������ lighting engine,
            // ������� ����������� World.

            lighting =
                world.GetLightEngine();


            // =====================================================
            // COLLISION
            // =====================================================

            worldCollision =
                new WorldCollision(
                    world
                );


            chunkCollision =
                new ChunkCollision(
                    worldCollision
                );


            // =====================================================
            // GENERATOR
            // =====================================================

            generator =
                new WorldGenerator(
                    settings
                );


            generator.ReloadOres();
            lighting.SetUnloadedSkyOpacityResolver(SampleUnloadedSkyOpacity);


            // =====================================================
            // RENDERER
            // =====================================================

            renderer =
                new ChunkRenderer(
                    world
                );


            // =====================================================
            // CHUNK LOADER
            // =====================================================

            loader =
                new ChunkLoader(
                    world,
                    generator,
                    settings,
                    renderer,
                    chunkCollision
                );


            // Place the inactive (movement-gated) character by the deterministic
            // terrain surface before any Update/FixedUpdate or camera-follow runs.
            PrepareSurfaceSpawn();
            worldGenerated = true;


            Debug.Log(
                "WORLD MANAGER: SYSTEMS INITIALIZED."
            );
        }


        // =====================================================
        // START
        // =====================================================

        private IEnumerator Start()
        {
            // A failed Awake/coroutine must never leave the loading UI spinning.
            float loaderWaitStarted = Time.realtimeSinceStartup;
            while (loader == null)
            {
                if (voxelRuntime != null)
                {
                    if (!voxelRuntime.Ready)
                    {
                        if(Time.realtimeSinceStartup-loaderWaitStarted>30){ReportStartupFailure("Не удалось инициализировать игрока 3D-мира. Проверь ошибки выше в Console.");yield break;}
                        yield return null;continue;
                    }
                    initialSpawnReady = initialLightingReady = worldGenerated = true;
                    initialSpawnFeet = voxelRuntime.Motor.transform.position;
                    startupProgress = 1f; startupStatus = "3D-мир загружен";
                    Game.World.Parallax.DimensionSpawnCurtain.NotifyPlayerSpawned();
                    Game.World.Parallax.DimensionSpawnCurtain.NotifyParallaxReady();
                    yield break;
                }
                if (!string.IsNullOrEmpty(startupError))
                    yield break;
                if (MobileGameDataBootstrap.HasError)
                {
                    ReportStartupFailure("GameData: " + MobileGameDataBootstrap.ErrorMessage);
                    yield break;
                }
                if (Time.realtimeSinceStartup - loaderWaitStarted > 120f)
                {
                    ReportStartupFailure("WorldManager не создал ChunkLoader за 120 секунд. " +
                        "Проверь ошибки инициализации в Android Logcat.");
                    yield break;
                }
                yield return null;
            }

            int spawnChunkX = Mathf.FloorToInt(plannedSpawnX / (float)Chunk.SizeX);
            int spawnChunkY = Mathf.FloorToInt(plannedSurfaceY / (float)Chunk.SizeY);
            loader.SetPlayerChunk(spawnChunkX, spawnChunkY);

            // A 3x3 square delayed spawn while below-ground chunks loaded.
            // For the first playable frame we need terrain under/around spawn
            // and the space directly above it. Other chunks continue streaming.
            int warmRadius = Mathf.Clamp(settings.ViewDistance, 0, 1);
            int requiredCount = (warmRadius * 2 + 1) * (warmRadius == 0 ? 1 : 2);
            int previousLoaded = -1;
            float lastProgressTime = Time.realtimeSinceStartup;
            float startupBegin = lastProgressTime;

            while (true)
            {
                if (!string.IsNullOrEmpty(startupError)) yield break;
                try
                {
                    loader.Process();
                }
                catch (Exception exception)
                {
                    ReportStartupFailure("Ошибка генерации или построения чанков", exception);
                }
                if (!string.IsNullOrEmpty(startupError)) yield break;

                int loaded = 0;
                for (int cx = spawnChunkX - warmRadius; cx <= spawnChunkX + warmRadius; cx++)
                {
                    int minCy = spawnChunkY;
                    int maxCy = warmRadius == 0 ? spawnChunkY : spawnChunkY + 1;
                    for (int cy = minCy; cy <= maxCy; cy++)
                    {
                        if (loader.IsChunkLoaded(cx, cy) &&
                            loader.IsChunkCollisionReady(cx, cy))
                            loaded++;
                    }
                }
                if (loaded != previousLoaded)
                {
                    previousLoaded = loaded;
                    lastProgressTime = Time.realtimeSinceStartup;
                    startupStatus = "Подготовка поверхности и коллизий: " +
                        loaded + "/" + requiredCount + " чанков";
                    startupProgress = 0.1f + 0.7f * loaded / Mathf.Max(1, requiredCount);
                    Debug.Log("TELDER WORLD STARTUP: " + startupStatus);
                }
                if (loaded >= requiredCount) break;

                // A timeout is an explicit error, not permission to spawn into a void.
                float elapsed = Time.realtimeSinceStartup;
                if (elapsed - lastProgressTime > 90f || elapsed - startupBegin > 240f)
                {
                    ReportStartupFailure("Чанки не готовы: " + loaded + "/" + requiredCount +
                        ". В очереди: " + loader.GetLoadQueueSize() +
                        ". Прогресс остановился или генерация слишком долгая.");
                    yield break;
                }
                yield return null;
            }

            startupStatus = "Расчёт освещения...";
            startupProgress = 0.85f;
            yield return null;
            try
            {
                GenerateInitialLighting();
                loader.PrepareInitialLighting(spawnChunkX,spawnChunkY,warmRadius);
            }
            catch (Exception exception)
            {
                ReportStartupFailure("Ошибка первоначального освещения", exception);
            }
            if (!string.IsNullOrEmpty(startupError)) yield break;

            float lightWaitStarted=Time.realtimeSinceStartup;
            while(!loader.IsInitialLightingReady(spawnChunkX,spawnChunkY,warmRadius))
            {
                loader.Process();
                if(Time.realtimeSinceStartup-lightWaitStarted>90f)
                {ReportStartupFailure("Освещение начальной области не готово. Проверь Console.");yield break;}
                yield return null;
            }
            initialLightingReady=true;
            startupStatus = "Создание игрока...";
            startupProgress = 0.93f;
            yield return null;

            try
            {
                initialSpawnReady = SpawnPlayer();
                if (!initialSpawnReady)
                    ReportStartupFailure("Не удалось создать игрока. " +
                        "Проверь Transform игрока и PlayerCollision в сцене Game.");
            }
            catch (Exception exception)
            {
                ReportStartupFailure("Ошибка создания игрока", exception);
            }
            if (!string.IsNullOrEmpty(startupError) || !initialSpawnReady) yield break;
            loader.FinishInitialLoad();

            Game.World.Parallax.DimensionSpawnCurtain.NotifyPlayerSpawned();
            startupStatus = "Запуск погоды...";
            try
            {
                WeatherManager weatherManager = GetComponent<WeatherManager>();
                if (weatherManager == null)
                    weatherManager = gameObject.AddComponent<WeatherManager>();
                weatherManager.Initialize(player, generator, worldCollision);
            }
            catch (Exception exception)
            {
                // Spawn succeeded: weather must not lock the loading screen.
                Debug.LogException(exception, this);
            }
            worldGenerated = true;
            startupStatus = "Мир загружен";
            startupProgress = 1f;
            Debug.Log("TELDER WORLD STARTUP: Player spawned, world ready.");
        }


        // =====================================================
        // UPDATE
        // =====================================================

        private readonly Dictionary<int,int> skySurfaceCache=new Dictionary<int,int>();
        private byte SampleUnloadedSkyOpacity(int x,int y)
        {
            if(SaveGameRuntime.TryGetForegroundOverride(x,y,out ushort saved))
            {var def=world.GetBlockDefinition(saved);return saved==0?(byte)0:(byte)Mathf.Clamp(def!=null?def.LightOpacity:15,0,15);}
            if(generator==null)return 15;
            if(!skySurfaceCache.TryGetValue(x,out int surface))
            {surface=generator.GetSurfaceHeight(x);if(skySurfaceCache.Count>8192)skySurfaceCache.Clear();skySurfaceCache[x]=surface;}
            return y<=surface&&generator.HasGeneratedTerrain(x,y)?(byte)15:(byte)0;
        }

        private void Update()
        {
            // Lighting has its own reserved slice and cannot be starved by terrain generation.
            lighting?.ProcessPending(1.25,256);
            if(renderer!=null)
            {
                renderer.ProcessQueuedRenders(1.0);
                renderer.UpdateDirtyLighting(loader!=null&&loader.GetLoadQueueSize()>0?1:3);
                renderer.UpdateAnimatedVisuals();
                renderer.FlushVisualEdits(loader!=null&&loader.GetLoadQueueSize()>0?1:2);
            }
        }


        private void LateUpdate()
        {
            int remaining = 2;
            while (remaining-- > 0 && pendingCollisionQueue.Count > 0)
            {
                UnityEngine.Vector2Int key = pendingCollisionQueue.Dequeue();
                pendingCollisionSet.Remove(key);
                if (world == null || chunkCollision == null) continue;
                Chunk chunk = world.GetChunk(key.x, key.y);
                if (chunk != null) chunkCollision.BuildChunkCollision(chunk);
            }
        }

        private void OnDestroy()
        {
            BlockVisualStateRuntime.StateChanged -=
                HandleBlockVisualStateChanged;

            if (Instance == this)
                Instance = null;
        }


        private void HandleBlockVisualStateChanged(
            int worldX,
            int worldY,
            BlockVisualLayer layer,
            string state)
        {
            if (layer == BlockVisualLayer.Furniture)
            {
                if (Game.World.Furniture.FurnitureLayerManager.Instance != null)
                {
                    Game.World.Furniture.FurnitureLayerManager.Instance
                        .RefreshVisual(worldX, worldY);
                }
                return;
            }

            if (renderer != null)
                renderer.RefreshVisualAt(worldX, worldY, layer);

            if (
                layer == BlockVisualLayer.Foreground &&
                world != null
            )
            {
                ushort blockId =
                    world.GetBlock(
                        worldX,
                        worldY
                    );

                if (
                    blockId != 0
                )
                {
                    BlockDefinition definition =
                        world.GetBlockDefinition(
                            blockId
                        );

                    if (
                        InteractiveBlockRuntime.IsDoorOrHatch(
                            definition
                        )
                    )
                    {
                        RefreshCollisionAt(
                            worldX,
                            worldY
                        );
                    }
                }
            }
        }


        // =====================================================
        // PLAYER SPAWN
        // =====================================================

        // The old spawn searched from WorldHeight down. When only a few chunks
        // were streamed in, an unloaded surface was treated as air, and the
        // first available cave floor could be mistaken for the world surface.
        private void PrepareSurfaceSpawn()
        {
            plannedSpawnX = 0;
            plannedSurfaceY = generator != null ? generator.GetSurfaceHeight(0) : 100;
            if (player == null) return;

            PlayerCollision playerCollision = player.GetComponent<PlayerCollision>();
            float halfHeight = playerCollision != null
                ? playerCollision.GetColliderSize().y * 0.5f : 0.9f;
            Vector3 pos = new Vector3(plannedSpawnX + 0.5f,
                plannedSurfaceY + 1f + halfHeight + 0.05f,
                player.position.z);
            player.position = pos;
            initialSpawnFeet = new Vector2(pos.x, plannedSurfaceY + 1f);
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private bool SpawnPlayer()
        {
            if (player == null || generator == null || worldCollision == null)
            {
                Debug.LogError("WORLD: Cannot spawn player: missing player/world references.");
                return false;
            }

            PlayerCollision collision = player.GetComponent<PlayerCollision>();
            if (collision == null)
            {
                Debug.LogError("WORLD: Cannot spawn player: PlayerCollision is missing.");
                return false;
            }

            Vector2 size = collision.GetColliderSize();
            Vector3 spawnPosition;
            int groundX;
            int groundY;
            if (!TryFindLoadedSurfaceSpawn(size, out spawnPosition, out groundX, out groundY))
            {
                // A bad biome/structure should not put the player underground.
                // This fallback is above the generator's predicted surface,
                // never a deep solid block found in an incomplete world.
                groundX = plannedSpawnX;
                groundY = plannedSurfaceY;
                spawnPosition = new Vector3(groundX + 0.5f,
                    groundY + 4f + size.y * 0.5f, player.position.z);
                Debug.LogWarning("WORLD: No safe loaded surface found near X=0. " +
                    "Using elevated surface fallback (inspect biome terrain blocks).");
            }

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            player.position = spawnPosition;
            initialSpawnFeet = new Vector2(spawnPosition.x, spawnPosition.y - size.y * 0.5f);
            PlayerController motor = player.GetComponent<PlayerController>();
            if (motor != null) motor.ResetForSurfaceSpawn();
            collision.RefreshAfterWorldChange();

            // Keep the streaming and physics window centered on the actual
            // spawn if a neighboring column was selected for safety.
            loader.SetPlayerChunk(
                Mathf.FloorToInt(spawnPosition.x / Chunk.SizeX),
                Mathf.FloorToInt(groundY / (float)Chunk.SizeY));

            Debug.Log("WORLD: Safe surface spawn at X=" + groundX +
                ", ground Y=" + groundY + ", player center Y=" +
                spawnPosition.y.ToString("F2"));
            return true;
        }

        private bool TryFindLoadedSurfaceSpawn(Vector2 playerSize,
            out Vector3 position, out int groundX, out int groundY)
        {
            position = Vector3.zero;
            groundX = plannedSpawnX;
            groundY = plannedSurfaceY;
            int maxPlayerCells = Mathf.CeilToInt(playerSize.y + 0.12f) + 1;
            float halfWidth = Mathf.Max(0.1f, playerSize.x * 0.5f);

            // Search sideways before considering a cave floor. The generator's
            // height is deterministic and independent of chunk loading.
            for (int radius = 0; radius <= 24; radius++)
            {
                for (int side = 0; side < (radius == 0 ? 1 : 2); side++)
                {
                    int x = plannedSpawnX + (side == 0 ? -radius : radius);
                    int expected = generator.GetSurfaceHeight(x);
                    // Small +/- tolerance for edits, structures and terrain variations;
                    // NEVER scan down to the bottom of the world.
                    for (int step = 0; step <= 7; step++)
                    {
                        int offset = step == 0 ? 0 : (step % 2 != 0 ? (step + 1) / 2 : -step / 2);
                        int y = expected + offset;
                        if (y < 0 || y >= settings.WorldHeight - maxPlayerCells - 1)
                            continue;
                        int cMinX = Mathf.FloorToInt(x + 0.5f - halfWidth + 0.02f);
                        int cMaxX = Mathf.FloorToInt(x + 0.5f + halfWidth - 0.02f);
                        bool valid = true;
                        for (int tx = cMinX; tx <= cMaxX && valid; tx++)
                        {
                            if (!IsWorldChunkLoadedAt(tx, y) ||
                                !worldCollision.IsSolid(tx, y)) { valid = false; break; }
                            for (int ty = y + 1; ty <= y + maxPlayerCells; ty++)
                            {
                                if (!IsWorldChunkLoadedAt(tx, ty) ||
                                    worldCollision.IsSolid(tx, ty))
                                {
                                    valid = false;
                                    break;
                                }
                            }
                        }
                        if (!valid) continue;
                        groundX = x;
                        groundY = y;
                        position = new Vector3(x + 0.5f,
                            y + 1f + playerSize.y * 0.5f + 0.05f,
                            player.position.z);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsWorldChunkLoadedAt(int x, int y)
        {
            return loader != null && loader.IsChunkLoaded(
                Mathf.FloorToInt(x / (float)Chunk.SizeX),
                Mathf.FloorToInt(y / (float)Chunk.SizeY));
        }

        // =====================================================
        // FIND PLAYER SPAWN POSITION
        // =====================================================

        private bool FindPlayerSpawnPosition(
            Vector2 playerSize,
            out Vector3 spawnPosition
        )
        {
            spawnPosition =
                Vector3.zero;


            int searchRadius = 128;


            for (
                int offset = 0;
                offset <= searchRadius;
                offset++
            )
            {
                // =================================================
                // CENTER
                // =================================================

                if (offset == 0)
                {
                    if (
                        TryFindSpawnAtX(
                            0,
                            playerSize,
                            out spawnPosition
                        )
                    )
                    {
                        return true;
                    }
                }
                else
                {
                    // =================================================
                    // LEFT
                    // =================================================

                    if (
                        TryFindSpawnAtX(
                            -offset,
                            playerSize,
                            out spawnPosition
                        )
                    )
                    {
                        return true;
                    }


                    // =================================================
                    // RIGHT
                    // =================================================

                    if (
                        TryFindSpawnAtX(
                            offset,
                            playerSize,
                            out spawnPosition
                        )
                    )
                    {
                        return true;
                    }
                }
            }


            return false;
        }


        // =====================================================
        // FIND SPAWN AT X
        // =====================================================

        private bool TryFindSpawnAtX(
            int worldX,
            Vector2 playerSize,
            out Vector3 spawnPosition
        )
        {
            spawnPosition =
                Vector3.zero;


            float halfWidth =
                playerSize.x * 0.5f;


            float halfHeight =
                playerSize.y * 0.5f;


            float skin = 0.02f;


            // =================================================
            // CHECK COLUMN
            // =================================================

            for (
                int groundY = settings.WorldHeight - 1;
                groundY >= 0;
                groundY--
            )
            {
                // =================================================
                // SOLID GROUND
                // =================================================

                if (
                    !worldCollision.IsSolid(
                        worldX,
                        groundY
                    )
                )
                {
                    continue;
                }


                // =================================================
                // PLAYER WIDTH
                // =================================================

                int leftX =
                    Mathf.FloorToInt(
                        worldX -
                        halfWidth +
                        skin
                    );


                int rightX =
                    Mathf.FloorToInt(
                        worldX +
                        halfWidth -
                        skin
                    );


                bool blocked = false;


                // =================================================
                // CHECK PLAYER HEIGHT
                // =================================================

                for (
                    int x = leftX;
                    x <= rightX;
                    x++
                )
                {
                    if (
                        worldCollision.IsSolid(
                            x,
                            groundY + 1
                        )
                    )
                    {
                        blocked = true;
                        break;
                    }


                    if (
                        worldCollision.IsSolid(
                            x,
                            groundY + 2
                        )
                    )
                    {
                        blocked = true;
                        break;
                    }


                    if (
                        worldCollision.IsSolid(
                            x,
                            groundY + 3
                        )
                    )
                    {
                        blocked = true;
                        break;
                    }
                }


                if (blocked)
                {
                    continue;
                }


                // =================================================
                // SPAWN POSITION
                // =================================================

                float playerCenterY =
                    groundY +
                    1f +
                    halfHeight;


                float playerCenterX =
                    worldX +
                    0.5f;


                spawnPosition =
                    new Vector3(
                        playerCenterX,
                        playerCenterY + skin,
                        0f
                    );


                return true;
            }


            return false;
        }


        // =====================================================
        // REFRESH COLLISION AT CELL
        // =====================================================

        public void RefreshCollisionAt(
            int worldX,
            int worldY
        )
        {
            if (
                world == null ||
                chunkCollision == null
            )
            {
                return;
            }

            int chunkX =
                FloorDiv(
                    worldX,
                    Chunk.SizeX
                );

            int chunkY =
                FloorDiv(
                    worldY,
                    Chunk.SizeY
                );

            Chunk chunk =
                world.GetChunk(
                    chunkX,
                    chunkY
                );

            if (chunk != null)
            {
                chunkCollision.BuildChunkCollision(
                    chunk
                );
            }
        }


        // =====================================================
        // GETTERS
        // =====================================================

        public ChunkLoader GetLoader()
        {
            return loader;
        }


        public World GetWorld()
        {
            return world;
        }


        public LightPropagationEngine GetLighting()
        {
            return lighting;
        }


        public WorldSettings GetSettings()
        {
            return settings;
        }


        public WorldGenerator GetGenerator()
        {
            return generator;
        }


        public WorldCollision GetWorldCollision()
        {
            return worldCollision;
        }


        public ChunkCollision GetChunkCollision()
        {
            return chunkCollision;
        }


        public ChunkRenderer GetChunkRenderer()
        {
            return renderer;
        }


        // =====================================================
        // FIND SURFACE
        // =====================================================

        private bool TryFindSurface(
            int worldX,
            out int surfaceY
        )
        {
            surfaceY = 0;


            World currentWorld =
                world;


            if (currentWorld == null)
            {
                return false;
            }


            for (
                int y = settings.WorldHeight - 1;
                y >= 0;
                y--
            )
            {
                ushort blockID =
                    currentWorld.GetBlock(
                        worldX,
                        y
                    );


                if (blockID == 0)
                {
                    continue;
                }


                ContentID contentID =
                    BlockIDRegistry.GetContentID(
                        blockID
                    );


                if (
                    !BlockRegistry.Contains(
                        contentID
                    )
                )
                {
                    continue;
                }


                BlockDefinition block =
                    BlockRegistry.Get(
                        contentID
                    );


                if (
                    block == null ||
                    !block.Solid
                )
                {
                    continue;
                }


                surfaceY = y;


                return true;
            }


            return false;
        }


        // =====================================================
        // DEFAULT RESPAWN POINT
        // =====================================================

        public bool TryGetDefaultSpawnFeetPosition(
            out Vector2 feetPosition
        )
        {
            feetPosition = Vector2.zero;

            if (!initialSpawnReady) return false;
            feetPosition = initialSpawnFeet;
            return true;
        }


        // =====================================================
        // SET FOREGROUND BLOCK
        // =====================================================

        public bool SetBlock(
            int worldX,
            int worldY,
            ushort blockID
        )
        {
            return SetBlockInternal(
                worldX,
                worldY,
                blockID,
                true
            );
        }


        // Tree cascade path: keep the visual/save hooks but defer the expensive
        // full chunk collision rebuild. StructureCascadeBreakRunner batches that
        // work and rebuilds each touched chunk at most once per short interval.
        public bool SetBlockForCascade(
            int worldX,
            int worldY,
            ushort blockID
        )
        {
            return SetBlockInternal(
                worldX,
                worldY,
                blockID,
                false
            );
        }


        private bool SetBlockInternal(
            int worldX,
            int worldY,
            ushort blockID,
            bool rebuildCollision
        )
        {
            if (world == null)
            {
                return false;
            }


            // =====================================================
            // FIND CHUNK
            // =====================================================

            int chunkX =
                FloorDiv(
                    worldX,
                    Chunk.SizeX
                );


            int chunkY =
                FloorDiv(
                    worldY,
                    Chunk.SizeY
                );


            Chunk chunk =
                world.GetChunk(
                    chunkX,
                    chunkY
                );


            if (chunk == null)
            {
                return false;
            }


            // =====================================================
            // LOCAL COORDINATES
            // =====================================================

            int localX =
                worldX -
                chunkX * Chunk.SizeX;


            int localY =
                worldY -
                chunkY * Chunk.SizeY;


            // =====================================================
            // GET OLD BLOCK
            // =====================================================

            ushort oldBlockID =
                chunk.GetBlock(
                    localX,
                    localY
                );


            if (oldBlockID == blockID)
            {
                return false;
            }


            // "alwaysFurniture" items cannot be put on the foreground map,
            // including through scripts and alternate placement modes.
            if (blockID != 0 && Game.World.Furniture.FurnitureSupportRuntime.IsFurnitureOnlyBlock(blockID))
                return false;

            // =====================================================
            // WORLD CHANGE
            // =====================================================

            if (
                !world.SetBlock(
                    worldX,
                    worldY,
                    blockID
                )
            )
            {
                return false;
            }

            // Any newly placed/replaced foreground block starts with its default
            // interactive state. Save loading writes directly into chunks, so this
            // does not erase persisted open doors during world restoration.
            InteractiveBlockRuntime.ClearStateAt(
                worldX,
                worldY,
                BlockVisualLayer.Foreground,
                true
            );

            BlockVisualStateRuntime.ClearState(
                worldX,
                worldY,
                BlockVisualLayer.Foreground,
                false
            );

            // Central removal hook: explosions, falling-block replacements,
            // structure edits and normal mining all pass through SetBlock().
            if (
                oldBlockID != 0 &&
                oldBlockID != blockID &&
                FurnaceRuntime.Instance != null
            )
            {
                FurnaceRuntime.Instance.NotifyForegroundBlockRemoved(
                    worldX,
                    worldY,
                    oldBlockID
                );
            }

            // =============================================
            // Cauldron contents follow every foreground replacement path.
            if (oldBlockID != 0 && oldBlockID != blockID &&
                Game.Gameplay.Cooking.CauldronRuntime.IsCauldron(world.GetBlockDefinition(oldBlockID)?.ID))
                Game.Gameplay.Cooking.CauldronRuntime.Instance?.NotifyRemoved(worldX,worldY);

            // BREAK DEBRIS
            // =============================================

            if (
                oldBlockID != 0 &&
                blockID == 0
            )
            {
                BlockBreakDebrisSystem.Emit(
                    world,
                    worldX,
                    worldY,
                    oldBlockID,
                    false
                );
            }
            // =====================================================
            // IMMEDIATE VISUAL UPDATE
            // =====================================================

            // [BT-AUTO-WORLD]
            Game.BlockTransforms.BlockTransformRegistry.Clear(worldX, worldY);

            if (renderer != null)
            {
                renderer.UpdateBlock(
                    chunk,
                    localX,
                    localY
                );
            }


            // =====================================================
            // COLLISION
            // =====================================================

            if (rebuildCollision && chunkCollision != null)
            {
                var key = new UnityEngine.Vector2Int(chunkX, chunkY);
                if (pendingCollisionSet.Add(key)) pendingCollisionQueue.Enqueue(key);
            }

            SaveGameRuntime.RecordForegroundChange(worldX,worldY,blockID);
            if (oldBlockID != 0 && blockID == 0)
            {
                try { Game.Achievements.AchievementRuntime.NotifyBlockBreak(Game.Content.BlockIDRegistry.GetContentID(oldBlockID).ToString()); } catch { }
            }
            else if (oldBlockID == 0 && blockID != 0)
            {
                try { Game.Achievements.AchievementRuntime.NotifyBlockPlace(Game.Content.BlockIDRegistry.GetContentID(blockID).ToString()); } catch { }
                Game.World.Fluids.LiquidRuntime.RemoveLiquidAt(worldX, worldY);
            }
            Game.World.Fluids.LiquidRuntime.NotifyCellChanged(worldX, worldY);
            // Only loaded neighbour cells are inspected; no forced chunk loading.
            if (oldBlockID != 0 && blockID == 0)
                Game.World.Furniture.FurnitureSupportRuntime.SupportChanged(worldX, worldY);
            // [TELDER-V34-FALLING-NOTIFY]
            Game.World.Physics.FallingBlockSystem.NotifyCellChanged(
                worldX,
                worldY
            );

            // Reliable multiplayer replication. Remote applications suppress echoes.
            TelderNetworkRuntime.NotifyBlockChanged(worldX, worldY, blockID);

            return true;
        }


        // =====================================================
        // SET BACKGROUND BLOCK
        // =====================================================

        public bool SetBackground(
    int worldX,
    int worldY,
    ushort blockID
)
        {
            if (world == null)
            {
                return false;
            }
            if (blockID != 0 && Game.World.Furniture.FurnitureSupportRuntime.IsFurnitureOnlyBlock(blockID))
                return false;


            // =====================================================
            // FIND CHUNK
            // =====================================================

            int chunkX =
                FloorDiv(
                    worldX,
                    Chunk.SizeX
                );


            int chunkY =
                FloorDiv(
                    worldY,
                    Chunk.SizeY
                );


            Chunk chunk =
                world.GetChunk(
                    chunkX,
                    chunkY
                );


            // ���� chunk �� ��������,
            // �� ����� ����� ����������� �������� world data.
            if (chunk == null)
            {
                bool changedWithoutChunk =
                    world.SetBackground(
                        worldX,
                        worldY,
                        blockID
                    );

                if (changedWithoutChunk)
                {
                    BlockVisualStateRuntime.ClearState(
                        worldX,
                        worldY,
                        BlockVisualLayer.Background,
                        false
                    );
                }

                return changedWithoutChunk;
            }


            // =====================================================
            // LOCAL COORDINATES
            // =====================================================

            int localX =
                worldX -
                chunkX *
                Chunk.SizeX;


            int localY =
                worldY -
                chunkY *
                Chunk.SizeY;


            // =====================================================
            // SAVE OLD BLOCK
            // =====================================================

            ushort oldBlockID =
                chunk.GetBackground(
                    localX,
                    localY
                );


            // =====================================================
            // NO CHANGE
            // =====================================================

            if (
                oldBlockID ==
                blockID
            )
            {
                return false;
            }


            // =====================================================
            // CHANGE WORLD DATA
            // =====================================================

            bool changed =
                world.SetBackground(
                    worldX,
                    worldY,
                    blockID
                );


            if (!changed)
            {
                return false;
            }

            BlockVisualStateRuntime.ClearState(
                worldX,
                worldY,
                BlockVisualLayer.Background,
                false
            );

            // [STRUCTURE-LAYERS-CLEAR-BACKGROUND-TRANSFORM-V1]
            Game.BlockTransforms.BackgroundBlockTransformRegistry.Clear(worldX, worldY);


            // =====================================================
            // BREAK DEBRIS
            // =====================================================

            if (
                oldBlockID != 0 &&
                blockID == 0
            )
            {
                BlockBreakDebrisSystem.Emit(
                    world,
                    worldX,
                    worldY,
                    oldBlockID,
                    true
                );
            }


            // =====================================================
            // UPDATE RENDER
            // =====================================================

            if (renderer != null)
            {
                renderer.UpdateBackgroundBlock(
                    chunk,
                    localX,
                    localY
                );
            }

            SaveGameRuntime.RecordBackgroundChange(worldX,worldY,blockID);

            return true;
        }


        // =====================================================
        // CHUNK COLLISION
        // =====================================================

        public void RebuildCollisionAtWorldCell(
            int worldX,
            int worldY
        )
        {
            RebuildChunkCollision(
                FloorDiv(
                    worldX,
                    Chunk.SizeX
                ),
                FloorDiv(
                    worldY,
                    Chunk.SizeY
                )
            );
        }


        private void RebuildChunkCollision(
            int chunkX,
            int chunkY
        )
        {
            if (
                world == null ||
                chunkCollision == null
            )
            {
                return;
            }


            Chunk chunk =
                world.GetChunk(
                    chunkX,
                    chunkY
                );


            if (chunk == null)
            {
                return;
            }


            chunkCollision.BuildChunkCollision(
                chunk
            );
        }


        // =====================================================
        // CHUNK BORDER UPDATE
        // =====================================================

        private void UpdateChunkBorder(
            int chunkX,
            int chunkY
        )
        {
            if (world == null)
            {
                return;
            }


            Chunk neighbourChunk =
                world.GetChunk(
                    chunkX,
                    chunkY
                );


            if (neighbourChunk == null)
            {
                return;
            }


            // =====================================================
            // RENDER
            // =====================================================

            if (renderer != null)
            {
                renderer.Render(
                    neighbourChunk
                );
            }


            // =====================================================
            // COLLISION
            // =====================================================

            if (chunkCollision != null)
            {
                chunkCollision.BuildChunkCollision(
                    neighbourChunk
                );
            }
        }


        // =====================================================
        // INITIAL LIGHTING
        // =====================================================

        private void GenerateInitialLighting()
        {
            if (
                lighting == null ||
                world == null
            )
            {
                return;
            }


            // Loading/ChunkLoader already queues viewport-local relighting.
            // Rebuilding *all* loaded chunks here was a long synchronous
            // stall at the end of world initialization. Keep the legacy path
            // only when there is no streaming chunk loader.
            if (loader != null)
            {
                Debug.Log("WORLD LIGHTING: Initial light queued incrementally by ChunkLoader.");
                return;
            }
            lighting.RebuildLoadedWorld(
                settings.WorldHeight
            );


            Debug.Log(
                "WORLD LIGHTING: Initial lighting generated."
            );


            if (renderer != null)
            {
                renderer.RenderAllLoaded(
                    world
                );
            }
        }


        // =====================================================
        // FLOOR DIVISION
        // =====================================================
        //
        // ����� ������ ����� �������, ������ ��� �������
        // ������������� ������� C# ����������� ��������
        // � �������������� ������������.
        //
        // ��������:
        //
        // -1 / 32 = 0   <- ����������� ��� chunk
        //
        // ��� �����:
        //
        // -1 -> chunk -1, local 31
        //

        private int FloorDiv(
            int value,
            int divisor
        )
        {
            int result =
                value / divisor;


            int remainder =
                value % divisor;


            if (
                remainder != 0 &&
                value < 0
            )
            {
                result--;
            }


            return result;
        }
    }
}

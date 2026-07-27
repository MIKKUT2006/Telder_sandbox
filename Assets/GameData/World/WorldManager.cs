using Game.Blocks;
using Game.Content;
using Game.World.Collision;
using Game.World.Generation;
using Game.World.Loading;
using Game.World.Rendering;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Game.World
{

    public class WorldManager :
        MonoBehaviour
    {

        public static WorldManager Instance;


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

        private WorldGenerator generator;

        private ChunkLoader loader;

        private ChunkRenderer renderer;


        // =====================================================
        // STATE
        // =====================================================

        private bool worldGenerated;


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

            Instance =
                this;


            ContentManager.Initialize();


            settings =
                new WorldSettings();


            world =
                new World();


            worldCollision =
                new WorldCollision(
                    world
                );


            chunkCollision =
                new ChunkCollision(
                    worldCollision
                );


            generator =
                new WorldGenerator(
                    settings
                );


            generator.ReloadOres();


            renderer =
                new ChunkRenderer();


            loader =
                new ChunkLoader(
                    world,
                    generator,
                    settings,
                    renderer,
                    chunkCollision
                );


            worldGenerated =
                true;


            Debug.Log(
                "WORLD MANAGER: SYSTEMS INITIALIZED."
            );

        }


        // =====================================================
        // START
        // =====================================================

        private IEnumerator Start()
        {
            // =====================================================
            // WAIT FOR INITIAL CHUNKS
            // =====================================================

            while (
                loader == null
            )
            {
                yield return null;
            }


            // =====================================================
            // START INITIAL CHUNK LOADING
            // =====================================================

            loader.SetPlayerChunk(
                0,
                0
            );


            // =====================================================
            // WAIT UNTIL START AREA IS LOADED
            // =====================================================

            while (
                !loader.IsChunkLoaded(
                    0,
                    0
                )
            )
            {
                loader.Process();

                yield return null;
            }


            // =====================================================
            // WAIT UNTIL INITIAL AREA IS LOADED
            // =====================================================

            while (
                loader.GetLoadQueueSize() > 0
            )
            {
                loader.Process();

                yield return null;
            }


            // =====================================================
            // WORLD READY
            // =====================================================

            worldGenerated =
                true;


            Debug.Log(
                "WORLD: Initial chunks generated."
            );


            // =====================================================
            // SPAWN PLAYER
            // =====================================================

            SpawnPlayer();

        }

        // =====================================================
        // PLAYER SPAWN
        // =====================================================

        private void SpawnPlayer()
        {

            if (
                player == null
            )
            {

                Debug.LogError(
                    "WORLD: Player reference is null."
                );

                return;

            }


            PlayerCollision playerCollision =
                player.GetComponent<PlayerCollision>();


            if (
                playerCollision == null
            )
            {

                Debug.LogError(
                    "WORLD: PlayerCollision component not found."
                );

                return;

            }


            Vector2 playerSize =
                playerCollision.GetColliderSize();


            int spawnX =
                0;


            int surfaceY;


            if (
                !TryFindSurface(
                    spawnX,
                    out surfaceY
                )
            )
            {

                Debug.LogError(
                    "WORLD: Failed to find surface at X = " +
                    spawnX
                );

                return;

            }


            // =====================================================
            // POSITION PLAYER ABOVE SURFACE
            // =====================================================

            float playerHalfHeight =
                playerSize.y *
                0.5f;


            float playerCenterY =
                surfaceY +
                1f +
                playerHalfHeight +
                0.05f;


            Vector3 spawnPosition =
                new Vector3(
                    spawnX + 0.5f,
                    playerCenterY,
                    0f
                );


            // =====================================================
            // RESET PHYSICS
            // =====================================================

            Rigidbody2D rb =
                player.GetComponent<Rigidbody2D>();


            if (
                rb != null
            )
            {

                rb.linearVelocity =
                    Vector2.zero;

                rb.angularVelocity =
                    0f;

            }


            // =====================================================
            // SET POSITION
            // =====================================================

            player.position =
                spawnPosition;


            Debug.Log(
                "WORLD: PLAYER SPAWNED ABOVE SURFACE. " +
                "X = " +
                spawnX +
                " SURFACE Y = " +
                surfaceY +
                " PLAYER Y = " +
                playerCenterY
            );

        }


        // =====================================================
        // FIND PLAYER SPAWN
        // =====================================================

        private bool FindPlayerSpawnPosition(
            Vector2 playerSize,
            out Vector3 spawnPosition
        )
        {
            spawnPosition =
                Vector3.zero;


            // Ищем сначала в центре мира.
            // Затем постепенно расширяем область поиска.

            int searchRadius =
                128;


            for (
                int offset = 0;
                offset <= searchRadius;
                offset++
            )
            {

                // =============================================
                // CENTER
                // =============================================

                if (
                    offset == 0
                )
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

                    // =============================================
                    // LEFT
                    // =============================================

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


                    // =============================================
                    // RIGHT
                    // =============================================

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
                playerSize.x *
                0.5f;


            float halfHeight =
                playerSize.y *
                0.5f;


            // Небольшой запас между игроком и землёй.

            float skin =
                0.02f;


            // =================================================
            // ПРОВЕРЯЕМ ВЕСЬ СТОЛБЕЦ
            // =================================================

            for (
                int groundY = settings.WorldHeight - 1;
                groundY >= 0;
                groundY--
            )
            {

                // =================================================
                // 1. ИЩЕМ ТВЁРДЫЙ БЛОК ПОД НОГАМИ
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
                // 2. ОПРЕДЕЛЯЕМ ШИРИНУ ИГРОКА
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


                bool blocked =
                    false;


                // =================================================
                // 3. ПРОВЕРЯЕМ ВСЮ ШИРИНУ ИГРОКА
                // =================================================

                for (
                    int x = leftX;
                    x <= rightX;
                    x++
                )
                {

                    // Первый блок над землёй

                    if (
                        worldCollision.IsSolid(
                            x,
                            groundY + 1
                        )
                    )
                    {

                        blocked =
                            true;

                        break;

                    }


                    // Второй блок над землёй

                    if (
                        worldCollision.IsSolid(
                            x,
                            groundY + 2
                        )
                    )
                    {

                        blocked =
                            true;

                        break;

                    }


                    // Третий блок над землёй.
                    // Это важно, если игрок высокий.

                    if (
                        worldCollision.IsSolid(
                            x,
                            groundY + 3
                        )
                    )
                    {

                        blocked =
                            true;

                        break;

                    }

                }


                if (
                    blocked
                )
                {
                    continue;
                }


                // =================================================
                // 4. СТАВИМ ИГРОКА НА ПОВЕРХНОСТЬ
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

        public ChunkLoader GetLoader()
        {

            return loader;

        }


        public World GetWorld()
        {

            return world;

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
        private bool TryFindSurface(int worldX,out int surfaceY)
        {
            surfaceY = 0;


            World currentWorld =
                world;


            if (
                currentWorld == null
            )
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


                if (
                    blockID == 0
                )
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


                surfaceY =
                    y;


                return true;

            }


            return false;

        }
        public bool SetBlock(
    int worldX,
    int worldY,
    ushort blockID
)
        {

            if (
                world == null
            )
            {
                return false;
            }


            // =====================================================
            // WORLD -> CHUNK
            // =====================================================

            int chunkX =
                Mathf.FloorToInt(
                    (float)worldX /
                    Chunk.SizeX
                );


            int chunkY =
                Mathf.FloorToInt(
                    (float)worldY /
                    Chunk.SizeY
                );


            Chunk chunk =
                world.GetChunk(
                    chunkX,
                    chunkY
                );


            if (
                chunk == null
            )
            {
                return false;
            }


            // =====================================================
            // LOCAL
            // =====================================================

            int localX =
                worldX -
                chunkX *
                Chunk.SizeX;


            int localY =
                worldY -
                chunkY *
                Chunk.SizeY;


            if (
                localX < 0
            )
            {
                localX +=
                    Chunk.SizeX;
            }


            if (
                localY < 0
            )
            {
                localY +=
                    Chunk.SizeY;
            }


            // =====================================================
            // OLD
            // =====================================================

            ushort oldBlockID =
                chunk.GetBlock(
                    localX,
                    localY
                );


            if (
                oldBlockID ==
                blockID
            )
            {
                return false;
            }


            // =====================================================
            // SET
            // =====================================================

            chunk.SetBlock(
                localX,
                localY,
                blockID
            );


            // =====================================================
            // UPDATE RENDER
            // =====================================================

            if (
                renderer != null
            )
            {

                renderer.UpdateBlock(
                    chunk,
                    localX,
                    localY
                );

            }


            // =====================================================
            // UPDATE COLLISION
            // =====================================================

            if (
                chunkCollision != null
            )
            {

                chunkCollision.BuildChunkCollision(
                    chunk
                );

            }


            // =====================================================
            // IMPORTANT:
            // UPDATE NEIGHBOUR CHUNKS
            //
            // Если блок находится на границе чанка,
            // соседняя коллизия может зависеть от него.
            // =====================================================

            if (
                localX == 0
            )
            {

                UpdateChunkBorder(
                    chunkX - 1,
                    chunkY
                );

            }


            if (
                localX ==
                Chunk.SizeX - 1
            )
            {

                UpdateChunkBorder(
                    chunkX + 1,
                    chunkY
                );

            }


            if (
                localY == 0
            )
            {

                UpdateChunkBorder(
                    chunkX,
                    chunkY - 1
                );

            }


            if (
                localY ==
                Chunk.SizeY - 1
            )
            {

                UpdateChunkBorder(
                    chunkX,
                    chunkY + 1
                );

            }


            return true;

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


            if (
                chunk == null
            )
            {
                return;
            }


            chunkCollision.BuildChunkCollision(
                chunk
            );
        }

        private void UpdateChunkBorder(
    int chunkX,
    int chunkY
)
        {

            Chunk neighbour =
                world.GetChunk(
                    chunkX,
                    chunkY
                );


            if (
                neighbour == null
            )
            {
                return;
            }


            if (
                renderer != null
            )
            {

                renderer.Render(
                    neighbour
                );

            }


            if (
                chunkCollision != null
            )
            {

                chunkCollision.BuildChunkCollision(
                    neighbour
                );

            }

        }

        public ChunkRenderer GetChunkRenderer()
        {
            return renderer;
        }
    }


}
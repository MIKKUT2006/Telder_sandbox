using Game.Content;
using Game.World.Collision;
using Game.World.Generation;
using Game.World.Loading;
using Game.World.Rendering;
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

        [Header("Player")]

        [SerializeField]
        private Transform player;


        // =====================================================
        // WORLD SYSTEMS
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


        private Game.World.Vector2Int lastPlayerChunk;

        private bool hasLastPlayerChunk;


        // =====================================================
        // READY
        // =====================================================

        public bool IsReady
        {
            get
            {
                return
                    worldGenerated &&
                    world != null &&
                    loader != null &&
                    worldCollision != null;
            }
        }


        // =====================================================
        // AWAKE
        // =====================================================

        private void Awake()
        {

            Instance =
                this;


            // =================================================
            // CONTENT
            // =================================================

            ContentManager.Initialize();


            // =================================================
            // SETTINGS
            // =================================================

            settings =
                new WorldSettings();


            // =================================================
            // WORLD
            // =================================================

            world =
                new World();


            // =================================================
            // WORLD COLLISION
            // =================================================

            worldCollision =
                new WorldCollision(
                    world
                );


            // =================================================
            // CHUNK COLLISION
            // =================================================

            chunkCollision =
                new ChunkCollision(
                    worldCollision
                );


            // =================================================
            // GENERATOR
            // =================================================

            generator =
                new WorldGenerator(
                    settings
                );


            generator.ReloadOres();


            // =================================================
            // RENDERER
            // =================================================

            renderer =
                new ChunkRenderer();


            // =================================================
            // CHUNK LOADER
            // =================================================

            loader =
                new ChunkLoader(
                    world,
                    generator,
                    settings,
                    renderer,
                    chunkCollision
                );


            worldGenerated =
                false;


            hasLastPlayerChunk =
                false;


            Debug.Log(
                "WORLD MANAGER: SYSTEMS INITIALIZED."
            );

        }


        // =====================================================
        // START
        // =====================================================

        private void Start()
        {

            if (
                loader == null
            )
            {

                Debug.LogError(
                    "WORLD MANAGER: ChunkLoader is null."
                );

                return;

            }


            // =================================================
            // œ≈–¬€… ¬€«Œ¬ «¿√–”«◊» ¿
            // =================================================

            UpdatePlayerChunk(
                true
            );


            worldGenerated =
                true;


            Debug.Log(
                "WORLD: Initial chunk loading started."
            );


            // =================================================
            // SPAWN PLAYER
            // =================================================

            SpawnPlayer();

        }


        // =====================================================
        // UPDATE
        // =====================================================

        private void Update()
        {

            if (
                !worldGenerated
            )
            {
                return;
            }


            if (
                loader == null
            )
            {
                return;
            }


            if (
                player == null
            )
            {
                return;
            }


            // =================================================
            // Œ¡ÕŒ¬Àﬂ≈Ã “≈ ”Ÿ»… ◊¿Õ  »√–Œ ¿
            // =================================================

            UpdatePlayerChunk(
                false
            );


            // =================================================
            // Œ¡–¿¡¿“€¬¿≈Ã Œ◊≈–≈ƒ‹ ◊¿Õ Œ¬
            // =================================================

            loader.Process();

        }


        // =====================================================
        // PLAYER CHUNK
        // =====================================================

        private void UpdatePlayerChunk(
            bool forceUpdate
        )
        {

            if (
                player == null
            )
            {
                return;
            }


            int chunkX =
                Mathf.FloorToInt(
                    player.position.x /
                    Chunk.SizeX
                );


            int chunkY =
                Mathf.FloorToInt(
                    player.position.y /
                    Chunk.SizeY
                );


            Game.World.Vector2Int currentChunk =
                new Game.World.Vector2Int(
                    chunkX,
                    chunkY
                );


            if (
                !forceUpdate &&
                hasLastPlayerChunk &&
                currentChunk ==
                lastPlayerChunk
            )
            {
                return;
            }


            lastPlayerChunk =
                currentChunk;


            hasLastPlayerChunk =
                true;


            loader.SetPlayerChunk(
                chunkX,
                chunkY
            );

        }


        // =====================================================
        // SPAWN PLAYER
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


            if (
                worldCollision == null
            )
            {

                Debug.LogError(
                    "WORLD: WorldCollision is null."
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


            Vector3 spawnPosition;


            if (
                !FindPlayerSpawnPosition(
                    playerSize,
                    out spawnPosition
                )
            )
            {

                Debug.LogError(
                    "WORLD: Failed to find player spawn position."
                );

                return;

            }


            // =================================================
            // RESET PHYSICS
            // =================================================

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


            // =================================================
            // SET POSITION
            // =================================================

            player.position =
                spawnPosition;


            // =================================================
            // Œ¡ÕŒ¬Àﬂ≈Ã ◊¿Õ  œŒ—À≈ SPAWN
            // =================================================

            UpdatePlayerChunk(
                true
            );


            Debug.Log(
                "WORLD: PLAYER SPAWNED AT " +
                spawnPosition
            );

        }


        // =====================================================
        // FIND SPAWN
        // =====================================================

        private bool FindPlayerSpawnPosition(
            Vector2 playerSize,
            out Vector3 spawnPosition
        )
        {

            spawnPosition =
                Vector3.zero;


            int searchRadius =
                100;


            for (
                int offset = 0;
                offset <= searchRadius;
                offset++
            )
            {

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


            int maxHeight =
                settings.WorldHeight;


            float halfWidth =
                playerSize.x *
                0.5f;


            float halfHeight =
                playerSize.y *
                0.5f;


            float skin =
                0.02f;


            for (
                int y = maxHeight - 1;
                y >= 0;
                y--
            )
            {

                if (
                    !worldCollision.IsSolid(
                        worldX,
                        y
                    )
                )
                {
                    continue;
                }


                int leftBlock =
                    Mathf.FloorToInt(
                        worldX -
                        halfWidth +
                        skin
                    );


                int rightBlock =
                    Mathf.FloorToInt(
                        worldX +
                        halfWidth -
                        skin
                    );


                bool blocked =
                    false;


                for (
                    int x = leftBlock;
                    x <= rightBlock;
                    x++
                )
                {

                    if (
                        worldCollision.IsSolid(
                            x,
                            y + 1
                        )
                    )
                    {

                        blocked =
                            true;

                        break;

                    }


                    if (
                        worldCollision.IsSolid(
                            x,
                            y + 2
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


                spawnPosition =
                    new Vector3(
                        worldX + 0.5f,
                        y + 1f + halfHeight,
                        0f
                    );


                return true;

            }


            return false;

        }


        // =====================================================
        // PUBLIC CHUNK UPDATE
        // =====================================================

        public void UpdatePlayerChunk(
            int currentChunkX,
            int currentChunkY
        )
        {

            if (
                loader == null
            )
            {
                return;
            }


            loader.SetPlayerChunk(
                currentChunkX,
                currentChunkY
            );

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

    }

}
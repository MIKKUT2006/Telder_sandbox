
using UnityEngine;

using Game.Content;
using Game.World.Generation;
using Game.World.Loading;
using Game.World.Rendering;
using Game.World.Collision;
using Game.World.Modification;


namespace Game.World
{
    public class WorldManager :
        MonoBehaviour
    {
        public static WorldManager Instance;


        private World world;

        private WorldSettings settings;

        private WorldCollision worldCollision;

        private ChunkCollision chunkCollision;

        private WorldGenerator generator;

        private ChunkLoader loader;

        private ChunkRenderer renderer;

        private BlockModificationService blockModification;
        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {
            Instance =
                this;


            // =============================================
            // CONTENT
            // =============================================

            ContentManager.Initialize();


            // =============================================
            // WORLD SETTINGS
            // =============================================

            settings =
                new WorldSettings();


            // =============================================
            // WORLD
            // =============================================

            world =
                new World();


            // =============================================
            // WORLD COLLISION
            // =============================================

            worldCollision =
                new WorldCollision(
                    world
                );

            chunkCollision =
    new ChunkCollision(
        worldCollision
    );

            // =============================================
            // WORLD GENERATOR
            // =============================================

            generator =
                new WorldGenerator(
                    settings
                );


            generator.ReloadOres();


            // =============================================
            // RENDERER
            // =============================================

            renderer =
                new ChunkRenderer();

            


            // =============================================
            // CHUNK LOADER
            // =============================================

            loader = new ChunkLoader(world,generator,settings,renderer,chunkCollision);

            blockModification = new BlockModificationService(world,renderer,chunkCollision);

            loader.Update(0,0);
        }


        private void Start()
        {
            // Первичная генерация мира
            LoadStartChunks();
        }


        // =====================================================
        // INITIAL WORLD
        // =====================================================

        private void LoadStartChunks()
        {
            if (
                loader == null
            )
            {
                Debug.LogError(
                    "WORLD MANAGER: CHUNK LOADER IS NULL."
                );

                return;
            }


            loader.Update(
                0,
                0
            );
        }


        // =====================================================
        // PLAYER CHUNK UPDATE
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
                Debug.LogError(
                    "WORLD MANAGER: CHUNK LOADER IS NULL."
                );

                return;
            }


            loader.Update(
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


        // =====================================================
        // READY
        // =====================================================

        public bool IsReady
        {
            get
            {
                return
                    world != null &&
                    settings != null &&
                    generator != null &&
                    renderer != null &&
                    worldCollision != null &&
                    loader != null;
            }
        }
        public BlockModificationService GetBlockModification()
        {
            return blockModification;
        }
    }
}


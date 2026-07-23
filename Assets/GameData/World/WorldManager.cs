using UnityEngine;

using Game.Content;
using Game.World.Generation;
using Game.World.Loading;
using Game.World.Rendering;


namespace Game.World
{

    public class WorldManager :
        MonoBehaviour
    {

        public static WorldManager Instance;


        private World world;

        private WorldSettings settings;

        private WorldGenerator generator;

        private ChunkLoader loader;

        private ChunkRenderer renderer;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {
            Instance =
                this;
        }


        private void Start()
        {
            ContentManager.Initialize();


            settings =
                new WorldSettings();


            world =
                new World();


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
                    renderer
                );


            // Первичная генерация
            loader.Update(
                0,
                0
            );
        }


        // =====================================================
        // INITIAL WORLD
        // =====================================================

        private void LoadStartChunk()
        {

            if (
                loader == null
            )
            {
                Debug.LogError(
                    "CHUNK LOADER IS NULL"
                );

                return;
            }


            loader.Update(
                0,
                0
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


            loader.Update(
                currentChunkX,
                currentChunkY
            );
        }
        public bool IsReady
        {
            get
            {
                return loader != null;
            }
        }
    }

}
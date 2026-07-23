using Game.Mods;
using Game.ResourcePacks;
using Game.Resources;
using Game.World;
using Game.World.Generation.Ores;
using System.Linq;
using UnityEngine;


namespace Game.Content
{

    public static class ContentManager
    {

        private static bool initialized = false;



        public static bool IsInitialized
        {
            get
            {
                return initialized;
            }
        }



        public static void Initialize()
        {

            // Защита от повторной инициализации

            if (initialized)
            {

                Debug.Log(
                    "CONTENT MANAGER ALREADY INITIALIZED"
                );

                return;

            }



            Debug.Log(
                "CONTENT MANAGER INITIALIZE START"
            );



            DataPaths.CreateFolders();



            ContentLoader.Initialize();

            ModLoader.LoadMods();

            ResourcePackLoader.LoadPacks();

            BlockDatabaseLoader.Load();

            BlockTextureLoader.Load();

            OreFolderLoader.LoadFolder(
                DataPaths.OresFolder
            );

            

            Debug.Log(
    "ORE REGISTRY COUNT AFTER LOAD: " +
    OreRegistry.GetAll().Count()
);

            Debug.Log(
                "ORES LOADED: " +
                OreRegistry.GetAll().Count()
            );

            initialized = true;


            Debug.Log(
                "CONTENT MANAGER INITIALIZE COMPLETE"
            );

        }

    }

}
using System.IO;
using UnityEngine;


namespace Game.Content
{

    public static class DataPaths
    {

        // Android APK is a read-only archive. Runtime JSON and PNG must be
        // unpacked from StreamingAssets into this writable, regular folder.
        public static string GameDataFolder =>
#if UNITY_ANDROID && !UNITY_EDITOR
            Path.Combine(Application.persistentDataPath, "GameData");
#else
            Path.Combine(Application.dataPath, "GameData");
#endif



        public static string ResourcePacksFolder =>
            Path.Combine(
                GameDataFolder,
                "ResourcePacks"
            );



        public static string ModsFolder =>
            Path.Combine(
                GameDataFolder,
                "Mods"
            );



        public static void CreateFolders()
        {

            if (!Directory.Exists(GameDataFolder))
            {
                Directory.CreateDirectory(
                    GameDataFolder
                );
            }



            if (!Directory.Exists(ResourcePacksFolder))
            {
                Directory.CreateDirectory(
                    ResourcePacksFolder
                );
            }



            if (!Directory.Exists(ModsFolder))
            {
                Directory.CreateDirectory(
                    ModsFolder
                );
            }

        }
        public static string BlocksFolder =>
    Path.Combine(
        GameDataFolder,
        "Blocks"
    );
        public static string OresFolder
        {
            get
            {
                return Path.Combine(
                    GameDataFolder,
                    "Ores"
                );
            }
        }
    }

}
using System.IO;
using UnityEngine;


namespace Game.Content
{

    public static class DataPaths
    {

        public static string GameDataFolder =>
            Path.Combine(
                Application.dataPath,
                "GameData"
            );



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
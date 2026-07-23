using System.IO;
using UnityEngine;


namespace Game.World.Generation.Ores
{

    public static class OreFolderLoader
    {

        public static void LoadFolder(
            string folderPath
        )
        {

            if (
                !Directory.Exists(
                    folderPath
                )
            )
            {

                Debug.LogWarning(
                    "ORES FOLDER NOT FOUND: " +
                    folderPath
                );

                return;

            }



            string[] files =
                Directory.GetFiles(
                    folderPath,
                    "*.json",
                    SearchOption.AllDirectories
                );



            foreach (
                string file
                in files
            )
            {

                OreDefinition ore =
                    Game.Content.JsonLoader.Load<OreDefinition>(
                        file
                    );



                if (
                    ore == null
                )
                {

                    continue;

                }



                OreRegistry.Register(
                    ore
                );



                Debug.Log(
                    "Loaded ore: " +
                    ore.ID
                );

            }

        }

    }

}
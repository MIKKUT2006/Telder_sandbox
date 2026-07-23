using Game.Blocks;
using System.IO;
using UnityEngine;


namespace Game.Content
{

    public static class BlockFolderLoader
    {

        public static void LoadFolder(
            string folderPath
        )
        {

            Debug.Log(
                "BLOCK FOLDER PATH: " +
                folderPath
            );



            if (!Directory.Exists(folderPath))
            {

                Debug.LogError(
                    "BLOCKS FOLDER NOT FOUND: " +
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



            Debug.Log(
                "BLOCK JSON FILES FOUND: " +
                files.Length
            );



            foreach (string file in files)
            {

                Debug.Log(
                    "TRY LOAD BLOCK FILE: " +
                    file
                );



                BlockDefinition block =
                    JsonLoader.Load<BlockDefinition>(
                        file
                    );



                if (block == null)
                {

                    Debug.LogError(
                        "FAILED TO LOAD BLOCK JSON: " +
                        file
                    );

                    continue;

                }



                Debug.Log(
                    "BLOCK JSON LOADED: " +
                    block.ID
                );



                if (
                    string.IsNullOrEmpty(
                        block.ID
                    )
                )
                {

                    Debug.LogError(
                        "BLOCK HAS EMPTY ID: " +
                        file
                    );

                    continue;

                }



                string[] idParts =
                    block.ID.Split(
                        ':'
                    );



                if (
                    idParts.Length != 2
                )
                {

                    Debug.LogError(
                        "WRONG BLOCK ID FORMAT: " +
                        block.ID
                    );

                    continue;

                }



                ContentID id =
                    new ContentID(
                        idParts[0],
                        idParts[1]
                    );



                Debug.Log(
                    "REGISTERING BLOCK: " +
                    id
                );



                BlockRegistry.Register(
                    block
                );



                Debug.Log(
                    "BLOCK REGISTERED: " +
                    id
                );

            }

        }

    }

}
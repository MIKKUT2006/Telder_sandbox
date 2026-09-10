using Game.Blocks;

using System;
using System.IO;

using UnityEngine;


namespace Game.Content
{

    public static class BlockFolderLoader
    {

        // =====================================================
        // JSON EXTRA DATA
        // =====================================================
        //
        // JsonUtility / JsonLoader не умеет сам преобразовать:
        //
        // "Drop": "game:stone"
        //
        // в ContentID.
        //
        // Поэтому строковые ContentID-поля читаем отдельно.
        //

        [Serializable]
        private class BlockExtraJson
        {

            public string Drop;

        }


        // =====================================================
        // LOAD FOLDER
        // =====================================================

        public static void LoadFolder(
            string folderPath
        )
        {

            Debug.Log(
                "BLOCK FOLDER PATH: " +
                folderPath
            );


            // =================================================
            // CHECK FOLDER
            // =================================================

            if (
                !Directory.Exists(
                    folderPath
                )
            )
            {

                Debug.LogError(
                    "BLOCKS FOLDER NOT FOUND: " +
                    folderPath
                );


                return;

            }


            // =================================================
            // FIND JSON FILES
            // =================================================

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


            // =================================================
            // LOAD FILES
            // =================================================

            foreach (
                string file
                in files
            )
            {

                LoadBlockFile(
                    file
                );

            }

        }


        // =====================================================
        // LOAD BLOCK FILE
        // =====================================================

        private static void LoadBlockFile(
            string file
        )
        {

            Debug.Log(
                "TRY LOAD BLOCK FILE: " +
                file
            );


            // =================================================
            // READ RAW JSON
            // =================================================
            //
            // Он нужен отдельно для ContentID-полей.
            //

            string rawJson;


            try
            {

                rawJson =
                    File.ReadAllText(
                        file
                    );

            }
            catch (
                Exception exception
            )
            {

                Debug.LogError(
                    "FAILED TO READ BLOCK JSON: " +
                    file +
                    "\n" +
                    exception
                );


                return;

            }


            // =================================================
            // LOAD NORMAL BLOCK DATA
            // =================================================

            BlockDefinition block =
                JsonLoader.Load<BlockDefinition>(
                    file
                );


            if (
                block == null
            )
            {

                Debug.LogError(
                    "FAILED TO LOAD BLOCK JSON: " +
                    file
                );


                return;

            }


            // =================================================
            // CHECK BLOCK ID
            // =================================================

            if (
                string.IsNullOrWhiteSpace(
                    block.ID
                )
            )
            {

                Debug.LogError(
                    "BLOCK HAS EMPTY ID: " +
                    file
                );


                return;

            }


            // =================================================
            // VALIDATE BLOCK CONTENT ID
            // =================================================

            ContentID blockContentID;


            try
            {

                blockContentID =
                    ContentID.Parse(
                        block.ID
                    );

            }
            catch (
                Exception exception
            )
            {

                Debug.LogError(
                    "WRONG BLOCK ID FORMAT: " +
                    block.ID +
                    "\nFILE: " +
                    file +
                    "\n" +
                    exception
                );


                return;

            }


            // =================================================
            // LOAD EXTRA JSON DATA
            // =================================================

            BlockExtraJson extraData;


            try
            {

                extraData =
                    JsonUtility
                        .FromJson<BlockExtraJson>(
                            rawJson
                        );

            }
            catch (
                Exception exception
            )
            {

                Debug.LogError(
                    "FAILED TO READ BLOCK EXTRA DATA: " +
                    file +
                    "\n" +
                    exception
                );


                return;

            }


            // =================================================
            // PARSE DROP
            // =================================================

            if (
                extraData != null &&
                !string.IsNullOrWhiteSpace(
                    extraData.Drop
                )
            )
            {

                try
                {

                    block.Drop =
                        ContentID.Parse(
                            extraData.Drop
                        );


                    Debug.Log(
                        "BLOCK DROP PARSED: " +
                        block.ID +
                        " -> " +
                        block.Drop
                    );

                }
                catch (
                    Exception exception
                )
                {

                    Debug.LogError(
                        "INVALID BLOCK DROP ID: " +
                        extraData.Drop +
                        "\nBLOCK: " +
                        block.ID +
                        "\nFILE: " +
                        file +
                        "\n" +
                        exception
                    );


                    return;

                }

            }
            else
            {

                // =============================================
                // BLOCK HAS NO DROP
                // =============================================

                block.Drop =
                    default;


                block.DropCount =
                    0;

            }


            // =================================================
            // FIX DROP COUNT
            // =================================================
            //
            // Если Drop указан, но DropCount случайно <= 0,
            // считаем, что должен выпадать 1 предмет.
            //

            if (
                extraData != null &&
                !string.IsNullOrWhiteSpace(
                    extraData.Drop
                ) &&
                block.DropCount <= 0
            )
            {

                block.DropCount =
                    1;

            }


            // =================================================
            // DEBUG
            // =================================================

            Debug.Log(
                "BLOCK JSON LOADED: " +
                blockContentID +
                " | DROP: " +
                block.Drop +
                " x" +
                block.DropCount
            );


            // =================================================
            // REGISTER
            // =================================================

            BlockRegistry.Register(
                block
            );


            Debug.Log(
                "BLOCK REGISTERED: " +
                blockContentID
            );

        }

    }

}
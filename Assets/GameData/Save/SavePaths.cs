using System.IO;
using UnityEngine;

namespace Game.Save
{
    public static class SavePaths
    {
        public static string RootFolder
        {
            get
            {
                string path =
                    Path.Combine(
                        Application.persistentDataPath,
                        "TelderSaves"
                    );

                Directory.CreateDirectory(path);

                return path;
            }
        }


        public static string GetSaveFolder(
            string saveId
        )
        {
            return
                Path.Combine(
                    RootFolder,
                    saveId
                );
        }


        public static string GetMainFile(
            string saveId
        )
        {
            return
                Path.Combine(
                    GetSaveFolder(saveId),
                    "save.json"
                );
        }


        public static string GetPreviewFile(
            string saveId
        )
        {
            return
                Path.Combine(
                    GetSaveFolder(saveId),
                    "preview.png"
                );
        }


        public static string GetDimensionsFolder(
            string saveId
        )
        {
            return
                Path.Combine(
                    GetSaveFolder(saveId),
                    "dimensions"
                );
        }


        public static string GetDimensionFile(
            string saveId,
            string dimensionName,
            int seed
        )
        {
            string fileName =
                "dimension_" +
                seed +
                "_" +
                StableHash(dimensionName) +
                ".json";


            return
                Path.Combine(
                    GetDimensionsFolder(saveId),
                    fileName
                );
        }


        private static uint StableHash(
            string value
        )
        {
            unchecked
            {
                uint hash =
                    2166136261u;

                if (value == null)
                {
                    return hash;
                }

                for (
                    int i = 0;
                    i < value.Length;
                    i++
                )
                {
                    hash ^=
                        value[i];

                    hash *=
                        16777619u;
                }

                return hash;
            }
        }
    }
}

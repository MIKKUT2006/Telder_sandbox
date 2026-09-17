
using System.IO;
using UnityEngine;

namespace Game.World.Structures
{
    public static class StructurePaths
    {
        public static string Folder
        {
            get
            {
                string path =
                    Path.Combine(
                        Application.dataPath,
                        "GameData",
                        "Structures"
                    );

                Directory.CreateDirectory(path);
                return path;
            }
        }
    }
}

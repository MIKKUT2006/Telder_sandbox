
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
                        Game.Content.DataPaths.GameDataFolder,
                        "Structures"
                    );

                Directory.CreateDirectory(path);
                return path;
            }
        }
    }
}

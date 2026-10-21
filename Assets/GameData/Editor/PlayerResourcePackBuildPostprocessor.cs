#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Deploys the source resource packs next to a standalone build in the same location
    /// used by Game.Content.DataPaths. This is a build payload, not an in-project Resources copy.
    /// The destination is replaced wholesale so removed PNGs cannot survive in an old build folder.
    /// </summary>
    internal static class PlayerResourcePackBuildPostprocessor
    {
        private const string SourcePath = "Assets/GameData/ResourcePacks";

        [PostProcessBuild(900)]
        private static void CopyResourcePacks(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.StandaloneWindows &&
                target != BuildTarget.StandaloneWindows64 &&
                target != BuildTarget.StandaloneLinux64 &&
                target != BuildTarget.StandaloneOSX)
                return;

            string source = Path.GetFullPath(SourcePath);
            string dataRoot;
            if (target == BuildTarget.StandaloneOSX)
            {
                dataRoot = Path.Combine(pathToBuiltProject, "Contents", "Resources", "Data");
            }
            else
            {
                string buildDirectory = Path.GetDirectoryName(pathToBuiltProject);
                string executableName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
                dataRoot = Path.Combine(buildDirectory, executableName + "_Data");
            }

            string destination = Path.Combine(dataRoot, "GameData", "ResourcePacks");
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
            if (File.Exists(destination)) File.Delete(destination);

            if (!Directory.Exists(source))
            {
                Debug.LogError("RESOURCE PACKS: source folder is missing: " + source + ". Build will not contain resource packs.");
                return;
            }

            Directory.CreateDirectory(destination);
            CopyDirectory(source, destination);
            Debug.Log("RESOURCE PACKS: deployed current source packs to " + destination);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            foreach (string directory in Directory.GetDirectories(source))
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
#endif

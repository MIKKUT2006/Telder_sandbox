#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Copies file-system based runtime content from Assets/GameData into the
/// standalone player's <player>_Data/GameData directory. Also exposes a menu
/// command so an already-created Windows build can be repaired without a
/// full rebuild.
/// </summary>
public sealed class TelderBuildContentPacker : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report == null || report.summary.result != BuildResult.Succeeded)
            return;

        // Android stores GameData INSIDE APK/StreamingAssets. Do not attempt
        // copying files to a Windows-style *_Data folder next to the .apk.
        if (report.summary.platform == BuildTarget.Android ||
            report.summary.platform == BuildTarget.iOS)
            return;

        string sourceRoot = ResolveSourceRoot();
        string playerDataRoot = ResolvePlayerDataRoot(
            report.summary.platform,
            report.summary.outputPath
        );

        if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
        {
            Debug.LogError("TELDER BUILD PACKER: Cannot find source Assets/GameData. Expected: " +
                Path.Combine(Application.dataPath, "GameData"));
            return;
        }

        if (string.IsNullOrWhiteSpace(playerDataRoot) || !Directory.Exists(playerDataRoot))
        {
            Debug.LogError("TELDER BUILD PACKER: Player data folder not found. Build output: " +
                report.summary.outputPath + "; resolved data folder: " + playerDataRoot);
            return;
        }

        CopyRuntimeContent(sourceRoot, Path.Combine(playerDataRoot, "GameData"));
    }

    /// <summary>Manually repair an existing Windows/Linux build.</summary>
    [MenuItem("Tools/Telder/Copy GameData Into Existing Build...")]
    private static void CopyIntoExistingBuild()
    {
        string executable = EditorUtility.OpenFilePanel(
            "Select the built game executable",
            Directory.GetParent(Application.dataPath).FullName,
            "exe"
        );

        if (string.IsNullOrWhiteSpace(executable))
            return;

        string sourceRoot = ResolveSourceRoot();
        string playerDataRoot = ResolvePlayerDataRoot(BuildTarget.StandaloneWindows64, executable);

        if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
        {
            EditorUtility.DisplayDialog(
                "Telder: GameData not found",
                "Could not find Assets/GameData in the current Unity project.",
                "OK"
            );
            Debug.LogError("TELDER BUILD PACKER: Source folder not found: " + sourceRoot);
            return;
        }

        if (string.IsNullOrWhiteSpace(playerDataRoot) || !Directory.Exists(playerDataRoot))
        {
            EditorUtility.DisplayDialog(
                "Telder: Build folder not found",
                "The selected EXE does not have a matching *_Data folder. Select the EXE from the completed build directory.",
                "OK"
            );
            Debug.LogError("TELDER BUILD PACKER: Matching player data folder not found for: " + executable);
            return;
        }

        bool success = CopyRuntimeContent(sourceRoot, Path.Combine(playerDataRoot, "GameData"));
        EditorUtility.DisplayDialog(
            success ? "Telder: GameData copied" : "Telder: Copy failed",
            success
                ? "Runtime content has been copied. Check the Console and launch the selected build again."
                : "Some required content is missing. Check the Unity Console for details.",
            "OK"
        );
    }

    private static string ResolveSourceRoot()
    {
        string primary = Path.Combine(Application.dataPath, "GameData");
        if (Directory.Exists(primary))
            return primary;

        DirectoryInfo projectRoot = Directory.GetParent(Application.dataPath);
        if (projectRoot == null)
            return null;

        string fallback = Path.Combine(projectRoot.FullName, "Assets", "GameData");
        return Directory.Exists(fallback) ? fallback : null;
    }

    private static bool CopyRuntimeContent(string sourceRoot, string destinationRoot)
    {
        try
        {
            int sourceBlockJson = CountFiles(Path.Combine(sourceRoot, "Blocks"), "*.json");
            int sourceItemJson = CountFiles(Path.Combine(sourceRoot, "Items"), "*.json");
            int sourceBlockTextures = CountFiles(
                Path.Combine(sourceRoot, "ResourcePacks", "Default", "textures", "blocks"), "*.png");
            int sourceItemTextures = CountFiles(
                Path.Combine(sourceRoot, "ResourcePacks", "Default", "textures", "items"), "*.png");

            Debug.Log("TELDER BUILD PACKER: Source=" + sourceRoot +
                " | block JSON=" + sourceBlockJson +
                " | item JSON=" + sourceItemJson +
                " | block PNG=" + sourceBlockTextures +
                " | item PNG=" + sourceItemTextures);

            if (sourceBlockJson == 0 || sourceItemJson == 0 || sourceBlockTextures == 0)
            {
                Debug.LogError("TELDER BUILD PACKER: Source content itself is incomplete. " +
                    "Check Assets/GameData/Blocks, Items and ResourcePacks/Default/textures/blocks.");
                return false;
            }

            Directory.CreateDirectory(destinationRoot);
            string[] files = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);
            int copiedFiles = 0;
            long copiedBytes = 0;

            for (int i = 0; i < files.Length; i++)
            {
                string sourceFile = files[i];
                string relativePath = sourceFile.Substring(sourceRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (ShouldSkip(relativePath))
                    continue;

                string destinationFile = Path.Combine(destinationRoot, relativePath);
                string destinationDirectory = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrEmpty(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                File.Copy(sourceFile, destinationFile, true);
                copiedFiles++;
                copiedBytes += new FileInfo(sourceFile).Length;
            }

            int destBlockJson = CountFiles(Path.Combine(destinationRoot, "Blocks"), "*.json");
            int destItemJson = CountFiles(Path.Combine(destinationRoot, "Items"), "*.json");
            int destBlockTextures = CountFiles(
                Path.Combine(destinationRoot, "ResourcePacks", "Default", "textures", "blocks"), "*.png");
            int destItemTextures = CountFiles(
                Path.Combine(destinationRoot, "ResourcePacks", "Default", "textures", "items"), "*.png");

            Debug.Log("TELDER BUILD PACKER: Copied " + copiedFiles + " files (" +
                (copiedBytes / 1024) + " KB) to " + destinationRoot +
                " | verified block JSON=" + destBlockJson +
                ", item JSON=" + destItemJson +
                ", block PNG=" + destBlockTextures +
                ", item PNG=" + destItemTextures);

            bool valid = destBlockJson > 0 && destItemJson > 0 && destBlockTextures > 0;
            if (!valid)
            {
                Debug.LogError("TELDER BUILD PACKER: Copy finished but the build is still missing required content. " +
                    "Expected non-empty GameData/Blocks, GameData/Items and " +
                    "GameData/ResourcePacks/Default/textures/blocks.");
            }
            return valid;
        }
        catch (Exception ex)
        {
            Debug.LogError("TELDER BUILD PACKER: Copy failed: " + ex);
            return false;
        }
    }

    private static int CountFiles(string folder, string pattern)
    {
        if (!Directory.Exists(folder))
            return 0;

        return Directory.GetFiles(folder, pattern, SearchOption.AllDirectories).Length;
    }

    private static string ResolvePlayerDataRoot(BuildTarget target, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            return null;

        if (target == BuildTarget.StandaloneOSX)
        {
            string appPath = outputPath;
            if (File.Exists(appPath))
                appPath = Path.GetDirectoryName(appPath);
            return Path.Combine(appPath, "Contents", "Resources", "Data");
        }

        string fullOutputPath = Path.GetFullPath(outputPath);
        string outputDirectory = Path.GetDirectoryName(fullOutputPath);
        string playerName = Path.GetFileNameWithoutExtension(fullOutputPath);
        if (string.IsNullOrEmpty(outputDirectory) || string.IsNullOrEmpty(playerName))
            return null;

        return Path.Combine(outputDirectory, playerName + "_Data");
    }

    private static bool ShouldSkip(string relativePath)
    {
        string normalized = relativePath.Replace('\\', '/');
        string[] parts = normalized.Split('/');

        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (string.Equals(parts[i], "Editor", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        string fileName = parts[parts.Length - 1];
        string extension = Path.GetExtension(fileName);
        if (string.Equals(extension, ".meta", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".asmdef", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".asmref", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase))
            return true;

        string lowerName = fileName.ToLowerInvariant();
        return lowerName.Contains("_backup") ||
            lowerName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
            lowerName.EndsWith(".v34_backup", StringComparison.OrdinalIgnoreCase) ||
            lowerName.EndsWith(".v36_performance_backup", StringComparison.OrdinalIgnoreCase) ||
            lowerName.EndsWith(".v351_biome_backup", StringComparison.OrdinalIgnoreCase) ||
            lowerName.EndsWith(".elbow_runtime_backup", StringComparison.OrdinalIgnoreCase) ||
            lowerName.EndsWith(".cave_floor_backup", StringComparison.OrdinalIgnoreCase) ||
            lowerName.EndsWith(".cave_furniture_fix_backup", StringComparison.OrdinalIgnoreCase) ||
            lowerName.EndsWith(".cave_vegetation_backup", StringComparison.OrdinalIgnoreCase) ||
            lowerName.EndsWith(".telder_backup", StringComparison.OrdinalIgnoreCase);
    }
}
#endif

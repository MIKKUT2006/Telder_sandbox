#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Before Android build: packs file-system based content into one asset that
/// Unity puts INSIDE APK (or app assets for AAB). The mobile bootstrap copies
/// the archive to persistentDataPath and extracts it before world loading.
/// Windows/Linux build packing remains handled by TelderBuildContentPacker.
/// </summary>
public sealed class TelderAndroidGameDataPacker : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder { get { return -1000; } }

    private const string ArchiveAsset = "Assets/StreamingAssets/Telder/GameData.zip";
    private const string VersionAsset = "Assets/StreamingAssets/Telder/GameData.sha256";

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        Pack();
    }

    [MenuItem("Tools/Telder/Android/Rebuild Bundled GameData")]
    private static void Pack()
    {
        string source = Path.Combine(Application.dataPath, "GameData");
        if (!Directory.Exists(source))
            throw new BuildFailedException("TELDER ANDROID: Assets/GameData not found: " + source);

        string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.Ordinal);
        string archiveAbsolute = Path.GetFullPath(ArchiveAsset);
        Directory.CreateDirectory(Path.GetDirectoryName(archiveAbsolute));
        string temporary = archiveAbsolute + ".building";
        int count = 0;
        try
        {
            using (var fs = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, true))
            {
                foreach (string full in files)
                {
                    string relative = full.Substring(source.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace('\\', '/');
                    if (!Include(relative)) continue;
                    var entry = zip.CreateEntry(relative, System.IO.Compression.CompressionLevel.Optimal);
                    using (var input = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var output = entry.Open()) input.CopyTo(output);
                    count++;
                }
            }

            if (count < 10)
                throw new BuildFailedException("TELDER ANDROID: too few runtime files: " + count);

            if (File.Exists(archiveAbsolute)) File.Delete(archiveAbsolute);
            File.Move(temporary, archiveAbsolute);

            string hash;
            using (var fs = File.OpenRead(archiveAbsolute))
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            File.WriteAllText(Path.GetFullPath(VersionAsset), hash + "\n", new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(ArchiveAsset, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(VersionAsset, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("TELDER ANDROID: bundled " + count + " content files, " +
                (new FileInfo(archiveAbsolute).Length / 1024) + " KiB, sha256=" + hash);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        // Keep the generated payload under StreamingAssets for inspection.
        // It is regenerated from current Assets/GameData on every Android build.
    }

    private static bool Include(string relative)
    {
        string[] parts = relative.Split('/');
        for (int i = 0; i < parts.Length - 1; i++)
            if (string.Equals(parts[i], "Editor", StringComparison.OrdinalIgnoreCase))
                return false;

        string name = parts[parts.Length - 1];
        string ext = Path.GetExtension(name).ToLowerInvariant();
        if (name.StartsWith(".", StringComparison.Ordinal)) return false;
        if (name.IndexOf("backup", StringComparison.OrdinalIgnoreCase) >= 0) return false;
        // Character appearance loader optionally reads original PNG .meta to
        // recover pivot and pixels-per-unit from resource-pack textures.
        if (relative.StartsWith("ResourcePacks/", StringComparison.OrdinalIgnoreCase) &&
            relative.EndsWith(".png.meta", StringComparison.OrdinalIgnoreCase))
            return true;
        // File-system loaders need raw JSON, PNG, audio, etc. Unity-specific
        // scripts, shaders, prefabs and sprites are packaged normally by Unity.
        switch (ext)
        {
            case ".json": case ".png": case ".jpg": case ".jpeg":
            case ".webp": case ".wav": case ".ogg": case ".mp3":
            case ".txt": case ".bytes": case ".csv": case ".xml":
            case ".ttf": case ".otf": case ".layersdata":
                return true;
            default: return false;
        }
    }
}
#endif


#if UNITY_EDITOR

using System;
using System.IO;

using UnityEditor;
using UnityEngine;


public static class BiomeConfigFolderMigration
{
    private const string OldCaves =
        "Assets/GameData/Biomes/Caves";


    private const string NewCaves =
        "Assets/GameData/CaveBiomes";


    private const string OldFlora =
        "Assets/GameData/Biomes/SurfaceFlora";


    private const string NewFlora =
        "Assets/GameData/SurfaceFlora";


    [MenuItem(
        "Tools/Game/Biomes/Migrate Cave + Flora Configs"
    )]
    public static void Migrate()
    {
        EnsureFolder(
            NewCaves
        );


        EnsureFolder(
            NewFlora
        );


        int movedCaves =
            MoveJsonFiles(
                OldCaves,
                NewCaves
            );


        int movedFlora =
            MoveJsonFiles(
                OldFlora,
                NewFlora
            );


        AssetDatabase.Refresh();


        Debug.Log(
            "BIOME CONFIG MIGRATION COMPLETE | caves=" +
            movedCaves +
            " | flora=" +
            movedFlora
        );


        EditorUtility.DisplayDialog(
            "Biome Config Migration",
            "Готово.\n\n" +
            "Cave biome JSON moved: " +
            movedCaves +
            "\n" +
            "Surface flora JSON moved: " +
            movedFlora +
            "\n\n" +
            "Обычные surface-biome JSON остаются в Assets/GameData/Biomes.",
            "OK"
        );
    }


    private static int MoveJsonFiles(
        string sourceFolder,
        string destinationFolder
    )
    {
        if (
            !AssetDatabase.IsValidFolder(
                sourceFolder
            )
        )
        {
            return 0;
        }


        string absoluteSource =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                sourceFolder
            );


        if (
            !Directory.Exists(
                absoluteSource
            )
        )
        {
            return 0;
        }


        string[] files =
            Directory.GetFiles(
                absoluteSource,
                "*.json",
                SearchOption.AllDirectories
            );


        int moved =
            0;


        for (
            int i = 0;
            i < files.Length;
            i++
        )
        {
            string sourceAbsolute =
                files[i]
                    .Replace(
                        "\\",
                        "/"
                    );


            string projectRoot =
                Directory
                    .GetCurrentDirectory()
                    .Replace(
                        "\\",
                        "/"
                    );


            if (
                !sourceAbsolute.StartsWith(
                    projectRoot,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }


            string sourceAsset =
                sourceAbsolute
                    .Substring(
                        projectRoot.Length
                    )
                    .TrimStart(
                        '/'
                    );


            string fileName =
                Path.GetFileName(
                    sourceAsset
                );


            string destinationAsset =
                destinationFolder +
                "/" +
                fileName;


            if (
                File.Exists(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        destinationAsset
                    )
                )
            )
            {
                Debug.LogWarning(
                    "BIOME CONFIG MIGRATION: destination already exists, skipped: " +
                    destinationAsset
                );


                continue;
            }


            string error =
                AssetDatabase.MoveAsset(
                    sourceAsset,
                    destinationAsset
                );


            if (
                string.IsNullOrWhiteSpace(
                    error
                )
            )
            {
                moved++;
            }
            else
            {
                Debug.LogError(
                    "BIOME CONFIG MIGRATION FAILED: " +
                    sourceAsset +
                    " -> " +
                    destinationAsset +
                    " | " +
                    error
                );
            }
        }


        return moved;
    }


    private static void EnsureFolder(
        string assetPath
    )
    {
        if (
            AssetDatabase.IsValidFolder(
                assetPath
            )
        )
        {
            return;
        }


        string[] parts =
            assetPath.Split(
                '/'
            );


        string current =
            parts[
                0
            ];


        for (
            int i = 1;
            i < parts.Length;
            i++
        )
        {
            string next =
                current +
                "/" +
                parts[i];


            if (
                !AssetDatabase.IsValidFolder(
                    next
                )
            )
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }


            current =
                next;
        }
    }
}

#endif

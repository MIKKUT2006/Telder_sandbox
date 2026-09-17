#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;
using Game.World.Structures;


public static class StructureLayerPersistenceDiagnostic
{
    [MenuItem(
        "Tools/Game/Structure Editor/Show Layer Data Files",
        priority = 248
    )]
    public static void ShowFiles()
    {
        string folder =
            StructurePaths.Folder;


        if (
            string.IsNullOrWhiteSpace(
                folder
            )
            ||
            !Directory.Exists(
                folder
            )
        )
        {
            EditorUtility.DisplayDialog(
                "Structure Layer Data",
                "Structure folder not found:\n" +
                folder,
                "OK"
            );


            return;
        }


        string[] files =
            Directory.GetFiles(
                folder,
                "*.layersdata",
                SearchOption.TopDirectoryOnly
            );


        string message =
            "Folder:\n" +
            folder +
            "\n\n.layersdata files: " +
            files.Length;


        for (
            int i = 0;
            i < files.Length;
            i++
        )
        {
            message +=
                "\n\n" +
                Path.GetFileName(
                    files[i]
                );
        }


        Debug.Log(
            "STRUCTURE LAYER DATA:\n" +
            message
        );


        EditorUtility.DisplayDialog(
            "Structure Layer Data",
            message,
            "OK"
        );
    }


    [MenuItem(
        "Tools/Game/Structure Editor/Clear Layer Metadata Cache",
        priority = 249
    )]
    public static void ClearCache()
    {
        StructureLayerMetadataStore
            .InvalidateAll();


        Debug.Log(
            "STRUCTURE LAYERS: cache cleared. Next load will come from disk."
        );
    }
}

#endif

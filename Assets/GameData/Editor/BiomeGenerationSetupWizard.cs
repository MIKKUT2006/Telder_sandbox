
#if UNITY_EDITOR

using System.IO;

using UnityEditor;
using UnityEngine;


public static class BiomeGenerationSetupWizard
{
    [MenuItem(
        "Tools/Game/Biomes/Open Generation Folders"
    )]
    public static void OpenGenerationFolders()
    {
        string caveFolder =
            "Assets/GameData/CaveBiomes";


        string floraFolder =
            "Assets/GameData/SurfaceFlora";


        EnsureFolder(
            caveFolder
        );


        EnsureFolder(
            floraFolder
        );


        AssetDatabase.Refresh();


        Object caveAsset =
            AssetDatabase.LoadAssetAtPath<
                Object
            >(
                caveFolder
            );


        if (
            caveAsset !=
            null
        )
        {
            Selection.activeObject =
                caveAsset;


            EditorGUIUtility.PingObject(
                caveAsset
            );
        }


        Debug.Log(
            "BIOME GENERATION FOLDERS: " +
            caveFolder +
            " | " +
            floraFolder
        );
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

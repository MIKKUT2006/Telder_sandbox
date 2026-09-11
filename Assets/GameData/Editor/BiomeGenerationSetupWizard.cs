
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
            "Assets/GameData/Biomes/Caves";


        string floraFolder =
            "Assets/GameData/Biomes/SurfaceFlora";


        Directory.CreateDirectory(
            caveFolder
        );


        Directory.CreateDirectory(
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
            "BIOME GENERATION: " +
            caveFolder +
            " | " +
            floraFolder
        );
    }
}

#endif

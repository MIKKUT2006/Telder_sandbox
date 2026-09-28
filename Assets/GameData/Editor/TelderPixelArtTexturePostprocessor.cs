#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


public sealed class TelderPixelArtTexturePostprocessor :
    AssetPostprocessor
{
    private const string PixelRoot =
        "Assets/GameData/ResourcePacks/Default/textures/";


    private void OnPreprocessTexture()
    {
        if (
            string.IsNullOrWhiteSpace(
                assetPath
            )
            ||
            !assetPath.StartsWith(
                PixelRoot,
                System.StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return;
        }


        TextureImporter importer =
            assetImporter
            as
            TextureImporter;


        if (importer == null)
            return;


        importer.filterMode =
            FilterMode.Point;


        importer.mipmapEnabled =
            false;


        importer.textureCompression =
            TextureImporterCompression.Uncompressed;
    }


    [MenuItem(
        "Tools/Game/Pixel Art/Reimport All Game Textures As Point"
    )]
    public static void ReimportAll()
    {
        if (
            !AssetDatabase.IsValidFolder(
                PixelRoot.TrimEnd(
                    '/'
                )
            )
        )
        {
            Debug.LogWarning(
                "TELDER PIXEL: folder not found: " +
                PixelRoot
            );

            return;
        }


        string[] guids =
            AssetDatabase.FindAssets(
                "t:Texture2D",
                new[]
                {
                    PixelRoot.TrimEnd(
                        '/'
                    )
                }
            );


        int changed =
            0;


        for (
            int i = 0;
            i < guids.Length;
            i++
        )
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );


            TextureImporter importer =
                AssetImporter.GetAtPath(
                    path
                )
                as
                TextureImporter;


            if (importer == null)
                continue;


            bool dirty =
                false;


            if (
                importer.filterMode !=
                FilterMode.Point
            )
            {
                importer.filterMode =
                    FilterMode.Point;

                dirty =
                    true;
            }


            if (
                importer.mipmapEnabled
            )
            {
                importer.mipmapEnabled =
                    false;

                dirty =
                    true;
            }


            if (
                importer.textureCompression !=
                TextureImporterCompression.Uncompressed
            )
            {
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;

                dirty =
                    true;
            }


            if (dirty)
            {
                importer.SaveAndReimport();

                changed++;
            }
        }


        Debug.Log(
            "TELDER PIXEL: checked " +
            guids.Length +
            " textures, reimported " +
            changed +
            "."
        );
    }
}

#endif

#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;


public static class TelderHeldPoseHandleInjector
{
    [MenuItem(
        "Tools/Game/Held Pose/Inject Elbow Preview Handles"
    )]
    public static void Apply()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "HeldItemPoseEditorWindow t:MonoScript"
            );


        if (
            guids == null
            ||
            guids.Length == 0
        )
        {
            Debug.LogError(
                "HeldItemPoseEditorWindow not found."
            );

            return;
        }


        string assetPath =
            AssetDatabase.GUIDToAssetPath(
                guids[0]
            );


        string project =
            Directory.GetParent(
                Application.dataPath
            ).FullName;


        string path =
            Path.Combine(
                project,
                assetPath
            );


        string source =
            File.ReadAllText(
                path,
                Encoding.UTF8
            );


        if (
            source.Contains(
                "// [TELDER-ELBOW-PREVIEW-HANDLE-CALL]"
            )
        )
        {
            Debug.Log(
                "Elbow preview handles already injected."
            );

            return;
        }


        int drawMethod =
            source.IndexOf(
                "private void DrawAnimatedPreview(",
                StringComparison.Ordinal
            );


        if (drawMethod < 0)
        {
            Debug.LogError(
                "DrawAnimatedPreview() not found."
            );

            return;
        }


        int drawList =
            source.IndexOf(
                "DrawSpriteRendererList(",
                drawMethod,
                StringComparison.Ordinal
            );


        if (drawList < 0)
        {
            Debug.LogError(
                "DrawSpriteRendererList call not found."
            );

            return;
        }


        int semicolon =
            source.IndexOf(
                ';',
                drawList
            );


        if (semicolon < 0)
            return;


        string call =
@"


        // [TELDER-ELBOW-PREVIEW-HANDLE-CALL]
        {
            float paddedWidth =
                Mathf.Max(
                    0.25f,
                    bounds.size.x
                );


            float paddedHeight =
                Mathf.Max(
                    0.25f,
                    bounds.size.y
                );


            float pixelsPerWorldUnit =
                Mathf.Min(
                    inner.width /
                    paddedWidth,

                    inner.height /
                    paddedHeight
                )
                /
                Mathf.Max(
                    0.05f,
                    previewZoom
                )
                *
                0.82f;


            Vector2 worldCenter =
                new Vector2(
                    bounds.center.x +
                    previewPan.x,

                    bounds.center.y +
                    previewPan.y
                );


            DrawRotationPivotHandles(
                inner,
                worldCenter,
                inner.center,
                pixelsPerWorldUnit
            );
        }
";


        source =
            source.Insert(
                semicolon + 1,
                call
            );


        File.WriteAllText(
            path,
            source,
            new UTF8Encoding(
                false
            )
        );


        AssetDatabase.Refresh();


        Debug.Log(
            "Elbow preview handles injected."
        );
    }
}

#endif

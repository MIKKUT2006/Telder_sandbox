
#if UNITY_EDITOR

using System;
using System.IO;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

using Game.World.Background;


namespace Game.EditorTools
{

    public static class WorldVisualUpgradeWizard
    {

        private const float ParallaxRaise =
            6f;


        [MenuItem(
            "Tools/Game/Apply World Visual Upgrades"
        )]
        public static void Apply()
        {

            int rendererPatches =
                PatchChunkRenderer();


            int parallaxRoots =
                RaiseParallaxRoots();


            EditorSceneManager
                .MarkSceneDirty(
                    SceneManager
                        .GetActiveScene()
                );


            AssetDatabase.Refresh();


            EditorUtility.DisplayDialog(
                "World Visual Upgrades",
                "Готово.\n\nChunkRenderer patches: " +
                rendererPatches +
                "\nParallax roots raised: " +
                parallaxRoots +
                "\n\nBackground blocks теперь должны быть на 7% темнее foreground (0.93 brightness).\nParallax root поднят на +" +
                ParallaxRaise +
                " по Y.",
                "OK"
            );

        }


        private static int PatchChunkRenderer()
        {

            string[] guids =
                AssetDatabase.FindAssets(
                    "ChunkRenderer t:Script"
                );


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


                if (
                    !File.Exists(
                        path
                    )
                )
                {

                    continue;

                }


                string text =
                    File.ReadAllText(
                        path
                    );


                if (
                    text.IndexOf(
                        "class ChunkRenderer",
                        StringComparison.Ordinal
                    )
                    <
                    0
                )
                {

                    continue;

                }


                string pattern =
                    @"(BackgroundProperties\s*\.\s*SetFloat\s*\(\s*""_LayerBrightness""\s*,\s*)([0-9]+(?:\.[0-9]+)?f)(\s*\)\s*;)";


                string replaced =
                    Regex.Replace(
                        text,
                        pattern,
                        match =>
                            match.Groups[1].Value +
                            "0.93f" +
                            match.Groups[3].Value,
                        RegexOptions.Multiline
                    );


                if (
                    replaced ==
                    text
                )
                {

                    // Fallback for the exact v9 formatting/value.
                    replaced =
                        text.Replace(
                            "_LayerBrightness\",\n                0.68f",
                            "_LayerBrightness\",\n                0.93f"
                        );

                }


                if (
                    replaced !=
                    text
                )
                {

                    File.WriteAllText(
                        path,
                        replaced
                    );


                    return 1;

                }

            }


            return 0;

        }


        private static int RaiseParallaxRoots()
        {

            GameObject[] all =
                UnityEngine.Resources
                    .FindObjectsOfTypeAll<
                        GameObject
                    >();


            int changed =
                0;


            Scene active =
                SceneManager
                    .GetActiveScene();


            for (
                int i = 0;
                i < all.Length;
                i++
            )
            {

                GameObject gameObject =
                    all[i];


                if (
                    gameObject ==
                    null
                    ||
                    !gameObject.scene.IsValid()
                    ||
                    gameObject.scene !=
                    active
                )
                {

                    continue;

                }


                if (
                    gameObject.name.IndexOf(
                        "Parallax",
                        StringComparison.OrdinalIgnoreCase
                    )
                    <
                    0
                )
                {

                    continue;

                }


                if (
                    HasParallaxAncestor(
                        gameObject.transform
                    )
                )
                {

                    continue;

                }


                ParallaxHeightMarker marker =
                    gameObject.GetComponent<
                        ParallaxHeightMarker
                    >();


                if (
                    marker !=
                    null
                )
                {

                    continue;

                }


                Undo.RecordObject(
                    gameObject.transform,
                    "Raise Parallax Background"
                );


                Vector3 position =
                    gameObject.transform.position;


                position.y +=
                    ParallaxRaise;


                gameObject.transform.position =
                    position;


                marker =
                    Undo.AddComponent<
                        ParallaxHeightMarker
                    >(
                        gameObject
                    );


                marker.SetAppliedOffset(
                    ParallaxRaise
                );


                EditorUtility.SetDirty(
                    gameObject
                );


                changed++;

            }


            return changed;

        }


        private static bool HasParallaxAncestor(
            Transform transform
        )
        {

            Transform parent =
                transform.parent;


            while (
                parent !=
                null
            )
            {

                if (
                    parent.name.IndexOf(
                        "Parallax",
                        StringComparison.OrdinalIgnoreCase
                    )
                    >=
                    0
                )
                {

                    return true;

                }


                parent =
                    parent.parent;

            }


            return false;

        }

    }

}

#endif

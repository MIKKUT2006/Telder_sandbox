
#if UNITY_EDITOR

using System;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

using Game.World.Background;


namespace Game.EditorTools
{

    public static class CloudHeightFixWizard
    {

        [MenuItem(
            "Tools/Game/Fix Cloud Vertical Parallax"
        )]
        public static void FixClouds()
        {

            Scene activeScene =
                SceneManager.GetActiveScene();


            if (
                !activeScene.IsValid()
            )
            {

                return;

            }


            GameObject[] all =
                UnityEngine.Resources
                    .FindObjectsOfTypeAll<
                        GameObject
                    >();


            int changed =
                0;


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
                    activeScene
                )
                {

                    continue;

                }


                if (
                    !LooksLikeCloud(
                        gameObject
                    )
                )
                {

                    continue;

                }


                // If a parent is already the Cloud root, do not add
                // one lock component to every child sprite.
                if (
                    HasCloudParent(
                        gameObject.transform
                    )
                )
                {

                    continue;

                }


                CloudFixedHeight lockComponent =
                    gameObject.GetComponent<
                        CloudFixedHeight
                    >();


                if (
                    lockComponent ==
                    null
                )
                {

                    lockComponent =
                        Undo.AddComponent<
                            CloudFixedHeight
                        >(
                            gameObject
                        );


                    changed++;

                }


                lockComponent.CaptureCurrentHeight();


                EditorUtility.SetDirty(
                    gameObject
                );

            }


            EditorSceneManager.MarkSceneDirty(
                activeScene
            );


            EditorUtility.DisplayDialog(
                "Cloud Height",
                "Готово.\n\nCloud roots обновлено: " +
                changed +
                "\n\nГоризонтальный параллакс остаётся.\nВертикальный параллакс блокируется — облачный слой сохраняет исходную мировую высоту.",
                "OK"
            );

        }


        private static bool LooksLikeCloud(
            GameObject gameObject
        )
        {

            string name =
                gameObject.name ??
                string.Empty;


            bool nameMatches =
                name.IndexOf(
                    "cloud",
                    StringComparison.OrdinalIgnoreCase
                )
                >=
                0
                ||
                name.IndexOf(
                    "облак",
                    StringComparison.OrdinalIgnoreCase
                )
                >=
                0;


            if (
                !nameMatches
            )
            {

                return false;

            }


            return
                gameObject
                    .GetComponentInChildren<
                        SpriteRenderer
                    >(
                        true
                    )
                !=
                null;

        }


        private static bool HasCloudParent(
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

                string name =
                    parent.name ??
                    string.Empty;


                if (
                    name.IndexOf(
                        "cloud",
                        StringComparison.OrdinalIgnoreCase
                    )
                    >=
                    0
                    ||
                    name.IndexOf(
                        "облак",
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

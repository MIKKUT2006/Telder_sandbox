using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.World.Structures.EditorRuntime
{
    /// <summary>
    /// Forces every TMP label/input/placeholder inside StructureEditorController
    /// to use the project's TMP default font asset.
    ///
    /// This intentionally does NOT ship or assign its own font.
    /// TMP_Settings.defaultFontAsset is used, so Cyrillic support comes from
    /// the font configured as default in the project.
    /// </summary>
    public sealed class StructureEditorDefaultFontEnforcer :
        MonoBehaviour
    {
        private static StructureEditorDefaultFontEnforcer instance;

        private const float RefreshInterval = 0.35f;

        private float nextRefreshTime;

        private TMP_FontAsset lastDefaultFont;


        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Bootstrap()
        {
            EnsureInstance();
        }


        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            GameObject go =
                new GameObject(
                    "StructureEditorDefaultFontEnforcer"
                );

            DontDestroyOnLoad(
                go
            );

            instance =
                go.AddComponent<
                    StructureEditorDefaultFontEnforcer
                >();
        }


        private void Awake()
        {
            if (
                instance != null
                &&
                instance != this
            )
            {
                Destroy(
                    gameObject
                );

                return;
            }

            instance = this;

            DontDestroyOnLoad(
                gameObject
            );

            SceneManager.activeSceneChanged +=
                OnActiveSceneChanged;

            ApplyToAllStructureEditors();
        }


        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.activeSceneChanged -=
                OnActiveSceneChanged;

            instance = null;
        }


        private void Update()
        {
            if (
                Time.unscaledTime <
                nextRefreshTime
            )
            {
                return;
            }

            nextRefreshTime =
                Time.unscaledTime +
                RefreshInterval;

            TMP_FontAsset defaultFont =
                TMP_Settings.defaultFontAsset;

            /*
             * We intentionally refresh periodically because Structure Editor
             * creates/rebuilds some UI elements dynamically.
             */
            if (
                defaultFont != null
                ||
                lastDefaultFont != defaultFont
            )
            {
                ApplyToAllStructureEditors();

                lastDefaultFont =
                    defaultFont;
            }
        }


        private void OnActiveSceneChanged(
            Scene oldScene,
            Scene newScene
        )
        {
            ApplyToAllStructureEditors();
        }


        private static void ApplyToAllStructureEditors()
        {
            TMP_FontAsset defaultFont =
                TMP_Settings.defaultFontAsset;

            if (defaultFont == null)
            {
                return;
            }

            MonoBehaviour[] behaviours =
                FindAllMonoBehaviours();

            for (
                int i = 0;
                i < behaviours.Length;
                i++
            )
            {
                MonoBehaviour behaviour =
                    behaviours[i];

                if (
                    behaviour == null
                    ||
                    behaviour.gameObject == null
                )
                {
                    continue;
                }

                if (
                    !behaviour.gameObject.scene.IsValid()
                )
                {
                    continue;
                }

                Type type =
                    behaviour.GetType();

                if (
                    type == null
                    ||
                    type.Name !=
                        "StructureEditorController"
                )
                {
                    continue;
                }

                ApplyToHierarchy(
                    behaviour.transform,
                    defaultFont
                );
            }
        }


        private static void ApplyToHierarchy(
            Transform root,
            TMP_FontAsset defaultFont
        )
        {
            if (
                root == null
                ||
                defaultFont == null
            )
            {
                return;
            }

            TMP_Text[] texts =
                root.GetComponentsInChildren<
                    TMP_Text
                >(
                    true
                );

            for (
                int i = 0;
                i < texts.Length;
                i++
            )
            {
                TMP_Text text =
                    texts[i];

                if (text == null)
                {
                    continue;
                }

                if (
                    text.font !=
                    defaultFont
                )
                {
                    text.font =
                        defaultFont;

                    /*
                     * Force TMP to rebuild the mesh immediately.
                     * This is important for already-visible Cyrillic strings.
                     */
                    text.SetVerticesDirty();
                    text.SetLayoutDirty();
                }
            }

            TMP_InputField[] inputFields =
                root.GetComponentsInChildren<
                    TMP_InputField
                >(
                    true
                );

            for (
                int i = 0;
                i < inputFields.Length;
                i++
            )
            {
                TMP_InputField input =
                    inputFields[i];

                if (input == null)
                {
                    continue;
                }

                if (
                    input.textComponent != null
                    &&
                    input.textComponent.font !=
                        defaultFont
                )
                {
                    input.textComponent.font =
                        defaultFont;

                    input.textComponent
                        .SetVerticesDirty();
                }

                TMP_Text placeholder =
                    input.placeholder
                    as TMP_Text;

                if (
                    placeholder != null
                    &&
                    placeholder.font !=
                        defaultFont
                )
                {
                    placeholder.font =
                        defaultFont;

                    placeholder
                        .SetVerticesDirty();
                }
            }
        }


        private static MonoBehaviour[]
            FindAllMonoBehaviours()
        {
#if UNITY_2023_1_OR_NEWER
            return
                FindObjectsByType<
                    MonoBehaviour
                >(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
#else
            return
                FindObjectsOfType<
                    MonoBehaviour
                >(
                    true
                );
#endif
        }
    }
}

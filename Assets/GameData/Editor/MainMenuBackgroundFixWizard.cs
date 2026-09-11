#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Game.UI.MainMenu;

namespace Game.EditorTools
{
    public static class MainMenuBackgroundFixWizard
    {
        [MenuItem("Tools/Game/Fix Main Menu Background")]
        public static void FixMainMenuBackground()
        {
            Camera camera = FindMenuCamera();

            if (camera == null)
            {
                EditorUtility.DisplayDialog(
                    "Main Menu Background",
                    "Не найдена MainMenuCamera.",
                    "OK"
                );

                return;
            }

            MainMenuGradientBackground gradient =
                camera.GetComponent<
                    MainMenuGradientBackground
                >();

            if (gradient == null)
            {
                gradient =
                    camera.gameObject.AddComponent<
                        MainMenuGradientBackground
                    >();
            }

            camera.clearFlags =
                CameraClearFlags.SolidColor;

            camera.depth =
                1000f;

            camera.backgroundColor =
                new Color(
                    0.001f,
                    0.003f,
                    0.012f,
                    1f
                );

            RenderSettings.skybox =
                null;

            RenderSettings.fog =
                false;

            EditorUtility.SetDirty(
                camera.gameObject
            );

            Selection.activeGameObject =
                camera.gameObject;

            EditorUtility.DisplayDialog(
                "Main Menu Background",
                "Исправлено.\n\nТеперь MainMenu дополнительно отключает persistent SpriteRenderer/MeshRenderer/Canvas и старые камеры из DontDestroyOnLoad.\n\nЭто устраняет фон игрового мира после выхода через Pause.",
                "OK"
            );
        }

        private static Camera FindMenuCamera()
        {
            GameObject menuCamera =
                GameObject.Find(
                    "MainMenuCamera"
                );

            if (
                menuCamera != null
            )
            {
                Camera camera =
                    menuCamera
                        .GetComponent<Camera>();

                if (
                    camera != null
                )
                {
                    return camera;
                }
            }

            return Camera.main;
        }
    }
}

#endif

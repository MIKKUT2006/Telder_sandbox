#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Game.UI.MainMenu;

namespace Game.EditorTools
{
    public static class MainMenuGradientSetupWizard
    {
        [MenuItem("Tools/Game/Add Main Menu Gradient")]
        public static void AddMainMenuGradient()
        {
            Camera camera = FindMenuCamera();

            if (camera == null)
            {
                EditorUtility.DisplayDialog(
                    "Main Menu Gradient",
                    "Не найдена MainMenuCamera или Main Camera.",
                    "OK"
                );
                return;
            }

            MainMenuGradientBackground gradient =
                camera.GetComponent<MainMenuGradientBackground>();

            if (gradient == null)
            {
                gradient =
                    camera.gameObject.AddComponent<
                        MainMenuGradientBackground
                    >();
            }

            camera.clearFlags =
                CameraClearFlags.SolidColor;

            camera.backgroundColor =
                new Color(
                    0.001f,
                    0.003f,
                    0.012f,
                    1f
                );

            EditorUtility.SetDirty(camera.gameObject);
            Selection.activeGameObject = camera.gameObject;

            EditorUtility.DisplayDialog(
                "Main Menu Gradient",
                "Готово.\n\nФон меню теперь почти чёрный снизу и тёмно-синий сверху.\n\nКомпонент также сбрасывает Skybox и отключает старую DontDestroyOnLoad-камеру, если она осталась от игры.",
                "OK"
            );
        }

        private static Camera FindMenuCamera()
        {
            GameObject menuCamera =
                GameObject.Find("MainMenuCamera");

            if (menuCamera != null)
            {
                Camera camera =
                    menuCamera.GetComponent<Camera>();

                if (camera != null)
                    return camera;
            }

            if (Camera.main != null)
                return Camera.main;

            return Object.FindFirstObjectByType<Camera>();
        }
    }
}

#endif

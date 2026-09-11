using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI.MainMenu
{
    public static class MainMenuVisualCleanup
    {
        // Вызывается перед загрузкой MainMenu и ещё раз после загрузки.
        public static void CleanupPersistentWorldVisuals(
            Camera menuCamera = null
        )
        {
            // =================================================
            // SKY / RENDER SETTINGS
            // =================================================

            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            // =================================================
            // CAMERAS
            // =================================================

            Camera[] cameras =
                Object.FindObjectsByType<Camera>(
                    FindObjectsSortMode.None
                );

            for (
                int i = 0;
                i < cameras.Length;
                i++
            )
            {
                Camera camera =
                    cameras[i];

                if (
                    camera == null
                    ||
                    camera == menuCamera
                )
                {
                    continue;
                }

                // Всё, что живёт в DontDestroyOnLoad,
                // не должно продолжать рисовать игровой мир
                // поверх MainMenu.
                if (
                    IsPersistent(
                        camera.gameObject
                    )
                )
                {
                    camera.enabled =
                        false;
                }
            }

            // =================================================
            // RENDERERS
            // =================================================
            //
            // Это главное отличие от v15.
            //
            // Фон мира может быть НЕ камерой, а SpriteRenderer,
            // MeshRenderer, LineRenderer и т.д. на
            // DontDestroyOnLoad-объекте.
            //

            Renderer[] renderers =
                Object.FindObjectsByType<Renderer>(
                    FindObjectsSortMode.None
                );

            for (
                int i = 0;
                i < renderers.Length;
                i++
            )
            {
                Renderer renderer =
                    renderers[i];

                if (
                    renderer == null
                )
                {
                    continue;
                }

                if (
                    IsPersistent(
                        renderer.gameObject
                    )
                )
                {
                    renderer.enabled =
                        false;
                }
            }

            // =================================================
            // PERSISTENT CANVAS
            // =================================================

            Canvas[] canvases =
                Object.FindObjectsByType<Canvas>(
                    FindObjectsSortMode.None
                );

            for (
                int i = 0;
                i < canvases.Length;
                i++
            )
            {
                Canvas canvas =
                    canvases[i];

                if (
                    canvas == null
                )
                {
                    continue;
                }

                if (
                    IsPersistent(
                        canvas.gameObject
                    )
                )
                {
                    canvas.enabled =
                        false;
                }
            }

            // =================================================
            // AUDIO LISTENERS
            // =================================================

            AudioListener[] listeners =
                Object.FindObjectsByType<AudioListener>(
                    FindObjectsSortMode.None
                );

            for (
                int i = 0;
                i < listeners.Length;
                i++
            )
            {
                AudioListener listener =
                    listeners[i];

                if (
                    listener == null
                )
                {
                    continue;
                }

                if (
                    menuCamera != null
                    &&
                    listener.gameObject ==
                    menuCamera.gameObject
                )
                {
                    continue;
                }

                if (
                    IsPersistent(
                        listener.gameObject
                    )
                )
                {
                    listener.enabled =
                        false;
                }
            }
        }


        private static bool IsPersistent(
            GameObject gameObject
        )
        {
            if (
                gameObject == null
            )
            {
                return false;
            }

            Scene scene =
                gameObject.scene;

            return
                scene.IsValid()
                &&
                scene.name ==
                "DontDestroyOnLoad";
        }
    }
}

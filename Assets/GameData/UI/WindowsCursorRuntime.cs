using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Standard OS/Windows cursor only.
    /// No custom texture, no OnGUI drawing and no per-frame Cursor.SetCursor.
    /// </summary>
    public sealed class WindowsCursorRuntime :
        MonoBehaviour
    {
        private static WindowsCursorRuntime instance;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad
        )]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject go =
                new GameObject(
                    "[Runtime] Windows Cursor"
                );

            DontDestroyOnLoad(
                go
            );

            instance =
                go.AddComponent<
                    WindowsCursorRuntime
                >();
        }


        private void Awake()
        {
            if (
                instance != null &&
                instance != this
            )
            {
                Destroy(
                    gameObject
                );

                return;
            }


            instance =
                this;


            DontDestroyOnLoad(
                gameObject
            );


            SceneManager.sceneLoaded +=
                OnSceneLoaded;


            Restore();
        }


        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -=
                    OnSceneLoaded;


                instance =
                    null;
            }
        }


        private void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode
        )
        {
            Restore();
        }


        private void OnApplicationFocus(
            bool focus
        )
        {
            if (focus)
                Restore();
        }


        public static void Restore()
        {
            global::UnityEngine.Cursor.SetCursor(
                null,
                Vector2.zero,
                global::UnityEngine.CursorMode.Auto
            );


            global::UnityEngine.Cursor.visible =
                true;


            global::UnityEngine.Cursor.lockState =
                global::UnityEngine.CursorLockMode.None;
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.World;
using Game.Content;

namespace Game.UI.MainMenu
{
    public class WorldLoadingOverlay : MonoBehaviour
    {
        private static WorldLoadingOverlay instance;
        private bool visible;
        private string message = "Загрузка мира...";
        private GUIStyle style;
        private Texture2D black;
        private string failure;
        private float showStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            if (instance != null) return;
            GameObject go = new GameObject("WorldLoadingOverlay");
            instance = go.AddComponent<WorldLoadingOverlay>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            black = new Texture2D(1,1,TextureFormat.RGBA32,false);
            black.SetPixel(0,0,Color.black);
            black.Apply();
        }

        public static void Show(string text = "Загрузка мира...")
        {
            EnsureExists();
            instance.message = string.IsNullOrWhiteSpace(text) ? "Загрузка мира..." : text;
            instance.visible = true;
            instance.failure = null;
            instance.showStarted = Time.realtimeSinceStartup;
            instance.StopAllCoroutines();
            instance.StartCoroutine(instance.WaitForWorldReady());
        }

        public static void Hide()
        {
            if (instance != null) instance.visible = false;
        }

        private IEnumerator WaitForWorldReady()
        {
            yield return null;
            // Unity coroutines stop executing when another script throws.
            // Poll explicit startup state, and show useful errors on the device.
            while (true)
            {
                if (MobileGameDataBootstrap.HasError)
                {
                    failure = "GameData: " + MobileGameDataBootstrap.ErrorMessage;
                    yield break;
                }
                WorldManager manager = WorldManager.Instance;
                if (manager != null && !string.IsNullOrEmpty(manager.StartupError))
                {
                    failure = manager.StartupError;
                    yield break;
                }
                if (manager != null && manager.InitialSpawnReady)
                    break;
                if (Time.realtimeSinceStartup - showStarted > 260f)
                {
                    failure = "Мир не загрузился за 260 секунд. " +
                        (manager == null
                            ? "WorldManager не появился в сцене Game."
                            : "Этап: " + manager.StartupStatus);
                    Debug.LogError("TELDER WORLD LOADING: " + failure);
                    yield break;
                }
                yield return null;
            }

            // Keep the existing short visual warm-up after safe spawn.
            float visualDeadline = Time.realtimeSinceStartup + 2f;
            while (WorldManager.Instance != null &&
                   !WorldManager.Instance.InitialSpawnVisualReady &&
                   Time.realtimeSinceStartup < visualDeadline)
                yield return null;
            yield return null;
            yield return null;
            visible = false;
        }

        private void OnGUI()
        {
            if (!visible) return;
            GUI.depth = -100000;
            Color old = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), black);
            GUI.color = Color.white;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.038f, 24f, 44f));
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.white;
            }
            string display = message;
            if (!string.IsNullOrEmpty(failure))
            {
                display = "Ошибка загрузки мира\n" + failure +
                    "\n\nСделай скриншот этого сообщения или открой Android Logcat.";
            }
            else if (!MobileGameDataBootstrap.IsReady)
            {
                display = "Подготовка GameData внутри приложения...";
            }
            else if (WorldManager.Instance != null)
            {
                WorldManager manager = WorldManager.Instance;
                display = message + "\n" + manager.StartupStatus +
                    "\n" + Mathf.RoundToInt(manager.StartupProgress * 100f) + "%";
            }
            else
            {
                display = message + "\nОжидание WorldManager в сцене...";
            }
            style.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.03f, 16f, 36f));
            style.wordWrap = true;
            GUI.Label(new Rect(Screen.width * .08f, Screen.height * .14f,
                Screen.width * .84f, Screen.height * .68f), display, style);
            if (!string.IsNullOrEmpty(failure))
            {
                if (GUI.Button(new Rect(Screen.width * .34f, Screen.height * .82f,
                    Screen.width * .32f, Screen.height * .11f), "Повторить"))
                {
                    string currentScene = SceneManager.GetActiveScene().name;
                    Show(message);
                    SceneManager.LoadScene(currentScene);
                }
            }
            GUI.color = old;
        }
    }
}

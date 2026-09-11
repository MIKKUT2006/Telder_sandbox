using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.World;

namespace Game.UI.MainMenu
{
    public class WorldLoadingOverlay : MonoBehaviour
    {
        private static WorldLoadingOverlay instance;
        private bool visible;
        private string message = "Загрузка мира...";
        private GUIStyle style;
        private Texture2D black;

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
            while (WorldManager.Instance == null) yield return null;
            while (WorldManager.Instance.GetLoader() == null) yield return null;

            // The WorldManager marks itself initialized in Awake, so do not use IsReady alone.
            // Wait until the initial chunk exists and the initial generation queue is drained.
            while (!WorldManager.Instance.GetLoader().IsChunkLoaded(0,0)) yield return null;
            while (WorldManager.Instance.GetLoader().GetLoadQueueSize() > 0) yield return null;

            // Let renderers/light textures receive one full frame.
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
            GUI.Label(new Rect(0,0,Screen.width,Screen.height), message, style);
            GUI.color = old;
        }
    }
}

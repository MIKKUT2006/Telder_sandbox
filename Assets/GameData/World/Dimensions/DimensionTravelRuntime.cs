using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.World.Dimensions
{
    public static class DimensionTravelRuntime
    {
        private static DimensionDefinition current;
        private static bool traveling;

        public static DimensionDefinition Current
        {
            get
            {
                EnsureInitialized();
                return current;
            }
        }

        public static bool IsTraveling => traveling;

        public static bool IsCurrent(string dimensionName)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(dimensionName))
                return false;

            return string.Equals(
                current.Name,
                dimensionName.Trim(),
                StringComparison.OrdinalIgnoreCase
            );
        }

        public static void TravelTo(string dimensionName)
        {
            if (traveling || string.IsNullOrWhiteSpace(dimensionName))
                return;

            EnsureInitialized();

            // Сначала меняем текущее измерение.
            // После reload новый WorldManager получит уже новый seed.
            current = DimensionDatabase.GetOrCreate(dimensionName.Trim());
            traveling = true;

            DimensionSceneTransition.EnsureExists().BeginTransition();
        }

        internal static void FinishTravel()
        {
            traveling = false;
        }

        private static void EnsureInitialized()
        {
            if (current != null)
                return;

            DimensionDatabase.Initialize();
            current = DimensionDatabase.GetOrCreate(DimensionDatabase.StartDimension);
        }
    }

    // Не требует Canvas/Prefab. Делает короткий fade и reload активной игровой сцены.
    public class DimensionSceneTransition : MonoBehaviour
    {
        private static DimensionSceneTransition instance;

        private Texture2D blackTexture;
        private float alpha;
        private bool running;

        public static DimensionSceneTransition EnsureExists()
        {
            if (instance != null)
                return instance;

            GameObject go = new GameObject("DimensionSceneTransition");
            instance = go.AddComponent<DimensionSceneTransition>();
            DontDestroyOnLoad(go);
            return instance;
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

            blackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            blackTexture.SetPixel(0, 0, Color.black);
            blackTexture.Apply();
        }

        public void BeginTransition()
        {
            if (!running)
                StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            running = true;

            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            alpha = 1f;

            Scene active = SceneManager.GetActiveScene();
            AsyncOperation operation = SceneManager.LoadSceneAsync(active.buildIndex, LoadSceneMode.Single);

            if (operation == null)
            {
                DimensionTravelRuntime.FinishTravel();
                running = false;
                yield break;
            }

            while (!operation.isDone)
                yield return null;

            // Один кадр новому WorldManager на Awake/Start.
            yield return null;

            duration = 0.35f;
            elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            alpha = 0f;
            running = false;
            DimensionTravelRuntime.FinishTravel();
        }

        private void OnGUI()
        {
            if (alpha <= 0.001f || blackTexture == null)
                return;

            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
            GUI.color = old;
        }
    }
}

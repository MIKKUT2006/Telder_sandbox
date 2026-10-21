using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Localization
{
    /// <summary>
    /// Localizes legacy UI immediately before Canvas is rendered. In particular,
    /// no polling interval may allow the first Russian frame of a new tab/menu.
    /// UI created by code is picked up on the same frame it becomes visible.
    /// </summary>
    public sealed class LocalizationUIRuntime : MonoBehaviour
    {
        private sealed class Label
        {
            public string original;
            public string rendered;
            public bool skipInput;
        }

        private readonly Dictionary<TMP_Text, Label> tmp = new Dictionary<TMP_Text, Label>();
        private readonly Dictionary<Text, Label> legacy = new Dictionary<Text, Label>();
        private readonly List<TMP_Text> deadTmp = new List<TMP_Text>();
        private readonly List<Text> deadLegacy = new List<Text>();

        private bool applying;
        private int lastVersion = -1;
        private int lastCleanupFrame = -120;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<LocalizationUIRuntime>() != null) return;
            var obj = new GameObject("Telder Localization UI");
            DontDestroyOnLoad(obj);
            obj.AddComponent<LocalizationUIRuntime>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameLocalization.Changed += OnLanguageChanged;
            Canvas.willRenderCanvases += TranslateBeforeRender;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= TranslateBeforeRender;
            GameLocalization.Changed -= OnLanguageChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Cover UI instantiated by Awake/OnEnable. Any UI instantiated later
            // by Start/Update is covered by Canvas.willRenderCanvases.
            TranslateNow();
        }

        private void OnLanguageChanged()
        {
            // Translate existing labels synchronously when the option changes;
            // do not wait for Update, LateUpdate or the next 0.8 second scan.
            TranslateNow();
        }

        private void TranslateBeforeRender()
        {
            TranslateNow();
        }

        /// <summary>
        /// Also safe for code that needs to force localization before showing a
        /// panel. Reentrancy is blocked because EnsureLoaded may fire Changed.
        /// </summary>
        public static void TranslateVisibleNow()
        {
            var runtime = FindObjectOfType<LocalizationUIRuntime>();
            if (runtime != null) runtime.TranslateNow();
        }

        private void TranslateNow()
        {
            if (applying) return;
            applying = true;
            try
            {
                GameLocalization.EnsureLoaded();
                bool forced = lastVersion != GameLocalization.Version;
                lastVersion = GameLocalization.Version;

                // Active objects only: invisible tabs will be translated on their
                // first visible frame. This keeps scanning cheap on mobile.
                foreach (TMP_Text target in FindObjectsByType<TMP_Text>(
                             FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (target == null) continue;
                    Label value;
                    if (!tmp.TryGetValue(target, out value))
                    {
                        value = new Label();
                        var input = target.GetComponentInParent<TMP_InputField>();
                        value.skipInput = input != null && input.textComponent == target;
                        tmp[target] = value;
                    }
                    if (value.skipInput) continue;
                    string current = target.text;
                    if (!forced && current == value.rendered) continue;

                    // Panels that translate text during creation store the original
                    // source explicitly, avoiding an irreversible English cache.
                    LocalizationSourceText source = target.GetComponent<LocalizationSourceText>();
                    string original = source != null ? source.Source : null;
                    UpdateLabel(current, s => target.text = s, value, forced, original);
                }

                foreach (Text target in FindObjectsByType<Text>(
                             FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (target == null) continue;
                    Label value;
                    if (!legacy.TryGetValue(target, out value))
                    {
                        value = new Label();
                        var input = target.GetComponentInParent<InputField>();
                        value.skipInput = input != null && input.textComponent == target;
                        legacy[target] = value;
                    }
                    if (value.skipInput) continue;
                    string current = target.text;
                    if (!forced && current == value.rendered) continue;

                    LocalizationSourceText source = target.GetComponent<LocalizationSourceText>();
                    string original = source != null ? source.Source : null;
                    UpdateLabel(current, s => target.text = s, value, forced, original);
                }

                if (Time.frameCount - lastCleanupFrame >= 120)
                {
                    lastCleanupFrame = Time.frameCount;
                    CleanupDestroyedObjects();
                }
            }
            finally
            {
                applying = false;
            }
        }

        private static void UpdateLabel(
            string current, System.Action<string> set, Label label,
            bool forced, string explicitSource)
        {
            if (!forced && current == label.rendered) return;

            // Explicit source wins even when the first value the translator sees
            // has already been translated by the constructor.
            if (!string.IsNullOrEmpty(explicitSource))
            {
                label.original = explicitSource;
            }
            else if (label.rendered != current)
            {
                // A gameplay UI script has updated a dynamic text. Treat it as
                // the new source instead of restoring an old caption.
                label.original = current;
            }

            string translated = GameLocalization.UI(label.original);
            label.rendered = translated;
            if (current != translated) set(translated);
        }

        private void CleanupDestroyedObjects()
        {
            deadTmp.Clear();
            foreach (var pair in tmp)
                if (pair.Key == null) deadTmp.Add(pair.Key);
            foreach (var key in deadTmp) tmp.Remove(key);

            deadLegacy.Clear();
            foreach (var pair in legacy)
                if (pair.Key == null) deadLegacy.Add(pair.Key);
            foreach (var key in deadLegacy) legacy.Remove(key);
        }
    }
}

using System;
using Game.Content;
using Game.Localization;
using Game.UI.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.SpecialWorlds
{
    /// <summary>Modal mini-game selector on the existing MainMenu Canvas.</summary>
    public sealed class SpecialWorldsMenu : MonoBehaviour
    {
        private static bool returnToSelector;
        public static void ReturnToSelectorAfterSceneLoad() { returnToSelector = true; }
        public static bool ConsumeReturnToSelector()
        {
            bool reopen = returnToSelector;
            returnToSelector = false;
            return reopen;
        }
        private MainMenuController owner;
        private GameObject modal;
        private GameObject home;
        private Action onBack;

        public static void Open(MainMenuController menu, GameObject homePanel, Action back)
        {
            if (menu == null || homePanel == null) return;
            var instance = menu.GetComponent<SpecialWorldsMenu>();
            if (instance == null) instance = menu.gameObject.AddComponent<SpecialWorldsMenu>();
            instance.owner = menu;
            instance.home = homePanel;
            instance.onBack = back;
            instance.Show();
        }

        private static TextMeshProUGUI Text(Transform parent, string value, int pointSize)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = pointSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button Button(Transform parent, string name, string caption, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(.15f, .20f, .32f, .97f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());
            var t = Text(go.transform, caption, 26);
            var tr = (RectTransform)t.transform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            return btn;
        }

        private void Show()
        {
            if (modal != null) Destroy(modal);
            // HomePanel is hidden while the selector is open; its parent remains visible.
            Transform parent = home.transform.parent;
            modal = new GameObject("TelderSpecialWorldsPanel", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(parent, false);
            modal.transform.SetAsLastSibling();
            var rect = (RectTransform)modal.transform;
            rect.anchorMin = new Vector2(.24f, .15f);
            rect.anchorMax = new Vector2(.76f, .85f);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            modal.GetComponent<Image>().color = new Color(.035f, .05f, .09f, .97f);
            var layout = modal.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 18, 18);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Text(modal.transform, GameLocalization.Get("ui.special.title", "ОСОБЫЕ МИРЫ"), 37).gameObject.AddComponent<LayoutElement>().preferredHeight = 65;
            Text(modal.transform, GameLocalization.Get("ui.special.subtitle", "Мини-испытания. Не влияют на сохранения обычных миров."), 19)
                .gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            Button(modal.transform, "HarpoonChallengeButton", GameLocalization.Get("ui.special.harpoon", "ГАРПУН\nЯркие пещеры · бесконечный паркур"), StartHarpoon)
                .gameObject.AddComponent<LayoutElement>().preferredHeight = 112;
            Button(modal.transform, "BackButton", GameLocalization.Get("ui.special.back", "НАЗАД"), Close)
                .gameObject.AddComponent<LayoutElement>().preferredHeight = 62;
            home.SetActive(false);
        }

        private void StartHarpoon()
        {
            if (!MobileGameDataBootstrap.IsReady)
            {
                Debug.LogWarning("SPECIAL WORLDS: GameData is still unpacking; wait a moment.");
                return;
            }
            Time.timeScale = 1f;
            SpecialHarpoonChallenge.Launch();
        }

        public void Close()
        {
            if (modal != null) { Destroy(modal); modal = null; }
            if (onBack != null) onBack();
            else if (home != null) home.SetActive(true);
        }

        private void OnDestroy()
        {
            if (modal != null) Destroy(modal);
        }
    }
}

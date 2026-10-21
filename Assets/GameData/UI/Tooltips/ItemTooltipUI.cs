using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.InputSupport;
using Game.Items;
using Game.Localization;
using Game.Blocks;
using Game.Content;

namespace Game.UI.Tooltips
{
    /// <summary>Shows the localized item name, and its optional description beneath it.</summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        public static ItemTooltipUI Instance { get; private set; }
        private RectTransform panel;
        private TMP_Text title;
        private TMP_Text description;
        private Canvas canvas;
        private RectTransform canvasRect;
        private Camera canvasCamera;
        private bool visible;
        private float downOffset = 34f;
        private string shownItemId;
        private int localizationVersion = -1;
        private const float Width = 275f;
        private const float Padding = 11f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (Instance != null) return;
            foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (candidate == null || !candidate.isActiveAndEnabled ||
                    candidate.renderMode != RenderMode.ScreenSpaceOverlay ||
                    candidate.name == "GamepadFocusOverlay" || candidate.sortingOrder >= 3000)
                    continue;
                Create(candidate.transform);
                return;
            }
        }

        private static TMP_Text MakeText(Transform parent, string objectName, int size, Color color)
        {
            GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.color = color;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            return text;
        }

        public static ItemTooltipUI Create(Transform parent)
        {
            if (Instance != null) return Instance;
            var root = new GameObject("ItemTooltip", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var ui = root.AddComponent<ItemTooltipUI>();
            ui.panel = root.GetComponent<RectTransform>();
            ui.panel.anchorMin = ui.panel.anchorMax = new Vector2(.5f, .5f);
            ui.panel.pivot = new Vector2(0f, 1f);
            ui.panel.sizeDelta = new Vector2(Width, 40f);
            var image = root.GetComponent<Image>();
            image.color = new Color(.02f, .02f, .025f, .95f);
            image.raycastTarget = false;
            ui.title = MakeText(root.transform, "ItemName", 16, Color.white);
            ui.description = MakeText(root.transform, "ItemDescription", 13, new Color(.78f, .82f, .89f));
            root.transform.SetAsLastSibling();
            root.SetActive(false);
            Instance = ui;
            return ui;
        }

        public static void ShowItem(string itemId, Vector2 position, float downOffset = 34f)
        {
            if (Instance == null) EnsureRuntime();
            if (Instance == null || string.IsNullOrWhiteSpace(itemId)) return;
            Instance.shownItemId = itemId;
            Instance.RefreshItem();
            Instance.Display(position, downOffset);
        }

        private void RefreshItem()
        {
            string name = shownItemId;
            string details = "";
            ItemDefinition item;
            if (ItemRegistry.TryGet(shownItemId, out item) && item != null)
            {
                name = !string.IsNullOrWhiteSpace(item.Name) ? item.Name : shownItemId;
                details = item.Description;
            }
            else
            {
                try
                {
                    ContentID blockId = ContentID.Parse(shownItemId);
                    if (BlockRegistry.Contains(blockId))
                    {
                        BlockDefinition block = BlockRegistry.Get(blockId);
                        if (block != null)
                        {
                            name = string.IsNullOrWhiteSpace(block.Name) ? shownItemId : block.Name;
                            details = block.Description;
                        }
                    }
                }
                catch (System.Exception) { /* Unknown ID: show ID as fallback. */ }
            }
            // Item ID is stable; translations never replace IDs in inventory or saves.
            name = GameLocalization.ContentName(shownItemId, name);
            details = GameLocalization.ContentDescription(shownItemId, details);
            string foodDetails = Game.PlayerStats.FoodMetadataRegistry.Describe(shownItemId);
            if (!string.IsNullOrWhiteSpace(foodDetails))
                details = string.IsNullOrWhiteSpace(details) ? foodDetails : details + "\n" + foodDetails;
            SetText(name, details);
            localizationVersion = GameLocalization.Version;
        }

        private void SetText(string name, string details)
        {
            title.text = name ?? "";
            bool hasDetails = !string.IsNullOrWhiteSpace(details);
            description.gameObject.SetActive(hasDetails);
            if (hasDetails) description.text = details;
            float inner = Width - 2f * Padding;
            float titleHeight = Mathf.Max(21f, title.GetPreferredValues(title.text, inner, 0f).y);
            float descriptionHeight = hasDetails ? Mathf.Max(17f, description.GetPreferredValues(details, inner, 0f).y) : 0f;
            float separation = hasDetails ? 5f : 0f;
            float height = Padding * 2f + titleHeight + separation + descriptionHeight;
            panel.sizeDelta = new Vector2(Width, height);
            PositionText(title.rectTransform, -Padding, titleHeight, inner);
            if (hasDetails) PositionText(description.rectTransform, -Padding - titleHeight - separation, descriptionHeight, inner);
        }

        private static void PositionText(RectTransform rect, float top, float height, float width)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Padding, top);
            rect.sizeDelta = new Vector2(width, height);
        }

        // Compatibility with other code that may show a non-item tooltip.
        public void Show(string value, Vector2 position, float downOffset = 34f)
        {
            shownItemId = null;
            SetText(value, "");
            Display(position, downOffset);
        }

        private void Display(Vector2 position, float offset)
        {
            downOffset = offset;
            visible = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ResolveCanvas();
            Reposition(position);
        }

        public static void Hide()
        {
            if (Instance == null) return;
            Instance.visible = false;
            Instance.shownItemId = null;
            Instance.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!visible) return;
            if (shownItemId != null && localizationVersion != GameLocalization.Version)
                RefreshItem();
            Vector3 position = GameInput.PointerPosition;
            Reposition(new Vector2(position.x, position.y));
        }

        private void ResolveCanvas()
        {
            canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? (canvas.worldCamera != null ? canvas.worldCamera : Camera.main) : null;
        }

        private void Reposition(Vector2 screenPosition)
        {
            if (canvasRect == null) ResolveCanvas();
            if (canvasRect == null) return;
            Vector2 position;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvasCamera, out position)) return;
            position.y -= downOffset;
            Rect bounds = canvasRect.rect;
            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax - panel.rect.width);
            position.y = Mathf.Clamp(position.y, bounds.yMin + panel.rect.height, bounds.yMax);
            panel.anchoredPosition = position;
        }
    }
}

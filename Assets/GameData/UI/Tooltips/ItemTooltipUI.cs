using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Items;

namespace Game.UI.Tooltips
{
    public class ItemTooltipUI : MonoBehaviour
    {
        public static ItemTooltipUI Instance { get; private set; }
        private RectTransform panel;
        private TMP_Text text;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (Instance != null) return;
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            Create(canvas.transform);
        }

        public static ItemTooltipUI Create(Transform parent)
        {
            if (Instance != null) return Instance;
            GameObject root = new GameObject("ItemTooltip", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            ItemTooltipUI ui = root.AddComponent<ItemTooltipUI>();
            ui.panel = root.GetComponent<RectTransform>();
            ui.panel.anchorMin = ui.panel.anchorMax = new Vector2(.5f,.5f);
            ui.panel.sizeDelta = new Vector2(220f, 34f);
            root.GetComponent<Image>().color = new Color(.02f,.02f,.025f,.94f);
            GameObject t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            t.transform.SetParent(root.transform,false);
            RectTransform tr=t.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(7,2);tr.offsetMax=new Vector2(-7,-2);
            ui.text=t.GetComponent<TMP_Text>();ui.text.fontSize=15;ui.text.alignment=TextAlignmentOptions.Center;ui.text.raycastTarget=false;
            root.transform.SetAsLastSibling(); root.SetActive(false); Instance=ui; return ui;
        }

        public static void ShowItem(string itemId, Vector2 screenPosition, float downOffset = 34f)
        {
            if (Instance == null) EnsureRuntime();
            if (Instance == null || string.IsNullOrWhiteSpace(itemId)) return;
            string name=itemId;
            if (ItemRegistry.TryGet(itemId, out ItemDefinition def) && def != null && !string.IsNullOrWhiteSpace(def.Name)) name=def.Name;
            Instance.Show(name, screenPosition, downOffset);
        }

        public void Show(string value, Vector2 screenPosition, float downOffset=34f)
        {
            if (panel == null) return;
            gameObject.SetActive(true); transform.SetAsLastSibling(); text.text=value;
            RectTransform canvasRect = transform.parent as RectTransform;
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 local))
                panel.anchoredPosition = local + Vector2.down * downOffset;
        }

        public static void Hide()
        {
            if (Instance != null) Instance.gameObject.SetActive(false);
        }
    }
}

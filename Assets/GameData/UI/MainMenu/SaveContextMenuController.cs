using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Save;

namespace Game.UI.MainMenu
{
    public class SaveContextMenuController : MonoBehaviour
    {
        public static SaveContextMenuController Instance { get; private set; }

        private SaveSelectionController owner;
        private string saveId;
        private GameObject panel;
        private TMP_InputField input;

        private void Awake()
        {
            Instance = this;
            BuildUI();
            Close();
        }

        public static SaveContextMenuController EnsureExists(SaveSelectionController owner)
        {
            if (Instance != null)
            {
                Instance.owner = owner;
                return Instance;
            }
            GameObject go = new GameObject("SaveContextMenu");
            DontDestroyOnLoad(go);
            SaveContextMenuController controller = go.AddComponent<SaveContextMenuController>();
            controller.owner = owner;
            return controller;
        }

        public void Open(string id, string displayName)
        {
            saveId = id;
            input.text = displayName ?? string.Empty;
            panel.SetActive(true);
            input.Select();
            input.ActivateInputField();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            saveId = null;
        }

        private void Rename()
        {
            if (SaveGameRuntime.RenameSave(saveId, input.text))
            {
                Close();
                owner?.Refresh();
            }
        }

        private void Delete()
        {
            if (SaveGameRuntime.DeleteSave(saveId))
            {
                Close();
                owner?.Refresh();
            }
        }

        private void BuildUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject c = new GameObject("SaveContextCanvas");
                canvas = c.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                c.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                c.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(c);
            }

            panel = new GameObject("SaveContextPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(.5f,.5f);
            pr.sizeDelta = new Vector2(420f, 220f);
            panel.GetComponent<Image>().color = new Color(.025f,.025f,.04f,.97f);

            input = CreateInput(panel.transform, new Vector2(0,50));
            CreateButton(panel.transform, "ПЕРЕИМЕНОВАТЬ", new Vector2(0,-15), Rename);
            CreateButton(panel.transform, "УДАЛИТЬ", new Vector2(0,-70), Delete);
            CreateButton(panel.transform, "ОТМЕНА", new Vector2(0,-125), Close);
        }

        private TMP_InputField CreateInput(Transform parent, Vector2 pos)
        {
            GameObject go = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent,false);
            RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.sizeDelta=new Vector2(340,44); r.anchoredPosition=pos;
            go.GetComponent<Image>().color = new Color(.1f,.1f,.14f,1f);
            GameObject tx = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); tx.transform.SetParent(go.transform,false);
            RectTransform tr=tx.GetComponent<RectTransform>(); tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(10,4);tr.offsetMax=new Vector2(-10,-4);
            TMP_Text t=tx.GetComponent<TMP_Text>(); t.fontSize=20;t.alignment=TextAlignmentOptions.MidlineLeft;
            TMP_InputField f=go.GetComponent<TMP_InputField>(); f.textComponent=t; f.targetGraphic=go.GetComponent<Image>();
            return f;
        }

        private void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            GameObject go=new GameObject(label,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false);
            RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(260,42);r.anchoredPosition=pos;
            go.GetComponent<Image>().color=new Color(.10f,.10f,.15f,1f); go.GetComponent<Button>().onClick.AddListener(action);
            GameObject tx=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));tx.transform.SetParent(go.transform,false);RectTransform tr=tx.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
            TMP_Text t=tx.GetComponent<TMP_Text>();t.text=label;t.fontSize=17;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
        }
    }
}

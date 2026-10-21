using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Networking
{
    /// <summary>Builds the LAN multiplayer menu at runtime, requiring no scene prefab edits.</summary>
    public sealed class NetworkMenuPanel : MonoBehaviour
    {
        private GameObject root, hostTab, joinTab;
        private TMP_InputField nickInput, joinNickInput, worldInput, addressInput;
        private TMP_Text status, serverList;
        private Transform serverButtons;
        private TelderNetworkRuntime net;
        private float nextRefresh;
        private string selectedTab = "join";
        private readonly List<GameObject> dynamicServerButtons = new List<GameObject>();

        public static void Ensure(GameObject homePanel)
        {
            if (homePanel == null) return;
            var parent = homePanel.transform.parent;
            if (parent == null) return;
            var existing = parent.GetComponentInChildren<NetworkMenuPanel>(true);
            if (existing != null) return;
            // This object contains UI, so it must have a RectTransform rather than a plain Transform.
            var go = new GameObject("NetworkMenuPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var panelRect = (RectTransform)go.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;
            go.AddComponent<NetworkMenuPanel>();
        }

        private void Start()
        {
            net = TelderNetworkRuntime.Instance;
            Build();
            root.SetActive(false);
        }

        private void Build()
        {
            root = new GameObject("NetworkOverlay", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(.035f,.045f,.065f,.97f);

            MakeText(root.transform, "Title", "СЕТЕВАЯ ИГРА", 34, new Vector2(0, .43f), new Vector2(.8f,.08f), TextAlignmentOptions.Center);
            MakeButton(root.transform, "Back", "НАЗАД", new Vector2(-.38f,.43f), new Vector2(.15f,.065f), () =>
                {
                    root.SetActive(false);
                    var menu = FindObjectOfType<Game.UI.MainMenu.MainMenuController>();
                    if (menu != null) menu.ShowHome();
                });
            MakeButton(root.transform, "HostTabButton", "СОЗДАТЬ СЕТЕВУЮ ИГРУ", new Vector2(-.18f,.32f), new Vector2(.3f,.065f), () => ShowTab("host"));
            MakeButton(root.transform, "JoinTabButton", "ПОДКЛЮЧИТЬСЯ", new Vector2(.18f,.32f), new Vector2(.3f,.065f), () => ShowTab("join"));

            hostTab = MakePanel("HostTab", root.transform);
            MakeText(hostTab.transform, "HostHint", "Создай новый мир. Он будет сохранён на компьютере хоста.", 18, new Vector2(0,.36f), new Vector2(.72f,.08f), TextAlignmentOptions.Center);
            nickInput = MakeInput(hostTab.transform, "Nickname", "Твой ник", new Vector2(0,.22f), new Vector2(.42f,.07f));
            nickInput.text = PlayerPrefs.GetString("Telder.NetworkNick", "Игрок");
            worldInput = MakeInput(hostTab.transform, "WorldName", "Название мира", new Vector2(0,.12f), new Vector2(.42f,.07f));
            worldInput.text = "Сетевая игра";
            MakeButton(hostTab.transform, "Create", "СОЗДАТЬ И ЗАПУСТИТЬ", new Vector2(0,-.02f), new Vector2(.42f,.075f), StartHost);

            joinTab = MakePanel("JoinTab", root.transform);
            MakeText(joinTab.transform, "JoinHint", "Поиск в локальной сети / Radmin VPN", 18, new Vector2(0,.36f), new Vector2(.7f,.06f), TextAlignmentOptions.Center);
            joinNickInput = MakeInput(joinTab.transform, "JoinNickname", "Твой ник", new Vector2(-.23f,.25f), new Vector2(.34f,.065f));
            joinNickInput.text = PlayerPrefs.GetString("Telder.NetworkNick", "Игрок");
            addressInput = MakeInput(joinTab.transform, "Address", "IP-адрес сервера (например 26.x.x.x)", new Vector2(.2f,.25f), new Vector2(.38f,.065f));
            MakeButton(joinTab.transform, "Refresh", "ОБНОВИТЬ СПИСОК", new Vector2(-.22f,.13f), new Vector2(.3f,.06f), Refresh);
            MakeButton(joinTab.transform, "Connect", "ПОДКЛЮЧИТЬСЯ ПО IP", new Vector2(.2f,.13f), new Vector2(.3f,.06f), () => { net.SetNick(joinNickInput.text); net.Join(addressInput.text, joinNickInput.text); });
            var rows = new GameObject("ServerRows", typeof(RectTransform));
            rows.transform.SetParent(joinTab.transform, false);
            var rowsRect = (RectTransform)rows.transform;
            rowsRect.anchorMin = new Vector2(.5f,.5f); rowsRect.anchorMax = new Vector2(.5f,.5f);
            rowsRect.sizeDelta = new Vector2(820,250); rowsRect.anchoredPosition = new Vector2(0,-115);
            serverButtons = rows.transform;
            serverList = MakeText(joinTab.transform, "ServerListHint", "Поиск серверов…", 15, new Vector2(0,-.36f), new Vector2(.8f,.045f), TextAlignmentOptions.Center);
            status = MakeText(root.transform, "Status", "", 15, new Vector2(0,-.43f), new Vector2(.9f,.05f), TextAlignmentOptions.Center);
            ShowTab("join");
        }

        private GameObject MakePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private TMP_Text MakeText(Transform parent, string name, string value, float size, Vector2 center, Vector2 dims, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            // center is expressed as an offset from the screen center (-0.5..0.5),
            // while Unity anchors use normalized coordinates (0..1).
            Vector2 anchor = new Vector2(Mathf.Clamp01(0.5f + center.x), Mathf.Clamp01(0.5f + center.y));
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(dims.x * 1000, dims.y * 700);
            rt.anchoredPosition = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>(); t.text = value; t.fontSize = size; t.color = Color.white; t.alignment = align; t.enableWordWrapping = true;
            return t;
        }

        private Button MakeButton(Transform parent, string name, string value, Vector2 center, Vector2 dims, Action action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            // center is expressed as an offset from the screen center (-0.5..0.5),
            // while Unity anchors use normalized coordinates (0..1).
            Vector2 anchor = new Vector2(Mathf.Clamp01(0.5f + center.x), Mathf.Clamp01(0.5f + center.y));
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(dims.x * 1000, dims.y * 700);
            rt.anchoredPosition = Vector2.zero;
            var image = go.GetComponent<Image>(); image.color = new Color(.14f,.19f,.26f,.98f);
            var button = go.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
            var text = MakeText(go.transform, "Label", value, 17, new Vector2(.5f,.5f), new Vector2(.95f,.8f), TextAlignmentOptions.Center);
            var labelRect = (RectTransform)text.transform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            text.raycastTarget = false;
            return button;
        }

        private TMP_InputField MakeInput(Transform parent, string name, string placeholder, Vector2 center, Vector2 dims)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            // center is expressed as an offset from the screen center (-0.5..0.5),
            // while Unity anchors use normalized coordinates (0..1).
            Vector2 anchor = new Vector2(Mathf.Clamp01(0.5f + center.x), Mathf.Clamp01(0.5f + center.y));
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(dims.x * 1000, dims.y * 700);
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(.1f,.12f,.16f,1);
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); textGO.transform.SetParent(go.transform,false);
            var tr = (RectTransform)textGO.transform; tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one; tr.offsetMin=new Vector2(10,4); tr.offsetMax=new Vector2(-10,-4);
            var text = textGO.GetComponent<TextMeshProUGUI>(); text.fontSize=18; text.color=Color.white;
            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI)); placeholderGO.transform.SetParent(go.transform,false);
            var pr = (RectTransform)placeholderGO.transform; pr.anchorMin=Vector2.zero; pr.anchorMax=Vector2.one; pr.offsetMin=new Vector2(10,4); pr.offsetMax=new Vector2(-10,-4);
            var ph = placeholderGO.GetComponent<TextMeshProUGUI>(); ph.text=placeholder; ph.fontSize=17; ph.color=new Color(.65f,.67f,.7f);
            var input=go.GetComponent<TMP_InputField>(); input.textComponent=text; input.placeholder=ph; input.targetGraphic=go.GetComponent<Image>(); input.lineType=TMP_InputField.LineType.SingleLine;
            return input;
        }

        private void ShowTab(string tab)
        {
            selectedTab=tab;
            if (hostTab != null) hostTab.SetActive(tab=="host");
            if (joinTab != null) joinTab.SetActive(tab=="join");
            if (tab=="join") Refresh();
        }

        private void StartHost()
        {
            net.SetNick(nickInput.text);
            net.Host(nickInput.text, worldInput.text);
            if (status != null) status.text = net.Status;
        }

        private void Refresh()
        {
            if (net == null) net = TelderNetworkRuntime.Instance;
            if (net == null) return;
            // Discovery is deliberately run off the UI thread so the menu never freezes.
            System.Threading.ThreadPool.QueueUserWorkItem(_ => net.RefreshServers());
        }

        private float nextServerPaint;
        private void Update()
        {
            if (root == null || !root.activeSelf) return;
            if (status != null && net != null) status.text = net.Status;
            if (selectedTab == "join" && Time.unscaledTime >= nextServerPaint)
            {
                nextServerPaint = Time.unscaledTime + 3f;
                Refresh();
                if (net != null && serverList != null)
                {
                    foreach (var old in dynamicServerButtons) if (old != null) Destroy(old);
                    dynamicServerButtons.Clear();
                    var found = net.Servers;
                    serverList.text = found.Count == 0 ? "Серверы не найдены. Для Radmin VPN введи IP вручную." : "Нажми на сервер, чтобы подключиться:";
                    int max = Mathf.Min(4, found.Count);
                    for (int i = 0; i < max; i++)
                    {
                        var record = found[i];
                        var row = new GameObject("Server_" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                        row.transform.SetParent(serverButtons, false);
                        var rr = (RectTransform)row.transform;
                        rr.anchorMin = new Vector2(.5f,.5f); rr.anchorMax = new Vector2(.5f,.5f);
                        rr.sizeDelta = new Vector2(800,46); rr.anchoredPosition = new Vector2(0, 100 - i * 55);
                        row.GetComponent<Image>().color = new Color(.13f,.18f,.24f,.98f);
                        var button = row.GetComponent<Button>(); button.targetGraphic = row.GetComponent<Image>();
                        button.onClick.AddListener(() => { addressInput.text = record.address; net.SetNick(joinNickInput.text); net.Join(record.address, joinNickInput.text); });
                        var label = MakeText(row.transform, "Label", record.worldName + "  |  " + record.players + " игрок(ов)  |  " + record.address, 16, new Vector2(.5f,.5f), new Vector2(.95f,.8f), TextAlignmentOptions.Center);
                        var labelRect = (RectTransform)label.transform;
                        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                        labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
                        label.raycastTarget = false;
                        dynamicServerButtons.Add(row);
                    }
                }
            }
        }

        public void Open() { if (root != null) { root.SetActive(true); ShowTab("join"); } }
    }
}

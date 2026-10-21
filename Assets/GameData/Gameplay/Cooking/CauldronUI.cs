using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Inventory;
using Game.Inventory.UI;
using Game.Items;

namespace Game.Gameplay.Cooking
{
    public sealed class CauldronUI : MonoBehaviour
    {
        public CauldronState State { get; private set; }
        public bool IsOpen => State != null && panel != null && panel.activeSelf;

        private GameObject panel;
        private PlayerInventory inventory;
        private InventoryUI inventoryUI;
        private CauldronDefinition definition;
        private readonly Image[] icons = new Image[5];
        private readonly Text[] counts = new Text[5];
        private Image progress;
        private Text status;
        private static RectTransform Node(GameObject go, Transform parent, Vector2 size, Vector2 pos)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1);
            rt.pivot = new Vector2(.5f, 1);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        private static Text Label(string text, Transform parent, Vector2 size, Vector2 position, int fontSize = 13)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            Node(go, parent, size, position);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        public void Open(CauldronState state, PlayerInventory player, CauldronDefinition settings)
        {
            Close();
            inventoryUI = InventoryUI.Instance;
            if (inventoryUI == null || inventoryUI.InventoryPanelRect == null)
                return;
            Game.GameplaySystems.Furnace.FurnaceRuntime.Instance?.CloseUI();
            if (Game.Chests.UI.ChestUIController.Instance != null && Game.Chests.UI.ChestUIController.Instance.IsOpen)
                Game.Chests.UI.ChestUIController.Instance.CloseChestVisualOnly();
            State = state;
            inventory = player;
            definition = settings;
            inventoryUI.OpenInventory();
            panel = new GameObject("Cauldron", typeof(RectTransform), typeof(Image));
            Node(panel, inventoryUI.InventoryPanelRect, new Vector2(410, 170), new Vector2(0, 180));
            panel.GetComponent<Image>().color = new Color(.045f, .05f, .07f, .98f);
            Label("КОТЁЛ · 3 разных продукта + 3 чашки", panel.transform, new Vector2(400, 24), new Vector2(0, -8), 15);
            for (int i = 0; i < 5; i++)
            {
                float x = -155 + i * 76;
                var slot = new GameObject("Cauldron slot " + i, typeof(RectTransform), typeof(Image), typeof(CauldronSlotUI));
                Node(slot, panel.transform, new Vector2(54, 54), new Vector2(x, -39));
                slot.GetComponent<Image>().color = i == 4 ? new Color(.18f, .25f, .18f) : new Color(.12f, .13f, .16f);
                slot.GetComponent<CauldronSlotUI>().Owner = this;
                slot.GetComponent<CauldronSlotUI>().Index = i;
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                Node(icon, slot.transform, new Vector2(40, 40), new Vector2(0, -6));
                icons[i] = icon.GetComponent<Image>();
                icons[i].raycastTarget = false;
                icons[i].preserveAspect = true;
                counts[i] = Label("", slot.transform, new Vector2(50, 18), new Vector2(0, -36), 13);
                Label(i < 3 ? "Еда " + (i + 1) : i == 3 ? "Чашки" : "Похлёбка", panel.transform, new Vector2(74, 18), new Vector2(x, -94), 11);
            }

            var bar = new GameObject("Cooking progress", typeof(RectTransform), typeof(Image));
            Node(bar, panel.transform, new Vector2(366, 5), new Vector2(0, -116));
            bar.GetComponent<Image>().color = new Color(.15f, .17f, .19f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            Node(fill, bar.transform, new Vector2(366, 5), Vector2.zero);
            progress = fill.GetComponent<Image>();
            progress.color = new Color(.95f, .65f, .25f);
            status = Label("", panel.transform, new Vector2(396, 34), new Vector2(0, -128), 11);
            Refresh();
        }

        public void Close()
        {
            if (panel != null)
                Destroy(panel);
            panel = null;
            State = null;
            inventory = null;
            inventoryUI = null;
            CauldronRuntime.Instance?.ForceSave();
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            Vector3 p = Game.World.Voxel3D.VoxelGameplayState3D.Position(State.X, State.Y, new Vector3(State.X + .5f, State.Y + .5f, inventory.transform.position.z));
            if (inventoryUI == null || !inventoryUI.IsOpen || inventory == null || (inventory.transform.position - p).sqrMagnitude > 36)
            {
                Close();
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (!IsOpen)
                return;
            for (int i = 0; i < 5; i++)
            {
                var s = State.Slots[i];
                icons[i].sprite = s.IsEmpty ? null : ItemIconProvider.GetIcon(s.ItemId);
                icons[i].enabled = !s.IsEmpty && icons[i].sprite != null;
                counts[i].text = s.IsEmpty ? "" : s.Count.ToString();
            }

            float duration = Mathf.Max(1, definition?.CookSeconds ?? 60);
            progress.rectTransform.sizeDelta = new Vector2(366 * Mathf.Clamp01(State.Progress / duration), 5);
            progress.rectTransform.pivot = new Vector2(0, 1);
            progress.rectTransform.anchoredPosition = new Vector2(-183, 0);
            bool recipe = CauldronRules.Recipe(State, definition, out string result);
            var output = State.Slots[4];
            if (recipe && (output.IsEmpty || output.ItemId == result && output.Count <= 12))
                status.text = "Варится: " + Mathf.CeilToInt(duration - State.Progress) + " с · выход: 3 порции";
            else if (recipe)
                status.text = "Заберите готовую похлёбку — выход заполнен";
            else
                status.text = "Три разных вида еды и 3 чашки. ЛКМ — стопка, ПКМ — один; Shift — перенести.";
        }

        private void Changed(int index)
        {
            if (index < 4)
                State.Progress = 0;
            inventory.NotifyExternalChange();
            CauldronRuntime.Instance?.MarkDirty();
            Refresh();
        }

        public void Click(int index, bool right, bool shift)
        {
            if (!IsOpen || index < 0 || index > 4)
                return;
            var slot = State.Slots[index];
            var cursor = inventoryUI.CursorStack;
            if (shift)
            {
                if (slot.IsEmpty)
                    return;
                int old = slot.Count;
                inventory.TryAddStack(slot);
                if (old != slot.Count)
                    Changed(index);
                return;
            }

            if (cursor.IsEmpty)
            {
                if (slot.IsEmpty)
                    return;
                int take = right ? (slot.Count + 1) / 2 : slot.Count;
                cursor.Set(slot);
                cursor.Count = take;
                slot.Count -= take;
                Changed(index);
                return;
            }

            if (index == 4)
            {
                if (slot.IsEmpty || cursor.ItemId != slot.ItemId || cursor.HasInstanceData || slot.HasInstanceData)
                    return;
                int moved = Mathf.Min(right ? 1 : slot.Count, ItemRegistry.GetMaxStack(cursor.ItemId) - cursor.Count);
                if (moved > 0)
                {
                    cursor.Count += moved;
                    slot.Count -= moved;
                    Changed(index);
                }

                return;
            }

            if (!CauldronRules.Accepts(State, index, cursor.ItemId, definition))
                return;
            if (slot.IsEmpty)
            {
                int moved = right ? 1 : cursor.Count;
                slot.Set(cursor);
                slot.Count = moved;
                cursor.Count -= moved;
                Changed(index);
                return;
            }

            if (slot.ItemId == cursor.ItemId && !slot.HasInstanceData && !cursor.HasInstanceData)
            {
                int moved = Mathf.Min(right ? 1 : cursor.Count, ItemRegistry.GetMaxStack(slot.ItemId) - slot.Count);
                if (moved > 0)
                {
                    slot.Count += moved;
                    cursor.Count -= moved;
                    Changed(index);
                }

                return;
            }

            if (!right)
            {
                var previous = slot.Clone();
                slot.Set(cursor);
                cursor.Set(previous);
                Changed(index);
            }
        }

        public bool QuickMoveFromPlayer(int slotIndex)
        {
            if (!IsOpen)
                return false;
            var source = inventory.GetSlot(slotIndex);
            if (source == null || source.IsEmpty)
                return false;
            int target = -1;
            if (CauldronRules.Accepts(State, 3, source.ItemId, definition))
                target = 3;
            else
                for (int i = 0; i < 3; i++)
                    if (CauldronRules.Accepts(State, i, source.ItemId, definition) && (State.Slots[i].IsEmpty || State.Slots[i].ItemId == source.ItemId))
                    {
                        target = i;
                        break;
                    }

            if (target < 0)
                return false;
            var destination = State.Slots[target];
            int max = ItemRegistry.GetMaxStack(source.ItemId);
            int moved = Mathf.Min(source.Count, max - (destination.IsEmpty ? 0 : destination.Count));
            if (moved <= 0)
                return false;
            if (destination.IsEmpty)
            {
                destination.Set(source);
                destination.Count = moved;
            }
            else
                destination.Count += moved;
            source.Count -= moved;
            Changed(target);
            return true;
        }

        private void OnDisable()
        {
            Close();
        }
    }
}

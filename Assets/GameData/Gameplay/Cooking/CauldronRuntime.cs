using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Content;
using Game.Blocks;
using Game.Inventory;
using Game.Inventory.UI;
using Game.InputSupport;
using Game.GameplaySystems;
using Game.GameplaySystems.Multiblock;
using Game.World;
using Game.World.Furniture;
using Game.World.Voxel3D;

namespace Game.Gameplay.Cooking
{
    [Serializable]
    public sealed class CauldronSave
    {
        public List<CauldronState> Entries = new List<CauldronState>();
    }

    [DefaultExecutionOrder(-300)]
    public sealed class CauldronRuntime : MonoBehaviour
    {
        public static CauldronRuntime Instance { get; private set; }

        private static int interactionFrame = -1;
        public static bool InteractedThisFrame => interactionFrame == Time.frameCount;

        private CauldronSave data = new CauldronSave();
        private readonly Dictionary<string, CauldronState> lookup = new Dictionary<string, CauldronState>();
        private string key, path;
        private float saveTimer;
        private bool dirty;
        private CauldronUI ui;
        private PlayerInventory player;
        private float nextSearch;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null)
                new GameObject("Cauldron cooking").AddComponent<CauldronRuntime>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            FurnitureLayerManager.FurnitureRemoved += Removed;
        }

        public static bool IsCauldron(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            var cid = ContentID.Parse(id);
            if (!BlockRegistry.Contains(cid))
                return false;
            var def = BlockRegistry.Get(cid);
            if (def.Cauldron != null || string.Equals(def.Behavior, "Cauldron", StringComparison.OrdinalIgnoreCase) || def.MultiBlock != null && string.Equals(def.MultiBlock.Kind, "cauldron", StringComparison.OrdinalIgnoreCase))
                return true;
            if (def.Tags != null)
                foreach (var t in def.Tags)
                    if (string.Equals(t, "cauldron", StringComparison.OrdinalIgnoreCase))
                        return true;
            return false;
        }

        private static string CellKey(int x, int y) => x + "," + y;
        private void Bind()
        {
            string now = GameplayContextKey.Get();
            if (now == key)
                return;
            ForceSave();
            ui?.Close();
            key = now;
            path = Game.Save.SaveGameRuntime.HasActiveSave ? Path.Combine(Application.persistentDataPath, "TelderGameplaySystems", "cauldrons_" + key + ".json") : null;
            data = GameplaySaveIO.Load<CauldronSave>(path);
            if (data.Entries == null)
                data.Entries = new List<CauldronState>();
            lookup.Clear();
            foreach (var state in data.Entries)
                if (state != null)
                {
                    state.Normalize();
                    lookup[CellKey(state.X, state.Y)] = state;
                }

            dirty = false;
            saveTimer = 0;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public void ForceSave()
        {
            if (dirty && path != null && GameplaySaveIO.Save(path, data))
            {
                dirty = false;
                saveTimer = 0;
            }
        }

        public bool TryResolve(int x, int y, out int ax, out int ay, out BlockDefinition definition)
        {
            ax = x;
            ay = y;
            definition = null;
            string id = null;
            if (VoxelGameplayState3D.TryResolve(x, y, out var p, out _))
            {
                var rt = VoxelWorld3DRuntime.Instance;
                id = VoxelBlockCatalog.Get(rt.World.GetBlock(p.x, p.y, p.z))?.ID;
            }
            else
            {
                var multi = MultiBlockStore.FindCell(x, y);
                if (multi != null)
                {
                    ax = multi.X;
                    ay = multi.Y;
                    id = multi.BlockId;
                }

                if (string.IsNullOrWhiteSpace(id))
                    id = FurnitureLayerManager.Instance?.GetFurniture(ax, ay);
                if (string.IsNullOrWhiteSpace(id))
                {
                    var world = WorldManager.Instance?.GetWorld();
                    if (world != null)
                    {
                        ushort n = world.GetBlock(ax, ay);
                        if (n != 0)
                            id = world.GetBlockDefinition(n)?.ID;
                    }
                }
            }

            if (!IsCauldron(id))
                return false;
            definition = BlockRegistry.Get(ContentID.Parse(id));
            return true;
        }

        public bool TryOpenAtCell(int x, int y)
        {
            Bind();
            FindPlayer();
            if (player == null || !TryResolve(x, y, out int ax, out int ay, out var def))
                return false;
            Vector3 center = VoxelGameplayState3D.Position(ax, ay, new Vector3(ax + .5f, ay + .5f, player.transform.position.z));
            if ((player.transform.position - center).sqrMagnitude > 36)
                return false;
            if (!lookup.TryGetValue(CellKey(ax, ay), out var state))
            {
                state = new CauldronState
                {
                    X = ax,
                    Y = ay
                };
                lookup[CellKey(ax, ay)] = state;
                data.Entries.Add(state);
                dirty = true;
            }

            state.BlockId = def.ID;
            if (ui == null)
                ui = gameObject.AddComponent<CauldronUI>();
            interactionFrame = Time.frameCount;
            ui.Open(state, player, def.Cauldron ?? new CauldronDefinition());
            return ui.IsOpen;
        }

        private void FindPlayer()
        {
            var voxel = VoxelWorld3DRuntime.Instance;
            if (voxel != null && voxel.Inventory != null)
            {
                player = voxel.Inventory;
                return;
            }

            var inventoryUI = InventoryUI.Instance;
            if (inventoryUI != null && inventoryUI.PlayerInventory != null)
            {
                player = inventoryUI.PlayerInventory;
                return;
            }

            if (player != null && player.isActiveAndEnabled)
                return;
            if (Time.unscaledTime < nextSearch)
                return;
            nextSearch = Time.unscaledTime + .5f;
            player = null;
            foreach (var candidate in FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.GetComponentInParent<Game.Entities.EntityActor>() == null)
                {
                    player = candidate;
                    break;
                }
        }

        private void Update()
        {
            Bind();
            var voxel = VoxelWorld3DRuntime.Instance;
            var wm = WorldManager.Instance;
            if (!(voxel != null && voxel.isActiveAndEnabled && voxel.Ready) && (wm == null || !wm.isActiveAndEnabled || !wm.IsReady))
                return;
            FindPlayer();
            for (int i = 0; i < data.Entries.Count; i++)
            {
                var state = data.Entries[i];
                if (state == null)
                    continue;
                BlockDefinition def = null;
                if (!TryResolve(state.X, state.Y, out _, out _, out def))
                {
                    var id = ContentID.Parse(state.BlockId ?? "game:cauldron");
                    if (BlockRegistry.Contains(id))
                        def = BlockRegistry.Get(id);
                }

                if (def != null)
                {
                    float old = state.Progress;
                    bool cooked = CauldronRules.Tick(state, def.Cauldron, Time.deltaTime);
                    if (cooked || state.Progress != old)
                        dirty = true;
                }
            }

            saveTimer += Time.deltaTime;
            if (saveTimer >= 5)
            {
                ForceSave();
                saveTimer = 0;
            }

            if (ui != null && ui.IsOpen || InventoryUI.Instance != null && InventoryUI.Instance.IsOpen || voxel != null)
                return;
            if (!GameInput.Pressed(GameAction.Use) && !GameInput.Pressed(GameAction.Interact))
                return;
            var camera = Camera.main;
            if (camera == null || !camera.orthographic)
                return;
            var point = camera.ScreenToWorldPoint(GameInput.PointerPosition);
            TryOpenAtCell(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
        }

        public bool QuickMoveFromPlayer(int slot) => ui != null && ui.IsOpen && ui.QuickMoveFromPlayer(slot);
        private void Removed(int x, int y, string id)
        {
            if (IsCauldron(id))
                RemoveAt(x, y);
        }

        public void NotifyRemoved(int x, int y)
        {
            RemoveAt(x, y);
        }

        private void RemoveAt(int x, int y)
        {
            Bind();
            if (!lookup.TryGetValue(CellKey(x, y), out var state))
                return;
            if (ui != null && ui.State == state)
                ui.Close();
            lookup.Remove(CellKey(x, y));
            data.Entries.Remove(state);
            Vector3 position = VoxelGameplayState3D.Position(x, y, new Vector3(x + .5f, y + .6f, 0));
            foreach (var stack in state.Slots)
                if (!stack.IsEmpty)
                {
                    if (VoxelWorld3DRuntime.Instance != null)
                        VoxelItemDrop3D.Spawn(stack.ItemId, stack.Count, position);
                    else
                        Game.World.Items.ItemDropSpawner.EnsureInstance().SpawnFromBlock(stack.ItemId, stack.Count, position);
                }

            dirty = true;
            ForceSave();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                ForceSave();
        }

        private void OnApplicationQuit()
        {
            ForceSave();
        }

        private void OnDestroy()
        {
            ForceSave();
            FurnitureLayerManager.FurnitureRemoved -= Removed;
            if (Instance == this)
                Instance = null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Inventory;
using Game.Inventory.UI;
using Game.Blocks;
using Game.Content;
using Game.World;
using Game.World.Furniture;
using Game.World.Items;
using Game.World.Rendering;
using Game.GameplaySystems;

namespace Game.GameplaySystems.Furnace
{
    [Serializable]
    public sealed class FurnaceState
    {
        public int X;
        public int Y;
        public string InputId;
        public int InputCount;
        public string FuelId;
        public int FuelCount;
        public string OutputId;
        public int OutputCount;
        public float BurnRemaining;
        public float BurnTotal;
        public float SmeltProgress;

        [NonSerialized]
        public bool VisualStateInitialized;

        [NonSerialized]
        public bool VisualBurning;

        [NonSerialized]
        public bool VisualLayerInitialized;

        [NonSerialized]
        public BlockVisualLayer VisualLayer;
    }

    [Serializable]
    internal sealed class FurnaceSaveFile
    {
        public List<FurnaceState> Entries = new List<FurnaceState>();
    }

    [DefaultExecutionOrder(-6000)]
    public sealed class FurnaceRuntime : MonoBehaviour
    {
        public static FurnaceRuntime Instance { get; private set; }
        public bool IsUIOpen => ui != null && ui.IsOpen;

        private const float SaveInterval = 5f;
        private const float ContextCheckInterval = 0.5f;

        private string key;
        private FurnaceSaveFile data = new FurnaceSaveFile();
        private float saveTimer;
        private float nextContextCheck;
        private bool dirty;
        private PlayerInventory playerInventory;
        private Camera cam;
        private FurnaceUI ui;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            FurnitureLayerManager.FurnitureRemoved += OnFurnitureRemoved;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureContext(true);
        }

        private void OnDestroy()
        {
            FurnitureLayerManager.FurnitureRemoved -= OnFurnitureRemoved;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SaveIfDirty();
            if (Instance == this)
                Instance = null;
        }

        private void OnApplicationQuit()
        {
            SaveIfDirty();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            playerInventory = null;
            cam = null;
            if (ui != null && ui.IsOpen)
                ui.Close();
            EnsureContext(true);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextContextCheck)
            {
                nextContextCheck = Time.unscaledTime + ContextCheckInterval;
                EnsureContext(false);
            }

            if (playerInventory == null)
                playerInventory = FindFirstObjectByType<PlayerInventory>();
            if (cam == null)
                cam = Camera.main;

            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                for (int i = 0; i < data.Entries.Count; i++)
                    Tick(data.Entries[i], dt);
            }

            if (dirty)
            {
                saveTimer += Time.unscaledDeltaTime;
                if (saveTimer >= SaveInterval)
                    SaveIfDirty();
            }

            if (
                playerInventory != null &&
                cam != null &&
                Input.GetMouseButtonDown(1) &&
                (ui == null || !ui.IsOpen) &&
                (InventoryUI.Instance == null || !InventoryUI.Instance.IsOpen)
            )
            {
                TryOpenAtMouse();
            }
        }

        /// <summary>
        /// Opens a furnace under the mouse cursor. Furnaces are supported both
        /// on the Furniture layer and as normal foreground blocks.
        /// </summary>
        public bool TryOpenAtMouse()
        {
            if (playerInventory == null || cam == null || IsUIOpen)
                return false;

            Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.FloorToInt(wp.x);
            int y = Mathf.FloorToInt(wp.y);

            return TryOpenAtCell(x, y);
        }

        /// <summary>
        /// Opens the furnace occupying a world cell. Returns true only when a
        /// real furnace was found and its UI was opened.
        /// </summary>
        public bool TryOpenAtCell(int x, int y)
        {
            if (playerInventory == null)
                playerInventory = FindFirstObjectByType<PlayerInventory>();

            if (playerInventory == null || IsUIOpen)
                return false;

            Vector2 target = new Vector2(x + 0.5f, y + 0.5f);
            if (((Vector2)playerInventory.transform.position - target).sqrMagnitude > 36f)
                return false;

            if (!TryResolveFurnaceAt(x, y, out _))
                return false;

            FurnaceState state = GetOrCreate(x, y);
            if (ui == null)
                ui = gameObject.AddComponent<FurnaceUI>();

            ui.Open(state, playerInventory);
            return true;
        }

        /// <summary>
        /// Shift-click bridge used by InventoryUI while the furnace companion
        /// panel is open. Returns false when the item is not valid for either
        /// furnace input slot so normal inventory quick-move can continue.
        /// </summary>
        public bool QuickMoveFromPlayer(int playerSlotIndex)
        {
            return
                ui != null
                &&
                ui.IsOpen
                &&
                ui.QuickMoveFromPlayer(playerSlotIndex);
        }

        /// <summary>
        /// Closes only the furnace companion panel. InventoryUI owns the actual
        /// inventory open/close state and E key.
        /// </summary>
        public void CloseUI()
        {
            if (ui != null && ui.IsOpen)
                ui.Close();
        }

        private bool TryResolveFurnaceAt(int x, int y, out string blockId)
        {
            blockId = null;

            // First check the dedicated Furniture layer.
            FurnitureLayerManager furniture = FurnitureLayerManager.EnsureInstance();
            if (furniture != null)
            {
                string furnitureId = furniture.GetFurniture(x, y);
                if (FurnaceMetadataRegistry.IsFurnace(furnitureId))
                {
                    blockId = furnitureId;
                    return true;
                }
            }

            // Furnaces can also be placed by the normal BlockInteraction system.
            // The previous implementation ignored these blocks completely, which
            // is why RMB did nothing for a normally placed furnace.
            WorldManager manager = WorldManager.Instance;
            Game.World.World world = manager != null ? manager.GetWorld() : null;
            if (world == null)
                return false;

            ushort runtimeId = world.GetBlock(x, y);
            if (runtimeId == 0)
                return false;

            try
            {
                ContentID contentId = BlockIDRegistry.GetContentID(runtimeId);
                string id = contentId.ToString();
                if (!FurnaceMetadataRegistry.IsFurnace(id))
                    return false;

                blockId = id;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes persistent furnace state when a normal foreground furnace is
        /// broken, and drops the items that were stored inside it. Furniture-layer
        /// furnaces are handled by FurnitureRemoved.
        /// </summary>
        public void NotifyForegroundBlockRemoved(int x, int y, ushort oldRuntimeId)
        {
            if (oldRuntimeId == 0)
                return;

            string id;
            try
            {
                id = BlockIDRegistry.GetContentID(oldRuntimeId).ToString();
            }
            catch
            {
                return;
            }

            if (!FurnaceMetadataRegistry.IsFurnace(id))
                return;

            RemoveStateAndDropContents(x, y);
        }

        private void RemoveStateAndDropContents(int x, int y)
        {
            EnsureContext(false);

            for (int i = data.Entries.Count - 1; i >= 0; i--)
            {
                FurnaceState state = data.Entries[i];
                if (state.X != x || state.Y != y)
                    continue;

                DropContents(state);
                data.Entries.RemoveAt(i);
                MarkDirty();
            }

            BlockVisualStateRuntime.ClearState(
                x,
                y,
                BlockVisualLayer.Foreground
            );
            BlockVisualStateRuntime.ClearState(
                x,
                y,
                BlockVisualLayer.Furniture
            );

            SaveIfDirty();
        }

        public FurnaceState GetOrCreate(int x, int y)
        {
            EnsureContext(false);
            for (int i = 0; i < data.Entries.Count; i++)
            {
                if (data.Entries[i].X == x && data.Entries[i].Y == y)
                    return data.Entries[i];
            }

            FurnaceState state = new FurnaceState { X = x, Y = y };
            data.Entries.Add(state);
            MarkDirty();
            SaveIfDirty();
            return state;
        }

        private void Tick(FurnaceState state, float dt)
        {
            if (state == null || dt <= 0f)
                return;

            bool hasRecipe = FurnaceMetadataRegistry.TryRecipe(
                state.InputId,
                out string outputId,
                out int outputCount,
                out float smeltTime
            ) && state.InputCount > 0;

            int maxOutputStack = hasRecipe ? FurnaceMetadataRegistry.GetMaxStack(outputId) : 0;
            bool outputCompatible = hasRecipe &&
                (string.IsNullOrWhiteSpace(state.OutputId) || string.Equals(state.OutputId, outputId, StringComparison.OrdinalIgnoreCase)) &&
                state.OutputCount + outputCount <= maxOutputStack;

            if (!hasRecipe || !outputCompatible)
            {
                if (state.SmeltProgress != 0f)
                {
                    state.SmeltProgress = 0f;
                    MarkDirty();
                }

                // Once a fuel item is ignited it keeps burning, matching common furnace behaviour.
                if (state.BurnRemaining > 0f)
                {
                    state.BurnRemaining = Mathf.Max(0f, state.BurnRemaining - dt);
                    MarkDirty();
                }

                SyncVisualState(state);
                return;
            }

            if (state.BurnRemaining <= 0f)
            {
                if (state.FuelCount <= 0 || !FurnaceMetadataRegistry.TryFuel(state.FuelId, out float burnSeconds))
                {
                    SyncVisualState(state);
                    return;
                }

                state.FuelCount--;
                if (state.FuelCount <= 0)
                {
                    state.FuelCount = 0;
                    state.FuelId = null;
                }

                state.BurnRemaining = burnSeconds;
                state.BurnTotal = burnSeconds;
                MarkDirty();
            }

            float used = Mathf.Min(dt, state.BurnRemaining);
            state.BurnRemaining -= used;
            state.SmeltProgress += used;
            MarkDirty();

            while (state.SmeltProgress >= smeltTime && state.InputCount > 0)
            {
                // Re-check capacity for every produced item, especially when recipe count > 1.
                if (state.OutputCount + outputCount > maxOutputStack)
                    break;

                state.SmeltProgress -= smeltTime;
                state.InputCount--;
                if (state.InputCount <= 0)
                {
                    state.InputCount = 0;
                    state.InputId = null;
                }

                if (string.IsNullOrWhiteSpace(state.OutputId))
                    state.OutputId = outputId;
                state.OutputCount += outputCount;
            }

            SyncVisualState(state);
        }

        private void SyncVisualState(FurnaceState state)
        {
            if (state == null)
                return;

            bool burning = state.BurnRemaining > 0f;
            string visualState = burning ? "burning" : "idle";

            if (!TryResolveVisualLayer(state, out BlockVisualLayer targetLayer))
            {
                // The chunk may not be loaded yet. Do not guess a layer and do
                // not mark the visual as initialized; a later tick will retry.
                return;
            }

            string currentVisualState =
                BlockVisualStateRuntime.GetState(
                    state.X,
                    state.Y,
                    targetLayer
                );

            if (
                state.VisualStateInitialized &&
                state.VisualBurning == burning &&
                state.VisualLayerInitialized &&
                state.VisualLayer == targetLayer &&
                string.Equals(
                    currentVisualState,
                    visualState,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return;
            }

            if (state.VisualLayerInitialized && state.VisualLayer != targetLayer)
            {
                BlockVisualStateRuntime.ClearState(
                    state.X,
                    state.Y,
                    state.VisualLayer
                );
            }

            BlockVisualLayer otherLayer =
                targetLayer == BlockVisualLayer.Furniture
                    ? BlockVisualLayer.Foreground
                    : BlockVisualLayer.Furniture;

            BlockVisualStateRuntime.ClearState(
                state.X,
                state.Y,
                otherLayer
            );

            BlockVisualStateRuntime.SetState(
                state.X,
                state.Y,
                targetLayer,
                visualState
            );

            state.VisualStateInitialized = true;
            state.VisualBurning = burning;
            state.VisualLayerInitialized = true;
            state.VisualLayer = targetLayer;
        }

        private bool TryResolveVisualLayer(
            FurnaceState state,
            out BlockVisualLayer layer)
        {
            layer = BlockVisualLayer.Foreground;

            if (state == null)
                return false;

            FurnitureLayerManager furniture = FurnitureLayerManager.Instance;
            if (
                furniture != null &&
                FurnaceMetadataRegistry.IsFurnace(
                    furniture.GetFurniture(state.X, state.Y)
                )
            )
            {
                layer = BlockVisualLayer.Furniture;
                return true;
            }

            WorldManager manager = WorldManager.Instance;
            Game.World.World world = manager != null ? manager.GetWorld() : null;
            if (world == null)
                return false;

            int chunkX = Mathf.FloorToInt(state.X / (float)Chunk.SizeX);
            int chunkY = Mathf.FloorToInt(state.Y / (float)Chunk.SizeY);
            if (world.GetChunk(chunkX, chunkY) == null)
                return false;

            ushort runtimeId = world.GetBlock(state.X, state.Y);
            if (runtimeId == 0)
                return false;

            try
            {
                string blockId = BlockIDRegistry.GetContentID(runtimeId).ToString();
                if (!FurnaceMetadataRegistry.IsFurnace(blockId))
                    return false;
            }
            catch
            {
                return false;
            }

            layer = BlockVisualLayer.Foreground;
            return true;
        }


        private void OnFurnitureRemoved(int x, int y, string id)
        {
            if (!FurnaceMetadataRegistry.IsFurnace(id))
                return;

            RemoveStateAndDropContents(x, y);
        }

        private void DropContents(FurnaceState state)
        {
            ItemDropSpawner spawner = ItemDropSpawner.Instance;
            if (spawner == null || state == null)
                return;

            Vector2 position = new Vector2(state.X + 0.5f, state.Y + 0.5f);
            if (state.InputCount > 0 && !string.IsNullOrWhiteSpace(state.InputId))
                spawner.SpawnFromPlayer(state.InputId, state.InputCount, position);
            if (state.FuelCount > 0 && !string.IsNullOrWhiteSpace(state.FuelId))
                spawner.SpawnFromPlayer(state.FuelId, state.FuelCount, position);
            if (state.OutputCount > 0 && !string.IsNullOrWhiteSpace(state.OutputId))
                spawner.SpawnFromPlayer(state.OutputId, state.OutputCount, position);
        }

        private void EnsureContext(bool force)
        {
            string now = GameplayContextKey.Get();
            if (!force && now == key)
                return;

            if (!string.IsNullOrWhiteSpace(key))
                SaveIfDirty();

            key = now;
            BlockVisualStateRuntime.SetContext(now);

            data = GameplaySaveIO.Load<FurnaceSaveFile>(PathNow());
            if (data.Entries == null)
                data.Entries = new List<FurnaceState>();

            for (int i = 0; i < data.Entries.Count; i++)
            {
                FurnaceState state = data.Entries[i];
                if (state == null)
                    continue;

                state.VisualStateInitialized = false;
                state.VisualLayerInitialized = false;
                SyncVisualState(state);
            }

            dirty = false;
            saveTimer = 0f;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public void ForceSave()
        {
            SaveIfDirty();
        }

        private void SaveIfDirty()
        {
            if (!dirty || string.IsNullOrWhiteSpace(key))
                return;

            if (GameplaySaveIO.Save(PathNow(), data, true))
            {
                dirty = false;
                saveTimer = 0f;
            }
        }

        private string Folder()
        {
            return Path.Combine(Application.persistentDataPath, "TelderGameplaySystems");
        }

        private string PathNow()
        {
            return Path.Combine(Folder(), "furnaces_" + key + ".json");
        }
    }
}

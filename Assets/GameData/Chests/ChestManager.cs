
using System;
using System.Collections.Generic;
using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Save;
using Game.World;
using Game.World.Dimensions;
using Game.World.Items;
using Game.World.Furniture;

namespace Game.Chests
{
    public class ChestManager : MonoBehaviour
    {
        public static ChestManager Instance { get; private set; }

        [Header("Chest Tag")]
        [SerializeField] private string chestTag = "chest";

        [Header("References")]
        [SerializeField] private PlayerInventory playerInventory;

        private readonly Dictionary<long, ChestInventoryRuntime>
            runtimeChests =
                new Dictionary<long, ChestInventoryRuntime>();

        public PlayerInventory PlayerInventory => playerInventory;

        private void Awake()
        {
            Instance = this;

            if (playerInventory == null)
                playerInventory = GetComponent<PlayerInventory>();
        }

        private void OnEnable()
        {
            SaveGameRuntime.ForegroundCleared += HandleForegroundCleared;
            FurnitureLayerManager.FurnitureRemoved += HandleFurnitureRemoved;
        }

        private void OnDisable()
        {
            SaveGameRuntime.ForegroundCleared -= HandleForegroundCleared;
            FurnitureLayerManager.FurnitureRemoved -= HandleFurnitureRemoved;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool IsChestBlock(ushort blockId)
        {
            return TryGetChestDefinition(
                blockId,
                out BlockDefinition _
            );
        }

        public bool IsChestAt(int worldX, int worldY)
        {
            WorldManager manager = WorldManager.Instance;

            if (manager == null || manager.GetWorld() == null)
                return false;

            if (FurnitureLayerManager.Instance != null &&
                FurnitureLayerManager.Instance.TryGetDefinition(worldX, worldY, out BlockDefinition furnitureDef) &&
                HasChestTag(furnitureDef))
                return true;

            ushort blockId =
                manager.GetWorld().GetBlock(worldX, worldY);

            return IsChestBlock(blockId);
        }

        public bool IsClosedChestAt(int worldX, int worldY)
        {
            WorldManager manager = WorldManager.Instance;

            if (manager == null || manager.GetWorld() == null)
                return false;

            if (FurnitureLayerManager.Instance != null &&
                FurnitureLayerManager.Instance.TryGetDefinition(worldX, worldY, out BlockDefinition furnitureDef) &&
                HasChestTag(furnitureDef))
                return furnitureDef.Closed;

            ushort blockId =
                manager.GetWorld().GetBlock(worldX, worldY);

            if (!TryGetChestDefinition(
                blockId,
                out BlockDefinition definition))
                return false;

            return definition.Closed;
        }

        public bool TryGetChestDefinition(
            ushort blockId,
            out BlockDefinition definition
        )
        {
            definition = null;

            if (blockId == 0)
                return false;

            try
            {
                ContentID contentId =
                    BlockIDRegistry.GetContentID(blockId);

                if (!BlockRegistry.Contains(contentId))
                    return false;

                definition = BlockRegistry.Get(contentId);

                if (definition == null || definition.Tags == null)
                    return false;

                for (int i = 0; i < definition.Tags.Count; i++)
                {
                    if (string.Equals(
                        definition.Tags[i],
                        chestTag,
                        StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                definition = null;
                return false;
            }
            catch
            {
                definition = null;
                return false;
            }
        }

        private bool HasChestTag(BlockDefinition definition)
        {
            if (definition == null || definition.Tags == null) return false;
            for (int i=0;i<definition.Tags.Count;i++)
                if (string.Equals(definition.Tags[i], chestTag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void HandleFurnitureRemoved(int worldX, int worldY, string blockId)
        {
            try
            {
                ContentID id = ContentID.Parse(blockId);
                if (!BlockRegistry.Contains(id)) return;
                if (!HasChestTag(BlockRegistry.Get(id))) return;
            }
            catch { return; }
            HandleForegroundCleared(worldX, worldY);
        }

        public ChestInventoryRuntime GetChest(
            int worldX,
            int worldY
        )
        {
            long key = Pack(worldX, worldY);

            if (runtimeChests.TryGetValue(
                key,
                out ChestInventoryRuntime existing))
                return existing;

            ChestSaveData saved = null;

            bool hasSaved =
                SaveGameRuntime.TryGetChestData(
                    worldX,
                    worldY,
                    out saved
                );

            bool generated = false;

            if (!hasSaved)
            {
                string dimensionName =
                    DimensionTravelRuntime.Current != null
                        ? DimensionTravelRuntime.Current.Name
                        : string.Empty;

                generated =
                    GeneratedChestLootRuntime.TryBuildInitialData(
                        dimensionName,
                        worldX,
                        worldY,
                        out saved
                    );
            }

            ChestInventoryRuntime chest =
                new ChestInventoryRuntime(
                    worldX,
                    worldY,
                    saved
                );

            chest.Changed += () => PersistChest(chest);

            runtimeChests[key] = chest;

            // Persist even an empty generated roll.
            if (generated)
                PersistChest(chest);

            return chest;
        }

        public void PersistChest(
            ChestInventoryRuntime chest
        )
        {
            if (chest == null)
                return;

            chest.BuildSaveArrays(
                out string[] itemIds,
                out int[] counts
            );

            SaveGameRuntime.StoreChestData(
                chest.WorldX,
                chest.WorldY,
                itemIds,
                counts
            );
        }

        private void HandleForegroundCleared(
            int worldX,
            int worldY
        )
        {
            long key = Pack(worldX, worldY);

            string dimensionName =
                DimensionTravelRuntime.Current != null
                    ? DimensionTravelRuntime.Current.Name
                    : string.Empty;

            bool hasRuntime = runtimeChests.ContainsKey(key);
            bool hasSaved =
                SaveGameRuntime.HasChestData(worldX, worldY);
            bool hasGenerated =
                GeneratedChestLootRuntime.Has(
                    dimensionName,
                    worldX,
                    worldY
                );

            if (!hasRuntime && !hasSaved && !hasGenerated)
                return;

            ChestInventoryRuntime chest =
                GetChest(worldX, worldY);

            DropAllContents(chest);

            runtimeChests.Remove(key);
            SaveGameRuntime.RemoveChestData(worldX, worldY);
        }

        private void DropAllContents(
            ChestInventoryRuntime chest
        )
        {
            if (chest == null)
                return;

            Vector2 dropPosition =
                new Vector2(
                    chest.WorldX + 0.5f,
                    chest.WorldY + 0.75f
                );

            for (int i = 0;
                 i < ChestInventoryRuntime.SlotCount;
                 i++)
            {
                ItemStack stack = chest.GetSlot(i);

                if (stack == null || stack.IsEmpty)
                    continue;

                if (ItemDropSpawner.Instance == null)
                {
                    Debug.LogError(
                        "CHEST: ItemDropSpawner.Instance is null."
                    );
                    continue;
                }

                ItemDropSpawner.Instance.SpawnFromBlock(
                    stack.ItemId,
                    stack.Count,
                    dropPosition
                );
            }
        }

        private static long Pack(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }
    }
}

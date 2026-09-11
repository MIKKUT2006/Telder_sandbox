using System.Collections.Generic;

using UnityEngine;

using Game.Content;
using Game.Inventory;
using Game.Save;
using Game.World;
using Game.World.Items;


namespace Game.Chests
{

    public class ChestManager :
        MonoBehaviour
    {

        public static ChestManager Instance
        {
            get;
            private set;
        }


        [Header("Chest Block")]

        [SerializeField]
        private string chestBlockContentId =
            "game:chest";


        [Header("References")]

        [SerializeField]
        private PlayerInventory playerInventory;


        private readonly Dictionary<
            long,
            ChestInventoryRuntime
        > runtimeChests =
            new Dictionary<
                long,
                ChestInventoryRuntime
            >();


        public string ChestBlockContentId =>
            chestBlockContentId;


        public PlayerInventory PlayerInventory =>
            playerInventory;


        private void Awake()
        {

            Instance =
                this;


            if (
                playerInventory == null
            )
            {

                playerInventory =
                    GetComponent<
                        PlayerInventory
                    >();

            }


        }


        private void OnEnable()
        {

            SaveGameRuntime.ForegroundCleared +=
                HandleForegroundCleared;

        }


        private void OnDisable()
        {

            SaveGameRuntime.ForegroundCleared -=
                HandleForegroundCleared;

        }


        private void OnDestroy()
        {

            if (
                Instance ==
                this
            )
            {

                Instance =
                    null;

            }

        }


        // =====================================================
        // BLOCK CHECK
        // =====================================================

        public bool IsChestBlock(
            ushort blockId
        )
        {

            if (
                blockId == 0
            )
            {

                return false;

            }


            try
            {

                ContentID contentId =
                    BlockIDRegistry.GetContentID(
                        blockId
                    );


                return
                    string.Equals(
                        contentId.ToString(),
                        chestBlockContentId,
                        System.StringComparison.OrdinalIgnoreCase
                    );

            }
            catch
            {

                return false;

            }

        }


        public bool IsChestAt(
            int worldX,
            int worldY
        )
        {

            WorldManager manager =
                WorldManager.Instance;


            if (
                manager == null
                ||
                manager.GetWorld() == null
            )
            {

                return false;

            }


            ushort blockId =
                manager.GetWorld()
                    .GetBlock(
                        worldX,
                        worldY
                    );


            return
                IsChestBlock(
                    blockId
                );

        }


        // =====================================================
        // GET RUNTIME CHEST
        // =====================================================

        public ChestInventoryRuntime GetChest(
            int worldX,
            int worldY
        )
        {

            long key =
                Pack(
                    worldX,
                    worldY
                );


            if (
                runtimeChests.TryGetValue(
                    key,
                    out ChestInventoryRuntime existing
                )
            )
            {

                return existing;

            }


            ChestSaveData saved =
                null;


            SaveGameRuntime.TryGetChestData(
                worldX,
                worldY,
                out saved
            );


            ChestInventoryRuntime chest =
                new ChestInventoryRuntime(
                    worldX,
                    worldY,
                    saved
                );


            chest.Changed +=
                () =>
                {
                    PersistChest(
                        chest
                    );
                };


            runtimeChests[
                key
            ] =
                chest;


            return chest;

        }


        // =====================================================
        // PERSIST
        // =====================================================

        public void PersistChest(
            ChestInventoryRuntime chest
        )
        {

            if (
                chest == null
            )
            {

                return;

            }


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


        // =====================================================
        // BREAK
        // =====================================================

        private void HandleForegroundCleared(
            int worldX,
            int worldY
        )
        {

            long key =
                Pack(
                    worldX,
                    worldY
                );


            bool hasRuntime =
                runtimeChests.ContainsKey(
                    key
                );


            bool hasSaved =
                SaveGameRuntime.HasChestData(
                    worldX,
                    worldY
                );


            // No chest inventory at this coordinate.
            if (
                !hasRuntime
                &&
                !hasSaved
            )
            {

                return;

            }


            ChestInventoryRuntime chest =
                GetChest(
                    worldX,
                    worldY
                );


            DropAllContents(
                chest
            );


            runtimeChests.Remove(
                key
            );


            SaveGameRuntime.RemoveChestData(
                worldX,
                worldY
            );

        }


        private void DropAllContents(
            ChestInventoryRuntime chest
        )
        {

            if (
                chest == null
            )
            {

                return;

            }


            Vector2 dropPosition =
                new Vector2(
                    chest.WorldX +
                    0.5f,
                    chest.WorldY +
                    0.75f
                );


            for (
                int i = 0;
                i < ChestInventoryRuntime.SlotCount;
                i++
            )
            {

                ItemStack stack =
                    chest.GetSlot(
                        i
                    );


                if (
                    stack == null
                    ||
                    stack.IsEmpty
                )
                {

                    continue;

                }


                if (
                    ItemDropSpawner.Instance ==
                    null
                )
                {

                    Debug.LogError(
                        "CHEST: ItemDropSpawner.Instance is null. " +
                        "Chest content cannot be dropped."
                    );


                    continue;

                }


                bool spawned =
                    ItemDropSpawner.Instance
                        .SpawnFromBlock(
                            stack.ItemId,
                            stack.Count,
                            dropPosition
                        );


                if (
                    !spawned
                )
                {

                    Debug.LogWarning(
                        "CHEST: Failed to drop " +
                        stack.ItemId +
                        " x" +
                        stack.Count
                    );

                }

            }

        }


        // =====================================================
        // ID
        // =====================================================

        private static long Pack(
            int x,
            int y
        )
        {

            return
                (
                    (long)x <<
                    32
                )
                ^
                (uint)y;

        }

    }

}

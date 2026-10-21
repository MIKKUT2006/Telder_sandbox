using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Game.Content;
using Game.Inventory;
using Game.World;
using Game.World.Dimensions;


namespace Game.Save
{
    public static class SaveGameRuntime
    {
        private sealed class RuntimeDimension
        {
            public DimensionSaveData Data;

            public readonly Dictionary<
                long,
                BlockChangeSaveData
            > Changes =
                new Dictionary<
                    long,
                    BlockChangeSaveData
                >();


            public readonly Dictionary<
                long,
                ChestSaveData
            > Chests =
                new Dictionary<
                    long,
                    ChestSaveData
                >();
        }


        private static bool droppedItemsRestored = true;

        // Scene-load handshake prevents autosave from erasing unloaded drops.
        public static void MarkDroppedItemsRestorePending()
        {
            droppedItemsRestored = false;
        }

        public static void RestoreDroppedItems()
        {
            if (droppedItemsRestored || !HasActiveSave)
                return;

            RuntimeDimension runtime = GetCurrentDimensionRuntime();
            Game.World.Items.ItemDropSpawner spawner =
                Game.World.Items.ItemDropSpawner.EnsureInstance();
            if (runtime == null || spawner == null)
                return;

            droppedItemsRestored = true;
            if (runtime.Data == null || runtime.Data.DroppedItems == null)
                return;

            foreach (DroppedItemSaveData drop in runtime.Data.DroppedItems)
            {
                if (drop != null && drop.Count > 0 && !string.IsNullOrWhiteSpace(drop.ItemId))
                {
                    if (drop.Embedded && drop.Count == 1)
                        spawner.SpawnEmbedded(drop.ItemId,
                            new Vector2(drop.EmbeddedTipX, drop.EmbeddedTipY),
                            Quaternion.Euler(0f, 0f, drop.RotationZ));
                    else
                        spawner.SpawnRestored(drop.ItemId, drop.Count,
                            new Vector2(drop.X, drop.Y),
                            new Vector2(drop.VelocityX, drop.VelocityY));
                }
            }
        }

        private static void SaveDroppedItems(RuntimeDimension runtime)
        {
            if (!droppedItemsRestored || runtime == null || runtime.Data == null)
                return;

            if (runtime.Data.DroppedItems == null)
                runtime.Data.DroppedItems = new List<DroppedItemSaveData>();
            else
                runtime.Data.DroppedItems.Clear();

            foreach (Game.World.Items.DroppedItem drop in Game.World.Items.DroppedItem.ActiveItems)
            {
                if (drop == null || drop.Count <= 0 || string.IsNullOrWhiteSpace(drop.ItemId))
                    continue;

                Vector2 v = drop.CurrentVelocity;
                Vector3 pos = drop.transform.position;
                runtime.Data.DroppedItems.Add(new DroppedItemSaveData
                {
                    ItemId = drop.ItemId, Count = drop.Count,
                    X = pos.x, Y = pos.y, VelocityX = v.x, VelocityY = v.y,
                    Embedded = drop.IsEmbedded, RotationZ = drop.transform.eulerAngles.z,
                    EmbeddedTipX = drop.EmbeddedTip.x, EmbeddedTipY = drop.EmbeddedTip.y
                });
            }
        }

        private static TelderSaveData currentSave;

        private static string currentSaveId;

        private static readonly Dictionary<
            string,
            RuntimeDimension
        > loadedDimensions =
            new Dictionary<
                string,
                RuntimeDimension
            >(
                StringComparer.OrdinalIgnoreCase
            );


        // A scene reload creates a new PlayerInventory before the initial chunks
        // are ready. A timer/autosave during that interval must never replace the
        // old inventory with this newly constructed (empty) inventory.
        private static List<InventorySlotSaveData> transferInventory;
        private static int transferHotbarIndex;
        public static bool IsInventoryTransferPending => transferInventory != null;

        public static void BeginInventoryTransfer()
        {
            if (!HasActiveSave) return;
            transferInventory = CloneInventorySlots(currentSave.Inventory);
            transferHotbarIndex = currentSave.SelectedHotbarIndex;
        }

        public static void CompleteInventoryTransfer()
        {
            transferInventory = null;
        }

        private static List<InventorySlotSaveData> CloneInventorySlots(
            List<InventorySlotSaveData> source)
        {
            var result = new List<InventorySlotSaveData>();
            if (source == null) return result;
            foreach (var slot in source)
            {
                if (slot == null) continue;
                result.Add(new InventorySlotSaveData
                {
                    Slot = slot.Slot, ItemId = slot.ItemId, Count = slot.Count,
                    CurrentDurability = slot.CurrentDurability,
                    BonusMaxDurability = slot.BonusMaxDurability,
                    InstalledUpgradeId = slot.InstalledUpgradeId
                });
            }
            return result;
        }

        private static bool applyingChanges;


        public static bool HasActiveSave =>
            currentSave != null &&
            !string.IsNullOrWhiteSpace(
                currentSaveId
            );


        public static string CurrentSaveId =>
            currentSaveId;


        public static string CurrentSaveName =>
            currentSave != null
                ? currentSave.DisplayName
                : null;


        // Fired whenever foreground becomes air.
        // ChestManager uses this so chest contents also drop
        // when the normal BlockInteraction destroys the block.
        public static event Action<int, int>
            ForegroundCleared;


        // =====================================================
        // CREATE
        // =====================================================

        public static bool CreateNewSave(
            string displayName
        )
        {
            return CreateNewSave(
                displayName,
                false
            );
        }


        // Compatibility overload used by the 3D world creation flow.
        // Existing 2D callers can keep using CreateNewSave(name).
        public static bool CreateNewSave(
            string displayName,
            bool is3D
        )
        {
            CompleteInventoryTransfer();
            DimensionDatabase.Initialize();


            DimensionDefinition startDimension =
                DimensionDatabase.GetOrCreate(
                    DimensionDatabase.StartDimension
                );


            if (
                startDimension ==
                null
            )
            {
                Debug.LogError(
                    "SAVE: Start dimension could not be created."
                );

                return false;
            }


            currentSaveId =
                Guid.NewGuid()
                    .ToString("N");


            string now =
                DateTime.UtcNow
                    .ToString("O");


            currentSave =
                new TelderSaveData
                {
                    SaveId =
                        currentSaveId,

                    DisplayName =
                        string.IsNullOrWhiteSpace(
                            displayName
                        )
                            ? "Telder мир"
                            : displayName.Trim(),

                    Is3D =
                        is3D,

                    CurrentDimensionName =
                        startDimension.Name,

                    CurrentDimensionSeed =
                        startDimension.Seed,

                    CreatedUtc =
                        now,

                    LastPlayedUtc =
                        now,

                    SelectedHotbarIndex =
                        0
                };


            loadedDimensions.Clear();


            EnsureDimension(
                startDimension.Name,
                startDimension.Seed
            );


            DimensionTravelRuntime
                .SetCurrentForLoad(
                    startDimension.Name,
                    startDimension.Seed
                );


            WriteAll();


            return true;
        }


        // =====================================================
        // LOAD
        // =====================================================

        public static bool LoadSave(
            string saveId
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    saveId
                )
            )
            {
                return false;
            }


            string file =
                SavePaths.GetMainFile(
                    saveId
                );


            if (
                !File.Exists(
                    file
                )
            )
            {
                Debug.LogError(
                    "SAVE: save.json not found: " +
                    file
                );

                return false;
            }


            try
            {
                string json =
                    File.ReadAllText(
                        file
                    );


                TelderSaveData data =
                    JsonUtility.FromJson<
                        TelderSaveData
                    >(
                        json
                    );


                if (
                    data == null
                )
                {
                    return false;
                }


                CompleteInventoryTransfer();
                currentSave =
                    data;


                if (
                    currentSave.Dimensions ==
                    null
                )
                {
                    currentSave.Dimensions =
                        new List<
                            DimensionSummarySaveData
                        >();
                }


                if (
                    currentSave.Inventory ==
                    null
                )
                {
                    currentSave.Inventory =
                        new List<
                            InventorySlotSaveData
                        >();
                }


                if (currentSave.Version < 2)
                {
                    // V1 stored only item id/count. Missing durability fields can
                    // deserialize as zero, which would make old tools appear broken.
                    for (int i = 0; i < currentSave.Inventory.Count; i++)
                    {
                        InventorySlotSaveData slot = currentSave.Inventory[i];
                        if (slot == null)
                            continue;

                        slot.CurrentDurability = -1;
                        slot.BonusMaxDurability = 0;
                        slot.InstalledUpgradeId = string.Empty;
                    }

                    currentSave.Version = 2;
                }


                currentSaveId =
                    saveId;


                // V21 briefly generated dimension seeds from SaveId+Name.
                // That can produce a dimension seed/type combination for which
                // the biome profile is invalid, leaving generated chunks empty.
                // Normalize old saves back to the seed owned by DimensionDatabase.
                DimensionDatabase.Initialize();
                DimensionDefinition canonicalDimension =
                    DimensionDatabase.GetOrCreate(
                        currentSave.CurrentDimensionName
                    );

                if (canonicalDimension != null &&
                    currentSave.CurrentDimensionSeed != canonicalDimension.Seed)
                {
                    currentSave.CurrentDimensionSeed = canonicalDimension.Seed;

                    if (currentSave.Dimensions != null)
                    {
                        for (int d = 0; d < currentSave.Dimensions.Count; d++)
                        {
                            DimensionSummarySaveData summary = currentSave.Dimensions[d];
                            if (summary != null && string.Equals(summary.Name, canonicalDimension.Name, StringComparison.OrdinalIgnoreCase))
                                summary.Seed = canonicalDimension.Seed;
                        }
                    }

                    File.WriteAllText(
                        SavePaths.GetMainFile(currentSaveId),
                        JsonUtility.ToJson(currentSave, true)
                    );
                }


                loadedDimensions.Clear();


                EnsureDimension(
                    currentSave.CurrentDimensionName,
                    currentSave.CurrentDimensionSeed
                );


                DimensionTravelRuntime
                    .SetCurrentForLoad(
                        currentSave.CurrentDimensionName,
                        currentSave.CurrentDimensionSeed
                    );


                return true;
            }
            catch (
                Exception exception
            )
            {
                Debug.LogError(
                    "SAVE: Failed to load save " +
                    saveId +
                    "\n" +
                    exception
                );

                return false;
            }
        }


        public static void ClearActiveSave()
        {
            currentSave =
                null;

            currentSaveId =
                null;

            loadedDimensions.Clear();
        }


        // =====================================================
        // SAVE LIST
        // =====================================================

        public static bool RenameSave(
            string saveId,
            string newDisplayName
        )
        {
            if (
                string.IsNullOrWhiteSpace(saveId)
                ||
                string.IsNullOrWhiteSpace(newDisplayName)
            )
            {
                return false;
            }

            string file =
                SavePaths.GetMainFile(saveId);

            if (!File.Exists(file))
                return false;

            try
            {
                TelderSaveData data =
                    JsonUtility.FromJson<TelderSaveData>(
                        File.ReadAllText(file)
                    );

                if (data == null)
                    return false;

                data.DisplayName =
                    newDisplayName.Trim();

                File.WriteAllText(
                    file,
                    JsonUtility.ToJson(
                        data,
                        true
                    )
                );

                if (
                    HasActiveSave
                    &&
                    currentSaveId ==
                    saveId
                )
                {
                    currentSave.DisplayName =
                        data.DisplayName;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "SAVE: Rename failed: " +
                    exception
                );

                return false;
            }
        }


        public static bool DeleteSave(
            string saveId
        )
        {
            if (
                string.IsNullOrWhiteSpace(saveId)
            )
            {
                return false;
            }

            string folder =
                SavePaths.GetSaveFolder(saveId);

            if (!Directory.Exists(folder))
                return false;

            try
            {
                if (
                    HasActiveSave
                    &&
                    currentSaveId ==
                    saveId
                )
                {
                    ClearActiveSave();
                }

                Directory.Delete(
                    folder,
                    true
                );

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "SAVE: Delete failed: " +
                    exception
                );

                return false;
            }
        }


        public static List<TelderSaveSummary>
            GetSaveSummaries()
        {
            List<TelderSaveSummary> result =
                new List<TelderSaveSummary>();


            string root =
                SavePaths.RootFolder;


            if (
                !Directory.Exists(
                    root
                )
            )
            {
                return result;
            }


            string[] directories =
                Directory.GetDirectories(
                    root
                );


            for (
                int i = 0;
                i < directories.Length;
                i++
            )
            {
                string folder =
                    directories[i];


                string file =
                    Path.Combine(
                        folder,
                        "save.json"
                    );


                if (
                    !File.Exists(
                        file
                    )
                )
                {
                    continue;
                }


                try
                {
                    TelderSaveData data =
                        JsonUtility.FromJson<
                            TelderSaveData
                        >(
                            File.ReadAllText(
                                file
                            )
                        );


                    if (
                        data == null
                    )
                    {
                        continue;
                    }


                    result.Add(
                        new TelderSaveSummary
                        {
                            SaveId =
                                data.SaveId,

                            DisplayName =
                                data.DisplayName,

                            Is3D =
                                data.Is3D,

                            CurrentDimensionName =
                                data.CurrentDimensionName,

                            LastPlayedUtc =
                                data.LastPlayedUtc,

                            PreviewPath =
                                SavePaths.GetPreviewFile(
                                    data.SaveId
                                )
                        }
                    );
                }
                catch
                {
                    // Повреждённое сохранение
                    // не должно ломать всё главное меню.
                }
            }


            result.Sort(
                (a, b) =>
                    string.CompareOrdinal(
                        b.LastPlayedUtc,
                        a.LastPlayedUtc
                    )
            );


            return result;
        }


        // =====================================================
        // DIMENSION
        // =====================================================

        public static void SetCurrentDimension(
            string name,
            int seed
        )
        {
            if (
                !HasActiveSave
            )
            {
                return;
            }


            currentSave.CurrentDimensionName =
                name;


            currentSave.CurrentDimensionSeed =
                seed;


            EnsureDimension(
                name,
                seed
            );


            WriteMainFile();
        }


        public static bool TryGetVisitedDimensionSeed(
            string dimensionName,
            out int seed
        )
        {
            seed =
                0;


            if (
                !HasActiveSave ||
                string.IsNullOrWhiteSpace(
                    dimensionName
                )
            )
            {
                return false;
            }


            for (
                int i = 0;
                i < currentSave.Dimensions.Count;
                i++
            )
            {
                DimensionSummarySaveData summary =
                    currentSave.Dimensions[i];


                if (
                    summary != null &&
                    string.Equals(
                        summary.Name,
                        dimensionName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    seed =
                        summary.Seed;

                    return true;
                }
            }


            return false;
        }


        public static int GetOrCreateVisitedDimensionSeed(
            string dimensionName
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    dimensionName
                )
            )
            {
                return 0;
            }


            if (
                TryGetVisitedDimensionSeed(
                    dimensionName,
                    out int existing
                )
            )
            {
                return existing;
            }


            if (
                !HasActiveSave
            )
            {
                return 0;
            }


            DimensionDefinition definition =
                DimensionDatabase.GetOrCreate(
                    dimensionName
                );


            if (
                definition ==
                null
            )
            {
                return 0;
            }


            int seed =
                definition.Seed;


            EnsureDimension(
                definition.Name,
                seed
            );


            WriteMainFile();


            return seed;
        }


        public static List<DimensionSummarySaveData>
            GetVisitedDimensions()
        {
            List<DimensionSummarySaveData> result =
                new List<DimensionSummarySaveData>();


            if (
                !HasActiveSave
                ||
                currentSave.Dimensions ==
                null
            )
            {
                return result;
            }


            for (
                int i = 0;
                i < currentSave.Dimensions.Count;
                i++
            )
            {
                DimensionSummarySaveData source =
                    currentSave.Dimensions[i];


                if (
                    source ==
                    null
                )
                {
                    continue;
                }


                result.Add(
                    new DimensionSummarySaveData
                    {
                        Name =
                            source.Name,

                        Seed =
                            source.Seed
                    }
                );
            }


            return result;
        }


        private static RuntimeDimension EnsureDimension(
            string name,
            int seed
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    name
                )
            )
            {
                return null;
            }


            if (
                loadedDimensions.TryGetValue(
                    name,
                    out RuntimeDimension existing
                )
            )
            {
                return existing;
            }


            DimensionSaveData data =
                LoadDimensionFile(
                    name,
                    seed
                );


            if (
                data == null
            )
            {
                data =
                    new DimensionSaveData
                    {
                        Name =
                            name,

                        Seed =
                            seed
                    };
            }


            if (data.Entities == null)
            {
                data.Entities =
                    new List<EntitySaveData>();
            }


            RuntimeDimension runtime =
                new RuntimeDimension
                {
                    Data =
                        data
                };


            if (
                data.Changes != null
            )
            {
                for (
                    int i = 0;
                    i < data.Changes.Count;
                    i++
                )
                {
                    BlockChangeSaveData change =
                        data.Changes[i];


                    if (
                        change == null
                    )
                    {
                        continue;
                    }


                    runtime.Changes[
                        Pack(
                            change.X,
                            change.Y
                        )
                    ] =
                        change;
                }
            }


            if (
                data.Chests != null
            )
            {
                for (
                    int i = 0;
                    i < data.Chests.Count;
                    i++
                )
                {
                    ChestSaveData chest =
                        data.Chests[i];


                    if (
                        chest == null
                    )
                    {
                        continue;
                    }


                    runtime.Chests[
                        Pack(
                            chest.X,
                            chest.Y
                        )
                    ] =
                        chest;
                }
            }


            loadedDimensions[name] =
                runtime;


            if (
                HasActiveSave
            )
            {
                bool exists =
                    false;


                for (
                    int i = 0;
                    i < currentSave.Dimensions.Count;
                    i++
                )
                {
                    DimensionSummarySaveData summary =
                        currentSave.Dimensions[i];


                    if (
                        summary != null &&
                        string.Equals(
                            summary.Name,
                            name,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        summary.Seed =
                            seed;

                        exists =
                            true;

                        break;
                    }
                }


                if (
                    !exists
                )
                {
                    currentSave.Dimensions.Add(
                        new DimensionSummarySaveData
                        {
                            Name =
                                name,

                            Seed =
                                seed
                        }
                    );
                }
            }


            return runtime;
        }


        // =====================================================
        // WORLD CHANGES
        // =====================================================

        // Read procedural sky occlusion without materializing chunks above a cave.
        public static bool TryGetForegroundOverride(int x,int y,out ushort blockId)
        {
            blockId=0;if(!HasActiveSave)return false;
            RuntimeDimension runtime=GetCurrentDimensionRuntime();
            if(runtime==null||!runtime.Changes.TryGetValue(Pack(x,y),out BlockChangeSaveData change)||!change.HasForeground)return false;
            blockId=StringToBlockId(change.ForegroundId);return true;
        }

        public static void RecordForegroundChange(
            int worldX,
            int worldY,
            ushort blockId
        )
        {
            // This event is intentionally independent from HasActiveSave.
            // It lets chest contents drop even while testing the Game
            // scene directly without selecting a Telder save.
            if (
                blockId == 0
                &&
                !applyingChanges
            )
            {
                ForegroundCleared?.Invoke(
                    worldX,
                    worldY
                );
            }


            if (
                !HasActiveSave ||
                applyingChanges
            )
            {
                return;
            }


            RuntimeDimension runtime =
                GetCurrentDimensionRuntime();


            if (
                runtime == null
            )
            {
                return;
            }


            BlockChangeSaveData change =
                GetOrCreateChange(
                    runtime,
                    worldX,
                    worldY
                );


            change.HasForeground =
                true;


            change.ForegroundId =
                BlockIdToString(
                    blockId
                );
        }


        public static void RecordBackgroundChange(
            int worldX,
            int worldY,
            ushort blockId
        )
        {
            if (
                !HasActiveSave ||
                applyingChanges
            )
            {
                return;
            }


            RuntimeDimension runtime =
                GetCurrentDimensionRuntime();


            if (
                runtime == null
            )
            {
                return;
            }


            BlockChangeSaveData change =
                GetOrCreateChange(
                    runtime,
                    worldX,
                    worldY
                );


            change.HasBackground =
                true;


            change.BackgroundId =
                BlockIdToString(
                    blockId
                );
        }


        public static void ApplyChangesToChunk(
            Chunk chunk
        )
        {
            if (
                !HasActiveSave ||
                chunk == null
            )
            {
                return;
            }


            RuntimeDimension runtime =
                GetCurrentDimensionRuntime();


            if (
                runtime == null ||
                runtime.Changes.Count == 0
            )
            {
                return;
            }


            applyingChanges =
                true;


            try
            {
                int startX =
                    chunk.X *
                    Chunk.SizeX;


                int startY =
                    chunk.Y *
                    Chunk.SizeY;


                for (
                    int localX = 0;
                    localX < Chunk.SizeX;
                    localX++
                )
                {
                    int worldX =
                        startX +
                        localX;


                    for (
                        int localY = 0;
                        localY < Chunk.SizeY;
                        localY++
                    )
                    {
                        int worldY =
                            startY +
                            localY;


                        if (
                            !runtime.Changes.TryGetValue(
                                Pack(
                                    worldX,
                                    worldY
                                ),
                                out BlockChangeSaveData change
                            )
                        )
                        {
                            continue;
                        }


                        if (
                            change.HasForeground
                        )
                        {
                            chunk.SetBlock(
                                localX,
                                localY,
                                StringToBlockId(
                                    change.ForegroundId
                                )
                            );
                        }


                        if (
                            change.HasBackground
                        )
                        {
                            chunk.SetBackground(
                                localX,
                                localY,
                                StringToBlockId(
                                    change.BackgroundId
                                )
                            );
                        }
                    }
                }
            }
            finally
            {
                applyingChanges =
                    false;
            }
        }


        // =====================================================
        // CHESTS
        // =====================================================

        public static bool TryGetChestData(
            int worldX,
            int worldY,
            out ChestSaveData chest
        )
        {
            chest =
                null;


            if (
                !HasActiveSave
            )
            {
                return false;
            }


            RuntimeDimension runtime =
                GetCurrentDimensionRuntime();


            if (
                runtime == null
            )
            {
                return false;
            }


            return
                runtime.Chests.TryGetValue(
                    Pack(
                        worldX,
                        worldY
                    ),
                    out chest
                );
        }


        public static void StoreChestData(
            int worldX,
            int worldY,
            string[] itemIds,
            int[] counts
        )
        {
            if (
                !HasActiveSave
            )
            {
                return;
            }


            RuntimeDimension runtime =
                GetCurrentDimensionRuntime();


            if (
                runtime == null
            )
            {
                return;
            }


            long key =
                Pack(
                    worldX,
                    worldY
                );


            ChestSaveData chest =
                new ChestSaveData
                {
                    X =
                        worldX,

                    Y =
                        worldY
                };


            bool hasItems =
                false;


            if (
                itemIds != null
                &&
                counts != null
            )
            {
                int length =
                    Mathf.Min(
                        itemIds.Length,
                        counts.Length
                    );


                for (
                    int i = 0;
                    i < length;
                    i++
                )
                {
                    if (
                        string.IsNullOrWhiteSpace(
                            itemIds[i]
                        )
                        ||
                        counts[i] <= 0
                    )
                    {
                        continue;
                    }


                    hasItems =
                        true;


                    chest.Slots.Add(
                        new ChestSlotSaveData
                        {
                            Slot =
                                i,

                            ItemId =
                                itemIds[i],

                            Count =
                                counts[i]
                        }
                    );
                }
            }


            // Keep even an empty initialized chest.
            // Generated loot must not reroll after reload.
            runtime.Chests[
                key
            ] =
                chest;
        }


        public static bool RemoveChestData(
            int worldX,
            int worldY
        )
        {
            if (
                !HasActiveSave
            )
            {
                return false;
            }


            RuntimeDimension runtime =
                GetCurrentDimensionRuntime();


            if (
                runtime == null
            )
            {
                return false;
            }


            return
                runtime.Chests.Remove(
                    Pack(
                        worldX,
                        worldY
                    )
                );
        }


        public static bool HasChestData(
            int worldX,
            int worldY
        )
        {
            if (
                !HasActiveSave
            )
            {
                return false;
            }


            RuntimeDimension runtime =
                GetCurrentDimensionRuntime();


            if (
                runtime == null
            )
            {
                return false;
            }


            return
                runtime.Chests.ContainsKey(
                    Pack(
                        worldX,
                        worldY
                    )
                );
        }


        // =====================================================
        // PLAYER + INVENTORY
        // =====================================================

        public static void SaveCurrentScene()
        {
            Game.World.Voxel3D.VoxelWorld3DRuntime.Instance?.SaveState();
            Game.Gameplay.Plants.PlantRuntime.Flush();
            Game.Gameplay.Cooking.CauldronRuntime.Instance?.ForceSave();
            foreach(var effect in UnityEngine.Object.FindObjectsByType<Game.Gameplay.Effects.PlayerFoodEffects>(FindObjectsSortMode.None)) effect.ForceSave();
            // A reload can put a brand-new, empty PlayerInventory into the scene.
            // Ignore saves until the transfer snapshot has been restored.
            if (IsInventoryTransferPending) return;
            if (
                !HasActiveSave
            )
            {
                return;
            }


            Game.World.Fluids.LiquidRuntime liquidRuntime =
                UnityEngine.Object.FindFirstObjectByType<Game.World.Fluids.LiquidRuntime>();
            if (liquidRuntime != null)
                liquidRuntime.Flush();

            if (Game.Achievements.AchievementRuntime.Instance != null)
                Game.Achievements.AchievementRuntime.Instance.Save();


            // Furniture is stored in a dimension-scoped sidecar file.
            // Flush it together with the main save so exiting/reloading cannot
            // leave inventory saved while placed furniture is stale.
            if (Game.World.Furniture.FurnitureLayerManager.Instance != null)
            {
                Game.World.Furniture.FurnitureLayerManager.Instance.Flush();
            }


            DimensionDefinition dimension =
                DimensionTravelRuntime.Current;


            if (
                dimension == null
            )
            {
                return;
            }


            currentSave.CurrentDimensionName =
                dimension.Name;


            currentSave.CurrentDimensionSeed =
                dimension.Seed;


            RuntimeDimension runtime =
                EnsureDimension(
                    dimension.Name,
                    dimension.Seed
                );


            PlayerInventory inventory =
                UnityEngine.Object
                    .FindFirstObjectByType<
                        PlayerInventory
                    >();


            if (
                inventory != null
            )
            {
                SaveInventory(
                    inventory
                );


                Vector3 position =
                    inventory.transform
                        .position;


                runtime.Data.HasPlayerPosition =
                    true;


                runtime.Data.PlayerX =
                    position.x;


                runtime.Data.PlayerY =
                    position.y;


                runtime.Data.PlayerZ =
                    position.z;
            }


            Game.PlayerStats.PlayerStats playerStats =
                UnityEngine.Object.FindFirstObjectByType<
                    Game.PlayerStats.PlayerStats
                >();


            if (playerStats != null)
            {
                currentSave.HasPlayerStats = true;
                currentSave.PlayerHealth = playerStats.Health;
                currentSave.PlayerHunger = playerStats.Hunger;
                currentSave.PlayerAirSeconds = playerStats.AirSeconds;
            }


            SaveEntities(runtime);
            SaveDroppedItems(runtime);

            currentSave.LastPlayedUtc =
                DateTime.UtcNow
                    .ToString("O");


            WriteAll();
        }


        public static bool TryGetCurrentPlayerPosition(
            out Vector3 position
        )
        {
            position =
                Vector3.zero;


            if (
                !HasActiveSave
            )
            {
                return false;
            }


            DimensionDefinition dimension =
                DimensionTravelRuntime.Current;


            if (
                dimension == null
            )
            {
                return false;
            }


            RuntimeDimension runtime =
                EnsureDimension(
                    dimension.Name,
                    dimension.Seed
                );


            if (
                runtime == null ||
                !runtime.Data.HasPlayerPosition
            )
            {
                return false;
            }


            position =
                new Vector3(
                    runtime.Data.PlayerX,
                    runtime.Data.PlayerY,
                    runtime.Data.PlayerZ
                );


            return true;
        }


        public static void RestoreInventory(
            PlayerInventory inventory
        )
        {
            if (
                !HasActiveSave ||
                inventory == null
            )
            {
                return;
            }


            // Restore the structured slot records instead of only parallel ID/count
            // arrays. This preserves durability and installed Tool Bench upgrades.
            inventory.RestoreFromSave(
                transferInventory ?? currentSave.Inventory
            );


            inventory.SelectHotbarSlot(
                Mathf.Clamp(
                    transferInventory != null ? transferHotbarIndex : currentSave.SelectedHotbarIndex,
                    0,
                    PlayerInventory.HotbarSize - 1
                )
            );


            // SelectHotbarSlot intentionally does nothing when the index is already
            // selected, so always broadcast once after a load/dimension transition.
            // HeldItemVisualController and weapon bridges rely on this notification.
            inventory.NotifyExternalChange();
        }


        public static void RestorePlayerStats(
            Game.PlayerStats.PlayerStats stats
        )
        {
            if (
                !HasActiveSave ||
                stats == null ||
                !currentSave.HasPlayerStats
            )
            {
                return;
            }


            stats.RestoreState(
                currentSave.PlayerHealth,
                currentSave.PlayerHunger,
                currentSave.PlayerAirSeconds
            );
        }


        public static List<EntitySaveData> GetCurrentSavedEntities()
        {
            List<EntitySaveData> result =
                new List<EntitySaveData>();


            if (!HasActiveSave)
                return result;


            RuntimeDimension runtime =
                GetCurrentDimensionRuntime();


            if (
                runtime == null ||
                runtime.Data == null ||
                runtime.Data.Entities == null
            )
            {
                return result;
            }


            for (int i = 0; i < runtime.Data.Entities.Count; i++)
            {
                EntitySaveData source = runtime.Data.Entities[i];
                if (source == null || string.IsNullOrWhiteSpace(source.DefinitionId))
                    continue;

                result.Add(
                    new EntitySaveData
                    {
                        DefinitionId = source.DefinitionId,
                        Stars = source.Stars,
                        Health = source.Health,
                        X = source.X,
                        Y = source.Y,
                        Z = source.Z,
                        HeldItemId = source.HeldItemId
                    }
                );
            }


            return result;
        }


        private static void SaveEntities(
            RuntimeDimension runtime
        )
        {
            if (runtime == null || runtime.Data == null)
                return;


            // Do not erase valid snapshots while the scene spawn manager is still
            // asynchronously restoring them. This matters during a very fast
            // autosave/teleport immediately after loading a world.
            Game.Entities.Spawn.EntitySpawnManager spawnManager =
                UnityEngine.Object.FindFirstObjectByType<
                    Game.Entities.Spawn.EntitySpawnManager
                >();

            if (spawnManager != null && !spawnManager.PersistenceRestoreFinished)
                return;


            if (runtime.Data.Entities == null)
                runtime.Data.Entities = new List<EntitySaveData>();
            else
                runtime.Data.Entities.Clear();


            if (spawnManager != null)
            {
                foreach (EntitySaveData deferred in spawnManager.DeferredRestores)
                    runtime.Data.Entities.Add(new EntitySaveData
                    {
                        DefinitionId = deferred.DefinitionId, Stars = deferred.Stars,
                        Health = deferred.Health, X = deferred.X, Y = deferred.Y,
                        Z = deferred.Z, HeldItemId = deferred.HeldItemId
                    });
            }

            IReadOnlyList<Game.Entities.EntityActor> active =
                Game.Entities.EntityActor.ActiveActors;


            for (int i = 0; i < active.Count; i++)
            {
                Game.Entities.EntityActor actor = active[i];

                if (
                    actor == null ||
                    actor.IsDead ||
                    string.IsNullOrWhiteSpace(actor.DefinitionId)
                )
                {
                    continue;
                }


                Vector3 position = actor.transform.position;

                runtime.Data.Entities.Add(
                    new EntitySaveData
                    {
                        DefinitionId = actor.DefinitionId,
                        Stars = actor.Stars,
                        Health = actor.Health,
                        X = position.x,
                        Y = position.y,
                        Z = position.z,
                        HeldItemId = actor.HeldItemId
                    }
                );
            }
        }


        private static void SaveInventory(
            PlayerInventory inventory
        )
        {
            currentSave.Inventory.Clear();


            for (
                int i = 0;
                i < PlayerInventory.SlotCount;
                i++
            )
            {
                ItemStack stack =
                    inventory.GetSlot(
                        i
                    );


                if (
                    stack == null ||
                    stack.IsEmpty
                )
                {
                    continue;
                }


                currentSave.Inventory.Add(
                    new InventorySlotSaveData
                    {
                        Slot =
                            i,

                        ItemId =
                            stack.ItemId,

                        Count =
                            stack.Count,

                        CurrentDurability =
                            stack.CurrentDurability,

                        BonusMaxDurability =
                            stack.BonusMaxDurability,

                        InstalledUpgradeId =
                            stack.InstalledUpgradeId
                    }
                );
            }


            currentSave.SelectedHotbarIndex =
                inventory.SelectedHotbarIndex;
        }


        // =====================================================
        // DISK
        // =====================================================

        public static void WriteAll()
        {
            if (
                !HasActiveSave
            )
            {
                return;
            }


            Directory.CreateDirectory(
                SavePaths.GetSaveFolder(
                    currentSaveId
                )
            );


            Directory.CreateDirectory(
                SavePaths.GetDimensionsFolder(
                    currentSaveId
                )
            );


            foreach (
                RuntimeDimension runtime
                in loadedDimensions.Values
            )
            {
                WriteDimension(
                    runtime
                );
            }


            WriteMainFile();
        }


        private static void WriteMainFile()
        {
            if (
                !HasActiveSave
            )
            {
                return;
            }


            Directory.CreateDirectory(
                SavePaths.GetSaveFolder(
                    currentSaveId
                )
            );


            string json =
                JsonUtility.ToJson(
                    currentSave,
                    true
                );


            SafeWriteText(
                SavePaths.GetMainFile(
                    currentSaveId
                ),
                json
            );
        }


        private static void WriteDimension(
            RuntimeDimension runtime
        )
        {
            if (
                runtime == null ||
                runtime.Data == null
            )
            {
                return;
            }


            runtime.Data.Changes =
                new List<BlockChangeSaveData>(
                    runtime.Changes.Values
                );


            runtime.Data.Chests =
                new List<ChestSaveData>(
                    runtime.Chests.Values
                );


            string path =
                SavePaths.GetDimensionFile(
                    currentSaveId,
                    runtime.Data.Name,
                    runtime.Data.Seed
                );


            string json =
                JsonUtility.ToJson(
                    runtime.Data,
                    true
                );


            SafeWriteText(
                path,
                json
            );
        }


        private static DimensionSaveData
            LoadDimensionFile(
                string name,
                int seed
            )
        {
            if (
                !HasActiveSave
            )
            {
                return null;
            }


            string path =
                SavePaths.GetDimensionFile(
                    currentSaveId,
                    name,
                    seed
                );


            if (
                !File.Exists(
                    path
                )
            )
            {
                return null;
            }


            try
            {
                return
                    JsonUtility.FromJson<
                        DimensionSaveData
                    >(
                        File.ReadAllText(
                            path
                        )
                    );
            }
            catch
            {
                return null;
            }
        }


        private static void SafeWriteText(
            string path,
            string content
        )
        {
            string directory =
                Path.GetDirectoryName(
                    path
                );


            if (
                !string.IsNullOrWhiteSpace(
                    directory
                )
            )
            {
                Directory.CreateDirectory(
                    directory
                );
            }


            string temp =
                path +
                ".tmp";


            File.WriteAllText(
                temp,
                content
            );


            if (
                File.Exists(
                    path
                )
            )
            {
                File.Delete(
                    path
                );
            }


            File.Move(
                temp,
                path
            );
        }


        // =====================================================
        // HELPERS
        // =====================================================

        private static RuntimeDimension
            GetCurrentDimensionRuntime()
        {
            DimensionDefinition dimension =
                DimensionTravelRuntime.Current;


            if (
                dimension == null
            )
            {
                return null;
            }


            return
                EnsureDimension(
                    dimension.Name,
                    dimension.Seed
                );
        }


        private static BlockChangeSaveData
            GetOrCreateChange(
                RuntimeDimension runtime,
                int x,
                int y
            )
        {
            long key =
                Pack(
                    x,
                    y
                );


            if (
                runtime.Changes.TryGetValue(
                    key,
                    out BlockChangeSaveData change
                )
            )
            {
                return change;
            }


            change =
                new BlockChangeSaveData
                {
                    X =
                        x,

                    Y =
                        y
                };


            runtime.Changes[
                key
            ] =
                change;


            return change;
        }


        private static int BuildSaveDimensionSeed(
            string saveId,
            string dimensionName
        )
        {
            unchecked
            {
                int hash =
                    17;


                string source =
                    (
                        saveId ??
                        string.Empty
                    )
                    +
                    "|" +
                    (
                        dimensionName ??
                        string.Empty
                    );


                for (
                    int i = 0;
                    i < source.Length;
                    i++
                )
                {
                    hash =
                        hash *
                        31 +
                        source[i];
                }


                if (
                    hash ==
                    0
                )
                {
                    hash =
                        1;
                }


                return hash;
            }
        }


        private static long Pack(
            int x,
            int y
        )
        {
            return
                (
                    (long)x
                    <<
                    32
                )
                ^
                (uint)y;
        }


        private static string BlockIdToString(
            ushort blockId
        )
        {
            if (
                blockId == 0
            )
            {
                return string.Empty;
            }


            try
            {
                return
                    BlockIDRegistry
                        .GetContentID(
                            blockId
                        )
                        .ToString();
            }
            catch
            {
                return string.Empty;
            }
        }


        private static ushort StringToBlockId(
            string contentId
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    contentId
                )
            )
            {
                return 0;
            }


            try
            {
                ContentID id =
                    ContentID.Parse(
                        contentId
                    );


                if (
                    !BlockIDRegistry.Contains(
                        id
                    )
                )
                {
                    return 0;
                }


                return
                    BlockIDRegistry.GetID(
                        id
                    );
            }
            catch
            {
                return 0;
            }
        }
    }
}

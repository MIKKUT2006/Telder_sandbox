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


        // =====================================================
        // CREATE
        // =====================================================

        public static bool CreateNewSave(
            string displayName
        )
        {
            DimensionDatabase.Initialize();

            DimensionDefinition startDimension =
                DimensionDatabase.GetOrCreate(
                    DimensionDatabase.StartDimension
                );


            if (
                startDimension == null
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


                currentSave =
                    data;


                currentSaveId =
                    saveId;


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

        public static void RecordForegroundChange(
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
        // PLAYER + INVENTORY
        // =====================================================

        public static void SaveCurrentScene()
        {
            if (
                !HasActiveSave
            )
            {
                return;
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


            string[] ids =
                new string[
                    PlayerInventory.SlotCount
                ];


            int[] counts =
                new int[
                    PlayerInventory.SlotCount
                ];


            if (
                currentSave.Inventory != null
            )
            {
                for (
                    int i = 0;
                    i < currentSave.Inventory.Count;
                    i++
                )
                {
                    InventorySlotSaveData slot =
                        currentSave.Inventory[i];


                    if (
                        slot == null ||
                        slot.Slot < 0 ||
                        slot.Slot >=
                        PlayerInventory.SlotCount
                    )
                    {
                        continue;
                    }


                    ids[
                        slot.Slot
                    ] =
                        slot.ItemId;


                    counts[
                        slot.Slot
                    ] =
                        slot.Count;
                }
            }


            inventory.RestoreFromSave(
                ids,
                counts,
                currentSave.SelectedHotbarIndex
            );
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
                            stack.Count
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

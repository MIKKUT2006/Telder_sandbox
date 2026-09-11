using System;
using System.Collections.Generic;

namespace Game.Save
{
    [Serializable]
    public class TelderSaveData
    {
        public int Version = 1;

        public string SaveId;
        public string DisplayName;

        public string CurrentDimensionName;
        public int CurrentDimensionSeed;

        public string CreatedUtc;
        public string LastPlayedUtc;

        public int SelectedHotbarIndex;

        public List<InventorySlotSaveData> Inventory =
            new List<InventorySlotSaveData>();

        public List<DimensionSummarySaveData> Dimensions =
            new List<DimensionSummarySaveData>();
    }


    [Serializable]
    public class InventorySlotSaveData
    {
        public int Slot;
        public string ItemId;
        public int Count;
    }


    [Serializable]
    public class DimensionSummarySaveData
    {
        public string Name;
        public int Seed;
    }


    [Serializable]
    public class DimensionSaveData
    {
        public string Name;
        public int Seed;

        public bool HasPlayerPosition;

        public float PlayerX;
        public float PlayerY;
        public float PlayerZ;

        public List<BlockChangeSaveData> Changes =
            new List<BlockChangeSaveData>();

        public List<ChestSaveData> Chests =
            new List<ChestSaveData>();
    }


    [Serializable]
    public class BlockChangeSaveData
    {
        public int X;
        public int Y;

        public bool HasForeground;
        public string ForegroundId;

        public bool HasBackground;
        public string BackgroundId;
    }



    [Serializable]
    public class ChestSaveData
    {
        public int X;
        public int Y;

        public List<ChestSlotSaveData> Slots =
            new List<ChestSlotSaveData>();
    }


    [Serializable]
    public class ChestSlotSaveData
    {
        public int Slot;
        public string ItemId;
        public int Count;
    }


    public sealed class TelderSaveSummary
    {
        public string SaveId;
        public string DisplayName;
        public string CurrentDimensionName;
        public string LastPlayedUtc;
        public string PreviewPath;
    }
}

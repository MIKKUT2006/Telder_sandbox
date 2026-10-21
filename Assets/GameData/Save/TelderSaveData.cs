using System;
using System.Collections.Generic;

namespace Game.Save
{
    [Serializable]
    public class TelderSaveData
    {
        public int Version = 2;

        public string SaveId;
        public string DisplayName;

        // True for saves that use the 3D/Voxel world runtime.
        // Kept in the main save file so the main menu can restore
        // the correct visual/launch mode after save-system migrations.
        public bool Is3D;

        public string CurrentDimensionName;
        public int CurrentDimensionSeed;

        public string CreatedUtc;
        public string LastPlayedUtc;

        public int SelectedHotbarIndex;

        public bool HasPlayerStats;
        public float PlayerHealth;
        public float PlayerHunger;
        public float PlayerAirSeconds;

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

        public int CurrentDurability = -1;
        public int BonusMaxDurability = 0;
        public string InstalledUpgradeId = "";
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

        public List<EntitySaveData> Entities =
            new List<EntitySaveData>();

        public List<DroppedItemSaveData> DroppedItems =
            new List<DroppedItemSaveData>();
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
    public class EntitySaveData
    {
        public string DefinitionId;
        public int Stars = 1;
        public float Health = 1f;
        public float X;
        public float Y;
        public float Z;
        public string HeldItemId;
    }


    [Serializable]
    public class DroppedItemSaveData
    {
        public string ItemId;
        public int Count;
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public bool Embedded;
        public float RotationZ;
        public float EmbeddedTipX;
        public float EmbeddedTipY;
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
        public bool Is3D;
        public string CurrentDimensionName;
        public string LastPlayedUtc;
        public string PreviewPath;
    }
}

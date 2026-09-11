
using System;
using System.Collections.Generic;

namespace Game.World.Structures
{
    public enum StructureType
    {
        Normal = 0,
        Tree = 1
    }

    public enum StructureSpawnType
    {
        Underground = 0,
        Surface = 1,
        Any = 2
    }

    [Serializable]
    public class StructureDefinition
    {
        public string ID = "new_structure";
        public string DisplayName = "New Structure";

        public int Width = 12;
        public int Height = 8;

        public int OriginX = 0;
        public int OriginY = 0;

        // Special runtime behaviour.
        //
        // Tree:
        // - OriginX is treated as the trunk column;
        // - breaking a trunk cell removes matching structure
        //   cells at the cut height and above.
        public StructureType Type =
            StructureType.Normal;

        public StructureSpawnType SpawnType =
            StructureSpawnType.Underground;

        public float SpawnChance = 0.10f;
        public int RegionSize = 64;

        public bool UseHeightRange = true;
        public int MinY = 10;
        public int MaxY = 90;

        public bool RequireFreeSpace = true;
        public int FreeSpacePadding = 1;

        // Empty = any biome.
        public List<string> Biomes =
            new List<string>();

        public List<StructureCellDefinition> Cells =
            new List<StructureCellDefinition>();
    }

    [Serializable]
    public class StructureCellDefinition
    {
        public int X;
        public int Y;

        public string ForegroundId;
        public string BackgroundId;

        public List<StructureLootEntryDefinition> Loot =
            new List<StructureLootEntryDefinition>();
    }

    [Serializable]
    public class StructureLootEntryDefinition
    {
        public string ItemId;
        public float Chance = 1f;
        public int MinCount = 1;
        public int MaxCount = 1;
    }
}

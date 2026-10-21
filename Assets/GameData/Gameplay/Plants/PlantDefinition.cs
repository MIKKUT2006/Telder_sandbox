using System;
using System.Collections.Generic;
using Game.Blocks;
using UnityEngine;

namespace Game.Gameplay.Plants
{
    [Serializable]
    public sealed class PlantDefinition
    {
        public string[] SoilBlocks =
        {
            "game:dirt",
            "game:grass"
        };
        public string[] Stages =
        {
            "crop_stage_0",
            "crop_stage_1",
            "crop_stage_2",
            "crop_stage_3"
        };
        public float GrowthSeconds = 1200;
        public List<BlockLootEntryDefinition> MatureLoot = new List<BlockLootEntryDefinition>();
        public List<BlockLootEntryDefinition> ImmatureLoot;
        public string[] Structures;
        public string[] Structures3D;
        public int Stage(double age)
        {
            int count = Stages == null ? 1 : Mathf.Max(1, Stages.Length);
            return Mathf.Clamp((int)(Math.Max(0, age) / Math.Max(1, GrowthSeconds) * (count - 1)), 0, count - 1);
        }

        public bool Mature(double age) => age >= Math.Max(1, GrowthSeconds);
        public bool Accepts(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || SoilBlocks == null)
                return false;
            foreach (string soil in SoilBlocks)
                if (string.Equals(id, soil, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}

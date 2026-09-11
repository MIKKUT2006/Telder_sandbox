
using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Items;
using Game.Save;

namespace Game.Chests
{
    [Serializable]
    public class StructureLootEntryRuntime
    {
        public string ItemId;
        public float Chance = 1f;
        public int MinCount = 1;
        public int MaxCount = 1;
    }

    public static class GeneratedChestLootRuntime
    {
        private sealed class GeneratedChestDefinition
        {
            public string StructureId;
            public int Seed;
            public List<StructureLootEntryRuntime> Loot =
                new List<StructureLootEntryRuntime>();
        }

        private static readonly Dictionary<string, GeneratedChestDefinition>
            definitions = new Dictionary<string, GeneratedChestDefinition>();

        private static readonly object sync = new object();

        public static void Clear()
        {
            lock (sync)
                definitions.Clear();
        }

        public static void Register(
            string dimensionName,
            int worldX,
            int worldY,
            string structureId,
            int seed,
            IList<StructureLootEntryRuntime> loot
        )
        {
            string key = BuildKey(dimensionName, worldX, worldY);

            GeneratedChestDefinition definition =
                new GeneratedChestDefinition
                {
                    StructureId = structureId,
                    Seed = seed
                };

            if (loot != null)
            {
                for (int i = 0; i < loot.Count; i++)
                {
                    StructureLootEntryRuntime source = loot[i];
                    if (source == null)
                        continue;

                    definition.Loot.Add(
                        new StructureLootEntryRuntime
                        {
                            ItemId = source.ItemId,
                            Chance = source.Chance,
                            MinCount = source.MinCount,
                            MaxCount = source.MaxCount
                        }
                    );
                }
            }

            lock (sync)
                definitions[key] = definition;
        }

        public static bool Has(
            string dimensionName,
            int worldX,
            int worldY
        )
        {
            string key = BuildKey(dimensionName, worldX, worldY);
            lock (sync)
                return definitions.ContainsKey(key);
        }

        public static bool TryBuildInitialData(
            string dimensionName,
            int worldX,
            int worldY,
            out ChestSaveData data
        )
        {
            data = null;

            string key = BuildKey(dimensionName, worldX, worldY);
            GeneratedChestDefinition definition;

            lock (sync)
            {
                if (!definitions.TryGetValue(key, out definition))
                    return false;
            }

            data = new ChestSaveData
            {
                X = worldX,
                Y = worldY
            };

            if (definition.Loot == null || definition.Loot.Count == 0)
                return true;

            System.Random random =
                new System.Random(
                    StableHash(
                        definition.Seed,
                        worldX,
                        worldY,
                        definition.StructureId
                    )
                );

            List<ChestSlotSaveData> generated =
                new List<ChestSlotSaveData>();

            for (int i = 0; i < definition.Loot.Count; i++)
            {
                StructureLootEntryRuntime entry = definition.Loot[i];

                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.ItemId))
                    continue;

                if (random.NextDouble() > Mathf.Clamp01(entry.Chance))
                    continue;

                if (!ItemRegistry.TryGet(entry.ItemId, out ItemDefinition item))
                    continue;

                int min = Mathf.Max(1, entry.MinCount);
                int max = Mathf.Max(min, entry.MaxCount);
                int count = random.Next(min, max + 1);
                int maxStack = Mathf.Max(1, item.GetMaxStack());

                while (count > 0 &&
                       generated.Count < ChestInventoryRuntime.SlotCount)
                {
                    int moved = Mathf.Min(maxStack, count);

                    generated.Add(
                        new ChestSlotSaveData
                        {
                            Slot = generated.Count,
                            ItemId = entry.ItemId,
                            Count = moved
                        }
                    );

                    count -= moved;
                }
            }

            List<int> freeSlots = new List<int>();
            for (int i = 0; i < ChestInventoryRuntime.SlotCount; i++)
                freeSlots.Add(i);

            for (int i = 0; i < generated.Count; i++)
            {
                int pick = random.Next(0, freeSlots.Count);
                generated[i].Slot = freeSlots[pick];
                freeSlots.RemoveAt(pick);
                data.Slots.Add(generated[i]);
            }

            return true;
        }

        private static string BuildKey(
            string dimensionName,
            int x,
            int y
        )
        {
            return (dimensionName ?? string.Empty) + "|" + x + "|" + y;
        }

        private static int StableHash(
            int seed,
            int x,
            int y,
            string id
        )
        {
            unchecked
            {
                int hash = seed;
                hash = hash * 397 ^ x;
                hash = hash * 397 ^ y;

                if (id != null)
                {
                    for (int i = 0; i < id.Length; i++)
                        hash = hash * 31 + id[i];
                }

                return hash;
            }
        }
    }
}

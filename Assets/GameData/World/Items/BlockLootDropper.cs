using System.Collections.Generic;
using UnityEngine;
using Game.Blocks;
using Game.Content;

namespace Game.World.Items
{
    /// <summary>One successfully rolled item stack from a block's loot table.</summary>
    public sealed class BlockLootResult
    {
        public string ItemId;
        public int Count;

        public BlockLootResult(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    /// <summary>
    /// Shared block-drop rules for mining, furniture, explosions, tree cascades,
    /// and 3D mining. New Loot entries are independent rolls; legacy Drop fields
    /// remain supported when the JSON does not define a Loot array.
    /// </summary>
    public static class BlockLootDropper
    {
        public static List<BlockLootResult> RollDrops(
            BlockDefinition block,
            string fallbackBlockId)
        {
            var results = new List<BlockLootResult>();

            if (block == null)
            {
                // Preserve furniture's historical fallback when a definition
                // cannot be resolved: drop one copy of the furniture block.
                if (!string.IsNullOrWhiteSpace(fallbackBlockId))
                    results.Add(new BlockLootResult(fallbackBlockId, 1));
                return results;
            }

            bool hasLootTable = block.HasLootTable ||
                (block.Loot != null && block.Loot.Count > 0);

            if (hasLootTable)
            {
                if (block.Loot == null)
                    return results;

                for (int i = 0; i < block.Loot.Count; i++)
                {
                    BlockLootEntryDefinition entry = block.Loot[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                        continue;

                    float chance = Mathf.Clamp01(entry.Chance);
                    if (chance <= 0f || (chance < 1f && UnityEngine.Random.value >= chance))
                        continue;

                    int min = Mathf.Max(0, entry.Min);
                    int max = Mathf.Max(min, entry.Max);
                    int count = min == max ? min : UnityEngine.Random.Range(min, max + 1);

                    if (count > 0)
                        results.Add(new BlockLootResult(entry.ItemId.Trim(), count));
                }

                return results;
            }

            // Backward-compatible single-drop behaviour.
            int legacyCount = Mathf.Max(0, block.DropCount);
            if (legacyCount <= 0)
                return results;

            string legacyId = null;
            if (!string.IsNullOrWhiteSpace(block.Drop.Namespace) &&
                !string.IsNullOrWhiteSpace(block.Drop.Name))
            {
                legacyId = block.Drop.ToString();
            }

            if (string.IsNullOrWhiteSpace(legacyId))
                legacyId = fallbackBlockId;

            if (!string.IsNullOrWhiteSpace(legacyId))
                results.Add(new BlockLootResult(legacyId, legacyCount));

            return results;
        }

        public static int SpawnDrops(
            BlockDefinition block,
            string fallbackBlockId,
            Vector2 position)
        {
            List<BlockLootResult> drops = RollDrops(block, fallbackBlockId);
            if (drops.Count == 0)
                return 0;

            ItemDropSpawner spawner = ItemDropSpawner.EnsureInstance();
            if (spawner == null)
                return 0;

            int spawnedStacks = 0;
            for (int i = 0; i < drops.Count; i++)
            {
                BlockLootResult drop = drops[i];
                if (drop == null || drop.Count <= 0 || string.IsNullOrWhiteSpace(drop.ItemId))
                    continue;

                if (spawner.SpawnFromBlock(drop.ItemId, drop.Count, position))
                    spawnedStacks++;
            }

            return spawnedStacks;
        }
    }
}

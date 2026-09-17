
using UnityEngine;

using Game.Chests;
using Game.Content;
using Game.World.Dimensions;
using Game.World.Generation;

namespace Game.World.Structures
{
    public static class StructureRuntimeSpawner
    {
        public static bool SpawnAheadOfPlayer(
            StructureDefinition structure,
            Transform player,
            int horizontalOffset = 5
        )
        {
            if (structure == null ||
                player == null ||
                WorldManager.Instance == null)
                return false;

            WorldManager manager = WorldManager.Instance;
            WorldGenerator generator = manager.GetGenerator();

            if (generator == null)
                return false;

            int direction =
                player.localScale.x < 0f
                    ? -1
                    : 1;

            int anchorX =
                Mathf.FloorToInt(player.position.x) +
                direction * horizontalOffset;

            int anchorY;

            if (structure.SpawnType ==
                StructureSpawnType.Surface)
            {
                anchorY =
                    generator.GetSurfaceHeight(anchorX) +
                    1;
            }
            else
            {
                anchorY =
                    Mathf.FloorToInt(player.position.y);
            }

            return SpawnAt(
                structure,
                anchorX,
                anchorY
            );
        }

        public static bool SpawnAt(
            StructureDefinition structure,
            int anchorX,
            int anchorY
        )
        {
            if (structure == null ||
                WorldManager.Instance == null)
                return false;

            WorldManager manager =
                WorldManager.Instance;

            string dimensionName =
                DimensionTravelRuntime.Current != null
                    ? DimensionTravelRuntime.Current.Name
                    : string.Empty;

            int seed =
                manager.GetSettings() != null
                    ? manager.GetSettings().Seed
                    : 0;

            StructureInstanceRuntimeIndex.Register(
                seed,
                dimensionName,
                structure,
                0,
                anchorX,
                anchorY
            );

            bool changed = false;

            for (int i = 0;
                 i < structure.Cells.Count;
                 i++)
            {
                StructureCellDefinition cell =
                    structure.Cells[i];

                if (cell == null)
                    continue;

                int worldX =
                    anchorX +
                    cell.X -
                    structure.OriginX;

                int worldY =
                    anchorY +
                    cell.Y -
                    structure.OriginY;

                if (!string.IsNullOrWhiteSpace(
                    cell.ForegroundId))
                {
                    ushort id =
                        ResolveBlockId(
                            cell.ForegroundId
                        );

                    if (id != 0 &&
                        manager.SetBlock(
                            worldX,
                            worldY,
                            id
                        ))
                    {
                        changed = true;

                        if (StructureGenerationRuntime
                            .IsChestBlock(id))
                        {
                            GeneratedChestLootRuntime.Register(
                                dimensionName,
                                worldX,
                                worldY,
                                structure.ID,
                                seed,
                                ConvertLoot(cell.Loot)
                            );
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(
                    cell.BackgroundId))
                {
                    ushort id =
                        ResolveBlockId(
                            cell.BackgroundId
                        );

                    if (id != 0 &&
                        manager.SetBackground(
                            worldX,
                            worldY,
                            id
                        ))
                    {
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static System.Collections.Generic.List<
            StructureLootEntryRuntime
        > ConvertLoot(
            System.Collections.Generic.List<
                StructureLootEntryDefinition
            > source
        )
        {
            var result =
                new System.Collections.Generic.List<
                    StructureLootEntryRuntime
                >();

            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                StructureLootEntryDefinition entry =
                    source[i];

                if (entry == null)
                    continue;

                result.Add(
                    new StructureLootEntryRuntime
                    {
                        ItemId = entry.ItemId,
                        Chance = entry.Chance,
                        MinCount = entry.MinCount,
                        MaxCount = entry.MaxCount
                    }
                );
            }

            return result;
        }

        private static ushort ResolveBlockId(
            string id
        )
        {
            try
            {
                ContentID contentId =
                    ContentID.Parse(id);

                return BlockIDRegistry.GetID(contentId);
            }
            catch
            {
                return 0;
            }
        }
    }
}

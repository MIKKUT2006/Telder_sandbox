
using System;

using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.World.Dimensions;
using Game.World.Items;


namespace Game.World.Structures
{
    public static class StructureCascadeBreakRuntime
    {
        /// <summary>
        /// If the clicked foreground block is a registered Tree trunk,
        /// removes matching structure cells at the cut Y and above.
        ///
        /// Returns true only when this method handled a tree cut.
        /// </summary>
        public static bool TryBreakTreeFromTrunk(
            WorldManager manager,
            World world,
            int cutX,
            int cutY,
            ushort clickedBlockId
        )
        {
            if (
                manager ==
                null
                ||
                world ==
                null
                ||
                clickedBlockId ==
                0
            )
            {
                return false;
            }


            WorldSettings settings =
                manager.GetSettings();


            int seed =
                settings !=
                null
                    ? settings.Seed
                    : 0;


            string dimensionName =
                DimensionTravelRuntime.Current !=
                null
                    ? DimensionTravelRuntime.Current.Name
                    : string.Empty;


            if (
                !StructureInstanceRuntimeIndex
                    .TryGetTreeAtTrunk(
                        seed,
                        dimensionName,
                        cutX,
                        cutY,
                        clickedBlockId,
                        out StructureInstanceRuntimeIndex.TreeInstance tree
                    )
            )
            {
                return false;
            }


            StructureDefinition structure =
                tree.Definition;


            if (
                structure ==
                null
                ||
                structure.Cells ==
                null
            )
            {
                return false;
            }


            bool changed =
                false;


            // Top-down makes the visual collapse feel immediate and also
            // guarantees the clicked trunk is processed only once.
            for (
                int passY = structure.Height - 1;
                passY >= 0;
                passY--
            )
            {
                for (
                    int i = 0;
                    i < structure.Cells.Count;
                    i++
                )
                {
                    StructureCellDefinition cell =
                        structure.Cells[i];


                    if (
                        cell ==
                        null
                        ||
                        cell.Y !=
                            passY
                    )
                    {
                        continue;
                    }


                    int worldX =
                        tree.AnchorX +
                        cell.X -
                        structure.OriginX;


                    int worldY =
                        tree.AnchorY +
                        cell.Y -
                        structure.OriginY;


                    if (
                        worldY <
                        cutY
                    )
                    {
                        continue;
                    }


                    // FOREGROUND
                    if (
                        !string.IsNullOrWhiteSpace(
                            cell.ForegroundId
                        )
                    )
                    {
                        ushort expected =
                            ResolveBlockId(
                                cell.ForegroundId
                            );


                        if (
                            expected !=
                            0
                            &&
                            world.GetBlock(
                                worldX,
                                worldY
                            ) ==
                            expected
                        )
                        {
                            if (
                                manager.SetBlock(
                                    worldX,
                                    worldY,
                                    0
                                )
                            )
                            {
                                SpawnDrop(
                                    expected,
                                    worldX,
                                    worldY
                                );


                                changed =
                                    true;
                            }
                        }
                    }


                    // BACKGROUND belonging to the structure can also be
                    // part of the upper tree art. Only remove it if the
                    // current world still matches the template.
                    if (
                        !string.IsNullOrWhiteSpace(
                            cell.BackgroundId
                        )
                    )
                    {
                        ushort expectedBackground =
                            ResolveBlockId(
                                cell.BackgroundId
                            );


                        if (
                            expectedBackground !=
                            0
                            &&
                            world.GetBackground(
                                worldX,
                                worldY
                            ) ==
                            expectedBackground
                        )
                        {
                            if (
                                manager.SetBackground(
                                    worldX,
                                    worldY,
                                    0
                                )
                            )
                            {
                                SpawnDrop(
                                    expectedBackground,
                                    worldX,
                                    worldY
                                );


                                changed =
                                    true;
                            }
                        }
                    }
                }
            }


            return changed;
        }


        private static void SpawnDrop(
            ushort blockId,
            int worldX,
            int worldY
        )
        {
            if (
                blockId ==
                0
                ||
                ItemDropSpawner.Instance ==
                null
            )
            {
                return;
            }


            try
            {
                ContentID contentId =
                    BlockIDRegistry.GetContentID(
                        blockId
                    );


                if (
                    !BlockRegistry.Contains(
                        contentId
                    )
                )
                {
                    return;
                }


                BlockDefinition block =
                    BlockRegistry.Get(
                        contentId
                    );


                if (
                    block ==
                    null
                )
                {
                    return;
                }


                int count =
                    block.DropCount;


                if (
                    count <=
                    0
                )
                {
                    return;
                }


                string dropId =
                    block.Drop.ToString();


                if (
                    string.IsNullOrWhiteSpace(
                        dropId
                    )
                )
                {
                    dropId =
                        contentId.ToString();
                }


                ItemDropSpawner.Instance.SpawnFromBlock(
                    dropId,
                    count,
                    new Vector2(
                        worldX +
                        0.5f,
                        worldY +
                        0.5f
                    )
                );
            }
            catch
            {
            }
        }


        private static ushort ResolveBlockId(
            string id
        )
        {
            try
            {
                ContentID contentId =
                    ContentID.Parse(
                        id
                    );


                if (
                    !BlockIDRegistry.Contains(
                        contentId
                    )
                )
                {
                    return 0;
                }


                return
                    BlockIDRegistry.GetID(
                        contentId
                    );
            }
            catch
            {
                return 0;
            }
        }
    }
}

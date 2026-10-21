using System;
using System.Collections.Generic;

using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.World.Dimensions;
using Game.World.Furniture;
using Game.World.Items;


namespace Game.World.Structures
{
    /// <summary>
    /// Starts an incremental, wave-like tree collapse.
    ///
    /// Important rules:
    /// - only the connected part of the authored tree at/above the cut is removed;
    /// - cells below the cut stay in the world, forming a stump;
    /// - destruction is spread over frames by StructureCascadeBreakRunner;
    /// - foreground/background/furniture layers are all supported.
    /// </summary>
    public static class StructureCascadeBreakRuntime
    {
        internal sealed class CascadeCell
        {
            public int WorldX;
            public int WorldY;
            public int Wave;

            public ushort ForegroundId;
            public ushort BackgroundId;
            public string FurnitureId;
        }


        internal sealed class CascadePlan
        {
            public string Key;
            public WorldManager Manager;
            public World World;
            public StructureInstanceRuntimeIndex.TreeInstance Tree;
            public List<CascadeCell> Cells;
            public int CutX;
            public int CutY;
        }


        private sealed class Node
        {
            public int X;
            public int Y;
            public ushort ForegroundId;
            public ushort BackgroundId;
            public string FurnitureId;
            public int Wave = -1;
        }


        private static readonly int[,] Neighbours =
        {
            {  0,  1 },
            { -1,  0 },
            {  1,  0 },
            { -1,  1 },
            {  1,  1 },
            { -1, -1 },
            {  1, -1 },
            {  0, -1 }
        };


        public static bool TryBreakTreeFromTrunk(
            WorldManager manager,
            World world,
            int cutX,
            int cutY,
            ushort clickedBlockId
        )
        {
            return TryBreakTreeFromTrunk(
                manager,
                world,
                cutX,
                cutY,
                clickedBlockId,
                false
            );
        }


        public static bool TryBreakTreeFromTrunk(
            WorldManager manager,
            World world,
            int cutX,
            int cutY,
            ushort clickedBlockId,
            bool background
        )
        {
            if (
                manager == null ||
                world == null ||
                clickedBlockId == 0
            )
            {
                return false;
            }

            GetWorldIdentity(
                manager,
                out int seed,
                out string dimensionName
            );

            if (
                !StructureInstanceRuntimeIndex.TryGetTreeAtTrunk(
                    seed,
                    dimensionName,
                    cutX,
                    cutY,
                    clickedBlockId,
                    background,
                    out StructureInstanceRuntimeIndex.TreeInstance tree
                )
            )
            {
                return false;
            }

            return ScheduleTreeCollapse(
                manager,
                world,
                tree,
                cutX,
                cutY
            );
        }


        public static bool TryBreakTreeFromFurnitureTrunk(
            WorldManager manager,
            World world,
            int cutX,
            int cutY,
            string clickedFurnitureId
        )
        {
            if (
                manager == null ||
                world == null ||
                string.IsNullOrWhiteSpace(clickedFurnitureId)
            )
            {
                return false;
            }

            GetWorldIdentity(
                manager,
                out int seed,
                out string dimensionName
            );

            if (
                !StructureInstanceRuntimeIndex.TryGetTreeAtFurnitureTrunk(
                    seed,
                    dimensionName,
                    cutX,
                    cutY,
                    clickedFurnitureId,
                    out StructureInstanceRuntimeIndex.TreeInstance tree
                )
            )
            {
                return false;
            }

            return ScheduleTreeCollapse(
                manager,
                world,
                tree,
                cutX,
                cutY
            );
        }


        private static bool ScheduleTreeCollapse(
            WorldManager manager,
            World world,
            StructureInstanceRuntimeIndex.TreeInstance tree,
            int cutX,
            int cutY
        )
        {
            CascadePlan plan =
                BuildPlan(
                    manager,
                    world,
                    tree,
                    cutX,
                    cutY
                );

            if (
                plan == null ||
                plan.Cells == null ||
                plan.Cells.Count == 0
            )
            {
                return false;
            }

            return StructureCascadeBreakRunner.Schedule(
                plan
            );
        }


        /// <summary>
        /// O(N) plan build. V9.2 used Height * CellCount nested passes and then
        /// destroyed the complete tree synchronously. Here every authored cell is
        /// visited once, indexed by coordinate, and a BFS creates the wave order.
        /// </summary>
        private static CascadePlan BuildPlan(
            WorldManager manager,
            World world,
            StructureInstanceRuntimeIndex.TreeInstance tree,
            int cutX,
            int cutY
        )
        {
            if (
                manager == null ||
                world == null ||
                tree == null ||
                tree.Definition == null ||
                tree.Definition.Cells == null
            )
            {
                return null;
            }

            StructureDefinition structure =
                tree.Definition;

            StructureLayerMetadata layerMetadata =
                StructureLayerMetadataStore.Load(
                    structure.ID
                );

            FurnitureLayerManager furniture =
                FurnitureLayerManager.Instance;

            Dictionary<long, Node> nodes =
                new Dictionary<long, Node>(
                    structure.Cells.Count
                );

            Node startNode = null;

            for (
                int i = 0;
                i < structure.Cells.Count;
                i++
            )
            {
                StructureCellDefinition cell =
                    structure.Cells[i];

                if (cell == null)
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

                // The requested stump rule: never destroy anything below the
                // actually mined block, regardless of the structure origin.
                if (worldY < cutY)
                {
                    continue;
                }

                StructureLayerCellMetadata metadata =
                    layerMetadata != null
                        ? layerMetadata.Get(
                            cell.X,
                            cell.Y
                        )
                        : null;

                Node node =
                    new Node
                    {
                        X = worldX,
                        Y = worldY,
                        ForegroundId = ResolveBlockId(cell.ForegroundId),
                        BackgroundId = ResolveBlockId(cell.BackgroundId),
                        FurnitureId =
                            metadata != null
                                ? metadata.FurnitureId
                                : null
                    };

                if (!HasAnyLayer(node))
                {
                    continue;
                }

                // Only live pieces participate in connectivity. If a branch was
                // already removed earlier, the cascade cannot jump through that
                // empty gap to another disconnected part.
                if (!IsLive(node, world, furniture))
                {
                    continue;
                }

                long key =
                    Pack(
                        worldX,
                        worldY
                    );

                nodes[key] = node;

                if (
                    worldX == cutX &&
                    worldY == cutY
                )
                {
                    startNode = node;
                }
            }

            if (startNode == null)
            {
                return null;
            }

            Queue<Node> queue =
                new Queue<Node>();

            startNode.Wave = 0;
            queue.Enqueue(startNode);

            List<Node> reachable =
                new List<Node>(nodes.Count);

            while (queue.Count > 0)
            {
                Node current =
                    queue.Dequeue();

                reachable.Add(current);

                for (
                    int i = 0;
                    i < Neighbours.GetLength(0);
                    i++
                )
                {
                    int nextX =
                        current.X +
                        Neighbours[i, 0];

                    int nextY =
                        current.Y +
                        Neighbours[i, 1];

                    if (nextY < cutY)
                    {
                        continue;
                    }

                    if (
                        !nodes.TryGetValue(
                            Pack(nextX, nextY),
                            out Node next
                        ) ||
                        next == null ||
                        next.Wave >= 0
                    )
                    {
                        continue;
                    }

                    next.Wave =
                        current.Wave + 1;

                    queue.Enqueue(next);
                }
            }

            if (reachable.Count == 0)
            {
                return null;
            }

            // Stable wave order. Within one BFS distance, prefer lower/closer
            // cells so the collapse visually grows from the cut instead of
            // teleporting between distant branch tips.
            reachable.Sort(
                delegate(Node a, Node b)
                {
                    int waveCompare =
                        a.Wave.CompareTo(b.Wave);

                    if (waveCompare != 0)
                    {
                        return waveCompare;
                    }

                    int yCompare =
                        a.Y.CompareTo(b.Y);

                    if (yCompare != 0)
                    {
                        return yCompare;
                    }

                    int ax =
                        Mathf.Abs(a.X - cutX);

                    int bx =
                        Mathf.Abs(b.X - cutX);

                    int distanceCompare =
                        ax.CompareTo(bx);

                    if (distanceCompare != 0)
                    {
                        return distanceCompare;
                    }

                    return a.X.CompareTo(b.X);
                }
            );

            List<CascadeCell> cells =
                new List<CascadeCell>(
                    reachable.Count
                );

            for (
                int i = 0;
                i < reachable.Count;
                i++
            )
            {
                Node node = reachable[i];

                cells.Add(
                    new CascadeCell
                    {
                        WorldX = node.X,
                        WorldY = node.Y,
                        Wave = node.Wave,
                        ForegroundId = node.ForegroundId,
                        BackgroundId = node.BackgroundId,
                        FurnitureId = node.FurnitureId
                    }
                );
            }

            // One active collapse per generated tree instance. A second mining
            // completion while the crown is still falling must not start another
            // traversal of the same structure.
            string planKey =
                (tree.DimensionName ?? string.Empty) +
                "|" +
                (structure.ID ?? string.Empty) +
                "|" +
                tree.AnchorX +
                "|" +
                tree.AnchorY;

            return
                new CascadePlan
                {
                    Key = planKey,
                    Manager = manager,
                    World = world,
                    Tree = tree,
                    Cells = cells,
                    CutX = cutX,
                    CutY = cutY
                };
        }


        private static bool HasAnyLayer(
            Node node
        )
        {
            return
                node != null &&
                (
                    node.ForegroundId != 0 ||
                    node.BackgroundId != 0 ||
                    !string.IsNullOrWhiteSpace(
                        node.FurnitureId
                    )
                );
        }


        private static bool IsLive(
            Node node,
            World world,
            FurnitureLayerManager furniture
        )
        {
            if (
                node == null ||
                world == null
            )
            {
                return false;
            }

            if (
                node.ForegroundId != 0 &&
                world.GetBlock(
                    node.X,
                    node.Y
                ) == node.ForegroundId
            )
            {
                return true;
            }

            if (
                node.BackgroundId != 0 &&
                world.GetBackground(
                    node.X,
                    node.Y
                ) == node.BackgroundId
            )
            {
                return true;
            }

            if (
                !string.IsNullOrWhiteSpace(
                    node.FurnitureId
                )
            )
            {
                if (furniture == null)
                {
                    furniture =
                        FurnitureLayerManager.EnsureInstance();
                }

                if (
                    furniture != null &&
                    string.Equals(
                        furniture.GetFurniture(
                            node.X,
                            node.Y
                        ),
                        node.FurnitureId,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }


        internal static bool BreakCell(
            CascadePlan plan,
            CascadeCell cell,
            FurnitureLayerManager furniture,
            out bool foregroundChanged,
            out bool furnitureChanged
        )
        {
            foregroundChanged = false;
            furnitureChanged = false;

            if (
                plan == null ||
                cell == null ||
                plan.Manager == null ||
                plan.World == null
            )
            {
                return false;
            }

            bool changed = false;

            if (
                cell.ForegroundId != 0 &&
                plan.World.GetBlock(
                    cell.WorldX,
                    cell.WorldY
                ) == cell.ForegroundId
            )
            {
                if (
                    plan.Manager.SetBlockForCascade(
                        cell.WorldX,
                        cell.WorldY,
                        0
                    )
                )
                {
                    SpawnDrop(
                        cell.ForegroundId,
                        cell.WorldX,
                        cell.WorldY
                    );

                    foregroundChanged = true;
                    changed = true;
                }
            }

            if (
                cell.BackgroundId != 0 &&
                plan.World.GetBackground(
                    cell.WorldX,
                    cell.WorldY
                ) == cell.BackgroundId
            )
            {
                if (
                    plan.Manager.SetBackground(
                        cell.WorldX,
                        cell.WorldY,
                        0
                    )
                )
                {
                    SpawnDrop(
                        cell.BackgroundId,
                        cell.WorldX,
                        cell.WorldY
                    );

                    changed = true;
                }
            }

            if (
                !string.IsNullOrWhiteSpace(
                    cell.FurnitureId
                )
            )
            {
                if (furniture == null)
                {
                    furniture =
                        FurnitureLayerManager.EnsureInstance();
                }

                if (
                    furniture != null &&
                    string.Equals(
                        furniture.GetFurniture(
                            cell.WorldX,
                            cell.WorldY
                        ),
                        cell.FurnitureId,
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    furniture.BreakFurnitureDeferred(
                        cell.WorldX,
                        cell.WorldY
                    )
                )
                {
                    furnitureChanged = true;
                    changed = true;
                }
            }

            return changed;
        }


        private static void GetWorldIdentity(
            WorldManager manager,
            out int seed,
            out string dimensionName
        )
        {
            WorldSettings settings =
                manager != null
                    ? manager.GetSettings()
                    : null;

            seed =
                settings != null
                    ? settings.Seed
                    : 0;

            dimensionName =
                DimensionTravelRuntime.Current != null
                    ? DimensionTravelRuntime.Current.Name
                    : string.Empty;
        }


        private static void SpawnDrop(
            ushort blockId,
            int worldX,
            int worldY
        )
        {
            if (blockId == 0)
                return;

            try
            {
                ContentID contentId = BlockIDRegistry.GetContentID(blockId);
                if (!BlockRegistry.Contains(contentId))
                    return;

                BlockDefinition block = BlockRegistry.Get(contentId);
                if (block == null)
                    return;

                BlockLootDropper.SpawnDrops(
                    block,
                    contentId.ToString(),
                    new Vector2(worldX + 0.5f, worldY + 0.5f));
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("TREE CASCADE: Could not resolve block loot: " + exception.Message);
            }
        }

        private static ushort ResolveBlockId(
            string id
        )
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return 0;
            }

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


        private static long Pack(
            int x,
            int y
        )
        {
            unchecked
            {
                return
                    ((long)x << 32) ^
                    (uint)y;
            }
        }
    }
}

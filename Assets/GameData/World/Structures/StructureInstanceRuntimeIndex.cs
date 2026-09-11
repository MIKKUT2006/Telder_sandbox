
using System;
using System.Collections.Generic;

using Game.Content;
using Game.World.Dimensions;


namespace Game.World.Structures
{
    /// <summary>
    /// Runtime spatial index for generated structures that need
    /// behaviour after world generation.
    ///
    /// At the moment only Tree structures are indexed.
    /// </summary>
    public static class StructureInstanceRuntimeIndex
    {
        internal sealed class TreeInstance
        {
            public int WorldSeed;
            public string DimensionName;

            public StructureDefinition Definition;

            public int AnchorX;
            public int AnchorY;

            public string TrunkForegroundId;
            public ushort TrunkBlockId;
        }


        private static readonly Dictionary<
            string,
            TreeInstance
        > instances =
            new Dictionary<
                string,
                TreeInstance
            >();


        private static readonly Dictionary<
            string,
            List<TreeInstance>
        > trunkCells =
            new Dictionary<
                string,
                List<TreeInstance>
            >();


        public static void Register(
            int worldSeed,
            string dimensionName,
            StructureDefinition structure,
            int regionX,
            int anchorX,
            int anchorY
        )
        {
            if (
                structure == null
                ||
                structure.Type !=
                    StructureType.Tree
                ||
                structure.Cells == null
                ||
                structure.Cells.Count == 0
            )
            {
                return;
            }


            string key =
                MakeInstanceKey(
                    worldSeed,
                    dimensionName,
                    structure.ID,
                    regionX,
                    anchorX,
                    anchorY
                );


            if (
                instances.ContainsKey(
                    key
                )
            )
            {
                return;
            }


            string trunkId =
                FindTrunkForegroundId(
                    structure
                );


            if (
                string.IsNullOrWhiteSpace(
                    trunkId
                )
            )
            {
                return;
            }


            ushort trunkBlockId =
                ResolveBlockId(
                    trunkId
                );


            if (
                trunkBlockId ==
                0
            )
            {
                return;
            }


            TreeInstance instance =
                new TreeInstance
                {
                    WorldSeed =
                        worldSeed,

                    DimensionName =
                        dimensionName ??
                        string.Empty,

                    Definition =
                        structure,

                    AnchorX =
                        anchorX,

                    AnchorY =
                        anchorY,

                    TrunkForegroundId =
                        trunkId,

                    TrunkBlockId =
                        trunkBlockId
                };


            instances.Add(
                key,
                instance
            );


            // Only actual trunk cells are indexed as cut points.
            // Branches/leaves remain ordinary breakable blocks.
            for (
                int i = 0;
                i < structure.Cells.Count;
                i++
            )
            {
                StructureCellDefinition cell =
                    structure.Cells[i];


                if (
                    cell == null
                    ||
                    cell.X !=
                        structure.OriginX
                    ||
                    !string.Equals(
                        cell.ForegroundId,
                        trunkId,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }


                int worldX =
                    anchorX +
                    cell.X -
                    structure.OriginX;


                int worldY =
                    anchorY +
                    cell.Y -
                    structure.OriginY;


                string cellKey =
                    MakeCellKey(
                        worldSeed,
                        dimensionName,
                        worldX,
                        worldY
                    );


                if (
                    !trunkCells.TryGetValue(
                        cellKey,
                        out List<TreeInstance> list
                    )
                )
                {
                    list =
                        new List<TreeInstance>();


                    trunkCells.Add(
                        cellKey,
                        list
                    );
                }


                list.Add(
                    instance
                );
            }
        }


        internal static bool TryGetTreeAtTrunk(
            int worldSeed,
            string dimensionName,
            int worldX,
            int worldY,
            ushort currentBlockId,
            out TreeInstance instance
        )
        {
            instance =
                null;


            string key =
                MakeCellKey(
                    worldSeed,
                    dimensionName,
                    worldX,
                    worldY
                );


            if (
                !trunkCells.TryGetValue(
                    key,
                    out List<TreeInstance> list
                )
            )
            {
                return false;
            }


            for (
                int i = 0;
                i < list.Count;
                i++
            )
            {
                TreeInstance candidate =
                    list[i];


                if (
                    candidate == null
                    ||
                    candidate.Definition ==
                        null
                    ||
                    candidate.TrunkBlockId !=
                        currentBlockId
                )
                {
                    continue;
                }


                instance =
                    candidate;


                return true;
            }


            return false;
        }


        private static string FindTrunkForegroundId(
            StructureDefinition structure
        )
        {
            // Prefer the exact Origin cell.
            for (
                int i = 0;
                i < structure.Cells.Count;
                i++
            )
            {
                StructureCellDefinition cell =
                    structure.Cells[i];


                if (
                    cell == null
                    ||
                    cell.X !=
                        structure.OriginX
                    ||
                    cell.Y !=
                        structure.OriginY
                    ||
                    string.IsNullOrWhiteSpace(
                        cell.ForegroundId
                    )
                )
                {
                    continue;
                }


                return
                    cell.ForegroundId;
            }


            // Fallback: lowest foreground block in the OriginX
            // column is considered the trunk material.
            StructureCellDefinition lowest =
                null;


            for (
                int i = 0;
                i < structure.Cells.Count;
                i++
            )
            {
                StructureCellDefinition cell =
                    structure.Cells[i];


                if (
                    cell == null
                    ||
                    cell.X !=
                        structure.OriginX
                    ||
                    string.IsNullOrWhiteSpace(
                        cell.ForegroundId
                    )
                )
                {
                    continue;
                }


                if (
                    lowest ==
                    null
                    ||
                    cell.Y <
                        lowest.Y
                )
                {
                    lowest =
                        cell;
                }
            }


            return
                lowest !=
                null
                    ? lowest.ForegroundId
                    : null;
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


        private static string MakeInstanceKey(
            int worldSeed,
            string dimensionName,
            string structureId,
            int regionX,
            int anchorX,
            int anchorY
        )
        {
            return
                worldSeed +
                "|" +
                (
                    dimensionName ??
                    string.Empty
                ) +
                "|" +
                (
                    structureId ??
                    string.Empty
                ) +
                "|" +
                regionX +
                "|" +
                anchorX +
                "|" +
                anchorY;
        }


        private static string MakeCellKey(
            int worldSeed,
            string dimensionName,
            int worldX,
            int worldY
        )
        {
            return
                worldSeed +
                "|" +
                (
                    dimensionName ??
                    string.Empty
                ) +
                "|" +
                worldX +
                "|" +
                worldY;
        }
    }
}

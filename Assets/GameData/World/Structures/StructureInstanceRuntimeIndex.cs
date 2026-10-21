using System;
using System.Collections.Generic;

using Game.Content;

namespace Game.World.Structures
{
    /// <summary>
    /// Runtime spatial index for generated structures that need behaviour after generation.
    ///
    /// Tree trunks can live on foreground, background or furniture layers. The tree's
    /// sidecar .layersdata file is authoritative for the furniture layer.
    /// </summary>
    public static class StructureInstanceRuntimeIndex
    {
        internal enum TreeCellLayer
        {
            Foreground = 0,
            Background = 1,
            Furniture = 2
        }


        internal sealed class TreeInstance
        {
            public int WorldSeed;
            public string DimensionName;
            public StructureDefinition Definition;
            public int AnchorX;
            public int AnchorY;
            public string TrunkId;
        }


        private sealed class TrunkCellBinding
        {
            public TreeInstance Instance;
            public TreeCellLayer Layer;
            public string BlockId;
            public ushort RuntimeBlockId;
        }


        private static readonly Dictionary<string, TreeInstance> instances =
            new Dictionary<string, TreeInstance>();

        private static readonly Dictionary<string, List<TrunkCellBinding>> trunkCells =
            new Dictionary<string, List<TrunkCellBinding>>();


        public static void Register(
            int worldSeed,
            string dimensionName,
            StructureDefinition structure,
            int regionX,
            int anchorX,
            int anchorY)
        {
            if (structure == null ||
                structure.Type != StructureType.Tree ||
                structure.Cells == null ||
                structure.Cells.Count == 0)
            {
                return;
            }

            string key = MakeInstanceKey(
                worldSeed,
                dimensionName,
                structure.ID,
                regionX,
                anchorX,
                anchorY);

            if (instances.ContainsKey(key))
                return;

            StructureLayerMetadata layerMetadata =
                StructureLayerMetadataStore.Load(structure.ID);

            if (!TryFindTrunk(
                    structure,
                    layerMetadata,
                    out string trunkId))
            {
                return;
            }

            ushort trunkBlockId = ResolveBlockId(trunkId);
            if (trunkBlockId == 0)
                return;

            TreeInstance instance = new TreeInstance
            {
                WorldSeed = worldSeed,
                DimensionName = dimensionName ?? string.Empty,
                Definition = structure,
                AnchorX = anchorX,
                AnchorY = anchorY,
                TrunkId = trunkId
            };

            instances.Add(key, instance);

            // Index every matching trunk cell in the origin column, regardless of
            // whether that cell is stored on foreground, background or furniture.
            // This lets a tree freely mix layers without breaking cascade chopping.
            for (int i = 0; i < structure.Cells.Count; i++)
            {
                StructureCellDefinition cell = structure.Cells[i];
                if (cell == null || cell.X != structure.OriginX)
                    continue;

                int worldX = anchorX + cell.X - structure.OriginX;
                int worldY = anchorY + cell.Y - structure.OriginY;

                AddTrunkBindingIfMatches(
                    worldSeed,
                    dimensionName,
                    worldX,
                    worldY,
                    instance,
                    TreeCellLayer.Foreground,
                    cell.ForegroundId,
                    trunkId,
                    trunkBlockId);

                AddTrunkBindingIfMatches(
                    worldSeed,
                    dimensionName,
                    worldX,
                    worldY,
                    instance,
                    TreeCellLayer.Background,
                    cell.BackgroundId,
                    trunkId,
                    trunkBlockId);

                StructureLayerCellMetadata metadata =
                    layerMetadata != null
                        ? layerMetadata.Get(cell.X, cell.Y)
                        : null;

                AddTrunkBindingIfMatches(
                    worldSeed,
                    dimensionName,
                    worldX,
                    worldY,
                    instance,
                    TreeCellLayer.Furniture,
                    metadata != null ? metadata.FurnitureId : null,
                    trunkId,
                    trunkBlockId);
            }
        }


        internal static bool TryGetTreeAtTrunk(
            int worldSeed,
            string dimensionName,
            int worldX,
            int worldY,
            ushort currentBlockId,
            bool background,
            out TreeInstance instance)
        {
            return TryGetTreeAtBlockTrunk(
                worldSeed,
                dimensionName,
                worldX,
                worldY,
                currentBlockId,
                background
                    ? TreeCellLayer.Background
                    : TreeCellLayer.Foreground,
                out instance);
        }


        // Compatibility overload for old callers: foreground only.
        internal static bool TryGetTreeAtTrunk(
            int worldSeed,
            string dimensionName,
            int worldX,
            int worldY,
            ushort currentBlockId,
            out TreeInstance instance)
        {
            return TryGetTreeAtTrunk(
                worldSeed,
                dimensionName,
                worldX,
                worldY,
                currentBlockId,
                false,
                out instance);
        }


        internal static bool TryGetTreeAtFurnitureTrunk(
            int worldSeed,
            string dimensionName,
            int worldX,
            int worldY,
            string currentBlockId,
            out TreeInstance instance)
        {
            instance = null;

            if (string.IsNullOrWhiteSpace(currentBlockId))
                return false;

            string key = MakeCellKey(
                worldSeed,
                dimensionName,
                worldX,
                worldY);

            if (!trunkCells.TryGetValue(key, out List<TrunkCellBinding> list))
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                TrunkCellBinding binding = list[i];

                if (binding == null ||
                    binding.Instance == null ||
                    binding.Instance.Definition == null ||
                    binding.Layer != TreeCellLayer.Furniture ||
                    !string.Equals(
                        binding.BlockId,
                        currentBlockId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                instance = binding.Instance;
                return true;
            }

            return false;
        }


        private static bool TryGetTreeAtBlockTrunk(
            int worldSeed,
            string dimensionName,
            int worldX,
            int worldY,
            ushort currentBlockId,
            TreeCellLayer layer,
            out TreeInstance instance)
        {
            instance = null;

            if (currentBlockId == 0)
                return false;

            string key = MakeCellKey(
                worldSeed,
                dimensionName,
                worldX,
                worldY);

            if (!trunkCells.TryGetValue(key, out List<TrunkCellBinding> list))
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                TrunkCellBinding binding = list[i];

                if (binding == null ||
                    binding.Instance == null ||
                    binding.Instance.Definition == null ||
                    binding.Layer != layer ||
                    binding.RuntimeBlockId != currentBlockId)
                {
                    continue;
                }

                instance = binding.Instance;
                return true;
            }

            return false;
        }


        private static void AddTrunkBindingIfMatches(
            int worldSeed,
            string dimensionName,
            int worldX,
            int worldY,
            TreeInstance instance,
            TreeCellLayer layer,
            string cellId,
            string trunkId,
            ushort trunkBlockId)
        {
            if (string.IsNullOrWhiteSpace(cellId) ||
                !string.Equals(
                    cellId,
                    trunkId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string cellKey = MakeCellKey(
                worldSeed,
                dimensionName,
                worldX,
                worldY);

            if (!trunkCells.TryGetValue(cellKey, out List<TrunkCellBinding> list))
            {
                list = new List<TrunkCellBinding>();
                trunkCells.Add(cellKey, list);
            }

            list.Add(
                new TrunkCellBinding
                {
                    Instance = instance,
                    Layer = layer,
                    BlockId = cellId,
                    RuntimeBlockId = trunkBlockId
                });
        }


        private static bool TryFindTrunk(
            StructureDefinition structure,
            StructureLayerMetadata layerMetadata,
            out string trunkId)
        {
            trunkId = null;

            // Prefer the exact origin cell. Existing structures used foreground or
            // background; V9.2 also allows the root trunk itself to be furniture.
            for (int i = 0; i < structure.Cells.Count; i++)
            {
                StructureCellDefinition cell = structure.Cells[i];
                if (cell == null ||
                    cell.X != structure.OriginX ||
                    cell.Y != structure.OriginY)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(cell.ForegroundId))
                {
                    trunkId = cell.ForegroundId;
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(cell.BackgroundId))
                {
                    trunkId = cell.BackgroundId;
                    return true;
                }

                StructureLayerCellMetadata metadata =
                    layerMetadata != null
                        ? layerMetadata.Get(cell.X, cell.Y)
                        : null;

                if (metadata != null &&
                    !string.IsNullOrWhiteSpace(metadata.FurnitureId))
                {
                    trunkId = metadata.FurnitureId;
                    return true;
                }
            }

            // Fallback: find the lowest occupied cell in the origin column. Keep
            // foreground/background priority for backwards compatibility, then
            // use furniture if the tree is authored entirely on that layer.
            StructureCellDefinition lowestForeground = null;
            StructureCellDefinition lowestBackground = null;
            StructureCellDefinition lowestFurnitureCell = null;
            string lowestFurnitureId = null;

            for (int i = 0; i < structure.Cells.Count; i++)
            {
                StructureCellDefinition cell = structure.Cells[i];
                if (cell == null || cell.X != structure.OriginX)
                    continue;

                if (!string.IsNullOrWhiteSpace(cell.ForegroundId) &&
                    (lowestForeground == null || cell.Y < lowestForeground.Y))
                {
                    lowestForeground = cell;
                }

                if (!string.IsNullOrWhiteSpace(cell.BackgroundId) &&
                    (lowestBackground == null || cell.Y < lowestBackground.Y))
                {
                    lowestBackground = cell;
                }

                StructureLayerCellMetadata metadata =
                    layerMetadata != null
                        ? layerMetadata.Get(cell.X, cell.Y)
                        : null;

                if (metadata != null &&
                    !string.IsNullOrWhiteSpace(metadata.FurnitureId) &&
                    (lowestFurnitureCell == null || cell.Y < lowestFurnitureCell.Y))
                {
                    lowestFurnitureCell = cell;
                    lowestFurnitureId = metadata.FurnitureId;
                }
            }

            if (lowestForeground != null)
            {
                trunkId = lowestForeground.ForegroundId;
                return true;
            }

            if (lowestBackground != null)
            {
                trunkId = lowestBackground.BackgroundId;
                return true;
            }

            if (lowestFurnitureCell != null &&
                !string.IsNullOrWhiteSpace(lowestFurnitureId))
            {
                trunkId = lowestFurnitureId;
                return true;
            }

            return false;
        }


        private static ushort ResolveBlockId(string id)
        {
            try
            {
                ContentID contentId = ContentID.Parse(id);
                if (!BlockIDRegistry.Contains(contentId))
                    return 0;
                return BlockIDRegistry.GetID(contentId);
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
            int anchorY)
        {
            return worldSeed + "|" +
                   (dimensionName ?? string.Empty) + "|" +
                   (structureId ?? string.Empty) + "|" +
                   regionX + "|" + anchorX + "|" + anchorY;
        }


        private static string MakeCellKey(
            int worldSeed,
            string dimensionName,
            int worldX,
            int worldY)
        {
            return worldSeed + "|" +
                   (dimensionName ?? string.Empty) + "|" +
                   worldX + "|" + worldY;
        }
    }
}

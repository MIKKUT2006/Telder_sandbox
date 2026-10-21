
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.World.Structures
{
    public static class StructureRegistry
    {
        private static readonly List<StructureDefinition>
            structures =
                new List<StructureDefinition>();

        private static bool loaded;

        public static IReadOnlyList<StructureDefinition> GetAll()
        {
            EnsureLoaded();
            return structures;
        }

        public static StructureDefinition Get(string id)
        {
            EnsureLoaded();

            for (int i = 0; i < structures.Count; i++)
            {
                StructureDefinition structure = structures[i];

                if (structure != null &&
                    string.Equals(
                        structure.ID,
                        id,
                        StringComparison.OrdinalIgnoreCase))
                    return structure;
            }

            return null;
        }

        public static void Reload()
        {
            loaded = false;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (loaded)
                return;

            loaded = true;
            structures.Clear();

            string folder = StructurePaths.Folder;

            if (!Directory.Exists(folder))
                return;

            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    StructureDefinition definition =
                        JsonUtility.FromJson<StructureDefinition>(
                            File.ReadAllText(files[i])
                        );

                    if (definition == null ||
                        string.IsNullOrWhiteSpace(definition.ID))
                        continue;

                    Normalize(definition);
                    MergeLegacyLayerMetadata(definition);
                    structures.Add(definition);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        "STRUCTURE: Failed to load " +
                        files[i] +
                        "\n" +
                        exception
                    );
                }
            }

            Debug.Log(
                "STRUCTURES LOADED: " +
                structures.Count
            );
        }

        private static void MergeLegacyLayerMetadata(StructureDefinition structure)
        {
            // Legacy .layersdata entries override the equivalent JSON fields.
            // All data is materialized when definitions load, so background
            // generation never performs sidecar disk I/O per chunk/cell.
            StructureLayerMetadata layers = StructureLayerMetadataStore.Load(structure.ID);
            for (int i = 0; i < structure.Cells.Count; i++)
            {
                StructureCellDefinition cell = structure.Cells[i];
                if (cell == null)
                    continue;
                StructureLayerCellMetadata layer = layers.Get(cell.X, cell.Y);
                if (layer == null)
                    continue;
                cell.ForegroundTransform = layer.ForegroundTransform;
                cell.BackgroundTransform = layer.BackgroundTransform;
                cell.FurnitureTransform = layer.FurnitureTransform;
                cell.FurnitureId = layer.FurnitureId;
                cell.Transform = layer.ForegroundTransform;
            }
        }

        private static void Normalize(
            StructureDefinition structure
        )
        {
            structure.Width = Mathf.Max(1, structure.Width);
            structure.Height = Mathf.Max(1, structure.Height);
            structure.Depth = Mathf.Max(1, structure.Depth);
            structure.RegionSize = Mathf.Max(8, structure.RegionSize);
            structure.SpawnChance =
                Mathf.Clamp01(structure.SpawnChance);

            if (structure.Cells == null)
                structure.Cells =
                    new List<StructureCellDefinition>();

            if (structure.Biomes == null)
                structure.Biomes =
                    new List<string>();

            for (int i = 0; i < structure.Cells.Count; i++)
            {
                StructureCellDefinition cell =
                    structure.Cells[i];

                if (cell != null && cell.Loot == null)
                    cell.Loot =
                        new List<StructureLootEntryDefinition>();
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Game.Blocks;
using Game.Chests;
using Game.Content;
using Game.World.Biomes;
using Game.World.Biomes.Caves;
using Game.World.Dimensions;
using Game.World.Generation;

namespace Game.World.Structures
{
    public static class StructureGenerationRuntime
    {
        private static MethodInfo generateForegroundMethod;
        private static FieldInfo biomeRuntimeField;
        private static MethodInfo biomeRuntimeGetMethod;

        public static void ApplyToChunk(
            WorldGenerator generator,
            WorldSettings settings,
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            if (generator == null ||
                settings == null ||
                data == null)
                return;

            IReadOnlyList<StructureDefinition> structures =
                StructureRegistry.GetAll();

            if (structures == null || structures.Count == 0)
                return;

            int chunkMinX = chunkX * Chunk.SizeX;
            int chunkMaxX = chunkMinX + Chunk.SizeX - 1;

            for (int s = 0; s < structures.Count; s++)
            {
                StructureDefinition structure = structures[s];

                if (structure == null ||
                    structure.Cells == null ||
                    structure.Cells.Count == 0)
                    continue;

                int regionSize = Mathf.Max(8, structure.RegionSize);
                int extra = Mathf.Max(structure.Width, 1) + 2;

                int minRegion =
                    FloorDiv(
                        chunkMinX - extra,
                        regionSize
                    );

                int maxRegion =
                    FloorDiv(
                        chunkMaxX + extra,
                        regionSize
                    );

                for (int regionX = minRegion;
                     regionX <= maxRegion;
                     regionX++)
                {
                    if (!TryGetCandidate(
                        generator,
                        settings,
                        structure,
                        regionX,
                        out int anchorX,
                        out int anchorY))
                        continue;

                    if (!CandidateCanTouchChunk(
                        structure,
                        anchorX,
                        chunkMinX,
                        chunkMaxX))
                        continue;

                    StampCandidateIntoChunk(
                        settings,
                        data,
                        chunkX,
                        chunkY,
                        structure,
                        regionX,
                        anchorX,
                        anchorY
                    );
                }
            }
        }

        private static bool TryGetCandidate(
            WorldGenerator generator,
            WorldSettings settings,
            StructureDefinition structure,
            int regionX,
            out int anchorX,
            out int anchorY
        )
        {
            anchorX = 0;
            anchorY = 0;

            int seed =
                StableHash(
                    settings.Seed,
                    structure.ID,
                    regionX
                );

            System.Random random =
                new System.Random(seed);

            if (random.NextDouble() >
                Mathf.Clamp01(structure.SpawnChance))
                return false;

            int regionSize =
                Mathf.Max(8, structure.RegionSize);

            anchorX =
                regionX * regionSize +
                random.Next(0, regionSize);

            int surface =
                generator.GetSurfaceHeight(anchorX);

            switch (structure.SpawnType)
            {
                case StructureSpawnType.Surface:
                    anchorY = surface + 1;
                    break;

                case StructureSpawnType.Any:
                    anchorY =
                        PickY(
                            settings,
                            structure,
                            random,
                            surface,
                            false
                        );
                    break;

                default:
                    anchorY =
                        PickY(
                            settings,
                            structure,
                            random,
                            surface,
                            true
                        );
                    break;
            }

            if (structure.UseHeightRange)
            {
                int min = Mathf.Min(
                    structure.MinY,
                    structure.MaxY
                );

                int max = Mathf.Max(
                    structure.MinY,
                    structure.MaxY
                );

                if (anchorY < min || anchorY > max)
                    return false;
            }

            if (
                !BiomeAllowed(
                    generator,
                    settings,
                    structure,
                    anchorX,
                    anchorY
                )
            )
            {
                return false;
            }

            if (structure.RequireFreeSpace &&
                !HasRequiredFreeSpace(
                    generator,
                    structure,
                    anchorX,
                    anchorY))
                return false;

            return true;
        }

        private static int PickY(
            WorldSettings settings,
            StructureDefinition structure,
            System.Random random,
            int surface,
            bool underground
        )
        {
            int min;
            int max;

            if (structure.UseHeightRange)
            {
                min = Mathf.Min(
                    structure.MinY,
                    structure.MaxY
                );

                max = Mathf.Max(
                    structure.MinY,
                    structure.MaxY
                );
            }
            else
            {
                min = settings.BottomWorldY + 4;

                max = underground
                    ? surface - Mathf.Max(4, structure.Height)
                    : settings.WorldHeight - 4;
            }

            if (underground)
                max = Mathf.Min(max, surface - 3);

            if (max < min)
                return min;

            return random.Next(min, max + 1);
        }

        private static bool CandidateCanTouchChunk(
            StructureDefinition structure,
            int anchorX,
            int chunkMinX,
            int chunkMaxX
        )
        {
            int minX =
                anchorX - structure.OriginX;

            int maxX =
                minX + structure.Width - 1;

            return maxX >= chunkMinX &&
                   minX <= chunkMaxX;
        }

        private static bool BiomeAllowed(
            WorldGenerator generator,
            WorldSettings settings,
            StructureDefinition structure,
            int worldX,
            int worldY
        )
        {
            if (
                structure.Biomes ==
                null
                ||
                structure.Biomes.Count ==
                0
            )
            {
                return true;
            }


            switch (
                structure.BiomeSource
            )
            {
                case StructureBiomeSource.Cave:
                    return
                        CaveBiomeAllowed(
                            settings,
                            structure,
                            worldX,
                            worldY
                        );


                case StructureBiomeSource.Either:
                    return
                        SurfaceBiomeAllowed(
                            generator,
                            structure,
                            worldX
                        )
                        ||
                        CaveBiomeAllowed(
                            settings,
                            structure,
                            worldX,
                            worldY
                        );


                default:
                    return
                        SurfaceBiomeAllowed(
                            generator,
                            structure,
                            worldX
                        );
            }
        }


        private static bool SurfaceBiomeAllowed(
            WorldGenerator generator,
            StructureDefinition structure,
            int worldX
        )
        {
            BiomeDefinition biome =
                generator.GetDominantBiome(
                    worldX
                );


            if (
                biome ==
                null
            )
            {
                return false;
            }


            List<string> names =
                GetBiomeNames(
                    biome
                );


            return
                MatchesRequiredBiome(
                    structure.Biomes,
                    names
                );
        }


        private static bool CaveBiomeAllowed(
            WorldSettings settings,
            StructureDefinition structure,
            int worldX,
            int worldY
        )
        {
            if (
                settings ==
                null
            )
            {
                return false;
            }


            string caveBiomeId =
                CaveBiomeRegistry
                    .GetBiomeIdAt(
                        worldX,
                        worldY,
                        settings.Seed
                    );


            if (
                string.IsNullOrWhiteSpace(
                    caveBiomeId
                )
            )
            {
                return false;
            }


            List<string> names =
                new List<string>
                {
                    caveBiomeId,
                    ShortContentId(
                        caveBiomeId
                    )
                };


            return
                MatchesRequiredBiome(
                    structure.Biomes,
                    names
                );
        }


        private static bool MatchesRequiredBiome(
            List<string> requiredBiomes,
            List<string> currentNames
        )
        {
            if (
                requiredBiomes ==
                null
                ||
                requiredBiomes.Count ==
                0
            )
            {
                return true;
            }


            if (
                currentNames ==
                null
                ||
                currentNames.Count ==
                0
            )
            {
                return false;
            }


            for (
                int i = 0;
                i < requiredBiomes.Count;
                i++
            )
            {
                string required =
                    requiredBiomes[i];


                if (
                    string.IsNullOrWhiteSpace(
                        required
                    )
                )
                {
                    continue;
                }


                if (
                    required ==
                    "*"
                )
                {
                    return true;
                }


                string normalizedRequired =
                    NormalizeBiomeKey(
                        required
                    );


                for (
                    int n = 0;
                    n < currentNames.Count;
                    n++
                )
                {
                    string current =
                        currentNames[n];


                    if (
                        string.IsNullOrWhiteSpace(
                            current
                        )
                    )
                    {
                        continue;
                    }


                    if (
                        string.Equals(
                            normalizedRequired,
                            NormalizeBiomeKey(
                                current
                            ),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return true;
                    }
                }
            }


            return false;
        }


        private static string NormalizeBiomeKey(
            string value
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return string.Empty;
            }


            return
                value.Trim();
        }


        private static string ShortContentId(
            string value
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return string.Empty;
            }


            int separator =
                value.LastIndexOf(
                    ':'
                );


            if (
                separator >=
                0
                &&
                separator <
                value.Length -
                1
            )
            {
                return
                    value.Substring(
                        separator +
                        1
                    );
            }


            return value;
        }


        private static List<string> GetBiomeNames(
            BiomeDefinition biome
        )
        {
            List<string> result =
                new List<string>();

            Type type = biome.GetType();

            string[] candidates =
            {
                "ID",
                "Id",
                "Name",
                "DisplayName"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                FieldInfo field =
                    type.GetField(
                        candidates[i],
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase
                    );

                if (field != null &&
                    field.FieldType == typeof(string))
                {
                    string value =
                        field.GetValue(biome) as string;

                    if (!string.IsNullOrWhiteSpace(value))
                        result.Add(value);
                }

                PropertyInfo property =
                    type.GetProperty(
                        candidates[i],
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase
                    );

                if (property != null &&
                    property.PropertyType == typeof(string) &&
                    property.CanRead)
                {
                    string value =
                        property.GetValue(
                            biome,
                            null
                        ) as string;

                    if (!string.IsNullOrWhiteSpace(value))
                        result.Add(value);
                }
            }

            return result;
        }

        private static bool HasRequiredFreeSpace(
            WorldGenerator generator,
            StructureDefinition structure,
            int anchorX,
            int anchorY
        )
        {
            int padding =
                Mathf.Max(
                    0,
                    structure.FreeSpacePadding
                );

            int minX =
                anchorX -
                structure.OriginX -
                padding;

            int maxX =
                minX +
                structure.Width -
                1 +
                padding * 2;

            int minY =
                anchorY -
                structure.OriginY -
                padding;

            int maxY =
                minY +
                structure.Height -
                1 +
                padding * 2;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // Ground under a Surface structure is allowed.
                    if (structure.SpawnType ==
                            StructureSpawnType.Surface &&
                        y < anchorY)
                        continue;

                    ushort baseBlock =
                        GetBaseForegroundBlock(
                            generator,
                            x,
                            y
                        );

                    if (baseBlock != 0)
                        return false;
                }
            }

            return true;
        }

        private static ushort GetBaseForegroundBlock(
            WorldGenerator generator,
            int worldX,
            int worldY
        )
        {
            try
            {
                if (generateForegroundMethod == null)
                {
                    generateForegroundMethod =
                        typeof(WorldGenerator).GetMethod(
                            "GenerateForegroundBlock",
                            BindingFlags.NonPublic |
                            BindingFlags.Instance
                        );
                }

                if (biomeRuntimeField == null)
                {
                    biomeRuntimeField =
                        typeof(WorldGenerator).GetField(
                            "biomeRuntime",
                            BindingFlags.NonPublic |
                            BindingFlags.Instance
                        );
                }

                if (generateForegroundMethod == null ||
                    biomeRuntimeField == null)
                {
                    return worldY >
                        generator.GetSurfaceHeight(worldX)
                            ? (ushort)0
                            : (ushort)1;
                }

                object runtimeTable =
                    biomeRuntimeField.GetValue(generator);

                if (runtimeTable == null)
                    return 0;

                if (biomeRuntimeGetMethod == null)
                {
                    biomeRuntimeGetMethod =
                        runtimeTable
                            .GetType()
                            .GetMethod(
                                "Get",
                                BindingFlags.Public |
                                BindingFlags.Instance
                            );
                }

                BiomeSample sample =
                    generator.GetBiomeSample(worldX);

                object runtimeBiome =
                    biomeRuntimeGetMethod.Invoke(
                        runtimeTable,
                        new object[]
                        {
                            sample.Dominant
                        }
                    );

                int surface =
                    generator.GetSurfaceHeight(worldX);

                object result =
                    generateForegroundMethod.Invoke(
                        generator,
                        new object[]
                        {
                            worldX,
                            worldY,
                            surface,
                            runtimeBiome
                        }
                    );

                return result is ushort value
                    ? value
                    : (ushort)0;
            }
            catch
            {
                // Safe fallback if generator internals change.
                return worldY >
                    generator.GetSurfaceHeight(worldX)
                        ? (ushort)0
                        : (ushort)1;
            }
        }

        private static void StampCandidateIntoChunk(
            WorldSettings settings,
            ChunkData data,
            int chunkX,
            int chunkY,
            StructureDefinition structure,
            int regionX,
            int anchorX,
            int anchorY
        )
        {
            int chunkMinX =
                chunkX * Chunk.SizeX;

            int chunkMinY =
                chunkY * Chunk.SizeY;

            string dimensionName =
                DimensionTravelRuntime.Current != null
                    ? DimensionTravelRuntime.Current.Name
                    : string.Empty;

            int structureSeed =
                StableHash(
                    settings.Seed,
                    structure.ID,
                    regionX
                );

            // Debug/runtime registry for ALL structures.
            //
            // This is called only after the candidate passed spawn chance,
            // height, biome and free-space checks and reached the stamping
            // stage for a loaded chunk. Therefore the debug menu can tell
            // us which structures were actually generated during this
            // session.
            StructureDebugRuntimeRegistry.Register(
                settings.Seed,
                dimensionName,
                structure,
                regionX,
                anchorX,
                anchorY
            );


            // Register the full deterministic instance even though
            // this method stamps only the cells that belong to the
            // current chunk. The runtime index deduplicates the same
            // candidate when neighbouring chunks are generated.
            StructureInstanceRuntimeIndex.Register(
                settings.Seed,
                dimensionName,
                structure,
                regionX,
                anchorX,
                anchorY
            );

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

                int localX =
                    worldX - chunkMinX;

                int localY =
                    worldY - chunkMinY;

                if (localX < 0 ||
                    localX >= Chunk.SizeX ||
                    localY < 0 ||
                    localY >= Chunk.SizeY)
                    continue;

                if (!string.IsNullOrWhiteSpace(
                    cell.ForegroundId))
                {
                    ushort id =
                        ResolveBlockId(
                            cell.ForegroundId
                        );

                    if (id != 0)
                    {
                        data.SetBlock(
                            localX,
                            localY,
                            id
                        );

                        if (IsChestBlock(id))
                        {
                            GeneratedChestLootRuntime.Register(
                                dimensionName,
                                worldX,
                                worldY,
                                structure.ID,
                                structureSeed,
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

                    if (id != 0)
                    {
                        data.SetBackground(
                            localX,
                            localY,
                            id
                        );
                    }
                }
            }
        }

        private static List<StructureLootEntryRuntime> ConvertLoot(
            List<StructureLootEntryDefinition> source
        )
        {
            List<StructureLootEntryRuntime> result =
                new List<StructureLootEntryRuntime>();

            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                StructureLootEntryDefinition entry = source[i];

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

        // =====================================================
        // DEBUG SEARCH
        // =====================================================

        /// <summary>
        /// Searches deterministic structure regions around centerX and
        /// returns the nearest candidate that passes the SAME checks used
        /// by real world generation:
        ///
        /// spawn chance
        /// height range
        /// surface/cave biome filter
        /// required free space
        ///
        /// This does not force-spawn anything.
        /// </summary>
        public static bool TryFindNearestCandidate(
            WorldGenerator generator,
            WorldSettings settings,
            StructureDefinition structure,
            int centerX,
            int searchRegionRadius,
            out int anchorX,
            out int anchorY,
            out int regionX
        )
        {
            anchorX =
                0;


            anchorY =
                0;


            regionX =
                0;


            if (
                generator ==
                null
                ||
                settings ==
                null
                ||
                structure ==
                null
            )
            {
                return false;
            }


            int regionSize =
                Mathf.Max(
                    8,
                    structure.RegionSize
                );


            int centerRegion =
                FloorDiv(
                    centerX,
                    regionSize
                );


            int radius =
                Mathf.Clamp(
                    searchRegionRadius,
                    0,
                    256
                );


            bool found =
                false;


            int bestDistance =
                int.MaxValue;


            for (
                int offset = -radius;
                offset <= radius;
                offset++
            )
            {
                int candidateRegion =
                    centerRegion +
                    offset;


                if (
                    !TryGetCandidate(
                        generator,
                        settings,
                        structure,
                        candidateRegion,
                        out int candidateX,
                        out int candidateY
                    )
                )
                {
                    continue;
                }


                int distance =
                    Mathf.Abs(
                        candidateX -
                        centerX
                    );


                if (
                    found
                    &&
                    distance >=
                    bestDistance
                )
                {
                    continue;
                }


                found =
                    true;


                bestDistance =
                    distance;


                anchorX =
                    candidateX;


                anchorY =
                    candidateY;


                regionX =
                    candidateRegion;
            }


            return found;
        }


        public static bool IsChestBlock(ushort blockId)
        {
            if (blockId == 0)
                return false;

            try
            {
                ContentID contentId =
                    BlockIDRegistry.GetContentID(blockId);

                if (!BlockRegistry.Contains(contentId))
                    return false;

                BlockDefinition block =
                    BlockRegistry.Get(contentId);

                if (block == null || block.Tags == null)
                    return false;

                for (int i = 0; i < block.Tags.Count; i++)
                {
                    if (string.Equals(
                        block.Tags[i],
                        "chest",
                        StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static ushort ResolveBlockId(string id)
        {
            try
            {
                ContentID contentId =
                    ContentID.Parse(id);

                if (!BlockIDRegistry.Contains(contentId))
                    return 0;

                return BlockIDRegistry.GetID(contentId);
            }
            catch
            {
                return 0;
            }
        }

        private static int FloorDiv(
            int value,
            int divisor
        )
        {
            int result = value / divisor;
            int remainder = value % divisor;

            if (remainder != 0 && value < 0)
                result--;

            return result;
        }

        private static int StableHash(
            int seed,
            string id,
            int regionX
        )
        {
            unchecked
            {
                int hash = seed;
                hash = hash * 397 ^ regionX;

                if (id != null)
                {
                    for (int i = 0; i < id.Length; i++)
                        hash = hash * 31 + id[i];
                }

                return hash;
            }
        }
    
    // [BT-AUTO-STRUCTURE-DATA]
    public byte Transform;
}
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Content;

namespace Game.World.Biomes
{
    public sealed class BiomeRuntimeData
    {
        public readonly BiomeDefinition Definition;

        public readonly ushort TopBlockID;
        public readonly ushort SoilBlockID;
        public readonly ushort StoneBlockID;
        public readonly ushort BackgroundBlockID;


        public BiomeRuntimeData(
            BiomeDefinition definition
        )
        {
            Definition = definition;

            TopBlockID =
                ResolveBlock(
                    definition.Terrain.TopBlock,
                    "game:grass"
                );

            SoilBlockID =
                ResolveBlock(
                    definition.Terrain.SoilBlock,
                    "game:dirt"
                );

            StoneBlockID =
                ResolveBlock(
                    definition.Terrain.StoneBlock,
                    "game:stone"
                );

            BackgroundBlockID =
                ResolveBlock(
                    definition.Terrain.BackgroundBlock,
                    "game:stone"
                );
        }


        public bool IsTerrainBlock(
            ushort blockID
        )
        {
            return
                blockID == TopBlockID ||
                blockID == SoilBlockID ||
                blockID == StoneBlockID ||
                blockID == BackgroundBlockID;
        }


        private static ushort ResolveBlock(
            string requested,
            string fallback
        )
        {
            ushort id =
                TryResolve(
                    requested
                );

            if (id != 0)
                return id;

            if (
                !string.Equals(
                    requested,
                    fallback,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                Debug.LogWarning(
                    "BIOME: block not found: " +
                    requested +
                    ". Fallback = " +
                    fallback
                );
            }

            return
                TryResolve(
                    fallback
                );
        }


        private static ushort TryResolve(
            string blockID
        )
        {
            if (string.IsNullOrWhiteSpace(blockID))
                return 0;

            ContentID id =
                ContentID.Parse(
                    blockID
                );

            if (
                !BlockIDRegistry.Contains(
                    id
                )
            )
            {
                return 0;
            }

            return
                BlockIDRegistry.GetID(
                    id
                );
        }
    }


    public sealed class BiomeRuntimeTable
    {
        private readonly Dictionary<string, BiomeRuntimeData>
            table =
            new Dictionary<string, BiomeRuntimeData>(
                StringComparer.OrdinalIgnoreCase
            );


        public BiomeRuntimeTable(
            DimensionBiomeProfile profile
        )
        {
            if (profile == null)
                return;

            for (
                int i = 0;
                i < profile.SurfaceBiomes.Count;
                i++
            )
            {
                BiomeDefinition biome =
                    profile.SurfaceBiomes[i];

                if (
                    biome == null ||
                    string.IsNullOrWhiteSpace(
                        biome.ID
                    ) ||
                    table.ContainsKey(
                        biome.ID
                    )
                )
                {
                    continue;
                }

                table.Add(
                    biome.ID,
                    new BiomeRuntimeData(
                        biome
                    )
                );
            }
        }


        public BiomeRuntimeData Get(
            BiomeDefinition biome
        )
        {
            if (
                biome == null ||
                string.IsNullOrWhiteSpace(
                    biome.ID
                )
            )
            {
                return null;
            }

            table.TryGetValue(
                biome.ID,
                out BiomeRuntimeData data
            );

            return data;
        }
    }
}
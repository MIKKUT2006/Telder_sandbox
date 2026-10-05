using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Fluids;
using Game.World.Biomes;
using Game.World.Biomes.Caves;
using Game.World.Generation;

namespace Game.World.Fluids
{
    /// <summary>
    /// Deterministic generation-time liquid placement.
    ///
    /// V2:
    /// - surface lakes are one candidate per region (no overlapping roll per fluid),
    /// - shores are organic and the water surface stays horizontal,
    /// - steep terrain rejects lakes instead of producing ugly vertical cuts,
    /// - cave biomes can define their own pools,
    /// - generated lake/pool cells are true source blocks (Minecraft-like),
    /// - all decisions are based on world coordinates so chunk borders are seamless.
    /// </summary>
    public static class LiquidLakeGenerator
    {
        private const int MaxGeneratedRadius = 30;

        public static void ApplyToChunk(
            WorldGenerator generator,
            WorldSettings settings,
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            if (generator == null || settings == null || data == null)
                return;

            ApplySurfaceLakes(
                generator,
                settings,
                data,
                chunkX,
                chunkY
            );

            ApplyCaveBiomePools(
                settings,
                data,
                chunkX,
                chunkY
            );
        }

        // =====================================================
        // SURFACE LAKES
        // =====================================================

        private static void ApplySurfaceLakes(
            WorldGenerator generator,
            WorldSettings settings,
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            int chunkMinX = chunkX * Chunk.SizeX;
            int chunkMaxX = chunkMinX + Chunk.SizeX - 1;

            DimensionBiomeProfile profile = generator.GetBiomeProfile();
            if (profile == null || profile.SurfaceBiomes == null)
                return;

            HashSet<int> processedRegionSizes = new HashSet<int>();

            // Different surface biomes may choose different lake spacing.
            // Process every unique spacing used by the active dimension profile.
            for (int b = 0; b < profile.SurfaceBiomes.Count; b++)
            {
                BiomeDefinition configuredBiome = profile.SurfaceBiomes[b];
                if (!SurfaceSettingsValid(configuredBiome))
                    continue;

                int regionSize = GetSurfaceRegionSize(
                    configuredBiome.LiquidGeneration
                );

                if (!processedRegionSizes.Add(regionSize))
                    continue;

                int firstRegion = FloorDiv(chunkMinX - MaxGeneratedRadius, regionSize) - 1;
                int lastRegion = FloorDiv(chunkMaxX + MaxGeneratedRadius, regionSize) + 1;

                for (int regionX = firstRegion; regionX <= lastRegion; regionX++)
                {
                    TryGenerateSurfaceCandidate(
                        generator,
                        settings,
                        data,
                        chunkX,
                        chunkY,
                        regionX,
                        regionSize
                    );
                }
            }
        }

        private static void TryGenerateSurfaceCandidate(
            WorldGenerator generator,
            WorldSettings settings,
            ChunkData data,
            int chunkX,
            int chunkY,
            int regionX,
            int regionSize
        )
        {
            int seed = Hash(settings.Seed, "surface_liquid", regionX, regionSize);
            System.Random rng = new System.Random(seed);

            int margin = Mathf.Clamp(regionSize / 8, 2, 10);
            int centerX =
                regionX * regionSize +
                rng.Next(margin, Mathf.Max(margin + 1, regionSize - margin));

            BiomeDefinition biome = generator.GetDominantBiome(centerX);
            if (!SurfaceSettingsValid(biome))
                return;

            // If the biome wants another grid size, only the correctly keyed pass
            // is allowed to produce a lake. This removes duplicate candidates.
            int configuredRegion = GetSurfaceRegionSize(
                biome.LiquidGeneration
            );

            if (configuredRegion != regionSize || FloorDiv(centerX, configuredRegion) != regionX)
                return;

            if (!TryChooseLiquid(
                biome.LiquidGeneration.Liquids,
                false,
                rng,
                out BiomeLiquidEntry entry
            ))
            {
                return;
            }

            int radius = PickRadius(
                entry,
                rng,
                2,
                Mathf.Max(2, Mathf.Min(MaxGeneratedRadius, regionSize / 2 - 1))
            );
            int chunkMinX = chunkX * Chunk.SizeX;
            int chunkMaxX = chunkMinX + Chunk.SizeX - 1;

            if (centerX + radius < chunkMinX || centerX - radius > chunkMaxX)
                return;

            // Do not let one lake casually cross into another surface biome.
            // The actual per-column check below is kept too for organic borders.
            if (!SameBiome(generator.GetDominantBiome(centerX), biome))
                return;

            int minSurface = int.MaxValue;
            int maxSurface = int.MinValue;

            for (int x = centerX - radius; x <= centerX + radius; x += 2)
            {
                BiomeDefinition at = generator.GetDominantBiome(x);
                if (!SameBiome(at, biome))
                    continue;

                int h = generator.GetSurfaceHeight(x);
                minSurface = Mathf.Min(minSurface, h);
                maxSurface = Mathf.Max(maxSurface, h);
            }

            if (minSurface == int.MaxValue)
                return;

            int allowedVariation = biome.LiquidGeneration.MaxSurfaceVariation <= 0
                ? 7
                : Mathf.Clamp(biome.LiquidGeneration.MaxSurfaceVariation, 1, 32);

            if (maxSurface - minSurface > allowedVariation)
                return;

            // Use the lowest bank as the water line so the generated lake is not
            // visibly hanging above terrain. Every filled lake cell is a true source
            // block, so generation is stable instead of spawning pre-spread liquid.
            int liquidTop = minSurface - 1;
            ushort liquidId = FluidIDRegistry.GetOrRegister(entry.ID);
            int minDepth = Mathf.Clamp(Mathf.Min(entry.MinDepth, entry.MaxDepth), 1, 24);
            int maxDepth = Mathf.Clamp(Mathf.Max(entry.MinDepth, entry.MaxDepth), minDepth, 32);
            float roughness = biome.LiquidGeneration.EdgeRoughness <= 0f
                ? 0.22f
                : Mathf.Clamp01(biome.LiquidGeneration.EdgeRoughness);

            int worldChunkMinY = chunkY * Chunk.SizeY;

            for (int localX = 0; localX < Chunk.SizeX; localX++)
            {
                int worldX = chunkMinX + localX;
                float normalizedX = (worldX - centerX) / (float)Mathf.Max(1, radius);
                float absX = Mathf.Abs(normalizedX);

                if (absX > 1f)
                    continue;

                BiomeDefinition columnBiome = generator.GetDominantBiome(worldX);
                if (!SameBiome(columnBiome, biome))
                    continue;

                float edgeNoise = SignedNoise(seed ^ 0x51ED270B, worldX, liquidTop);
                float edge = 1f - absX;

                if (edge + edgeNoise * roughness * 0.34f <= 0f)
                    continue;

                // Elliptic bowl with a little deterministic roughness.
                float bowl = Mathf.Sqrt(Mathf.Max(0f, 1f - normalizedX * normalizedX));
                float depthNoise = SignedNoise(seed ^ 0x2C1B3C6D, worldX, liquidTop) * roughness;
                float depth01 = Mathf.Clamp01(bowl + depthNoise * 0.22f);
                int depth = Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.Lerp(minDepth, maxDepth, depth01)),
                    1,
                    maxDepth
                );

                int naturalSurface = generator.GetSurfaceHeight(worldX);
                int floorY = liquidTop - depth;

                // Carve only the bowl interior. Background is intentionally kept,
                // preserving the usual Terraria-like rear wall behind the water.
                for (int worldY = floorY + 1; worldY <= naturalSurface; worldY++)
                {
                    int localY = worldY - worldChunkMinY;
                    if (localY < 0 || localY >= Chunk.SizeY)
                        continue;

                    if (data.GetFurniture(localX, localY) != 0)
                        continue;

                    ushort currentBlock = data.GetBlock(localX, localY);

                    // Liquid generation runs after structures. Only carve blocks
                    // that belong to the natural surface terrain; this keeps a
                    // generated house/tree/ruin from being erased by a lake.
                    if (currentBlock != 0 &&
                        !generator.IsNaturalSurfaceTerrainBlock(worldX, currentBlock))
                    {
                        continue;
                    }

                    data.SetBlock(localX, localY, 0);

                    if (worldY <= liquidTop)
                    {
                        data.SetLiquidSource(localX, localY, liquidId);
                    }
                    else
                    {
                        data.SetLiquid(localX, localY, 0, 0);
                    }
                }
            }
        }

        // =====================================================
        // CAVE BIOME POOLS
        // =====================================================

        private static void ApplyCaveBiomePools(
            WorldSettings settings,
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            int chunkMinX = chunkX * Chunk.SizeX;
            int chunkMinY = chunkY * Chunk.SizeY;
            string[] dimensionKeys = CaveBiomeDimensionRuntime.GetCurrentKeys();

            // Work from real cave floors. This avoids liquids embedded in stone.
            // Generated pool cells are stable sources; only exposed borders flow.
            for (int localX = 0; localX < Chunk.SizeX; localX++)
            {
                int worldX = chunkMinX + localX;

                for (int localY = 1; localY < Chunk.SizeY - 1; localY++)
                {
                    // The liquid source cell must be air, with a solid floor below.
                    if (data.GetBlock(localX, localY) != 0)
                        continue;

                    ushort caveFloorBlock = data.GetBlock(localX, localY - 1);
                    if (caveFloorBlock == 0)
                        continue;

                    if (data.GetFurniture(localX, localY) != 0)
                        continue;

                    int worldY = chunkMinY + localY;

                    CaveBiomeRuntimeData cave = CaveBiomeRegistry.FindAt(
                        worldX,
                        worldY,
                        settings.Seed,
                        dimensionKeys
                    );

                    if (!CaveSettingsValid(cave))
                        continue;

                    // When the cave biome replaces its rock/floor material, use
                    // that as a natural-floor marker. This prevents pools from
                    // spawning on a wooden/brick floor of a generated structure.
                    if ((cave.StoneBlockId != 0 || cave.FloorBlockId != 0) &&
                        caveFloorBlock != cave.StoneBlockId &&
                        caveFloorBlock != cave.FloorBlockId)
                    {
                        continue;
                    }

                    CaveBiomeLiquidSettings liquidSettings = cave.Definition.LiquidGeneration;

                    if (liquidSettings.RequireBackground && data.GetBackground(localX, localY) == 0)
                        continue;

                    int regionW = Mathf.Clamp(liquidSettings.HorizontalRegionSize, 8, 96);
                    int regionH = Mathf.Clamp(liquidSettings.VerticalRegionSize, 8, 64);
                    int regionX = FloorDiv(worldX, regionW);
                    int regionY = FloorDiv(worldY, regionH);
                    int seed = Hash(
                        settings.Seed,
                        cave.Definition.ID + "|cave_liquid",
                        regionX,
                        regionY
                    );

                    System.Random rng = new System.Random(seed);

                    if (!TryChooseLiquid(
                        liquidSettings.Liquids,
                        true,
                        rng,
                        out BiomeLiquidEntry entry
                    ))
                    {
                        continue;
                    }

                    int centerX = regionX * regionW + rng.Next(0, regionW);
                    int centerY = regionY * regionH + rng.Next(0, regionH);
                    int radiusX = PickRadius(
                        entry,
                        rng,
                        2,
                        Mathf.Max(2, Mathf.Min(MaxGeneratedRadius, regionW / 2))
                    );
                    int radiusY = Mathf.Clamp(
                        Mathf.Max(2, Mathf.RoundToInt(radiusX * 0.45f)),
                        2,
                        Mathf.Max(2, regionH / 2)
                    );

                    float nx = (worldX - centerX) / (float)Mathf.Max(1, radiusX);
                    float ny = (worldY - centerY) / (float)Mathf.Max(1, radiusY);
                    float ellipse = nx * nx + ny * ny;
                    float rough = SignedNoise(seed ^ 0x63D83595, worldX, worldY) *
                                  Mathf.Clamp01(liquidSettings.EdgeRoughness) * 0.32f;

                    if (ellipse > 1f + rough)
                        continue;

                    // Never bleed one cave biome's pool into another cave biome.
                    CaveBiomeRuntimeData aboveBiome = CaveBiomeRegistry.FindAt(
                        worldX,
                        worldY,
                        settings.Seed,
                        dimensionKeys
                    );

                    if (aboveBiome == null ||
                        aboveBiome.Definition == null ||
                        !string.Equals(
                            aboveBiome.Definition.ID,
                            cave.Definition.ID,
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        continue;
                    }

                    ushort liquidId = FluidIDRegistry.GetOrRegister(entry.ID);
                    int minDepth = Mathf.Clamp(Mathf.Min(entry.MinDepth, entry.MaxDepth), 1, 8);
                    int maxDepth = Mathf.Clamp(Mathf.Max(entry.MinDepth, entry.MaxDepth), minDepth, 12);

                    float centerStrength = Mathf.Clamp01(1f - Mathf.Sqrt(Mathf.Clamp01(ellipse)));
                    int sourceDepth = Mathf.Clamp(
                        Mathf.RoundToInt(Mathf.Lerp(minDepth, maxDepth, centerStrength)),
                        1,
                        maxDepth
                    );

                    // Fill the real cave basin with source cells. Source cells stay
                    // full; only their exposed edges create flowing liquid.
                    for (int d = 0; d < sourceDepth; d++)
                    {
                        int y = localY + d;
                        if (y < 0 || y >= Chunk.SizeY)
                            break;

                        if (data.GetBlock(localX, y) != 0 || data.GetFurniture(localX, y) != 0)
                            break;

                        int wy = chunkMinY + y;
                        CaveBiomeRuntimeData cellBiome = CaveBiomeRegistry.FindAt(
                            worldX,
                            wy,
                            settings.Seed,
                            dimensionKeys
                        );

                        if (cellBiome == null ||
                            cellBiome.Definition == null ||
                            !string.Equals(
                                cellBiome.Definition.ID,
                                cave.Definition.ID,
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            break;
                        }

                        if (liquidSettings.RequireBackground && data.GetBackground(localX, y) == 0)
                            break;

                        data.SetLiquidSource(localX, y, liquidId);
                    }
                }
            }
        }

        // =====================================================
        // SHARED HELPERS
        // =====================================================

        private static bool SurfaceSettingsValid(BiomeDefinition biome)
        {
            return biome != null &&
                   biome.LiquidGeneration != null &&
                   biome.LiquidGeneration.Enabled &&
                   biome.LiquidGeneration.Liquids != null &&
                   biome.LiquidGeneration.Liquids.Length > 0;
        }

        private static bool CaveSettingsValid(CaveBiomeRuntimeData cave)
        {
            return cave != null &&
                   cave.Definition != null &&
                   cave.Definition.Enabled &&
                   cave.Definition.LiquidGeneration != null &&
                   cave.Definition.LiquidGeneration.Enabled &&
                   cave.Definition.LiquidGeneration.Liquids != null &&
                   cave.Definition.LiquidGeneration.Liquids.Length > 0;
        }

        private static bool TryChooseLiquid(
            BiomeLiquidEntry[] entries,
            bool cave,
            System.Random rng,
            out BiomeLiquidEntry chosen
        )
        {
            chosen = null;

            if (entries == null || entries.Length == 0 || rng == null)
                return false;

            float totalRate = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                BiomeLiquidEntry e = entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.ID))
                    continue;

                float chance = GetGenerationChance(e, cave);

                totalRate += Mathf.Clamp01(chance) * Mathf.Max(0f, e.Weight);
            }

            if (totalRate <= 0f)
                return false;

            // One candidate maximum per region. This is the key difference from V1:
            // two liquid types can no longer generate on top of one another.
            if (rng.NextDouble() >= Mathf.Clamp01(totalRate))
                return false;

            float pick = (float)rng.NextDouble() * totalRate;
            float acc = 0f;

            for (int i = 0; i < entries.Length; i++)
            {
                BiomeLiquidEntry e = entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.ID))
                    continue;

                float chance = GetGenerationChance(e, cave);

                float rate = Mathf.Clamp01(chance) * Mathf.Max(0f, e.Weight);
                if (rate <= 0f)
                    continue;

                acc += rate;
                if (pick <= acc)
                {
                    chosen = e;
                    return true;
                }
            }

            // Floating point fallback.
            for (int i = entries.Length - 1; i >= 0; i--)
            {
                if (entries[i] != null && !string.IsNullOrWhiteSpace(entries[i].ID))
                {
                    chosen = entries[i];
                    return true;
                }
            }

            return false;
        }

        private static float GetGenerationChance(
            BiomeLiquidEntry entry,
            bool cave
        )
        {
            if (entry == null)
                return 0f;

            if (!cave)
                return Mathf.Clamp01(entry.LakeChance);

            // PoolChance is optional. Old/shared entries can keep LakeChance.
            float value = entry.PoolChance > 0f
                ? entry.PoolChance
                : entry.LakeChance;

            return Mathf.Clamp01(value);
        }

        private static int GetSurfaceRegionSize(BiomeLiquidSettings settings)
        {
            int value = settings != null && settings.RegionSize > 0
                ? settings.RegionSize
                : 48;

            return Mathf.Clamp(value, 24, 128);
        }

        private static int PickRadius(
            BiomeLiquidEntry entry,
            System.Random rng,
            int minAllowed,
            int maxAllowed
        )
        {
            int minRadius;
            int maxRadius;

            if (entry.MinWidth > 0 || entry.MaxWidth > 0)
            {
                int minWidth = entry.MinWidth > 0 ? entry.MinWidth : entry.MaxWidth;
                int maxWidth = entry.MaxWidth > 0 ? entry.MaxWidth : entry.MinWidth;
                int lo = Mathf.Max(2, Mathf.Min(minWidth, maxWidth));
                int hi = Mathf.Max(lo, Mathf.Max(minWidth, maxWidth));
                minRadius = Mathf.CeilToInt(lo * 0.5f);
                maxRadius = Mathf.CeilToInt(hi * 0.5f);
            }
            else
            {
                int loSize = Mathf.Max(4, Mathf.Min(entry.MinLakeSize, entry.MaxLakeSize));
                int hiSize = Mathf.Max(loSize, Mathf.Max(entry.MinLakeSize, entry.MaxLakeSize));
                minRadius = Mathf.RoundToInt(Mathf.Sqrt(loSize) * 0.58f);
                maxRadius = Mathf.RoundToInt(Mathf.Sqrt(hiSize) * 0.72f);
            }

            minRadius = Mathf.Clamp(minRadius, minAllowed, maxAllowed);
            maxRadius = Mathf.Clamp(maxRadius, minRadius, maxAllowed);

            return rng.Next(minRadius, maxRadius + 1);
        }

        private static bool SameBiome(BiomeDefinition a, BiomeDefinition b)
        {
            return a != null &&
                   b != null &&
                   !string.IsNullOrWhiteSpace(a.ID) &&
                   string.Equals(a.ID, b.ID, StringComparison.OrdinalIgnoreCase);
        }

        private static float SignedNoise(int seed, int x, int y)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= (uint)x * 374761393u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 668265263u;
                h *= 2246822519u;
                h ^= h >> 15;
                return (h / (float)uint.MaxValue) * 2f - 1f;
            }
        }

        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            int r = a % b;
            if (r != 0 && ((r < 0) != (b < 0)))
                q--;
            return q;
        }

        private static int Hash(int seed, string key, int x, int y)
        {
            unchecked
            {
                int h = seed * 486187739;
                h = h * 31 + x;
                h = h * 31 + y;
                string s = key ?? string.Empty;
                for (int i = 0; i < s.Length; i++)
                    h = h * 31 + s[i];
                return h;
            }
        }
    }
}

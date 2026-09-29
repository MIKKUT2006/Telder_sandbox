using UnityEngine;
using Game.Fluids;
using Game.World.Biomes;
using Game.World.Generation;

namespace Game.World.Fluids
{
    public static class LiquidLakeGenerator
    {
        public static void ApplyToChunk(WorldGenerator generator, WorldSettings settings, ChunkData data, int chunkX, int chunkY)
        {
            if (generator == null || settings == null || data == null) return;
            for (int lx = 0; lx < Chunk.SizeX; lx++)
            {
                int wx = chunkX * Chunk.SizeX + lx;
                BiomeDefinition biome = generator.GetDominantBiome(wx);
                if (biome == null || biome.LiquidGeneration == null || !biome.LiquidGeneration.Enabled || biome.LiquidGeneration.Liquids == null) continue;
                for (int e = 0; e < biome.LiquidGeneration.Liquids.Length; e++)
                {
                    BiomeLiquidEntry entry = biome.LiquidGeneration.Liquids[e];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.ID) || entry.LakeChance <= 0f) continue;
                    int region = Mathf.Max(12, Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(4, entry.MinLakeSize + entry.MaxLakeSize)) * 2.5f));
                    int regionX = FloorDiv(wx, region);
                    int seed = Hash(settings.Seed, biome.ID, entry.ID, regionX);
                    System.Random rng = new System.Random(seed);
                    float totalWeight = 0f;
                    for (int w = 0; w < biome.LiquidGeneration.Liquids.Length; w++)
                        if (biome.LiquidGeneration.Liquids[w] != null)
                            totalWeight += Mathf.Max(0f, biome.LiquidGeneration.Liquids[w].Weight);
                    float share = totalWeight <= 0f ? 1f : Mathf.Max(0f, entry.Weight) / totalWeight;
                    if (rng.NextDouble() > Mathf.Clamp01(entry.LakeChance * Mathf.Max(0.05f, share))) continue;
                    int centerX = regionX * region + rng.Next(0, region);
                    int radius = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(rng.Next(Mathf.Max(4, entry.MinLakeSize), Mathf.Max(entry.MinLakeSize + 1, entry.MaxLakeSize + 1))) * 0.55f), 2, 18);
                    if (Mathf.Abs(wx - centerX) > radius) continue;
                    int surface = generator.GetSurfaceHeight(centerX);
                    float nx = (wx - centerX) / (float)Mathf.Max(1, radius);
                    int depth = Mathf.RoundToInt(Mathf.Lerp(Mathf.Max(1, entry.MinDepth), Mathf.Max(entry.MinDepth, entry.MaxDepth), 1f - nx * nx));
                    int waterTop = surface - 1;
                    ushort fluid = FluidIDRegistry.GetOrRegister(entry.ID);
                    for (int wy = waterTop - depth + 1; wy <= waterTop; wy++)
                    {
                        int ly = wy - chunkY * Chunk.SizeY;
                        if (ly < 0 || ly >= Chunk.SizeY) continue;
                        // Carve foreground for the basin but keep the background wall.
                        data.SetBlock(lx, ly, 0);
                        data.SetLiquid(lx, ly, fluid, 8);
                    }
                }
            }
        }

        private static int FloorDiv(int a, int b) { int q = a / b; int r = a % b; if (r != 0 && ((r < 0) != (b < 0))) q--; return q; }
        private static int Hash(int seed, string a, string b, int x)
        {
            unchecked { int h = seed * 486187739 + x * 16777619; string s = (a ?? "") + "|" + (b ?? ""); for (int i=0;i<s.Length;i++) h = h * 31 + s[i]; return h; }
        }
    }
}

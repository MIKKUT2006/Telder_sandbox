using System;
using System.IO;
using Game.Content;
using Game.World.Generation;
using UnityEngine;

namespace Game.World.Vegetation.Caves
{
    // Each column reconstructs ceiling anchors up to nine cells above the chunk.
    // No neighbour chunk is created or modified, so streaming order is irrelevant.
    public static class CaveVineGenerator
    {
        [Serializable] private sealed class Profile
        {
            public string BlockId = "game:vines";
            public float Chance = .16f;
            public int MinLength = 7;
            public int MaxLength = 9;
        }

        private static Profile profile;

        public static void Reload() { profile = null; }

        public static void Apply(WorldGenerator generator, WorldSettings settings,
            ChunkData data, int chunkX, int chunkY)
        {
            if (profile == null)
            {
                profile = new Profile();
                string path = Path.Combine(DataPaths.GameDataFolder, "Vegetation", "Cave", "vines.json");
                try { if (File.Exists(path)) profile = JsonUtility.FromJson<Profile>(File.ReadAllText(path)) ?? profile; }
                catch (Exception e) { Debug.LogWarning("VINES: " + e.Message); }
            }
            ushort id;
            try { id = BlockIDRegistry.GetID(ContentID.Parse(profile.BlockId)); }
            catch { return; }
            if (id == 0) return;
            int maximum = Mathf.Clamp(profile.MaxLength, 1, 9);
            int minimum = Mathf.Clamp(profile.MinLength, 1, maximum);
            int bottom = chunkY * Chunk.SizeY, top = bottom + Chunk.SizeY - 1;
            for (int lx = 0; lx < Chunk.SizeX; lx++)
            {
                int x = chunkX * Chunk.SizeX + lx;
                int surface = generator.GetSurfaceHeight(x);
                bool aboveSolid = Solid(generator, data, lx, top + maximum + 1, bottom, x);
                for (int y = top + maximum; y >= bottom; y--)
                {
                    bool solid = Solid(generator, data, lx, y, bottom, x);
                    if (!solid && aboveSolid && y < surface - 3 &&
                        Hash01(settings.Seed, x, y) < Mathf.Clamp01(profile.Chance))
                    {
                        int length = minimum + (int)(Hash01(settings.Seed ^ 17291, x, y) * (maximum - minimum + 1));
                        for (int down = 0; down < length; down++)
                        {
                            int vy = y - down;
                            if (Solid(generator, data, lx, vy, bottom, x)) break;
                            int ly = vy - bottom;
                            if (ly < 0 || ly >= Chunk.SizeY) continue;
                            if (data.GetBackground(lx, ly) == 0 || data.GetFurniture(lx, ly) != 0 ||
                                data.GetLiquidAmount(lx, ly) > 0) break;
                            data.SetFurniture(lx, ly, id);
                        }
                    }
                    aboveSolid = solid;
                }
            }
        }

        private static bool Solid(WorldGenerator generator, ChunkData data, int lx, int y, int bottom, int x)
        {
            int ly = y - bottom;
            return ly >= 0 && ly < Chunk.SizeY ? data.GetBlock(lx, ly) != 0 : generator.HasGeneratedTerrain(x, y);
        }

        private static float Hash01(int seed, int x, int y)
        {
            unchecked
            {
                uint h = (uint)seed ^ (uint)x * 374761393u ^ (uint)y * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xffffffu) / 16777216f;
            }
        }
    }
}

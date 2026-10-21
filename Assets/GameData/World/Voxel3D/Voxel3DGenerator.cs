using UnityEngine;
using Game.Content;

namespace Game.World.Voxel3D
{
    /// <summary>
    /// Infinite Minecraft-alpha-style terrain generator.
    /// Surface terrain uses several scales (continents, hills, valleys, ridged mountains),
    /// while caves use genuine deterministic 3D value-noise/fBm instead of 2D Perlin slices.
    /// </summary>
    public sealed class Voxel3DGenerator
    {
        private readonly int seed;

        public Voxel3DGenerator(int seed)
        {
            // Keep the seed as an integer. Do NOT convert it to a huge float offset for
            // Mathf.PerlinNoise: large world seeds lose neighbouring X/Z precision and
            // can make the whole surface evaluate to almost one height.
            this.seed = seed;
        }

        public VoxelBiome3DDefinition GetBiome(int worldX, int worldZ)
        {
            var list = VoxelBiome3DRegistry.All;
            if (list == null || list.Count == 0)
                return null;
            if (list.Count == 1)
                return list[0];

            // Stable integer-seeded biome noise. Seed never becomes a large float.
            float warpX = FbmSigned2D(worldX, worldZ, .0024f, 3, 2.03f, .52f, 1703) * 72f;
            float warpZ = FbmSigned2D(worldX, worldZ, .0024f, 3, 2.03f, .52f, 5197) * 72f;
            float n = Fbm01_2D(worldX + warpX, worldZ + warpZ, .00145f, 4, 2.0f, .52f, 9127);
            int index = Mathf.Clamp(Mathf.FloorToInt(n * list.Count), 0, list.Count - 1);
            return list[index];
        }

        public int GetSurfaceHeight(int x, int z)
        {
            VoxelBiome3DDefinition b = GetBiome(x, z);
            if (b == null)
                return 48;

            // Domain warp bends hills and ranges so they do not look like straight noise bands.
            float warpX = FbmSigned2D(x, z, .0048f, 3, 2.02f, .52f, 2017) * 23f;
            float warpZ = FbmSigned2D(x, z, .0048f, 3, 2.02f, .52f, 2791) * 23f;
            float sx = x + warpX;
            float sz = z + warpZ;

            // Several spatial scales are deliberately mixed. The local layers make terrain
            // visibly non-flat within 2-4 chunks, while the broad layers create real regions.
            float continent = FbmSigned2D(sx, sz, .0016f, 4, 2.0f, .53f, 311);
            float broadHills = FbmSigned2D(sx, sz, Mathf.Max(.0032f, b.HillScale), 5, 2.02f, .53f, 4703);
            float localHills = FbmSigned2D(sx, sz, Mathf.Max(.0105f, b.HillScale * 1.85f), 4, 2.06f, .50f, 8309);
            float detail = FbmSigned2D(sx, sz, Mathf.Max(.024f, b.DetailScale), 3, 2.13f, .45f, 13331);

            // Mountain *regions* are low frequency. Inside them, ridged noise creates peaks
            // and chains instead of simply making every point noisy and high.
            float rangeRegion = Fbm01_2D(sx, sz, Mathf.Max(.0018f, b.MountainScale * .62f), 4, 2.0f, .52f, 21101);
            float rangeMask = Smooth01(.43f, .66f, rangeRegion);
            float ridgeA = RidgedFbm2D(sx, sz, Mathf.Max(.0052f, b.MountainScale * 1.30f), 5, 2.03f, .54f, 30703);
            float ridgeB = RidgedFbm2D(sx, sz, Mathf.Max(.0110f, b.MountainScale * 2.55f), 4, 2.07f, .50f, 38113);
            float ridges = Mathf.Pow(Mathf.Clamp01(ridgeA * .70f + ridgeB * .30f), 1.18f);

            // Carve broad valleys between hill systems.
            float valleyField = Fbm01_2D(sx, sz, .0068f, 3, 2.04f, .50f, 42901);
            float valley = Smooth01(.61f, .84f, valleyField);

            float hillStrength = Mathf.Max(14f, b.HillStrength);
            float mountainStrength = Mathf.Max(28f, b.MountainStrength);

            float height = b.TerrainBaseHeight;
            height += continent * 10f;
            height += broadHills * hillStrength;
            height += localHills * (hillStrength * .48f);
            height += detail * Mathf.Max(2f, b.DetailStrength);
            height += rangeMask * ridges * mountainStrength;
            height -= valley * (hillStrength * .22f);

            return Mathf.Clamp(Mathf.RoundToInt(height), -48, 220);
        }

        public bool IsCave(int x, int y, int z, VoxelBiome3DDefinition b, int surface)
        {
            if (b == null)
                return false;

            int depth = surface - y;
            if (depth < 5)
                return false; // always keep a stable roof near the surface

            // Fade caves in over the first few underground blocks.
            float depthFade = Mathf.Clamp01((depth - 4f) / 14f);
            float scale = Mathf.Max(.008f, b.CaveScale);

            // Domain warp in 3D produces winding tunnel networks rather than bubbles.
            float wx = FbmSigned3D(x * .019f, y * .019f, z * .019f, 3, 2.0f, .5f, seed + 7001) * 8.5f;
            float wy = FbmSigned3D(x * .017f, y * .017f, z * .017f, 2, 2.0f, .5f, seed + 7127) * 5.0f;
            float wz = FbmSigned3D(x * .019f, y * .019f, z * .019f, 3, 2.0f, .5f, seed + 7243) * 8.5f;

            float px = (x + wx) * scale;
            float py = (y + wy) * scale * .82f;
            float pz = (z + wz) * scale;

            float cavern = Fbm01_3D(px, py, pz, 4, 2.01f, .52f, seed + 8111);

            // Two elongated fields intersect to create recognisable worm/tunnel shapes.
            float tunnelA = Mathf.Abs(FbmSigned3D(px * 1.22f, py * .78f, pz * 1.22f, 3, 2.05f, .5f, seed + 9001));
            float tunnelB = Mathf.Abs(FbmSigned3D(px * .82f + 31.7f, py * 1.16f, pz * .82f - 19.4f, 3, 2.08f, .5f, seed + 9157));
            float tunnelField = 1f - Mathf.Clamp01(Mathf.Min(tunnelA, tunnelB) * 3.15f);

            // Large chambers appear occasionally deeper underground.
            float chamber = Fbm01_3D(px * .43f + 13.1f, py * .43f - 7.2f, pz * .43f + 21.7f, 3, 2.0f, .55f, seed + 10009);

            float threshold = Mathf.Lerp(.78f, b.CaveThreshold, Mathf.Clamp01(b.CaveStrength));
            threshold -= Mathf.Clamp01((depth - 18f) / 70f) * .035f;

            bool cavernCut = cavern > threshold;
            bool tunnelCut = tunnelField > Mathf.Lerp(.81f, .68f, depthFade) && cavern > threshold - .13f;
            bool chamberCut = depth > 18 && chamber > .735f && cavern > threshold - .18f;

            return depthFade > 0f && (cavernCut || tunnelCut || chamberCut);
        }

        public VoxelChunk3D Generate(Vector3Int chunkCoordinate)
        {
            var chunk = new VoxelChunk3D(chunkCoordinate);
            Vector3Int o = chunk.WorldOrigin;

            for (int x = 0; x < VoxelChunk3D.Size; x++)
            for (int z = 0; z < VoxelChunk3D.Size; z++)
            {
                int wx = o.x + x;
                int wz = o.z + z;
                VoxelBiome3DDefinition biome = GetBiome(wx, wz);
                int surface = GetSurfaceHeight(wx, wz);

                ushort surfaceId = Resolve(biome != null ? biome.SurfaceBlock : "game:grass");
                ushort subsurfaceId = Resolve(biome != null ? biome.SubsurfaceBlock : "game:dirt");
                ushort stoneId = Resolve(biome != null ? biome.StoneBlock : "game:stone");

                for (int y = 0; y < VoxelChunk3D.Size; y++)
                {
                    int wy = o.y + y;
                    if (wy > surface)
                        continue;
                    if (IsCave(wx, wy, wz, biome, surface))
                        continue;

                    ushort id;
                    if (wy == surface)
                        id = surfaceId;
                    else if (wy >= surface - 3)
                        id = subsurfaceId;
                    else
                        id = stoneId;

                    chunk.Set(x, y, z, id);
                }
            }

            VoxelNature3D.Apply(this,chunk,seed);
            Game.World.Voxel3D.Structures.VoxelStructureGeneration3D.ApplyToChunk(this, chunk, seed);
            Voxel3DSaveRuntime.ApplyToChunk(chunk);
            return chunk;
        }

        // ---------------- Stable deterministic 2D value noise ----------------

        private float Fbm01_2D(float x, float z, float scale, int octaves, float lacunarity, float persistence, int salt)
        {
            float value = 0f, amplitude = 1f, total = 0f, frequency = 1f;
            int seededSalt = MixSalt(salt);
            for (int i = 0; i < octaves; i++)
            {
                value += ValueNoise2D(x * scale * frequency, z * scale * frequency, seededSalt + i * 1013) * amplitude;
                total += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return total > 0f ? value / total : .5f;
        }

        private float FbmSigned2D(float x, float z, float scale, int octaves, float lacunarity, float persistence, int salt)
        {
            return Fbm01_2D(x, z, scale, octaves, lacunarity, persistence, salt) * 2f - 1f;
        }

        private float RidgedFbm2D(float x, float z, float scale, int octaves, float lacunarity, float persistence, int salt)
        {
            float value = 0f, amplitude = 1f, total = 0f, frequency = 1f;
            int seededSalt = MixSalt(salt);
            for (int i = 0; i < octaves; i++)
            {
                float n = ValueNoise2D(x * scale * frequency, z * scale * frequency, seededSalt + i * 1619);
                float ridge = 1f - Mathf.Abs(n * 2f - 1f);
                ridge *= ridge;
                value += ridge * amplitude;
                total += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return total > 0f ? value / total : 0f;
        }

        private int MixSalt(int salt)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= (uint)salt + 0x9E3779B9u + (h << 6) + (h >> 2);
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                return (int)h;
            }
        }

        private static float ValueNoise2D(float x, float z, int salt)
        {
            int x0 = Mathf.FloorToInt(x);
            int z0 = Mathf.FloorToInt(z);
            int x1 = x0 + 1;
            int z1 = z0 + 1;
            float tx = Fade(x - x0);
            float tz = Fade(z - z0);

            float c00 = Hash01(x0, 0, z0, salt);
            float c10 = Hash01(x1, 0, z0, salt);
            float c01 = Hash01(x0, 0, z1, salt);
            float c11 = Hash01(x1, 0, z1, salt);

            float a = Mathf.Lerp(c00, c10, tx);
            float b = Mathf.Lerp(c01, c11, tx);
            return Mathf.Lerp(a, b, tz);
        }

        // ---------------- Genuine deterministic 3D value noise ----------------

        private static float Fbm01_3D(float x, float y, float z, int octaves, float lacunarity, float persistence, int salt)
        {
            float v = 0f, amp = 1f, total = 0f, freq = 1f;
            for (int i = 0; i < octaves; i++)
            {
                v += ValueNoise3D(x * freq, y * freq, z * freq, salt + i * 1013) * amp;
                total += amp;
                amp *= persistence;
                freq *= lacunarity;
            }
            return total > 0f ? v / total : .5f;
        }

        private static float FbmSigned3D(float x, float y, float z, int octaves, float lacunarity, float persistence, int salt)
        {
            return Fbm01_3D(x, y, z, octaves, lacunarity, persistence, salt) * 2f - 1f;
        }

        private static float ValueNoise3D(float x, float y, float z, int salt)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y), z0 = Mathf.FloorToInt(z);
            int x1 = x0 + 1, y1 = y0 + 1, z1 = z0 + 1;
            float tx = Fade(x - x0), ty = Fade(y - y0), tz = Fade(z - z0);

            float c000 = Hash01(x0, y0, z0, salt); float c100 = Hash01(x1, y0, z0, salt);
            float c010 = Hash01(x0, y1, z0, salt); float c110 = Hash01(x1, y1, z0, salt);
            float c001 = Hash01(x0, y0, z1, salt); float c101 = Hash01(x1, y0, z1, salt);
            float c011 = Hash01(x0, y1, z1, salt); float c111 = Hash01(x1, y1, z1, salt);

            float x00 = Mathf.Lerp(c000, c100, tx); float x10 = Mathf.Lerp(c010, c110, tx);
            float x01 = Mathf.Lerp(c001, c101, tx); float x11 = Mathf.Lerp(c011, c111, tx);
            float y0v = Mathf.Lerp(x00, x10, ty); float y1v = Mathf.Lerp(x01, x11, ty);
            return Mathf.Lerp(y0v, y1v, tz);
        }

        private static float Hash01(int x, int y, int z, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + z * 2147483647 + salt * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static float Fade(float t) { return t * t * (3f - 2f * t); }

        private static float Smooth01(float min, float max, float value)
        {
            float t = Mathf.InverseLerp(min, max, value);
            return t * t * (3f - 2f * t);
        }

        private static ushort Resolve(string id)
        {
            try
            {
                ContentID cid = ContentID.Parse(id);
                return BlockIDRegistry.Contains(cid) ? BlockIDRegistry.GetID(cid) : (ushort)0;
            }
            catch { return 0; }
        }
    }
}

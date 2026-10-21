using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.World.Voxel3D
{
    [Serializable]
    public sealed class VoxelBiome3DDefinition
    {
        public string Id;
        public string DisplayName;
        public string SourceBiome;
        public string SourceBiomeId=>!string.IsNullOrEmpty(SourceBiome)?SourceBiome:"game:"+(Id=="badlands3d"?"barrens":(Id??"plains3d").Replace("3d",""));
        public bool Matches(string id)=>id=="*"||string.Equals(id,Id,StringComparison.OrdinalIgnoreCase)||string.Equals(id,SourceBiomeId,StringComparison.OrdinalIgnoreCase)||string.Equals("game:"+id,SourceBiomeId,StringComparison.OrdinalIgnoreCase);

        [Header("Terrain")]
        public float TerrainBaseHeight = 48f;
        public float HillStrength = 14f;
        public float HillScale = .0075f;
        public float MountainStrength = 26f;
        public float MountainScale = .0045f;
        public float DetailStrength = 5f;
        public float DetailScale = .03f;

        [Header("Caves")]
        public float CaveScale = .035f;
        public float CaveThreshold = .61f;
        public float CaveStrength = 1f;

        [Header("Blocks")]
        public string SurfaceBlock;
        public string SubsurfaceBlock;
        public string StoneBlock;
        public int WaterLevel = 42;
        public string FluidId = "game:water";

        [Header("Sky / Fog (0..1)")]
        public float SkyR = .49f;
        public float SkyG = .70f;
        public float SkyB = 1f;
        public float GroundR = .34f;
        public float GroundG = .42f;
        public float GroundB = .50f;
        public float FogR = .49f;
        public float FogG = .70f;
        public float FogB = 1f;

        public Color SkyColor => new Color(SkyR, SkyG, SkyB, 1f);
        public Color GroundColor => new Color(GroundR, GroundG, GroundB, 1f);
        public Color FogColor => new Color(FogR, FogG, FogB, 1f);
    }

    public static class VoxelBiome3DRegistry
    {
        private static readonly List<VoxelBiome3DDefinition> all = new List<VoxelBiome3DDefinition>();
        private static bool loaded;
        public static IReadOnlyList<VoxelBiome3DDefinition> All { get { EnsureLoaded(); return all; } }

        public static void Reload() { loaded = false; EnsureLoaded(); }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            all.Clear();
            string folder = Path.Combine(Game.Content.DataPaths.GameDataFolder, "VoxelBiomes3D");
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    try
                    {
                        var b = JsonUtility.FromJson<VoxelBiome3DDefinition>(File.ReadAllText(file));
                        if (b != null && !string.IsNullOrWhiteSpace(b.Id))
                        {
                            Sanitize(b);
                            all.Add(b);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("VOXEL3D BIOME: failed to load " + file + " | " + e.Message);
                    }
                }
            }

            if (all.Count == 0)
            {
                var fallback = new VoxelBiome3DDefinition
                {
                    Id = "plains3d",
                    DisplayName = "Voxel Plains",
                    TerrainBaseHeight = 48,
                    HillStrength = 16,
                    HillScale = .0075f,
                    MountainStrength = 24,
                    MountainScale = .0045f,
                    DetailStrength = 5,
                    DetailScale = .03f,
                    CaveScale = .035f,
                    CaveThreshold = .61f,
                    CaveStrength = 1f,
                    SurfaceBlock = "game:grass",
                    SubsurfaceBlock = "game:dirt",
                    StoneBlock = "game:stone"
                };
                Sanitize(fallback);
                all.Add(fallback);
            }
        }

        private static void Sanitize(VoxelBiome3DDefinition b)
        {
            b.HillScale = Mathf.Max(.00005f, b.HillScale);
            b.MountainScale = Mathf.Max(.00005f, b.MountainScale);
            b.DetailScale = Mathf.Max(.00005f, b.DetailScale);
            b.CaveScale = Mathf.Max(.00005f, b.CaveScale);
            b.CaveThreshold = Mathf.Clamp(b.CaveThreshold, .45f, .85f);
            b.CaveStrength = Mathf.Max(.1f, b.CaveStrength);

            b.SkyR = Mathf.Clamp01(b.SkyR); b.SkyG = Mathf.Clamp01(b.SkyG); b.SkyB = Mathf.Clamp01(b.SkyB);
            b.GroundR = Mathf.Clamp01(b.GroundR); b.GroundG = Mathf.Clamp01(b.GroundG); b.GroundB = Mathf.Clamp01(b.GroundB);
            b.FogR = Mathf.Clamp01(b.FogR); b.FogG = Mathf.Clamp01(b.FogG); b.FogB = Mathf.Clamp01(b.FogB);
        }
    }
}

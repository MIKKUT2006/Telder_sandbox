using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Game.Content;
using Game.World.Biomes.Caves;
using Game.World.Biomes.Surface;
using Game.World.Generation;


namespace Game.World.Vegetation.Caves
{
    /// <summary>
    /// Deterministic cave furniture generation.
    ///
    /// V4 deliberately uses ChunkData.GetFurniture/SetFurniture directly.
    /// There is NO reflection API discovery anymore.
    ///
    /// Sources:
    /// 1. Assets/GameData/Vegetation/Cave/*.json
    /// 2. Assets/GameData/SurfaceFlora/*.json whose Biomes entry matches
    ///    the active cave biome. This keeps ancient_caves_flora.json useful
    ///    without forcing the project to duplicate that profile.
    /// </summary>
    public static class CaveVegetationGenerationRuntime
    {
        [Serializable]
        private sealed class CaveVegetationFile
        {
            public string ID;
            public string SpawnType = "Cave";
            public string BlockId;

            public List<string> Biomes =
                new List<string>();

            public float Chance = 0.15f;

            public int MinY = -1000000;
            public int MaxY = 1000000;

            public bool Floor = true;
            public bool Ceiling = false;
            public bool Walls = false;

            public float NoiseScale = 0.08f;
            public float NoiseThreshold = 0f;
        }


        private sealed class RuntimePlant
        {
            public string SourceId;
            public ushort BlockId;

            public List<string> Biomes;

            public float Chance;

            public int MinY;
            public int MaxY;

            public bool Floor;
            public bool Ceiling;
            public bool Walls;

            public float NoiseScale;
            public float NoiseThreshold;
        }


        private static readonly List<RuntimePlant>
            plants =
                new List<RuntimePlant>();


        private static bool loaded;


        public static void Reload()
        {
            loaded = false;

            plants.Clear();

            EnsureLoaded();
        }


        public static void ApplyToChunk(
            WorldGenerator generator,
            WorldSettings settings,
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            if (
                generator == null ||
                settings == null ||
                data == null
            )
            {
                return;
            }


            EnsureLoaded();


            if (
                plants.Count == 0
            )
            {
                return;
            }


            int chunkMinX =
                chunkX *
                Chunk.SizeX;


            int chunkMinY =
                chunkY *
                Chunk.SizeY;


            for (
                int localX = 0;
                localX < Chunk.SizeX;
                localX++
            )
            {
                int worldX =
                    chunkMinX +
                    localX;


                // We need +/-1 Y to test floor/ceiling support.
                for (
                    int localY = 1;
                    localY < Chunk.SizeY - 1;
                    localY++
                )
                {
                    if (
                        data.GetBlock(
                            localX,
                            localY
                        ) != 0
                    )
                    {
                        continue;
                    }


                    // Surface/open sky normally has no cave background.
                    // This keeps cave vegetation underground.
                    if (
                        data.GetBackground(
                            localX,
                            localY
                        ) == 0
                    )
                    {
                        continue;
                    }


                    if (
                        data.GetFurniture(
                            localX,
                            localY
                        ) != 0
                    )
                    {
                        continue;
                    }


                    int worldY =
                        chunkMinY +
                        localY;


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
                        continue;
                    }


                    bool floor =
                        data.GetBlock(
                            localX,
                            localY - 1
                        ) !=
                        0;


                    bool ceiling =
                        data.GetBlock(
                            localX,
                            localY + 1
                        ) !=
                        0;


                    bool wall =
                        (
                            localX > 0 &&
                            data.GetBlock(
                                localX - 1,
                                localY
                            ) != 0
                        )
                        ||
                        (
                            localX < Chunk.SizeX - 1 &&
                            data.GetBlock(
                                localX + 1,
                                localY
                            ) != 0
                        );


                    for (
                        int i = 0;
                        i < plants.Count;
                        i++
                    )
                    {
                        RuntimePlant plant =
                            plants[i];


                        if (
                            plant == null ||
                            plant.BlockId == 0
                        )
                        {
                            continue;
                        }


                        if (
                            worldY <
                            Mathf.Min(
                                plant.MinY,
                                plant.MaxY
                            )
                            ||
                            worldY >
                            Mathf.Max(
                                plant.MinY,
                                plant.MaxY
                            )
                        )
                        {
                            continue;
                        }


                        if (
                            !BiomeMatches(
                                caveBiomeId,
                                plant.Biomes
                            )
                        )
                        {
                            continue;
                        }


                        bool validSupport =
                            (
                                plant.Floor &&
                                floor
                            )
                            ||
                            (
                                plant.Ceiling &&
                                ceiling
                            )
                            ||
                            (
                                plant.Walls &&
                                wall
                            );


                        if (!validSupport)
                        {
                            continue;
                        }


                        if (
                            plant.NoiseThreshold >
                            0f
                        )
                        {
                            float scale =
                                Mathf.Max(
                                    0.00001f,
                                    plant.NoiseScale
                                );


                            float patch =
                                Mathf.PerlinNoise(
                                    (
                                        worldX +
                                        settings.Seed *
                                        0.1947f +
                                        i *
                                        97.13f
                                    )
                                    *
                                    scale,

                                    (
                                        worldY +
                                        settings.Seed *
                                        0.4181f +
                                        i *
                                        41.37f
                                    )
                                    *
                                    scale
                                );


                            if (
                                patch <
                                Mathf.Clamp01(
                                    plant.NoiseThreshold
                                )
                            )
                            {
                                continue;
                            }
                        }


                        float roll =
                            Hash01(
                                settings.Seed,
                                worldX,
                                worldY,
                                i
                            );


                        if (
                            roll >
                            Mathf.Clamp01(
                                plant.Chance
                            )
                        )
                        {
                            continue;
                        }


                        if (
                            data.SetFurniture(
                                localX,
                                localY,
                                plant.BlockId
                            )
                        )
                        {
                            break;
                        }
                    }
                }
            }
        }


        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }


            loaded = true;

            plants.Clear();


            LoadDedicatedCaveFiles();

            LoadCaveProfilesFromSurfaceFlora();


            Debug.Log(
                "CAVE VEGETATION V4: loaded runtime plants = " +
                plants.Count
            );
        }


        private static void LoadDedicatedCaveFiles()
        {
            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Vegetation",
                    "Cave"
                );


            if (
                !Directory.Exists(
                    folder
                )
            )
            {
                return;
            }


            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );


            Array.Sort(
                files,
                StringComparer.OrdinalIgnoreCase
            );


            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {
                try
                {
                    CaveVegetationFile definition =
                        JsonUtility.FromJson<
                            CaveVegetationFile
                        >(
                            File.ReadAllText(
                                files[i]
                            )
                        );


                    if (
                        definition == null ||
                        string.IsNullOrWhiteSpace(
                            definition.BlockId
                        )
                    )
                    {
                        continue;
                    }


                    if (
                        !string.IsNullOrWhiteSpace(
                            definition.SpawnType
                        )
                        &&
                        !string.Equals(
                            definition.SpawnType,
                            "Cave",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }


                    ushort blockId =
                        ResolveBlockId(
                            definition.BlockId
                        );


                    if (blockId == 0)
                    {
                        Debug.LogWarning(
                            "CAVE VEGETATION V4: block is not registered: " +
                            definition.BlockId +
                            " | file=" +
                            files[i]
                        );


                        continue;
                    }


                    bool anySurface =
                        definition.Floor ||
                        definition.Ceiling ||
                        definition.Walls;


                    plants.Add(
                        new RuntimePlant
                        {
                            SourceId =
                                definition.ID,

                            BlockId =
                                blockId,

                            Biomes =
                                definition.Biomes != null
                                    ? definition.Biomes
                                    : new List<string>(),

                            Chance =
                                definition.Chance,

                            MinY =
                                definition.MinY,

                            MaxY =
                                definition.MaxY,

                            Floor =
                                anySurface
                                    ? definition.Floor
                                    : true,

                            Ceiling =
                                definition.Ceiling,

                            Walls =
                                definition.Walls,

                            NoiseScale =
                                definition.NoiseScale,

                            NoiseThreshold =
                                definition.NoiseThreshold
                        }
                    );
                }
                catch (
                    Exception exception
                )
                {
                    Debug.LogWarning(
                        "CAVE VEGETATION V4: failed to load " +
                        files[i] +
                        " | " +
                        exception.Message
                    );
                }
            }
        }


        /// <summary>
        /// ancient_caves_flora.json currently lives in SurfaceFlora but names
        /// game:ancient_caves. The normal surface generator can never match
        /// that to a surface biome. V4 intentionally reuses such profiles as
        /// cave-floor flora.
        /// </summary>
        private static void LoadCaveProfilesFromSurfaceFlora()
        {
            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "SurfaceFlora"
                );


            if (
                !Directory.Exists(
                    folder
                )
            )
            {
                return;
            }


            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );


            Array.Sort(
                files,
                StringComparer.OrdinalIgnoreCase
            );


            for (
                int fileIndex = 0;
                fileIndex < files.Length;
                fileIndex++
            )
            {
                try
                {
                    SurfaceFloraProfile profile =
                        JsonUtility.FromJson<
                            SurfaceFloraProfile
                        >(
                            File.ReadAllText(
                                files[fileIndex]
                            )
                        );


                    if (
                        profile == null ||
                        !profile.Enabled ||
                        profile.Biomes == null ||
                        profile.Biomes.Count == 0 ||
                        profile.Plants == null
                    )
                    {
                        continue;
                    }


                    // Only accept this profile here if at least one of its
                    // biome names is a registered cave biome at some point.
                    // We cannot enumerate the internal registry publicly, so
                    // use a conservative naming rule: profiles containing
                    // "cave" are cave-compatible; ordinary plains/desert/snow
                    // profiles remain surface-only.
                    bool caveNamed =
                        false;


                    for (
                        int b = 0;
                        b < profile.Biomes.Count;
                        b++
                    )
                    {
                        string normalized =
                            NormalizeBiomeId(
                                profile.Biomes[b]
                            );


                        if (
                            normalized.IndexOf(
                                "cave",
                                StringComparison.OrdinalIgnoreCase
                            )
                            >= 0
                        )
                        {
                            caveNamed = true;

                            break;
                        }
                    }


                    if (!caveNamed)
                    {
                        continue;
                    }


                    for (
                        int p = 0;
                        p < profile.Plants.Count;
                        p++
                    )
                    {
                        SurfaceFloraEntry entry =
                            profile.Plants[p];


                        if (
                            entry == null ||
                            string.IsNullOrWhiteSpace(
                                entry.BlockId
                            )
                        )
                        {
                            continue;
                        }


                        ushort blockId =
                            ResolveBlockId(
                                entry.BlockId
                            );


                        if (blockId == 0)
                        {
                            continue;
                        }


                        plants.Add(
                            new RuntimePlant
                            {
                                SourceId =
                                    profile.ID,

                                BlockId =
                                    blockId,

                                Biomes =
                                    profile.Biomes,

                                Chance =
                                    entry.Chance,

                                MinY =
                                    -1000000,

                                MaxY =
                                    1000000,

                                Floor =
                                    true,

                                Ceiling =
                                    false,

                                Walls =
                                    false,

                                NoiseScale =
                                    entry.NoiseScale,

                                NoiseThreshold =
                                    entry.NoiseThreshold
                            }
                        );
                    }
                }
                catch (
                    Exception exception
                )
                {
                    Debug.LogWarning(
                        "CAVE VEGETATION V4: failed to inspect SurfaceFlora file " +
                        files[fileIndex] +
                        " | " +
                        exception.Message
                    );
                }
            }
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


        private static bool BiomeMatches(
            string currentBiome,
            List<string> allowed
        )
        {
            if (
                allowed == null ||
                allowed.Count == 0
            )
            {
                return true;
            }


            string current =
                NormalizeBiomeId(
                    currentBiome
                );


            for (
                int i = 0;
                i < allowed.Count;
                i++
            )
            {
                string candidate =
                    NormalizeBiomeId(
                        allowed[i]
                    );


                if (
                    string.Equals(
                        current,
                        candidate,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }


            return false;
        }


        private static string NormalizeBiomeId(
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


            string result =
                value.Trim();


            int separator =
                result.LastIndexOf(
                    ':'
                );


            if (
                separator >= 0 &&
                separator < result.Length - 1
            )
            {
                result =
                    result.Substring(
                        separator + 1
                    );
            }


            return result;
        }


        private static float Hash01(
            int seed,
            int x,
            int y,
            int index
        )
        {
            unchecked
            {
                uint h =
                    (uint)seed;


                h ^=
                    (uint)x *
                    374761393u;


                h ^=
                    (uint)y *
                    668265263u;


                h ^=
                    (uint)index *
                    2246822519u;


                h ^=
                    h >> 13;


                h *=
                    1274126177u;


                h ^=
                    h >> 16;


                return
                    (
                        h &
                        0x00FFFFFFu
                    )
                    /
                    16777215f;
            }
        }
    }
}

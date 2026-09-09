using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.World.Biomes
{
    public static class BiomeRegistry
    {
        private static readonly Dictionary<string, BiomeDefinition>
            definitions =
            new Dictionary<string, BiomeDefinition>(
                StringComparer.OrdinalIgnoreCase
            );

        private static readonly List<BiomeDefinition>
            ordered =
            new List<BiomeDefinition>();

        private static bool initialized;


        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            Reload();
        }


        public static void Reload()
        {
            definitions.Clear();
            ordered.Clear();

            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Biomes"
                );

            if (!Directory.Exists(folder))
            {
                Debug.LogError(
                    "BIOME REGISTRY: folder not found: " +
                    folder
                );

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
                LoadFile(files[i]);
            }

            ordered.Sort(
                (a, b) =>
                    string.Compare(
                        a.ID,
                        b.ID,
                        StringComparison.OrdinalIgnoreCase
                    )
            );

            Debug.Log(
                "BIOME REGISTRY: loaded " +
                ordered.Count +
                " biomes."
            );
        }


        private static void LoadFile(
            string filePath
        )
        {
            try
            {
                string json =
                    File.ReadAllText(
                        filePath
                    );

                BiomeDefinition biome =
                    JsonUtility.FromJson<BiomeDefinition>(
                        json
                    );

                if (
                    biome == null ||
                    string.IsNullOrWhiteSpace(
                        biome.ID
                    )
                )
                {
                    Debug.LogError(
                        "BIOME REGISTRY: invalid file: " +
                        filePath
                    );

                    return;
                }

                biome.ID = biome.ID.Trim();

                if (
                    string.IsNullOrWhiteSpace(
                        biome.DisplayName
                    )
                )
                {
                    biome.DisplayName =
                        biome.ID;
                }

                if (biome.Terrain == null)
                    biome.Terrain = new BiomeTerrainSettings();

                if (biome.Caves == null)
                    biome.Caves = new BiomeCaveSettings();

                if (biome.Climate == null)
                    biome.Climate = new BiomeClimateSettings();

                biome.Weight =
                    Mathf.Max(
                        0.001f,
                        biome.Weight
                    );

                biome.Terrain.SoilDepth =
                    Mathf.Max(
                        0,
                        biome.Terrain.SoilDepth
                    );

                if (
                    definitions.ContainsKey(
                        biome.ID
                    )
                )
                {
                    Debug.LogError(
                        "BIOME REGISTRY: duplicate ID: " +
                        biome.ID +
                        " in " +
                        filePath
                    );

                    return;
                }

                definitions.Add(
                    biome.ID,
                    biome
                );

                ordered.Add(
                    biome
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "BIOME REGISTRY: failed to load " +
                    filePath +
                    "\n" +
                    exception
                );
            }
        }


        public static IReadOnlyList<BiomeDefinition> GetAll()
        {
            Initialize();
            return ordered;
        }


        public static BiomeDefinition Get(
            string biomeID
        )
        {
            Initialize();

            definitions.TryGetValue(
                biomeID,
                out BiomeDefinition biome
            );

            return biome;
        }
    }
}
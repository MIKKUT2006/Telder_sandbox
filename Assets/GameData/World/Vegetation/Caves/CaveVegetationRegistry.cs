using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.World.Vegetation.Caves
{
    public static class CaveVegetationRegistry
    {
        private static readonly List<CaveVegetationDefinition> Definitions =
            new List<CaveVegetationDefinition>();

        private static bool loaded;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration
        )]
        private static void ResetRuntime()
        {
            loaded = false;
            Definitions.Clear();
        }

        public static IReadOnlyList<CaveVegetationDefinition> GetAll()
        {
            EnsureLoaded();
            return Definitions;
        }

        public static void EnsureLoaded()
        {
            if (!loaded)
                Reload();
        }

        public static void Reload()
        {
            loaded = true;
            Definitions.Clear();

            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Vegetation",
                    "Cave"
                );

            Debug.Log(
                "CAVE VEGETATION: loading folder: " +
                folder
            );

            if (!Directory.Exists(folder))
            {
                Debug.LogWarning(
                    "CAVE VEGETATION: folder does not exist."
                );
                return;
            }

            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );

            Debug.Log(
                "CAVE VEGETATION: JSON files found = " +
                files.Length
            );

            for (int i = 0; i < files.Length; i++)
                TryLoad(files[i]);

            Debug.Log(
                "CAVE VEGETATION: loaded rules = " +
                Definitions.Count
            );
        }

        private static void TryLoad(string path)
        {
            try
            {
                string json =
                    File.ReadAllText(path);

                CaveVegetationDefinition definition =
                    JsonUtility.FromJson<CaveVegetationDefinition>(
                        json
                    );

                if (definition == null)
                    return;

                if (string.IsNullOrWhiteSpace(definition.BlockId))
                    return;

                if (!string.IsNullOrWhiteSpace(definition.SpawnType) &&
                    !string.Equals(
                        definition.SpawnType,
                        "Cave",
                        StringComparison.OrdinalIgnoreCase))
                    return;

                definition.Chance =
                    Mathf.Clamp01(definition.Chance);

                if (definition.Biomes == null)
                    definition.Biomes = new List<string>();

                Definitions.Add(definition);

                Debug.Log(
                    "CAVE VEGETATION RULE: " +
                    definition.ID +
                    " | Block=" + definition.BlockId +
                    " | Chance=" + definition.Chance +
                    " | Floor=" + definition.Floor +
                    " | Ceiling=" + definition.Ceiling +
                    " | Walls=" + definition.Walls +
                    " | Biomes=" +
                    string.Join(", ", definition.Biomes)
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "CAVE VEGETATION: failed to load " +
                    path + "\n" + exception
                );
            }
        }
    }
}

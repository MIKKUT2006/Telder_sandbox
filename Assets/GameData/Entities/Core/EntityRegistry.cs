using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Entities
{
    public static class EntityRegistry
    {
        private static readonly Dictionary<string, EntityDefinition> Data =
            new Dictionary<string, EntityDefinition>(StringComparer.OrdinalIgnoreCase);

        private static bool loaded;

        public static IEnumerable<EntityDefinition> All
        {
            get
            {
                Initialize();
                return Data.Values;
            }
        }

        public static void Initialize()
        {
            if (!loaded)
                Reload();
        }

        public static void Reload()
        {
            loaded = true;
            Data.Clear();

            string folder = Path.Combine(Application.dataPath, "GameData", "Entities", "Definitions");
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning("ENTITIES: Definition folder not found: " + folder);
                return;
            }

            string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
                TryLoad(files[i]);

            Debug.Log("ENTITIES: Loaded " + Data.Count + " entity definitions.");
        }

        public static bool TryGet(string id, out EntityDefinition definition)
        {
            Initialize();
            definition = null;

            if (string.IsNullOrWhiteSpace(id))
                return false;

            return Data.TryGetValue(id.Trim(), out definition);
        }

        private static void TryLoad(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                EntityDefinition definition = JsonUtility.FromJson<EntityDefinition>(json);

                if (definition == null || string.IsNullOrWhiteSpace(definition.ID))
                {
                    Debug.LogWarning("ENTITIES: Invalid definition: " + Path.GetFileName(path));
                    return;
                }

                definition.Normalize();
                Data[definition.ID.Trim()] = definition;
            }
            catch (Exception e)
            {
                Debug.LogWarning("ENTITIES: Failed to load " + Path.GetFileName(path) + ": " + e.Message);
            }
        }
    }
}

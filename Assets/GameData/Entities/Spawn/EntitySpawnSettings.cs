using System;
using System.IO;
using UnityEngine;

namespace Game.Entities.Spawn
{
    [Serializable]
    public sealed class EntitySpawnSettingsData
    {
        [Min(0)] public int GlobalMaxAlive = 24;
        [Min(0.1f)] public float SpawnInterval = 1.5f;
        [Min(1)] public int AttemptsPerTick = 8;
        [Min(1f)] public float DespawnDistance = 58f;
        [Min(0.1f)] public float SpawnSafetyRadius = 10f;
        [Min(1)] public int VerticalSearchCells = 14;
    }

    public static class EntitySpawnSettings
    {
        private static EntitySpawnSettingsData data;

        public static EntitySpawnSettingsData Data
        {
            get
            {
                if (data == null)
                    data = Load();
                return data;
            }
        }

        private static EntitySpawnSettingsData Load()
        {
            EntitySpawnSettingsData result = new EntitySpawnSettingsData();
            string path = Path.Combine(Application.dataPath, "GameData", "Entities", "entity_spawn_settings.json");

            if (!File.Exists(path))
                return result;

            try
            {
                string json = File.ReadAllText(path);
                EntitySpawnSettingsData parsed = JsonUtility.FromJson<EntitySpawnSettingsData>(json);
                if (parsed != null)
                    result = parsed;
            }
            catch (Exception e)
            {
                Debug.LogWarning("ENTITIES: Spawn settings load failed: " + e.Message);
            }

            result.GlobalMaxAlive = Mathf.Max(0, result.GlobalMaxAlive);
            result.SpawnInterval = Mathf.Max(0.1f, result.SpawnInterval);
            result.AttemptsPerTick = Mathf.Max(1, result.AttemptsPerTick);
            result.DespawnDistance = Mathf.Max(1f, result.DespawnDistance);
            result.SpawnSafetyRadius = Mathf.Max(0.1f, result.SpawnSafetyRadius);
            result.VerticalSearchCells = Mathf.Max(1, result.VerticalSearchCells);
            return result;
        }
    }
}

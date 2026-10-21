using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Dimensions
{
    /*
     * V8: сид измерения определяется ТОЛЬКО полем сида.
     * Имя — просто метка, на сид оно не влияет.
     * Сид может быть числом или произвольным текстом (хешируется).
     */
    public static class DimensionDatabase
    {
        public const string StartDimension = "M113";
        private const string DatabaseKey = "GAME_DIMENSION_DATABASE_V7";
        private const string LegacyVisitedKey = "GAME_VISITED_DIMENSIONS_V6";

        [Serializable]
        private class DimensionRecord
        {
            public string Name;
            public int Seed;
        }

        [Serializable]
        private class DimensionRecordList
        {
            public List<DimensionRecord> Items = new List<DimensionRecord>();
        }

        private static readonly List<DimensionRecord> records = new List<DimensionRecord>();
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            records.Clear();

            string json = PlayerPrefs.GetString(DatabaseKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    DimensionRecordList wrapper =
                        JsonUtility.FromJson<DimensionRecordList>(json);

                    if (wrapper != null && wrapper.Items != null)
                    {
                        for (int i = 0; i < wrapper.Items.Count; i++)
                        {
                            DimensionRecord record = wrapper.Items[i];
                            if (record == null ||
                                string.IsNullOrWhiteSpace(record.Name) ||
                                record.Seed == 0)
                                continue;

                            if (FindRecord(record.Name) == null)
                                records.Add(record);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Dimension database load failed: " + exception.Message);
                }
            }

            // Миграция старого V6 списка посещённых миров.
            if (records.Count == 0)
            {
                string legacy = PlayerPrefs.GetString(LegacyVisitedKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(legacy))
                {
                    string[] names = legacy.Split('|');
                    for (int i = 0; i < names.Length; i++)
                    {
                        string name = Normalize(names[i]);
                        if (string.IsNullOrWhiteSpace(name) || FindRecord(name) != null)
                            continue;

                        records.Add(new DimensionRecord
                        {
                            Name = name,
                            Seed = GetSeedFromName(name)
                        });
                    }
                }
            }

            if (FindRecord(StartDimension) == null)
            {
                records.Insert(0, new DimensionRecord
                {
                    Name = StartDimension,
                    Seed = GetSeedFromName(StartDimension)
                });
            }

            Save();
        }

        public static IReadOnlyList<DimensionDefinition> Visited
        {
            get
            {
                Initialize();
                List<DimensionDefinition> result = new List<DimensionDefinition>(records.Count);
                for (int i = 0; i < records.Count; i++)
                    result.Add(ToDefinition(records[i]));
                return result;
            }
        }

        public static DimensionDefinition GetOrCreate(string dimensionName)
        {
            Initialize();
            dimensionName = Normalize(dimensionName);

            if (string.IsNullOrWhiteSpace(dimensionName))
                return GetOrCreate(StartDimension);

            DimensionRecord existing = FindRecord(dimensionName);
            if (existing != null)
                return ToDefinition(existing);

            int seed;
            if (!TryExtractRandomSeed(dimensionName, out seed))
                seed = GetSeedFromName(dimensionName);

            DimensionRecord record = new DimensionRecord
            {
                Name = dimensionName,
                Seed = seed
            };

            records.Add(record);
            Save();
            return ToDefinition(record);
        }

        public static DimensionDefinition CreateFromInput(string dimensionName)
        {
            return CreateFromInput(dimensionName, string.Empty, 0);
        }

        // =========================================================
        // Сид берётся ТОЛЬКО из seedInput.
        // Имя измерения на сид не влияет никак.
        //   - пусто          -> случайный сид
        //   - "12345"        -> 12345
        //   - "Hello"        -> детерминированный хеш строки
        //   - "0"            -> хеш строки "0" (не ноль)
        // =========================================================
        public static DimensionDefinition CreateFromInput(
            string dimensionName,
            string seedInput,
            int gunLevel)
        {
            Initialize();

            dimensionName = Normalize(dimensionName);
            seedInput = Normalize(seedInput);

            int seed = string.IsNullOrWhiteSpace(seedInput)
                ? UnityEngine.Random.Range(1, int.MaxValue)
                : ParseSeedOrHash(seedInput);

            if (string.IsNullOrWhiteSpace(dimensionName))
            {
                string baseName = "RIFT-" +
                    unchecked((uint)seed).ToString("X8");

                dimensionName = baseName;

                int suffix = 2;
                while (FindRecord(dimensionName) != null)
                {
                    dimensionName = baseName + " (" + suffix + ")";
                    suffix++;
                }
            }
            else if (FindRecord(dimensionName) != null)
            {
                // Имя уже занято — разводим суффиксом.
                // Сид НЕ подменяем на старую запись: он берётся из поля.
                string baseName = dimensionName;
                int suffix = 2;

                do
                {
                    dimensionName = baseName + " (" + suffix + ")";
                    suffix++;
                }
                while (FindRecord(dimensionName) != null);
            }

            DimensionRecord record = new DimensionRecord
            {
                Name = dimensionName,
                Seed = seed
            };

            records.Add(record);
            Save();
            return ToDefinition(record);
        }

        // =========================================================
        // LookupByName — найти уже существующее измерение по имени.
        // Используется, когда игрок жмёт "ОТКРЫТЬ" на карточке
        // или стреляет в уже созданное измерение.
        // =========================================================
        public static DimensionDefinition LookupByName(string dimensionName)
        {
            Initialize();
            dimensionName = Normalize(dimensionName);

            if (string.IsNullOrWhiteSpace(dimensionName))
                return null;

            DimensionRecord existing = FindRecord(dimensionName);
            return existing != null ? ToDefinition(existing) : null;
        }

        // =========================================================
        // PreviewNamed(name) — семантически это "найти по имени".
        // Оставлено для совместимости со старыми вызовами.
        // =========================================================
        public static DimensionDefinition PreviewNamed(string dimensionName)
        {
            return LookupByName(dimensionName);
        }

        // =========================================================
        // PreviewNamed(name, seedInput) — предпросмотр в меню создания.
        // Зависит ТОЛЬКО от seedInput. Имя в сид не подмешивается.
        // Пустой сид -> null, UI покажет "СЛУЧАЙНО".
        // =========================================================
        public static DimensionDefinition PreviewNamed(
            string dimensionName,
            string seedInput)
        {
            Initialize();

            dimensionName = Normalize(dimensionName);
            seedInput = Normalize(seedInput);

            if (string.IsNullOrWhiteSpace(seedInput))
                return null;

            int seed = ParseSeedOrHash(seedInput);

            string previewName =
                string.IsNullOrWhiteSpace(dimensionName)
                    ? "RIFT-" + unchecked((uint)seed).ToString("X8")
                    : dimensionName;

            return new DimensionDefinition(previewName, seed);
        }

        public static int GenerateRandomSeed()
        {
            int seed;

            do
            {
                seed = UnityEngine.Random.Range(1, int.MaxValue);
            }
            while (seed == 0);

            return seed;
        }

        // Старый API оставлен для совместимости с существующими вызовами.
        public static DimensionDefinition CreateRandom()
        {
            return CreateRandom(int.MaxValue);
        }

        public static DimensionDefinition CreateRandom(int maxTier)
        {
            Initialize();
            maxTier = Math.Max(1, maxTier);

            int seed;
            string name;
            DimensionTypeDefinition type;

            do
            {
                seed = UnityEngine.Random.Range(1, int.MaxValue);
                name = "RIFT-" + unchecked((uint)seed).ToString("X8");
                type = DimensionTypeRegistry.GetForSeed(seed);
            }
            while (FindRecord(name) != null || type == null || type.Tier > maxTier);

            DimensionRecord record = new DimensionRecord
            {
                Name = name,
                Seed = seed
            };

            records.Add(record);
            Save();
            return ToDefinition(record);
        }

        public static int GetSeedFromName(string dimensionName)
        {
            dimensionName = Normalize(dimensionName);
            if (string.IsNullOrEmpty(dimensionName))
                dimensionName = StartDimension;

            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < dimensionName.Length; i++)
                {
                    char c = char.ToUpperInvariant(dimensionName[i]);
                    hash ^= c;
                    hash *= 16777619u;
                }

                int seed = (int)(hash & 0x7FFFFFFF);
                return seed == 0 ? 1 : seed;
            }
        }

        // =========================================================
        // Внутренние помощники для работы с текстовым сидом.
        // =========================================================

        // "12345" -> 12345
        // "Hello" -> детерминированный хеш
        // Регистр значим: "Hello" != "hello".
        private static int ParseSeedOrHash(string seedInput)
        {
            seedInput = Normalize(seedInput);

            if (int.TryParse(seedInput, out int numeric) && numeric != 0)
                return numeric;

            return HashTextToSeed(seedInput);
        }

        private static int HashTextToSeed(string text)
        {
            unchecked
            {
                uint hash = 2166136261u; // FNV-1a
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619u;
                }

                int seed = (int)(hash & 0x7FFFFFFF);
                return seed == 0 ? 1 : seed;
            }
        }

        // =========================================================

        private static DimensionDefinition ToDefinition(DimensionRecord record)
        {
            return new DimensionDefinition(record.Name, record.Seed);
        }

        private static DimensionRecord FindRecord(string name)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (string.Equals(records[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return records[i];
            }

            return null;
        }

        private static bool TryExtractRandomSeed(string name, out int seed)
        {
            seed = 0;
            const string prefix = "RIFT-";

            if (name.Length != prefix.Length + 8 ||
                !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string hex = name.Substring(prefix.Length);
            if (uint.TryParse(hex,
                System.Globalization.NumberStyles.HexNumber,
                null,
                out uint raw))
            {
                seed = (int)(raw & 0x7FFFFFFF);
                if (seed == 0)
                    seed = 1;
                return true;
            }

            return false;
        }

        private static void Save()
        {
            DimensionRecordList wrapper = new DimensionRecordList();
            for (int i = 0; i < records.Count; i++)
                wrapper.Items.Add(records[i]);

            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(DatabaseKey, json);
            PlayerPrefs.Save();
        }

        private static string Normalize(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }
    }
}
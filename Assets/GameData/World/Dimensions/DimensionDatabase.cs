using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Dimensions
{
    /*
     * V7 хранит не только имя, но и seed.
     *
     * Это необходимо для случайных измерений:
     * если игрок оставил имя пустым, seed выбирается случайно,
     * а измерение получает техническое имя RIFT-XXXXXXXX.
     */
    public static class DimensionDatabase
    {
        public const string StartDimension =
            "M113";

        private const string DatabaseKey =
            "GAME_DIMENSION_DATABASE_V7";

        private const string LegacyVisitedKey =
            "GAME_VISITED_DIMENSIONS_V6";


        [Serializable]
        private class DimensionRecord
        {
            public string Name;
            public int Seed;
        }


        [Serializable]
        private class DimensionRecordList
        {
            public List<DimensionRecord> Items =
                new List<DimensionRecord>();
        }


        private static readonly List<DimensionRecord>
            records =
            new List<DimensionRecord>();


        private static bool initialized;


        public static void Initialize()
        {
            if (initialized)
                return;

            initialized =
                true;

            records.Clear();

            string json =
                PlayerPrefs.GetString(
                    DatabaseKey,
                    string.Empty
                );


            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    DimensionRecordList wrapper =
                        JsonUtility.FromJson<
                            DimensionRecordList
                        >(json);

                    if (
                        wrapper != null &&
                        wrapper.Items != null
                    )
                    {
                        for (
                            int i = 0;
                            i < wrapper.Items.Count;
                            i++
                        )
                        {
                            DimensionRecord record =
                                wrapper.Items[i];

                            if (
                                record == null ||
                                string.IsNullOrWhiteSpace(
                                    record.Name
                                ) ||
                                record.Seed == 0
                            )
                            {
                                continue;
                            }

                            if (
                                FindRecord(
                                    record.Name
                                ) == null
                            )
                            {
                                records.Add(
                                    record
                                );
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "Dimension database load failed: " +
                        exception.Message
                    );
                }
            }


            /*
             * Миграция старого V6 списка посещённых миров.
             */
            if (records.Count == 0)
            {
                string legacy =
                    PlayerPrefs.GetString(
                        LegacyVisitedKey,
                        string.Empty
                    );

                if (!string.IsNullOrWhiteSpace(legacy))
                {
                    string[] names =
                        legacy.Split('|');

                    for (
                        int i = 0;
                        i < names.Length;
                        i++
                    )
                    {
                        string name =
                            Normalize(
                                names[i]
                            );

                        if (
                            string.IsNullOrWhiteSpace(name) ||
                            FindRecord(name) != null
                        )
                        {
                            continue;
                        }

                        records.Add(
                            new DimensionRecord
                            {
                                Name = name,
                                Seed = GetSeedFromName(name)
                            }
                        );
                    }
                }
            }


            if (
                FindRecord(
                    StartDimension
                ) == null
            )
            {
                records.Insert(
                    0,
                    new DimensionRecord
                    {
                        Name = StartDimension,
                        Seed = GetSeedFromName(
                            StartDimension
                        )
                    }
                );
            }

            Save();
        }


        public static IReadOnlyList<DimensionDefinition> Visited
        {
            get
            {
                Initialize();

                List<DimensionDefinition> result =
                    new List<DimensionDefinition>(
                        records.Count
                    );

                for (
                    int i = 0;
                    i < records.Count;
                    i++
                )
                {
                    result.Add(
                        ToDefinition(
                            records[i]
                        )
                    );
                }

                return result;
            }
        }


        /*
         * Для обычного именованного измерения:
         * имя -> всегда тот же seed.
         *
         * Если такое измерение уже сохранено,
         * используем сохранённый seed.
         */
        public static DimensionDefinition GetOrCreate(
            string dimensionName
        )
        {
            Initialize();

            dimensionName =
                Normalize(
                    dimensionName
                );

            if (string.IsNullOrWhiteSpace(dimensionName))
            {
                return GetOrCreate(
                    StartDimension
                );
            }


            DimensionRecord existing =
                FindRecord(
                    dimensionName
                );

            if (existing != null)
            {
                return ToDefinition(
                    existing
                );
            }


            int seed;

            /*
             * Случайные миры кодируют seed в имени.
             * Поэтому даже после очистки кэша такой мир
             * можно восстановить из его имени.
             */
            if (
                !TryExtractRandomSeed(
                    dimensionName,
                    out seed
                )
            )
            {
                seed =
                    GetSeedFromName(
                        dimensionName
                    );
            }


            DimensionRecord record =
                new DimensionRecord
                {
                    Name =
                        dimensionName,

                    Seed =
                        seed
                };


            records.Add(
                record
            );

            Save();

            return ToDefinition(
                record
            );
        }


        /*
         * Используется кнопкой CREATE RIFT.
         *
         * Есть имя:
         *     seed = hash(name)
         *
         * Поле пустое:
         *     seed = random
         *     name = RIFT-XXXXXXXX
         */
        public static DimensionDefinition CreateFromInput(
            string dimensionName
        )
        {
            Initialize();

            dimensionName =
                Normalize(
                    dimensionName
                );

            if (!string.IsNullOrWhiteSpace(dimensionName))
            {
                return GetOrCreate(
                    dimensionName
                );
            }

            return CreateRandom();
        }


        public static DimensionDefinition CreateRandom()
        {
            Initialize();

            int seed;
            string name;

            do
            {
                seed =
                    UnityEngine.Random.Range(
                        1,
                        int.MaxValue
                    );

                name =
                    "RIFT-" +
                    unchecked((uint)seed)
                        .ToString("X8");
            }
            while (
                FindRecord(name) != null
            );


            DimensionRecord record =
                new DimensionRecord
                {
                    Name = name,
                    Seed = seed
                };


            records.Add(
                record
            );

            Save();

            return ToDefinition(
                record
            );
        }


        /*
         * Preview НЕ сохраняет измерение.
         */
        public static DimensionDefinition PreviewNamed(
            string dimensionName
        )
        {
            Initialize();

            dimensionName =
                Normalize(
                    dimensionName
                );

            if (string.IsNullOrWhiteSpace(dimensionName))
            {
                return null;
            }


            DimensionRecord existing =
                FindRecord(
                    dimensionName
                );

            if (existing != null)
            {
                return ToDefinition(
                    existing
                );
            }


            return new DimensionDefinition(
                dimensionName,
                GetSeedFromName(
                    dimensionName
                )
            );
        }


        public static int GetSeedFromName(
            string dimensionName
        )
        {
            dimensionName =
                Normalize(
                    dimensionName
                );

            if (string.IsNullOrEmpty(dimensionName))
            {
                dimensionName =
                    StartDimension;
            }


            unchecked
            {
                uint hash =
                    2166136261u;

                for (
                    int i = 0;
                    i < dimensionName.Length;
                    i++
                )
                {
                    char c =
                        char.ToUpperInvariant(
                            dimensionName[i]
                        );

                    hash ^= c;

                    hash *=
                        16777619u;
                }


                int seed =
                    (int)(
                        hash &
                        0x7FFFFFFF
                    );

                return seed == 0
                    ? 1
                    : seed;
            }
        }


        private static DimensionDefinition ToDefinition(
            DimensionRecord record
        )
        {
            return new DimensionDefinition(
                record.Name,
                record.Seed
            );
        }


        private static DimensionRecord FindRecord(
            string name
        )
        {
            for (
                int i = 0;
                i < records.Count;
                i++
            )
            {
                if (
                    string.Equals(
                        records[i].Name,
                        name,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return records[i];
                }
            }

            return null;
        }


        private static bool TryExtractRandomSeed(
            string name,
            out int seed
        )
        {
            seed = 0;

            const string prefix =
                "RIFT-";

            if (
                name.Length !=
                prefix.Length + 8 ||
                !name.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return false;
            }


            string hex =
                name.Substring(
                    prefix.Length
                );


            if (
                uint.TryParse(
                    hex,
                    System.Globalization.NumberStyles.HexNumber,
                    null,
                    out uint raw
                )
            )
            {
                seed =
                    (int)(
                        raw &
                        0x7FFFFFFF
                    );

                if (seed == 0)
                    seed = 1;

                return true;
            }

            return false;
        }


        private static void Save()
        {
            DimensionRecordList wrapper =
                new DimensionRecordList();

            for (
                int i = 0;
                i < records.Count;
                i++
            )
            {
                wrapper.Items.Add(
                    records[i]
                );
            }

            string json =
                JsonUtility.ToJson(
                    wrapper
                );

            PlayerPrefs.SetString(
                DatabaseKey,
                json
            );

            PlayerPrefs.Save();
        }


        private static string Normalize(
            string value
        )
        {
            return value == null
                ? string.Empty
                : value.Trim();
        }
    }
}
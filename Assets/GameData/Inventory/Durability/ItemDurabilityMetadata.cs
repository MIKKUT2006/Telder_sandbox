using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using UnityEngine;

using Game.Items;


namespace Game.Items.Durability
{
    [Serializable]
    public sealed class ItemDurabilityMetadataData
    {
        public string ID = "";

        public int Durability = 0;

        public string RepairItem = "";

        public int RepairItemCount = 0;

        public string UpgradeType = "";

        public int UpgradeValue = 0;

        public List<string> Tags =
            new List<string>();
    }


    /// <summary>
    /// Compatibility layer for the current project.
    ///
    /// It first tries to read durability fields from ItemDefinition by reflection.
    /// If the current ItemDefinition does not have those fields yet, it falls back
    /// to Assets/GameData/Items/**/*.json.
    ///
    /// This means the durability package does not require replacing your current
    /// ItemDefinition just to compile.
    /// </summary>
    public static class ItemDurabilityMetadata
    {
        private static readonly Dictionary<
            string,
            ItemDurabilityMetadataData
        > cache =
            new Dictionary<
                string,
                ItemDurabilityMetadataData
            >(
                StringComparer.OrdinalIgnoreCase
            );


        private static bool jsonScanned;


        public static bool TryGet(
            string itemId,
            out ItemDurabilityMetadataData data
        )
        {
            data =
                null;


            if (
                string.IsNullOrWhiteSpace(
                    itemId
                )
            )
            {
                return false;
            }


            if (
                cache.TryGetValue(
                    itemId,
                    out data
                )
            )
            {
                return true;
            }


            if (
                ItemRegistry.TryGet(
                    itemId,
                    out ItemDefinition definition
                )
            )
            {
                ItemDurabilityMetadataData reflected =
                    ReadFromDefinition(
                        itemId,
                        definition
                    );


                if (
                    reflected !=
                    null
                    &&
                    HasAnyDurabilityData(
                        reflected
                    )
                )
                {
                    cache[itemId] =
                        reflected;


                    data =
                        reflected;


                    return true;
                }
            }


            EnsureJsonScanned();


            return
                cache.TryGetValue(
                    itemId,
                    out data
                );
        }


        public static int GetDurability(
            string itemId
        )
        {
            return
                TryGet(
                    itemId,
                    out ItemDurabilityMetadataData data
                )
                    ? Mathf.Max(
                        0,
                        data.Durability
                    )
                    : 0;
        }


        public static bool HasTag(
            string itemId,
            string wantedTag
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    wantedTag
                )
                ||
                !TryGet(
                    itemId,
                    out ItemDurabilityMetadataData data
                )
                ||
                data.Tags ==
                null
            )
            {
                return false;
            }


            for (
                int i = 0;
                i < data.Tags.Count;
                i++
            )
            {
                if (
                    string.Equals(
                        data.Tags[i],
                        wantedTag,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }


            return false;
        }


        public static void Reload()
        {
            cache.Clear();

            jsonScanned =
                false;
        }


        private static ItemDurabilityMetadataData ReadFromDefinition(
            string itemId,
            ItemDefinition definition
        )
        {
            if (
                definition ==
                null
            )
            {
                return null;
            }


            ItemDurabilityMetadataData data =
                new ItemDurabilityMetadataData();


            data.ID =
                itemId;


            data.Durability =
                ReadIntMember(
                    definition,
                    "Durability"
                );


            data.RepairItem =
                ReadStringMember(
                    definition,
                    "RepairItem"
                );


            data.RepairItemCount =
                ReadIntMember(
                    definition,
                    "RepairItemCount"
                );


            data.UpgradeType =
                ReadStringMember(
                    definition,
                    "UpgradeType"
                );


            data.UpgradeValue =
                ReadIntMember(
                    definition,
                    "UpgradeValue"
                );


            data.Tags =
                ReadStringListMember(
                    definition,
                    "Tags"
                );


            return data;
        }


        private static void EnsureJsonScanned()
        {
            if (
                jsonScanned
            )
            {
                return;
            }


            jsonScanned =
                true;


            string root =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Items"
                );


            if (
                !Directory.Exists(
                    root
                )
            )
            {
                return;
            }


            string[] files;


            try
            {
                files =
                    Directory.GetFiles(
                        root,
                        "*.json",
                        SearchOption.AllDirectories
                    );
            }
            catch (
                Exception exception
            )
            {
                Debug.LogWarning(
                    "DURABILITY: Cannot scan item JSON folder: " +
                    exception.Message
                );


                return;
            }


            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {
                try
                {
                    string json =
                        File.ReadAllText(
                            files[i]
                        );


                    ItemDurabilityMetadataData data =
                        JsonUtility.FromJson<
                            ItemDurabilityMetadataData
                        >(
                            json
                        );


                    if (
                        data ==
                        null
                        ||
                        string.IsNullOrWhiteSpace(
                            data.ID
                        )
                    )
                    {
                        continue;
                    }


                    if (
                        !HasAnyDurabilityData(
                            data
                        )
                    )
                    {
                        continue;
                    }


                    cache[data.ID] =
                        data;
                }
                catch (
                    Exception exception
                )
                {
                    Debug.LogWarning(
                        "DURABILITY: Failed to read " +
                        files[i] +
                        ": " +
                        exception.Message
                    );
                }
            }
        }


        private static bool HasAnyDurabilityData(
            ItemDurabilityMetadataData data
        )
        {
            return
                data !=
                null
                &&
                (
                    data.Durability >
                    0
                    ||
                    !string.IsNullOrWhiteSpace(
                        data.RepairItem
                    )
                    ||
                    !string.IsNullOrWhiteSpace(
                        data.UpgradeType
                    )
                    ||
                    (
                        data.Tags !=
                        null
                        &&
                        data.Tags.Count >
                        0
                    )
                );
        }


        private static FieldInfo FindField(
            object instance,
            string name
        )
        {
            if (
                instance ==
                null
            )
            {
                return null;
            }


            return
                instance
                    .GetType()
                    .GetField(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.IgnoreCase
                    );
        }


        private static PropertyInfo FindProperty(
            object instance,
            string name
        )
        {
            if (
                instance ==
                null
            )
            {
                return null;
            }


            return
                instance
                    .GetType()
                    .GetProperty(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.IgnoreCase
                    );
        }


        private static object ReadMember(
            object instance,
            string name
        )
        {
            FieldInfo field =
                FindField(
                    instance,
                    name
                );


            if (
                field !=
                null
            )
            {
                return
                    field.GetValue(
                        instance
                    );
            }


            PropertyInfo property =
                FindProperty(
                    instance,
                    name
                );


            if (
                property !=
                null
                &&
                property.CanRead
            )
            {
                return
                    property.GetValue(
                        instance,
                        null
                    );
            }


            return null;
        }


        private static int ReadIntMember(
            object instance,
            string name
        )
        {
            object value =
                ReadMember(
                    instance,
                    name
                );


            if (
                value is int intValue
            )
            {
                return intValue;
            }


            if (
                value !=
                null
                &&
                int.TryParse(
                    value.ToString(),
                    out int parsed
                )
            )
            {
                return parsed;
            }


            return 0;
        }


        private static string ReadStringMember(
            object instance,
            string name
        )
        {
            object value =
                ReadMember(
                    instance,
                    name
                );


            return
                value !=
                null
                    ? value.ToString()
                    : "";
        }


        private static List<string> ReadStringListMember(
            object instance,
            string name
        )
        {
            List<string> result =
                new List<string>();


            object value =
                ReadMember(
                    instance,
                    name
                );


            if (
                value is IEnumerable<string> strings
            )
            {
                foreach (
                    string entry
                    in strings
                )
                {
                    if (
                        !string.IsNullOrWhiteSpace(
                            entry
                        )
                    )
                    {
                        result.Add(
                            entry
                        );
                    }
                }
            }


            return result;
        }
    }
}

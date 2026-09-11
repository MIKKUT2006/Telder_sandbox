using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using UnityEngine;

using Game.World.Dimensions;


namespace Game.World.Biomes.Caves
{
    /// <summary>
    /// Builds stable identifiers for the active dimension.
    ///
    /// A generated dimension has its own instance Name, while cave-biome
    /// configs usually want the dimension TYPE, for example "crystalline"
    /// or "volcanic". v31 compared only against Current.Name, so once the
    /// JSON Dimensions field started parsing correctly the biome could be
    /// rejected everywhere.
    /// </summary>
    internal static class CaveBiomeDimensionRuntime
    {
        private static string lastLoggedSignature =
            string.Empty;


        public static string[] GetCurrentKeys()
        {
            List<string> keys =
                new List<string>();


            object dimension =
                DimensionTravelRuntime.Current;


            if (
                dimension ==
                null
            )
            {
                return keys.ToArray();
            }


            AddMemberValue(
                keys,
                dimension,
                "Name"
            );


            AddMemberValue(
                keys,
                dimension,
                "ID"
            );


            AddMemberValue(
                keys,
                dimension,
                "Id"
            );


            AddMemberValue(
                keys,
                dimension,
                "Key"
            );


            object dimensionType =
                GetMemberValue(
                    dimension,
                    "Type"
                );


            AddIdentityValues(
                keys,
                dimensionType
            );


            string[] result =
                keys.ToArray();


            LogOnce(
                result
            );


            return result;
        }


        private static void AddIdentityValues(
            List<string> keys,
            object target
        )
        {
            if (
                target ==
                null
            )
            {
                return;
            }


            if (
                target is string stringTarget
            )
            {
                AddKey(
                    keys,
                    stringTarget
                );


                return;
            }


            string[] commonNames =
            {
                "ID",
                "Id",
                "Key",
                "Name",
                "DisplayName",
                "TypeName"
            };


            for (
                int i = 0;
                i < commonNames.Length;
                i++
            )
            {
                AddMemberValue(
                    keys,
                    target,
                    commonNames[i]
                );
            }


            // ContentID-like and enum-like types often expose the useful
            // identifier only through ToString(). Keep it as an additional
            // candidate; harmless class names will simply never match JSON.
            AddKey(
                keys,
                target.ToString()
            );
        }


        private static void AddMemberValue(
            List<string> keys,
            object target,
            string memberName
        )
        {
            object value =
                GetMemberValue(
                    target,
                    memberName
                );


            if (
                value ==
                null
            )
            {
                return;
            }


            if (
                value is string text
            )
            {
                AddKey(
                    keys,
                    text
                );


                return;
            }


            AddKey(
                keys,
                value.ToString()
            );
        }


        private static object GetMemberValue(
            object target,
            string memberName
        )
        {
            if (
                target ==
                null
            )
            {
                return null;
            }


            const BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;


            Type type =
                target.GetType();


            FieldInfo field =
                type.GetField(
                    memberName,
                    flags
                );


            if (
                field !=
                null
            )
            {
                return
                    field.GetValue(
                        target
                    );
            }


            PropertyInfo property =
                type.GetProperty(
                    memberName,
                    flags
                );


            if (
                property !=
                null
                &&
                property.CanRead
                &&
                property.GetIndexParameters().Length ==
                0
            )
            {
                try
                {
                    return
                        property.GetValue(
                            target,
                            null
                        );
                }
                catch
                {
                    return null;
                }
            }


            return null;
        }


        private static void AddKey(
            List<string> keys,
            string value
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return;
            }


            string trimmed =
                value.Trim();


            AddUnique(
                keys,
                trimmed
            );


            int colon =
                trimmed.LastIndexOf(
                    ':'
                );


            if (
                colon >=
                0
                &&
                colon <
                trimmed.Length -
                1
            )
            {
                AddUnique(
                    keys,
                    trimmed.Substring(
                        colon + 1
                    )
                );
            }
        }


        private static void AddUnique(
            List<string> keys,
            string value
        )
        {
            for (
                int i = 0;
                i < keys.Count;
                i++
            )
            {
                if (
                    string.Equals(
                        keys[i],
                        value,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return;
                }
            }


            keys.Add(
                value
            );
        }


        private static void LogOnce(
            string[] keys
        )
        {
            StringBuilder builder =
                new StringBuilder();


            for (
                int i = 0;
                i < keys.Length;
                i++
            )
            {
                if (
                    i >
                    0
                )
                {
                    builder.Append(
                        ", "
                    );
                }


                builder.Append(
                    keys[i]
                );
            }


            string signature =
                builder.ToString();


            if (
                string.Equals(
                    signature,
                    lastLoggedSignature,
                    StringComparison.Ordinal
                )
            )
            {
                return;
            }


            lastLoggedSignature =
                signature;


            Debug.Log(
                "CAVE BIOME DIMENSION KEYS: [" +
                signature +
                "]"
            );
        }
    }
}

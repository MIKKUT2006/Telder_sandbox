
using System;
using System.Collections.Generic;
using System.Reflection;


namespace Game.World.Biomes.Generation
{
    internal static class BiomeNameUtility
    {
        public static bool Matches(
            BiomeDefinition biome,
            List<string> allowed
        )
        {
            if (
                allowed ==
                null
                ||
                allowed.Count ==
                0
            )
            {
                return true;
            }


            if (
                biome ==
                null
            )
            {
                return false;
            }


            List<string> names =
                GetNames(
                    biome
                );


            for (
                int i = 0;
                i < allowed.Count;
                i++
            )
            {
                string required =
                    allowed[i];


                if (
                    string.IsNullOrWhiteSpace(
                        required
                    )
                )
                {
                    continue;
                }


                if (
                    required ==
                    "*"
                )
                {
                    return true;
                }


                for (
                    int n = 0;
                    n < names.Count;
                    n++
                )
                {
                    if (
                        string.Equals(
                            names[n],
                            required,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return true;
                    }
                }
            }


            return false;
        }


        private static List<string> GetNames(
            BiomeDefinition biome
        )
        {
            List<string> result =
                new List<string>();


            Type type =
                biome.GetType();


            string[] candidates =
            {
                "ID",
                "Id",
                "Name",
                "DisplayName"
            };


            for (
                int i = 0;
                i < candidates.Length;
                i++
            )
            {
                FieldInfo field =
                    type.GetField(
                        candidates[i],
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase
                    );


                if (
                    field !=
                    null
                    &&
                    field.FieldType ==
                        typeof(
                            string
                        )
                )
                {
                    string value =
                        field.GetValue(
                            biome
                        ) as string;


                    if (
                        !string.IsNullOrWhiteSpace(
                            value
                        )
                    )
                    {
                        result.Add(
                            value
                        );
                    }
                }


                PropertyInfo property =
                    type.GetProperty(
                        candidates[i],
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase
                    );


                if (
                    property !=
                    null
                    &&
                    property.PropertyType ==
                        typeof(
                            string
                        )
                    &&
                    property.CanRead
                )
                {
                    string value =
                        property.GetValue(
                            biome,
                            null
                        ) as string;


                    if (
                        !string.IsNullOrWhiteSpace(
                            value
                        )
                    )
                    {
                        result.Add(
                            value
                        );
                    }
                }
            }


            return result;
        }
    }
}

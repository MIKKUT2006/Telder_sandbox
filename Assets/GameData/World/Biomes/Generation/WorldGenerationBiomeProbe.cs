
using System;
using System.Reflection;

using Game.World.Generation;


namespace Game.World.Biomes.Generation
{
    /// <summary>
    /// Small compatibility bridge to the current WorldGenerator.
    ///
    /// We only reflect once per surface X column, not for every block.
    /// This lets cave post-processing preserve ores and replace only
    /// the normal surface-biome stone/background materials.
    /// </summary>
    internal static class WorldGenerationBiomeProbe
    {
        private static FieldInfo biomeRuntimeField;

        private static MethodInfo biomeRuntimeGetMethod;


        public static bool TryGetTerrainIds(
            WorldGenerator generator,
            int worldX,
            out ushort stoneBlockId,
            out ushort backgroundBlockId
        )
        {
            stoneBlockId =
                0;


            backgroundBlockId =
                0;


            if (
                generator ==
                null
            )
            {
                return false;
            }


            try
            {
                if (
                    biomeRuntimeField ==
                    null
                )
                {
                    biomeRuntimeField =
                        typeof(
                            WorldGenerator
                        ).GetField(
                            "biomeRuntime",
                            BindingFlags.NonPublic |
                            BindingFlags.Instance
                        );
                }


                if (
                    biomeRuntimeField ==
                    null
                )
                {
                    return false;
                }


                object runtimeTable =
                    biomeRuntimeField.GetValue(
                        generator
                    );


                if (
                    runtimeTable ==
                    null
                )
                {
                    return false;
                }


                if (
                    biomeRuntimeGetMethod ==
                    null
                    ||
                    biomeRuntimeGetMethod.DeclaringType !=
                        runtimeTable.GetType()
                )
                {
                    biomeRuntimeGetMethod =
                        runtimeTable
                            .GetType()
                            .GetMethod(
                                "Get",
                                BindingFlags.Public |
                                BindingFlags.Instance
                            );
                }


                if (
                    biomeRuntimeGetMethod ==
                    null
                )
                {
                    return false;
                }


                BiomeSample sample =
                    generator.GetBiomeSample(
                        worldX
                    );


                object runtimeBiome =
                    biomeRuntimeGetMethod.Invoke(
                        runtimeTable,
                        new object[]
                        {
                            sample.Dominant
                        }
                    );


                if (
                    runtimeBiome ==
                    null
                )
                {
                    return false;
                }


                stoneBlockId =
                    ReadUShort(
                        runtimeBiome,
                        "StoneBlockID"
                    );


                backgroundBlockId =
                    ReadUShort(
                        runtimeBiome,
                        "BackgroundBlockID"
                    );


                return
                    stoneBlockId !=
                    0
                    ||
                    backgroundBlockId !=
                    0;
            }
            catch
            {
                return false;
            }
        }


        private static ushort ReadUShort(
            object target,
            string name
        )
        {
            Type type =
                target.GetType();


            FieldInfo field =
                type.GetField(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance
                );


            if (
                field !=
                null
            )
            {
                object value =
                    field.GetValue(
                        target
                    );


                if (
                    value is ushort ushortValue
                )
                {
                    return
                        ushortValue;
                }


                if (
                    value is int intValue
                )
                {
                    return
                        (ushort)
                        Math.Max(
                            0,
                            Math.Min(
                                ushort.MaxValue,
                                intValue
                            )
                        );
                }
            }


            PropertyInfo property =
                type.GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance
                );


            if (
                property !=
                null
                &&
                property.CanRead
            )
            {
                object value =
                    property.GetValue(
                        target,
                        null
                    );


                if (
                    value is ushort ushortValue
                )
                {
                    return
                        ushortValue;
                }


                if (
                    value is int intValue
                )
                {
                    return
                        (ushort)
                        Math.Max(
                            0,
                            Math.Min(
                                ushort.MaxValue,
                                intValue
                            )
                        );
                }
            }


            return 0;
        }
    }
}

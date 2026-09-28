using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Game.Combat
{
    public static class ItemSpriteResolver
    {
        private static readonly Dictionary<
            string,
            Sprite
        > Cache =
            new Dictionary<
                string,
                Sprite
            >(
                StringComparer.OrdinalIgnoreCase
            );

        private static MethodInfo resolverMethod;
        private static bool searched;

        public static Sprite Resolve(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(
                    itemId))
            {
                return null;
            }

            if (Cache.TryGetValue(
                    itemId,
                    out Sprite cached))
            {
                return cached;
            }

            EnsureResolver();

            if (resolverMethod == null)
                return null;

            try
            {
                ParameterInfo parameter =
                    resolverMethod
                        .GetParameters()[0];

                object converted =
                    ConvertId(
                        itemId,
                        parameter.ParameterType
                    );

                Sprite sprite =
                    resolverMethod.Invoke(
                        null,
                        new[]
                        {
                            converted
                        }
                    )
                    as Sprite;

                if (sprite != null)
                    Cache[itemId] = sprite;

                return sprite;
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "WEAPON PROJECTILE: sprite resolve failed for " +
                    itemId +
                    ": " +
                    e.Message
                );

                return null;
            }
        }

        public static void ClearCache()
        {
            Cache.Clear();
            resolverMethod = null;
            searched = false;
        }

        private static void EnsureResolver()
        {
            if (searched)
                return;

            searched = true;

            resolverMethod =
                FindStaticSpriteMethod(
                    "ItemIconProvider",
                    "GetIcon"
                );

            if (resolverMethod == null)
            {
                resolverMethod =
                    FindStaticSpriteMethod(
                        "ItemRegistry",
                        "GetIcon"
                    );
            }
        }

        private static MethodInfo FindStaticSpriteMethod(
            string typeName,
            string methodName)
        {
            Assembly[] assemblies =
                AppDomain
                    .CurrentDomain
                    .GetAssemblies();

            for (int i = 0;
                 i < assemblies.Length;
                 i++)
            {
                Type type =
                    null;

                try
                {
                    type =
                        assemblies[i]
                            .GetTypes()
                            .FirstOrDefault(
                                t =>
                                    t.Name ==
                                    typeName
                            );
                }
                catch
                {
                }

                if (type == null)
                    continue;

                BindingFlags flags =
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic;

                MethodInfo method =
                    type
                        .GetMethods(flags)
                        .FirstOrDefault(
                            m =>
                                m.Name ==
                                    methodName &&
                                m.GetParameters()
                                    .Length ==
                                    1 &&
                                typeof(Sprite)
                                    .IsAssignableFrom(
                                        m.ReturnType
                                    )
                        );

                if (method != null)
                    return method;
            }

            return null;
        }

        private static object ConvertId(
            string id,
            Type targetType)
        {
            if (targetType ==
                typeof(string))
            {
                return id;
            }

            MethodInfo parse =
                targetType.GetMethod(
                    "Parse",
                    BindingFlags.Static |
                    BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(string)
                    },
                    null
                );

            if (parse != null)
            {
                return parse.Invoke(
                    null,
                    new object[]
                    {
                        id
                    }
                );
            }

            ConstructorInfo ctor =
                targetType.GetConstructor(
                    new[]
                    {
                        typeof(string)
                    }
                );

            if (ctor != null)
            {
                return ctor.Invoke(
                    new object[]
                    {
                        id
                    }
                );
            }

            return id;
        }
    }
}

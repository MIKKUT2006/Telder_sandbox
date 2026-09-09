using System;
using System.Reflection;
using UnityEngine;

namespace Game.World.Generation
{
    /*
     * The user's CaveGenerator API has changed between versions.
     *
     * This adapter removes a compile-time dependency on a method
     * called exactly "IsCave".
     *
     * Once, in the constructor, it tries to bind a bool method:
     *
     * (int x, int y, int surfaceHeight)
     *
     * Known names:
     * IsCave
     * ShouldCarve
     * IsCaveAt
     * ShouldGenerateCave
     * ShouldCarveCave
     *
     * It also supports:
     *
     * (int x, int y)
     *
     * If no compatible method exists, a deterministic fallback
     * cave field is used instead of breaking world generation.
     */
    public sealed class CaveQueryAdapter
    {
        private readonly CaveGenerator generator;

        private Func<int, int, int, bool>
            query3;

        private Func<int, int, bool>
            query2;

        private readonly int seed;

        private bool warnedFallback;


        private static readonly string[] preferredNames =
        {
            "IsCave",
            "ShouldCarve",
            "IsCaveAt",
            "ShouldGenerateCave",
            "ShouldCarveCave"
        };


        public CaveQueryAdapter(
            CaveGenerator generator,
            int seed
        )
        {
            this.generator = generator;
            this.seed = seed;

            Bind();
        }


        public bool IsCave(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {
            if (query3 != null)
            {
                return
                    query3(
                        worldX,
                        worldY,
                        surfaceHeight
                    );
            }

            if (query2 != null)
            {
                return
                    query2(
                        worldX,
                        worldY
                    );
            }

            if (!warnedFallback)
            {
                warnedFallback = true;

                Debug.LogWarning(
                    "CAVE ADAPTER: no compatible CaveGenerator " +
                    "query method was found. Using deterministic " +
                    "fallback cave noise. This is compile-safe, " +
                    "but connect your current cave query method later."
                );
            }

            return
                FallbackCave(
                    worldX,
                    worldY,
                    surfaceHeight
                );
        }


        private void Bind()
        {
            if (generator == null)
                return;

            Type type =
                generator.GetType();

            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;


            for (
                int n = 0;
                n < preferredNames.Length;
                n++
            )
            {
                string name =
                    preferredNames[n];

                MethodInfo method3 =
                    type.GetMethod(
                        name,
                        flags,
                        null,
                        new Type[]
                        {
                            typeof(int),
                            typeof(int),
                            typeof(int)
                        },
                        null
                    );

                if (
                    method3 != null &&
                    method3.ReturnType ==
                    typeof(bool)
                )
                {
                    try
                    {
                        query3 =
                            (Func<int, int, int, bool>)
                            Delegate.CreateDelegate(
                                typeof(
                                    Func<
                                        int,
                                        int,
                                        int,
                                        bool
                                    >
                                ),
                                generator,
                                method3,
                                false
                            );

                        if (query3 != null)
                        {
                            Debug.Log(
                                "CAVE ADAPTER: bound " +
                                type.Name +
                                "." +
                                method3.Name +
                                "(int,int,int)"
                            );

                            return;
                        }
                    }
                    catch
                    {
                    }
                }


                MethodInfo method2 =
                    type.GetMethod(
                        name,
                        flags,
                        null,
                        new Type[]
                        {
                            typeof(int),
                            typeof(int)
                        },
                        null
                    );

                if (
                    method2 != null &&
                    method2.ReturnType ==
                    typeof(bool)
                )
                {
                    try
                    {
                        query2 =
                            (Func<int, int, bool>)
                            Delegate.CreateDelegate(
                                typeof(
                                    Func<
                                        int,
                                        int,
                                        bool
                                    >
                                ),
                                generator,
                                method2,
                                false
                            );

                        if (query2 != null)
                        {
                            Debug.Log(
                                "CAVE ADAPTER: bound " +
                                type.Name +
                                "." +
                                method2.Name +
                                "(int,int)"
                            );

                            return;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }


        private bool FallbackCave(
            int x,
            int y,
            int surface
        )
        {
            int depth =
                surface -
                y;

            if (depth < 12)
                return false;

            float seedX =
                seed *
                0.17321f;

            float seedY =
                seed *
                0.73129f;

            float main =
                Mathf.PerlinNoise(
                    (
                        x +
                        seedX
                    )
                    *
                    0.025f,

                    (
                        y +
                        seedY
                    )
                    *
                    0.025f
                );

            float detail =
                Mathf.PerlinNoise(
                    (
                        x -
                        seedY
                    )
                    *
                    0.063f,

                    (
                        y +
                        seedX
                    )
                    *
                    0.063f
                );

            float value =
                main +
                (
                    detail -
                    0.5f
                )
                *
                0.22f;

            return
                value >
                0.69f;
        }
    }
}
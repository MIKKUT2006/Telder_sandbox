
using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Game.Content;


namespace Game.World.Biomes.Surface
{
    internal sealed class SurfaceFloraEntryRuntime
    {
        public SurfaceFloraEntry Definition;

        public ushort BlockId;
    }


    internal sealed class SurfaceFloraProfileRuntime
    {
        public SurfaceFloraProfile Definition;

        public List<SurfaceFloraEntryRuntime> Plants =
            new List<SurfaceFloraEntryRuntime>();
    }


    public static class SurfaceFloraRegistry
    {
        private static readonly List<
            SurfaceFloraProfileRuntime
        > profiles =
            new List<
                SurfaceFloraProfileRuntime
            >();


        private static bool loaded;


        public static void Reload()
        {
            loaded =
                false;


            profiles.Clear();


            EnsureLoaded();
        }


        internal static IReadOnlyList<
            SurfaceFloraProfileRuntime
        > GetAll()
        {
            EnsureLoaded();


            return profiles;
        }


        private static void EnsureLoaded()
        {
            if (
                loaded
            )
            {
                return;
            }


            loaded =
                true;


            profiles.Clear();


            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Biomes",
                    "SurfaceFlora"
                );


            if (
                !Directory.Exists(
                    folder
                )
            )
            {
                Directory.CreateDirectory(
                    folder
                );


                return;
            }


            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );


            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {
                try
                {
                    SurfaceFloraProfile definition =
                        JsonUtility.FromJson<
                            SurfaceFloraProfile
                        >(
                            File.ReadAllText(
                                files[i]
                            )
                        );


                    if (
                        definition ==
                        null
                        ||
                        !definition.Enabled
                        ||
                        definition.Plants ==
                        null
                    )
                    {
                        continue;
                    }


                    SurfaceFloraProfileRuntime runtime =
                        new SurfaceFloraProfileRuntime
                        {
                            Definition =
                                definition
                        };


                    for (
                        int p = 0;
                        p < definition.Plants.Count;
                        p++
                    )
                    {
                        SurfaceFloraEntry plant =
                            definition.Plants[p];


                        if (
                            plant ==
                            null
                            ||
                            string.IsNullOrWhiteSpace(
                                plant.BlockId
                            )
                        )
                        {
                            continue;
                        }


                        ushort blockId =
                            ResolveBlockId(
                                plant.BlockId
                            );


                        // Missing optional flora content must never
                        // break world generation.
                        if (
                            blockId ==
                            0
                        )
                        {
                            continue;
                        }


                        runtime.Plants.Add(
                            new SurfaceFloraEntryRuntime
                            {
                                Definition =
                                    plant,

                                BlockId =
                                    blockId
                            }
                        );
                    }


                    if (
                        runtime.Plants.Count >
                        0
                    )
                    {
                        profiles.Add(
                            runtime
                        );
                    }
                }
                catch (
                    Exception exception
                )
                {
                    Debug.LogWarning(
                        "SURFACE FLORA: Failed to load " +
                        files[i] +
                        " | " +
                        exception.Message
                    );
                }
            }
        }


        private static ushort ResolveBlockId(
            string id
        )
        {
            try
            {
                ContentID contentId =
                    ContentID.Parse(
                        id
                    );


                if (
                    !BlockIDRegistry.Contains(
                        contentId
                    )
                )
                {
                    return 0;
                }


                return
                    BlockIDRegistry.GetID(
                        contentId
                    );
            }
            catch
            {
                return 0;
            }
        }
    }
}

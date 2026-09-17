using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.World.Structures
{
    [Serializable]
    public class StructureLayerCellMetadata
    {
        public int X;
        public int Y;

        public string FurnitureId;

        public byte ForegroundTransform;
        public byte BackgroundTransform;
        public byte FurnitureTransform;
    }


    [Serializable]
    public class StructureLayerMetadataFile
    {
        public int Version =
            2;

        public string StructureId;

        public List<StructureLayerCellMetadata> Cells =
            new List<StructureLayerCellMetadata>();
    }


    public sealed class StructureLayerMetadata
    {
        private readonly Dictionary<
            int,
            StructureLayerCellMetadata
        > cells =
            new Dictionary<
                int,
                StructureLayerCellMetadata
            >();


        public StructureLayerCellMetadata Get(
            int x,
            int y
        )
        {
            cells.TryGetValue(
                Key(
                    x,
                    y
                ),
                out StructureLayerCellMetadata metadata
            );


            return metadata;
        }


        public StructureLayerCellMetadata GetOrCreate(
            int x,
            int y
        )
        {
            int key =
                Key(
                    x,
                    y
                );


            if (
                cells.TryGetValue(
                    key,
                    out StructureLayerCellMetadata existing
                )
            )
            {
                return existing;
            }


            StructureLayerCellMetadata metadata =
                new StructureLayerCellMetadata
                {
                    X =
                        x,

                    Y =
                        y
                };


            cells.Add(
                key,
                metadata
            );


            return metadata;
        }


        public void Remove(
            int x,
            int y
        )
        {
            cells.Remove(
                Key(
                    x,
                    y
                )
            );
        }


        public bool IsEmpty
        {
            get
            {
                return
                    cells.Count ==
                    0;
            }
        }


        public List<StructureLayerCellMetadata> Export()
        {
            List<StructureLayerCellMetadata> result =
                new List<StructureLayerCellMetadata>(
                    cells.Count
                );


            foreach (
                KeyValuePair<
                    int,
                    StructureLayerCellMetadata
                > pair
                in cells
            )
            {
                StructureLayerCellMetadata metadata =
                    pair.Value;


                if (metadata == null)
                {
                    continue;
                }


                if (
                    string.IsNullOrWhiteSpace(
                        metadata.FurnitureId
                    )
                    &&
                    metadata.ForegroundTransform ==
                    0
                    &&
                    metadata.BackgroundTransform ==
                    0
                    &&
                    metadata.FurnitureTransform ==
                    0
                )
                {
                    continue;
                }


                result.Add(
                    metadata
                );
            }


            return result;
        }


        public static StructureLayerMetadata From(
            IEnumerable<StructureLayerCellMetadata> source
        )
        {
            StructureLayerMetadata result =
                new StructureLayerMetadata();


            if (source == null)
            {
                return result;
            }


            foreach (
                StructureLayerCellMetadata metadata
                in source
            )
            {
                if (metadata == null)
                {
                    continue;
                }


                result.cells[
                    Key(
                        metadata.X,
                        metadata.Y
                    )
                ] =
                    metadata;
            }


            return result;
        }


        private static int Key(
            int x,
            int y
        )
        {
            unchecked
            {
                return
                    (
                        x &
                        0xFFFF
                    )
                    |
                    (
                        y <<
                        16
                    );
            }
        }
    }


    public static class StructureLayerMetadataStore
    {
        private static readonly object Sync =
            new object();


        private static readonly Dictionary<
            string,
            StructureLayerMetadata
        > Cache =
            new Dictionary<
                string,
                StructureLayerMetadata
            >(
                StringComparer.OrdinalIgnoreCase
            );


        /*
         * IMPORTANT:
         *
         * Never use ".meta" here.
         *
         * Unity owns *.meta files and may rewrite/delete/treat them as
         * asset metadata. That was why transforms survived only while
         * the static in-memory cache was alive.
         *
         * ".layersdata" is our own sidecar extension and does not collide
         * with StructureRegistry *.json scanning either.
         */
        private const string DataExtension =
            ".layersdata";


        private const string LegacyMetaExtension =
            ".layers.meta";


        public static StructureLayerMetadata Load(
            string structureId
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    structureId
                )
            )
            {
                return
                    new StructureLayerMetadata();
            }


            lock (Sync)
            {
                if (
                    Cache.TryGetValue(
                        structureId,
                        out StructureLayerMetadata cached
                    )
                )
                {
                    return cached;
                }


                StructureLayerMetadata loaded =
                    LoadFromDisk(
                        structureId
                    );


                Cache[
                    structureId
                ] =
                    loaded;


                return loaded;
            }
        }


        public static StructureLayerCellMetadata GetCell(
            string structureId,
            int x,
            int y
        )
        {
            return
                Load(
                    structureId
                )
                .Get(
                    x,
                    y
                );
        }


        public static void Save(
            string structureId,
            StructureLayerMetadata metadata
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    structureId
                )
                ||
                metadata ==
                null
            )
            {
                return;
            }


            lock (Sync)
            {
                Directory.CreateDirectory(
                    StructurePaths.Folder
                );


                StructureLayerMetadataFile file =
                    new StructureLayerMetadataFile
                    {
                        Version =
                            2,

                        StructureId =
                            structureId,

                        Cells =
                            metadata.Export()
                    };


                string path =
                    GetDataFile(
                        structureId
                    );


                string temp =
                    path +
                    ".tmp";


                string json =
                    JsonUtility.ToJson(
                        file,
                        true
                    );


                /*
                 * Atomic-ish save:
                 * first write a temp file, then replace the actual sidecar.
                 *
                 * This prevents an interrupted editor save from leaving
                 * an empty/corrupted transform file.
                 */
                File.WriteAllText(
                    temp,
                    json
                );


                if (
                    File.Exists(
                        path
                    )
                )
                {
                    File.Delete(
                        path
                    );
                }


                File.Move(
                    temp,
                    path
                );


                Cache[
                    structureId
                ] =
                    metadata;


                Debug.Log(
                    "STRUCTURE LAYERS SAVE: " +
                    structureId +
                    " -> " +
                    path +
                    " | cells = " +
                    file.Cells.Count
                );
            }
        }


        public static void Invalidate(
            string structureId
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    structureId
                )
            )
            {
                return;
            }


            lock (Sync)
            {
                Cache.Remove(
                    structureId
                );
            }
        }


        public static void InvalidateAll()
        {
            lock (Sync)
            {
                Cache.Clear();
            }
        }


        public static string GetDebugFilePath(
            string structureId
        )
        {
            return
                GetDataFile(
                    structureId
                );
        }


        private static StructureLayerMetadata LoadFromDisk(
            string structureId
        )
        {
            string newPath =
                GetDataFile(
                    structureId
                );


            if (
                File.Exists(
                    newPath
                )
            )
            {
                StructureLayerMetadata loaded =
                    TryRead(
                        newPath,
                        structureId
                    );


                if (loaded != null)
                {
                    return loaded;
                }
            }


            /*
             * Migration from v1.
             *
             * If Unity did not already eat the old *.meta file,
             * read it once and immediately move the data into the
             * safe .layersdata sidecar.
             */
            string legacyPath =
                GetLegacyMetaFile(
                    structureId
                );


            if (
                File.Exists(
                    legacyPath
                )
            )
            {
                StructureLayerMetadata legacy =
                    TryRead(
                        legacyPath,
                        structureId
                    );


                if (legacy != null)
                {
                    Debug.LogWarning(
                        "STRUCTURE LAYERS: migrating legacy Unity-reserved .meta file: " +
                        legacyPath
                    );


                    Save(
                        structureId,
                        legacy
                    );


                    /*
                     * Do NOT delete the legacy file automatically.
                     * Unity may consider it metadata for another asset.
                     * The new .layersdata file is now authoritative.
                     */
                    return legacy;
                }
            }


            Debug.Log(
                "STRUCTURE LAYERS LOAD: no sidecar for " +
                structureId +
                " | expected: " +
                newPath
            );


            return
                new StructureLayerMetadata();
        }


        private static StructureLayerMetadata TryRead(
            string path,
            string structureId
        )
        {
            try
            {
                string json =
                    File.ReadAllText(
                        path
                    );


                if (
                    string.IsNullOrWhiteSpace(
                        json
                    )
                )
                {
                    return null;
                }


                StructureLayerMetadataFile file =
                    JsonUtility.FromJson<
                        StructureLayerMetadataFile
                    >(
                        json
                    );


                if (file == null)
                {
                    return null;
                }


                StructureLayerMetadata result =
                    StructureLayerMetadata.From(
                        file.Cells
                    );


                Debug.Log(
                    "STRUCTURE LAYERS LOAD: " +
                    structureId +
                    " <- " +
                    path +
                    " | cells = " +
                    (
                        file.Cells !=
                        null
                            ? file.Cells.Count
                            : 0
                    )
                );


                return result;
            }
            catch (
                Exception exception
            )
            {
                Debug.LogError(
                    "STRUCTURE LAYERS LOAD FAILED: " +
                    structureId +
                    "\nPath: " +
                    path +
                    "\n" +
                    exception
                );


                return null;
            }
        }


        private static string GetDataFile(
            string structureId
        )
        {
            return
                Path.Combine(
                    StructurePaths.Folder,
                    SafeId(
                        structureId
                    )
                    +
                    DataExtension
                );
        }


        private static string GetLegacyMetaFile(
            string structureId
        )
        {
            return
                Path.Combine(
                    StructurePaths.Folder,
                    SafeId(
                        structureId
                    )
                    +
                    LegacyMetaExtension
                );
        }


        private static string SafeId(
            string structureId
        )
        {
            return
                structureId
                    .Replace(
                        ":",
                        "_"
                    )
                    .Replace(
                        "/",
                        "_"
                    )
                    .Replace(
                        "\\",
                        "_"
                    );
        }
    }
}

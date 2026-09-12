
using System;
using System.Collections.Generic;


namespace Game.World.Structures
{
    /// <summary>
    /// Session-only debug registry.
    ///
    /// An entry appears here when StructureGenerationRuntime reaches the
    /// actual StampCandidateIntoChunk stage for a valid candidate.
    ///
    /// It is intentionally NOT save data.
    /// </summary>
    public static class StructureDebugRuntimeRegistry
    {
        public sealed class Entry
        {
            public int WorldSeed;

            public string DimensionName;

            public string StructureId;

            public string DisplayName;

            public int RegionX;

            public int AnchorX;

            public int AnchorY;
        }


        private static readonly Dictionary<
            string,
            Entry
        > entries =
            new Dictionary<
                string,
                Entry
            >(
                StringComparer.OrdinalIgnoreCase
            );


        public static void Register(
            int worldSeed,
            string dimensionName,
            StructureDefinition structure,
            int regionX,
            int anchorX,
            int anchorY
        )
        {
            if (
                structure ==
                null
                ||
                string.IsNullOrWhiteSpace(
                    structure.ID
                )
            )
            {
                return;
            }


            string key =
                worldSeed +
                "|" +
                (
                    dimensionName ??
                    string.Empty
                ) +
                "|" +
                structure.ID +
                "|" +
                regionX +
                "|" +
                anchorX +
                "|" +
                anchorY;


            if (
                entries.ContainsKey(
                    key
                )
            )
            {
                return;
            }


            entries.Add(
                key,

                new Entry
                {
                    WorldSeed =
                        worldSeed,

                    DimensionName =
                        dimensionName ??
                        string.Empty,

                    StructureId =
                        structure.ID,

                    DisplayName =
                        string.IsNullOrWhiteSpace(
                            structure.DisplayName
                        )
                            ? structure.ID
                            : structure.DisplayName,

                    RegionX =
                        regionX,

                    AnchorX =
                        anchorX,

                    AnchorY =
                        anchorY
                }
            );
        }


        public static List<Entry> GetNearby(
            string dimensionName,
            int worldX,
            int worldY,
            string search,
            int maxResults
        )
        {
            string dimension =
                dimensionName ??
                string.Empty;


            string filter =
                (
                    search ??
                    string.Empty
                ).Trim();


            List<Entry> result =
                new List<Entry>();


            foreach (
                KeyValuePair<
                    string,
                    Entry
                > pair
                in entries
            )
            {
                Entry entry =
                    pair.Value;


                if (
                    entry ==
                    null
                )
                {
                    continue;
                }


                if (
                    !string.Equals(
                        entry.DimensionName,
                        dimension,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }


                if (
                    !MatchesSearch(
                        entry,
                        filter
                    )
                )
                {
                    continue;
                }


                result.Add(
                    entry
                );
            }


            result.Sort(
                (
                    a,
                    b
                ) =>
                {
                    long aDistance =
                        DistanceSquared(
                            a,
                            worldX,
                            worldY
                        );


                    long bDistance =
                        DistanceSquared(
                            b,
                            worldX,
                            worldY
                        );


                    return
                        aDistance.CompareTo(
                            bDistance
                        );
                }
            );


            int limit =
                Math.Max(
                    1,
                    maxResults
                );


            if (
                result.Count >
                limit
            )
            {
                result.RemoveRange(
                    limit,
                    result.Count -
                    limit
                );
            }


            return result;
        }


        public static int CountForDimension(
            string dimensionName
        )
        {
            string dimension =
                dimensionName ??
                string.Empty;


            int count =
                0;


            foreach (
                Entry entry
                in entries.Values
            )
            {
                if (
                    entry !=
                    null
                    &&
                    string.Equals(
                        entry.DimensionName,
                        dimension,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    count++;
                }
            }


            return count;
        }


        private static bool MatchesSearch(
            Entry entry,
            string filter
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    filter
                )
            )
            {
                return true;
            }


            return
                ContainsIgnoreCase(
                    entry.StructureId,
                    filter
                )
                ||
                ContainsIgnoreCase(
                    entry.DisplayName,
                    filter
                );
        }


        private static bool ContainsIgnoreCase(
            string value,
            string search
        )
        {
            return
                !string.IsNullOrWhiteSpace(
                    value
                )
                &&
                value.IndexOf(
                    search,
                    StringComparison.OrdinalIgnoreCase
                )
                >=
                0;
        }


        private static long DistanceSquared(
            Entry entry,
            int worldX,
            int worldY
        )
        {
            long dx =
                (long)
                entry.AnchorX -
                worldX;


            long dy =
                (long)
                entry.AnchorY -
                worldY;


            return
                dx *
                dx +
                dy *
                dy;
        }
    }
}

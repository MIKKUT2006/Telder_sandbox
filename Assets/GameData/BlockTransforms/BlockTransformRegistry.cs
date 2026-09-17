using System;
using System.Collections.Generic;

namespace Game.BlockTransforms
{
    [Serializable]
    public class BlockTransformSaveEntry
    {
        public int X;
        public int Y;
        public byte Value;
    }


    [Serializable]
    public class BlockTransformSaveFile
    {
        public int Version =
            1;


        public List<BlockTransformSaveEntry> Entries =
            new List<BlockTransformSaveEntry>();
    }


    public static class BlockTransformRegistry
    {
        private static readonly object Sync =
            new object();


        private static readonly Dictionary<long, byte> Data =
            new Dictionary<long, byte>();


        private static long Key(
            int x,
            int y
        )
        {
            unchecked
            {
                return
                    (
                        (long)x <<
                        32
                    )
                    ^
                    (uint)y;
            }
        }


        private static void Decode(
            long key,
            out int x,
            out int y
        )
        {
            x =
                (int)(
                    key >>
                    32
                );


            y =
                unchecked(
                    (int)(uint)key
                );
        }


        public static BlockTransformState Get(
            int x,
            int y
        )
        {
            lock (Sync)
            {
                return
                    Data.TryGetValue(
                        Key(
                            x,
                            y
                        ),
                        out byte value
                    )
                        ? new BlockTransformState(
                            value
                        )
                        : BlockTransformState.Identity;
            }
        }


        public static byte GetRaw(
            int x,
            int y
        )
        {
            lock (Sync)
            {
                return
                    Data.TryGetValue(
                        Key(
                            x,
                            y
                        ),
                        out byte value
                    )
                        ? value
                        : (byte)0;
            }
        }


        public static bool Contains(
            int x,
            int y
        )
        {
            lock (Sync)
            {
                return
                    Data.ContainsKey(
                        Key(
                            x,
                            y
                        )
                    );
            }
        }


        public static void Set(
            int x,
            int y,
            BlockTransformState state
        )
        {
            SetRaw(
                x,
                y,
                state.Value,
                true
            );
        }


        public static void SetRaw(
            int x,
            int y,
            byte value,
            bool markDirty = true
        )
        {
            bool changed =
                false;


            lock (Sync)
            {
                long key =
                    Key(
                        x,
                        y
                    );


                if (value == 0)
                {
                    changed =
                        Data.Remove(
                            key
                        );
                }
                else
                {
                    if (
                        !Data.TryGetValue(
                            key,
                            out byte old
                        )
                        ||
                        old != value
                    )
                    {
                        Data[
                            key
                        ] =
                            value;


                        changed =
                            true;
                    }
                }
            }


            if (
                changed
                &&
                markDirty
            )
            {
                BlockTransformPersistence.MarkDirty();
            }
        }


        public static void Rotate(
            int x,
            int y
        )
        {
            BlockTransformState state =
                Get(
                    x,
                    y
                );


            state.RotateClockwise();


            Set(
                x,
                y,
                state
            );
        }


        public static void ToggleMirror(
            int x,
            int y
        )
        {
            BlockTransformState state =
                Get(
                    x,
                    y
                );


            state.ToggleMirror();


            Set(
                x,
                y,
                state
            );
        }


        public static void Clear(
            int x,
            int y
        )
        {
            bool removed;


            lock (Sync)
            {
                removed =
                    Data.Remove(
                        Key(
                            x,
                            y
                        )
                    );
            }


            if (removed)
            {
                BlockTransformPersistence.MarkDirty();
            }
        }


        public static void ClearAll(
            bool markDirty = false
        )
        {
            lock (Sync)
            {
                Data.Clear();
            }


            if (markDirty)
            {
                BlockTransformPersistence.MarkDirty();
            }
        }


        public static List<BlockTransformSaveEntry> Export()
        {
            lock (Sync)
            {
                List<BlockTransformSaveEntry> result =
                    new List<BlockTransformSaveEntry>(
                        Data.Count
                    );


                foreach (
                    KeyValuePair<long, byte> pair
                    in Data
                )
                {
                    if (
                        pair.Value ==
                        0
                    )
                    {
                        continue;
                    }


                    Decode(
                        pair.Key,
                        out int x,
                        out int y
                    );


                    result.Add(
                        new BlockTransformSaveEntry
                        {
                            X =
                                x,

                            Y =
                                y,

                            Value =
                                pair.Value
                        }
                    );
                }


                return result;
            }
        }


        public static void Import(
            IEnumerable<BlockTransformSaveEntry> entries
        )
        {
            lock (Sync)
            {
                Data.Clear();


                if (entries == null)
                {
                    return;
                }


                foreach (
                    BlockTransformSaveEntry entry
                    in entries
                )
                {
                    if (
                        entry ==
                        null
                        ||
                        entry.Value ==
                        0
                    )
                    {
                        continue;
                    }


                    Data[
                        Key(
                            entry.X,
                            entry.Y
                        )
                    ] =
                        entry.Value;
                }
            }
        }
    }
}

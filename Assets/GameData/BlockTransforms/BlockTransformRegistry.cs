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
        public int Version = 1;
        public List<BlockTransformSaveEntry> Entries =
            new List<BlockTransformSaveEntry>();
    }

    public static class BlockTransformRegistry
    {
        private static readonly Dictionary<long, byte> Data =
            new Dictionary<long, byte>();

        private static long Key(int x, int y)
        {
            unchecked
            {
                return ((long)x << 32) ^ (uint)y;
            }
        }

        private static void Decode(long key, out int x, out int y)
        {
            x = (int)(key >> 32);
            y = unchecked((int)(uint)key);
        }

        public static BlockTransformState Get(int x, int y)
        {
            byte value;
            return Data.TryGetValue(Key(x, y), out value)
                ? new BlockTransformState(value)
                : BlockTransformState.Identity;
        }

        public static void Set(int x, int y, BlockTransformState state)
        {
            long key = Key(x, y);

            if (state.Value == 0)
            {
                if (Data.Remove(key))
                    BlockTransformPersistence.MarkDirty();
                return;
            }

            byte old;
            if (Data.TryGetValue(key, out old) && old == state.Value)
                return;

            Data[key] = state.Value;
            BlockTransformPersistence.MarkDirty();
        }

        public static void Rotate(int x, int y)
        {
            BlockTransformState state = Get(x, y);
            state.RotateClockwise();
            Set(x, y, state);
        }

        public static void ToggleMirror(int x, int y)
        {
            BlockTransformState state = Get(x, y);
            state.ToggleMirror();
            Set(x, y, state);
        }

        public static void Clear(int x, int y)
        {
            if (Data.Remove(Key(x, y)))
                BlockTransformPersistence.MarkDirty();
        }

        public static void ClearAll(bool markDirty = false)
        {
            Data.Clear();
            if (markDirty)
                BlockTransformPersistence.MarkDirty();
        }

        public static List<BlockTransformSaveEntry> Export()
        {
            List<BlockTransformSaveEntry> result =
                new List<BlockTransformSaveEntry>(Data.Count);

            foreach (KeyValuePair<long, byte> pair in Data)
            {
                if (pair.Value == 0)
                    continue;

                int x;
                int y;
                Decode(pair.Key, out x, out y);

                result.Add(new BlockTransformSaveEntry
                {
                    X = x,
                    Y = y,
                    Value = pair.Value
                });
            }

            return result;
        }

        public static void Import(IEnumerable<BlockTransformSaveEntry> entries)
        {
            Data.Clear();

            if (entries == null)
                return;

            foreach (BlockTransformSaveEntry entry in entries)
            {
                if (entry == null || entry.Value == 0)
                    continue;

                Data[Key(entry.X, entry.Y)] = entry.Value;
            }
        }
    }
}

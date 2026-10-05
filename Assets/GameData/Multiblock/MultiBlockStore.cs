using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.GameplaySystems;
using Game.World.Furniture;

namespace Game.GameplaySystems.Multiblock
{
    [Serializable]
    public sealed class MultiBlockRecord
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int AnchorX;
        public int AnchorY;
        public string BlockId;
        public string Kind;
    }

    [Serializable]
    internal sealed class MultiBlockSaveFile
    {
        public List<MultiBlockRecord> Entries = new List<MultiBlockRecord>();
    }

    internal static class MultiBlockStore
    {
        private static string loadedKey;
        private static MultiBlockSaveFile data = new MultiBlockSaveFile();
        private static readonly Dictionary<long, MultiBlockRecord> cells = new Dictionary<long, MultiBlockRecord>();

        public static IEnumerable<MultiBlockRecord> Entries
        {
            get { Ensure(); return data.Entries; }
        }

        public static MultiBlockRecord FindCell(int x, int y)
        {
            Ensure();
            cells.TryGetValue(Pack(x, y), out MultiBlockRecord record);

            if (record != null && !IsAlive(record))
            {
                RemoveInternal(record, true);
                return null;
            }

            return record;
        }

        public static MultiBlockRecord FindAnchor(int x, int y)
        {
            Ensure();
            for (int i = data.Entries.Count - 1; i >= 0; i--)
            {
                MultiBlockRecord r = data.Entries[i];
                if (r == null || r.X != x || r.Y != y)
                    continue;

                if (!IsAlive(r))
                {
                    RemoveInternal(r, true);
                    return null;
                }

                return r;
            }
            return null;
        }

        public static bool Add(MultiBlockRecord record)
        {
            Ensure();
            if (record == null || string.IsNullOrWhiteSpace(record.BlockId))
                return false;

            Normalize(record);
            int minX = record.X - record.AnchorX;
            int minY = record.Y - record.AnchorY;

            for (int ix = 0; ix < record.Width; ix++)
            for (int iy = 0; iy < record.Height; iy++)
            {
                if (cells.ContainsKey(Pack(minX + ix, minY + iy)))
                    return false;
            }

            data.Entries.Add(record);
            Index(record);
            Save();
            return true;
        }

        public static bool Remove(MultiBlockRecord record)
        {
            Ensure();
            return RemoveInternal(record, true);
        }

        public static bool RemoveAnchorAt(int x, int y, string blockId = null)
        {
            Ensure();

            for (int i = data.Entries.Count - 1; i >= 0; i--)
            {
                MultiBlockRecord record = data.Entries[i];
                if (record == null || record.X != x || record.Y != y)
                    continue;

                if (
                    !string.IsNullOrWhiteSpace(blockId) &&
                    !string.Equals(record.BlockId, blockId, StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                return RemoveInternal(record, true);
            }

            return false;
        }

        public static int PruneAgainstFurniture(FurnitureLayerManager furniture)
        {
            Ensure();

            if (furniture == null)
                return 0;

            int removed = 0;
            for (int i = data.Entries.Count - 1; i >= 0; i--)
            {
                MultiBlockRecord record = data.Entries[i];
                if (record == null)
                {
                    data.Entries.RemoveAt(i);
                    removed++;
                    continue;
                }

                string actual = furniture.GetFurniture(record.X, record.Y);
                if (
                    string.IsNullOrWhiteSpace(actual) ||
                    !string.Equals(actual, record.BlockId, StringComparison.OrdinalIgnoreCase)
                )
                {
                    data.Entries.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
            {
                RebuildIndex();
                Save();
            }

            return removed;
        }

        private static bool RemoveInternal(MultiBlockRecord record, bool save)
        {
            if (record == null || !data.Entries.Remove(record))
                return false;

            RebuildIndex();
            if (save)
                Save();

            return true;
        }

        private static bool IsAlive(MultiBlockRecord record)
        {
            if (record == null)
                return false;

            FurnitureLayerManager furniture = FurnitureLayerManager.Instance;

            // During very early startup the furniture save may not be loaded yet.
            // In that short window we cannot validate the record, so keep it.
            if (furniture == null)
                return true;

            string actual = furniture.GetFurniture(record.X, record.Y);
            return
                !string.IsNullOrWhiteSpace(actual) &&
                string.Equals(actual, record.BlockId, StringComparison.OrdinalIgnoreCase);
        }

        private static void Ensure()
        {
            string key = GameplayContextKey.Get();
            if (key == loadedKey)
                return;

            loadedKey = key;
            data = GameplaySaveIO.Load<MultiBlockSaveFile>(PathFor(key));
            if (data.Entries == null)
                data.Entries = new List<MultiBlockRecord>();

            // Repair invalid/stale serialized geometry instead of letting it poison lookup.
            for (int i = data.Entries.Count - 1; i >= 0; i--)
            {
                MultiBlockRecord r = data.Entries[i];
                if (r == null || string.IsNullOrWhiteSpace(r.BlockId))
                {
                    data.Entries.RemoveAt(i);
                    continue;
                }
                Normalize(r);
            }

            RebuildIndex();
        }

        private static void Normalize(MultiBlockRecord r)
        {
            r.Width = Mathf.Max(1, r.Width);
            r.Height = Mathf.Max(1, r.Height);
            r.AnchorX = Mathf.Clamp(r.AnchorX, 0, r.Width - 1);
            r.AnchorY = Mathf.Clamp(r.AnchorY, 0, r.Height - 1);
        }

        private static void RebuildIndex()
        {
            cells.Clear();
            for (int i = 0; i < data.Entries.Count; i++)
                Index(data.Entries[i]);
        }

        private static void Index(MultiBlockRecord r)
        {
            if (r == null)
                return;

            int minX = r.X - r.AnchorX;
            int minY = r.Y - r.AnchorY;
            for (int ix = 0; ix < r.Width; ix++)
            for (int iy = 0; iy < r.Height; iy++)
                cells[Pack(minX + ix, minY + iy)] = r;
        }

        private static void Save()
        {
            if (string.IsNullOrWhiteSpace(loadedKey))
                return;
            GameplaySaveIO.Save(PathFor(loadedKey), data, true);
        }

        private static string Folder()
        {
            return Path.Combine(Application.persistentDataPath, "TelderGameplaySystems");
        }

        private static string PathFor(string key)
        {
            return Path.Combine(Folder(), "multiblocks_" + key + ".json");
        }

        private static long Pack(int x, int y)
        {
            unchecked { return ((long)x << 32) ^ (uint)y; }
        }
    }
}

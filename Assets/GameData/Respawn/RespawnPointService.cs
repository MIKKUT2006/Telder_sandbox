using System;
using System.IO;
using UnityEngine;
using Game.GameplaySystems;
using Game.GameplaySystems.Multiblock;

namespace Game.GameplaySystems.Respawn
{
    [Serializable]
    internal sealed class SpawnData
    {
        public int Version;
        public bool HasSpawn;
        public float X;
        public float Y;

        // Bed anchor that created this spawn point. New saves use these fields
        // so a destroyed bed can invalidate its spawn safely.
        public int BedAnchorX;
        public int BedAnchorY;
        public string BedBlockId;
    }

    public static class RespawnPointService
    {
        public static void SetSpawn(MultiBlockRecord record)
        {
            if (record == null)
                return;

            int width = Mathf.Max(1, record.Width);
            int height = Mathf.Max(1, record.Height);
            int minX = record.X - Mathf.Clamp(record.AnchorX, 0, width - 1);
            int minY = record.Y - Mathf.Clamp(record.AnchorY, 0, height - 1);

            SpawnData data = new SpawnData
            {
                Version = 2,
                HasSpawn = true,
                X = minX + width * 0.5f,
                Y = minY + height,
                BedAnchorX = record.X,
                BedAnchorY = record.Y,
                BedBlockId = record.BlockId
            };

            if (GameplaySaveIO.Save(PathNow(), data, true))
                DeathScreenUI.ShowToast("Точка возрождения установлена");
        }

        public static bool TryGet(out Vector2 point)
        {
            point = Vector2.zero;
            string path = PathNow();
            if (!File.Exists(path))
                return false;

            SpawnData data = GameplaySaveIO.Load<SpawnData>(path);
            if (data == null || !data.HasSpawn)
                return false;

            if (data.Version < 2 || string.IsNullOrWhiteSpace(data.BedBlockId))
            {
                // Migrate old spawn files by matching their stored feet point to a
                // currently existing bed. If no bed matches, the old point was
                // stale and is discarded.
                MultiBlockRecord legacyBed = FindBedForPoint(data.X, data.Y);
                if (legacyBed == null)
                {
                    Clear();
                    return false;
                }

                data.Version = 2;
                data.BedAnchorX = legacyBed.X;
                data.BedAnchorY = legacyBed.Y;
                data.BedBlockId = legacyBed.BlockId;
                GameplaySaveIO.Save(path, data, true);
            }

            // Version 2 spawn points know exactly which bed created them.
            // If that bed is gone, do not respawn in empty air at its old location.
            MultiBlockRecord record =
                MultiBlockStore.FindAnchor(
                    data.BedAnchorX,
                    data.BedAnchorY
                );

            if (
                record == null ||
                !string.Equals(
                    record.BlockId,
                    data.BedBlockId,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                Clear();
                return false;
            }

            point = new Vector2(data.X, data.Y);
            return true;
        }

        private static MultiBlockRecord FindBedForPoint(float x, float y)
        {
            foreach (MultiBlockRecord record in MultiBlockStore.Entries)
            {
                if (record == null)
                    continue;

                bool isBed =
                    string.Equals(record.Kind, "bed", StringComparison.OrdinalIgnoreCase) ||
                    MultiBlockMetadataRegistry.HasTag(record.BlockId, "bed");

                if (!isBed)
                    continue;

                int width = Mathf.Max(1, record.Width);
                int height = Mathf.Max(1, record.Height);
                int minX = record.X - Mathf.Clamp(record.AnchorX, 0, width - 1);
                int minY = record.Y - Mathf.Clamp(record.AnchorY, 0, height - 1);

                float expectedX = minX + width * 0.5f;
                float expectedY = minY + height;

                if (
                    Mathf.Abs(expectedX - x) <= 0.01f &&
                    Mathf.Abs(expectedY - y) <= 0.01f
                )
                {
                    return record;
                }
            }

            return null;
        }


        public static void InvalidateIfBedAnchor(
            int anchorX,
            int anchorY,
            string blockId)
        {
            string path = PathNow();
            if (!File.Exists(path))
                return;

            SpawnData data = GameplaySaveIO.Load<SpawnData>(path);
            if (
                data == null ||
                !data.HasSpawn ||
                data.Version < 2 ||
                string.IsNullOrWhiteSpace(data.BedBlockId)
            )
            {
                return;
            }

            if (
                data.BedAnchorX == anchorX &&
                data.BedAnchorY == anchorY &&
                (
                    string.IsNullOrWhiteSpace(blockId) ||
                    string.Equals(data.BedBlockId, blockId, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                Clear();
            }
        }

        public static void Clear()
        {
            string path = PathNow();
            GameplaySaveIO.Save(
                path,
                new SpawnData
                {
                    Version = 2,
                    HasSpawn = false
                },
                true
            );
        }

        private static string Folder()
        {
            return Path.Combine(Application.persistentDataPath, "TelderGameplaySystems");
        }

        private static string PathNow()
        {
            return Path.Combine(Folder(), "spawn_" + GameplayContextKey.Get() + ".json");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game.Content;

namespace Game.World.PolishFixes
{
    public static class SandPlacementFallRuntime
    {
        private static Runner runner;

        private static readonly HashSet<long> active =
            new HashSet<long>();

        public static void NotifyPlaced(
            WorldManager worldManager,
            World world,
            int worldX,
            int worldY,
            ushort blockId
        )
        {
            if (worldManager == null ||
                world == null ||
                blockId == 0 ||
                !IsSand(blockId))
            {
                return;
            }

            if (world.GetBlock(worldX, worldY - 1) != 0)
                return;

            EnsureRunner();

            if (runner == null)
                return;

            long key = Pack(worldX, worldY);

            if (!active.Add(key))
                return;

            runner.StartCoroutine(
                Fall(
                    worldManager,
                    world,
                    worldX,
                    worldY,
                    blockId,
                    key
                )
            );
        }

        private static IEnumerator Fall(
            WorldManager worldManager,
            World world,
            int startX,
            int startY,
            ushort blockId,
            long activeKey
        )
        {
            int x = startX;
            int y = startY;

            // Small delay allows any existing falling-block system
            // to take ownership first. If it already moved the block,
            // this fallback exits without fighting it.
            yield return null;

            while (worldManager != null &&
                   world != null)
            {
                if (world.GetBlock(x, y) != blockId)
                    break;

                int belowY = y - 1;

                if (!world.IsLoaded(x, belowY))
                    break;

                if (world.GetBlock(x, belowY) != 0)
                    break;

                if (!worldManager.SetBlock(x, y, 0))
                    break;

                if (!worldManager.SetBlock(x, belowY, blockId))
                {
                    // Best effort rollback.
                    worldManager.SetBlock(x, y, blockId);
                    break;
                }

                y = belowY;

                yield return new WaitForSeconds(0.035f);
            }

            active.Remove(activeKey);
        }

        private static bool IsSand(ushort blockId)
        {
            try
            {
                string id =
                    BlockIDRegistry
                        .GetContentID(blockId)
                        .ToString();

                if (string.IsNullOrWhiteSpace(id))
                    return false;

                id = id.ToLowerInvariant();

                return
                    id == "game:sand" ||
                    id.EndsWith(":sand") ||
                    id.Contains("sand_") ||
                    id.Contains("_sand");
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureRunner()
        {
            if (runner != null)
                return;

            GameObject obj =
                new GameObject(
                    "[Runtime] Sand Placement Fall"
                );

            Object.DontDestroyOnLoad(obj);

            runner =
                obj.AddComponent<Runner>();
        }

        private static long Pack(int x, int y)
        {
            unchecked
            {
                return ((long)x << 32) ^ (uint)y;
            }
        }

        private sealed class Runner :
            MonoBehaviour
        {
        }
    }
}

using System;
using UnityEngine;

using Game.Blocks;
using Game.Content;
using Game.World.Effects;
using Game.World.Furniture;
using Game.World.Items;
using Game.World.Physics;

namespace Game.World.Explosions
{
    /// <summary>
    /// Grid-aware block explosion system.
    ///
    /// Foreground blocks inside the radius are removed, each block produces
    /// its normal Drop/DropCount item, block debris is emitted and falling
    /// blocks above the crater are woken up.
    /// </summary>
    public static class ExplosionSystem
    {
        public static int Explode(
            Vector2 center,
            float radius,
            bool dropBlocks = true,
            bool destroyBackground = false,
            bool destroyFurniture = true)
        {
            Game.World.WorldManager manager =
                Game.World.WorldManager.Instance;

            if (manager == null)
            {
                Debug.LogWarning(
                    "EXPLOSION: WorldManager.Instance is null."
                );
                return 0;
            }

            Game.World.World world =
                manager.GetWorld();

            if (world == null)
            {
                Debug.LogWarning(
                    "EXPLOSION: World is null."
                );
                return 0;
            }

            radius =
                Mathf.Max(0.25f, radius);

            float radiusSqr =
                radius * radius;

            int minX =
                Mathf.FloorToInt(center.x - radius);

            int maxX =
                Mathf.CeilToInt(center.x + radius);

            int minY =
                Mathf.FloorToInt(center.y - radius);

            int maxY =
                Mathf.CeilToInt(center.y + radius);

            int destroyed = 0;

            // Visual burst is spawned once for the blast itself.
            ExplosionPixelVfx.Spawn(
                center,
                radius
            );

            FurnitureLayerManager furniture =
                destroyFurniture
                    ? FurnitureLayerManager.Instance
                    : null;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 cellCenter =
                        new Vector2(
                            x + 0.5f,
                            y + 0.5f
                        );

                    if ((cellCenter - center).sqrMagnitude >
                        radiusSqr)
                    {
                        continue;
                    }

                    ushort foreground =
                        world.GetBlock(x, y);

                    if (foreground != 0)
                    {
                        bool changed =
                            manager.SetBlock(
                                x,
                                y,
                                0
                            );

                        if (changed)
                        {
                            destroyed++;

                            BlockBreakDebrisSystem.Emit(
                                world,
                                x,
                                y,
                                foreground,
                                false
                            );

                            if (dropBlocks)
                            {
                                SpawnBlockDrop(
                                    foreground,
                                    x,
                                    y
                                );
                            }

                            FallingBlockSystem.NotifyCellChanged(
                                x,
                                y
                            );
                        }
                    }

                    if (destroyBackground)
                    {
                        ushort background =
                            world.GetBackground(x, y);

                        if (background != 0)
                        {
                            bool changed =
                                manager.SetBackground(
                                    x,
                                    y,
                                    0
                                );

                            if (changed)
                            {
                                destroyed++;

                                BlockBreakDebrisSystem.Emit(
                                    world,
                                    x,
                                    y,
                                    background,
                                    true
                                );

                                if (dropBlocks)
                                {
                                    SpawnBlockDrop(
                                        background,
                                        x,
                                        y
                                    );
                                }
                            }
                        }
                    }

                    if (furniture != null &&
                        furniture.HasFurniture(x, y))
                    {
                        // BreakFurniture already uses the configured block drop.
                        if (furniture.BreakFurniture(x, y))
                            destroyed++;
                    }
                }
            }

            GlobalParticleFrontEnforcer.ApplyNow();

            return destroyed;
        }

        /// <summary>
        /// Exact cell-box explosion used by bombs. For a 2x2 bomb this destroys
        /// exactly the two nearest columns by two nearest rows around the center.
        /// </summary>
        public static int ExplodeBox(
            Vector2 center,
            int widthCells,
            int heightCells,
            bool dropBlocks = true,
            bool destroyBackground = false,
            bool destroyFurniture = true)
        {
            Game.World.WorldManager manager =
                Game.World.WorldManager.Instance;

            if (manager == null)
                return 0;

            Game.World.World world = manager.GetWorld();
            if (world == null)
                return 0;

            widthCells = Mathf.Max(1, widthCells);
            heightCells = Mathf.Max(1, heightCells);

            int startX = Mathf.FloorToInt(center.x - (widthCells - 1) * 0.5f);
            int startY = Mathf.FloorToInt(center.y - (heightCells - 1) * 0.5f);
            int destroyed = 0;

            ExplosionPixelVfx.Spawn(
                center,
                Mathf.Max(widthCells, heightCells) * 0.55f
            );

            FurnitureLayerManager furniture =
                destroyFurniture
                    ? FurnitureLayerManager.Instance
                    : null;

            for (int oy = 0; oy < heightCells; oy++)
            {
                for (int ox = 0; ox < widthCells; ox++)
                {
                    int x = startX + ox;
                    int y = startY + oy;

                    ushort foreground = world.GetBlock(x, y);
                    if (foreground != 0 && manager.SetBlock(x, y, 0))
                    {
                        destroyed++;
                        BlockBreakDebrisSystem.Emit(world, x, y, foreground, false);

                        if (dropBlocks)
                            SpawnBlockDrop(foreground, x, y);

                        FallingBlockSystem.NotifyCellChanged(x, y);
                    }

                    if (destroyBackground)
                    {
                        ushort background = world.GetBackground(x, y);
                        if (background != 0 && manager.SetBackground(x, y, 0))
                        {
                            destroyed++;
                            BlockBreakDebrisSystem.Emit(world, x, y, background, true);

                            if (dropBlocks)
                                SpawnBlockDrop(background, x, y);
                        }
                    }

                    if (furniture != null && furniture.HasFurniture(x, y))
                    {
                        if (furniture.BreakFurniture(x, y))
                            destroyed++;
                    }
                }
            }

            GlobalParticleFrontEnforcer.ApplyNow();
            return destroyed;
        }

        public static int Explode(
            int worldX,
            int worldY,
            float radius)
        {
            return Explode(
                new Vector2(
                    worldX + 0.5f,
                    worldY + 0.5f
                ),
                radius,
                true,
                false,
                true
            );
        }

        private static void SpawnBlockDrop(
            ushort blockId,
            int worldX,
            int worldY)
        {
            if (blockId == 0 ||
                ItemDropSpawner.Instance == null)
            {
                return;
            }

            try
            {
                ContentID contentId =
                    BlockIDRegistry.GetContentID(blockId);

                if (!BlockRegistry.Contains(contentId))
                    return;

                BlockDefinition block =
                    BlockRegistry.Get(contentId);

                if (block == null)
                    return;

                int count =
                    Mathf.Max(0, block.DropCount);

                if (count <= 0)
                    return;

                string dropId = null;

                try
                {
                    dropId = block.Drop.ToString();
                }
                catch
                {
                }

                if (string.IsNullOrWhiteSpace(dropId))
                    dropId = contentId.ToString();

                ItemDropSpawner.Instance.SpawnFromBlock(
                    dropId,
                    count,
                    new Vector2(
                        worldX + 0.5f,
                        worldY + 0.55f
                    )
                );
            }
            catch (Exception)
            {
                // One invalid block definition must not abort the full blast.
            }
        }
    }
}

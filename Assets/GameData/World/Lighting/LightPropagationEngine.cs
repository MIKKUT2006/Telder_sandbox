using Game.Blocks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Lighting
{
    public class LightPropagationEngine
    {
        public const byte MaxLight = 15;

        private const int LightSpreadRadius = MaxLight + 2;

        private readonly ILightWorld world;

        private readonly Queue<LightPoint> propagationQueue =
            new Queue<LightPoint>(4096);

        private static readonly int[] DirectionX =
        {
            -1,
            1,
            0,
            0
        };

        private static readonly int[] DirectionY =
        {
            0,
            0,
            -1,
            1
        };


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public LightPropagationEngine(
            ILightWorld world
        )
        {
            this.world = world;
        }


        // =========================================================
        // PUBLIC API
        // =========================================================

        public void RebuildLoadedWorld(
            int worldHeight
        )
        {
            if (!TryGetLoadedBounds(
                    out LightBounds loadedBounds))
            {
                return;
            }

            int minY =
                loadedBounds.MinY;

            int maxY =
                Mathf.Min(
                    loadedBounds.MaxY,
                    worldHeight - 1
                );

            if (minY > maxY)
                return;

            // ВАЖНО:
            // RebuildRegion принимает:
            //
            // minX, maxX, minY, maxY
            //
            RebuildRegion(
                loadedBounds.MinX,
                loadedBounds.MaxX,
                minY,
                maxY
            );
        }


        public void RebuildAfterChunkGenerated(
            int chunkX,
            int chunkY,
            int worldHeight
        )
        {
            if (!TryGetLoadedBounds(
                    out LightBounds loadedBounds))
            {
                return;
            }

            int chunkMinX =
                chunkX * Chunk.SizeX;

            int chunkMaxX =
                chunkMinX +
                Chunk.SizeX - 1;


            int minX =
                chunkMinX -
                LightSpreadRadius;

            int maxX =
                chunkMaxX +
                LightSpreadRadius;


            int minY =
                loadedBounds.MinY;

            int maxY =
                Mathf.Min(
                    loadedBounds.MaxY,
                    worldHeight - 1
                );


            if (minY > maxY)
                return;


            RebuildRegion(
                minX,
                maxX,
                minY,
                maxY
            );
        }


        public void RebuildAfterBlockChanged(
            int worldX,
            int worldY,
            int worldHeight
        )
        {
            int minX =
                worldX -
                LightSpreadRadius;

            int maxX =
                worldX +
                LightSpreadRadius;


            int minY = 0;

            int maxY =
                worldHeight - 1;


            RebuildRegion(
                minX,
                maxX,
                minY,
                maxY
            );
        }


        public void GenerateSunlightForChunk(
            int chunkX,
            int chunkY
        )
        {
            RebuildAfterChunkGenerated(
                chunkX,
                chunkY,
                256
            );
        }


        // =========================================================
        // REGION REBUILD
        // =========================================================

        private void RebuildRegion(
            int minX,
            int maxX,
            int minY,
            int maxY
        )
        {
            if (world == null)
                return;


            if (minX > maxX ||
                minY > maxY)
            {
                return;
            }


            // -----------------------------------------------------
            // Не пересчитываем область за пределами загруженного
            // мира по вертикали/горизонтали.
            // -----------------------------------------------------

            if (!TryGetLoadedBounds(
                    out LightBounds loadedBounds))
            {
                return;
            }


            minX =
                Mathf.Max(
                    minX,
                    loadedBounds.MinX
                );

            maxX =
                Mathf.Min(
                    maxX,
                    loadedBounds.MaxX
                );

            minY =
                Mathf.Max(
                    minY,
                    loadedBounds.MinY
                );

            maxY =
                Mathf.Min(
                    maxY,
                    loadedBounds.MaxY
                );


            if (minX > maxX ||
                minY > maxY)
            {
                return;
            }


            // -----------------------------------------------------
            // Очищаем очередь от предыдущего пересчёта.
            // -----------------------------------------------------

            propagationQueue.Clear();


            // -----------------------------------------------------
            // 1. ОЧИЩАЕМ СТАРОЕ ОСВЕЩЕНИЕ
            // -----------------------------------------------------

            ClearRegion(
                new LightBounds(
                    minX,
                    minY,
                    maxX,
                    maxY
                )
            );


            // -----------------------------------------------------
            // 2. СОЛНЕЧНЫЙ СВЕТ
            // -----------------------------------------------------

            SeedSunlight(
                new LightBounds(
                    minX,
                    minY,
                    maxX,
                    maxY
                ),
                maxY + 1
            );


            // -----------------------------------------------------
            // 3. RGB ИСТОЧНИКИ
            // -----------------------------------------------------

            SeedEmissiveBlocks(
                new LightBounds(
                    minX,
                    minY,
                    maxX,
                    maxY
                )
            );


            // -----------------------------------------------------
            // 4. ГРАНИЧНЫЙ СВЕТ
            //
            // Нужен, чтобы локальный пересчёт не создавал
            // резкую тёмную границу.
            // -----------------------------------------------------

            SeedExistingBoundaryLight(
                new LightBounds(
                    minX,
                    minY,
                    maxX,
                    maxY
                )
            );


            // -----------------------------------------------------
            // 5. РАСПРОСТРАНЯЕМ СВЕТ
            // -----------------------------------------------------

            Propagate(
                new LightBounds(
                    minX,
                    minY,
                    maxX,
                    maxY
                )
            );
        }


        // =========================================================
        // CLEAR
        // =========================================================

        private void ClearRegion(
            LightBounds bounds
        )
        {
            for (
                int x = bounds.MinX;
                x <= bounds.MaxX;
                x++
            )
            {
                for (
                    int y = bounds.MinY;
                    y <= bounds.MaxY;
                    y++
                )
                {
                    if (!world.IsLoaded(
                            x,
                            y))
                    {
                        continue;
                    }


                    world.SetLight(
                        x,
                        y,
                        0,
                        0,
                        0,
                        0
                    );
                }
            }
        }


        // =========================================================
        // SUNLIGHT
        // =========================================================

        private void SeedSunlight(
            LightBounds bounds,
            int worldHeight
        )
        {
            int skyTop =
                worldHeight - 1;


            for (
                int x = bounds.MinX;
                x <= bounds.MaxX;
                x++
            )
            {
                byte directSun =
                    MaxLight;


                for (
                    int y = skyTop;
                    y >= bounds.MinY;
                    y--
                )
                {
                    if (directSun == 0)
                        break;


                    if (!world.IsLoaded(
                            x,
                            y))
                    {
                        continue;
                    }


                    ushort blockID =
                        world.GetBlock(
                            x,
                            y
                        );


                    int opacity =
                        GetOpacity(
                            blockID
                        );


                    bool insideRegion =
                        y <= bounds.MaxY;


                    // -------------------------------------------------
                    // AIR
                    // -------------------------------------------------

                    if (blockID == 0)
                    {
                        if (insideRegion)
                        {
                            SetSunlightSource(
                                x,
                                y,
                                directSun,
                                true
                            );
                        }

                        continue;
                    }


                    // -------------------------------------------------
                    // ПОЛНОСТЬЮ НЕПРОЗРАЧНЫЙ БЛОК
                    // -------------------------------------------------

                    if (opacity >= MaxLight)
                    {
                        if (insideRegion)
                        {
                            byte faceLight =
                                directSun > 0
                                    ? (byte)(
                                        directSun - 1)
                                    : (byte)0;


                            SetSunlightSource(
                                x,
                                y,
                                faceLight,
                                false
                            );
                        }


                        // Солнечный свет дальше
                        // через блок не проходит.
                        break;
                    }


                    // -------------------------------------------------
                    // ЧАСТИЧНО ПРОЗРАЧНЫЙ БЛОК
                    // -------------------------------------------------

                    directSun =
                        SubtractLight(
                            directSun,
                            opacity
                        );


                    if (directSun == 0)
                        continue;


                    if (insideRegion)
                    {
                        SetSunlightSource(
                            x,
                            y,
                            directSun,
                            true
                        );
                    }
                }
            }
        }


        private void SetSunlightSource(
            int x,
            int y,
            byte sunlight,
            bool propagate
        )
        {
            if (sunlight == 0)
                return;


            LightNode current =
                world.GetLight(
                    x,
                    y
                );


            if (sunlight <= current.Sun)
                return;


            current.Sun =
                sunlight;


            world.SetLight(
                x,
                y,
                current.Sun,
                current.R,
                current.G,
                current.B
            );


            if (propagate)
            {
                propagationQueue.Enqueue(
                    new LightPoint(
                        x,
                        y
                    )
                );
            }
        }


        // =========================================================
        // BLOCK LIGHT
        // =========================================================

        private void SeedEmissiveBlocks(
            LightBounds bounds
        )
        {
            for (
                int x = bounds.MinX;
                x <= bounds.MaxX;
                x++
            )
            {
                for (
                    int y = bounds.MinY;
                    y <= bounds.MaxY;
                    y++
                )
                {
                    if (!world.IsLoaded(
                            x,
                            y))
                    {
                        continue;
                    }


                    ushort blockID =
                        world.GetBlock(
                            x,
                            y
                        );


                    if (blockID == 0)
                        continue;


                    BlockDefinition definition =
                        world.GetBlockDefinition(
                            blockID
                        );


                    if (definition == null)
                        continue;


                    byte r =
                        ClampLight(
                            definition.LightEmissionR
                        );

                    byte g =
                        ClampLight(
                            definition.LightEmissionG
                        );

                    byte b =
                        ClampLight(
                            definition.LightEmissionB
                        );


                    if (r == 0 &&
                        g == 0 &&
                        b == 0)
                    {
                        continue;
                    }


                    LightNode existing =
                        world.GetLight(
                            x,
                            y
                        );


                    LightNode result =
                        new LightNode(
                            existing.Sun,

                            Max(
                                existing.R,
                                r
                            ),

                            Max(
                                existing.G,
                                g
                            ),

                            Max(
                                existing.B,
                                b
                            )
                        );


                    world.SetLight(
                        x,
                        y,
                        result.Sun,
                        result.R,
                        result.G,
                        result.B
                    );


                    propagationQueue.Enqueue(
                        new LightPoint(
                            x,
                            y
                        )
                    );
                }
            }
        }


        // =========================================================
        // BOUNDARY LIGHT
        // =========================================================

        private void SeedExistingBoundaryLight(
            LightBounds bounds
        )
        {
            int left =
                bounds.MinX - 1;

            int right =
                bounds.MaxX + 1;


            for (
                int y = bounds.MinY;
                y <= bounds.MaxY;
                y++
            )
            {
                AddBoundarySource(
                    left,
                    y
                );


                AddBoundarySource(
                    right,
                    y
                );
            }


            int bottom =
                bounds.MinY - 1;

            int top =
                bounds.MaxY + 1;


            for (
                int x = bounds.MinX;
                x <= bounds.MaxX;
                x++
            )
            {
                AddBoundarySource(
                    x,
                    bottom
                );


                AddBoundarySource(
                    x,
                    top
                );
            }
        }


        private void AddBoundarySource(
            int x,
            int y
        )
        {
            if (!world.IsLoaded(
                    x,
                    y))
            {
                return;
            }


            LightNode light =
                world.GetLight(
                    x,
                    y
                );


            if (light.IsEmpty)
                return;


            propagationQueue.Enqueue(
                new LightPoint(
                    x,
                    y
                )
            );
        }


        // =========================================================
        // PROPAGATION
        // =========================================================

        private void Propagate(
            LightBounds bounds
        )
        {
            while (
                propagationQueue.Count > 0
            )
            {
                LightPoint sourcePoint =
                    propagationQueue.Dequeue();


                LightNode source =
                    world.GetLight(
                        sourcePoint.X,
                        sourcePoint.Y
                    );


                if (source.IsEmpty)
                    continue;


                for (
                    int direction = 0;
                    direction < 4;
                    direction++
                )
                {
                    int targetX =
                        sourcePoint.X +
                        DirectionX[
                            direction
                        ];


                    int targetY =
                        sourcePoint.Y +
                        DirectionY[
                            direction
                        ];


                    if (!bounds.Contains(
                            targetX,
                            targetY))
                    {
                        continue;
                    }


                    if (!world.IsLoaded(
                            targetX,
                            targetY))
                    {
                        continue;
                    }


                    PropagateTo(
                        source,
                        targetX,
                        targetY
                    );
                }
            }
        }


        private void PropagateTo(
            LightNode source,
            int targetX,
            int targetY
        )
        {
            ushort targetBlock =
                world.GetBlock(
                    targetX,
                    targetY
                );


            int opacity =
                GetOpacity(
                    targetBlock
                );


            bool fullyOpaque =
                targetBlock != 0 &&
                opacity >= MaxLight;


            int attenuation;


            if (fullyOpaque)
            {
                // Блок получает свет на своей
                // поверхности.
                attenuation = 1;
            }
            else
            {
                attenuation =
                    1 + opacity;
            }


            byte newSun =
                SubtractLight(
                    source.Sun,
                    attenuation
                );


            byte newR =
                SubtractLight(
                    source.R,
                    attenuation
                );


            byte newG =
                SubtractLight(
                    source.G,
                    attenuation
                );


            byte newB =
                SubtractLight(
                    source.B,
                    attenuation
                );


            if (newSun == 0 &&
                newR == 0 &&
                newG == 0 &&
                newB == 0)
            {
                return;
            }


            LightNode existing =
                world.GetLight(
                    targetX,
                    targetY
                );


            LightNode merged =
                new LightNode(
                    Max(
                        existing.Sun,
                        newSun
                    ),

                    Max(
                        existing.R,
                        newR
                    ),

                    Max(
                        existing.G,
                        newG
                    ),

                    Max(
                        existing.B,
                        newB
                    )
                );


            if (
                merged.Sun == existing.Sun &&
                merged.R == existing.R &&
                merged.G == existing.G &&
                merged.B == existing.B
            )
            {
                return;
            }


            world.SetLight(
                targetX,
                targetY,
                merged.Sun,
                merged.R,
                merged.G,
                merged.B
            );


            // Полностью непрозрачный блок
            // получает свет, но дальше его
            // не пропускает.
            if (fullyOpaque)
                return;


            propagationQueue.Enqueue(
                new LightPoint(
                    targetX,
                    targetY
                )
            );
        }


        // =========================================================
        // BLOCK PROPERTIES
        // =========================================================

        private int GetOpacity(
            ushort blockID
        )
        {
            // ID 0 = AIR.
            if (blockID == 0)
                return 0;


            BlockDefinition definition =
                world.GetBlockDefinition(
                    blockID
                );


            if (definition == null)
            {
                // Неизвестный блок считаем
                // полностью непрозрачным.
                return MaxLight;
            }


            int opacity =
                definition.LightOpacity;


            if (opacity < 0)
                return 0;


            if (opacity > MaxLight)
                return MaxLight;


            return opacity;
        }


        // =========================================================
        // LOADED WORLD BOUNDS
        // =========================================================

        private bool TryGetLoadedBounds(
            out LightBounds bounds
        )
        {
            bounds = default;


            Game.World.World concreteWorld =
                world as Game.World.World;


            if (concreteWorld == null)
                return false;


            bool found = false;


            int minX = 0;
            int minY = 0;

            int maxX = 0;
            int maxY = 0;


            foreach (
                var pair
                in concreteWorld.GetLoadedChunks()
            )
            {
                Chunk chunk =
                    pair.Value;


                if (chunk == null)
                    continue;


                int chunkMinX =
                    chunk.X *
                    Chunk.SizeX;


                int chunkMinY =
                    chunk.Y *
                    Chunk.SizeY;


                int chunkMaxX =
                    chunkMinX +
                    Chunk.SizeX - 1;


                int chunkMaxY =
                    chunkMinY +
                    Chunk.SizeY - 1;


                if (!found)
                {
                    minX =
                        chunkMinX;

                    minY =
                        chunkMinY;

                    maxX =
                        chunkMaxX;

                    maxY =
                        chunkMaxY;


                    found = true;

                    continue;
                }


                if (chunkMinX < minX)
                    minX = chunkMinX;


                if (chunkMinY < minY)
                    minY = chunkMinY;


                if (chunkMaxX > maxX)
                    maxX = chunkMaxX;


                if (chunkMaxY > maxY)
                    maxY = chunkMaxY;
            }


            if (!found)
                return false;


            bounds =
                new LightBounds(
                    minX,
                    minY,
                    maxX,
                    maxY
                );


            return true;
        }


        // =========================================================
        // HELPERS
        // =========================================================

        private static byte SubtractLight(
            byte value,
            int amount
        )
        {
            int result =
                value - amount;


            if (result <= 0)
                return 0;


            if (result >= MaxLight)
                return MaxLight;


            return (byte)result;
        }


        private static byte ClampLight(
            int value
        )
        {
            if (value <= 0)
                return 0;


            if (value >= MaxLight)
                return MaxLight;


            return (byte)value;
        }


        private static byte Max(
            byte a,
            byte b
        )
        {
            return a > b
                ? a
                : b;
        }


        // =========================================================
        // LIGHT POINT
        // =========================================================

        private struct LightPoint
        {
            public readonly int X;
            public readonly int Y;


            public LightPoint(
                int x,
                int y
            )
            {
                X = x;
                Y = y;
            }
        }


        // =========================================================
        // LIGHT BOUNDS
        // =========================================================

        private struct LightBounds
        {
            public readonly int MinX;
            public readonly int MinY;

            public readonly int MaxX;
            public readonly int MaxY;


            public LightBounds(
                int minX,
                int minY,
                int maxX,
                int maxY
            )
            {
                MinX = minX;
                MinY = minY;

                MaxX = maxX;
                MaxY = maxY;
            }


            public bool Contains(
                int x,
                int y
            )
            {
                return
                    x >= MinX &&
                    x <= MaxX &&
                    y >= MinY &&
                    y <= MaxY;
            }
        }
    }
}
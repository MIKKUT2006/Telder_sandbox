
using Game.Blocks;
using Game.World.Furniture;

using System.Collections.Generic;

using UnityEngine;


namespace Game.World.Lighting
{

    public class LightPropagationEngine
    {

        public const byte MaxLight =
            15;


        private const int LightSpreadRadius =
            MaxLight +
            2;


        private readonly ILightWorld world;


        private readonly Queue<LightPoint>
            propagationQueue =
            new Queue<LightPoint>(
                4096
            );


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


        public LightPropagationEngine(
            ILightWorld world
        )
        {

            this.world =
                world;

        }


        // =========================================================
        // PUBLIC API
        // =========================================================

        public void RebuildLoadedWorld(
            int worldHeight
        )
        {

            if (
                !TryGetLoadedBounds(
                    out LightBounds loadedBounds
                )
            )
            {

                return;

            }


            RebuildRegion(
                loadedBounds,
                worldHeight
            );

        }


        public void RebuildAfterChunkGenerated(
            int chunkX,
            int chunkY,
            int worldHeight
        )
        {

            if (
                !TryGetLoadedBounds(
                    out LightBounds loadedBounds
                )
            )
            {

                return;

            }


            int chunkMinX =
                chunkX *
                Chunk.SizeX;


            int chunkMaxX =
                chunkMinX +
                Chunk.SizeX -
                1;


            LightBounds bounds =
                new LightBounds(
                    Mathf.Max(
                        loadedBounds.MinX,
                        chunkMinX -
                        LightSpreadRadius
                    ),

                    loadedBounds.MinY,

                    Mathf.Min(
                        loadedBounds.MaxX,
                        chunkMaxX +
                        LightSpreadRadius
                    ),

                    loadedBounds.MaxY
                );


            RebuildRegion(
                bounds,
                worldHeight
            );

        }


        public void RebuildAfterBlockChanged(
            int worldX,
            int worldY,
            int worldHeight
        )
        {

            if (
                !TryGetLoadedBounds(
                    out LightBounds loadedBounds
                )
            )
            {

                return;

            }


            int minX =
                Mathf.Max(
                    loadedBounds.MinX,
                    worldX -
                    LightSpreadRadius
                );


            int maxX =
                Mathf.Min(
                    loadedBounds.MaxX,
                    worldX +
                    LightSpreadRadius
                );


            if (
                minX >
                maxX
            )
            {

                return;

            }


            // IMPORTANT:
            //
            // Do NOT clamp MinY to 0.
            //
            // Negative chunk/world Y is valid in this world,
            // therefore a torch at Y=-20 must participate in
            // exactly the same rebuild as a torch at Y=20.
            LightBounds bounds =
                new LightBounds(
                    minX,
                    loadedBounds.MinY,
                    maxX,
                    loadedBounds.MaxY
                );


            RebuildRegion(
                bounds,
                worldHeight
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
        // REGION REBUILD — MAIN IMPLEMENTATION
        // =========================================================
        //
        // The project previously contained TWO incomplete lighting
        // implementations in this class:
        //
        // RebuildRegion(int,int,int,int)
        //
        // and a newer bounds-based pipeline whose public methods
        // called:
        //
        // RebuildRegion(LightBounds,int)
        //
        // but that overload did not exist.
        //
        // This is the missing canonical overload.
        //
        // =========================================================

        private void RebuildRegion(
            LightBounds bounds,
            int worldHeight
        )
        {

            if (
                world ==
                null
            )
            {

                return;

            }


            if (
                bounds.MinX >
                bounds.MaxX
                ||
                bounds.MinY >
                bounds.MaxY
            )
            {

                return;

            }


            propagationQueue.Clear();


            // 1. Remove stale light from the rebuild region.
            ClearRegion(
                bounds
            );


            // 2. Re-seed sunlight.
            SeedSunlight(
                bounds,
                worldHeight
            );


            // 3. Re-seed foreground + Furniture RGB emitters.
            SeedEmissiveBlocks(
                bounds
            );


            // 4. Keep light coming from immediately outside the
            // rebuilt X region.
            SeedExistingBoundaryLight(
                bounds
            );


            // 5. Propagate all channels.
            Propagate(
                bounds
            );


            // 6. Renderer light textures must know their data changed.
            MarkRegionDirty(
                bounds
            );

        }


        // =========================================================
        // COMPATIBILITY OVERLOAD
        // =========================================================
        //
        // Keep this signature because older project code and old
        // patchers may still call it.
        //
        // =========================================================

        private void RebuildRegion(
            int minX,
            int maxX,
            int minY,
            int maxY
        )
        {

            RebuildRegion(
                new LightBounds(
                    minX,
                    minY,
                    maxX,
                    maxY
                ),

                Mathf.Max(
                    1,
                    maxY +
                    1
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

                    if (
                        !world.IsLoaded(
                            x,
                            y
                        )
                    )
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
                Mathf.Max(
                    worldHeight -
                    1,
                    bounds.MaxY
                );


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

                    if (
                        directSun ==
                        0
                    )
                    {

                        break;

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
                        y <=
                        bounds.MaxY;


                    if (
                        blockID ==
                        0
                    )
                    {

                        if (
                            insideRegion
                            &&
                            world.IsLoaded(
                                x,
                                y
                            )
                        )
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


                    if (
                        opacity >=
                        MaxLight
                    )
                    {

                        if (
                            insideRegion
                            &&
                            world.IsLoaded(
                                x,
                                y
                            )
                        )
                        {

                            byte faceLight =
                                directSun >
                                0
                                    ? (byte)(
                                        directSun -
                                        1
                                    )
                                    : (byte)0;


                            SetSunlightSource(
                                x,
                                y,
                                faceLight,
                                false
                            );

                        }


                        break;

                    }


                    directSun =
                        SubtractLight(
                            directSun,
                            opacity
                        );


                    if (
                        insideRegion
                        &&
                        world.IsLoaded(
                            x,
                            y
                        )
                    )
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

            if (
                sunlight ==
                0
            )
            {

                return;

            }


            LightNode current =
                world.GetLight(
                    x,
                    y
                );


            if (
                sunlight <=
                current.Sun
            )
            {

                return;

            }


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


            if (
                propagate
            )
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

                    if (
                        !world.IsLoaded(
                            x,
                            y
                        )
                    )
                    {

                        continue;

                    }


                    byte r =
                        0;


                    byte g =
                        0;


                    byte b =
                        0;


                    // =============================================
                    // FOREGROUND
                    // =============================================

                    ushort blockID =
                        world.GetBlock(
                            x,
                            y
                        );


                    if (
                        blockID !=
                        0
                    )
                    {

                        BlockDefinition definition =
                            world.GetBlockDefinition(
                                blockID
                            );


                        if (
                            definition !=
                            null
                        )
                        {

                            r =
                                ClampLight(
                                    definition
                                        .LightEmissionR
                                );


                            g =
                                ClampLight(
                                    definition
                                        .LightEmissionG
                                );


                            b =
                                ClampLight(
                                    definition
                                        .LightEmissionB
                                );

                        }

                    }


                    // =============================================
                    // FURNITURE
                    // =============================================

                    if (
                        FurnitureLayerManager.Instance !=
                        null
                        &&
                        FurnitureLayerManager.Instance
                            .TryGetDefinition(
                                x,
                                y,
                                out BlockDefinition
                                    furniture
                            )
                        &&
                        furniture !=
                        null
                    )
                    {

                        r =
                            Max(
                                r,
                                ClampLight(
                                    furniture
                                        .LightEmissionR
                                )
                            );


                        g =
                            Max(
                                g,
                                ClampLight(
                                    furniture
                                        .LightEmissionG
                                )
                            );


                        b =
                            Max(
                                b,
                                ClampLight(
                                    furniture
                                        .LightEmissionB
                                )
                            );

                    }


                    if (
                        r ==
                        0
                        &&
                        g ==
                        0
                        &&
                        b ==
                        0
                    )
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
                bounds.MinX -
                1;


            int right =
                bounds.MaxX +
                1;


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
                bounds.MinY -
                1;


            int top =
                bounds.MaxY +
                1;


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

            if (
                !world.IsLoaded(
                    x,
                    y
                )
            )
            {

                return;

            }


            LightNode light =
                world.GetLight(
                    x,
                    y
                );


            if (
                light.IsEmpty
            )
            {

                return;

            }


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
                propagationQueue.Count >
                0
            )
            {

                LightPoint sourcePoint =
                    propagationQueue.Dequeue();


                LightNode source =
                    world.GetLight(
                        sourcePoint.X,
                        sourcePoint.Y
                    );


                if (
                    source.IsEmpty
                )
                {

                    continue;

                }


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


                    if (
                        !bounds.Contains(
                            targetX,
                            targetY
                        )
                    )
                    {

                        continue;

                    }


                    if (
                        !world.IsLoaded(
                            targetX,
                            targetY
                        )
                    )
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
                targetBlock !=
                0
                &&
                opacity >=
                MaxLight;


            int attenuation =
                fullyOpaque
                    ? 1
                    : 1 +
                      opacity;


            LightNode candidate =
                new LightNode(
                    SubtractLight(
                        source.Sun,
                        attenuation
                    ),

                    SubtractLight(
                        source.R,
                        attenuation
                    ),

                    SubtractLight(
                        source.G,
                        attenuation
                    ),

                    SubtractLight(
                        source.B,
                        attenuation
                    )
                );


            if (
                candidate.IsEmpty
            )
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
                        candidate.Sun
                    ),

                    Max(
                        existing.R,
                        candidate.R
                    ),

                    Max(
                        existing.G,
                        candidate.G
                    ),

                    Max(
                        existing.B,
                        candidate.B
                    )
                );


            if (
                merged.Sun ==
                existing.Sun
                &&
                merged.R ==
                existing.R
                &&
                merged.G ==
                existing.G
                &&
                merged.B ==
                existing.B
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


            if (
                fullyOpaque
            )
            {

                return;

            }


            propagationQueue.Enqueue(
                new LightPoint(
                    targetX,
                    targetY
                )
            );

        }


        // =========================================================
        // DIRTY
        // =========================================================

        private void MarkRegionDirty(
            LightBounds bounds
        )
        {

            HashSet<ChunkLightData> dirty =
                new HashSet<ChunkLightData>();


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

                    if (
                        !world.IsLoaded(
                            x,
                            y
                        )
                    )
                    {

                        continue;

                    }


                    ChunkLightData data =
                        world.GetLightData(
                            x,
                            y
                        );


                    if (
                        data !=
                        null
                    )
                    {

                        dirty.Add(
                            data
                        );

                    }

                }

            }


            foreach (
                ChunkLightData data
                in dirty
            )
            {

                data.MarkDirty();

            }

        }


        // =========================================================
        // BLOCK PROPERTIES
        // =========================================================

        private int GetOpacity(
            ushort blockID
        )
        {

            if (
                blockID ==
                0
            )
            {

                return 0;

            }


            BlockDefinition definition =
                world.GetBlockDefinition(
                    blockID
                );


            if (
                definition ==
                null
            )
            {

                return
                    MaxLight;

            }


            return
                Mathf.Clamp(
                    definition.LightOpacity,
                    0,
                    MaxLight
                );

        }


        // =========================================================
        // LOADED WORLD BOUNDS
        // =========================================================

        private bool TryGetLoadedBounds(
            out LightBounds bounds
        )
        {

            bounds =
                default;


            Game.World.World concreteWorld =
                world
                as
                Game.World.World;


            if (
                concreteWorld ==
                null
            )
            {

                return false;

            }


            bool found =
                false;


            int minX =
                0;


            int minY =
                0;


            int maxX =
                0;


            int maxY =
                0;


            foreach (
                var pair
                in concreteWorld.GetLoadedChunks()
            )
            {

                Chunk chunk =
                    pair.Value;


                if (
                    chunk ==
                    null
                )
                {

                    continue;

                }


                int chunkMinX =
                    chunk.X *
                    Chunk.SizeX;


                int chunkMinY =
                    chunk.Y *
                    Chunk.SizeY;


                int chunkMaxX =
                    chunkMinX +
                    Chunk.SizeX -
                    1;


                int chunkMaxY =
                    chunkMinY +
                    Chunk.SizeY -
                    1;


                if (
                    !found
                )
                {

                    minX =
                        chunkMinX;


                    minY =
                        chunkMinY;


                    maxX =
                        chunkMaxX;


                    maxY =
                        chunkMaxY;


                    found =
                        true;


                    continue;

                }


                if (
                    chunkMinX <
                    minX
                )
                {

                    minX =
                        chunkMinX;

                }


                if (
                    chunkMinY <
                    minY
                )
                {

                    minY =
                        chunkMinY;

                }


                if (
                    chunkMaxX >
                    maxX
                )
                {

                    maxX =
                        chunkMaxX;

                }


                if (
                    chunkMaxY >
                    maxY
                )
                {

                    maxY =
                        chunkMaxY;

                }

            }


            if (
                !found
            )
            {

                return false;

            }


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
                value -
                amount;


            if (
                result <=
                0
            )
            {

                return 0;

            }


            if (
                result >=
                MaxLight
            )
            {

                return MaxLight;

            }


            return
                (byte)result;

        }


        private static byte ClampLight(
            int value
        )
        {

            if (
                value <=
                0
            )
            {

                return 0;

            }


            if (
                value >=
                MaxLight
            )
            {

                return MaxLight;

            }


            return
                (byte)value;

        }


        private static byte Max(
            byte a,
            byte b
        )
        {

            return
                a >
                b
                    ? a
                    : b;

        }


        private struct LightPoint
        {

            public readonly int X;

            public readonly int Y;


            public LightPoint(
                int x,
                int y
            )
            {

                X =
                    x;


                Y =
                    y;

            }

        }


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

                MinX =
                    minX;


                MinY =
                    minY;


                MaxX =
                    maxX;


                MaxY =
                    maxY;

            }


            public bool Contains(
                int x,
                int y
            )
            {

                return
                    x >=
                    MinX
                    &&
                    x <=
                    MaxX
                    &&
                    y >=
                    MinY
                    &&
                    y <=
                    MaxY;

            }

        }

    }

}


using System.Collections.Generic;

using UnityEngine;

using Game.Blocks;
using Game.Content;


namespace Game.World.Collision
{

    public class WorldCollision
    {

        private readonly World world;


        private const float CellEpsilon =
            0.0001f;


        public WorldCollision(
            World world
        )
        {

            this.world =
                world;

        }


        // =====================================================
        // BASIC BLOCK ACCESS
        // =====================================================

        public ushort GetBlock(
            int worldX,
            int worldY
        )
        {

            return
                world.GetBlock(
                    worldX,
                    worldY
                );

        }


        public bool TryGetBlockDefinition(
            int worldX,
            int worldY,
            out BlockDefinition definition
        )
        {

            return
                TryGetBlockDefinition(
                    GetBlock(
                        worldX,
                        worldY
                    ),
                    out definition
                );

        }


        public bool TryGetBlockDefinition(
            ushort blockID,
            out BlockDefinition definition
        )
        {

            definition =
                null;


            if (
                blockID ==
                0
            )
            {

                return false;

            }


            ContentID contentID;


            try
            {

                contentID =
                    BlockIDRegistry.GetContentID(
                        blockID
                    );

            }
            catch
            {

                return false;

            }


            if (
                !BlockRegistry.Contains(
                    contentID
                )
            )
            {

                return false;

            }


            definition =
                BlockRegistry.Get(
                    contentID
                );


            return
                definition !=
                null;

        }


        // =====================================================
        // SHAPE
        // =====================================================

        public BlockCollisionShape GetCollisionShape(
            int worldX,
            int worldY
        )
        {

            return
                GetCollisionShape(
                    GetBlock(
                        worldX,
                        worldY
                    )
                );

        }


        public BlockCollisionShape GetCollisionShape(
            ushort blockID
        )
        {

            if (
                !TryGetBlockDefinition(
                    blockID,
                    out BlockDefinition block
                )
            )
            {

                return
                    BlockCollisionShape.None;

            }


            if (
                !block.Solid
            )
            {

                return
                    BlockCollisionShape.None;

            }


            BlockCollisionShape shape =
                BlockCollisionShapeParser.Parse(
                    block.CollisionShape
                );


            if (
                shape ==
                BlockCollisionShape.Custom
            )
            {

                if (
                    block.CollisionRects ==
                    null
                    ||
                    block.CollisionRects.Count ==
                    0
                )
                {

                    return
                        BlockCollisionShape.None;

                }

            }


            return shape;

        }


        // =====================================================
        // COMPATIBILITY
        // =====================================================
        //
        // Old systems can still ask "IsSolid".
        //
        // It now means:
        // "does this block have ANY collision shape?"
        //
        // PlayerCollision itself no longer treats a solid block
        // as a full 1x1 square.
        //
        // =====================================================

        public bool IsSolid(
            int worldX,
            int worldY
        )
        {

            return
                GetCollisionShape(
                    worldX,
                    worldY
                )
                !=
                BlockCollisionShape.None;

        }


        // =====================================================
        // LOCAL COLLISION RECTS
        // =====================================================

        public void GetLocalCollisionRects(
            ushort blockID,
            List<Rect> results
        )
        {

            if (
                results ==
                null
            )
            {

                return;

            }


            results.Clear();


            if (
                !TryGetBlockDefinition(
                    blockID,
                    out BlockDefinition block
                )
                ||
                !block.Solid
            )
            {

                return;

            }


            BlockCollisionShape shape =
                BlockCollisionShapeParser.Parse(
                    block.CollisionShape
                );


            switch (
                shape
            )
            {

                case BlockCollisionShape.None:

                    return;


                case BlockCollisionShape.Full:

                    results.Add(
                        new Rect(
                            0f,
                            0f,
                            1f,
                            1f
                        )
                    );


                    return;


                case BlockCollisionShape.HalfBottom:

                    results.Add(
                        new Rect(
                            0f,
                            0f,
                            1f,
                            0.5f
                        )
                    );


                    return;


                case BlockCollisionShape.HalfTop:

                    results.Add(
                        new Rect(
                            0f,
                            0.5f,
                            1f,
                            0.5f
                        )
                    );


                    return;


                case BlockCollisionShape.StairUpRight:

                    // Left side height = 0.5.
                    // Right side height = 1.0.

                    results.Add(
                        new Rect(
                            0f,
                            0f,
                            1f,
                            0.5f
                        )
                    );


                    results.Add(
                        new Rect(
                            0.5f,
                            0.5f,
                            0.5f,
                            0.5f
                        )
                    );


                    return;


                case BlockCollisionShape.StairUpLeft:

                    // Right side height = 0.5.
                    // Left side height = 1.0.

                    results.Add(
                        new Rect(
                            0f,
                            0f,
                            1f,
                            0.5f
                        )
                    );


                    results.Add(
                        new Rect(
                            0f,
                            0.5f,
                            0.5f,
                            0.5f
                        )
                    );


                    return;


                case BlockCollisionShape.Custom:

                    AppendCustomRects(
                        block,
                        results
                    );


                    return;

            }

        }


        private void AppendCustomRects(
            BlockDefinition block,
            List<Rect> results
        )
        {

            if (
                block ==
                null
                ||
                block.CollisionRects ==
                null
            )
            {

                return;

            }


            for (
                int i = 0;
                i < block.CollisionRects.Count;
                i++
            )
            {

                BlockCollisionRectDefinition source =
                    block.CollisionRects[i];


                if (
                    source ==
                    null
                )
                {

                    continue;

                }


                float x =
                    Mathf.Clamp01(
                        source.X
                    );


                float y =
                    Mathf.Clamp01(
                        source.Y
                    );


                float width =
                    Mathf.Clamp(
                        source.Width,
                        0f,
                        1f -
                        x
                    );


                float height =
                    Mathf.Clamp(
                        source.Height,
                        0f,
                        1f -
                        y
                    );


                if (
                    width <=
                    CellEpsilon
                    ||
                    height <=
                    CellEpsilon
                )
                {

                    continue;

                }


                results.Add(
                    new Rect(
                        x,
                        y,
                        width,
                        height
                    )
                );

            }

        }


        // =====================================================
        // WORLD AABB COLLISION
        // =====================================================

        public bool OverlapsAny(
            Rect worldBounds
        )
        {

            int minBlockX =
                Mathf.FloorToInt(
                    worldBounds.xMin
                );


            int maxBlockX =
                Mathf.FloorToInt(
                    worldBounds.xMax -
                    CellEpsilon
                );


            int minBlockY =
                Mathf.FloorToInt(
                    worldBounds.yMin
                );


            int maxBlockY =
                Mathf.FloorToInt(
                    worldBounds.yMax -
                    CellEpsilon
                );


            for (
                int x = minBlockX;
                x <= maxBlockX;
                x++
            )
            {

                for (
                    int y = minBlockY;
                    y <= maxBlockY;
                    y++
                )
                {

                    if (
                        OverlapsBlock(
                            x,
                            y,
                            worldBounds
                        )
                    )
                    {

                        return true;

                    }

                }

            }


            return false;

        }


        public bool OverlapsBlock(
            int worldX,
            int worldY,
            Rect worldBounds
        )
        {

            ushort blockID =
                GetBlock(
                    worldX,
                    worldY
                );


            if (
                !TryGetBlockDefinition(
                    blockID,
                    out BlockDefinition block
                )
                ||
                !block.Solid
            )
            {

                return false;

            }


            BlockCollisionShape shape =
                BlockCollisionShapeParser.Parse(
                    block.CollisionShape
                );


            switch (
                shape
            )
            {

                case BlockCollisionShape.None:

                    return false;


                case BlockCollisionShape.Full:

                    return
                        Intersects(
                            worldBounds,

                            new Rect(
                                worldX,
                                worldY,
                                1f,
                                1f
                            )
                        );


                case BlockCollisionShape.HalfBottom:

                    return
                        Intersects(
                            worldBounds,

                            new Rect(
                                worldX,
                                worldY,
                                1f,
                                0.5f
                            )
                        );


                case BlockCollisionShape.HalfTop:

                    return
                        Intersects(
                            worldBounds,

                            new Rect(
                                worldX,
                                worldY +
                                0.5f,
                                1f,
                                0.5f
                            )
                        );


                case BlockCollisionShape.StairUpRight:

                    return
                        Intersects(
                            worldBounds,

                            new Rect(
                                worldX,
                                worldY,
                                1f,
                                0.5f
                            )
                        )
                        ||
                        Intersects(
                            worldBounds,

                            new Rect(
                                worldX +
                                0.5f,
                                worldY +
                                0.5f,
                                0.5f,
                                0.5f
                            )
                        );


                case BlockCollisionShape.StairUpLeft:

                    return
                        Intersects(
                            worldBounds,

                            new Rect(
                                worldX,
                                worldY,
                                1f,
                                0.5f
                            )
                        )
                        ||
                        Intersects(
                            worldBounds,

                            new Rect(
                                worldX,
                                worldY +
                                0.5f,
                                0.5f,
                                0.5f
                            )
                        );


                case BlockCollisionShape.Custom:

                    return
                        OverlapsCustom(
                            block,
                            worldX,
                            worldY,
                            worldBounds
                        );

            }


            return false;

        }


        private bool OverlapsCustom(
            BlockDefinition block,
            int worldX,
            int worldY,
            Rect worldBounds
        )
        {

            if (
                block.CollisionRects ==
                null
            )
            {

                return false;

            }


            for (
                int i = 0;
                i < block.CollisionRects.Count;
                i++
            )
            {

                BlockCollisionRectDefinition source =
                    block.CollisionRects[i];


                if (
                    source ==
                    null
                )
                {

                    continue;

                }


                float x =
                    Mathf.Clamp01(
                        source.X
                    );


                float y =
                    Mathf.Clamp01(
                        source.Y
                    );


                float width =
                    Mathf.Clamp(
                        source.Width,
                        0f,
                        1f -
                        x
                    );


                float height =
                    Mathf.Clamp(
                        source.Height,
                        0f,
                        1f -
                        y
                    );


                if (
                    width <=
                    CellEpsilon
                    ||
                    height <=
                    CellEpsilon
                )
                {

                    continue;

                }


                if (
                    Intersects(
                        worldBounds,

                        new Rect(
                            worldX +
                            x,

                            worldY +
                            y,

                            width,
                            height
                        )
                    )
                )
                {

                    return true;

                }

            }


            return false;

        }


        private bool Intersects(
            Rect a,
            Rect b
        )
        {

            return
                a.xMax >
                b.xMin
                &&
                a.xMin <
                b.xMax
                &&
                a.yMax >
                b.yMin
                &&
                a.yMin <
                b.yMax;

        }

    }

}

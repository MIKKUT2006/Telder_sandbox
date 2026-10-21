
using Game.Blocks;
using Game.BlockTransforms;
using Game.Content;
using Game.GameplaySystems.Interactions;
using System.Collections.Generic;
using UnityEngine;


namespace Game.World.Collision
{

    public class WorldCollision
    {

        private readonly World world;
        // Collision is queried from the main thread. Reuse this buffer to
        // avoid allocation in every PlayerCollision.OverlapsAny call.
        private readonly List<Rect> transformedCollisionRects = new List<Rect>(4);


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
            )
            {
                return BlockCollisionShape.None;
            }

            if (!block.Solid || InteractiveBlockRuntime.IsPlatform(block)
                || InteractiveBlockRuntime.DisablesForegroundCollision(worldX, worldY, block))
            {
                return BlockCollisionShape.None;
            }

            BlockCollisionShape shape =
                BlockCollisionShapeParser.Parse(
                    block.CollisionShape
                );

            if (
                shape == BlockCollisionShape.Custom &&
                (block.CollisionRects == null || block.CollisionRects.Count == 0)
            )
            {
                return BlockCollisionShape.None;
            }

            return shape;
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
                !block.Solid ||
                InteractiveBlockRuntime.IsLadder(block) ||
                InteractiveBlockRuntime.IsPlatform(block)
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
                ||
                InteractiveBlockRuntime.IsLadder(block) ||
                InteractiveBlockRuntime.IsPlatform(block)
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


        public void GetLocalCollisionRects(
            int worldX,
            int worldY,
            List<Rect> results
        )
        {
            if (results == null)
                return;

            results.Clear();

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
                ||
                InteractiveBlockRuntime.DisablesForegroundCollision(
                    worldX,
                    worldY,
                    block
                )
            )
            {
                return;
            }

            GetLocalCollisionRects(
                blockID,
                results
            );
            byte transform = BlockTransformRegistry.GetRaw(worldX, worldY);
            if (transform == 0 || results.Count == 0) return;
            var state = new BlockTransformState(transform);
            for (int i = 0; i < results.Count; i++)
            {
                Rect r = results[i];
                Vector2 a = BlockTransformRenderBridge.TransformPoint01(
                    new Vector2(r.xMin, r.yMin), state);
                Vector2 b = BlockTransformRenderBridge.TransformPoint01(
                    new Vector2(r.xMax, r.yMin), state);
                Vector2 c = BlockTransformRenderBridge.TransformPoint01(
                    new Vector2(r.xMin, r.yMax), state);
                Vector2 d = BlockTransformRenderBridge.TransformPoint01(
                    new Vector2(r.xMax, r.yMax), state);
                float minX = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x));
                float maxX = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x));
                float minY = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
                float maxY = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));
                results[i] = Rect.MinMaxRect(minX, minY, maxX, maxY);
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
            // Creatures and pathfinding use this query; furniture doors are not tiles.
            if (InteractiveBlockRuntime.IntersectsClosedFurniture(worldBounds))
                return true;


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
     ||
     InteractiveBlockRuntime.IsPlatform(block)
     ||
     InteractiveBlockRuntime.DisablesForegroundCollision(
         worldX,
         worldY,
         block
     )
 )
            {
                return false;
            }



            BlockCollisionShape shape =
                BlockCollisionShapeParser.Parse(
                    block.CollisionShape
                );


            if (shape != BlockCollisionShape.Full &&
                BlockTransformRegistry.GetRaw(worldX, worldY) != 0)
            {
                GetLocalCollisionRects(worldX, worldY, transformedCollisionRects);
                for (int i = 0; i < transformedCollisionRects.Count; i++)
                {
                    Rect r = transformedCollisionRects[i];
                    if (Intersects(worldBounds,
                        new Rect(worldX + r.x, worldY + r.y, r.width, r.height)))
                        return true;
                }
                return false;
            }

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


        public bool TryGetPlatformTop(
            Rect horizontalBounds,
            float previousBottom,
            float nextBottom,
            int ignoredPlatformY,
            out float platformTop)
        {
            platformTop = 0f;

            if (world == null || nextBottom >= previousBottom)
                return false;

            const float epsilon = 0.0001f;

            int minX = Mathf.FloorToInt(horizontalBounds.xMin + epsilon);
            int maxX = Mathf.FloorToInt(horizontalBounds.xMax - epsilon);
            int minY = Mathf.FloorToInt(nextBottom) - 1;
            int maxY = Mathf.FloorToInt(previousBottom);

            bool found = false;
            float bestTop = float.NegativeInfinity;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = maxY; y >= minY; y--)
                {
                    if (y == ignoredPlatformY)
                        continue;

                    ushort id = world.GetBlock(x, y);

                    if (!TryGetBlockDefinition(id, out BlockDefinition definition))
                        continue;

                    if (!definition.Solid ||
                        !InteractiveBlockRuntime.IsPlatform(definition))
                        continue;

                    if (BlockCollisionShapeParser.Parse(definition.CollisionShape)
                        != BlockCollisionShape.Custom)
                        continue;

                    if (definition.CollisionRects == null)
                        continue;

                    for (int i = 0; i < definition.CollisionRects.Count; i++)
                    {
                        BlockCollisionRectDefinition rect = definition.CollisionRects[i];

                        if (rect == null)
                            continue;

                        float rx = Mathf.Clamp01(rect.X);
                        float ry = Mathf.Clamp01(rect.Y);
                        float rw = Mathf.Clamp(rect.Width, 0f, 1f - rx);
                        float rh = Mathf.Clamp(rect.Height, 0f, 1f - ry);

                        if (rw <= epsilon || rh <= epsilon)
                            continue;

                        // Проверяем горизонтальное пересечение игрока с прямоугольником.
                        float rectMinX = x + rx;
                        float rectMaxX = rectMinX + rw;

                        if (horizontalBounds.xMax <= rectMinX + epsilon ||
                            horizontalBounds.xMin >= rectMaxX - epsilon)
                            continue;

                        // Верхняя поверхность именно коллизионного прямоугольника.
                        float top = y + ry + rh;

                        // Игрок должен пересечь эту поверхность сверху вниз.
                        if (top > previousBottom + epsilon ||
                            top < nextBottom - epsilon)
                            continue;

                        if (!found || top > bestTop)
                        {
                            found = true;
                            bestTop = top;
                        }
                    }
                }
            }

            if (found)
                platformTop = bestTop;

            return found;
        }

    }

}

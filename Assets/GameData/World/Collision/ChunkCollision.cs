
using System.Collections.Generic;

using UnityEngine;


namespace Game.World.Collision
{

    public class ChunkCollision
    {

        private readonly WorldCollision worldCollision;


        private readonly Dictionary<
            Vector2Int,
            GameObject
        > collisionObjects =
            new Dictionary<
                Vector2Int,
                GameObject
            >();


        private readonly List<Rect>
            shapeRects =
            new List<Rect>(
                4
            );


        public ChunkCollision(
            WorldCollision worldCollision
        )
        {

            this.worldCollision =
                worldCollision;

        }


        // =====================================================
        // BUILD CHUNK COLLISION
        // =====================================================

        public void BuildChunkCollision(
            Chunk chunk
        )
        {

            if (
                chunk ==
                null
            )
            {

                return;

            }


            Vector2Int chunkPosition =
                new Vector2Int(
                    chunk.X,
                    chunk.Y
                );


            RemoveChunkCollision(
                chunk.X,
                chunk.Y
            );


            GameObject chunkObject =
                new GameObject(
                    "ChunkCollision_" +
                    chunk.X +
                    "_" +
                    chunk.Y
                );


            chunkObject.transform.position =
                new Vector3(
                    chunk.X *
                    Chunk.SizeX,

                    chunk.Y *
                    Chunk.SizeY,

                    0f
                );


            int groundLayer =
                LayerMask.NameToLayer(
                    "Ground"
                );


            if (
                groundLayer !=
                -1
            )
            {

                chunkObject.layer =
                    groundLayer;

            }


            collisionObjects.Add(
                chunkPosition,
                chunkObject
            );


            // =================================================
            // FULL BLOCK MAP
            // =================================================
            //
            // Only Full blocks participate in rectangle merging.
            // Partial shapes are added separately below.
            //
            // =================================================

            bool[,] full =
                new bool[
                    Chunk.SizeX,
                    Chunk.SizeY
                ];


            for (
                int x = 0;
                x < Chunk.SizeX;
                x++
            )
            {

                for (
                    int y = 0;
                    y < Chunk.SizeY;
                    y++
                )
                {

                    ushort blockID =
                        chunk.GetBlock(
                            x,
                            y
                        );


                    full[
                        x,
                        y
                    ] =
                        worldCollision
                            .GetCollisionShape(
                                blockID
                            )
                        ==
                        BlockCollisionShape.Full;

                }

            }


            // =================================================
            // MERGE FULL BLOCKS
            // =================================================

            bool[,] used =
                new bool[
                    Chunk.SizeX,
                    Chunk.SizeY
                ];


            for (
                int y = 0;
                y < Chunk.SizeY;
                y++
            )
            {

                for (
                    int x = 0;
                    x < Chunk.SizeX;
                    x++
                )
                {

                    if (
                        !full[
                            x,
                            y
                        ]
                        ||
                        used[
                            x,
                            y
                        ]
                    )
                    {

                        continue;

                    }


                    int width =
                        0;


                    while (
                        x +
                        width <
                        Chunk.SizeX
                        &&
                        full[
                            x +
                            width,
                            y
                        ]
                        &&
                        !used[
                            x +
                            width,
                            y
                        ]
                    )
                    {

                        width++;

                    }


                    int height =
                        1;


                    bool canExpand =
                        true;


                    while (
                        y +
                        height <
                        Chunk.SizeY
                        &&
                        canExpand
                    )
                    {

                        for (
                            int checkX = 0;
                            checkX < width;
                            checkX++
                        )
                        {

                            if (
                                !full[
                                    x +
                                    checkX,
                                    y +
                                    height
                                ]
                                ||
                                used[
                                    x +
                                    checkX,
                                    y +
                                    height
                                ]
                            )
                            {

                                canExpand =
                                    false;


                                break;

                            }

                        }


                        if (
                            canExpand
                        )
                        {

                            height++;

                        }

                    }


                    for (
                        int markX = 0;
                        markX < width;
                        markX++
                    )
                    {

                        for (
                            int markY = 0;
                            markY < height;
                            markY++
                        )
                        {

                            used[
                                x +
                                markX,
                                y +
                                markY
                            ] =
                                true;

                        }

                    }


                    AddBoxCollider(
                        chunkObject,
                        x,
                        y,
                        width,
                        height
                    );

                }

            }


            // =================================================
            // PARTIAL SHAPES
            // =================================================
            //
            // All colliders live on the single chunk collision
            // GameObject. No GameObject-per-slab/stair spam.
            //
            // =================================================

            for (
                int x = 0;
                x < Chunk.SizeX;
                x++
            )
            {

                for (
                    int y = 0;
                    y < Chunk.SizeY;
                    y++
                )
                {

                    ushort blockID =
                        chunk.GetBlock(
                            x,
                            y
                        );


                    BlockCollisionShape shape =
                        worldCollision
                            .GetCollisionShape(
                                blockID
                            );


                    if (
                        shape ==
                        BlockCollisionShape.None
                        ||
                        shape ==
                        BlockCollisionShape.Full
                    )
                    {

                        continue;

                    }


                    worldCollision
                        .GetLocalCollisionRects(
                            blockID,
                            shapeRects
                        );


                    for (
                        int i = 0;
                        i < shapeRects.Count;
                        i++
                    )
                    {

                        Rect rect =
                            shapeRects[i];


                        AddBoxCollider(
                            chunkObject,

                            x +
                            rect.x,

                            y +
                            rect.y,

                            rect.width,

                            rect.height
                        );

                    }

                }

            }

        }


        // =====================================================
        // ADD BOX COLLIDER
        // =====================================================

        private void AddBoxCollider(
            GameObject parent,
            float x,
            float y,
            float width,
            float height
        )
        {

            if (
                width <=
                0f
                ||
                height <=
                0f
            )
            {

                return;

            }


            BoxCollider2D collider =
                parent.AddComponent<
                    BoxCollider2D
                >();


            collider.offset =
                new Vector2(
                    x +
                    width *
                    0.5f,

                    y +
                    height *
                    0.5f
                );


            collider.size =
                new Vector2(
                    width,
                    height
                );

        }


        // =====================================================
        // REMOVE
        // =====================================================

        public void RemoveChunkCollision(
            int chunkX,
            int chunkY
        )
        {

            Vector2Int position =
                new Vector2Int(
                    chunkX,
                    chunkY
                );


            if (
                !collisionObjects.TryGetValue(
                    position,
                    out GameObject chunkObject
                )
            )
            {

                return;

            }


            Object.Destroy(
                chunkObject
            );


            collisionObjects.Remove(
                position
            );

        }

    }

}

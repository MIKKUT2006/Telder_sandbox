
using Game.Blocks;
using Game.Content;
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
                chunk == null
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
                groundLayer != -1
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
            // SOLID MAP
            // =================================================

            bool[,] solid =
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


                    solid[x, y] =
                        IsSolidBlock(
                            blockID
                        );

                }

            }


            // =================================================
            // FIND RECTANGLES
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
                        !solid[x, y]
                    )
                    {
                        continue;
                    }


                    if (
                        used[x, y]
                    )
                    {
                        continue;
                    }


                    // =============================================
                    // FIND WIDTH
                    // =============================================

                    int width =
                        0;


                    while (
                        x + width <
                        Chunk.SizeX
                        &&
                        solid[
                            x + width,
                            y
                        ]
                        &&
                        !used[
                            x + width,
                            y
                        ]
                    )
                    {

                        width++;

                    }


                    // =============================================
                    // FIND HEIGHT
                    // =============================================

                    int height =
                        1;


                    bool canExpand =
                        true;


                    while (
                        y + height <
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
                                !solid[
                                    x + checkX,
                                    y + height
                                ]
                                ||
                                used[
                                    x + checkX,
                                    y + height
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


                    // =============================================
                    // MARK USED
                    // =============================================

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
                                x + markX,
                                y + markY
                            ] = true;

                        }

                    }


                    // =============================================
                    // CREATE COLLIDER
                    // =============================================

                    CreateCollider(
                        chunkObject,
                        x,
                        y,
                        width,
                        height
                    );

                }

            }

        }


        // =====================================================
        // CREATE COLLIDER
        // =====================================================

        private void CreateCollider(
            GameObject parent,
            int x,
            int y,
            int width,
            int height
        )
        {

            GameObject colliderObject =
                new GameObject(
                    "Collider_" +
                    x +
                    "_" +
                    y
                );


            colliderObject.transform.SetParent(
                parent.transform,
                false
            );


            colliderObject.transform.localPosition =
                new Vector3(
                    x +
                    width *
                    0.5f,

                    y +
                    height *
                    0.5f,

                    0f
                );


            BoxCollider2D collider =
                colliderObject.AddComponent<
                    BoxCollider2D
                >();


            collider.size =
                new Vector2(
                    width,
                    height
                );

        }


        // =====================================================
        // CHECK SOLID
        // =====================================================

        private bool IsSolidBlock(
            ushort blockID
        )
        {

            if (
                blockID == 0
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


            BlockDefinition block =
                BlockRegistry.Get(
                    contentID
                );


            if (
                block == null
            )
            {
                return false;
            }


            return block.Solid;

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


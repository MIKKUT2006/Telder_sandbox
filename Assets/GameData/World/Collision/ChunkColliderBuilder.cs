//using Game.Blocks;
//using Game.Content;
//using System.Collections.Generic;
//using UnityEngine;


//namespace Game.World.Collision
//{

//    public class ChunkColliderBuilder
//    {

//        public void Build(
//            Chunk chunk,
//            GameObject chunkObject
//        )
//        {

//            EdgeCollider2D collider =
//                chunkObject.GetComponent<EdgeCollider2D>();


//            if (
//                collider == null
//            )
//            {

//                collider =
//                    chunkObject.AddComponent<EdgeCollider2D>();

//            }


//            List<Vector2> points =
//                new List<Vector2>();


//            for (
//                int x = 0;
//                x < Chunk.SizeX;
//                x++
//            )
//            {

//                int surfaceY =
//                    FindSurfaceY(
//                        chunk,
//                        x
//                    );


//                if (
//                    surfaceY < 0
//                )
//                {

//                    continue;

//                }


//                points.Add(
//                    new Vector2(
//                        x,
//                        surfaceY
//                    )
//                );

//            }


//            if (
//                points.Count < 2
//            )
//            {

//                collider.enabled =
//                    false;

//                return;

//            }


//            collider.enabled =
//                true;


//            collider.points =
//                points.ToArray();

//        }


//        private int FindSurfaceY(
//            Chunk chunk,
//            int x
//        )
//        {

//            for (
//                int y = Chunk.SizeY - 1;
//                y >= 0;
//                y--
//            )
//            {

//                ushort blockID =
//                    chunk.GetBlock(
//                        x,
//                        y
//                    );


//                if (
//                    blockID == 0
//                )
//                {

//                    continue;

//                }


//                if (
//                    !BlockDatabase.Contains(
//                        blockID
//                    )
//                )
//                {

//                    continue;

//                }


//                BlockDefinition block =
//                    BlockDatabase.Get(
//                        blockID
//                    );


//                if (
//                    block == null
//                )
//                {

//                    continue;

//                }


//                if (
//                    block.Solid
//                )
//                {

//                    return y;

//                }

//            }


//            return -1;

//        }

//    }

//}
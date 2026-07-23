using Game.Resources;
using Game.World.Collision;
using System.Collections.Generic;
using UnityEngine;


namespace Game.World.Rendering
{

    public class ChunkRenderer
    {

        private readonly ChunkColliderBuilder colliderBuilder;
        private readonly Dictionary<Vector2Int, GameObject> chunkObjects =
            new Dictionary<Vector2Int, GameObject>();

        public ChunkRenderer()
        {

            colliderBuilder =
                new ChunkColliderBuilder();

        }
        public void Render(
    Chunk chunk
)
        {

            Vector2Int chunkPosition =
                new Vector2Int(
                    chunk.X,
                    chunk.Y
                );


            // Если чанк уже существует
            if (
                chunkObjects.ContainsKey(
                    chunkPosition
                )
            )
            {

                GameObject existingChunkObject =
                    chunkObjects[
                        chunkPosition
                    ];


                // Обновляем текстуру
                UpdateChunk(
                    chunk,
                    existingChunkObject
                );


                // Обновляем коллизию
                colliderBuilder.Build(
                    chunk,
                    existingChunkObject
                );


                return;

            }


            // Создаём новый объект чанка
            GameObject chunkObject =
                new GameObject(
                    "Chunk_" +
                    chunk.X +
                    "_" +
                    chunk.Y
                );


            // Устанавливаем слой Ground
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


            // Устанавливаем позицию чанка
            chunkObject.transform.position =
                new Vector3(
                    chunk.X *
                    Chunk.SizeX,

                    chunk.Y *
                    Chunk.SizeY,

                    0f
                );


            // Создаём SpriteRenderer
            SpriteRenderer spriteRenderer =
                chunkObject.AddComponent<SpriteRenderer>();


            // Сохраняем объект
            chunkObjects.Add(
                chunkPosition,
                chunkObject
            );


            // Создаём текстуру
            UpdateChunk(
                chunk,
                chunkObject
            );


            // Создаём коллизию
            colliderBuilder.Build(
                chunk,
                chunkObject
            );

        }


        private void UpdateChunk(
            Chunk chunk,
            GameObject chunkObject
        )
        {

            SpriteRenderer renderer =
                chunkObject.GetComponent<SpriteRenderer>();


            Texture2D texture =
                new Texture2D(
                    Chunk.SizeX *
                    BlockRenderer.BlockPixelSize,

                    Chunk.SizeY *
                    BlockRenderer.BlockPixelSize
                );


            texture.filterMode =
                FilterMode.Point;


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


                    BlockRenderer.DrawBlock(
                        texture,
                        x,
                        y,
                        blockID
                    );

                }

            }


            texture.Apply();


            Sprite sprite =
                Sprite.Create(
                    texture,

                    new Rect(
                        0,
                        0,
                        texture.width,
                        texture.height
                    ),

                    new Vector2(
                        0.5f,
                        0.5f
                    ),

                    BlockRenderer.BlockPixelSize
                );


            renderer.sprite =
                sprite;

        }


        public void RemoveChunk(
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
                !chunkObjects.TryGetValue(
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


            chunkObjects.Remove(
                position
            );

        }


        public void Clear()
        {

            foreach (
                GameObject chunkObject
                in chunkObjects.Values
            )
            {

                if (
                    chunkObject != null
                )
                {

                    Object.Destroy(
                        chunkObject
                    );

                }

            }


            chunkObjects.Clear();

        }

    }

}
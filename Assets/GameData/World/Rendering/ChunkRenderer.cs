using System.Collections.Generic;
using UnityEngine;


namespace Game.World.Rendering
{

    public class ChunkRenderer
    {

        // =====================================================
        // CHUNK RENDER DATA
        // =====================================================

        private class ChunkRenderData
        {

            public GameObject GameObject;

            public SpriteRenderer Renderer;

            public Texture2D Texture;

            public Sprite Sprite;

        }


        // =====================================================
        // LOADED CHUNKS
        // =====================================================

        private readonly Dictionary<
            Vector2Int,
            ChunkRenderData
        >
        chunkObjects =
            new Dictionary<
                Vector2Int,
                ChunkRenderData
            >();


        // =====================================================
        // RENDER
        // =====================================================

        public void Render(
            Chunk chunk
        )
        {

            if (
                chunk == null
            )
            {
                return;
            }


            Vector2Int position =
                new Vector2Int(
                    chunk.X,
                    chunk.Y
                );


            // =================================================
            // ◊¿Õ  ”∆≈ —”Ÿ≈—“¬”≈“
            // =================================================

            if (
                chunkObjects.TryGetValue(
                    position,
                    out ChunkRenderData existingData
                )
            )
            {

                UpdateChunk(
                    chunk,
                    existingData
                );


                return;

            }


            // =================================================
            // —Œ«ƒ¿®Ã GAMEOBJECT
            // =================================================

            GameObject chunkObject =
                new GameObject(
                    "Chunk_" +
                    chunk.X +
                    "_" +
                    chunk.Y
                );


            // =================================================
            // LAYER
            // =================================================

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


            // =================================================
            // POSITION
            // =================================================

            chunkObject.transform.position =
                new Vector3(
                    chunk.X *
                    Chunk.SizeX,

                    chunk.Y *
                    Chunk.SizeY,

                    0f
                );


            // =================================================
            // SPRITE RENDERER
            // =================================================

            SpriteRenderer spriteRenderer =
                chunkObject.AddComponent<
                    SpriteRenderer
                >();


            // =================================================
            // TEXTURE SIZE
            // =================================================

            int textureWidth =
                Chunk.SizeX *
                BlockRenderer.BlockPixelSize;


            int textureHeight =
                Chunk.SizeY *
                BlockRenderer.BlockPixelSize;


            // =================================================
            // CREATE TEXTURE
            // =================================================

            Texture2D texture =
                new Texture2D(
                    textureWidth,
                    textureHeight,
                    TextureFormat.RGBA32,
                    false
                );


            texture.filterMode =
                FilterMode.Point;


            texture.wrapMode =
                TextureWrapMode.Clamp;


            // =================================================
            // CREATE SPRITE
            // =================================================

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
                        0f,
                        0f
                    ),

                    BlockRenderer.BlockPixelSize
                );


            spriteRenderer.sprite =
                sprite;


            // =================================================
            // SAVE DATA
            // =================================================

            ChunkRenderData renderData =
                new ChunkRenderData
                {

                    GameObject =
                        chunkObject,

                    Renderer =
                        spriteRenderer,

                    Texture =
                        texture,

                    Sprite =
                        sprite

                };


            chunkObjects.Add(
                position,
                renderData
            );


            // =================================================
            // DRAW
            // =================================================

            UpdateChunk(
                chunk,
                renderData
            );

        }


        // =====================================================
        // UPDATE CHUNK
        // =====================================================

        private void UpdateChunk(
            Chunk chunk,
            ChunkRenderData renderData
        )
        {

            if (
                chunk == null ||
                renderData == null ||
                renderData.Texture == null
            )
            {
                return;
            }


            Texture2D texture =
                renderData.Texture;


            // =================================================
            // DRAW BLOCKS
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


                    BlockRenderer.DrawBlock(
                        texture,
                        x,
                        y,
                        blockID
                    );

                }

            }


            // =================================================
            // APPLY
            // =================================================

            texture.Apply(
                false,
                false
            );

        }


        // =====================================================
        // REMOVE CHUNK
        // =====================================================

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
                    out ChunkRenderData renderData
                )
            )
            {

                return;

            }


            // =================================================
            // DESTROY SPRITE
            // =================================================

            if (
                renderData.Sprite != null
            )
            {

                Object.Destroy(
                    renderData.Sprite
                );

            }


            // =================================================
            // DESTROY TEXTURE
            // =================================================

            if (
                renderData.Texture != null
            )
            {

                Object.Destroy(
                    renderData.Texture
                );

            }


            // =================================================
            // DESTROY GAMEOBJECT
            // =================================================

            if (
                renderData.GameObject != null
            )
            {

                Object.Destroy(
                    renderData.GameObject
                );

            }


            // =================================================
            // REMOVE DICTIONARY
            // =================================================

            chunkObjects.Remove(
                position
            );

        }


        // =====================================================
        // CLEAR
        // =====================================================

        public void Clear()
        {

            foreach (
                ChunkRenderData renderData
                in chunkObjects.Values
            )
            {

                if (
                    renderData == null
                )
                {
                    continue;
                }


                if (
                    renderData.Sprite != null
                )
                {

                    Object.Destroy(
                        renderData.Sprite
                    );

                }


                if (
                    renderData.Texture != null
                )
                {

                    Object.Destroy(
                        renderData.Texture
                    );

                }


                if (
                    renderData.GameObject != null
                )
                {

                    Object.Destroy(
                        renderData.GameObject
                    );

                }

            }


            chunkObjects.Clear();

        }

    }

}
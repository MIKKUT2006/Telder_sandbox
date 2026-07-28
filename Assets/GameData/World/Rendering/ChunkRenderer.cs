using System.Collections.Generic;
using UnityEngine;

using Game.World;

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


            public GameObject BackgroundObject;

            public GameObject ForegroundObject;


            public SpriteRenderer BackgroundRenderer;

            public SpriteRenderer ForegroundRenderer;


            public Texture2D BackgroundTexture;

            public Texture2D ForegroundTexture;


            public Sprite BackgroundSprite;

            public Sprite ForegroundSprite;

        }


        // =====================================================
        // LOADED CHUNKS
        // =====================================================

        private readonly Dictionary<
            UnityEngine.Vector2Int,
            ChunkRenderData
        >
        chunkObjects =
            new Dictionary<
                UnityEngine.Vector2Int,
                ChunkRenderData
            >();


        // =====================================================
        // RENDER CHUNK
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


            UnityEngine.Vector2Int position =
                new UnityEngine.Vector2Int(
                    chunk.X,
                    chunk.Y
                );


            // =================================================
            // EXISTING
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
            // ROOT OBJECT
            // =================================================

            GameObject chunkObject =
                new GameObject(
                    "Chunk_" +
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
            // BACKGROUND OBJECT
            // =================================================

            GameObject backgroundObject =
                new GameObject(
                    "Background"
                );


            backgroundObject.transform.SetParent(
                chunkObject.transform
            );


            backgroundObject.transform.localPosition =
                Vector3.zero;


            SpriteRenderer backgroundRenderer =
                backgroundObject.AddComponent<
                    SpriteRenderer
                >();


            // =================================================
            // BACKGROUND COLOR
            // =================================================
            //
            // Затемняем только задний слой.
            //
            // 1.0 = оригинальная яркость
            // 0.5 = 50% яркости
            // 0.25 = 25% яркости
            //
            // Альфа оставляем 1, чтобы фон
            // не становился прозрачным.
            //

            backgroundRenderer.color =
                new Color(
                    0.5f,
                    0.5f,
                    0.5f,
                    1f
                );


            // =================================================
            // FOREGROUND OBJECT
            // =================================================

            GameObject foregroundObject =
                new GameObject(
                    "Foreground"
                );


            foregroundObject.transform.SetParent(
                chunkObject.transform
            );


            foregroundObject.transform.localPosition =
                Vector3.zero;


            SpriteRenderer foregroundRenderer =
                foregroundObject.AddComponent<
                    SpriteRenderer
                >();


            // =================================================
            // FOREGROUND COLOR
            // =================================================
            //
            // Передний слой остаётся полностью оригинальным.
            //

            foregroundRenderer.color =
                Color.white;


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
            // BACKGROUND TEXTURE
            // =================================================

            Texture2D backgroundTexture =
                CreateTexture(
                    textureWidth,
                    textureHeight
                );


            // =================================================
            // FOREGROUND TEXTURE
            // =================================================

            Texture2D foregroundTexture =
                CreateTexture(
                    textureWidth,
                    textureHeight
                );


            // =================================================
            // BACKGROUND SPRITE
            // =================================================

            Sprite backgroundSprite =
                CreateSprite(
                    backgroundTexture
                );


            // =================================================
            // FOREGROUND SPRITE
            // =================================================

            Sprite foregroundSprite =
                CreateSprite(
                    foregroundTexture
                );


            backgroundRenderer.sprite =
                backgroundSprite;


            foregroundRenderer.sprite =
                foregroundSprite;


            // =================================================
            // SORTING
            // =================================================

            backgroundRenderer.sortingOrder =
                0;


            foregroundRenderer.sortingOrder =
                1;


            // =================================================
            // SAVE DATA
            // =================================================

            ChunkRenderData renderData =
                new ChunkRenderData
                {

                    GameObject =
                        chunkObject,

                    BackgroundObject =
                        backgroundObject,

                    ForegroundObject =
                        foregroundObject,

                    BackgroundRenderer =
                        backgroundRenderer,

                    ForegroundRenderer =
                        foregroundRenderer,

                    BackgroundTexture =
                        backgroundTexture,

                    ForegroundTexture =
                        foregroundTexture,

                    BackgroundSprite =
                        backgroundSprite,

                    ForegroundSprite =
                        foregroundSprite

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
        // CREATE TEXTURE
        // =====================================================

        private Texture2D CreateTexture(
            int width,
            int height
        )
        {

            Texture2D texture =
                new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false
                );


            texture.filterMode =
                FilterMode.Point;


            texture.wrapMode =
                TextureWrapMode.Clamp;


            // =================================================
            // CLEAR TEXTURE
            // =================================================

            Color[] clearPixels =
                new Color[
                    width *
                    height
                ];


            for (
                int i = 0;
                i < clearPixels.Length;
                i++
            )
            {

                clearPixels[i] =
                    Color.clear;

            }


            texture.SetPixels(
                clearPixels
            );


            texture.Apply(
                false,
                false
            );


            return texture;

        }


        // =====================================================
        // CREATE SPRITE
        // =====================================================

        private Sprite CreateSprite(
            Texture2D texture
        )
        {

            return
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
                renderData == null
            )
            {
                return;
            }


            // =================================================
            // CLEAR BOTH LAYERS
            // =================================================

            ClearTexture(
                renderData.BackgroundTexture
            );


            ClearTexture(
                renderData.ForegroundTexture
            );


            // =================================================
            // DRAW
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

                    // =========================================
                    // BACKGROUND
                    // =========================================

                    ushort backgroundID =
                        chunk.GetBackground(
                            x,
                            y
                        );


                    BlockRenderer.DrawBlock(
                        renderData.BackgroundTexture,
                        x,
                        y,
                        backgroundID
                    );


                    // =========================================
                    // FOREGROUND
                    // =========================================

                    ushort foregroundID =
                        chunk.GetBlock(
                            x,
                            y
                        );


                    BlockRenderer.DrawBlock(
                        renderData.ForegroundTexture,
                        x,
                        y,
                        foregroundID
                    );

                }

            }


            // =================================================
            // APPLY
            // =================================================

            renderData.BackgroundTexture.Apply(
                false,
                false
            );


            renderData.ForegroundTexture.Apply(
                false,
                false
            );

        }


        // =====================================================
        // CLEAR TEXTURE
        // =====================================================

        private void ClearTexture(
            Texture2D texture
        )
        {

            if (
                texture == null
            )
            {
                return;
            }


            Color[] pixels =
                new Color[
                    texture.width *
                    texture.height
                ];


            for (
                int i = 0;
                i < pixels.Length;
                i++
            )
            {

                pixels[i] =
                    Color.clear;

            }


            texture.SetPixels(
                pixels
            );

        }


        // =====================================================
        // UPDATE BLOCK
        // =====================================================

        public void UpdateBlock(
            Chunk chunk,
            int localX,
            int localY
        )
        {

            if (
                chunk == null
            )
            {
                return;
            }


            UnityEngine.Vector2Int position =
                new UnityEngine.Vector2Int(
                    chunk.X,
                    chunk.Y
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
            // ПЕРЕРИСОВЫВАЕМ ЧАНК
            // =================================================

            UpdateChunk(
                chunk,
                renderData
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

            UnityEngine.Vector2Int position =
                new UnityEngine.Vector2Int(
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


            DestroyRenderData(
                renderData
            );


            chunkObjects.Remove(
                position
            );

        }


        // =====================================================
        // DESTROY DATA
        // =====================================================

        private void DestroyRenderData(
            ChunkRenderData renderData
        )
        {

            if (
                renderData == null
            )
            {
                return;
            }


            if (
                renderData.BackgroundSprite != null
            )
            {

                Object.Destroy(
                    renderData.BackgroundSprite
                );

            }


            if (
                renderData.ForegroundSprite != null
            )
            {

                Object.Destroy(
                    renderData.ForegroundSprite
                );

            }


            if (
                renderData.BackgroundTexture != null
            )
            {

                Object.Destroy(
                    renderData.BackgroundTexture
                );

            }


            if (
                renderData.ForegroundTexture != null
            )
            {

                Object.Destroy(
                    renderData.ForegroundTexture
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

                DestroyRenderData(
                    renderData
                );

            }


            chunkObjects.Clear();

        }

    }

}
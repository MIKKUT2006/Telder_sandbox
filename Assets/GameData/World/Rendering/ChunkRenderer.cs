using System.Collections.Generic;

using UnityEngine;

using Game.World.Lighting;

namespace Game.World.Rendering
{
    public class ChunkRenderer
    {
        private readonly Dictionary<
            UnityEngine.Vector2Int,
            ChunkRenderData
        > renderedChunks =
            new Dictionary<
                UnityEngine.Vector2Int,
                ChunkRenderData
            >();


        private Game.World.World world;

        private Material lightingMaterial;


        // =========================================================
        // LIGHT TEXTURE
        // =========================================================

        private const int LightBorder = 1;

        private const int LightTextureWidth =
            Chunk.SizeX +
            LightBorder * 2;

        private const int LightTextureHeight =
            Chunk.SizeY +
            LightBorder * 2;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ChunkRenderer()
        {
            CreateMaterial();
        }


        public ChunkRenderer(
            Game.World.World world
        )
        {
            this.world =
                world;

            CreateMaterial();
        }


        public void SetWorld(
            Game.World.World world
        )
        {
            this.world =
                world;
        }


        // =========================================================
        // MATERIAL
        // =========================================================

        private void CreateMaterial()
        {
            Shader shader =
                Shader.Find(
                    "Game/ChunkLitSprite"
                );


            if (shader == null)
            {
                Debug.LogError(
                    "Shader Game/ChunkLitSprite " +
                    "not found."
                );

                return;
            }


            lightingMaterial =
                new Material(
                    shader
                );


            lightingMaterial.name =
                "Runtime Chunk Lighting";
        }


        // =========================================================
        // RENDER WHOLE CHUNK
        // =========================================================

        public void Render(
            Chunk chunk
        )
        {
            if (chunk == null)
            {
                return;
            }


            UnityEngine.Vector2Int key =
                new UnityEngine.Vector2Int(
                    chunk.X,
                    chunk.Y
                );


            if (
                !renderedChunks.TryGetValue(
                    key,
                    out ChunkRenderData data
                )
            )
            {
                data =
                    CreateChunkRenderData(
                        chunk
                    );


                renderedChunks.Add(
                    key,
                    data
                );
            }


            DrawBlocks(
                chunk,
                data
            );


            UpdateLightTexture(
                chunk,
                data
            );
        }


        // =========================================================
        // CREATE CHUNK OBJECT
        // =========================================================

        private ChunkRenderData
            CreateChunkRenderData(
                Chunk chunk
            )
        {
            ChunkRenderData data =
                new ChunkRenderData();


            // =====================================================
            // ROOT
            // =====================================================

            GameObject root =
                new GameObject(
                    "Chunk_" +
                    chunk.X +
                    "_" +
                    chunk.Y
                );


            root.transform.position =
                new Vector3(
                    chunk.X *
                    Chunk.SizeX,

                    chunk.Y *
                    Chunk.SizeY,

                    0f
                );


            data.Root =
                root;


            // =====================================================
            // BACKGROUND OBJECT
            // =====================================================

            GameObject backgroundObject =
                new GameObject(
                    "Background"
                );


            backgroundObject.transform.SetParent(
                root.transform,
                false
            );


            SpriteRenderer backgroundRenderer =
                backgroundObject.AddComponent<
                    SpriteRenderer
                >();


            backgroundRenderer.sortingOrder =
                0;


            data.BackgroundRenderer =
                backgroundRenderer;


            // =====================================================
            // FOREGROUND OBJECT
            // =====================================================

            GameObject foregroundObject =
                new GameObject(
                    "Foreground"
                );


            foregroundObject.transform.SetParent(
                root.transform,
                false
            );


            SpriteRenderer foregroundRenderer =
                foregroundObject.AddComponent<
                    SpriteRenderer
                >();


            foregroundRenderer.sortingOrder =
                1;


            data.ForegroundRenderer =
                foregroundRenderer;


            // =====================================================
            // TILE TEXTURES
            // =====================================================

            int textureWidth =
                Chunk.SizeX *
                BlockRenderer.BlockPixelSize;


            int textureHeight =
                Chunk.SizeY *
                BlockRenderer.BlockPixelSize;


            data.BackgroundTexture =
                CreateBlockTexture(
                    textureWidth,
                    textureHeight
                );


            data.ForegroundTexture =
                CreateBlockTexture(
                    textureWidth,
                    textureHeight
                );


            data.BackgroundRenderer.sprite =
                CreateSprite(
                    data.BackgroundTexture
                );


            data.ForegroundRenderer.sprite =
                CreateSprite(
                    data.ForegroundTexture
                );


            // =====================================================
            // LIGHT TEXTURE
            // =====================================================

            data.LightTexture =
                new Texture2D(
                    LightTextureWidth,
                    LightTextureHeight,
                    TextureFormat.RGB24,
                    false
                );


            data.LightTexture.name =
                "Light_" +
                chunk.X +
                "_" +
                chunk.Y;


            data.LightTexture.filterMode =
                FilterMode.Bilinear;


            data.LightTexture.wrapMode =
                TextureWrapMode.Clamp;


            data.LightPixels =
                new Color32[
                    LightTextureWidth *
                    LightTextureHeight
                ];


            // =====================================================
            // MATERIAL
            // =====================================================

            if (lightingMaterial != null)
            {
                data.BackgroundRenderer.sharedMaterial =
                    lightingMaterial;


                data.ForegroundRenderer.sharedMaterial =
                    lightingMaterial;
            }


            // =====================================================
            // PROPERTY BLOCKS
            // =====================================================

            data.BackgroundProperties =
                new MaterialPropertyBlock();


            data.ForegroundProperties =
                new MaterialPropertyBlock();


            ApplyMaterialProperties(
                data
            );


            return data;
        }


        // =========================================================
        // CREATE BLOCK TEXTURE
        // =========================================================

        private Texture2D CreateBlockTexture(
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


            return texture;
        }


        // =========================================================
        // CREATE SPRITE
        // =========================================================

        private Sprite CreateSprite(
            Texture2D texture
        )
        {
            return Sprite.Create(
                texture,

                new Rect(
                    0,
                    0,
                    texture.width,
                    texture.height
                ),

                Vector2.zero,

                BlockRenderer.BlockPixelSize
            );
        }


        // =========================================================
        // DRAW ALL BLOCKS
        // =========================================================

        private void DrawBlocks(
            Chunk chunk,
            ChunkRenderData data
        )
        {
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
                    // =================================================
                    // BACKGROUND
                    // =================================================

                    DrawSingleBlock(
                        data.BackgroundTexture,
                        x,
                        y,
                        chunk.GetBackground(
                            x,
                            y
                        )
                    );


                    // =================================================
                    // FOREGROUND
                    // =================================================

                    DrawSingleBlock(
                        data.ForegroundTexture,
                        x,
                        y,
                        chunk.GetBlock(
                            x,
                            y
                        )
                    );
                }
            }


            data.BackgroundTexture.Apply(
                false,
                false
            );


            data.ForegroundTexture.Apply(
                false,
                false
            );
        }


        // =========================================================
        // DRAW SINGLE BLOCK
        // =========================================================
        //
        // Сначала очищаем клетку.
        //
        // Это КРИТИЧНО для удаления блока ID 0:
        //
        // старые пиксели не могут остаться в Texture2D.
        //

        private void DrawSingleBlock(
            Texture2D texture,
            int localX,
            int localY,
            ushort blockID
        )
        {
            ClearBlockArea(
                texture,
                localX,
                localY
            );


            // =====================================================
            // AIR
            // =====================================================

            if (blockID == 0)
            {
                return;
            }


            // =====================================================
            // DRAW BLOCK
            // =====================================================

            BlockRenderer.DrawBlock(
                texture,
                localX,
                localY,
                blockID
            );
        }


        // =========================================================
        // CLEAR ONE BLOCK AREA
        // =========================================================

        private void ClearBlockArea(
            Texture2D texture,
            int localX,
            int localY
        )
        {
            int pixelSize =
                BlockRenderer.BlockPixelSize;


            int pixelX =
                localX *
                pixelSize;


            int pixelY =
                localY *
                pixelSize;


            Color32[] clearPixels =
                new Color32[
                    pixelSize *
                    pixelSize
                ];


            // Все значения по умолчанию:
            //
            // R = 0
            // G = 0
            // B = 0
            // A = 0
            //
            // То есть полностью прозрачный пиксель.

            texture.SetPixels32(
                pixelX,
                pixelY,
                pixelSize,
                pixelSize,
                clearPixels
            );
        }


        // =========================================================
        // UPDATE FOREGROUND BLOCK
        // =========================================================

        public void UpdateBlock(
            Chunk chunk,
            int localX,
            int localY
        )
        {
            if (chunk == null)
            {
                return;
            }


            if (
                localX < 0 ||
                localX >= Chunk.SizeX ||
                localY < 0 ||
                localY >= Chunk.SizeY
            )
            {
                return;
            }


            UnityEngine.Vector2Int key =
                new UnityEngine.Vector2Int(
                    chunk.X,
                    chunk.Y
                );


            if (
                !renderedChunks.TryGetValue(
                    key,
                    out ChunkRenderData data
                )
            )
            {
                Render(chunk);
                return;
            }


            // =====================================================
            // DRAW ONLY THIS CELL
            // =====================================================

            DrawSingleBlock(
                data.ForegroundTexture,
                localX,
                localY,
                chunk.GetBlock(
                    localX,
                    localY
                )
            );


            // =====================================================
            // APPLY IMMEDIATELY
            // =====================================================

            data.ForegroundTexture.Apply(
                false,
                false
            );
        }


        // =========================================================
        // UPDATE BACKGROUND BLOCK
        // =========================================================

        public void UpdateBackgroundBlock(
            Chunk chunk,
            int localX,
            int localY
        )
        {
            if (chunk == null)
            {
                return;
            }


            if (
                localX < 0 ||
                localX >= Chunk.SizeX ||
                localY < 0 ||
                localY >= Chunk.SizeY
            )
            {
                return;
            }


            UnityEngine.Vector2Int key =
                new UnityEngine.Vector2Int(
                    chunk.X,
                    chunk.Y
                );


            if (
                !renderedChunks.TryGetValue(
                    key,
                    out ChunkRenderData data
                )
            )
            {
                Render(chunk);
                return;
            }


            // =====================================================
            // DRAW ONLY THIS BACKGROUND CELL
            // =====================================================

            DrawSingleBlock(
                data.BackgroundTexture,
                localX,
                localY,
                chunk.GetBackground(
                    localX,
                    localY
                )
            );


            // =====================================================
            // APPLY IMMEDIATELY
            // =====================================================

            data.BackgroundTexture.Apply(
                false,
                false
            );
        }


        // =========================================================
        // LIGHT UPDATES
        // =========================================================

        public void UpdateDirtyLighting(
            int maxChunksPerFrame = 6
        )
        {
            if (world == null)
            {
                return;
            }


            int updated = 0;


            foreach (
                var pair
                in world.GetLoadedChunks()
            )
            {
                if (
                    updated >=
                    maxChunksPerFrame
                )
                {
                    break;
                }


                Chunk chunk =
                    pair.Value;


                if (chunk == null)
                {
                    continue;
                }


                ChunkLightData lightData =
                    chunk.GetLightData();


                if (
                    lightData == null ||
                    !lightData.IsDirty
                )
                {
                    continue;
                }


                UnityEngine.Vector2Int key =
                    new UnityEngine.Vector2Int(
                        chunk.X,
                        chunk.Y
                    );


                if (
                    !renderedChunks.TryGetValue(
                        key,
                        out ChunkRenderData data
                    )
                )
                {
                    continue;
                }


                UpdateLightTexture(
                    chunk,
                    data
                );


                updated++;
            }
        }


        // =========================================================
        // UPDATE LIGHT TEXTURE
        // =========================================================

        private void UpdateLightTexture(
            Chunk chunk,
            ChunkRenderData data
        )
        {
            int startWorldX =
                chunk.X *
                Chunk.SizeX;


            int startWorldY =
                chunk.Y *
                Chunk.SizeY;


            int index = 0;


            for (
                int lightY = -LightBorder;
                lightY <
                Chunk.SizeY + LightBorder;
                lightY++
            )
            {
                for (
                    int lightX = -LightBorder;
                    lightX <
                    Chunk.SizeX + LightBorder;
                    lightX++
                )
                {
                    int worldX =
                        startWorldX +
                        lightX;


                    int worldY =
                        startWorldY +
                        lightY;


                    LightNode node =
                        GetWorldLight(
                            chunk,
                            lightX,
                            lightY,
                            worldX,
                            worldY
                        );


                    float sunlight =
                        node.Sun /
                        15f;


                    float red =
                        Mathf.Max(
                            sunlight,
                            node.R / 15f
                        );


                    float green =
                        Mathf.Max(
                            sunlight,
                            node.G / 15f
                        );


                    float blue =
                        Mathf.Max(
                            sunlight,
                            node.B / 15f
                        );


                    data.LightPixels[index++] =
                        new Color(
                            red,
                            green,
                            blue,
                            1f
                        );
                }
            }


            data.LightTexture.SetPixels32(
                data.LightPixels
            );


            data.LightTexture.Apply(
                false,
                false
            );


            chunk
                .GetLightData()
                .MarkClean();
        }


        // =========================================================
        // GET WORLD LIGHT
        // =========================================================

        private LightNode GetWorldLight(
            Chunk chunk,
            int localX,
            int localY,
            int worldX,
            int worldY
        )
        {
            if (world != null)
            {
                return world.GetLight(
                    worldX,
                    worldY
                );
            }


            if (
                localX >= 0 &&
                localX < Chunk.SizeX &&
                localY >= 0 &&
                localY < Chunk.SizeY
            )
            {
                return chunk
                    .GetLightData()
                    .Get(
                        localX,
                        localY
                    );
            }


            return LightNode.None;
        }


        // =========================================================
        // SHADER PROPERTIES
        // =========================================================

        private void ApplyMaterialProperties(
            ChunkRenderData data
        )
        {
            float scaleX =
                (float)Chunk.SizeX /
                LightTextureWidth;


            float scaleY =
                (float)Chunk.SizeY /
                LightTextureHeight;


            float offsetX =
                (float)LightBorder /
                LightTextureWidth;


            float offsetY =
                (float)LightBorder /
                LightTextureHeight;


            Vector4 lightUV =
                new Vector4(
                    scaleX,
                    scaleY,
                    offsetX,
                    offsetY
                );


            // =====================================================
            // FOREGROUND
            // =====================================================

            data.ForegroundProperties.SetTexture(
                "_LightTex",
                data.LightTexture
            );


            data.ForegroundProperties.SetVector(
                "_LightUVScaleOffset",
                lightUV
            );


            data.ForegroundProperties.SetFloat(
                "_LayerBrightness",
                1f
            );


            data.ForegroundProperties.SetFloat(
                "_Ambient",
                0.055f
            );


            data.ForegroundProperties.SetFloat(
                "_LightGamma",
                0.72f
            );


            data.ForegroundRenderer.SetPropertyBlock(
                data.ForegroundProperties
            );


            // =====================================================
            // BACKGROUND
            // =====================================================

            data.BackgroundProperties.SetTexture(
                "_LightTex",
                data.LightTexture
            );


            data.BackgroundProperties.SetVector(
                "_LightUVScaleOffset",
                lightUV
            );


            data.BackgroundProperties.SetFloat(
                "_LayerBrightness",
                0.68f
            );


            data.BackgroundProperties.SetFloat(
                "_Ambient",
                0.045f
            );


            data.BackgroundProperties.SetFloat(
                "_LightGamma",
                0.72f
            );


            data.BackgroundRenderer.SetPropertyBlock(
                data.BackgroundProperties
            );
        }


        // =========================================================
        // RENDER ALL LOADED
        // =========================================================

        public void RenderAllLoaded(
            Game.World.World world
        )
        {
            if (world == null)
            {
                return;
            }


            foreach (
                var pair
                in world.GetLoadedChunks()
            )
            {
                Chunk chunk =
                    pair.Value;


                if (chunk == null)
                {
                    continue;
                }


                Render(chunk);
            }
        }


        // =========================================================
        // REMOVE CHUNK
        // =========================================================

        public void RemoveChunk(
            int chunkX,
            int chunkY
        )
        {
            UnityEngine.Vector2Int key =
                new UnityEngine.Vector2Int(
                    chunkX,
                    chunkY
                );


            if (
                !renderedChunks.TryGetValue(
                    key,
                    out ChunkRenderData data
                )
            )
            {
                return;
            }


            renderedChunks.Remove(
                key
            );


            if (data.Root != null)
            {
                Object.Destroy(
                    data.Root
                );
            }


            if (data.ForegroundTexture != null)
            {
                Object.Destroy(
                    data.ForegroundTexture
                );
            }


            if (data.BackgroundTexture != null)
            {
                Object.Destroy(
                    data.BackgroundTexture
                );
            }


            if (data.LightTexture != null)
            {
                Object.Destroy(
                    data.LightTexture
                );
            }
        }


        // =========================================================
        // CLEAR
        // =========================================================

        public void Clear()
        {
            foreach (
                var pair
                in renderedChunks
            )
            {
                ChunkRenderData data =
                    pair.Value;


                if (data.Root != null)
                {
                    Object.Destroy(
                        data.Root
                    );
                }


                if (data.ForegroundTexture != null)
                {
                    Object.Destroy(
                        data.ForegroundTexture
                    );
                }


                if (data.BackgroundTexture != null)
                {
                    Object.Destroy(
                        data.BackgroundTexture
                    );
                }


                if (data.LightTexture != null)
                {
                    Object.Destroy(
                        data.LightTexture
                    );
                }
            }


            renderedChunks.Clear();
        }


        // =========================================================
        // RENDER DATA
        // =========================================================

        private sealed class ChunkRenderData
        {
            public GameObject Root;


            public SpriteRenderer
                ForegroundRenderer;


            public SpriteRenderer
                BackgroundRenderer;


            public Texture2D
                ForegroundTexture;


            public Texture2D
                BackgroundTexture;


            public Texture2D
                LightTexture;


            public Color32[]
                LightPixels;


            public MaterialPropertyBlock
                ForegroundProperties;


            public MaterialPropertyBlock
                BackgroundProperties;
        }
    }
}
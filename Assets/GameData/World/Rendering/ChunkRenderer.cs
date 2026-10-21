using Game.InputSupport;
using System.Collections.Generic;

using UnityEngine;

using Game.World.Lighting;
using Game.Content;
using Game.Blocks;

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


        // Assignable by ChunkLoader. Off-camera chunks keep CPU data but do not
        // allocate expensive sprite textures until they enter the viewport.
        public System.Func<int, int, bool> RenderEligibility;

        public bool HasChunkVisual(int x, int y)
        {
            return renderedChunks.TryGetValue(new UnityEngine.Vector2Int(x,y),out var data)&&data.Ready;
        }

        public bool IsChunkVisualEnabled(int x, int y)
        {
            return renderedChunks.TryGetValue(new UnityEngine.Vector2Int(x, y),
                out ChunkRenderData data) && data.Ready && data.Root != null && data.Root.activeSelf;
        }

        public void SetChunkVisible(int x, int y, bool visible)
        {
            if (renderedChunks.TryGetValue(new UnityEngine.Vector2Int(x, y),
                out ChunkRenderData data) && data.Root != null &&
                data.Ready && data.Root.activeSelf != visible)
                data.Root.SetActive(visible);
        }

        private readonly Queue<UnityEngine.Vector2Int> buildQueue=new Queue<UnityEngine.Vector2Int>();
        private readonly Dictionary<UnityEngine.Vector2Int,int> buildRows=new Dictionary<UnityEngine.Vector2Int,int>();
        private readonly Queue<UnityEngine.Vector2Int> uploadQueue=new Queue<UnityEngine.Vector2Int>();
        private readonly HashSet<UnityEngine.Vector2Int> pendingUploads=new HashSet<UnityEngine.Vector2Int>();
        private readonly System.Diagnostics.Stopwatch buildClock=new System.Diagnostics.Stopwatch();

        private Game.World.World world;

        private Material lightingMaterial;
        private Material windMaterial;

        private readonly Dictionary<ushort, bool> waveTagCache =
            new Dictionary<ushort, bool>();

        private readonly List<int> dynamicCellScratch =
            new List<int>(128);

        private readonly List<int> dynamicRemoveScratch =
            new List<int>(32);

        private float nextDynamicVisualCheck;

        private int lightingDetail = 2;
        public void SetLightingDetail(int value)
        {
            lightingDetail = Mathf.Clamp(value, 0, 2);
            foreach (var pair in renderedChunks)
            {
                if (pair.Value.LightTexture != null)
                    pair.Value.LightTexture.filterMode = lightingDetail == 0 ? FilterMode.Point : FilterMode.Bilinear;
            }
        }



        // =========================================================
        // VISUAL LAYERS
        // =========================================================
        //
        // Background is ALWAYS 30% darker than foreground.
        //
        // IMPORTANT:
        // This is SpriteRenderer tint, not light level.
        // Therefore a torch can brighten the background, but even
        // under maximum light its albedo remains visibly darker.
        //
        // 1.00 = normal foreground
        // 0.70 = 30% darker background
        //
        private const float BackgroundSpriteBrightness =
            0.70f;


        private const int LightBorder =
            1;


        private const int LightTextureWidth =
            Chunk.SizeX +
            LightBorder *
            2;


        private const int LightTextureHeight =
            Chunk.SizeY +
            LightBorder *
            2;


        public ChunkRenderer()
        {
            lightingDetail = GameInput.Settings.LightingQuality;
            CreateMaterial();
        }


        public ChunkRenderer(
            Game.World.World world
        )
        {
            this.world =
                world;


            lightingDetail = GameInput.Settings.LightingQuality;
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
                ChunkLightingShaderProvider.Get();


            if (
                shader ==
                null
            )
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

            Shader windShader = UnityEngine.Resources.Load<Shader>(
                "Shaders/PixelatedWaveLitSprite");
            if (windShader == null)
                windShader = Shader.Find("Game/PixelatedWaveLitSprite");

            if (windShader != null)
            {
                windMaterial = new Material(windShader);
                windMaterial.name = "Runtime Pixelated Wave Wind";
                windMaterial.SetFloat("_WindStrength", 2.0f);
                windMaterial.SetFloat("_WindSpeed", 1.0f);
                windMaterial.SetFloat("_WindFrequency", 1.1f);
                windMaterial.SetFloat("_BlockPixelSize", BlockRenderer.BlockPixelSize);
            }
            else
            {
                Debug.LogError(
                    "Pixelated wind shader not found. Expected Resources/Shaders/PixelatedWaveLitSprite. " +
                    "Blocks tagged 'wave' will remain static.");
            }
        }


        // =========================================================
        // RENDER WHOLE CHUNK
        // =========================================================

        public void QueueRender(Chunk chunk,bool refresh=false)
        {
            if(chunk==null||RenderEligibility!=null&&!RenderEligibility(chunk.X,chunk.Y))return;
            var key=new UnityEngine.Vector2Int(chunk.X,chunk.Y);
            if(renderedChunks.TryGetValue(key,out var existing)&&existing.Ready&&!refresh)return;
            if(buildRows.ContainsKey(key))return;
            buildRows[key]=-1;buildQueue.Enqueue(key);
        }
        public void ProcessQueuedRenders(double budgetMilliseconds=1.0)
        {
            buildClock.Restart();int attempts=buildQueue.Count;
            while(attempts-->0&&buildQueue.Count>0&&buildClock.Elapsed.TotalMilliseconds<budgetMilliseconds)
            {
                var key=buildQueue.Dequeue();if(!buildRows.TryGetValue(key,out int row))continue;
                Chunk chunk=world?.GetChunk(key.x,key.y);
                if(chunk==null){buildRows.Remove(key);continue;}
                if(RenderEligibility!=null&&!RenderEligibility(key.x,key.y)){buildQueue.Enqueue(key);continue;}
                if(!renderedChunks.TryGetValue(key,out var data))
                {data=CreateChunkRenderData(chunk);data.Root.SetActive(false);renderedChunks.Add(key,data);buildRows[key]=0;buildQueue.Enqueue(key);continue;}
                if(row<0)row=0;
                int end=Mathf.Min(Chunk.SizeY,row+2);DrawRows(chunk,data,row,end);
                if(end<Chunk.SizeY){buildRows[key]=end;buildQueue.Enqueue(key);continue;}
                UploadLayers(data);UpdateLightTexture(chunk,data);data.Ready=true;data.Root.SetActive(true);buildRows.Remove(key);
                break; // At most one cold chunk's texture set is uploaded per frame.
            }
            buildClock.Stop();
        }
        private void QueueUpload(UnityEngine.Vector2Int key,ChunkRenderData data,bool background)
        {
            if(background)data.BackgroundDirty=true;else data.ForegroundDirty=true;
            if(pendingUploads.Add(key))uploadQueue.Enqueue(key);
        }
        public void FlushVisualEdits(int maxChunks=2)
        {
            int attempts=uploadQueue.Count,done=0;
            while(attempts-->0&&uploadQueue.Count>0&&done<maxChunks)
            {
                var key=uploadQueue.Dequeue();pendingUploads.Remove(key);
                if(!renderedChunks.TryGetValue(key,out var data))continue;
                if(!data.Ready||data.Root==null||!data.Root.activeSelf)
                {if(pendingUploads.Add(key))uploadQueue.Enqueue(key);continue;}
                if(data.BackgroundDirty){data.BackgroundTexture.Apply(false,false);data.BackgroundWaveTexture.Apply(false,false);data.BackgroundWaveModes.Apply(false,false);data.BackgroundDirty=false;}
                if(data.ForegroundDirty){data.ForegroundTexture.Apply(false,false);data.ForegroundWaveTexture.Apply(false,false);data.ForegroundWaveModes.Apply(false,false);data.ForegroundDirty=false;}
                done++;
            }
        }

        public void Render(
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


            if (RenderEligibility != null && !RenderEligibility(chunk.X, chunk.Y))
                return;

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


            UpdateLightTexture(chunk,data);data.Ready=true;buildRows.Remove(key);
            data.Root.SetActive(true);
        }


        // =========================================================
        // CREATE CHUNK OBJECT
        // =========================================================

        private ChunkRenderData CreateChunkRenderData(
            Chunk chunk
        )
        {
            ChunkRenderData data =
                new ChunkRenderData();


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


            // -------------------------
            // BACKGROUND
            // -------------------------

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


            // -----------------------------------------------------
            // PERMANENT BACKGROUND TINT
            // -----------------------------------------------------
            //
            // The layer is darkened at SpriteRenderer level.
            // Lighting remains exactly the same for both terrain
            // layers, so torch/sun light cannot erase the visual
            // distinction between foreground and background.
            //
            backgroundRenderer.color =
                new Color(
                    BackgroundSpriteBrightness,
                    BackgroundSpriteBrightness,
                    BackgroundSpriteBrightness,
                    1f
                );


            data.BackgroundRenderer =
                backgroundRenderer;


            // -------------------------
            // FOREGROUND
            // -------------------------

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


            foregroundRenderer.color =
                Color.white;


            data.ForegroundRenderer =
                foregroundRenderer;

            GameObject backgroundWaveObject = new GameObject("Background_Wave");
            backgroundWaveObject.transform.SetParent(root.transform, false);
            SpriteRenderer backgroundWaveRenderer = backgroundWaveObject.AddComponent<SpriteRenderer>();
            backgroundWaveRenderer.sortingOrder = 0;
            backgroundWaveRenderer.color = new Color(
                BackgroundSpriteBrightness, BackgroundSpriteBrightness, BackgroundSpriteBrightness, 1f);
            data.BackgroundWaveRenderer = backgroundWaveRenderer;

            GameObject foregroundWaveObject = new GameObject("Foreground_Wave");
            foregroundWaveObject.transform.SetParent(root.transform, false);
            SpriteRenderer foregroundWaveRenderer = foregroundWaveObject.AddComponent<SpriteRenderer>();
            foregroundWaveRenderer.sortingOrder = 2;
            foregroundWaveRenderer.color = Color.white;
            data.ForegroundWaveRenderer = foregroundWaveRenderer;


            // -------------------------
            // TILE TEXTURES
            // -------------------------

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

            data.ForegroundWaveModes = CreateBlockTexture(Chunk.SizeX, Chunk.SizeY);
            data.BackgroundWaveModes = CreateBlockTexture(Chunk.SizeX, Chunk.SizeY);
            data.BackgroundWaveTexture = CreateBlockTexture(textureWidth, textureHeight);
            data.ForegroundWaveTexture = CreateBlockTexture(textureWidth, textureHeight);


            data.BackgroundRenderer.sprite =
                CreateSprite(
                    data.BackgroundTexture
                );


            data.ForegroundRenderer.sprite =
                CreateSprite(
                    data.ForegroundTexture
                );

            data.BackgroundWaveRenderer.sprite = CreateSprite(data.BackgroundWaveTexture);
            data.ForegroundWaveRenderer.sprite = CreateSprite(data.ForegroundWaveTexture);


            // -------------------------
            // LIGHT TEXTURE
            // -------------------------

            data.LightTexture =
                new Texture2D(
                    LightTextureWidth,
                    LightTextureHeight,
                    TextureFormat.RGBA32,
                    false
                );


            data.LightTexture.name =
                "Light_" +
                chunk.X +
                "_" +
                chunk.Y;


            data.LightTexture.filterMode =
                lightingDetail == 0 ? FilterMode.Point : FilterMode.Bilinear;


            data.LightTexture.wrapMode =
                TextureWrapMode.Clamp;


            data.LightPixels =
                new Color32[
                    LightTextureWidth *
                    LightTextureHeight
                ];


            if (lightingMaterial != null)
            {
                data.BackgroundRenderer.sharedMaterial = lightingMaterial;
                data.ForegroundRenderer.sharedMaterial = lightingMaterial;
            }

            Material overlayMaterial = windMaterial != null ? windMaterial : lightingMaterial;
            if (overlayMaterial != null)
            {
                data.BackgroundWaveRenderer.sharedMaterial = overlayMaterial;
                data.ForegroundWaveRenderer.sharedMaterial = overlayMaterial;
            }


            data.BackgroundProperties = new MaterialPropertyBlock();
            data.ForegroundProperties = new MaterialPropertyBlock();
            data.BackgroundWaveProperties = new MaterialPropertyBlock();
            data.ForegroundWaveProperties = new MaterialPropertyBlock();


            ApplyMaterialProperties(
                data
            );


            return data;
        }


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

                    Vector2.zero,

                    BlockRenderer.BlockPixelSize
                );
        }


        // =========================================================
        // DRAW BLOCKS
        // =========================================================

        private void DrawBlocks(Chunk chunk,ChunkRenderData data)
        {DrawRows(chunk,data,0,Chunk.SizeY);UploadLayers(data);}

        private void DrawRows(
            Chunk chunk,
            ChunkRenderData data,int rowStart,int rowEnd
        )
        {
            for (
                int y = rowStart;
                y < rowEnd;
                y++
            )
            {
                for (
                    int x = 0;
                    x < Chunk.SizeX;
                    x++
                )
                {
                    ushort backgroundId =
                        chunk.GetBackground(
                            x,
                            y
                        );

                    int worldX =
                        chunk.X * Chunk.SizeX +
                        x;

                    int worldY =
                        chunk.Y * Chunk.SizeY +
                        y;

                    DrawCellToStaticAndWaveLayers(
                        data, x, y, backgroundId, worldX, worldY,
                        BlockVisualLayer.Background);

                    TrackDynamicCell(
                        data.BackgroundDynamicVisuals,
                        x,
                        y,
                        backgroundId,
                        worldX,
                        worldY,
                        BlockVisualLayer.Background
                    );
                    ushort foregroundId =
                        chunk.GetBlock(
                            x,
                            y
                        );

                    DrawCellToStaticAndWaveLayers(
                        data, x, y, foregroundId, worldX, worldY,
                        BlockVisualLayer.Foreground);

                    TrackDynamicCell(
                        data.ForegroundDynamicVisuals,
                        x,
                        y,
                        foregroundId,
                        worldX,
                        worldY,
                        BlockVisualLayer.Foreground
                    );
                }
            }


        }
        private void UploadLayers(ChunkRenderData data)
        {
            data.BackgroundTexture.Apply(false,false);data.BackgroundWaveTexture.Apply(false,false);data.BackgroundWaveModes.Apply(false,false);
            data.ForegroundTexture.Apply(false,false);data.ForegroundWaveTexture.Apply(false,false);data.ForegroundWaveModes.Apply(false,false);
            data.ForegroundDirty=data.BackgroundDirty=false;
        }



        private bool IsWaveBlock(ushort blockId)
        {
            if (blockId == 0)
                return false;

            bool cached;
            if (waveTagCache.TryGetValue(blockId, out cached))
                return cached;

            ContentID contentId = BlockIDRegistry.GetContentID(blockId);
            if (!BlockRegistry.Contains(contentId))
            {
                waveTagCache[blockId] = false;
                return false;
            }

            BlockDefinition definition = BlockRegistry.Get(contentId);
            bool hasWaveTag = false;

            if (definition != null && definition.Tags != null)
            {
                for (int i = 0; i < definition.Tags.Count; i++)
                {
                    if (string.Equals(definition.Tags[i], "wave",
                        System.StringComparison.OrdinalIgnoreCase))
                    {
                        hasWaveTag = true;
                        break;
                    }
                }
            }
            waveTagCache[blockId] = hasWaveTag;
            return hasWaveTag;
        }


        private void DrawCellToStaticAndWaveLayers(
            ChunkRenderData data,
            int localX,
            int localY,
            ushort blockId,
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            bool background = layer == BlockVisualLayer.Background;
            bool wave = IsWaveBlock(blockId);
            var modes = background ? data.BackgroundWaveModes : data.ForegroundWaveModes;
            var definition = blockId != 0 ? world.GetBlockDefinition(blockId) : null;
            bool groundGrass = definition != null && definition.Solid &&
                Game.Visuals.PixelEffects.PixelWorldOcclusion.HasTag(definition, "grass") &&
                Game.Visuals.PixelEffects.PixelWorldOcclusion.HasTag(definition, "dirt");
            modes.SetPixel(localX, localY, groundGrass ? Color.white : Color.clear);

            Texture2D staticTexture = background ? data.BackgroundTexture : data.ForegroundTexture;
            Texture2D waveTexture = background ? data.BackgroundWaveTexture : data.ForegroundWaveTexture;

            // Keep only one copy: static terrain OR animated overlay.
            BlockRenderer.DrawBlock(
                staticTexture, localX, localY, wave ? (ushort)0 : blockId,
                worldX, worldY, layer);
            BlockRenderer.DrawBlock(
                waveTexture, localX, localY, wave ? blockId : (ushort)0,
                worldX, worldY, layer);

            if (background)
            {
                Game.BlockTransforms.BackgroundBlockTransformRegistry.ApplyToCell(
                    staticTexture, localX, localY, BlockRenderer.BlockPixelSize, worldX, worldY);
                Game.BlockTransforms.BackgroundBlockTransformRegistry.ApplyToCell(
                    waveTexture, localX, localY, BlockRenderer.BlockPixelSize, worldX, worldY);
            }
            else
            {
                Game.BlockTransforms.BlockTransformRenderBridge.ApplyToCell(
                    staticTexture, localX, localY, BlockRenderer.BlockPixelSize, worldX, worldY);
                Game.BlockTransforms.BlockTransformRenderBridge.ApplyToCell(
                    waveTexture, localX, localY, BlockRenderer.BlockPixelSize, worldX, worldY);
            }
        }


        private static void TrackDynamicCell(
            Dictionary<int, string> registry,
            int localX,
            int localY,
            ushort blockId,
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            if (registry == null)
                return;

            int index =
                localY * Chunk.SizeX +
                localX;

            if (!BlockRenderer.IsDynamic(blockId))
            {
                registry.Remove(index);
                return;
            }

            registry[index] =
                BlockRenderer.GetVisualKey(
                    blockId,
                    worldX,
                    worldY,
                    layer
                );
        }


        /// <summary>
        /// Redraws only cells whose visual can change with time/state.
        /// Static terrain never enters this path.
        /// </summary>
        public void UpdateAnimatedVisuals()
        {
            if (world == null || renderedChunks.Count == 0)
                return;

            // The game can run at several hundred FPS. Visual animations do not
            // need a 400 Hz scheduler, so cap checks at 60 Hz.
            if (Time.time < nextDynamicVisualCheck)
                return;

            nextDynamicVisualCheck = Time.time + 1f / 60f;

            foreach (KeyValuePair<UnityEngine.Vector2Int, ChunkRenderData> pair in renderedChunks)
            {
                if (pair.Value.Root == null || !pair.Value.Root.activeSelf) continue;
                Chunk chunk =
                    world.GetChunk(
                        pair.Key.x,
                        pair.Key.y
                    );

                if (chunk == null)
                    continue;

                ChunkRenderData data = pair.Value;
                bool foregroundDirty = UpdateDynamicLayer(
                    chunk,
                    data.ForegroundTexture,
                    data.ForegroundDynamicVisuals,
                    false
                );

                bool backgroundDirty = UpdateDynamicLayer(
                    chunk,
                    data.BackgroundTexture,
                    data.BackgroundDynamicVisuals,
                    true
                );

                if(foregroundDirty)QueueUpload(pair.Key,data,false);
                if(backgroundDirty)QueueUpload(pair.Key,data,true);
            }
        }


        private bool UpdateDynamicLayer(
            Chunk chunk,
            Texture2D texture,
            Dictionary<int, string> registry,
            bool background)
        {
            if (texture == null || registry == null || registry.Count == 0)
                return false;

            bool dirty = false;

            dynamicCellScratch.Clear();
            dynamicRemoveScratch.Clear();

            foreach (int key in registry.Keys)
                dynamicCellScratch.Add(key);

            for (int i = 0; i < dynamicCellScratch.Count; i++)
            {
                int index = dynamicCellScratch[i];
                int localX = index % Chunk.SizeX;
                int localY = index / Chunk.SizeX;

                ushort blockId =
                    background
                        ? chunk.GetBackground(localX, localY)
                        : chunk.GetBlock(localX, localY);

                if (!BlockRenderer.IsDynamic(blockId))
                {
                    dynamicRemoveScratch.Add(index);
                    continue;
                }

                int worldX = chunk.X * Chunk.SizeX + localX;
                int worldY = chunk.Y * Chunk.SizeY + localY;
                BlockVisualLayer layer =
                    background
                        ? BlockVisualLayer.Background
                        : BlockVisualLayer.Foreground;

                string currentKey =
                    BlockRenderer.GetVisualKey(
                        blockId,
                        worldX,
                        worldY,
                        layer
                    );

                registry.TryGetValue(index, out string oldKey);
                if (string.Equals(oldKey, currentKey, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                ChunkRenderData renderData =
                    renderedChunks[new UnityEngine.Vector2Int(chunk.X, chunk.Y)];
                DrawCellToStaticAndWaveLayers(
                    renderData, localX, localY, blockId, worldX, worldY, layer);

                registry[index] = currentKey;
                dirty = true;
            }

            for (int i = 0; i < dynamicRemoveScratch.Count; i++)
                registry.Remove(dynamicRemoveScratch[i]);

            return dirty;
        }


        public void RefreshVisualAt(
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            if (world == null || layer == BlockVisualLayer.Furniture)
                return;

            int chunkX = Mathf.FloorToInt(worldX / (float)Chunk.SizeX);
            int chunkY = Mathf.FloorToInt(worldY / (float)Chunk.SizeY);

            Chunk chunk = world.GetChunk(chunkX, chunkY);
            if (chunk == null)
                return;

            int localX = worldX - chunkX * Chunk.SizeX;
            int localY = worldY - chunkY * Chunk.SizeY;

            if (layer == BlockVisualLayer.Background)
                UpdateBackgroundBlock(chunk, localX, localY);
            else
                UpdateBlock(chunk, localX, localY);
        }


        // =========================================================
        // UPDATE ONE BLOCK
        // =========================================================

        public void UpdateBlock(
            Chunk chunk,
            int localX,
            int localY
        )
        {
            if (
                chunk ==
                null
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
                QueueRender(chunk);


                return;
            }


            ushort blockId =
                chunk.GetBlock(
                    localX,
                    localY
                );

            int worldX =
                chunk.X * Chunk.SizeX +
                localX;

            int worldY =
                chunk.Y * Chunk.SizeY +
                localY;

            DrawCellToStaticAndWaveLayers(
                data, localX, localY, blockId, worldX, worldY,
                BlockVisualLayer.Foreground);

            TrackDynamicCell(
                data.ForegroundDynamicVisuals,
                localX,
                localY,
                blockId,
                worldX,
                worldY,
                BlockVisualLayer.Foreground);

            QueueUpload(key,data,false);
        }


        public void UpdateBackgroundBlock(
            Chunk chunk,
            int localX,
            int localY
        )
        {
            if (
                chunk ==
                null
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
                QueueRender(chunk);


                return;
            }


            ushort blockId =
                chunk.GetBackground(
                    localX,
                    localY
                );

            int worldX =
                chunk.X * Chunk.SizeX +
                localX;

            int worldY =
                chunk.Y * Chunk.SizeY +
                localY;

            DrawCellToStaticAndWaveLayers(
                data, localX, localY, blockId, worldX, worldY,
                BlockVisualLayer.Background);

            TrackDynamicCell(
                data.BackgroundDynamicVisuals,
                localX,
                localY,
                blockId,
                worldX,
                worldY,
                BlockVisualLayer.Background);

            QueueUpload(key,data,true);
        }


        // =========================================================
        // LIGHT UPDATES
        // =========================================================

        public void UpdateDirtyLighting(int maxChunksPerFrame=6)
        {
            if(world==null)return;
            maxChunksPerFrame=Mathf.Min(maxChunksPerFrame,lightingDetail==0?2:lightingDetail==1?4:6);
            int done=0;
            foreach(var pair in renderedChunks)
            {
                if(done>=maxChunksPerFrame)break;var data=pair.Value;
                if(!data.Ready||data.Root==null||!data.Root.activeSelf)continue;
                Chunk chunk=world.GetChunk(pair.Key.x,pair.Key.y);var light=chunk?.GetLightData();
                if(light==null||!light.IsDirty||!light.IsInitialized)continue;
                UpdateLightTexture(chunk,data);done++;
            }
        }


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


            int index =
                0;


            for (
                int lightY = -LightBorder;
                lightY < Chunk.SizeY + LightBorder;
                lightY++
            )
            {
                for (
                    int lightX = -LightBorder;
                    lightX < Chunk.SizeX + LightBorder;
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


                    // Store artificial light independently from sunlight.
                    // RGB = torches/lava/other emissive blocks; A = daylight.
                    // The shader attenuates only A at night; lamp RGB never dims.
                    data.LightPixels[index++] = new Color32(
                        node.R == 0 ? (byte)0 : (byte)Mathf.RoundToInt(node.R * (255f / 15f)),
                        node.G == 0 ? (byte)0 : (byte)Mathf.RoundToInt(node.G * (255f / 15f)),
                        node.B == 0 ? (byte)0 : (byte)Mathf.RoundToInt(node.B * (255f / 15f)),
                        node.Sun == 0 ? (byte)0 : (byte)Mathf.RoundToInt(node.Sun * (255f / 15f))
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


        private LightNode GetWorldLight(
            Chunk chunk,
            int localX,
            int localY,
            int worldX,
            int worldY
        )
        {
            if (
                world !=
                null
            )
            {
                return
                    world.GetLight(
                        worldX,
                        worldY
                    );
            }


            if (
                localX >=
                0
                &&
                localX <
                Chunk.SizeX
                &&
                localY >=
                0
                &&
                localY <
                Chunk.SizeY
            )
            {
                return
                    chunk
                        .GetLightData()
                        .Get(
                            localX,
                            localY
                        );
            }


            return
                LightNode.None;
        }


        /// <summary>
        /// Read-only handle to the same updated RGBA light texture used by the
        /// foreground and background renderers. Never allocate/copy its pixels.
        /// Returning false is normal while a chunk is still streaming.
        /// </summary>
        public bool TryGetChunkLightTexture(
            int chunkX, int chunkY, out Texture2D texture)
        {
            texture = null;
            ChunkRenderData renderData;
            if (!renderedChunks.TryGetValue(
                    new UnityEngine.Vector2Int(chunkX, chunkY), out renderData) ||
                renderData == null || renderData.LightTexture == null)
                return false;

            texture = renderData.LightTexture;
            return true;
        }

        // =========================================================
        // SHADER PROPERTIES
        // =========================================================

        private void ApplyMaterialProperties(
            ChunkRenderData data
        )
        {
            float scaleX =
                (float)
                Chunk.SizeX /
                LightTextureWidth;


            float scaleY =
                (float)
                Chunk.SizeY /
                LightTextureHeight;


            float offsetX =
                (float)
                LightBorder /
                LightTextureWidth;


            float offsetY =
                (float)
                LightBorder /
                LightTextureHeight;


            Vector4 lightUV =
                new Vector4(
                    scaleX,
                    scaleY,
                    offsetX,
                    offsetY
                );


            // -------------------------
            // FOREGROUND
            // -------------------------

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


            data.ForegroundProperties.SetFloat("_Ambient", 0f);


            data.ForegroundProperties.SetFloat(
                "_LightGamma",
                0.72f
            );


            data.ForegroundRenderer.SetPropertyBlock(
                data.ForegroundProperties
            );


            // -------------------------
            // BACKGROUND
            // -------------------------
            //
            // DO NOT darken the background through lighting.
            //
            // The old renderer used 0.68 here. That couples visual
            // separation to the lighting shader. We now keep the
            // same light response as foreground and darken the actual
            // SpriteRenderer by exactly 30% instead.
            //
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
                1f
            );


            data.BackgroundProperties.SetFloat("_Ambient", 0f);


            data.BackgroundProperties.SetFloat(
                "_LightGamma",
                0.72f
            );


            data.BackgroundRenderer.SetPropertyBlock(data.BackgroundProperties);

            data.ForegroundWaveProperties.SetTexture("_PlantModeTex", data.ForegroundWaveModes);
            data.ForegroundWaveProperties.SetTexture("_LightTex", data.LightTexture);
            data.ForegroundWaveProperties.SetVector("_LightUVScaleOffset", lightUV);
            data.ForegroundWaveProperties.SetFloat("_LayerBrightness", 1f);
            data.ForegroundWaveProperties.SetFloat("_Ambient", 0f);
            data.ForegroundWaveProperties.SetFloat("_LightGamma", 0.72f);
            data.ForegroundWaveProperties.SetVector("_WindWorldOffset",
                new Vector4(data.Root.transform.position.x, data.Root.transform.position.y, 0f, 0f));
            data.ForegroundWaveRenderer.SetPropertyBlock(data.ForegroundWaveProperties);

            data.BackgroundWaveProperties.SetTexture("_PlantModeTex", data.BackgroundWaveModes);
            data.BackgroundWaveProperties.SetTexture("_LightTex", data.LightTexture);
            data.BackgroundWaveProperties.SetVector("_LightUVScaleOffset", lightUV);
            data.BackgroundWaveProperties.SetFloat("_LayerBrightness", 1f);
            data.BackgroundWaveProperties.SetFloat("_Ambient", 0f);
            data.BackgroundWaveProperties.SetFloat("_LightGamma", 0.72f);
            data.BackgroundWaveProperties.SetVector("_WindWorldOffset",
                new Vector4(data.Root.transform.position.x, data.Root.transform.position.y, 0f, 0f));
            data.BackgroundWaveRenderer.SetPropertyBlock(data.BackgroundWaveProperties);
        }


        // =========================================================
        // REMOVE
        // =========================================================

        public void RemoveChunk(
            int chunkX,
            int chunkY
        )
        {
            var removedKey=new UnityEngine.Vector2Int(chunkX,chunkY);
            buildRows.Remove(removedKey);pendingUploads.Remove(removedKey);

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


            if (
                data.Root !=
                null
            )
            {
                Object.Destroy(
                    data.Root
                );
            }


            if (
                data.ForegroundTexture !=
                null
            )
            {
                Object.Destroy(
                    data.ForegroundTexture
                );
            }


            if (
                data.BackgroundTexture !=
                null
            )
            {
                Object.Destroy(
                    data.BackgroundTexture
                );
            }


            if (
                data.LightTexture !=
                null
            )
            {
                Object.Destroy(
                    data.LightTexture
                );
            }

            if (data.BackgroundWaveModes != null) Object.Destroy(data.BackgroundWaveModes);
            if (data.BackgroundWaveTexture != null)
                Object.Destroy(data.BackgroundWaveTexture);
            if (data.ForegroundWaveModes != null) Object.Destroy(data.ForegroundWaveModes);
            if (data.ForegroundWaveTexture != null)
                Object.Destroy(data.ForegroundWaveTexture);
        }


        public void Clear()
        {
            buildQueue.Clear();buildRows.Clear();uploadQueue.Clear();pendingUploads.Clear();

            foreach (
                var pair
                in renderedChunks
            )
            {
                ChunkRenderData data =
                    pair.Value;


                if (
                    data.Root !=
                    null
                )
                {
                    Object.Destroy(
                        data.Root
                    );
                }


                if (
                    data.ForegroundTexture !=
                    null
                )
                {
                    Object.Destroy(
                        data.ForegroundTexture
                    );
                }


                if (
                    data.BackgroundTexture !=
                    null
                )
                {
                    Object.Destroy(
                        data.BackgroundTexture
                    );
                }


                if (
                    data.LightTexture !=
                    null
                )
                {
                    Object.Destroy(
                        data.LightTexture
                    );
                }

                if (data.BackgroundWaveModes != null) Object.Destroy(data.BackgroundWaveModes);
                if (data.BackgroundWaveTexture != null)
                    Object.Destroy(data.BackgroundWaveTexture);
                if (data.ForegroundWaveModes != null) Object.Destroy(data.ForegroundWaveModes);
                if (data.ForegroundWaveTexture != null)
                    Object.Destroy(data.ForegroundWaveTexture);
            }


            renderedChunks.Clear();
        }


        // =========================================================
        // RENDER DATA
        // =========================================================

        private sealed class ChunkRenderData
        {
            public GameObject Root;
            public bool Ready,ForegroundDirty,BackgroundDirty;


            public SpriteRenderer
                ForegroundRenderer;


            public SpriteRenderer BackgroundRenderer;
            public SpriteRenderer ForegroundWaveRenderer;
            public SpriteRenderer BackgroundWaveRenderer;

            public Texture2D ForegroundTexture;
            public Texture2D BackgroundTexture;
            public Texture2D ForegroundWaveTexture;
            public Texture2D ForegroundWaveModes;
            public Texture2D BackgroundWaveTexture;
            public Texture2D BackgroundWaveModes;


            public Texture2D
                LightTexture;


            public Color32[]
                LightPixels;


            public MaterialPropertyBlock
                ForegroundProperties;


            public MaterialPropertyBlock BackgroundProperties;
            public MaterialPropertyBlock ForegroundWaveProperties;
            public MaterialPropertyBlock BackgroundWaveProperties;


            // localCellIndex -> last resolved visual texture name.
            // Only blocks that actually have Animation/States live here.
            public readonly Dictionary<int, string>
                ForegroundDynamicVisuals =
                    new Dictionary<int, string>();


            public readonly Dictionary<int, string>
                BackgroundDynamicVisuals =
                    new Dictionary<int, string>();
        }


        // =========================================================
        // RENDER ALL
        // =========================================================

        public void RenderAllLoaded(
            Game.World.World world
        )
        {
            if (
                world ==
                null
            )
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


                if (
                    chunk ==
                    null
                )
                {
                    continue;
                }


                if (RenderEligibility == null || RenderEligibility(chunk.X, chunk.Y))
                    QueueRender(chunk,true);
            }
        }
    }
}

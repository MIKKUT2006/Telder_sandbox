using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Game.World.Generation;
using Game.World.Rendering;
using Game.World.Dimensions;

namespace Game.World.Parallax
{
    public sealed class ParallaxTerrainLayer
    {
        private const int ChunkWidthBlocks =
            28;

        private const int TextureHeightBlocks =
            72;


        private sealed class RenderChunk
        {
            public GameObject Root;
            public Texture2D Texture;
            public Sprite Sprite;
            public SpriteRenderer Renderer;
        }


        private readonly Dictionary<int, RenderChunk>
            chunks =
            new Dictionary<int, RenderChunk>();


        private readonly Transform root;

        private readonly Camera camera;

        private readonly WorldSettings settings;

        private readonly WorldGenerator generator;

        private readonly ParallaxLayerSettings layer;

        private readonly ParallaxBiomeSampler biomeSampler;

        private readonly int dimensionSeed;

        private readonly Vector3 initialCameraPosition;

        private readonly Material material;

        private readonly MethodInfo getSurfaceHeightMethod;


        /*
         * Actual playable ground at the camera's initial X.
         *
         * This avoids relying only on settings.SurfaceHeight when the
         * current generator/biome has moved the real surface.
         */
        private readonly float playableGroundAtStart;


        private int lastCenterChunk =
            int.MinValue;


        public ParallaxTerrainLayer(
            Transform parent,
            Camera camera,
            WorldSettings settings,
            WorldGenerator generator,
            ParallaxLayerSettings layer,
            Material material
        )
        {
            this.camera = camera;
            this.settings = settings;
            this.generator = generator;
            this.layer = layer;
            this.material = material;

            dimensionSeed =
                DimensionTravelRuntime.Current != null
                ? DimensionTravelRuntime.Current.Seed
                : settings.Seed;

            biomeSampler =
                new ParallaxBiomeSampler(
                    generator
                );

            initialCameraPosition =
                camera.transform.position;

            if (generator != null)
            {
                getSurfaceHeightMethod =
                    generator.GetType().GetMethod(
                        "GetSurfaceHeight",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        new Type[]
                        {
                            typeof(int)
                        },
                        null
                    );
            }

            playableGroundAtStart =
                ResolvePlayableGround(
                    Mathf.FloorToInt(
                        initialCameraPosition.x
                    )
                );

            GameObject layerObject =
                new GameObject(
                    "Terrain_" +
                    layer.Name
                );

            layerObject.transform.SetParent(
                parent,
                false
            );

            root =
                layerObject.transform;
        }


        public void Tick()
        {
            if (camera == null)
                return;

            Vector3 delta =
                camera.transform.position -
                initialCameraPosition;

            /*
             * Horizontal parallax stays normal.
             *
             * Vertical placement is anchored to the ACTUAL playable
             * surface detected at startup, then pushed down by the
             * layer's VerticalOffset.
             */
            root.position =
                new Vector3(
                    delta.x *
                    (
                        1f -
                        layer.FactorX
                    ),

                    delta.y *
                    (
                        1f -
                        layer.FactorY
                    )
                    +
                    (
                        playableGroundAtStart -
                        settings.SurfaceHeight
                    )
                    +
                    layer.VerticalOffset,

                    0f
                );

            float virtualCenterX =
                (
                    initialCameraPosition.x +
                    delta.x *
                    layer.FactorX
                )
                /
                Mathf.Max(
                    0.01f,
                    layer.BlockWorldScale
                );

            int centerChunk =
                FloorDiv(
                    Mathf.FloorToInt(
                        virtualCenterX
                    ),
                    ChunkWidthBlocks
                );

            if (
                centerChunk !=
                lastCenterChunk
            )
            {
                lastCenterChunk =
                    centerChunk;

                UnloadFarChunks(
                    centerChunk
                );
            }

            for (
                int distance = 0;
                distance <= layer.LoadRadius;
                distance++
            )
            {
                int left =
                    centerChunk -
                    distance;

                if (!chunks.ContainsKey(left))
                {
                    CreateChunk(left);
                    return;
                }

                if (distance == 0)
                    continue;

                int right =
                    centerChunk +
                    distance;

                if (!chunks.ContainsKey(right))
                {
                    CreateChunk(right);
                    return;
                }
            }
        }


        /*
         * Generates a few nearest visual chunks synchronously while the
         * DimensionSpawnCurtain is still black.
         *
         * Normal gameplay still uses Tick() and creates at most one
         * missing chunk per frame.
         */
        public void Prime(
            int iterations
        )
        {
            iterations =
                Mathf.Max(
                    1,
                    iterations
                );


            for (
                int i = 0;
                i < iterations;
                i++
            )
            {
                Tick();
            }
        }


        private void CreateChunk(
            int chunkIndex
        )
        {
            int textureWidth =
                ChunkWidthBlocks *
                BlockRenderer.BlockPixelSize;

            int textureHeight =
                TextureHeightBlocks *
                BlockRenderer.BlockPixelSize;

            Texture2D texture =
                new Texture2D(
                    textureWidth,
                    textureHeight,
                    TextureFormat.RGBA32,
                    false
                );

            texture.name =
                "Parallax_" +
                layer.Name +
                "_" +
                chunkIndex;

            texture.filterMode =
                FilterMode.Point;

            texture.wrapMode =
                TextureWrapMode.Clamp;

            texture.SetPixels32(
                new Color32[
                    textureWidth *
                    textureHeight
                ]
            );

            int startX =
                chunkIndex *
                ChunkWidthBlocks;

            /*
             * We no longer render a huge 42+ block-deep slab.
             * Bottom is tied to the visible depth around the horizon.
             */
            int bottomY =
                Mathf.RoundToInt(
                    settings.SurfaceHeight
                )
                -
                layer.VisibleDepth
                -
                8;

            for (
                int localX = 0;
                localX < ChunkWidthBlocks;
                localX++
            )
            {
                int virtualX =
                    startX +
                    localX;

                ParallaxBiomeSampler.Palette palette =
                    biomeSampler.GetPalette(
                        virtualX
                    );

                int surfaceY =
                    GenerateSurfaceHeight(
                        virtualX,
                        palette
                    );

                /*
                 * Hard visual cutoff:
                 * only a shallow slice below the generated surface is drawn.
                 *
                 * This is the direct fix for the visible deep stone layer.
                 */
                int lowestVisibleY =
                    surfaceY -
                    layer.VisibleDepth;

                for (
                    int localY = 0;
                    localY < TextureHeightBlocks;
                    localY++
                )
                {
                    int virtualY =
                        bottomY +
                        localY;

                    if (
                        virtualY <
                        lowestVisibleY
                    )
                    {
                        continue;
                    }

                    ushort block =
                        GetBlock(
                            virtualY,
                            surfaceY,
                            palette
                        );

                    if (block == 0)
                        continue;

                    BlockRenderer.DrawBlock(
                        texture,
                        localX,
                        localY,
                        block
                    );
                }
            }

            texture.Apply(
                false,
                false
            );

            Sprite sprite =
                Sprite.Create(
                    texture,

                    new Rect(
                        0f,
                        0f,
                        texture.width,
                        texture.height
                    ),

                    Vector2.zero,

                    BlockRenderer.BlockPixelSize
                );

            sprite.name =
                texture.name +
                "_Sprite";

            GameObject chunkObject =
                new GameObject(
                    "ParallaxChunk_" +
                    chunkIndex
                );

            chunkObject.transform.SetParent(
                root,
                false
            );

            float scale =
                layer.BlockWorldScale;

            /*
             * Anchor the texture around SurfaceHeight while the entire
             * layer root handles the final world/horizon offset.
             */
            float verticalAnchor =
                (
                    1f -
                    scale
                )
                *
                settings.SurfaceHeight;

            chunkObject.transform.localPosition =
                new Vector3(
                    startX *
                    scale,

                    bottomY *
                    scale +
                    verticalAnchor,

                    0f
                );

            chunkObject.transform.localScale =
                new Vector3(
                    scale,
                    scale,
                    1f
                );

            SpriteRenderer renderer =
                chunkObject.AddComponent<
                    SpriteRenderer
                >();

            renderer.sprite =
                sprite;

            renderer.sortingOrder =
                layer.SortingOrder;

            if (material != null)
            {
                renderer.sharedMaterial =
                    material;
            }

            RenderChunk chunk =
                new RenderChunk();

            chunk.Root = chunkObject;
            chunk.Texture = texture;
            chunk.Sprite = sprite;
            chunk.Renderer = renderer;

            chunks.Add(
                chunkIndex,
                chunk
            );
        }


        private int GenerateSurfaceHeight(
            int x,
            ParallaxBiomeSampler.Palette palette
        )
        {
            float layerSalt =
                layer.SortingOrder *
                17.371f;

            float warpedX =
                x;

            if (
                Mathf.Abs(
                    palette.DistortionStrength
                )
                >
                0.001f
            )
            {
                float distortion =
                    Mathf.PerlinNoise(
                        (
                            x +
                            dimensionSeed *
                            0.3187f +
                            layerSalt
                        )
                        *
                        Mathf.Max(
                            0.000001f,
                            palette.DistortionScale
                        ),

                        71.37f +
                        layerSalt
                    );

                warpedX +=
                    (
                        distortion -
                        0.5f
                    )
                    *
                    2f *
                    palette.DistortionStrength *
                    0.40f;
            }

            float macro =
                Mathf.PerlinNoise(
                    (
                        warpedX +
                        dimensionSeed *
                        0.14731f +
                        layerSalt *
                        13f
                    )
                    *
                    layer.NoiseScale,

                    17.17f +
                    layerSalt
                );

            float detail =
                Mathf.PerlinNoise(
                    (
                        warpedX -
                        dimensionSeed *
                        0.2711f +
                        layerSalt *
                        41f
                    )
                    *
                    layer.DetailScale,

                    93.71f -
                    layerSalt
                );

            float height =
                settings.SurfaceHeight +
                palette.HeightOffset *
                0.15f;

            height +=
                (
                    macro -
                    0.5f
                )
                *
                2f *
                layer.HeightAmplitude *
                Mathf.Clamp(
                    palette.HillHeightMultiplier,
                    0.35f,
                    1.25f
                );

            height +=
                (
                    detail -
                    0.5f
                )
                *
                2f *
                layer.DetailAmplitude *
                Mathf.Clamp(
                    palette.TerrainVariationMultiplier,
                    0.4f,
                    1.25f
                );

            if (
                Mathf.Abs(
                    palette.RidgeStrength
                )
                >
                0.001f
            )
            {
                float ridgeNoise =
                    Mathf.PerlinNoise(
                        (
                            warpedX +
                            dimensionSeed *
                            0.8917f
                        )
                        *
                        Mathf.Max(
                            0.000001f,
                            palette.RidgeScale
                        ),

                        141.3f
                    );

                float ridge =
                    1f -
                    Mathf.Abs(
                        ridgeNoise *
                        2f -
                        1f
                    );

                ridge *= ridge;

                height +=
                    (
                        ridge -
                        0.38f
                    )
                    *
                    palette.RidgeStrength *
                    0.22f;
            }

            if (
                Mathf.Abs(
                    palette.WaveStrength
                )
                >
                0.001f
            )
            {
                height +=
                    Mathf.Sin(
                        warpedX *
                        palette.WaveScale +
                        layerSalt
                    )
                    *
                    palette.WaveStrength *
                    0.28f;
            }

            return
                Mathf.RoundToInt(
                    height
                );
        }


        private ushort GetBlock(
            int y,
            int surface,
            ParallaxBiomeSampler.Palette palette
        )
        {
            if (y > surface)
                return 0;

            if (y == surface)
                return palette.Top;

            int depth =
                surface -
                y;

            if (
                depth <=
                Mathf.Max(
                    1,
                    palette.SoilDepth
                )
            )
            {
                return palette.Soil;
            }

            return palette.Stone;
        }


        private float ResolvePlayableGround(
            int worldX
        )
        {
            if (
                generator == null ||
                getSurfaceHeightMethod == null
            )
            {
                return
                    settings.SurfaceHeight;
            }

            try
            {
                object value =
                    getSurfaceHeightMethod.Invoke(
                        generator,
                        new object[]
                        {
                            worldX
                        }
                    );

                if (value != null)
                {
                    return
                        Convert.ToSingle(
                            value
                        );
                }
            }
            catch
            {
            }

            return
                settings.SurfaceHeight;
        }


        private void UnloadFarChunks(
            int centerChunk
        )
        {
            int allowed =
                layer.LoadRadius +
                2;

            List<int> remove =
                null;

            foreach (
                KeyValuePair<int, RenderChunk> pair
                in chunks
            )
            {
                if (
                    Mathf.Abs(
                        pair.Key -
                        centerChunk
                    )
                    <=
                    allowed
                )
                {
                    continue;
                }

                if (remove == null)
                    remove = new List<int>();

                remove.Add(
                    pair.Key
                );
            }

            if (remove == null)
                return;

            for (
                int i = 0;
                i < remove.Count;
                i++
            )
            {
                int key =
                    remove[i];

                RenderChunk chunk =
                    chunks[key];

                chunks.Remove(
                    key
                );

                DestroyChunk(
                    chunk
                );
            }
        }


        public void Dispose()
        {
            foreach (
                KeyValuePair<int, RenderChunk> pair
                in chunks
            )
            {
                DestroyChunk(
                    pair.Value
                );
            }

            chunks.Clear();

            if (root != null)
            {
                UnityEngine.Object.Destroy(
                    root.gameObject
                );
            }

            if (material != null)
            {
                UnityEngine.Object.Destroy(
                    material
                );
            }
        }


        private void DestroyChunk(
            RenderChunk chunk
        )
        {
            if (chunk == null)
                return;

            if (chunk.Root != null)
                UnityEngine.Object.Destroy(chunk.Root);

            if (chunk.Sprite != null)
                UnityEngine.Object.Destroy(chunk.Sprite);

            if (chunk.Texture != null)
                UnityEngine.Object.Destroy(chunk.Texture);
        }


        private static int FloorDiv(
            int value,
            int divisor
        )
        {
            int result =
                value /
                divisor;

            int remainder =
                value %
                divisor;

            if (
                remainder != 0 &&
                value < 0
            )
            {
                result--;
            }

            return result;
        }
    }
}
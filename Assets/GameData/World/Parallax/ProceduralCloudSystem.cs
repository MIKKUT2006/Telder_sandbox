using System.Collections.Generic;
using UnityEngine;
using Game.World.Dimensions;

namespace Game.World.Parallax
{
    public sealed class ProceduralCloudSystem
    {
        private const int VariantCount =
            9;

        private const int CloudTextureWidth =
            160;

        private const int CloudTextureHeight =
            64;


        private sealed class CloudTile
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public MaterialPropertyBlock Properties;

            public float BaseAlpha;
            public bool RainBand;
        }


        private sealed class CloudBand
        {
            public string Name;

            public float FactorX;
            public float FactorY;

            public float Spacing;
            public float BaseY;
            public float YVariation;

            public float MinScale;
            public float MaxScale;

            public int Radius;
            public int SortingOrder;

            public float BaseAlpha;

            public bool RainBand;

            public Transform Root;

            public readonly Dictionary<int, CloudTile>
                Tiles =
                new Dictionary<int, CloudTile>();
        }


        private readonly Transform parent;
        private readonly Camera camera;
        private readonly WorldSettings settings;

        private readonly int seed;

        private readonly Vector3 initialCameraPosition;

        private readonly Material material;

        private readonly bool customCloudShader;

        private readonly Color fallbackCloudColor;
        private readonly Color fallbackStormColor;

        private readonly List<Sprite>
            sprites =
            new List<Sprite>();

        private readonly List<Texture2D>
            textures =
            new List<Texture2D>();

        private readonly List<CloudBand>
            bands =
            new List<CloudBand>();


        private float rain;


        public ProceduralCloudSystem(
            Transform parent,
            Camera camera,
            WorldSettings settings,
            Material material
        )
        {
            this.parent = parent;
            this.camera = camera;
            this.settings = settings;
            this.material = material;

            seed =
                DimensionTravelRuntime.Current != null
                ? DimensionTravelRuntime.Current.Seed
                : settings.Seed;

            initialCameraPosition =
                camera.transform.position;

            customCloudShader =
                material != null &&
                material.shader != null &&
                material.shader.name ==
                "Game/PS1PixelCloud";

            DimensionAtmosphereProfile atmosphere =
                DimensionAtmosphereProfile
                    .Create();

            fallbackCloudColor =
                atmosphere.CloudColor;

            fallbackStormColor =
                atmosphere.StormCloudColor;

            GenerateVariants();
            CreateBands();
        }


        public void Tick(
            float rainIntensity
        )
        {
            rain =
                Mathf.MoveTowards(
                    rain,
                    Mathf.Clamp01(
                        rainIntensity
                    ),
                    Time.unscaledDeltaTime *
                    0.50f
                );

            if (
                material != null &&
                customCloudShader
            )
            {
                material.SetFloat(
                    "_Rain",
                    rain
                );

                material.SetFloat(
                    "_BlurRadius",
                    Mathf.Lerp(
                        1.05f,
                        1.60f,
                        rain
                    )
                );

                material.SetFloat(
                    "_PixelGridX",
                    Mathf.Lerp(
                        88f,
                        70f,
                        rain
                    )
                );

                material.SetFloat(
                    "_PixelGridY",
                    Mathf.Lerp(
                        38f,
                        30f,
                        rain
                    )
                );

                material.SetFloat(
                    "_DistortionStrength",
                    Mathf.Lerp(
                        0.55f,
                        0.95f,
                        rain
                    )
                );

                material.SetFloat(
                    "_Density",
                    Mathf.Lerp(
                        1.42f,
                        1.95f,
                        rain
                    )
                );
            }

            for (
                int i = 0;
                i < bands.Count;
                i++
            )
            {
                TickBand(
                    bands[i]
                );
            }
        }


        private void CreateBands()
        {
            /*
             * V10.3 cloud centers are also pushed lower.
             */
            AddBand(
                "Far",
                0.13f,
                0.08f,
                28f,
                5.5f,
                2.8f,
                0.82f,
                1.22f,
                5,
                -72,
                0.78f,
                false
            );

            AddBand(
                "Main",
                0.22f,
                0.12f,
                21f,
                3.5f,
                2.4f,
                0.96f,
                1.46f,
                6,
                -62,
                0.96f,
                false
            );

            AddBand(
                "Storm",
                0.31f,
                0.16f,
                16f,
                2f,
                2.0f,
                1.08f,
                1.58f,
                7,
                -52,
                1.00f,
                true
            );
        }


        private void AddBand(
            string name,
            float factorX,
            float factorY,
            float spacing,
            float baseYOffset,
            float yVariation,
            float minScale,
            float maxScale,
            int radius,
            int sortingOrder,
            float baseAlpha,
            bool rainBand
        )
        {
            CloudBand band =
                new CloudBand();

            band.Name = name;

            band.FactorX = factorX;
            band.FactorY = factorY;

            band.Spacing = spacing;

            band.BaseY =
                settings.SurfaceHeight +
                baseYOffset;

            band.YVariation =
                yVariation;

            band.MinScale = minScale;
            band.MaxScale = maxScale;

            band.Radius = radius;

            band.SortingOrder =
                sortingOrder;

            band.BaseAlpha =
                baseAlpha;

            band.RainBand =
                rainBand;

            GameObject rootObject =
                new GameObject(
                    "CloudBand_" +
                    name
                );

            rootObject.transform.SetParent(
                parent,
                false
            );

            band.Root =
                rootObject.transform;

            bands.Add(
                band
            );
        }


        private void TickBand(
            CloudBand band
        )
        {
            Vector3 delta =
                camera.transform.position -
                initialCameraPosition;

            band.Root.position =
                new Vector3(
                    delta.x *
                    (
                        1f -
                        band.FactorX
                    ),

                    delta.y *
                    (
                        1f -
                        band.FactorY
                    ),

                    0f
                );

            float virtualCenter =
                initialCameraPosition.x +
                delta.x *
                band.FactorX;

            int centerTile =
                Mathf.FloorToInt(
                    virtualCenter /
                    band.Spacing
                );

            for (
                int offset = -band.Radius;
                offset <= band.Radius;
                offset++
            )
            {
                int tileIndex =
                    centerTile +
                    offset;

                if (
                    !band.Tiles.ContainsKey(
                        tileIndex
                    )
                )
                {
                    CreateCloudTile(
                        band,
                        tileIndex
                    );
                }
            }

            List<int> remove =
                null;

            foreach (
                KeyValuePair<int, CloudTile> pair
                in band.Tiles
            )
            {
                if (
                    Mathf.Abs(
                        pair.Key -
                        centerTile
                    )
                    <=
                    band.Radius +
                    2
                )
                {
                    UpdateCloudAppearance(
                        pair.Value
                    );

                    continue;
                }

                if (remove == null)
                    remove = new List<int>();

                remove.Add(
                    pair.Key
                );
            }

            if (remove != null)
            {
                for (
                    int i = 0;
                    i < remove.Count;
                    i++
                )
                {
                    int key =
                        remove[i];

                    CloudTile tile =
                        band.Tiles[key];

                    band.Tiles.Remove(
                        key
                    );

                    if (tile.Root != null)
                        Object.Destroy(tile.Root);
                }
            }

            foreach (
                KeyValuePair<int, CloudTile> pair
                in band.Tiles
            )
            {
                UpdateCloudAppearance(
                    pair.Value
                );
            }
        }


        private void CreateCloudTile(
            CloudBand band,
            int tileIndex
        )
        {
            uint hash =
                Hash(
                    seed,
                    tileIndex,
                    band.SortingOrder
                );

            float randomA =
                To01(
                    hash
                );

            float randomB =
                To01(
                    Hash(
                        seed,
                        tileIndex,
                        band.SortingOrder +
                        913
                    )
                );

            float randomC =
                To01(
                    Hash(
                        seed,
                        tileIndex,
                        band.SortingOrder +
                        2711
                    )
                );

            int spriteIndex =
                (int)(
                    hash %
                    VariantCount
                );

            GameObject cloud =
                new GameObject(
                    "Cloud_" +
                    tileIndex
                );

            cloud.transform.SetParent(
                band.Root,
                false
            );

            float x =
                (
                    tileIndex +
                    0.5f
                )
                *
                band.Spacing;

            float y =
                band.BaseY +
                (
                    randomB *
                    2f -
                    1f
                )
                *
                band.YVariation;

            float scale =
                Mathf.Lerp(
                    band.MinScale,
                    band.MaxScale,
                    randomC
                );

            cloud.transform.localPosition =
                new Vector3(
                    x,
                    y,
                    0f
                );

            cloud.transform.localScale =
                new Vector3(
                    scale *
                    Mathf.Lerp(
                        1.10f,
                        1.95f,
                        randomA
                    ),

                    scale,
                    1f
                );

            SpriteRenderer renderer =
                cloud.AddComponent<
                    SpriteRenderer
                >();

            renderer.sprite =
                sprites[
                    spriteIndex
                ];

            renderer.sortingOrder =
                band.SortingOrder;

            if (material != null)
            {
                renderer.sharedMaterial =
                    material;
            }

            CloudTile tile =
                new CloudTile();

            tile.Root = cloud;
            tile.Renderer = renderer;

            tile.Properties =
                new MaterialPropertyBlock();

            tile.BaseAlpha =
                band.BaseAlpha *
                Mathf.Lerp(
                    0.92f,
                    1.06f,
                    randomA
                );

            tile.RainBand =
                band.RainBand;

            band.Tiles.Add(
                tileIndex,
                tile
            );

            UpdateCloudAppearance(
                tile
            );
        }


        private void UpdateCloudAppearance(
            CloudTile tile
        )
        {
            if (
                tile == null ||
                tile.Renderer == null
            )
            {
                return;
            }

            float alpha;

            if (tile.RainBand)
            {
                alpha =
                    tile.BaseAlpha *
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(
                            0.08f,
                            0.68f,
                            rain
                        )
                    );
            }
            else
            {
                alpha =
                    tile.BaseAlpha *
                    Mathf.Lerp(
                        1.0f,
                        1.12f,
                        rain
                    );
            }

            if (customCloudShader)
            {
                tile.Properties.SetFloat(
                    "_Alpha",
                    alpha
                );

                tile.Renderer.SetPropertyBlock(
                    tile.Properties
                );
            }
            else
            {
                /*
                 * If the custom shader fails, fallback still shows
                 * textured clouds instead of magenta/pink.
                 */
                Color color =
                    Color.Lerp(
                        fallbackCloudColor,
                        fallbackStormColor,
                        rain
                    );

                color.a =
                    Mathf.Clamp01(
                        alpha
                    );

                tile.Renderer.color =
                    color;
            }
        }


        private void GenerateVariants()
        {
            for (
                int variant = 0;
                variant < VariantCount;
                variant++
            )
            {
                Texture2D texture =
                    new Texture2D(
                        CloudTextureWidth,
                        CloudTextureHeight,
                        TextureFormat.RGBA32,
                        false
                    );

                texture.name =
                    "ProceduralCloudDetailed_" +
                    variant;

                texture.filterMode =
                    FilterMode.Point;

                texture.wrapMode =
                    TextureWrapMode.Clamp;

                Color32[] pixels =
                    new Color32[
                        CloudTextureWidth *
                        CloudTextureHeight
                    ];

                float offsetX =
                    seed *
                    0.000173f +
                    variant *
                    37.17f;

                float offsetY =
                    seed *
                    0.000319f +
                    variant *
                    19.73f;

                int index = 0;

                for (
                    int y = 0;
                    y < CloudTextureHeight;
                    y++
                )
                {
                    float normalizedY =
                        y /
                        (
                            CloudTextureHeight -
                            1f
                        );

                    float verticalMask =
                        Mathf.Sin(
                            normalizedY *
                            Mathf.PI
                        );

                    verticalMask =
                        Mathf.Pow(
                            Mathf.Max(
                                0f,
                                verticalMask
                            ),
                            0.48f
                        );

                    for (
                        int x = 0;
                        x < CloudTextureWidth;
                        x++
                    )
                    {
                        float normalizedX =
                            x /
                            (
                                CloudTextureWidth -
                                1f
                            );

                        float edge =
                            Mathf.Sin(
                                normalizedX *
                                Mathf.PI
                            );

                        edge =
                            Mathf.Pow(
                                Mathf.Max(
                                    0f,
                                    edge
                                ),
                                0.34f
                            );

                        float n1 =
                            Mathf.PerlinNoise(
                                x * 0.030f + offsetX,
                                y * 0.052f + offsetY
                            );

                        float n2 =
                            Mathf.PerlinNoise(
                                x * 0.061f + offsetY,
                                y * 0.097f + offsetX
                            );

                        float n3 =
                            Mathf.PerlinNoise(
                                x * 0.118f +
                                offsetX * 0.41f,

                                y * 0.171f +
                                offsetY * 0.37f
                            );

                        float n4 =
                            Mathf.PerlinNoise(
                                x * 0.018f +
                                offsetY * 0.23f,

                                y * 0.026f +
                                offsetX * 0.19f
                            );

                        float noise =
                            n1 * 0.40f +
                            n2 * 0.25f +
                            n3 * 0.13f +
                            n4 * 0.22f;

                        float alpha =
                            (
                                noise -
                                0.31f
                            )
                            *
                            3.20f;

                        alpha *=
                            verticalMask *
                            edge;

                        alpha =
                            Mathf.Pow(
                                Mathf.Clamp01(
                                    alpha
                                ),
                                0.66f
                            );

                        byte value =
                            (byte)
                            Mathf.RoundToInt(
                                alpha *
                                255f
                            );

                        pixels[index++] =
                            new Color32(
                                255,
                                255,
                                255,
                                value
                            );
                    }
                }

                texture.SetPixels32(
                    pixels
                );

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

                        new Vector2(
                            0.5f,
                            0.5f
                        ),

                        6.4f
                    );

                textures.Add(
                    texture
                );

                sprites.Add(
                    sprite
                );
            }
        }


        public void Dispose()
        {
            for (
                int i = 0;
                i < bands.Count;
                i++
            )
            {
                foreach (
                    KeyValuePair<int, CloudTile> pair
                    in bands[i].Tiles
                )
                {
                    if (pair.Value.Root != null)
                        Object.Destroy(pair.Value.Root);
                }

                if (bands[i].Root != null)
                    Object.Destroy(bands[i].Root.gameObject);
            }

            bands.Clear();

            for (
                int i = 0;
                i < sprites.Count;
                i++
            )
            {
                if (sprites[i] != null)
                    Object.Destroy(sprites[i]);
            }

            for (
                int i = 0;
                i < textures.Count;
                i++
            )
            {
                if (textures[i] != null)
                    Object.Destroy(textures[i]);
            }

            sprites.Clear();
            textures.Clear();

            if (material != null)
                Object.Destroy(material);
        }


        private static uint Hash(
            int seed,
            int x,
            int salt
        )
        {
            unchecked
            {
                uint h = (uint)seed;

                h ^=
                    (uint)x +
                    0x9E3779B9u +
                    (h << 6) +
                    (h >> 2);

                h ^=
                    (uint)salt +
                    0x85EBCA6Bu +
                    (h << 6) +
                    (h >> 2);

                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;

                return h;
            }
        }


        private static float To01(
            uint value
        )
        {
            return
                (
                    value &
                    0x00FFFFFFu
                )
                /
                16777215f;
        }
    }
}
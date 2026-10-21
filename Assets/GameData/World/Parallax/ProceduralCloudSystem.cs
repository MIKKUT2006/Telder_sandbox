using System.Collections.Generic;
using UnityEngine;
using Game.World.Dimensions;

namespace Game.World.Parallax
{
    public sealed class ProceduralCloudSystem
    {
        private const int VariantCount =
            9;

        /*
         * The texture is the cloud body (CloudInnerWidth x CloudInnerHeight,
         * the same 160x64 as before) plus an empty margin of CloudTexturePad
         * texels on every side. The margin is where the wide soft edge of
         * the cloud fades out, so it is never cut by the sprite border.
         */
        private const int CloudTexturePad =
            16;

        private const int CloudInnerWidth =
            160;

        private const int CloudInnerHeight =
            64;

        private const int CloudTextureWidth =
            CloudInnerWidth +
            CloudTexturePad * 2;

        private const int CloudTextureHeight =
            CloudInnerHeight +
            CloudTexturePad * 2;

        /*
         * The texture stores a SIGNED DISTANCE to the cloud border (not a
         * density): 0.5 = on the border, 1 = SdfRange texels inside,
         * 0 = SdfRange texels outside. This is what makes a wide, smooth
         * edge blur possible. Keep it larger than the widest blur, in texels.
         */
        private const float SdfRange =
            16f;

        /*
         * Pixels-per-unit of the cloud sprite. The shader needs the sprite
         * size in object units (texture size / this value).
         */
        private const float SpritePixelsPerUnit =
            6.4f;

        /*
         * Size of ONE visible cloud pixel in WORLD units (1 block = 1 unit).
         * 1/12 = a twelfth of a block (3 times finer than the old 0.25).
         * Cloud positions are snapped to this grid, so all clouds of a band
         * share one pixel lattice.
         */
        private const float CloudPixelSize =
            0.25f / 3f;

        /*
         * Density above which a point is inside the cloud (the cloud border
         * is built here, when the textures are generated).
         * Lower value = more / bigger clouds, higher = fewer / smaller.
         */
        private const float CloudThreshold =
            0.40f;

        /*
         * Pixelated edge blur (PS1 style): the border of a cloud fades out
         * over a wide band built from single pixels (dithering).
         *
         * CloudEdgeBlur        - half-width of the band, in cloud pixels.
         *                        14 pixels = about 1.2 world units.
         *                        Keep it below ~24 (see SdfRange).
         * CloudEdgeInner       - how far the blur reaches INTO the cloud, as
         *                        a share of the outside width. 1 = symmetric
         *                        (very foggy), 0.5 = firm silhouette.
         * CloudEdgeAlphaLevels - number of transparency steps in the band.
         *                        1 = pure on/off dissolve, 8 = smooth.
         * CloudEdgeRagged      - 0 = regular Bayer dither (clean grain),
         *                        1 = ragged noise with vertical streaks.
         * CloudEdgeStreak      - length of the vertical streaks, in pixels.
         * CloudEdgeBoilFps     - 0 = static edge, 2-4 = retro "boiling".
         * CloudRainGrow        - how many pixels clouds grow in rain.
         * CloudShadeRows       - height of one shading band, in pixels.
         */
        private const float CloudEdgeBlur =
            14f;

        private const float CloudEdgeInner =
            0.5f;

        private const float CloudEdgeAlphaLevels =
            8f;

        private const float CloudEdgeRagged =
            0.30f;

        private const float CloudEdgeStreak =
            4f;

        private const float CloudEdgeBoilFps =
            0f;

        private const float CloudRainGrow =
            5f;

        private const float CloudShadeRows =
            6f;

        /*
         * Flight animation: world units per second the clouds drift to the
         * RIGHT. This is the speed of the Main band, the other bands scale
         * with their parallax factor (far = slower, storm = faster). It is
         * added on top of the normal parallax movement with the camera.
         * 0 = clouds stand still.
         */
        private const float CloudWindSpeed =
            0.35f;


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

            // How far the band has drifted to the right (world units).
            public float WindOffset;

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

            ConfigureMaterial();

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
                /*
                 * The pixel size stays constant on purpose: the shader
                 * handles rain by itself (storm palette + more coverage).
                 */
                material.SetFloat(
                    "_Rain",
                    rain
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
                0.05f,
                0.06f,
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
                0.06f,
                0.07f,
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
                0.07f,
                0.09f,
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

            /*
             * Flight animation. Nearer bands (bigger parallax factor) drift
             * faster, storm clouds are blown faster still.
             */
            float windSpeed =
                CloudWindSpeed *
                (
                    band.FactorX /
                    0.22f
                )
                *
                Mathf.Lerp(
                    1f,
                    1.6f,
                    rain
                );

            band.WindOffset +=
                windSpeed *
                Time.deltaTime;

            band.Root.position =
                new Vector3(
                    delta.x *
                    (
                        1f -
                        band.FactorX
                    ) +
                    band.WindOffset,

                    delta.y *
                    (
                        1f -
                        band.FactorY
                    ),

                    0f
                );

            /*
             * The band moves right by WindOffset, so the part of the band
             * that is above the camera moves left by the same amount.
             */
            float virtualCenter =
                initialCameraPosition.x +
                delta.x *
                band.FactorX -
                band.WindOffset;

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

            /*
             * Snap to the cloud pixel grid: every cloud of a band then has
             * the same pixel lattice, so overlapping clouds never show
             * half-shifted pixels.
             */
            x = SnapToPixel(x);
            y = SnapToPixel(y);

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
            int width =
                CloudTextureWidth;

            int height =
                CloudTextureHeight;

            float[] density =
                new float[
                    width *
                    height
                ];

            float[] signedDistance =
                new float[
                    width *
                    height
                ];

            float[] scratch =
                new float[
                    width *
                    height
                ];

            for (
                int variant = 0;
                variant < VariantCount;
                variant++
            )
            {
                Texture2D texture =
                    new Texture2D(
                        width,
                        height,
                        TextureFormat.RGBA32,
                        false
                    );

                texture.name =
                    "ProceduralCloudDetailed_" +
                    variant;

                /*
                 * The texture is a smooth distance field. It must be sampled
                 * smoothly (Bilinear), the shader cuts it into pixels itself.
                 */
                texture.filterMode =
                    FilterMode.Bilinear;

                texture.wrapMode =
                    TextureWrapMode.Clamp;

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

                BuildDensity(
                    density,
                    offsetX,
                    offsetY
                );

                BuildSignedDistance(
                    density,
                    signedDistance,
                    scratch
                );

                Color32[] pixels =
                    new Color32[
                        width *
                        height
                    ];

                for (
                    int i = 0;
                    i < pixels.Length;
                    i++
                )
                {
                    /*
                     * 0.5 = on the border, >0.5 inside, <0.5 outside.
                     */
                    float encoded =
                        Mathf.Clamp01(
                            0.5f +
                            signedDistance[i] /
                            (
                                2f *
                                SdfRange
                            )
                        );

                    byte value =
                        (byte)
                        Mathf.RoundToInt(
                            encoded *
                            255f
                        );

                    pixels[i] =
                        new Color32(
                            255,
                            255,
                            255,
                            value
                        );
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

                        SpritePixelsPerUnit,

                        0u,

                        /*
                         * Full Rect: the shader samples the mask around each
                         * pixel, a Tight mesh could clip those samples.
                         */
                        SpriteMeshType.FullRect
                    );

                textures.Add(
                    texture
                );

                sprites.Add(
                    sprite
                );
            }
        }


        /*
         * Cloud body (the same noise as before) in the middle of the texture,
         * empty margin around it. Values are 0..1 density.
         */
        private static void BuildDensity(
            float[] density,
            float offsetX,
            float offsetY
        )
        {
            int index = 0;

            for (
                int y = 0;
                y < CloudTextureHeight;
                y++
            )
            {
                float innerY =
                    y -
                    CloudTexturePad;

                float normalizedY =
                    innerY /
                    (
                        CloudInnerHeight -
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
                        0.55f
                    );

                float sampleY =
                    Mathf.Clamp(
                        innerY,
                        0f,
                        CloudInnerHeight -
                        1f
                    );

                for (
                    int x = 0;
                    x < CloudTextureWidth;
                    x++
                )
                {
                    float innerX =
                        x -
                        CloudTexturePad;

                    float normalizedX =
                        innerX /
                        (
                            CloudInnerWidth -
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
                            0.75f
                        );

                    float sampleX =
                        Mathf.Clamp(
                            innerX,
                            0f,
                            CloudInnerWidth -
                            1f
                        );

                    float n1 =
                        Mathf.PerlinNoise(
                            sampleX * 0.030f + offsetX,
                            sampleY * 0.052f + offsetY
                        );

                    float n2 =
                        Mathf.PerlinNoise(
                            sampleX * 0.061f + offsetY,
                            sampleY * 0.097f + offsetX
                        );

                    float n3 =
                        Mathf.PerlinNoise(
                            sampleX * 0.118f +
                            offsetX * 0.41f,

                            sampleY * 0.171f +
                            offsetY * 0.37f
                        );

                    float n4 =
                        Mathf.PerlinNoise(
                            sampleX * 0.018f +
                            offsetY * 0.23f,

                            sampleY * 0.026f +
                            offsetX * 0.19f
                        );

                    /*
                     * The highest-frequency octave (n3) is weak on
                     * purpose: it would produce lone specks and holes.
                     */
                    float noise =
                        n1 * 0.44f +
                        n2 * 0.27f +
                        n3 * 0.05f +
                        n4 * 0.24f;

                    float alpha =
                        (
                            noise -
                            0.25f
                        )
                        *
                        3.20f;

                    alpha *=
                        verticalMask *
                        edge;

                    density[index++] =
                        Mathf.Clamp01(
                            alpha
                        );
                }
            }
        }


        /*
         * Signed distance (in texels) to the cloud border: positive inside,
         * negative outside. The border is where density == CloudThreshold.
         */
        private static void BuildSignedDistance(
            float[] density,
            float[] signedDistance,
            float[] scratch
        )
        {
            int width =
                CloudTextureWidth;

            int height =
                CloudTextureHeight;

            int count =
                width *
                height;

            const double Far =
                1e20;

            double[] toInside =
                new double[count];

            double[] toOutside =
                new double[count];

            for (
                int i = 0;
                i < count;
                i++
            )
            {
                bool inside =
                    density[i] >
                    CloudThreshold;

                toInside[i] =
                    inside
                    ? 0.0
                    : Far;

                toOutside[i] =
                    inside
                    ? Far
                    : 0.0;
            }

            DistanceTransform2D(
                toInside,
                width,
                height
            );

            DistanceTransform2D(
                toOutside,
                width,
                height
            );

            float limit =
                SdfRange *
                2f;

            for (
                int i = 0;
                i < count;
                i++
            )
            {
                bool inside =
                    density[i] >
                    CloudThreshold;

                float distance =
                    inside
                    ?
                    Mathf.Sqrt(
                        (float)toOutside[i]
                    ) -
                    0.5f
                    :
                    -(
                        Mathf.Sqrt(
                            (float)toInside[i]
                        ) -
                        0.5f
                    );

                signedDistance[i] =
                    Mathf.Clamp(
                        distance,
                        -limit,
                        limit
                    );
            }

            /*
             * Two light blurs remove the texel staircase of the border.
             */
            BoxBlur3(
                signedDistance,
                scratch,
                width,
                height
            );

            BoxBlur3(
                scratch,
                signedDistance,
                width,
                height
            );
        }


        private static void BoxBlur3(
            float[] source,
            float[] target,
            int width,
            int height
        )
        {
            for (
                int y = 0;
                y < height;
                y++
            )
            {
                for (
                    int x = 0;
                    x < width;
                    x++
                )
                {
                    float sum = 0f;

                    for (
                        int oy = -1;
                        oy <= 1;
                        oy++
                    )
                    {
                        int yy =
                            Mathf.Clamp(
                                y + oy,
                                0,
                                height - 1
                            );

                        for (
                            int ox = -1;
                            ox <= 1;
                            ox++
                        )
                        {
                            int xx =
                                Mathf.Clamp(
                                    x + ox,
                                    0,
                                    width - 1
                                );

                            sum +=
                                source[
                                    yy *
                                    width +
                                    xx
                                ];
                        }
                    }

                    target[
                        y *
                        width +
                        x
                    ] =
                        sum /
                        9f;
                }
            }
        }


        /*
         * Exact squared euclidean distance transform
         * (Felzenszwalb & Huttenlocher). 'grid' holds 0 for source pixels and
         * a huge value elsewhere; it is replaced by the squared distance to
         * the nearest source pixel.
         */
        private static void DistanceTransform2D(
            double[] grid,
            int width,
            int height
        )
        {
            int longest =
                Mathf.Max(
                    width,
                    height
                );

            double[] f =
                new double[longest];

            double[] d =
                new double[longest];

            int[] v =
                new int[longest];

            double[] z =
                new double[longest + 1];

            for (
                int x = 0;
                x < width;
                x++
            )
            {
                for (
                    int y = 0;
                    y < height;
                    y++
                )
                {
                    f[y] =
                        grid[
                            y *
                            width +
                            x
                        ];
                }

                DistanceTransform1D(
                    f,
                    d,
                    height,
                    v,
                    z
                );

                for (
                    int y = 0;
                    y < height;
                    y++
                )
                {
                    grid[
                        y *
                        width +
                        x
                    ] =
                        d[y];
                }
            }

            for (
                int y = 0;
                y < height;
                y++
            )
            {
                for (
                    int x = 0;
                    x < width;
                    x++
                )
                {
                    f[x] =
                        grid[
                            y *
                            width +
                            x
                        ];
                }

                DistanceTransform1D(
                    f,
                    d,
                    width,
                    v,
                    z
                );

                for (
                    int x = 0;
                    x < width;
                    x++
                )
                {
                    grid[
                        y *
                        width +
                        x
                    ] =
                        d[x];
                }
            }
        }


        private static void DistanceTransform1D(
            double[] f,
            double[] d,
            int n,
            int[] v,
            double[] z
        )
        {
            int k = 0;

            v[0] = 0;

            z[0] = -1e30;
            z[1] = 1e30;

            for (
                int q = 1;
                q < n;
                q++
            )
            {
                double s =
                    (
                        (f[q] + (double)q * q) -
                        (f[v[k]] + (double)v[k] * v[k])
                    )
                    /
                    (
                        2.0 * q -
                        2.0 * v[k]
                    );

                while (s <= z[k])
                {
                    k--;

                    s =
                        (
                            (f[q] + (double)q * q) -
                            (f[v[k]] + (double)v[k] * v[k])
                        )
                        /
                        (
                            2.0 * q -
                            2.0 * v[k]
                        );
                }

                k++;

                v[k] = q;

                z[k] = s;
                z[k + 1] = 1e30;
            }

            k = 0;

            for (
                int q = 0;
                q < n;
                q++
            )
            {
                while (z[k + 1] < q)
                    k++;

                double dx =
                    q -
                    v[k];

                d[q] =
                    dx * dx +
                    f[v[k]];
            }
        }


        private void ConfigureMaterial()
        {
            if (
                material == null ||
                !customCloudShader
            )
            {
                return;
            }

            material.SetFloat(
                "_PixelSize",
                CloudPixelSize
            );

            material.SetFloat(
                "_SdfRange",
                SdfRange
            );

            material.SetFloat(
                "_EdgeBlur",
                CloudEdgeBlur
            );

            material.SetFloat(
                "_EdgeInner",
                CloudEdgeInner
            );

            material.SetFloat(
                "_EdgeAlphaLevels",
                CloudEdgeAlphaLevels
            );

            material.SetFloat(
                "_EdgeRagged",
                CloudEdgeRagged
            );

            material.SetFloat(
                "_EdgeStreak",
                CloudEdgeStreak
            );

            material.SetFloat(
                "_EdgeBoilFPS",
                CloudEdgeBoilFps
            );

            material.SetFloat(
                "_RainGrow",
                CloudRainGrow
            );

            material.SetFloat(
                "_ShadeRows",
                CloudShadeRows
            );

            /*
             * Texture size in texels. The shader uses it to convert a
             * distance in texels into world units.
             */
            material.SetVector(
                "_TexSize",
                new Vector4(
                    CloudTextureWidth,
                    CloudTextureHeight,
                    0f,
                    0f
                )
            );

            /*
             * Sprite size in object units. The shader uses it to keep
             * the pixels square regardless of the per-cloud scale.
             */
            material.SetVector(
                "_SpriteSize",
                new Vector4(
                    CloudTextureWidth /
                    SpritePixelsPerUnit,

                    CloudTextureHeight /
                    SpritePixelsPerUnit,

                    0f,
                    0f
                )
            );
        }


        private static float SnapToPixel(
            float value
        )
        {
            return
                Mathf.Round(
                    value /
                    CloudPixelSize
                )
                *
                CloudPixelSize;
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
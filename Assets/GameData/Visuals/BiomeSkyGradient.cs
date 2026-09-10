using System;
using System.Collections.Generic;
using UnityEngine;

using Game.World;
using Game.World.Biomes;
using Game.World.Generation;

namespace Game.Visuals
{
    /// <summary>
    /// Full-screen world-space gradient background.
    ///
    /// The sprite is NOT parented to the camera.
    /// Every LateUpdate it follows the camera in world space
    /// and is scaled from the real camera viewport size.
    ///
    /// Overscan guarantees that the gradient remains larger
    /// than the visible screen, including when aspect ratio,
    /// resolution or orthographic size changes.
    /// </summary>
    public sealed class BiomeSkyGradient :
        MonoBehaviour
    {
        [Serializable]
        private sealed class PaletteFile
        {
            public string FallbackTop =
                "#171A2B";

            public string FallbackBottom =
                "#05060B";

            public float TransitionSpeed =
                5f;

            public float Overscan =
                1.35f;

            public Entry[] Biomes;
        }


        [Serializable]
        private sealed class Entry
        {
            public string Biome;

            public string Top;

            public string Bottom;
        }


        private struct GradientColors
        {
            public Color Top;
            public Color Bottom;


            public GradientColors(
                Color top,
                Color bottom
            )
            {
                Top =
                    top;

                Bottom =
                    bottom;
            }
        }


        private readonly Dictionary<
            string,
            GradientColors
        >
        palette =
            new Dictionary<
                string,
                GradientColors
            >(
                StringComparer.OrdinalIgnoreCase
            );


        private GradientColors fallback =
            new GradientColors(
                new Color(
                    0.09f,
                    0.10f,
                    0.17f,
                    1f
                ),
                new Color(
                    0.02f,
                    0.025f,
                    0.045f,
                    1f
                )
            );


        private float transitionSpeed =
            5f;


        // 1.35 = 35% larger than the visible screen.
        private float overscan =
            1.35f;


        private Camera targetCamera;

        private GameObject skyObject;

        private SpriteRenderer skyRenderer;

        private Texture2D gradientTexture;

        private Sprite gradientSprite;


        private Color currentTop;

        private Color currentBottom;

        private Color appliedTop;

        private Color appliedBottom;

        private bool colorsInitialized;

        private bool hasAppliedColors;


        private void Awake()
        {
            DontDestroyOnLoad(
                gameObject
            );


            LoadPalette();

            CreateSkyObject();
        }


        private void OnDestroy()
        {
            DestroyRuntimeSky();
        }


        private void LateUpdate()
        {
            EnsureRuntimeSky();

            FindCamera();


            if (
                targetCamera ==
                null
            )
            {
                if (
                    skyRenderer !=
                    null
                )
                {
                    skyRenderer.enabled =
                        false;
                }


                return;
            }


            skyRenderer.enabled =
                true;


            FitToCamera();

            UpdateBiomeColors();
        }


        // =====================================================
        // PALETTE
        // =====================================================

        private void LoadPalette()
        {
            TextAsset asset =
                UnityEngine.Resources.Load<TextAsset>(
                    "BiomeSkyPalette"
                );


            if (asset == null)
            {
                Debug.LogWarning(
                    "BIOME SKY: Resources/BiomeSkyPalette.json " +
                    "was not found. Using fallback colors."
                );

                return;
            }


            PaletteFile file;


            try
            {
                file =
                    JsonUtility.FromJson<
                        PaletteFile
                    >(
                        asset.text
                    );
            }
            catch (
                Exception exception
            )
            {
                Debug.LogError(
                    "BIOME SKY: failed to parse palette: " +
                    exception.Message
                );

                return;
            }


            if (file == null)
                return;


            fallback =
                new GradientColors(
                    ParseColor(
                        file.FallbackTop,
                        fallback.Top
                    ),
                    ParseColor(
                        file.FallbackBottom,
                        fallback.Bottom
                    )
                );


            transitionSpeed =
                Mathf.Max(
                    0.01f,
                    file.TransitionSpeed
                );


            overscan =
                Mathf.Max(
                    1.05f,
                    file.Overscan
                );


            palette.Clear();


            if (file.Biomes == null)
                return;


            for (
                int i = 0;
                i < file.Biomes.Length;
                i++
            )
            {
                Entry entry =
                    file.Biomes[i];


                if (
                    entry == null ||
                    string.IsNullOrWhiteSpace(
                        entry.Biome
                    )
                )
                {
                    continue;
                }


                string key =
                    entry.Biome.Trim();


                palette[key] =
                    new GradientColors(
                        ParseColor(
                            entry.Top,
                            fallback.Top
                        ),
                        ParseColor(
                            entry.Bottom,
                            fallback.Bottom
                        )
                    );
            }
        }


        // =====================================================
        // RUNTIME SKY
        // =====================================================

        private void EnsureRuntimeSky()
        {
            if (
                skyObject != null &&
                skyRenderer != null &&
                gradientTexture != null &&
                gradientSprite != null
            )
            {
                return;
            }


            CreateSkyObject();
        }


        private void CreateSkyObject()
        {
            DestroyRuntimeSky();


            skyObject =
                new GameObject(
                    "Biome Sky Gradient"
                );


            // Keep generated sky independent from scene camera.
            DontDestroyOnLoad(
                skyObject
            );


            skyRenderer =
                skyObject.AddComponent<
                    SpriteRenderer
                >();


            skyRenderer.sortingOrder =
                short.MinValue;


            gradientTexture =
                new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                );


            gradientTexture.name =
                "Runtime Biome Sky Gradient";


            gradientTexture.filterMode =
                FilterMode.Bilinear;


            gradientTexture.wrapMode =
                TextureWrapMode.Clamp;


            gradientSprite =
                Sprite.Create(
                    gradientTexture,
                    new Rect(
                        0f,
                        0f,
                        2f,
                        2f
                    ),
                    new Vector2(
                        0.5f,
                        0.5f
                    ),
                    1f
                );


            gradientSprite.name =
                "Runtime Biome Sky Sprite";


            skyRenderer.sprite =
                gradientSprite;


            hasAppliedColors =
                false;


            ApplyGradient(
                fallback.Top,
                fallback.Bottom
            );
        }


        private void DestroyRuntimeSky()
        {
            if (gradientSprite != null)
            {
                Destroy(
                    gradientSprite
                );

                gradientSprite =
                    null;
            }


            if (gradientTexture != null)
            {
                Destroy(
                    gradientTexture
                );

                gradientTexture =
                    null;
            }


            if (skyObject != null)
            {
                Destroy(
                    skyObject
                );

                skyObject =
                    null;
            }


            skyRenderer =
                null;
        }


        // =====================================================
        // CAMERA
        // =====================================================

        private void FindCamera()
        {
            Camera main =
                Camera.main;


            if (
                main ==
                targetCamera
            )
            {
                return;
            }


            targetCamera =
                main;
        }


        private void FitToCamera()
        {
            if (
                targetCamera ==
                null ||
                skyObject ==
                null ||
                gradientSprite ==
                null
            )
            {
                return;
            }


            float visibleHeight;
            float visibleWidth;


            // =================================================
            // ORTHOGRAPHIC CAMERA
            // =================================================

            if (
                targetCamera.orthographic
            )
            {
                visibleHeight =
                    targetCamera.orthographicSize *
                    2f;


                visibleWidth =
                    visibleHeight *
                    targetCamera.aspect;
            }

            // =================================================
            // PERSPECTIVE FALLBACK
            // =================================================

            else
            {
                float distance =
                    50f;


                visibleHeight =
                    2f *
                    distance *
                    Mathf.Tan(
                        targetCamera.fieldOfView *
                        0.5f *
                        Mathf.Deg2Rad
                    );


                visibleWidth =
                    visibleHeight *
                    targetCamera.aspect;
            }


            // =================================================
            // OVERSCAN
            // =================================================

            visibleWidth *=
                overscan;


            visibleHeight *=
                overscan;


            Vector2 spriteSize =
                gradientSprite.bounds.size;


            float scaleX =
                visibleWidth /
                Mathf.Max(
                    0.0001f,
                    spriteSize.x
                );


            float scaleY =
                visibleHeight /
                Mathf.Max(
                    0.0001f,
                    spriteSize.y
                );


            // -------------------------------------------------
            // Do NOT parent to camera.
            //
            // This avoids inherited transform scale and fixes
            // the "small square in the middle" problem.
            // -------------------------------------------------

            Vector3 cameraPosition =
                targetCamera.transform.position;


            skyObject.transform.position =
                new Vector3(
                    cameraPosition.x,
                    cameraPosition.y,
                    5f
                );


            skyObject.transform.rotation =
                Quaternion.identity;


            skyObject.transform.localScale =
                new Vector3(
                    scaleX,
                    scaleY,
                    1f
                );
        }


        // =====================================================
        // BIOME COLORS
        // =====================================================

        private void UpdateBiomeColors()
        {
            WorldManager manager =
                WorldManager.Instance;


            if (manager == null)
                return;


            WorldGenerator generator =
                manager.GetGenerator();


            if (generator == null)
                return;


            int worldX =
                Mathf.FloorToInt(
                    targetCamera
                        .transform
                        .position
                        .x
                );


            BiomeSample sample =
                generator.GetBiomeSample(
                    worldX
                );


            GradientColors primary =
                GetColors(
                    sample.Primary
                );


            GradientColors secondary =
                sample.Secondary != null
                    ? GetColors(
                        sample.Secondary
                    )
                    : primary;


            float biomeBlend =
                sample.Secondary != null &&
                sample.Secondary !=
                sample.Primary
                    ? Mathf.Clamp01(
                        sample.Blend
                    )
                    : 0f;


            Color targetTop =
                Color.Lerp(
                    primary.Top,
                    secondary.Top,
                    biomeBlend
                );


            Color targetBottom =
                Color.Lerp(
                    primary.Bottom,
                    secondary.Bottom,
                    biomeBlend
                );


            if (!colorsInitialized)
            {
                currentTop =
                    targetTop;

                currentBottom =
                    targetBottom;

                colorsInitialized =
                    true;


                ApplyGradient(
                    currentTop,
                    currentBottom
                );

                return;
            }


            float t =
                1f -
                Mathf.Exp(
                    -transitionSpeed *
                    Time.unscaledDeltaTime
                );


            currentTop =
                Color.Lerp(
                    currentTop,
                    targetTop,
                    t
                );


            currentBottom =
                Color.Lerp(
                    currentBottom,
                    targetBottom,
                    t
                );


            ApplyGradient(
                currentTop,
                currentBottom
            );
        }


        private GradientColors GetColors(
            BiomeDefinition biome
        )
        {
            if (biome == null)
                return fallback;


            string key =
                biome.DisplayName;


            if (
                string.IsNullOrWhiteSpace(
                    key
                )
            )
            {
                return fallback;
            }


            if (
                palette.TryGetValue(
                    key.Trim(),
                    out GradientColors colors
                )
            )
            {
                return colors;
            }


            return fallback;
        }


        // =====================================================
        // TEXTURE
        // =====================================================

        private void ApplyGradient(
            Color top,
            Color bottom
        )
        {
            if (gradientTexture == null)
                return;


            // Avoid Texture2D.Apply every frame when colors
            // effectively did not change.
            if (
                hasAppliedColors &&
                ColorsAlmostEqual(
                    top,
                    appliedTop
                ) &&
                ColorsAlmostEqual(
                    bottom,
                    appliedBottom
                )
            )
            {
                return;
            }


            gradientTexture.SetPixel(
                0,
                0,
                bottom
            );

            gradientTexture.SetPixel(
                1,
                0,
                bottom
            );

            gradientTexture.SetPixel(
                0,
                1,
                top
            );

            gradientTexture.SetPixel(
                1,
                1,
                top
            );


            gradientTexture.Apply(
                false,
                false
            );


            appliedTop =
                top;


            appliedBottom =
                bottom;


            hasAppliedColors =
                true;
        }


        private bool ColorsAlmostEqual(
            Color a,
            Color b
        )
        {
            const float epsilon =
                0.001f;


            return
                Mathf.Abs(
                    a.r -
                    b.r
                ) <
                epsilon &&

                Mathf.Abs(
                    a.g -
                    b.g
                ) <
                epsilon &&

                Mathf.Abs(
                    a.b -
                    b.b
                ) <
                epsilon &&

                Mathf.Abs(
                    a.a -
                    b.a
                ) <
                epsilon;
        }


        private Color ParseColor(
            string html,
            Color fallbackColor
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    html
                )
            )
            {
                return fallbackColor;
            }


            if (
                ColorUtility.TryParseHtmlString(
                    html,
                    out Color result
                )
            )
            {
                return result;
            }


            Debug.LogWarning(
                "BIOME SKY: invalid color '" +
                html +
                "'."
            );


            return fallbackColor;
        }
    }
}

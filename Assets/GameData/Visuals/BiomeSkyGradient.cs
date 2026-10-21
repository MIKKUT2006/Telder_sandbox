using Game.World;
using Game.World.Biomes;
using Game.World.Generation;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Visuals
{
    public sealed class BiomeSkyGradient : MonoBehaviour
    {
        private static readonly string[] ForbiddenSceneMarkers =
        {
            "menu",
            "mainmenu",
            "main_menu",
            "title",
            "start"
        };

        // =====================================================
        // PS1-СТИЛЬ ДЛЯ СОЛНЦА И ЛУНЫ
        // =====================================================

        // Матрица Байера 4x4 для упорядоченного дизеринга,
        // как в аппаратном дизеринге PS1.
        private static readonly float[] Bayer4x4 =
        {
             0f,  8f,  2f, 10f,
            12f,  4f, 14f,  6f,
             3f, 11f,  1f,  9f,
            15f,  7f, 13f,  5f
        };

        // Палитра солнца: от края к центру (5 тонов).
        private static readonly Color[] SunTones =
        {
            new Color(0.90f, 0.36f, 0.10f, 1f),
            new Color(0.98f, 0.55f, 0.15f, 1f),
            new Color(1.00f, 0.74f, 0.22f, 1f),
            new Color(1.00f, 0.88f, 0.42f, 1f),
            new Color(1.00f, 0.97f, 0.75f, 1f)
        };

        private static readonly Color SunHaloColor =
            new Color(1f, 0.78f, 0.30f, 1f);

        // Палитра луны: от тени к свету (5 тонов).
        private static readonly Color[] MoonTones =
        {
            new Color(0.24f, 0.29f, 0.46f, 1f),
            new Color(0.40f, 0.47f, 0.67f, 1f),
            new Color(0.60f, 0.68f, 0.88f, 1f),
            new Color(0.80f, 0.87f, 1.00f, 1f),
            new Color(0.95f, 0.98f, 1.00f, 1f)
        };

        private static readonly Color MoonHaloColor =
            new Color(0.65f, 0.75f, 1f, 1f);

        // Кратеры луны: x, y — положение, z — радиус
        // (в долях радиуса диска).
        private static readonly Vector3[] MoonCraters =
        {
            new Vector3(-0.35f,  0.30f, 0.22f),
            new Vector3( 0.30f,  0.38f, 0.14f),
            new Vector3( 0.12f, -0.18f, 0.26f),
            new Vector3(-0.42f, -0.35f, 0.12f),
            new Vector3( 0.50f, -0.20f, 0.10f),
            new Vector3(-0.05f,  0.62f, 0.10f)
        };

        // «Моря» луны: крупные тёмные пятна с мягкими краями.
        private static readonly Vector3[] MoonMaria =
        {
            new Vector3(-0.22f,  0.20f, 0.50f),
            new Vector3( 0.28f, -0.05f, 0.38f),
            new Vector3(-0.05f, -0.45f, 0.30f)
        };

        // Свет на луне падает сверху-слева.
        private static readonly Vector3 MoonLightDir =
            new Vector3(-0.45f, 0.5f, 0.74f).normalized;

        [Serializable]
        private sealed class PaletteFile
        {
            public string FallbackTop = "#171A2B";
            public string FallbackBottom = "#05060B";
            public float TransitionSpeed = 5f;
            public float Overscan = 1.35f;
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

            public GradientColors(Color top, Color bottom)
            {
                Top = top;
                Bottom = bottom;
            }
        }

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;
        [SerializeField] private bool logPaletteMissing = true;

        [Header("Celestials")]
        [SerializeField] private float sunSizeMultiplier = 1f;
        [SerializeField] private float moonSizeMultiplier = 1f;
        [SerializeField] private float celestialDistance = 10f;
        [SerializeField] private int celestialSortingOrder = -70;

        [Header("Celestials / PS1 texture")]
        [Tooltip("Виртуальное вертикальное разрешение. " +
                 "Один «пиксель» светила = высота экрана / это значение. " +
                 "Меньше значение — крупнее пиксели.")]
        [SerializeField] private int virtualScreenHeight = 240;

        [Tooltip("Диаметр солнца в виртуальных пикселях.")]
        [SerializeField] private int sunDiameterPixels = 36;

        [Tooltip("Диаметр луны в виртуальных пикселях.")]
        [SerializeField] private int moonDiameterPixels = 29;

        [Tooltip("Ширина дизеринг-ореола вокруг солнца (пиксели).")]
        [Range(0, 8)]
        [SerializeField] private int sunHaloPixels = 5;

        [Tooltip("Ширина дизеринг-ореола вокруг луны (пиксели).")]
        [Range(0, 8)]
        [SerializeField] private int moonHaloPixels = 3;

        [Header("Celestials / Pixel shader (PS1Celestial)")]
        [Tooltip("Насколько сильно размыта текстура: " +
                 "0 — чёткие пиксели, 1 — полностью мягкая.")]
        [Range(0f, 1f)]
        [SerializeField] private float celestialSoftness = 0.35f;

        [Tooltip("Радиус размытия в текселях.")]
        [Range(0.25f, 1.5f)]
        [SerializeField] private float celestialBlurRadius = 0.75f;

        [Tooltip("Число уровней на канал цвета (32 ≈ 15-битный цвет PS1).")]
        [Range(4f, 64f)]
        [SerializeField] private float celestialColorLevels = 32f;

        [Tooltip("Сила экранного дизеринга при квантовании цвета.")]
        [Range(0f, 1f)]
        [SerializeField] private float celestialDither = 1f;

        private readonly Dictionary<string, GradientColors> palette =
            new Dictionary<string, GradientColors>(
                StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> warnedMissing =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        private GradientColors fallback = new GradientColors(
            new Color(0.09f, 0.10f, 0.17f, 1f),
            new Color(0.02f, 0.025f, 0.045f, 1f));

        private float transitionSpeed = 5f;
        private float overscan = 1.35f;

        private Camera targetCamera;

        private GameObject skyObject;
        private SpriteRenderer skyRenderer;
        private Texture2D gradientTexture;
        private Sprite gradientSprite;

        private GameObject sunObject;
        private SpriteRenderer sunRenderer;
        private Texture2D sunTexture;
        private Sprite sunSprite;
        private Material sunMaterial;

        private GameObject moonObject;
        private SpriteRenderer moonRenderer;
        private Texture2D moonTexture;
        private Sprite moonSprite;
        private Material moonMaterial;

        // Ключ параметров, с которыми были собраны текстуры светил.
        // Если параметры изменили в инспекторе, текстуры пересоздаются.
        private int celestialBuildKey;

        private bool warnedShaderMissing;

        private Color currentTop;
        private Color currentBottom;
        private Color appliedTop;
        private Color appliedBottom;

        private bool colorsInitialized;
        private bool hasAppliedColors;

        private string lastWarnOnce;

        private int SunDiameter =>
            Mathf.Max(8, Mathf.RoundToInt(
                sunDiameterPixels * sunSizeMultiplier));

        private int MoonDiameter =>
            Mathf.Max(8, Mathf.RoundToInt(
                moonDiameterPixels * moonSizeMultiplier));

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            LoadPalette();

            if (!IsForbiddenScene(
                SceneManager.GetActiveScene().name))
            {
                CreateSkyObject();
            }
        }

        private void LateUpdate()
        {
            bool forbidden = Game.World.Dimensions.DimensionTravelRuntime.Current.Is3D || IsForbiddenScene(
                SceneManager.GetActiveScene().name);

            if (forbidden)
            {
                if (skyRenderer != null)
                    skyRenderer.enabled = false;

                if (sunRenderer != null)
                    sunRenderer.enabled = false;

                if (moonRenderer != null)
                    moonRenderer.enabled = false;

                return;
            }

            EnsureRuntimeSky();
            FindCamera();

            if (targetCamera == null)
            {
                if (skyRenderer != null)
                    skyRenderer.enabled = false;

                if (sunRenderer != null)
                    sunRenderer.enabled = false;

                if (moonRenderer != null)
                    moonRenderer.enabled = false;

                return;
            }

            skyRenderer.enabled = true;

            FitToCamera();
            UpdateBiomeColors();
            UpdateCelestials();
        }

        private void OnDestroy()
        {
            DestroyRuntimeSky();
        }

        private static bool IsForbiddenScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            string lower = sceneName.ToLowerInvariant();

            for (int i = 0; i < ForbiddenSceneMarkers.Length; i++)
            {
                if (lower.Contains(ForbiddenSceneMarkers[i]))
                    return true;
            }

            return false;
        }

        // =====================================================
        // PALETTE
        // =====================================================

        private void LoadPalette()
        {
            string json = ReadPaletteText();

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning(
                    "[SKY] Палитра не найдена. Используются fallback-цвета.");

                return;
            }

            PaletteFile file;

            try
            {
                file = JsonUtility.FromJson<PaletteFile>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[SKY] Ошибка чтения палитры: " +
                    exception.Message);

                return;
            }

            if (file == null)
            {
                Debug.LogError(
                    "[SKY] Не удалось разобрать BiomeSkyPalette.json.");

                return;
            }

            fallback = new GradientColors(
                ParseColor(file.FallbackTop, fallback.Top),
                ParseColor(file.FallbackBottom, fallback.Bottom));

            transitionSpeed = Mathf.Max(
                0.01f, file.TransitionSpeed);

            overscan = Mathf.Max(1.05f, file.Overscan);

            palette.Clear();

            if (file.Biomes != null)
            {
                for (int i = 0; i < file.Biomes.Length; i++)
                {
                    Entry entry = file.Biomes[i];

                    if (entry == null ||
                        string.IsNullOrWhiteSpace(entry.Biome))
                    {
                        continue;
                    }

                    palette[entry.Biome.Trim()] =
                        new GradientColors(
                            ParseColor(entry.Top, fallback.Top),
                            ParseColor(entry.Bottom, fallback.Bottom));
                }
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[SKY] Загружено биомов: {palette.Count}");
            }
        }

        private string ReadPaletteText()
        {
            TextAsset asset =
                UnityEngine.Resources.Load<TextAsset>("BiomeSkyPalette");

            if (asset != null && !string.IsNullOrEmpty(asset.text))
                return asset.text;

            string[] paths =
            {
                Path.Combine(
                    Game.Content.DataPaths.GameDataFolder,
                    "Resources/BiomeSkyPalette.json"),

                Path.Combine(
                    Game.Content.DataPaths.GameDataFolder,
                    "BiomeSkyPalette.json"),

                Path.Combine(
                    Application.streamingAssetsPath,
                    "GameData/Resources/BiomeSkyPalette.json"),

                Path.Combine(
                    Application.streamingAssetsPath,
                    "GameData/BiomeSkyPalette.json")
            };

            for (int i = 0; i < paths.Length; i++)
            {
                try
                {
                    if (!File.Exists(paths[i]))
                        continue;

                    string text = File.ReadAllText(paths[i]);

                    if (!string.IsNullOrEmpty(text))
                        return text;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"[SKY] Ошибка чтения {paths[i]}: " +
                        exception.Message);
                }
            }

            return null;
        }

        private static Color ParseColor(
            string html,
            Color fallbackColor)
        {
            if (string.IsNullOrWhiteSpace(html))
                return fallbackColor;

            if (ColorUtility.TryParseHtmlString(
                html, out Color result))
            {
                return result;
            }

            Debug.LogWarning(
                $"[SKY] Некорректный цвет: {html}");

            return fallbackColor;
        }

        // =====================================================
        // SKY OBJECT
        // =====================================================

        private void EnsureRuntimeSky()
        {
            if (skyObject == null ||
                skyRenderer == null ||
                gradientTexture == null ||
                gradientSprite == null)
            {
                CreateSkyObject();
                return;
            }

            // Восстанавливаем светила, если их удалил Unity
            // или другой компонент, либо если изменились
            // параметры их внешнего вида.
            if (sunObject == null ||
                moonObject == null ||
                sunRenderer == null ||
                moonRenderer == null ||
                sunSprite == null ||
                moonSprite == null ||
                sunTexture == null ||
                moonTexture == null ||
                celestialBuildKey != ComputeCelestialBuildKey())
            {
                CreateCelestialObjects();
            }
        }

        private void CreateSkyObject()
        {
            DestroyRuntimeSky();

            skyObject = new GameObject("Biome Sky Gradient");
            DontDestroyOnLoad(skyObject);

            skyRenderer = skyObject.AddComponent<SpriteRenderer>();
            skyRenderer.sortingLayerName = "Default";
            skyRenderer.sortingOrder = short.MinValue;

            gradientTexture = new Texture2D(
                2, 2, TextureFormat.RGBA32, false)
            {
                name = "Runtime Biome Sky Gradient",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            gradientSprite = Sprite.Create(
                gradientTexture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f),
                1f);

            gradientSprite.name = "Runtime Biome Sky Sprite";
            skyRenderer.sprite = gradientSprite;

            hasAppliedColors = false;
            colorsInitialized = false;

            ApplyGradient(fallback.Top, fallback.Bottom);
            CreateCelestialObjects();
        }

        private void DestroyRuntimeSky()
        {
            DestroyCelestials();

            if (skyObject != null)
                Destroy(skyObject);

            if (gradientSprite != null)
                Destroy(gradientSprite);

            if (gradientTexture != null)
                Destroy(gradientTexture);

            skyObject = null;
            skyRenderer = null;
            gradientSprite = null;
            gradientTexture = null;
        }

        private void DestroyCelestials()
        {
            if (sunObject != null)
                Destroy(sunObject);

            if (moonObject != null)
                Destroy(moonObject);

            if (sunSprite != null)
                Destroy(sunSprite);

            if (sunTexture != null)
                Destroy(sunTexture);

            if (sunMaterial != null)
                Destroy(sunMaterial);

            if (moonSprite != null)
                Destroy(moonSprite);

            if (moonTexture != null)
                Destroy(moonTexture);

            if (moonMaterial != null)
                Destroy(moonMaterial);

            sunObject = null;
            sunRenderer = null;
            sunSprite = null;
            sunTexture = null;
            sunMaterial = null;

            moonObject = null;
            moonRenderer = null;
            moonSprite = null;
            moonTexture = null;
            moonMaterial = null;
        }

        // =====================================================
        // SUN AND MOON
        // =====================================================

        private int ComputeCelestialBuildKey()
        {
            unchecked
            {
                int key = SunDiameter;
                key = key * 31 + MoonDiameter;
                key = key * 31 + Mathf.Max(0, sunHaloPixels);
                key = key * 31 + Mathf.Max(0, moonHaloPixels);
                return key;
            }
        }

        // Ищем пиксельный шейдер: сначала в Resources
        // (попадает в билд), потом по имени.
        private static Shader FindCelestialShader()
        {
            Shader shader =
                UnityEngine.Resources.Load<Shader>("PS1Celestial");

            if (shader == null)
                shader = Shader.Find("Game/PS1Celestial");

            if (shader != null && shader.isSupported)
                return shader;

            return null;
        }

        private void CreateCelestialObjects()
        {
            // Удаляем старые объекты и ресурсы перед пересозданием.
            DestroyCelestials();

            celestialBuildKey = ComputeCelestialBuildKey();

            int sunHalo = Mathf.Max(0, sunHaloPixels);
            int moonHalo = Mathf.Max(0, moonHaloPixels);

            Shader shader = FindCelestialShader();
            bool useShader = shader != null;

            if (!useShader && !warnedShaderMissing)
            {
                warnedShaderMissing = true;

                Debug.LogWarning(
                    "[SKY] Шейдер PS1Celestial не найден. " +
                    "Положите PS1Celestial.shader в любую папку Resources. " +
                    "Пока используется стандартный материал спрайтов.");
            }

            // С шейдером нужна билинейная фильтрация (шейдер сам
            // делает пиксели чёткими), без шейдера — Point.
            FilterMode filter = useShader
                ? FilterMode.Bilinear
                : FilterMode.Point;

            sunTexture = BuildPixelDisc(
                "Runtime Sun Texture",
                SunDiameter,
                sunHalo,
                SunTones,
                SunHaloColor,
                0.55f,
                false,
                filter);

            moonTexture = BuildPixelDisc(
                "Runtime Moon Texture",
                MoonDiameter,
                moonHalo,
                MoonTones,
                MoonHaloColor,
                0.40f,
                true,
                filter);

            sunSprite = CreatePixelSprite(
                sunTexture, "Runtime Sun Sprite");

            moonSprite = CreatePixelSprite(
                moonTexture, "Runtime Moon Sprite");

            // Объекты являются дочерними persistent-компонента,
            // а не фона. Поэтому масштаб фона не увеличивает
            // размер солнца и луны.
            sunObject = new GameObject("Sky Sun");
            sunObject.transform.SetParent(transform, false);

            sunRenderer = sunObject.AddComponent<SpriteRenderer>();
            sunRenderer.sprite = sunSprite;
            sunRenderer.color = Color.white;
            sunRenderer.sortingLayerName = "Default";
            sunRenderer.sortingOrder = celestialSortingOrder;
            sunRenderer.enabled = true;

            moonObject = new GameObject("Sky Moon");
            moonObject.transform.SetParent(transform, false);

            moonRenderer = moonObject.AddComponent<SpriteRenderer>();
            moonRenderer.sprite = moonSprite;
            moonRenderer.color = Color.white;
            moonRenderer.sortingLayerName = "Default";
            moonRenderer.sortingOrder = celestialSortingOrder;
            moonRenderer.enabled = true;

            if (useShader)
            {
                sunMaterial = new Material(shader)
                {
                    name = "Runtime Sun Material"
                };

                moonMaterial = new Material(shader)
                {
                    name = "Runtime Moon Material"
                };

                sunRenderer.sharedMaterial = sunMaterial;
                moonRenderer.sharedMaterial = moonMaterial;

                ApplyShaderSettings(sunMaterial, sunTexture);
                ApplyShaderSettings(moonMaterial, moonTexture);
            }

            if (verboseLogs)
            {
                Debug.Log(
                    $"[SKY] Солнце и луна созданы. " +
                    $"Sun: {sunTexture.width}x{sunTexture.height}, " +
                    $"Moon: {moonTexture.width}x{moonTexture.height}, " +
                    $"Shader: {useShader}");
            }
        }

        // Передаём параметры в пиксельный шейдер.
        private void ApplyShaderSettings(
            Material material,
            Texture2D texture)
        {
            if (material == null || texture == null)
                return;

            material.SetVector(
                "_TexSize",
                new Vector4(texture.width, texture.height, 0f, 0f));

            material.SetFloat("_Softness", celestialSoftness);
            material.SetFloat("_BlurRadius", celestialBlurRadius);
            material.SetFloat("_ColorLevels", celestialColorLevels);
            material.SetFloat("_Dither", celestialDither);
        }

        // Спрайт с PPU = 1: один тексель = одна world-единица.
        // Потом масштабом выставляем размер «пикселя» целиком.
        private static Sprite CreatePixelSprite(
            Texture2D texture,
            string spriteName)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                1f,
                0,
                SpriteMeshType.FullRect);

            sprite.name = spriteName;
            return sprite;
        }

        // Строит диск из нескольких плоских тонов с дизерингом
        // Байера и зернистостью, с потемнением к краю, и
        // (опционально) ореол из дизеринг-пикселей вокруг диска.
        private static Texture2D BuildPixelDisc(
            string textureName,
            int diameter,
            int halo,
            Color[] tones,
            Color haloColor,
            float haloAlpha,
            bool moon,
            FilterMode filter)
        {
            // Минимум 2 прозрачных пикселя по краям, чтобы размытие
            // в шейдере не «растягивало» край текстуры.
            int pad = Mathf.Max(halo, 2);
            int size = diameter + pad * 2;

            Texture2D texture = new Texture2D(
                size, size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            Color[] pixels = new Color[size * size];

            // Прозрачные пиксели несут цвет ореола, чтобы
            // билинейная фильтрация не давала тёмной каймы.
            Color clear = new Color(
                haloColor.r, haloColor.g, haloColor.b, 0f);

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            float center = (size - 1) * 0.5f;
            float radius = diameter * 0.5f;
            float haloFraction = halo > 0 ? halo / radius : 0f;
            float grain = moon ? 0.10f : 0.06f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float threshold = BayerThreshold(x, y);

                    if (dist <= 1f)
                    {
                        float value = moon
                            ? MoonShade(dx, dy, dist)
                            : SunShade(dist);

                        // Зернистость текстуры.
                        value += (Hash01(x, y) - 0.5f) * grain;

                        // Потемнение к краю диска (внешний пиксель).
                        float edgePixels = (1f - dist) * radius;

                        if (edgePixels < 1.2f)
                            value *= 0.55f;

                        pixels[y * size + x] =
                            PickDitheredTone(tones, value, threshold);
                    }
                    else if (haloFraction > 0f &&
                             dist <= 1f + haloFraction)
                    {
                        float t = 1f - (dist - 1f) / haloFraction;
                        float chance = t * t;

                        if (chance > threshold)
                        {
                            Color c = haloColor;
                            c.a = haloAlpha;
                            pixels[y * size + x] = c;
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            return texture;
        }

        private static float BayerThreshold(int x, int y)
        {
            return (Bayer4x4[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;
        }

        // Детерминированный псевдослучайный шум 0..1.
        private static float Hash01(int x, int y)
        {
            float h = Mathf.Sin(x * 12.9898f + y * 78.233f)
                      * 43758.5453f;

            return h - Mathf.Floor(h);
        }

        // Выбирает один из двух соседних тонов палитры по порогу
        // Байера — получается ступенчатый градиент с дизерингом.
        private static Color PickDitheredTone(
            Color[] tones,
            float value,
            float threshold)
        {
            value = Mathf.Clamp01(value);

            float scaled = value * (tones.Length - 1);

            int lower = Mathf.Min(
                Mathf.FloorToInt(scaled), tones.Length - 2);

            float fraction = scaled - lower;

            int index = lower + (fraction > threshold ? 1 : 0);

            return tones[index];
        }

        // Яркость солнца: центр горячее, к краю темнее.
        private static float SunShade(float dist)
        {
            return 1f - Mathf.Pow(dist, 1.8f) * 0.9f;
        }

        // Освещённость «шара» луны, тёмные «моря» и кратеры
        // со светлым ободком.
        private static float MoonShade(
            float dx,
            float dy,
            float dist)
        {
            float nz = Mathf.Sqrt(
                Mathf.Max(0f, 1f - dist * dist));

            float lambert = Mathf.Clamp01(
                Vector3.Dot(new Vector3(dx, dy, nz), MoonLightDir));

            float value = 0.35f + 0.65f * lambert;

            for (int i = 0; i < MoonMaria.Length; i++)
            {
                Vector3 mare = MoonMaria[i];

                float mx = dx - mare.x;
                float my = dy - mare.y;

                float d = Mathf.Sqrt(mx * mx + my * my) / mare.z;

                if (d < 1f)
                    value -= 0.18f * (1f - d * d);
            }

            for (int i = 0; i < MoonCraters.Length; i++)
            {
                Vector3 crater = MoonCraters[i];

                float cx = dx - crater.x;
                float cy = dy - crater.y;

                float cd = Mathf.Sqrt(cx * cx + cy * cy) / crater.z;

                if (cd <= 1f)
                    value -= 0.28f;
                else if (cd <= 1.3f)
                    value += 0.10f;
            }

            return Mathf.Clamp01(value);
        }

        private void UpdateCelestials()
        {
            if (targetCamera == null ||
                sunObject == null ||
                moonObject == null ||
                sunRenderer == null ||
                moonRenderer == null ||
                sunTexture == null ||
                moonTexture == null)
            {
                return;
            }

            float time = Mathf.Repeat(
                Shader.GetGlobalFloat("_TelderDayTime"), 1f);

            float day = Shader.GetGlobalFloat("_TelderDayFactor");

            // Размеры видимой области камеры.
            float height;
            float width;

            if (targetCamera.orthographic)
            {
                height = targetCamera.orthographicSize * 2f;
                width = height * targetCamera.aspect;
            }
            else
            {
                float distance = celestialDistance;

                height = 2f * distance * Mathf.Tan(
                    targetCamera.fieldOfView * 0.5f *
                    Mathf.Deg2Rad);

                width = height * targetCamera.aspect;
            }

            // Размер одного виртуального «пикселя» в world-единицах.
            float pixelSize =
                height / Mathf.Max(16, virtualScreenHeight);

            // Радиус орбиты — половина размеров камеры.
            float radiusX = width * 0.50f;
            float radiusY = height * 0.50f;

            // Центр орбиты находится в центре камеры.
            Vector3 center =
                targetCamera.transform.position +
                targetCamera.transform.forward * celestialDistance;

            Vector3 right = targetCamera.transform.right;
            Vector3 up = targetCamera.transform.up;
            float angle = -time * Mathf.PI * 2f - Mathf.PI * 0.5f;

            // Солнце.
            float sunX = Mathf.Cos(angle) * radiusX;
            float sunY = Mathf.Sin(angle) * radiusY;

            // Луна находится строго напротив солнца.
            float moonX = -sunX;
            float moonY = -sunY;

            Quaternion rotation = targetCamera.transform.rotation;

            sunObject.transform.rotation = rotation;
            moonObject.transform.rotation = rotation;

            // Движение плавное, без привязки к пиксельной сетке.
            // Чёткость пикселей при субпиксельном сдвиге
            // обеспечивает шейдер PS1Celestial.
            sunObject.transform.position =
                center + right * sunX + up * sunY;

            moonObject.transform.position =
                center + right * moonX + up * moonY;

            // Один тексель текстуры = один виртуальный пиксель.
            sunObject.transform.localScale =
                Vector3.one * pixelSize;

            moonObject.transform.localScale =
                Vector3.one * pixelSize;

            // Параметры пиксельного шейдера (можно крутить
            // в инспекторе прямо во время игры).
            ApplyShaderSettings(sunMaterial, sunTexture);
            ApplyShaderSettings(moonMaterial, moonTexture);

            // Прозрачность (цвета запечены в текстуры).
            float dayVisibility = Mathf.Clamp01(
                (day - 0.25f) / 0.75f);

            float nightVisibility = 1f - dayVisibility;

            sunRenderer.color = new Color(
                1f, 1f, 1f,
                Mathf.Lerp(0.85f, 1f, dayVisibility));

            moonRenderer.color = new Color(
                1f, 1f, 1f,
                Mathf.Lerp(0.85f, 1f, nightVisibility));

            sunRenderer.enabled = true;
            moonRenderer.enabled = true;
        }


        // =====================================================
        // CAMERA
        // =====================================================

        private void FindCamera()
        {
            Camera main = Camera.main;

            if (main != targetCamera)
                targetCamera = main;
        }

        private void FitToCamera()
        {
            if (targetCamera == null ||
                skyObject == null ||
                gradientSprite == null)
            {
                return;
            }

            float visibleHeight;
            float visibleWidth;

            if (targetCamera.orthographic)
            {
                visibleHeight =
                    targetCamera.orthographicSize * 2f;

                visibleWidth =
                    visibleHeight * targetCamera.aspect;
            }
            else
            {
                const float distance = 50f;

                visibleHeight = 2f * distance * Mathf.Tan(
                    targetCamera.fieldOfView * 0.5f *
                    Mathf.Deg2Rad);

                visibleWidth =
                    visibleHeight * targetCamera.aspect;
            }

            visibleWidth *= overscan;
            visibleHeight *= overscan;

            Vector2 spriteSize = gradientSprite.bounds.size;

            float scaleX = visibleWidth /
                Mathf.Max(0.0001f, spriteSize.x);

            float scaleY = visibleHeight /
                Mathf.Max(0.0001f, spriteSize.y);

            // Фон располагается позади светил, но перед камерой
            // при стандартной 2D-камере, направленной по +Z.
            skyObject.transform.position =
                targetCamera.transform.position +
                targetCamera.transform.forward * 20f;

            skyObject.transform.rotation =
                targetCamera.transform.rotation;

            skyObject.transform.localScale =
                new Vector3(scaleX, scaleY, 1f);
        }

        // =====================================================
        // BIOME COLORS
        // =====================================================

        private void UpdateBiomeColors()
        {
            WorldManager manager = WorldManager.Instance;

            if (manager == null)
            {
                WarnOnce("WorldManager.Instance == null");
                return;
            }

            WorldGenerator generator = manager.GetGenerator();

            if (generator == null)
            {
                WarnOnce("generator == null");
                return;
            }

            int worldX = Mathf.FloorToInt(
                targetCamera.transform.position.x);

            BiomeSample sample =
                generator.GetBiomeSample(worldX);

            if (sample.Primary == null)
            {
                WarnOnce("sample.Primary == null");
                return;
            }

            GradientColors primary =
                GetColors(sample.Primary);

            GradientColors secondary =
                sample.Secondary != null
                    ? GetColors(sample.Secondary)
                    : primary;

            float blend =
                sample.Secondary != null &&
                sample.Secondary != sample.Primary
                    ? Mathf.Clamp01(sample.Blend)
                    : 0f;

            Color targetTop =
                Color.Lerp(primary.Top, secondary.Top, blend);

            Color targetBottom =
                Color.Lerp(primary.Bottom, secondary.Bottom, blend);

            if (!colorsInitialized)
            {
                currentTop = targetTop;
                currentBottom = targetBottom;
                colorsInitialized = true;

                ApplyGradient(currentTop, currentBottom);
                return;
            }

            float k = 1f - Mathf.Exp(
                -transitionSpeed * Time.deltaTime);

            currentTop = Color.Lerp(currentTop, targetTop, k);
            currentBottom = Color.Lerp(
                currentBottom, targetBottom, k);

            if (ColorDiff(currentTop, targetTop) < 0.0008f)
                currentTop = targetTop;

            if (ColorDiff(currentBottom, targetBottom) < 0.0008f)
                currentBottom = targetBottom;

            ApplyGradient(currentTop, currentBottom);
        }

        private static float ColorDiff(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) +
                   Mathf.Abs(a.g - b.g) +
                   Mathf.Abs(a.b - b.b);
        }

        private GradientColors GetColors(BiomeDefinition biome)
        {
            if (biome == null)
                return fallback;

            if (TryPalette(biome.ID, out GradientColors colors))
                return colors;

            if (!string.IsNullOrWhiteSpace(biome.ID))
            {
                string id = biome.ID.Trim();
                int colon = id.IndexOf(':');

                if (colon >= 0 &&
                    TryPalette(id.Substring(colon + 1), out colors))
                {
                    return colors;
                }
            }

            if (TryPalette(biome.DisplayName, out colors))
                return colors;

            WarnMissingBiome(biome);
            return fallback;
        }

        private bool TryPalette(
            string key,
            out GradientColors colors)
        {
            colors = default;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            return palette.TryGetValue(key.Trim(), out colors);
        }

        private void WarnMissingBiome(BiomeDefinition biome)
        {
            if (!logPaletteMissing)
                return;

            string id = biome.ID ?? "?";
            string displayName = biome.DisplayName ?? "?";
            string signature = id + "|" + displayName;

            if (!warnedMissing.Add(signature))
                return;

            Debug.LogWarning(
                $"[SKY] Палитра не содержит биом: ID='{id}', " +
                $"DisplayName='{displayName}'.");
        }

        // =====================================================
        // GRADIENT TEXTURE
        // =====================================================

        private void ApplyGradient(Color top, Color bottom)
        {
            if (gradientTexture == null)
                return;

            float dayTime =
                Shader.GetGlobalFloat("_TelderDayTime");

            float dayFactor =
                Shader.GetGlobalFloat("_TelderDayFactor");

            float solar;

            if (dayFactor > 0.0001f)
            {
                solar = Mathf.Clamp01(
                    (dayFactor - 0.26f) / 0.74f);
            }
            else if (dayTime > 0.0001f)
            {
                solar = Mathf.Clamp01(
                    Mathf.Sin(
                        (dayTime - 0.25f) * Mathf.PI * 2f));
            }
            else
            {
                solar = 1f;
            }

            Color nightTop =
                new Color(0.055f, 0.070f, 0.125f, 1f);

            Color nightBottom =
                new Color(0.025f, 0.035f, 0.075f, 1f);

            Color dayTop = Color.Lerp(top, Color.white, 0.05f);
            Color dayBottom = Color.Lerp(
                bottom, Color.white, 0.05f);

            top = Color.Lerp(nightTop, dayTop, solar);
            bottom = Color.Lerp(nightBottom, dayBottom, solar);

            if (hasAppliedColors &&
                ColorsAlmostEqual(top, appliedTop) &&
                ColorsAlmostEqual(bottom, appliedBottom))
            {
                return;
            }

            gradientTexture.SetPixel(0, 0, bottom);
            gradientTexture.SetPixel(1, 0, bottom);
            gradientTexture.SetPixel(0, 1, top);
            gradientTexture.SetPixel(1, 1, top);

            gradientTexture.Apply(false, false);

            appliedTop = top;
            appliedBottom = bottom;
            hasAppliedColors = true;
        }

        private static bool ColorsAlmostEqual(Color a, Color b)
        {
            const float epsilon = 0.0008f;

            return Mathf.Abs(a.r - b.r) < epsilon &&
                   Mathf.Abs(a.g - b.g) < epsilon &&
                   Mathf.Abs(a.b - b.b) < epsilon &&
                   Mathf.Abs(a.a - b.a) < epsilon;
        }

        // =====================================================
        // LOGGING
        // =====================================================

        private void WarnOnce(string message)
        {
            if (lastWarnOnce == message)
                return;

            lastWarnOnce = message;

            Debug.LogWarning("[SKY] " + message);
        }
    }
}
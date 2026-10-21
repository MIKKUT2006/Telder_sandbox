using System;
using UnityEngine;

using Game.World.Biomes;
using Game.World.Collision;
using Game.World.Generation;

namespace Game.World.Weather
{
    public class WeatherManager : MonoBehaviour
    {
        public static WeatherManager Instance { get; private set; }

        [SerializeField]
        private Camera targetCamera;

        private Transform player;
        private WorldGenerator generator;
        private WorldCollision worldCollision;
        private WeatherParticleRenderer particleRenderer;
        private PixelFogRenderer fogRenderer;
        private float dayNormalized = 0.32f;
        private float dayLengthSeconds = 720f;
        private bool freezeDayTime;
        private float timeSaveClock;
        private string timeKey;

        private WeatherType currentWeather = WeatherType.Clear;
        private WeatherControlMode controlMode = WeatherControlMode.Auto;

        private BiomeDefinition currentBiome;

        private float weatherTimer;
        private float biomeCheckTimer;

        private const float BiomeCheckInterval = 0.25f;

        private float debugIntensity = 1f;
        private float rainTiltDegrees;

        public WeatherType CurrentWeather => currentWeather;
        public WeatherControlMode ControlMode => controlMode;
        public float RainTiltDegrees => rainTiltDegrees;
        public float DebugIntensity => debugIntensity;
        public float SecondsUntilWeatherChange => Mathf.Max(0f, weatherTimer);
        public float NormalizedDayTime => dayNormalized;
        public float DayLengthSeconds => dayLengthSeconds;
        public bool FreezeDayTime => freezeDayTime;
        public void SetDayTime(float time) { dayNormalized = Mathf.Repeat(time, 1f); ApplyDayNight(); }
        public void SetDayLength(float seconds) { dayLengthSeconds = Mathf.Clamp(seconds, 60f, 3600f); }
        public void SetFreezeDayTime(bool freeze) { freezeDayTime = freeze; }
        public void ForceNextWeather() { controlMode = WeatherControlMode.Auto; weatherTimer = 0f; TryStartBiomeWeather(true); }

        public string CurrentBiomeId =>
            currentBiome != null
                ? currentBiome.ID
                : "none";

        private void Awake()
        {
            Instance = this;
            CreateParticleRenderer();
        }

        private void OnDestroy()
        {
            StoreDayTime();
            if (Instance == this) Instance = null;
            Shader.SetGlobalFloat("_TelderDayFactor", 1f);
        }

        public void Initialize(
            Transform player,
            WorldGenerator generator,
            WorldCollision worldCollision
        )
        {
            this.player = player;
            this.generator = generator;
            // Per-save + per-dimension key, not shared between different worlds.
            string worldId = Game.Save.SaveGameRuntime.HasActiveSave ? Game.Save.SaveGameRuntime.CurrentSaveId : "editor";
            var dimension = Game.World.Dimensions.DimensionTravelRuntime.Current;
            timeKey = "Telder.DayTime." + worldId + "." + (dimension != null ? dimension.Name : "default");
            dayNormalized = PlayerPrefs.GetFloat(timeKey, .32f);
            this.worldCollision = worldCollision;

            if (targetCamera == null)
                targetCamera = Camera.main;

            InitializeRenderer();
            if (fogRenderer == null) fogRenderer = gameObject.GetComponent<PixelFogRenderer>();
            if (fogRenderer == null) fogRenderer = gameObject.AddComponent<PixelFogRenderer>();
            fogRenderer.Initialize(targetCamera, generator);
            ApplyDayNight();

            RefreshBiome(true);

            SetCurrentWeather(
                WeatherType.Clear,
                0f,
                true
            );

            ScheduleClearPhase();
        }

        private void CreateParticleRenderer()
        {
            if (particleRenderer != null)
                return;

            GameObject particleObject =
                new GameObject("WeatherParticles");

            particleObject.transform.SetParent(
                transform,
                false
            );

            particleObject.transform.position =
                Vector3.zero;

            particleObject.transform.rotation =
                Quaternion.identity;

            particleObject.transform.localScale =
                Vector3.one;

            particleRenderer =
                particleObject.AddComponent<
                    WeatherParticleRenderer
                >();
        }

        private void InitializeRenderer()
        {
            if (particleRenderer == null)
                CreateParticleRenderer();

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (
                particleRenderer == null ||
                targetCamera == null
            )
                return;

            particleRenderer.Initialize(
                targetCamera,
                worldCollision,
                generator
            );
        }

        private void Update()
        {
            if (!freezeDayTime && dayLengthSeconds > 0f)
                dayNormalized = Mathf.Repeat(dayNormalized + Time.deltaTime / dayLengthSeconds, 1f);
            ApplyDayNight();
            timeSaveClock += Time.deltaTime;
            if (timeSaveClock >= 30f) { timeSaveClock = 0f; StoreDayTime(); }
            if (
                player == null ||
                generator == null ||
                worldCollision == null
            )
                return;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                InitializeRenderer();
                if (fogRenderer != null) fogRenderer.Initialize(targetCamera, generator);
            }

            biomeCheckTimer -= Time.deltaTime;

            if (biomeCheckTimer <= 0f)
            {
                biomeCheckTimer =
                    BiomeCheckInterval;

                RefreshBiome(false);
            }

            if (
                controlMode !=
                WeatherControlMode.Auto
            )
                return;

            UpdateAutomaticWeather();
        }

        private void RefreshBiome(bool force)
        {
            int worldX =
                Mathf.FloorToInt(
                    player.position.x
                );

            BiomeDefinition biome =
                generator.GetDominantBiome(
                    worldX
                );

            if (
                !force &&
                biome == currentBiome
            )
                return;

            BiomeDefinition oldBiome =
                currentBiome;

            currentBiome = biome;

            if (
                controlMode !=
                WeatherControlMode.Auto
            )
                return;

            if (oldBiome == currentBiome)
                return;

            WeatherType allowedWeather =
                GetBiomeWeatherType();

            if (
                currentWeather != WeatherType.Clear &&
                currentWeather != allowedWeather && currentWeather != WeatherType.Fog
            )
            {
                SetCurrentWeather(
                    WeatherType.Clear,
                    0f,
                    true
                );

                ScheduleClearPhase();
            }
        }

        private void StoreDayTime()
        {
            if (string.IsNullOrWhiteSpace(timeKey)) return;
            PlayerPrefs.SetFloat(timeKey, dayNormalized);
        }

        private void ApplyDayNight()
        {
            // No per-block rebuild: the existing lit shader receives a single global multiplier.
            float solar = Mathf.Clamp01(Mathf.Sin((dayNormalized - .25f) * Mathf.PI * 2f));
            solar = solar * solar * (3f - 2f * solar);
            float dayFactor = Mathf.Lerp(.26f, 1f, solar);
            Shader.SetGlobalFloat("_TelderDayFactor", dayFactor);
            Shader.SetGlobalFloat("_TelderDayTime", dayNormalized);

            if (targetCamera != null)
            {
                // Approximate day/night sky darkening for worlds that currently use a flat background.
                Color nightBottom = new Color(0.05f, 0.06f, 0.10f, 1f);
                Color dayBottom = new Color(0.63f, 0.52f, 0.42f, 1f);
                Color sky = Color.Lerp(nightBottom, dayBottom, solar);
                if (currentWeather == WeatherType.Fog)
                    sky = Color.Lerp(sky, new Color(0.68f, 0.70f, 0.72f, 1f), 0.18f);
                else if (currentWeather == WeatherType.Rain || currentWeather == WeatherType.Snow)
                    sky = Color.Lerp(sky, new Color(0.36f, 0.40f, 0.48f, 1f), 0.16f);
                targetCamera.backgroundColor = sky;
            }
        }

        private void UpdateAutomaticWeather()
        {
            weatherTimer -=
                Time.deltaTime;

            if (weatherTimer > 0f)
                return;

            if (
                currentWeather !=
                WeatherType.Clear
            )
            {
                SetCurrentWeather(
                    WeatherType.Clear,
                    0f,
                    true
                );

                ScheduleClearPhase();
                return;
            }

            TryStartBiomeWeather(false);
        }

        private void TryStartBiomeWeather(bool force)
        {
            BiomeWeatherSettings settings =
                GetBiomeWeatherSettings();

            if (settings == null)
            {
                ScheduleClearPhase();
                return;
            }

            WeatherType precipitationType = GetBiomeWeatherType();
            bool hasPrecipitation = precipitationType == WeatherType.Rain || precipitationType == WeatherType.Snow;
            float precipitationChance = Mathf.Clamp01(settings.Chance);
            float fogChance = Mathf.Clamp01(settings.FogChance);
            float eventChance = Mathf.Clamp01(Mathf.Max(precipitationChance, fogChance));

            if (!force && UnityEngine.Random.value > eventChance)
            {
                ScheduleClearPhase();
                return;
            }

            WeatherType type = WeatherType.Clear;
            if (force)
            {
                type = hasPrecipitation ? precipitationType : (fogChance > 0f ? WeatherType.Fog : WeatherType.Clear);
            }
            else
            {
                float fogWeight = fogChance;
                float precipitationWeight = hasPrecipitation ? Mathf.Max(0.01f, precipitationChance) : 0f;
                float total = fogWeight + precipitationWeight;
                if (total <= 0f)
                {
                    ScheduleClearPhase();
                    return;
                }

                float roll = UnityEngine.Random.value * total;
                type = roll < fogWeight ? WeatherType.Fog : precipitationType;
            }

            if (type == WeatherType.Clear)
            {
                ScheduleClearPhase();
                return;
            }

            float intensity = type == WeatherType.Fog
                ? Mathf.Clamp(settings.FogIntensity, 0.05f, 1.5f)
                : Mathf.Clamp(settings.Intensity, 0f, 1.5f);

            SetCurrentWeather(type, intensity, true);

            float min = Mathf.Max(1f, settings.MinDuration);
            float max = Mathf.Max(min, settings.MaxDuration);
            weatherTimer = UnityEngine.Random.Range(min, max);
        }

        private void ScheduleClearPhase()
        {
            BiomeWeatherSettings settings =
                GetBiomeWeatherSettings();

            if (settings == null)
            {
                weatherTimer =
                    UnityEngine.Random.Range(
                        60f,
                        120f
                    );

                return;
            }

            float min =
                Mathf.Max(
                    1f,
                    settings.MinClearDuration
                );

            float max =
                Mathf.Max(
                    min,
                    settings.MaxClearDuration
                );

            weatherTimer =
                UnityEngine.Random.Range(
                    min,
                    max
                );
        }

        private BiomeWeatherSettings GetBiomeWeatherSettings()
        {
            if (currentBiome == null)
                return null;

            return currentBiome.Weather;
        }

        private WeatherType GetBiomeWeatherType()
        {
            BiomeWeatherSettings settings =
                GetBiomeWeatherSettings();

            if (
                settings == null ||
                string.IsNullOrWhiteSpace(
                    settings.Precipitation
                )
            )
            {
                return WeatherType.Rain;
            }

            string value =
                settings.Precipitation.Trim();

            if (
                string.Equals(
                    value,
                    "Rain",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return WeatherType.Rain;
            }

            if (string.Equals(value, "Fog", StringComparison.OrdinalIgnoreCase))
                return WeatherType.Fog;

            if (
                string.Equals(
                    value,
                    "Snow",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return WeatherType.Snow;
            }

            return WeatherType.Clear;
        }

        private void SetCurrentWeather(
            WeatherType type,
            float intensity,
            bool newWeatherEvent
        )
        {
            currentWeather = type;

            if (
                type == WeatherType.Rain &&
                newWeatherEvent
            )
            {
                rainTiltDegrees =
                    UnityEngine.Random.Range(
                        -18f,
                        18f
                    );
            }

            if (fogRenderer != null)
            {
                string hex = GetBiomeWeatherSettings() != null ? GetBiomeWeatherSettings().FogColor : "#AFBDCA";
                Color fogColor;
                if (!ColorUtility.TryParseHtmlString(hex, out fogColor)) fogColor = new Color(.69f,.74f,.8f);
                fogRenderer.SetFog(type == WeatherType.Fog, fogColor, intensity);
            }
            if (particleRenderer != null)
            {
                particleRenderer.SetWeather(
                    type,
                    intensity,
                    rainTiltDegrees,
                    newWeatherEvent
                );
            }
        }

        public void SetControlMode(
            WeatherControlMode mode
        )
        {
            controlMode = mode;

            switch (mode)
            {
                case WeatherControlMode.Auto:
                {
                    RefreshBiome(true);

                    SetCurrentWeather(
                        WeatherType.Clear,
                        0f,
                        true
                    );

                    weatherTimer = 0f;
                    break;
                }

                case WeatherControlMode.Clear:
                {
                    SetCurrentWeather(
                        WeatherType.Clear,
                        0f,
                        true
                    );
                    break;
                }

                case WeatherControlMode.Rain:
                {
                    SetCurrentWeather(
                        WeatherType.Rain,
                        debugIntensity,
                        true
                    );
                    break;
                }

                case WeatherControlMode.Fog:
                {
                    SetCurrentWeather(WeatherType.Fog, debugIntensity, true);
                    break;
                }
                case WeatherControlMode.Snow:
                {
                    SetCurrentWeather(
                        WeatherType.Snow,
                        debugIntensity,
                        true
                    );
                    break;
                }
            }
        }

        public void SetDebugIntensity(
            float value
        )
        {
            debugIntensity =
                Mathf.Clamp(
                    value,
                    0.1f,
                    1.5f
                );

            if (
                controlMode ==
                WeatherControlMode.Rain
            )
            {
                SetCurrentWeather(
                    WeatherType.Rain,
                    debugIntensity,
                    false
                );
            }
            else if (controlMode == WeatherControlMode.Fog)
            {
                SetCurrentWeather(WeatherType.Fog, debugIntensity, false);
            }
            else if (
                controlMode ==
                WeatherControlMode.Snow
            )
            {
                SetCurrentWeather(
                    WeatherType.Snow,
                    debugIntensity,
                    false
                );
            }
        }
    }
}

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
            if (Instance == this)
                Instance = null;
        }

        public void Initialize(
            Transform player,
            WorldGenerator generator,
            WorldCollision worldCollision
        )
        {
            this.player = player;
            this.generator = generator;
            this.worldCollision = worldCollision;

            if (targetCamera == null)
                targetCamera = Camera.main;

            InitializeRenderer();

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
                currentWeather != allowedWeather
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

            TryStartBiomeWeather();
        }

        private void TryStartBiomeWeather()
        {
            BiomeWeatherSettings settings =
                GetBiomeWeatherSettings();

            if (settings == null)
            {
                ScheduleClearPhase();
                return;
            }

            WeatherType type =
                GetBiomeWeatherType();

            if (type == WeatherType.Clear)
            {
                ScheduleClearPhase();
                return;
            }

            float chance =
                Mathf.Clamp01(
                    settings.Chance
                );

            if (
                UnityEngine.Random.value >
                chance
            )
            {
                ScheduleClearPhase();
                return;
            }

            float intensity =
                Mathf.Clamp(
                    settings.Intensity,
                    0f,
                    1.5f
                );

            SetCurrentWeather(
                type,
                intensity,
                true
            );

            float min =
                Mathf.Max(
                    1f,
                    settings.MinDuration
                );

            float max =
                Mathf.Max(
                    min,
                    settings.MaxDuration
                );

            weatherTimer =
                UnityEngine.Random.Range(
                    min,
                    max
                );
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
                return WeatherType.Clear;
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

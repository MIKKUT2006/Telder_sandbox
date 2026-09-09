using System.Collections.Generic;
using UnityEngine;

using Game.World.Generation;

namespace Game.World.Parallax
{
    public class ParallaxWorldController :
        MonoBehaviour
    {
        public static ParallaxWorldController Instance
        {
            get;
            private set;
        }


        [SerializeField]
        private Camera worldCamera;


        private Transform terrainRoot;
        private Transform cloudRoot;


        private readonly List<ParallaxTerrainLayer>
            terrainLayers =
            new List<ParallaxTerrainLayer>();


        private ProceduralCloudSystem clouds;

        private bool initialized;


        private void Awake()
        {
            if (
                Instance != null &&
                Instance != this
            )
            {
                Destroy(
                    gameObject
                );

                return;
            }

            Instance = this;
        }


        private void Start()
        {
            TryInitialize();
        }


        private void Update()
        {
            if (!initialized)
            {
                TryInitialize();
                return;
            }

            for (
                int i = 0;
                i < terrainLayers.Count;
                i++
            )
            {
                terrainLayers[i]
                    .Tick();
            }

            if (clouds != null)
            {
                clouds.Tick(
                    ParallaxWeatherState
                        .RainIntensity
                );
            }
        }


        private void TryInitialize()
        {
            if (initialized)
                return;


            /*
             * V10.5:
             *
             * Never capture camera/player position while the player is
             * still temporarily at 0,0 inside caves.
             *
             * NotifyPlayerSpawned() is called by WorldManager.SpawnPlayer().
             * CanInitializeVisuals becomes true on the NEXT frame, after
             * the camera has had a LateUpdate to follow the surface spawn.
             */
            if (
                !DimensionSpawnCurtain
                    .CanInitializeVisuals
            )
            {
                return;
            }


            WorldManager manager =
                WorldManager.Instance;

            if (manager == null)
                return;

            WorldSettings settings =
                manager.GetSettings();

            WorldGenerator generator =
                manager.GetGenerator();

            if (
                settings == null ||
                generator == null
            )
            {
                return;
            }

            if (worldCamera == null)
            {
                worldCamera =
                    Camera.main;
            }

            if (worldCamera == null)
            {
                Camera[] cameras =
                    FindObjectsByType<Camera>(
                        FindObjectsSortMode.None
                    );

                if (cameras.Length > 0)
                {
                    worldCamera =
                        cameras[0];
                }
            }

            if (worldCamera == null)
                return;

            CreateRoots();

            CreateTerrainLayers(
                settings,
                generator
            );

            CreateClouds(
                settings
            );


            /*
             * Build the center and nearest left/right terrain chunks while
             * the black curtain is still covering the screen.
             *
             * Clouds are also instantiated immediately.
             */
            PrimeInitialVisuals();


            initialized = true;


            DimensionSpawnCurtain
                .NotifyParallaxReady();


            Debug.Log(
                "PARALLAX V10.5: rebuilt for dimension and initialized after surface spawn."
            );
        }


        private void PrimeInitialVisuals()
        {
            for (
                int i = 0;
                i < terrainLayers.Count;
                i++
            )
            {
                /*
                 * ParallaxTerrainLayer normally creates one missing
                 * visual chunk per Tick().
                 *
                 * Three iterations create:
                 * center
                 * left neighbour
                 * right neighbour
                 *
                 * before the scene becomes visible.
                 */
                terrainLayers[i]
                    .Prime(
                        3
                    );
            }


            if (clouds != null)
            {
                /*
                 * One cloud Tick creates all currently visible cloud tiles
                 * for every cloud band.
                 */
                clouds.Tick(
                    ParallaxWeatherState
                        .RainIntensity
                );
            }
        }


        private void CreateRoots()
        {
            GameObject terrain =
                new GameObject(
                    "ProceduralParallaxTerrain"
                );

            terrain.transform.SetParent(
                transform,
                false
            );

            terrainRoot =
                terrain.transform;


            GameObject cloud =
                new GameObject(
                    "ProceduralParallaxClouds"
                );

            cloud.transform.SetParent(
                transform,
                false
            );

            cloudRoot =
                cloud.transform;
        }


        private void CreateTerrainLayers(
            WorldSettings settings,
            WorldGenerator generator
        )
        {
            Shader shader =
                Shader.Find(
                    "Game/ParallaxBlockUnlit"
                );

            if (
                shader == null ||
                !shader.isSupported
            )
            {
                Debug.LogError(
                    "PARALLAX: block shader not found/supported."
                );

                return;
            }

            ParallaxLayerSettings[] layers =
                CreateDefaultLayers();

            for (
                int i = 0;
                i < layers.Length;
                i++
            )
            {
                ParallaxLayerSettings layer =
                    layers[i];

                Material material =
                    new Material(
                        shader
                    );

                material.name =
                    "Parallax_" +
                    layer.Name;

                material.SetColor(
                    "_Tint",
                    layer.Tint
                );

                terrainLayers.Add(
                    new ParallaxTerrainLayer(
                        terrainRoot,
                        worldCamera,
                        settings,
                        generator,
                        layer,
                        material
                    )
                );
            }
        }


        private void CreateClouds(
            WorldSettings settings
        )
        {
            Shader shader =
                Shader.Find(
                    "Game/PS1PixelCloud"
                );

            if (
                shader == null ||
                !shader.isSupported
            )
            {
                Debug.LogError(
                    "CLOUDS: Game/PS1PixelCloud unavailable. Using Sprites/Default fallback."
                );

                shader =
                    Shader.Find(
                        "Sprites/Default"
                    );
            }

            if (shader == null)
                return;

            DimensionAtmosphereProfile atmosphere =
                DimensionAtmosphereProfile
                    .Create();

            Material material =
                new Material(
                    shader
                );

            material.name =
                "Procedural PS1 Clouds";

            if (
                material.HasProperty(
                    "_CloudColor"
                )
            )
            {
                material.SetColor(
                    "_CloudColor",
                    atmosphere.CloudColor
                );
            }

            if (
                material.HasProperty(
                    "_StormColor"
                )
            )
            {
                material.SetColor(
                    "_StormColor",
                    atmosphere.StormCloudColor
                );
            }

            SetFloatIfExists(material, "_Rain", 0f);
            SetFloatIfExists(material, "_PixelGridX", 88f);
            SetFloatIfExists(material, "_PixelGridY", 38f);
            SetFloatIfExists(material, "_BlurRadius", 1.05f);
            SetFloatIfExists(material, "_DistortionStrength", 0.55f);
            SetFloatIfExists(material, "_DistortionScale", 8f);
            SetFloatIfExists(material, "_Density", 1.42f);
            SetFloatIfExists(material, "_DitherStrength", 0.035f);
            SetFloatIfExists(material, "_JitterStrength", 0.10f);

            clouds =
                new ProceduralCloudSystem(
                    cloudRoot,
                    worldCamera,
                    settings,
                    material
                );
        }


        private static void SetFloatIfExists(
            Material material,
            string property,
            float value
        )
        {
            if (
                material != null &&
                material.HasProperty(
                    property
                )
            )
            {
                material.SetFloat(
                    property,
                    value
                );
            }
        }


        private ParallaxLayerSettings[]
            CreateDefaultLayers()
        {
            /*
             * V10.4
             * -----
             *
             * OLD block scales:
             *      Far  0.52
             *      Mid  0.68
             *      Near 0.84
             *
             * NEW:
             *      Far  0.20  (~2.6x smaller)
             *      Mid  0.27  (~2.5x smaller)
             *      Near 0.34  (~2.5x smaller)
             *
             * OLD vertical offsets:
             *      -68 / -52 / -38
             *
             * NEW:
             *      -92 / -76 / -60
             *
             * VisibleDepth also prevents the deep stone body from
             * becoming a huge wall behind the playable surface.
             */
            return
                new ParallaxLayerSettings[]
                {
                    new ParallaxLayerSettings(
                        "Far",
                        0.13f,
                        0.07f,
                        0.20f,
                        11f,
                        0.0020f,
                        3f,
                        0.012f,
                        3,
                        -58,
                        new Color(
                            0.31f,
                            0.34f,
                            0.40f,
                            0.72f
                        ),
                        -92f,
                        18
                    ),

                    new ParallaxLayerSettings(
                        "Mid",
                        0.31f,
                        0.12f,
                        0.27f,
                        9f,
                        0.0032f,
                        2.7f,
                        0.017f,
                        4,
                        -42,
                        new Color(
                            0.46f,
                            0.49f,
                            0.56f,
                            0.80f
                        ),
                        -76f,
                        15
                    ),

                    new ParallaxLayerSettings(
                        "Near",
                        0.60f,
                        0.22f,
                        0.34f,
                        7f,
                        0.0052f,
                        2.2f,
                        0.024f,
                        4,
                        -26,
                        new Color(
                            0.61f,
                            0.63f,
                            0.68f,
                            0.88f
                        ),
                        -60f,
                        12
                    )
                };
        }


        private void OnDestroy()
        {
            for (
                int i = 0;
                i < terrainLayers.Count;
                i++
            )
            {
                terrainLayers[i]
                    .Dispose();
            }

            terrainLayers.Clear();

            if (clouds != null)
            {
                clouds.Dispose();
                clouds = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
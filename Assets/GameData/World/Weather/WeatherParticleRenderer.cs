using System.Collections.Generic;
using UnityEngine;

using Game.World.Collision;
using Game.World.Generation;

namespace Game.World.Weather
{
    [RequireComponent(
        typeof(MeshFilter),
        typeof(MeshRenderer)
    )]
    public class WeatherParticleRenderer :
        MonoBehaviour
    {
        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;

            public float Width;
            public float Length;

            public float Phase;
            public float Frequency;

            public Color32 Color;
        }

        private const int MaxParticles = 450;
        private const int BaseRainParticles = 280;
        private const int BaseSnowParticles = 180;

        private Camera targetCamera;
        private WorldCollision worldCollision;
        private WorldGenerator generator;

        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private Material material;

        private readonly List<Vector3> vertices =
            new List<Vector3>(
                MaxParticles * 4
            );

        private readonly List<Vector2> uvs =
            new List<Vector2>(
                MaxParticles * 4
            );

        private readonly List<Color32> colors =
            new List<Color32>(
                MaxParticles * 4
            );

        private readonly List<int> triangles =
            new List<int>(
                MaxParticles * 6
            );

        private readonly Particle[] particles =
            new Particle[
                MaxParticles
            ];

        private int activeParticles;

        private WeatherType weatherType =
            WeatherType.Clear;

        private float intensity = 1f;
        private float rainTiltDegrees;

        public void Initialize(
            Camera targetCamera,
            WorldCollision worldCollision,
            WorldGenerator generator
        )
        {
            this.targetCamera =
                targetCamera;

            this.worldCollision =
                worldCollision;

            this.generator =
                generator;

            CreateRenderResources();
        }

        private void Awake()
        {
            CreateRenderResources();
        }

        private void CreateRenderResources()
        {
            if (mesh == null)
            {
                mesh =
                    new Mesh();

                mesh.name =
                    "WeatherParticleMesh";

                mesh.MarkDynamic();

                GetComponent<MeshFilter>()
                    .sharedMesh =
                    mesh;
            }

            if (meshRenderer == null)
            {
                meshRenderer =
                    GetComponent<
                        MeshRenderer
                    >();
            }

            if (material == null)
            {
                Shader shader =
                    Shader.Find(
                        "Universal Render Pipeline/2D/Sprite-Unlit-Default"
                    );

                if (shader == null)
                {
                    shader =
                        Shader.Find(
                            "Sprites/Default"
                        );
                }

                if (shader == null)
                {
                    Debug.LogError(
                        "WEATHER: Sprite shader not found."
                    );

                    return;
                }

                material =
                    new Material(
                        shader
                    );

                material.name =
                    "WeatherRuntimeMaterial";

                material.mainTexture =
                    Texture2D.whiteTexture;

                meshRenderer.sharedMaterial =
                    material;

                meshRenderer.sortingOrder =
                    5000;
            }
        }

        private void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);

            if (material != null)
                Destroy(material);
        }

        public void SetWeather(
            WeatherType type,
            float intensity,
            float rainTiltDegrees,
            bool restart
        )
        {
            bool changed =
                weatherType != type;

            this.weatherType =
                type;

            this.intensity =
                Mathf.Clamp(
                    intensity,
                    0f,
                    1.5f
                );

            this.rainTiltDegrees =
                Mathf.Clamp(
                    rainTiltDegrees,
                    -30f,
                    30f
                );

            switch (type)
            {
                case WeatherType.Rain:
                    activeParticles =
                        Mathf.Clamp(
                            Mathf.RoundToInt(
                                BaseRainParticles *
                                this.intensity
                            ),
                            0,
                            MaxParticles
                        );
                    break;

                case WeatherType.Snow:
                    activeParticles =
                        Mathf.Clamp(
                            Mathf.RoundToInt(
                                BaseSnowParticles *
                                this.intensity
                            ),
                            0,
                            MaxParticles
                        );
                    break;

                default:
                    activeParticles = 0;
                    break;
            }

            if (
                changed ||
                restart
            )
            {
                ResetParticles();
            }

            if (activeParticles == 0)
                ClearMesh();
        }

        private void ResetParticles()
        {
            if (targetCamera == null)
                return;

            for (
                int i = 0;
                i < activeParticles;
                i++
            )
            {
                RespawnParticle(
                    i,
                    true
                );
            }
        }

        private void LateUpdate()
        {
            if (
                weatherType ==
                    WeatherType.Clear ||
                activeParticles <= 0 ||
                targetCamera == null ||
                worldCollision == null
            )
            {
                ClearMesh();
                return;
            }

            float deltaTime =
                Mathf.Min(
                    Time.deltaTime,
                    0.05f
                );

            UpdateParticles(
                deltaTime
            );

            BuildMesh();
        }

        private void UpdateParticles(
            float deltaTime
        )
        {
            float halfHeight =
                targetCamera.orthographicSize;

            float halfWidth =
                halfHeight *
                targetCamera.aspect;

            Vector3 cameraPosition =
                targetCamera
                    .transform
                    .position;

            float minX =
                cameraPosition.x -
                halfWidth -
                3f;

            float maxX =
                cameraPosition.x +
                halfWidth +
                3f;

            float minY =
                cameraPosition.y -
                halfHeight -
                3f;

            for (
                int i = 0;
                i < activeParticles;
                i++
            )
            {
                Particle particle =
                    particles[i];

                Vector2 oldPosition =
                    particle.Position;

                Vector2 movement =
                    particle.Velocity *
                    deltaTime;

                if (
                    weatherType ==
                    WeatherType.Snow
                )
                {
                    movement.x +=
                        Mathf.Sin(
                            Time.time *
                            particle.Frequency +
                            particle.Phase
                        )
                        *
                        0.55f *
                        deltaTime;
                }

                Vector2 newPosition =
                    oldPosition +
                    movement;

                if (
                    HitsSolidBlock(
                        oldPosition,
                        newPosition
                    )
                )
                {
                    RespawnParticle(
                        i,
                        false
                    );

                    continue;
                }

                particle.Position =
                    newPosition;

                particles[i] =
                    particle;

                if (
                    newPosition.y < minY ||
                    newPosition.x < minX ||
                    newPosition.x > maxX
                )
                {
                    RespawnParticle(
                        i,
                        false
                    );
                }
            }
        }

        private void RespawnParticle(
            int index,
            bool initial
        )
        {
            if (targetCamera == null)
                return;

            float halfHeight =
                targetCamera.orthographicSize;

            float halfWidth =
                halfHeight *
                targetCamera.aspect;

            Vector3 cameraPosition =
                targetCamera
                    .transform
                    .position;

            float x =
                UnityEngine.Random.Range(
                    cameraPosition.x -
                    halfWidth -
                    2f,

                    cameraPosition.x +
                    halfWidth +
                    2f
                );

            float cameraTop =
                cameraPosition.y +
                halfHeight;

            float cameraBottom =
                cameraPosition.y -
                halfHeight;

            float surfaceY =
                cameraTop;

            if (generator != null)
            {
                int blockX =
                    Mathf.FloorToInt(
                        x
                    );

                surfaceY =
                    generator.GetSurfaceHeight(
                        blockX
                    )
                    +
                    1.15f;
            }

            float y;

            if (initial)
            {
                float randomVisibleY =
                    UnityEngine.Random.Range(
                        cameraBottom,
                        cameraTop +
                        2f
                    );

                y =
                    Mathf.Max(
                        randomVisibleY,
                        surfaceY
                    );
            }
            else
            {
                y =
                    Mathf.Max(
                        cameraTop +
                        UnityEngine.Random.Range(
                            0.5f,
                            2.5f
                        ),

                        surfaceY
                    );
            }

            Particle particle =
                new Particle();

            particle.Position =
                new Vector2(
                    x,
                    y
                );

            particle.Phase =
                UnityEngine.Random.Range(
                    0f,
                    Mathf.PI * 2f
                );

            particle.Frequency =
                UnityEngine.Random.Range(
                    1.2f,
                    3.8f
                );

            if (
                weatherType ==
                WeatherType.Rain
            )
            {
                SetupRainParticle(
                    ref particle
                );
            }
            else
            {
                SetupSnowParticle(
                    ref particle
                );
            }

            particles[index] =
                particle;
        }

        private void SetupRainParticle(
            ref Particle particle
        )
        {
            float speed =
                UnityEngine.Random.Range(
                    18f,
                    27f
                );

            float angle =
                rainTiltDegrees +
                UnityEngine.Random.Range(
                    -2.5f,
                    2.5f
                );

            float radians =
                angle *
                Mathf.Deg2Rad;

            particle.Velocity =
                new Vector2(
                    Mathf.Tan(
                        radians
                    )
                    *
                    speed,

                    -speed
                );

            particle.Width =
                UnityEngine.Random.Range(
                    0.025f,
                    0.045f
                );

            particle.Length =
                UnityEngine.Random.Range(
                    0.35f,
                    0.7f
                );

            byte blue =
                (byte)
                UnityEngine.Random.Range(
                    220,
                    250
                );

            particle.Color =
                new Color32(
                    175,
                    205,
                    blue,
                    185
                );
        }

        private void SetupSnowParticle(
            ref Particle particle
        )
        {
            particle.Velocity =
                new Vector2(
                    UnityEngine.Random.Range(
                        -0.35f,
                        0.35f
                    ),

                    -UnityEngine.Random.Range(
                        2.0f,
                        4.0f
                    )
                );

            float size =
                UnityEngine.Random.Range(
                    0.055f,
                    0.12f
                );

            particle.Width = size;
            particle.Length = size;

            float blueAmount =
                UnityEngine.Random.Range(
                    0f,
                    0.45f
                );

            Color color =
                Color.Lerp(
                    new Color(
                        1f,
                        1f,
                        1f,
                        0.95f
                    ),

                    new Color(
                        0.72f,
                        0.88f,
                        1f,
                        0.95f
                    ),

                    blueAmount
                );

            particle.Color =
                color;
        }

        private bool HitsSolidBlock(
            Vector2 start,
            Vector2 end
        )
        {
            float distance =
                Vector2.Distance(
                    start,
                    end
                );

            int steps =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        distance /
                        0.25f
                    ),
                    1,
                    12
                );

            for (
                int i = 1;
                i <= steps;
                i++
            )
            {
                float t =
                    i /
                    (float)
                    steps;

                Vector2 position =
                    Vector2.Lerp(
                        start,
                        end,
                        t
                    );

                int blockX =
                    Mathf.FloorToInt(
                        position.x
                    );

                int blockY =
                    Mathf.FloorToInt(
                        position.y
                    );

                if (
                    worldCollision.IsSolid(
                        blockX,
                        blockY
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildMesh()
        {
            if (mesh == null)
                return;

            vertices.Clear();
            uvs.Clear();
            colors.Clear();
            triangles.Clear();

            for (
                int i = 0;
                i < activeParticles;
                i++
            )
            {
                Particle particle =
                    particles[i];

                if (
                    weatherType ==
                    WeatherType.Rain
                )
                {
                    AddRainQuad(
                        particle
                    );
                }
                else
                {
                    AddSnowQuad(
                        particle
                    );
                }
            }

            mesh.Clear(false);

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(
                triangles,
                0,
                false
            );

            float halfHeight =
                targetCamera.orthographicSize;

            float halfWidth =
                halfHeight *
                targetCamera.aspect;

            Vector3 cameraPosition =
                targetCamera
                    .transform
                    .position;

            mesh.bounds =
                new Bounds(
                    new Vector3(
                        cameraPosition.x,
                        cameraPosition.y,
                        0f
                    ),

                    new Vector3(
                        halfWidth * 2f + 12f,
                        halfHeight * 2f + 12f,
                        10f
                    )
                );
        }

        private void AddSnowQuad(
            Particle particle
        )
        {
            float halfWidth =
                particle.Width *
                0.5f;

            float halfHeight =
                particle.Length *
                0.5f;

            Vector2 center =
                particle.Position;

            AddQuad(
                center +
                new Vector2(
                    -halfWidth,
                    -halfHeight
                ),

                center +
                new Vector2(
                    -halfWidth,
                    halfHeight
                ),

                center +
                new Vector2(
                    halfWidth,
                    halfHeight
                ),

                center +
                new Vector2(
                    halfWidth,
                    -halfHeight
                ),

                particle.Color
            );
        }

        private void AddRainQuad(
            Particle particle
        )
        {
            Vector2 direction =
                particle
                    .Velocity
                    .normalized;

            Vector2 perpendicular =
                new Vector2(
                    -direction.y,
                    direction.x
                );

            Vector2 along =
                direction *
                (
                    particle.Length *
                    0.5f
                );

            Vector2 side =
                perpendicular *
                (
                    particle.Width *
                    0.5f
                );

            Vector2 center =
                particle.Position;

            AddQuad(
                center -
                along -
                side,

                center +
                along -
                side,

                center +
                along +
                side,

                center -
                along +
                side,

                particle.Color
            );
        }

        private void AddQuad(
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d,
            Color32 color
        )
        {
            int index =
                vertices.Count;

            vertices.Add(
                new Vector3(
                    a.x,
                    a.y,
                    0f
                )
            );

            vertices.Add(
                new Vector3(
                    b.x,
                    b.y,
                    0f
                )
            );

            vertices.Add(
                new Vector3(
                    c.x,
                    c.y,
                    0f
                )
            );

            vertices.Add(
                new Vector3(
                    d.x,
                    d.y,
                    0f
                )
            );

            uvs.Add(
                new Vector2(
                    0f,
                    0f
                )
            );

            uvs.Add(
                new Vector2(
                    0f,
                    1f
                )
            );

            uvs.Add(
                new Vector2(
                    1f,
                    1f
                )
            );

            uvs.Add(
                new Vector2(
                    1f,
                    0f
                )
            );

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);

            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        private void ClearMesh()
        {
            if (
                mesh != null &&
                mesh.vertexCount > 0
            )
            {
                mesh.Clear(false);
            }
        }
    }
}

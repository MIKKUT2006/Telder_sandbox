using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Dimensions
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class DimensionPortal :
        MonoBehaviour
    {
        public const int MaxShaderParticles =
            16;


        private string targetDimension;

        private Vector2 baseSize;

        private Color coreColor;

        private Color rimColor;

        private float distortion;

        private float collapseSuction;

        private float rimGlow;


        private float openAmount;

        private float collapseAmount;

        private float visualScaleX =
            1f;

        private float visualScaleY =
            1f;


        private bool traveling;

        private bool collapsing;


        private readonly List<RiftDistortionParticle>
            particles =
            new List<RiftDistortionParticle>();


        private readonly Vector4[] particleShaderData =
            new Vector4[
                MaxShaderParticles
            ];


        public static DimensionPortal Create(
            Vector3 worldPosition,
            string targetDimension,
            Vector2 size,
            Color coreColor,
            Color rimColor,
            float distortion,
            float collapseSuction,
            float rimGlow
        )
        {
            GameObject root =
                new GameObject(
                    "DimensionRift_" +
                    targetDimension
                );

            root.transform.position =
                worldPosition;

            root.transform.localScale =
                Vector3.one;

            DimensionPortal portal =
                root.AddComponent<
                    DimensionPortal
                >();

            portal.Initialize(
                targetDimension,
                size,
                coreColor,
                rimColor,
                distortion,
                collapseSuction,
                rimGlow
            );

            return portal;
        }


        private void Initialize(
            string dimensionName,
            Vector2 size,
            Color newCoreColor,
            Color newRimColor,
            float distortionStrength,
            float suctionStrength,
            float glow
        )
        {
            targetDimension =
                dimensionName;

            baseSize =
                size;

            coreColor =
                newCoreColor;

            rimColor =
                newRimColor;

            distortion =
                distortionStrength;

            collapseSuction =
                suctionStrength;

            rimGlow =
                glow;

            openAmount =
                0f;

            collapseAmount =
                0f;

            visualScaleX =
                0.001f;

            visualScaleY =
                0.001f;

            SetupCollider();
            CreateParticles();
        }


        private void SetupCollider()
        {
            BoxCollider2D trigger =
                GetComponent<
                    BoxCollider2D
                >();

            trigger.isTrigger =
                true;

            trigger.size =
                new Vector2(
                    baseSize.x * 0.64f,
                    baseSize.y * 0.82f
                );

            trigger.offset =
                Vector2.zero;

            /*
             * Пока портал открывается, collider выключен.
             * Включим его только когда разлом достигнет размера.
             */
            trigger.enabled =
                false;
        }


        private void CreateParticles()
        {
            float radiusX =
                baseSize.x * 0.90f;

            float radiusY =
                baseSize.y * 0.72f;

            for (
                int i = 0;
                i < MaxShaderParticles;
                i++
            )
            {
                float angle =
                    Random.Range(
                        0f,
                        Mathf.PI * 2f
                    );

                float distance =
                    Random.Range(
                        0.72f,
                        1.25f
                    );

                Vector2 point =
                    new Vector2(
                        Mathf.Cos(angle) *
                        radiusX *
                        distance,

                        Mathf.Sin(angle) *
                        radiusY *
                        distance
                    );

                RiftDistortionParticle particle =
                    RiftDistortionParticle.Create(
                        transform,
                        point
                    );

                particles.Add(
                    particle
                );
            }
        }


        public IEnumerator Open(
            float duration
        )
        {
            float elapsed =
                0f;

            openAmount =
                0f;

            collapseAmount =
                0f;

            visualScaleX =
                0.001f;

            visualScaleY =
                0.001f;


            /*
             * Портал появляется ИЗ ТОЧКИ.
             *
             * Обе оси начинаются почти с нуля.
             * Затем точка сначала становится маленьким сгустком,
             * после чего разрастается до полного вытянутого разлома.
             */
            while (
                elapsed <
                duration
            )
            {
                elapsed +=
                    Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );


                /*
                 * Мягкий ease-out.
                 */
                float ease =
                    1f -
                    Mathf.Pow(
                        1f - t,
                        3f
                    );


                openAmount =
                    ease;


                /*
                 * Первая треть:
                 * маленькая почти круглая точка.
                 */
                if (t < 0.30f)
                {
                    float pointT =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            t / 0.30f
                        );

                    float pointScale =
                        Mathf.Lerp(
                            0.001f,
                            0.13f,
                            pointT
                        );

                    visualScaleX =
                        pointScale;

                    visualScaleY =
                        pointScale;
                }
                else
                {
                    /*
                     * После точки разлом одновременно
                     * расширяется и вытягивается.
                     */
                    float growT =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            (t - 0.30f) / 0.70f
                        );

                    visualScaleX =
                        Mathf.Lerp(
                            0.13f,
                            1f,
                            Mathf.Pow(
                                growT,
                                1.10f
                            )
                        );

                    visualScaleY =
                        Mathf.Lerp(
                            0.13f,
                            1f,
                            Mathf.Pow(
                                growT,
                                0.82f
                            )
                        );
                }


                yield return null;
            }


            openAmount =
                1f;

            visualScaleX =
                1f;

            visualScaleY =
                1f;


            BoxCollider2D collider =
                GetComponent<
                    BoxCollider2D
                >();

            if (collider != null)
            {
                collider.enabled =
                    true;
            }
        }


        public IEnumerator Collapse(
            float duration
        )
        {
            if (collapsing)
            {
                yield break;
            }

            collapsing =
                true;

            BoxCollider2D collider =
                GetComponent<
                    BoxCollider2D
                >();

            if (collider != null)
            {
                collider.enabled =
                    false;
            }


            float elapsed =
                0f;


            /*
             * Две фазы:
             *
             * 1. 0-55%:
             *    пространство всё сильнее втягивается,
             *    портал лишь слегка сжимается.
             *
             * 2. 55-100%:
             *    сам разлом быстро уменьшается В ТОЧКУ
             *    и полностью исчезает.
             */
            while (
                elapsed <
                duration
            )
            {
                elapsed +=
                    Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );


                /*
                 * Втягивание реальности начинает расти сразу
                 * и достигает максимума раньше полного закрытия.
                 */
                collapseAmount =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(
                            t / 0.78f
                        )
                    );


                if (t < 0.55f)
                {
                    float tension =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            t / 0.55f
                        );

                    /*
                     * Разлом "напрягается" перед закрытием:
                     * чуть становится меньше.
                     */
                    visualScaleX =
                        Mathf.Lerp(
                            1f,
                            0.88f,
                            tension
                        );

                    visualScaleY =
                        Mathf.Lerp(
                            1f,
                            0.92f,
                            tension
                        );
                }
                else
                {
                    float closeT =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                0.55f,
                                1f,
                                t
                            )
                        );


                    /*
                     * ВАЖНО:
                     * теперь ОБЕ оси стремятся к нулю.
                     *
                     * Поэтому портал реально уменьшается
                     * до маленькой точки и исчезает,
                     * а не превращается только в тонкую щель.
                     */
                    visualScaleX =
                        Mathf.Lerp(
                            0.88f,
                            0.001f,
                            Mathf.Pow(
                                closeT,
                                1.25f
                            )
                        );

                    visualScaleY =
                        Mathf.Lerp(
                            0.92f,
                            0.001f,
                            Mathf.Pow(
                                closeT,
                                1.05f
                            )
                        );
                }


                /*
                 * Частицы втягиваются к центру одновременно.
                 */
                float particleCollapse =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );

                for (
                    int i = 0;
                    i < particles.Count;
                    i++
                )
                {
                    if (particles[i] != null)
                    {
                        particles[i].SetCollapse(
                            particleCollapse
                        );
                    }
                }


                yield return null;
            }


            collapseAmount =
                1f;

            visualScaleX =
                0.0001f;

            visualScaleY =
                0.0001f;


            /*
             * Один последний кадр:
             * максимальное втягивание + портал уже почти точка.
             */
            yield return null;


            DimensionPortalManager.Instance
                ?.NotifyDestroyed(
                    this
                );


            Destroy(
                gameObject
            );
        }


        private void Update()
        {
            if (
                !collapsing &&
                openAmount >= 0.999f
            )
            {
                float pulse =
                    Mathf.Sin(
                        Time.time * 2.8f
                    );

                visualScaleX =
                    1f +
                    pulse *
                    0.012f;

                visualScaleY =
                    1f -
                    pulse *
                    0.006f;
            }
        }


        private void OnTriggerEnter2D(
            Collider2D other
        )
        {
            TryTravel(
                other
            );
        }


        private void OnTriggerStay2D(
            Collider2D other
        )
        {
            TryTravel(
                other
            );
        }


        private void TryTravel(
            Collider2D other
        )
        {
            if (
                traveling ||
                collapsing ||
                other == null ||
                !other.CompareTag("Player")
            )
            {
                return;
            }

            traveling =
                true;

            DimensionTravelRuntime.TravelTo(
                targetDimension
            );
        }


        public void ApplyVisualToMaterial(
            Material material,
            Camera camera
        )
        {
            if (
                material == null ||
                camera == null
            )
            {
                return;
            }


            Vector3 centerViewport =
                camera.WorldToViewportPoint(
                    transform.position
                );


            /*
             * Большая зона distortion / suction.
             * Она остаётся большой даже когда портал схлопывается.
             */
            Vector3 baseRightViewport =
                camera.WorldToViewportPoint(
                    transform.position +
                    Vector3.right *
                    (
                        baseSize.x *
                        0.5f
                    )
                );

            Vector3 baseUpViewport =
                camera.WorldToViewportPoint(
                    transform.position +
                    Vector3.up *
                    (
                        baseSize.y *
                        0.5f
                    )
                );


            float baseRadiusX =
                Mathf.Max(
                    Mathf.Abs(
                        baseRightViewport.x -
                        centerViewport.x
                    ),
                    0.0001f
                );

            float baseRadiusY =
                Mathf.Max(
                    Mathf.Abs(
                        baseUpViewport.y -
                        centerViewport.y
                    ),
                    0.0001f
                );


            /*
             * Размер только визуальной формы.
             */
            Vector3 visualRightViewport =
                camera.WorldToViewportPoint(
                    transform.position +
                    Vector3.right *
                    (
                        baseSize.x *
                        0.5f *
                        visualScaleX
                    )
                );

            Vector3 visualUpViewport =
                camera.WorldToViewportPoint(
                    transform.position +
                    Vector3.up *
                    (
                        baseSize.y *
                        0.5f *
                        visualScaleY
                    )
                );


            float visualRadiusX =
                Mathf.Max(
                    Mathf.Abs(
                        visualRightViewport.x -
                        centerViewport.x
                    ),
                    0.000001f
                );

            float visualRadiusY =
                Mathf.Max(
                    Mathf.Abs(
                        visualUpViewport.y -
                        centerViewport.y
                    ),
                    0.000001f
                );


            material.SetVector(
                "_RiftCenter",
                new Vector4(
                    centerViewport.x,
                    centerViewport.y,
                    0f,
                    0f
                )
            );


            material.SetVector(
                "_RiftRadius",
                new Vector4(
                    baseRadiusX,
                    baseRadiusY,
                    0f,
                    0f
                )
            );


            material.SetVector(
                "_RiftVisualRadius",
                new Vector4(
                    visualRadiusX,
                    visualRadiusY,
                    0f,
                    0f
                )
            );


            /*
             * НОВОЕ:
             * сердцевина и обводка имеют независимые цвета.
             */
            material.SetColor(
                "_RiftCoreColor",
                coreColor
            );


            material.SetColor(
                "_RiftRimColor",
                rimColor
            );


            material.SetFloat(
                "_RiftDistortion",
                distortion
            );


            material.SetFloat(
                "_RiftSuction",
                collapseSuction
            );


            material.SetFloat(
                "_RiftGlow",
                rimGlow
            );


            material.SetFloat(
                "_RiftOpen",
                openAmount
            );


            material.SetFloat(
                "_RiftCollapse",
                collapseAmount
            );


            int count =
                Mathf.Min(
                    particles.Count,
                    MaxShaderParticles
                );


            for (
                int i = 0;
                i < count;
                i++
            )
            {
                RiftDistortionParticle particle =
                    particles[i];


                if (particle == null)
                {
                    particleShaderData[i] =
                        Vector4.zero;

                    continue;
                }


                Vector3 vp =
                    camera.WorldToViewportPoint(
                        particle.transform.position
                    );


                Vector3 radiusPoint =
                    camera.WorldToViewportPoint(
                        particle.transform.position +
                        Vector3.right *
                        particle.BaseRadius
                    );


                float particleRadius =
                    Mathf.Abs(
                        radiusPoint.x -
                        vp.x
                    );


                particleShaderData[i] =
                    new Vector4(
                        vp.x,
                        vp.y,
                        Mathf.Max(
                            particleRadius,
                            0.0015f
                        ),
                        particle.Strength
                    );
            }


            for (
                int i = count;
                i < MaxShaderParticles;
                i++
            )
            {
                particleShaderData[i] =
                    Vector4.zero;
            }


            material.SetInt(
                "_RiftParticleCount",
                count
            );


            material.SetVectorArray(
                "_RiftParticles",
                particleShaderData
            );
        }
    }
}
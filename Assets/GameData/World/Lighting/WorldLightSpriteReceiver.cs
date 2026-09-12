
using System.Collections.Generic;

using UnityEngine;


namespace Game.World.Lighting
{
    /// <summary>
    /// Applies world tile lighting to every SpriteRenderer below this object.
    ///
    /// This does NOT create Unity lights.
    /// It reads the same Game.World.LightNode data that terrain uses and
    /// multiplies the sprite colors by that light.
    /// </summary>
    [DefaultExecutionOrder(32500)]
    public class WorldLightSpriteReceiver :
        MonoBehaviour
    {
        // =====================================================
        // LIGHT LOOK
        // =====================================================

        [Header("World Light")]

        [SerializeField]
        [Range(0f, 0.3f)]
        private float ambient =
            0.055f;


        [SerializeField]
        [Range(0.1f, 2f)]
        private float lightGamma =
            0.72f;


        [SerializeField]
        [Range(1f, 40f)]
        private float transitionSpeed =
            20f;


        [SerializeField]
        private Vector2 sampleOffset =
            Vector2.zero;


        // =====================================================
        // RENDERERS
        // =====================================================

        private readonly List<SpriteRenderer>
            renderers =
            new List<SpriteRenderer>();


        private readonly Dictionary<
            int,
            Color
        > baseColors =
            new Dictionary<
                int,
                Color
            >();


        private float nextRendererRefreshTime;


        private Color currentLightColor =
            Color.white;


        private bool initializedLight;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {
            RefreshRenderers();

            nextRendererRefreshTime =
                Time.unscaledTime +
                0.25f;
        }


        private void OnEnable()
        {
            initializedLight =
                false;


            RefreshRenderers();
        }


        private void LateUpdate()
        {
            if (
                Time.unscaledTime >=
                nextRendererRefreshTime
            )
            {
                RefreshRenderers();


                nextRendererRefreshTime =
                    Time.unscaledTime +
                    0.25f;
            }


            WorldManager manager =
                WorldManager.Instance;


            if (
                manager ==
                null
            )
            {
                return;
            }


            World world =
                manager.GetWorld();


            if (
                world ==
                null
            )
            {
                return;
            }


            Vector2 position =
                (Vector2)
                transform.position +
                sampleOffset;


            Color targetLight =
                SampleSmoothWorldLight(
                    world,
                    position
                );


            if (
                !initializedLight
            )
            {
                currentLightColor =
                    targetLight;


                initializedLight =
                    true;
            }
            else
            {
                float t =
                    1f -
                    Mathf.Exp(
                        -transitionSpeed *
                        Time.deltaTime
                    );


                currentLightColor =
                    Color.Lerp(
                        currentLightColor,
                        targetLight,
                        t
                    );
            }


            ApplyLight(
                currentLightColor
            );
        }


        // =====================================================
        // CONFIGURE
        // =====================================================

        public void Configure(
            Vector2 newSampleOffset
        )
        {
            sampleOffset =
                newSampleOffset;


            initializedLight =
                false;


            RefreshRenderers();
        }


        // =====================================================
        // RENDERER CACHE
        // =====================================================

        private void RefreshRenderers()
        {
            SpriteRenderer[] found =
                GetComponentsInChildren<
                    SpriteRenderer
                >(
                    true
                );


            renderers.Clear();


            for (
                int i = 0;
                i < found.Length;
                i++
            )
            {
                SpriteRenderer renderer =
                    found[i];


                if (
                    renderer ==
                    null
                )
                {
                    continue;
                }


                renderers.Add(
                    renderer
                );


                int id =
                    renderer.GetInstanceID();


                if (
                    !baseColors.ContainsKey(
                        id
                    )
                )
                {
                    baseColors.Add(
                        id,
                        renderer.color
                    );
                }
            }
        }


        private void ApplyLight(
            Color lightColor
        )
        {
            for (
                int i = renderers.Count - 1;
                i >= 0;
                i--
            )
            {
                SpriteRenderer renderer =
                    renderers[i];


                if (
                    renderer ==
                    null
                )
                {
                    renderers.RemoveAt(
                        i
                    );


                    continue;
                }


                int id =
                    renderer.GetInstanceID();


                if (
                    !baseColors.TryGetValue(
                        id,
                        out Color baseColor
                    )
                )
                {
                    baseColor =
                        renderer.color;


                    baseColors[
                        id
                    ] =
                        baseColor;
                }


                renderer.color =
                    new Color(
                        baseColor.r *
                        lightColor.r,

                        baseColor.g *
                        lightColor.g,

                        baseColor.b *
                        lightColor.b,

                        baseColor.a
                    );
            }
        }


        // =====================================================
        // WORLD LIGHT SAMPLE
        // =====================================================

        private Color SampleSmoothWorldLight(
            World world,
            Vector2 position
        )
        {
            int x0 =
                Mathf.FloorToInt(
                    position.x
                );


            int y0 =
                Mathf.FloorToInt(
                    position.y
                );


            int x1 =
                x0 +
                1;


            int y1 =
                y0 +
                1;


            float tx =
                Mathf.Clamp01(
                    position.x -
                    x0
                );


            float ty =
                Mathf.Clamp01(
                    position.y -
                    y0
                );


            Color c00 =
                NodeToColor(
                    world.GetLight(
                        x0,
                        y0
                    )
                );


            Color c10 =
                NodeToColor(
                    world.GetLight(
                        x1,
                        y0
                    )
                );


            Color c01 =
                NodeToColor(
                    world.GetLight(
                        x0,
                        y1
                    )
                );


            Color c11 =
                NodeToColor(
                    world.GetLight(
                        x1,
                        y1
                    )
                );


            Color bottom =
                Color.Lerp(
                    c00,
                    c10,
                    tx
                );


            Color top =
                Color.Lerp(
                    c01,
                    c11,
                    tx
                );


            return
                Color.Lerp(
                    bottom,
                    top,
                    ty
                );
        }


        private Color NodeToColor(
            LightNode node
        )
        {
            float sunlight =
                node.Sun /
                15f;


            float red =
                Mathf.Max(
                    sunlight,
                    node.R /
                    15f
                );


            float green =
                Mathf.Max(
                    sunlight,
                    node.G /
                    15f
                );


            float blue =
                Mathf.Max(
                    sunlight,
                    node.B /
                    15f
                );


            red =
                ApplyLightCurve(
                    red
                );


            green =
                ApplyLightCurve(
                    green
                );


            blue =
                ApplyLightCurve(
                    blue
                );


            return
                new Color(
                    red,
                    green,
                    blue,
                    1f
                );
        }


        private float ApplyLightCurve(
            float value
        )
        {
            value =
                Mathf.Clamp01(
                    value
                );


            float curved =
                Mathf.Pow(
                    value,
                    lightGamma
                );


            // Same idea as terrain ambient:
            // complete darkness stays visible, but very dark.
            return
                Mathf.Lerp(
                    ambient,
                    1f,
                    curved
                );
        }
    }
}

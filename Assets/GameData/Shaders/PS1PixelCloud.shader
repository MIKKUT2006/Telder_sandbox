Shader "Game/PS1PixelCloud"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cloud Mask", 2D) = "white" {}

        _CloudColor ("Cloud Color", Color) = (0.8,0.85,0.95,1)
        _StormColor ("Storm Color", Color) = (0.15,0.17,0.22,1)

        _Rain ("Rain", Range(0,1)) = 0

        _PixelGridX ("Pixel Grid X", Float) = 88
        _PixelGridY ("Pixel Grid Y", Float) = 38

        _BlurRadius ("Pixel Blur", Range(0,3)) = 1.05

        _DistortionStrength ("Distortion", Range(0,2)) = 0.55
        _DistortionScale ("Distortion Cells", Range(1,20)) = 8

        _Density ("Density", Range(0.5,3)) = 1.42

        _DitherStrength ("Dither", Range(0,0.2)) = 0.035
        _JitterStrength ("PS1 Jitter", Range(0,1)) = 0.10

        [PerRendererData] _Alpha ("Alpha", Range(0,2)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "PS1PixelCloud"

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            CBUFFER_START(UnityPerMaterial)

                float4 _CloudColor;
                float4 _StormColor;

                float _Rain;

                float _PixelGridX;
                float _PixelGridY;

                float _BlurRadius;

                float _DistortionStrength;
                float _DistortionScale;

                float _Density;

                float _DitherStrength;
                float _JitterStrength;

                float _Alpha;

            CBUFFER_END


            Varyings Vert(
                Attributes input
            )
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );

                output.uv =
                    input.uv;

                return output;
            }


            float Hash21(
                float2 p
            )
            {
                float value =
                    sin(
                        dot(
                            p,
                            float2(
                                12.9898,
                                78.233
                            )
                        )
                    )
                    *
                    43758.5453;

                return
                    frac(
                        value
                    );
            }


            half SampleCloud(
                float2 uv
            )
            {
                return
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        saturate(
                            uv
                        )
                    ).a;
            }


            half4 Frag(
                Varyings input
            ) : SV_Target
            {
                float2 grid =
                    max(
                        float2(
                            _PixelGridX,
                            _PixelGridY
                        ),
                        float2(
                            8.0,
                            8.0
                        )
                    );


                float2 pixelSize =
                    1.0 /
                    grid;


                // -----------------------------------------------------
                // BLOCKY DISTORTION
                // -----------------------------------------------------

                float distortionScale =
                    max(
                        1.0,
                        _DistortionScale
                    );


                float2 distortionCell =
                    floor(
                        input.uv *
                        grid /
                        distortionScale
                    );


                float timeStep =
                    floor(
                        _Time.y *
                        4.0
                    );


                float nx =
                    Hash21(
                        distortionCell +
                        float2(
                            timeStep *
                            0.13,
                            17.0
                        )
                    );


                float ny =
                    Hash21(
                        distortionCell +
                        float2(
                            41.0,
                            timeStep *
                            0.11
                        )
                    );


                float2 distortion =
                    float2(
                        nx - 0.5,
                        ny - 0.5
                    )
                    *
                    2.0;


                /*
                 * Quantize distortion itself.
                 */
                distortion =
                    floor(
                        distortion *
                        4.0 +
                        0.5
                    )
                    /
                    4.0;


                float2 warpedUV =
                    input.uv +
                    distortion *
                    pixelSize *
                    _DistortionStrength *
                    (
                        1.0 +
                        _Rain *
                        0.50
                    );


                // -----------------------------------------------------
                // LOW-FREQUENCY PS1 JITTER
                // -----------------------------------------------------

                float jitter =
                    Hash21(
                        floor(
                            input.positionHCS.xy /
                            7.0
                        )
                        +
                        timeStep *
                        0.17
                    )
                    -
                    0.5;


                warpedUV +=
                    jitter *
                    pixelSize *
                    _JitterStrength;


                // -----------------------------------------------------
                // PIXEL GRID
                // -----------------------------------------------------

                float2 uv =
                    (
                        floor(
                            warpedUV *
                            grid
                        )
                        +
                        0.5
                    )
                    /
                    grid;


                // -----------------------------------------------------
                // CHUNKY PIXEL BLUR
                // -----------------------------------------------------

                float2 step1 =
                    pixelSize *
                    _BlurRadius;


                half mask =
                    SampleCloud(
                        uv
                    )
                    *
                    0.30h;


                mask +=
                    (
                        SampleCloud(
                            uv +
                            float2(
                                step1.x,
                                0.0
                            )
                        )
                        +
                        SampleCloud(
                            uv -
                            float2(
                                step1.x,
                                0.0
                            )
                        )
                        +
                        SampleCloud(
                            uv +
                            float2(
                                0.0,
                                step1.y
                            )
                        )
                        +
                        SampleCloud(
                            uv -
                            float2(
                                0.0,
                                step1.y
                            )
                        )
                    )
                    *
                    0.12h;


                mask +=
                    (
                        SampleCloud(
                            uv +
                            step1
                        )
                        +
                        SampleCloud(
                            uv -
                            step1
                        )
                        +
                        SampleCloud(
                            uv +
                            float2(
                                step1.x,
                                -step1.y
                            )
                        )
                        +
                        SampleCloud(
                            uv +
                            float2(
                                -step1.x,
                                step1.y
                            )
                        )
                    )
                    *
                    0.055h;


                // -----------------------------------------------------
                // DENSITY
                // -----------------------------------------------------

                mask =
                    saturate(
                        mask *
                        _Density +
                        0.10 +
                        _Rain *
                        0.15
                    );


                mask =
                    smoothstep(
                        0.05,
                        0.78,
                        mask
                    );


                // -----------------------------------------------------
                // COLOR
                // -----------------------------------------------------

                float3 color =
                    lerp(
                        _CloudColor.rgb,
                        _StormColor.rgb,
                        _Rain
                    );


                /*
                 * Cheap ordered-looking dither without array indexing.
                 * This is intentionally shader-model-2-friendly.
                 */
                float dither =
                    Hash21(
                        floor(
                            input.positionHCS.xy /
                            2.0
                        )
                    )
                    -
                    0.5;


                color +=
                    dither *
                    _DitherStrength;


                color =
                    floor(
                        saturate(
                            color
                        )
                        *
                        31.0 +
                        0.5
                    )
                    /
                    31.0;


                half alpha =
                    saturate(
                        mask *
                        _Alpha *
                        1.20
                    );


                alpha =
                    floor(
                        alpha *
                        31.0 +
                        0.5
                    )
                    /
                    31.0;


                return
                    half4(
                        color,
                        alpha
                    );
            }

            ENDHLSL
        }
    }
}
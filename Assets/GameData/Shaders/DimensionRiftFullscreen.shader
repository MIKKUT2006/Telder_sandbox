Shader "Hidden/Game/DimensionRiftFullscreen"
{
    Properties
    {
        _RiftCoreColor ("Core Color", Color) =
            (0.0, 0.0, 0.0, 1)

        _RiftRimColor ("Rim Color", Color) =
            (0.24, 0.015, 0.48, 1)

        _RiftDistortion ("Distortion", Float) =
            0.05

        _RiftSuction ("Collapse Suction", Float) =
            0.18

        _RiftGlow ("Rim Glow", Float) =
            0.95
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
        }


        Pass
        {
            Name "DimensionRiftFullscreen"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off


            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"


            float4 _RiftCenter;

            /*
             * Размер гравитационного поля.
             * Не уменьшается при схлопывании.
             */
            float4 _RiftRadius;

            /*
             * Размер только визуальной чёрной дыры.
             * Именно он схлопывается.
             */
            float4 _RiftVisualRadius;


            float4 _RiftCoreColor;
            float4 _RiftRimColor;

            float _RiftDistortion;
            float _RiftSuction;
            float _RiftGlow;

            float _RiftOpen;
            float _RiftCollapse;

            int _RiftParticleCount;

            float4 _RiftParticles[16];


            float hash21(
                float2 p
            )
            {
                p =
                    frac(
                        p *
                        float2(
                            123.34,
                            456.21
                        )
                    );

                p +=
                    dot(
                        p,
                        p + 45.32
                    );

                return
                    frac(
                        p.x *
                        p.y
                    );
            }


            float noise(
                float2 p
            )
            {
                float2 i =
                    floor(p);

                float2 f =
                    frac(p);

                f =
                    f *
                    f *
                    (
                        3.0 -
                        2.0 * f
                    );

                float a =
                    hash21(i);

                float b =
                    hash21(
                        i +
                        float2(1, 0)
                    );

                float c =
                    hash21(
                        i +
                        float2(0, 1)
                    );

                float d =
                    hash21(
                        i +
                        float2(1, 1)
                    );

                return
                    lerp(
                        lerp(
                            a,
                            b,
                            f.x
                        ),
                        lerp(
                            c,
                            d,
                            f.x
                        ),
                        f.y
                    );
            }


            float fbm(
                float2 p
            )
            {
                float value =
                    0.0;

                float amplitude =
                    0.5;

                [unroll]
                for (
                    int i = 0;
                    i < 5;
                    i++
                )
                {
                    value +=
                        noise(p) *
                        amplitude;

                    p =
                        p *
                        2.03 +
                        17.71;

                    amplitude *=
                        0.5;
                }

                return
                    value;
            }


            half4 Frag(
                Varyings input
            ) : SV_Target
            {
                float2 uv =
                    input.texcoord;

                float2 center =
                    _RiftCenter.xy;


                float2 baseRadius =
                    max(
                        _RiftRadius.xy,
                        float2(
                            0.0001,
                            0.0001
                        )
                    );


                float2 visualRadius =
                    max(
                        _RiftVisualRadius.xy,
                        float2(
                            0.00001,
                            0.00001
                        )
                    );


                float time =
                    _Time.y;


                /*
                 * =====================================================
                 * TWO COORDINATE SYSTEMS
                 * =====================================================
                 */

                /*
                 * fieldQ используется для distortion/suction.
                 *
                 * Его размер остаётся большим даже когда
                 * чёрная форма уже схлопывается.
                 */
                float2 fieldQ =
                    (
                        uv -
                        center
                    )
                    /
                    baseRadius;


                /*
                 * visualQ используется только для формы портала.
                 */
                float2 visualQ =
                    (
                        uv -
                        center
                    )
                    /
                    visualRadius;


                /*
                 * Живая органическая неровность формы.
                 */
                visualQ.x +=
                    sin(
                        visualQ.y * 5.5 +
                        time * 1.1
                    )
                    *
                    0.035;


                float rough =
                    (
                        fbm(
                            float2(
                                visualQ.y * 2.1 +
                                time * 0.08,
                                visualQ.x * 1.7 -
                                time * 0.06
                            )
                        )
                        -
                        0.5
                    )
                    *
                    0.075;


                float visualSignedDistance =
                    length(
                        visualQ
                    )
                    -
                    1.0
                    +
                    rough;


                /*
                 * Отдельная distance для большого
                 * гравитационного поля.
                 */
                float fieldSignedDistance =
                    length(
                        fieldQ
                    )
                    -
                    1.0;


                /*
                 * =====================================================
                 * NORMAL REALITY DISTORTION
                 * =====================================================
                 */

                float2 screenDelta =
                    uv -
                    center;


                float screenDistance =
                    max(
                        length(
                            screenDelta
                        ),
                        0.00001
                    );


                float2 radial =
                    screenDelta
                    /
                    screenDistance;


                float outsideField =
                    max(
                        fieldSignedDistance,
                        0.0
                    );


                float gravityInfluence =
                    1.0
                    -
                    smoothstep(
                        0.0,
                        2.6,
                        outsideField
                    );


                gravityInfluence =
                    pow(
                        saturate(
                            gravityInfluence
                        ),
                        1.8
                    );


                /*
                 * Пока портал только открывается,
                 * distortion тоже плавно появляется.
                 */
                gravityInfluence *=
                    smoothstep(
                        0.0,
                        0.35,
                        _RiftOpen
                    );


                float wave =
                    sin(
                        fieldSignedDistance *
                        18.0 -
                        time * 3.0
                    )
                    *
                    0.20;


                float2 warpOffset =
                    radial
                    *
                    _RiftDistortion
                    *
                    gravityInfluence
                    *
                    (
                        1.0 +
                        wave
                    );


                float2 tangent =
                    float2(
                        -radial.y,
                        radial.x
                    );


                warpOffset +=
                    tangent
                    *
                    sin(
                        fieldQ.y * 7.0 +
                        time * 1.7
                    )
                    *
                    _RiftDistortion
                    *
                    gravityInfluence
                    *
                    0.16;


                /*
                 * =====================================================
                 * COLLAPSE SUCTION
                 * =====================================================
                 *
                 * Вот главный эффект:
                 * мир реально растягивается и затягивается
                 * в центр перед закрытием разлома.
                 */

                float collapseCurve =
                    _RiftCollapse
                    *
                    _RiftCollapse;


                /*
                 * Во время collapse зона захвата реальности
                 * становится БОЛЬШЕ, а не меньше.
                 */
                float expandedFieldDistance =
                    (
                        length(
                            fieldQ
                        )
                        /
                        lerp(
                            1.0,
                            1.55,
                            collapseCurve
                        )
                    )
                    -
                    1.0;


                float suctionOutside =
                    max(
                        expandedFieldDistance,
                        0.0
                    );


                float suctionInfluence =
                    1.0
                    -
                    smoothstep(
                        0.0,
                        4.4,
                        suctionOutside
                    );


                suctionInfluence =
                    pow(
                        saturate(
                            suctionInfluence
                        ),
                        1.10
                    );


                /*
                 * Ближе к центру suction ещё сильнее.
                 */
                float centerBoost =
                    1.0
                    -
                    smoothstep(
                        0.0,
                        2.3,
                        length(
                            fieldQ
                        )
                    );


                float suctionAmount =
                    _RiftSuction
                    *
                    collapseCurve
                    *
                    suctionInfluence
                    *
                    (
                        0.72 +
                        centerBoost *
                        0.55
                    );


                /*
                 * Положительный radial sample offset заставляет
                 * визуальные объекты казаться смещёнными К центру.
                 */
                float2 suctionOffset =
                    radial
                    *
                    suctionAmount;


                warpOffset +=
                    suctionOffset;


                /*
                 * У центра появляется лёгкое вращение материи.
                 */
                warpOffset +=
                    tangent
                    *
                    _RiftSuction
                    *
                    collapseCurve
                    *
                    suctionInfluence
                    *
                    sin(
                        time * 4.0 +
                        length(fieldQ) * 4.0
                    )
                    *
                    0.10;


                /*
                 * =====================================================
                 * PARTICLES ALSO DISTORT REALITY
                 * =====================================================
                 */

                float particleGlow =
                    0.0;

                float particleCore =
                    0.0;


                [unroll]
                for (
                    int pIndex = 0;
                    pIndex < 16;
                    pIndex++
                )
                {
                    if (
                        pIndex >=
                        _RiftParticleCount
                    )
                    {
                        break;
                    }


                    float4 particle =
                        _RiftParticles[
                            pIndex
                        ];


                    float2 pd =
                        uv -
                        particle.xy;


                    float pdist =
                        length(
                            pd
                        );


                    float particleRadius =
                        max(
                            particle.z,
                            0.0001
                        );


                    float normalizedDistance =
                        pdist
                        /
                        particleRadius;


                    float influence =
                        1.0
                        -
                        smoothstep(
                            0.3,
                            2.4,
                            normalizedDistance
                        );


                    float2 pdir =
                        pd
                        /
                        max(
                            pdist,
                            0.00001
                        );


                    warpOffset +=
                        pdir
                        *
                        particleRadius
                        *
                        particle.w
                        *
                        influence
                        *
                        0.44;


                    particleGlow =
                        max(
                            particleGlow,
                            (
                                1.0
                                -
                                smoothstep(
                                    0.35,
                                    1.75,
                                    normalizedDistance
                                )
                            )
                            *
                            particle.w
                        );


                    particleCore =
                        max(
                            particleCore,
                            1.0
                            -
                            smoothstep(
                                0.0,
                                0.36,
                                normalizedDistance
                            )
                        );
                }


                /*
                 * =====================================================
                 * RADIAL STRETCH / SMEAR DURING COLLAPSE
                 * =====================================================
                 *
                 * Не один sample, а несколько.
                 * Это визуально вытягивает блоки в сторону разлома.
                 */

                float2 uv0 =
                    saturate(
                        uv +
                        warpOffset
                    );


                float2 uv1 =
                    saturate(
                        uv +
                        warpOffset +
                        suctionOffset *
                        0.65
                    );


                float2 uv2 =
                    saturate(
                        uv +
                        warpOffset +
                        suctionOffset *
                        1.45
                    );


                float2 uv3 =
                    saturate(
                        uv +
                        warpOffset +
                        suctionOffset *
                        2.35
                    );


                half3 scene0 =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        uv0
                    ).rgb;


                half3 scene1 =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        uv1
                    ).rgb;


                half3 scene2 =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        uv2
                    ).rgb;


                half3 scene3 =
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        uv3
                    ).rgb;


                float smearStrength =
                    saturate(
                        collapseCurve *
                        suctionInfluence *
                        0.82
                    );


                half3 smearedScene =
                    (
                        scene0 * 0.46 +
                        scene1 * 0.26 +
                        scene2 * 0.18 +
                        scene3 * 0.10
                    );


                half3 scene =
                    lerp(
                        scene0,
                        smearedScene,
                        smearStrength
                    );


                /*
                 * =====================================================
                 * TRUE BLACK VOID
                 * =====================================================
                 */

                float inside =
                    1.0
                    -
                    smoothstep(
                        -0.015,
                        0.035,
                        visualSignedDistance
                    );


                float deepInside =
                    1.0
                    -
                    smoothstep(
                        -0.62,
                        -0.12,
                        visualSignedDistance
                    );


                half3 coreColor =
                    _RiftCoreColor.rgb;


                half3 result =
                    lerp(
                        scene,
                        coreColor,
                        inside
                    );


                /*
                 * =====================================================
                 * DARK MATTER RIM
                 * =====================================================
                 */

                float rim =
                    1.0
                    -
                    smoothstep(
                        0.015,
                        0.155,
                        abs(
                            visualSignedDistance
                        )
                    );


                rim *=
                    1.0
                    -
                    deepInside;


                float rimPulse =
                    0.78
                    +
                    sin(
                        visualQ.y * 8.0 +
                        time * 2.4
                    )
                    *
                    0.22;


                result +=
                    _RiftRimColor.rgb
                    *
                    rim
                    *
                    _RiftGlow
                    *
                    rimPulse;


                /*
                 * =====================================================
                 * DARKEN MATTER BEING SUCKED IN
                 * =====================================================
                 */

                float collapseDark =
                    collapseCurve
                    *
                    suctionInfluence
                    *
                    (
                        0.10 +
                        centerBoost *
                        0.22
                    );


                result *=
                    1.0
                    -
                    collapseDark;


                /*
                 * Particles are tiny dark-matter nodes.
                 */
                result =
                    lerp(
                        result,
                        coreColor,
                        particleCore *
                        0.52
                    );


                result +=
                    _RiftRimColor.rgb
                    *
                    particleGlow
                    *
                    0.28;


                return
                    half4(
                        result,
                        1.0
                    );
            }

            ENDHLSL
        }
    }
}
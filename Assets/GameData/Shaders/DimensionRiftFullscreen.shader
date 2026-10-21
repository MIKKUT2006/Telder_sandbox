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

        // Воронка внутри разлома.
        _VortexStrength ("Vortex Strength", Range(0, 3)) =
            1.35

        _VortexSpeed ("Vortex Speed", Range(-5, 5)) =
            1.15

        _VortexSpiral ("Vortex Spiral", Range(1, 14)) =
            6.0

        _VortexDepth ("Vortex Depth", Range(0, 1)) =
            0.82

        // Размер пикселя только для разлома и его зоны искажений.
        // Обычная сцена вне эффекта остаётся без пикселизации.
        _PixelSize ("Pixel Size", Range(1, 16)) =
            4

        [HideInInspector] _PortalBeamActive ("Portal Gun Beam Active", Float) = 0
        [HideInInspector] _PortalBeamColor ("Portal Gun Beam Color", Color) = (0.38, 0.91, 1, 1)
        [HideInInspector] _PortalBeamWidth ("Portal Gun Beam Width", Float) = 5
        [HideInInspector] _PortalBeamDistortion ("Portal Gun Beam Distortion", Float) = 6
        [HideInInspector] _PortalBeamStrength ("Portal Gun Beam Strength", Float) = 1
        [HideInInspector] _PortalBeamStart ("Portal Gun Beam Start", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PortalBeamEnd ("Portal Gun Beam End", Vector) = (0, 0, 0, 0)
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

            float _VortexStrength;
            float _VortexSpeed;
            float _VortexSpiral;
            float _VortexDepth;

            float _PixelSize;

            float _RiftOpen;
            float _RiftCollapse;

            float _PortalBeamActive;
            int _PlanetRingCount;
            float4 _PlanetRings[12];
            float4 _PlanetRingColors[12];
            int _BossOrbCount;
            float4 _BossOrbs[8];
            float4 _PortalBeamStart;
            float4 _PortalBeamEnd;
            float4 _PortalBeamColor;
            float _PortalBeamWidth;
            float _PortalBeamDistortion;
            float _PortalBeamStrength;

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


            half3 SampleSceneLinear(
                float2 sampleUv
            )
            {
                return
                    SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        saturate(sampleUv)
                    ).rgb;
            }


            half3 SampleScenePoint(
                float2 sampleUv
            )
            {
                sampleUv =
                    saturate(
                        sampleUv
                    );


                float2 sourceSize =
                    max(
                        _BlitTexture_TexelSize.zw,
                        float2(1.0, 1.0)
                    );


                int2 sourcePixel =
                    int2(
                        sampleUv
                        *
                        max(
                            sourceSize - 1.0,
                            float2(0.0, 0.0)
                        )
                    );


                return
                    LOAD_TEXTURE2D_X(
                        _BlitTexture,
                        sourcePixel
                    ).rgb;
            }


            half4 Frag(
                Varyings input
            ) : SV_Target
            {
                /*
                 * =====================================================
                 * LOCAL RIFT PIXELATION
                 * =====================================================
                 *
                 * baseUv остаётся исходным и используется для обычной
                 * сцены. Пиксельная сетка применяется только к копии UV,
                 * на которой рассчитываются разлом и его искажения.
                 */

                float2 baseUv =
                    input.texcoord;


                half3 baseScene =
                    SampleSceneLinear(
                        baseUv
                    );


                float pixelSize =
                    max(
                        _PixelSize,
                        1.0
                    );


                float2 screenSize =
                    max(
                        _ScreenParams.xy,
                        float2(1.0, 1.0)
                    );


                float2 screenPixel =
                    baseUv
                    *
                    screenSize;


                float2 snappedPixel =
                    floor(
                        screenPixel
                        /
                        pixelSize
                    )
                    *
                    pixelSize
                    +
                    pixelSize * 0.5;


                /*
                 * uv используется ТОЛЬКО разломом.
                 * Вне его маски на экран вернётся baseScene.
                 */
                float2 uv =
                    saturate(
                        snappedPixel
                        /
                        screenSize
                    );

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

                float particleField =
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


                    particleField =
                        max(
                            particleField,
                            influence
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
                    SampleScenePoint(
                        uv0
                    );


                half3 scene1 =
                    SampleScenePoint(
                        uv1
                    );


                half3 scene2 =
                    SampleScenePoint(
                        uv2
                    );


                half3 scene3 =
                    SampleScenePoint(
                        uv3
                    );


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


                /*
                 * =====================================================
                 * ROTATING VORTEX INSIDE THE RIFT
                 * =====================================================
                 *
                 * Воронка существует только внутри visual-mask.
                 * Она закручивает UV всё сильнее к центру, слегка
                 * стягивает изображение внутрь и добавляет неровность,
                 * чтобы движение не выглядело как идеальная окружность.
                 */

                float vortexRadius =
                    saturate(
                        length(
                            visualQ
                        )
                    );


                float vortexCenter =
                    1.0 -
                    smoothstep(
                        0.0,
                        1.0,
                        vortexRadius
                    );


                /*
                 * Чем ближе к центру, тем сильнее закручивание.
                 */
                float vortexTwist =
                    _VortexStrength
                    *
                    (
                        0.35 +
                        vortexCenter * 1.65
                    )
                    *
                    (
                        1.0 -
                        vortexRadius * 0.22
                    );


                float vortexAngle =
                    time *
                    _VortexSpeed *
                    1.85
                    +
                    vortexTwist *
                    2.65;


                float vortexSin =
                    sin(
                        vortexAngle
                    );


                float vortexCos =
                    cos(
                        vortexAngle
                    );


                float2 vortexLocal =
                    uv -
                    center;


                /*
                 * Вращаем точку вокруг центра разлома.
                 */
                vortexLocal =
                    float2(
                        vortexLocal.x * vortexCos -
                        vortexLocal.y * vortexSin,

                        vortexLocal.x * vortexSin +
                        vortexLocal.y * vortexCos
                    );


                /*
                 * Радиальное втягивание создаёт ощущение глубины,
                 * а не просто вращающегося диска.
                 */
                float funnelPull =
                    1.0 -
                    _VortexDepth *
                    vortexCenter *
                    0.58;


                vortexLocal *=
                    max(
                        funnelPull,
                        0.18
                    );


                /*
                 * Небольшое живое искажение самой воронки.
                 */
                float vortexNoise =
                    (
                        fbm(
                            visualQ * 2.7 +
                            float2(
                                time * 0.18,
                                -time * 0.14
                            )
                        )
                        -
                        0.5
                    );


                float2 vortexDir =
                    normalize(
                        vortexLocal +
                        float2(
                            0.00001,
                            0.00001
                        )
                    );


                float2 vortexTangent =
                    float2(
                        -vortexDir.y,
                        vortexDir.x
                    );


                vortexLocal +=
                    vortexTangent
                    *
                    vortexNoise
                    *
                    _VortexStrength
                    *
                    visualRadius
                    *
                    0.12;


                float2 vortexUv =
                    saturate(
                        center +
                        vortexLocal
                    );


                half3 vortexScene =
                    SampleScenePoint(
                        vortexUv
                    );


                /*
                 * Полярные спиральные полосы делают само вращение
                 * заметным даже на однотонном фоне.
                 */
                float polarAngle =
                    atan2(
                        visualQ.y,
                        visualQ.x
                    );


                float spiralPhase =
                    polarAngle *
                    _VortexSpiral
                    -
                    vortexRadius *
                    17.0
                    -
                    time *
                    _VortexSpeed *
                    5.0
                    +
                    vortexNoise *
                    2.2;


                float spiralWave =
                    0.5 +
                    0.5 *
                    sin(
                        spiralPhase
                    );


                spiralWave =
                    pow(
                        saturate(
                            spiralWave
                        ),
                        3.2
                    );


                float spiralMask =
                    spiralWave
                    *
                    inside
                    *
                    (
                        1.0 -
                        smoothstep(
                            0.86,
                            1.0,
                            vortexRadius
                        )
                    )
                    *
                    (
                        0.30 +
                        vortexCenter * 0.70
                    );


                /*
                 * Чем глубже к центру, тем сильнее сцена проваливается
                 * в чёрный цвет. Снаружи ещё читается закрученная материя.
                 */
                float vortexDarkness =
                    saturate(
                        0.42 +
                        vortexCenter *
                        _VortexDepth *
                        0.86
                    );


                half3 vortexColor =
                    lerp(
                        vortexScene,
                        coreColor,
                        vortexDarkness
                    );


                vortexColor +=
                    _RiftRimColor.rgb
                    *
                    spiralMask
                    *
                    _RiftGlow
                    *
                    0.48;


                /*
                 * Совсем центр почти полностью проваливается в пустоту.
                 */
                float vortexAbyss =
                    1.0 -
                    smoothstep(
                        0.0,
                        0.24,
                        vortexRadius
                    );


                vortexColor =
                    lerp(
                        vortexColor,
                        coreColor,
                        vortexAbyss *
                        0.92
                    );


                half3 result =
                    lerp(
                        scene,
                        vortexColor,
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


                /*
                 * =====================================================
                 * LOCAL EFFECT COMPOSITE
                 * =====================================================
                 *
                 * Только область разлома и реально затронутая им зона
                 * берутся из пиксельного результата. Весь остальной кадр
                 * остаётся исходным и плавным.
                 */

                float effectMask =
                    max(
                        inside,
                        rim
                    );


                effectMask =
                    max(
                        effectMask,
                        gravityInfluence
                    );


                effectMask =
                    max(
                        effectMask,
                        suctionInfluence
                        *
                        collapseCurve
                    );


                effectMask =
                    max(
                        effectMask,
                        particleField
                    );


                effectMask =
                    saturate(
                        effectMask
                    );


                half3 finalColor =
                    lerp(
                        baseScene,
                        result,
                        effectMask
                    );


                /*
                 * =====================================================
                 * PORTAL GUN BEAM
                 * =====================================================
                 * A thin travelling strip bends the already rendered world.
                 * The mask is measured in screen pixels so the beam remains
                 * consistent across aspect ratios and resolutions.
                 */
                if (_PortalBeamActive > 0.5)
                {
                    float2 beamA = _PortalBeamStart.xy * screenSize;
                    float2 beamB = _PortalBeamEnd.xy * screenSize;
                    float2 beamSegment = beamB - beamA;
                    float segmentLengthSq = max(dot(beamSegment, beamSegment), 1.0);
                    float2 beamTangent = beamSegment * rsqrt(segmentLengthSq);
                    float2 beamNormal = float2(-beamTangent.y, beamTangent.x);
                    float2 pixelPosition = baseUv * screenSize;
                    float along = saturate(dot(pixelPosition - beamA, beamSegment) / segmentLengthSq);
                    float2 closest = beamA + beamSegment * along;
                    float distanceToBeam = length(pixelPosition - closest);
                    float beamWidth = max(1.0, _PortalBeamWidth);
                    float beamBody = 1.0 - smoothstep(beamWidth * 0.30, beamWidth * 1.65, distanceToBeam);
                    float beamCore = 1.0 - smoothstep(0.25, beamWidth * 0.42, distanceToBeam);
                    float beamEdge = exp(-pow((distanceToBeam - beamWidth * 0.95) / max(1.0, beamWidth * 0.42), 2.0));
                    float beamWave = sin(along * 76.0 - time * 17.0 + sin(along * 21.0 + time * 3.4) * 2.2);
                    float beamWave2 = sin(along * 143.0 + time * 11.0);
                    float warpPixels = (beamWave * 0.75 + beamWave2 * 0.25) * _PortalBeamDistortion * saturate(_PortalBeamStrength);
                    warpPixels += sin(distanceToBeam * 0.85 - time * 20.0) * _PortalBeamDistortion * 0.22;
                    float2 beamWarpUv = beamNormal * warpPixels / screenSize;
                    half3 beamWarpedScene = SampleScenePoint(saturate(baseUv + beamWarpUv));
                    finalColor = lerp(finalColor, beamWarpedScene, saturate(beamBody * 0.82 * _PortalBeamStrength));
                    float flicker = 0.82 + 0.18 * sin(time * 31.0 + along * 90.0);
                    finalColor += _PortalBeamColor.rgb * flicker * saturate(_PortalBeamStrength) *
                                  (beamBody * 0.48 + beamCore * 0.72 + beamEdge * 0.22);
                }


                // Independent boss projectile lenses use the same fullscreen
                // scene sampling as dimensional rifts; each entry is viewport
                // center XY, horizontal viewport radius Z, intensity W.
                [unroll]
                for (int orb = 0; orb < 8; orb++)
                {
                    if (orb >= _BossOrbCount) break;
                    float4 orbData = _BossOrbs[orb];
                    float2 orbOffsetPixels = (baseUv - orbData.xy) * screenSize;
                    float orbRadiusPixels = max(orbData.z * screenSize.x, 1.0);
                    float orbDistancePixels = length(orbOffsetPixels);
                    float orbNormalizedRadius = orbDistancePixels / orbRadiusPixels;
                    float orbMask = 1.0 - smoothstep(0.76, 1.70, orbNormalizedRadius);
                    float orbInner = 1.0 - smoothstep(0.0, 1.0, orbNormalizedRadius);
                    float2 orbRadial = orbOffsetPixels / max(orbDistancePixels, 0.01);
                    float2 orbTangent = float2(-orbRadial.y, orbRadial.x);
                    float orbPulse = sin(time * 8.0 + orbNormalizedRadius * 14.0);
                    float2 orbDisplacementPixels = orbRadial * (-7.0 * orbInner + orbPulse * 3.0 * orbMask) +
                                                   orbTangent * (6.0 * orbMask * sin(time * 3.0));
                    half3 orbDistortedScene = SampleScenePoint(saturate(baseUv + orbDisplacementPixels / screenSize));
                    finalColor = lerp(finalColor, orbDistortedScene, saturate(orbMask * 0.84 * orbData.w));
                    float orbRim = exp(-pow((orbNormalizedRadius - 0.98) * 7.0, 2.0));
                    finalColor += float3(0.36, 0.08, 0.78) * orbRim * (0.55 + 0.3 * orbPulse);
                }


                // Elliptical, pixel-quantized lenses. The upper half is behind
                // the planet; the lower half passes in front of its silhouette.
                [loop] for (int ring = 0; ring < 12; ring++)
                {
                    if (ring >= _PlanetRingCount) break;
                    float4 data = _PlanetRings[ring], tint = _PlanetRingColors[ring];
                    float2 p = ((floor(baseUv * screenSize / 2) + .5) * 2 / screenSize - data.xy)
                        / max(data.zw, .00001);
                    float tilt = .25 + sin(tint.w) * .38;
                    float cs = cos(tilt), sn = sin(tilt);
                    float2 q = float2(p.x * cs - p.y * sn, p.x * sn + p.y * cs);
                    q.y /= .42;
                    float r = length(q);
                    float bandA = 1 - smoothstep(.025, .065, abs(r - 1.48));
                    float bandB = 1 - smoothstep(.015, .04, abs(r - 1.70));
                    float lens = exp(-pow((r - 1.55) * 7, 2));
                    float front = step(q.y, 0) + step(1.02, length(p));
                    float visible = saturate(front);
                    float ripple = sin(floor(_Time.y * 16) / 16 * 2.2 + atan2(q.y,q.x) * 10 + tint.w);
                    float2 radial = p / max(length(p), .001);
                    float2 warped = baseUv + radial * (lens * (2 + ripple) * visible) / screenSize;
                    half3 displaced = SampleScenePoint(warped);
                    finalColor = lerp(finalColor, displaced, saturate(lens * .8 * visible));
                    finalColor += tint.rgb * (bandA * .62 + bandB * .3) * (.8 + .2 * ripple) * visible;
                }

                return
                    half4(
                        finalColor,
                        1.0
                    );
            }

            ENDHLSL
        }
    }
}
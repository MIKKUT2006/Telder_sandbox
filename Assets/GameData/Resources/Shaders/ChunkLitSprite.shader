Shader "Game/ChunkLitSprite"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1,1,1,1)

        _LightTex ("Light Texture", 2D) = "white" {}

        _LightUVScaleOffset (
            "Light UV Scale Offset",
            Vector
        ) = (1,1,0,0)

        _LayerBrightness (
            "Layer Brightness",
            Float
        ) = 1

        _Ambient (
            "Ambient",
            Range(0,1)
        ) = 0.05

        _LightGamma (
            "Light Gamma",
            Range(0.01,3)
        ) = 0.72

        // 0 = chunk sprite UV (legacy), 1 = furniture world-space UV.
        [HideInInspector] _FurnitureWorldLightMode ("Furniture World Lighting", Float) = 0
        [HideInInspector] _FurnitureWorldLightUV ("Furniture World Light UV", Vector) = (0,0,0,0)
        [HideInInspector] _FurnitureFallbackMode ("Furniture Light Fallback", Float) = 0

    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };


            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float2 worldXY : TEXCOORD1;
            };


            sampler2D _MainTex;
            sampler2D _LightTex;

            fixed4 _Color;

            float4 _LightUVScaleOffset;

            float _LayerBrightness;
            float _Ambient;
            float _LightGamma;
            float _DebugFullBright;
            float _TelderDayFactor;
            float _FurnitureWorldLightMode;
            float4 _FurnitureWorldLightUV;
            float _FurnitureFallbackMode;


            v2f vert(
                appdata input
            )
            {
                v2f output;

                output.vertex =
                    UnityObjectToClipPos(
                        input.vertex
                    );

                output.uv =
                    input.uv;

                output.color =
                    input.color *
                    _Color;

                output.worldXY = mul(unity_ObjectToWorld, input.vertex).xy;

                return output;
            }


            fixed4 frag(
                v2f input
            ) : SV_Target
            {
                fixed4 sprite =
                    tex2D(
                        _MainTex,
                        input.uv
                    ) *
                    input.color;


                float2 chunkLightUV =
                    input.uv *
                    _LightUVScaleOffset.xy +
                    _LightUVScaleOffset.zw;

                // Furniture uses exactly the same RGBA light texture as its chunk.
                // Mapping is in WORLD units, so animation, flipping and multi-block
                // dimensions cannot shift the light along with the sprite UV.
                float2 furnitureLightUV =
                    input.worldXY * _FurnitureWorldLightUV.xy +
                    _FurnitureWorldLightUV.zw;
                float2 lightUV = lerp(chunkLightUV,
                    saturate(furnitureLightUV), saturate(_FurnitureWorldLightMode));

                float4 encodedLight = tex2D(_LightTex, lightUV);
                // Sun is in alpha; emissive RGB remains bright at night.
                float day = (_TelderDayFactor > 0.001 ? _TelderDayFactor : 1.0);
                float3 light = max(encodedLight.rgb, encodedLight.a * day);
                // Until the owning chunk is rendered, use neutral outdoor ambient
                // instead of a white flash or sampled zero from an invalid texture.
                light = lerp(light, float3(day, day, day), saturate(_FurnitureFallbackMode));


                // Smooth low-light curve.
                light =
                    pow(
                        saturate(
                            light
                        ),
                        max(
                            _LightGamma,
                            0.01
                        )
                    );


                light =
                    max(
                        light,
                        _Ambient.xxx
                    );


                light *=
                    _LayerBrightness;


                // Debug mode.
                //
                // No lighting rebuild is needed.
                // One global shader float makes all loaded and
                // future chunks immediately full-bright.
                light =
                    lerp(
                        light,
                        float3(
                            1.0,
                            1.0,
                            1.0
                        ),
                        saturate(
                            _DebugFullBright
                        )
                    );


                sprite.rgb *= saturate(light);


                return sprite;
            }

            ENDCG
        }
    }

    Fallback "Sprites/Default"
}

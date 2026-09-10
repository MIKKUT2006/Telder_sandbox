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
            };


            sampler2D _MainTex;
            sampler2D _LightTex;

            fixed4 _Color;

            float4 _LightUVScaleOffset;

            float _LayerBrightness;
            float _Ambient;
            float _LightGamma;
            float _DebugFullBright;


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


                float2 lightUV =
                    input.uv *
                    _LightUVScaleOffset.xy +
                    _LightUVScaleOffset.zw;


                float3 light =
                    tex2D(
                        _LightTex,
                        lightUV
                    ).rgb;


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


                sprite.rgb *=
                    saturate(
                        light
                    );


                return sprite;
            }

            ENDCG
        }
    }

    Fallback "Sprites/Default"
}

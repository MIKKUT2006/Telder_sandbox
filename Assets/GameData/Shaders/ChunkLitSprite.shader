Shader "Game/ChunkLitSprite"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _LightTex ("Light Texture", 2D) = "white" {}

        _LightUVScaleOffset(
            "Light UV Scale Offset",
            Vector
        ) = (1,1,0,0)

        _LayerBrightness(
            "Layer Brightness",
            Float
        ) = 1

        _Ambient(
            "Ambient",
            Float
        ) = 0.005

        _LightGamma(
            "Light Gamma",
            Float
        ) = 0.58
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
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _LightTex;

            float4 _LightUVScaleOffset;

            float _LayerBrightness;
            float _Ambient;
            float _LightGamma;

            v2f vert(appdata input)
            {
                v2f output;

                output.vertex =
                    UnityObjectToClipPos(
                        input.vertex
                    );

                output.uv =
                    input.uv;

                output.color =
                    input.color;

                return output;
            }

            fixed4 frag(v2f input)
                : SV_Target
            {
                fixed4 sprite =
                    tex2D(
                        _MainTex,
                        input.uv
                    );

                sprite *=
                    input.color;

                float2 lightUV;

                lightUV.x =
                    input.uv.x *
                    _LightUVScaleOffset.x +
                    _LightUVScaleOffset.z;

                lightUV.y =
                    input.uv.y *
                    _LightUVScaleOffset.y +
                    _LightUVScaleOffset.w;

                fixed3 lightColor =
                    tex2D(
                        _LightTex,
                        lightUV
                    ).rgb;

                lightColor =
                    max(
                        lightColor,
                        _Ambient
                    );

                lightColor =
                    pow(
                        saturate(lightColor),
                        _LightGamma
                    );

                sprite.rgb *=
                    lightColor *
                    _LayerBrightness;

                return sprite;
            }

            ENDCG
        }
    }
}
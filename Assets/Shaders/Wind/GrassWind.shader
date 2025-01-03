Shader "Custom/GrassWaving"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveAmplitude ("Wave Amplitude", Float) = 0.1
  _WaveFrequency("Wave Frequency", Float) = 1.0
        _WavingHeight ("Waving Height Ratio", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent"}
        LOD 100
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
                float4 tileData : TEXCOORD1; // tileData.xy - ������� �����
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
    float mask : TEXCOORD2;
            };

            sampler2D _MainTex;
            float _WaveSpeed;
            float _WaveAmplitude;
   float _WaveFrequency;
            float _WavingHeight;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float mask = saturate((v.tileData.y + 1.0) * _WavingHeight);
    o.mask = mask;

    float wave = sin((_Time.x * _WaveSpeed + v.tileData.x) * _WaveFrequency) * _WaveAmplitude * o.mask;

                o.vertex.x += wave;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
    return col;
            }
            ENDCG
        }
    }
}
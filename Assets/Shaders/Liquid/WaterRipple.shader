Shader "Custom/WaterRippleWithTransparency"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {} // �������� ����
        _RippleTex ("Ripple Texture", 2D) = "Yellow" {} // �������� ���� ��� ����
        _RippleStrength ("Ripple Strength", Float) = 0.1 // ���� ����
        _RippleSpeed ("Ripple Speed", Float) = 0.2 // �������� ����
        _TimeMultiplier ("Time Multiplier", Float) = 1.0 // ��������� �������
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // ������ �������
            sampler2D _MainTex;
            sampler2D _RippleTex;

            // ��������� ����
            float _RippleStrength;
            float _RippleSpeed;
            float _TimeMultiplier;

            // ��������� ��� ������ � ��������
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // ��������� ��� ������ ����� ��������� ������
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // �������������� ������
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // ����������� ������
            half4 frag(v2f i) : SV_Target
            {
                // ���������� ������� �������� ����
                half4 baseColor = tex2D(_MainTex, i.uv);
                
                // ���� ������� ��������� ����������, ���������� ���
                if (baseColor.a == 0)
                    return baseColor;

                // �������� �������� �� �������� ���� ��� ����
                float ripple = tex2D(_RippleTex, i.uv + float2(sin(_Time.y * _RippleSpeed), cos(_Time.y * _RippleSpeed)) * _RippleStrength).r;

                // ��������� ���� � ����� ����, �������� �����-�����
                baseColor.rgb += ripple * baseColor.a;

                return baseColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
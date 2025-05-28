Shader "Custom/WaterRippleWithTransparency"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {} // Текстура воды
        _RippleTex ("Ripple Texture", 2D) = "Yellow" {} // Текстура шума для ряби
        _RippleStrength ("Ripple Strength", Float) = 0.1 // Сила ряби
        _RippleSpeed ("Ripple Speed", Float) = 0.2 // Скорость ряби
        _TimeMultiplier ("Time Multiplier", Float) = 1.0 // Множитель времени
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

            // Сэмплы текстур
            sampler2D _MainTex;
            sampler2D _RippleTex;

            // Параметры ряби
            float _RippleStrength;
            float _RippleSpeed;
            float _TimeMultiplier;

            // Структура для данных о вершинах
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // Структура для данных после обработки вершин
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Преобразование вершин
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Фрагментный шейдер
            half4 frag(v2f i) : SV_Target
            {
                // Считывание базовой текстуры воды
                half4 baseColor = tex2D(_MainTex, i.uv);
                
                // Если пиксель полностью прозрачный, возвращаем его
                if (baseColor.a == 0)
                    return baseColor;

                // Получаем значение из текстуры шума для ряби
                float ripple = tex2D(_RippleTex, i.uv + float2(sin(_Time.y * _RippleSpeed), cos(_Time.y * _RippleSpeed)) * _RippleStrength).r;

                // Добавляем рябь к цвету воды, сохраняя альфа-канал
                baseColor.rgb += ripple * baseColor.a;

                return baseColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
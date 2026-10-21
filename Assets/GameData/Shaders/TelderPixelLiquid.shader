Shader "Game/TelderPixelLiquid"
{
    Properties
    {
        _MainTex("Main", 2D) = "white" {}
        _LightTex("Lighting", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _LightUVScaleOffset("Lighting UV", Vector) = (1,1,0,0)
        _LayerBrightness("Brightness", Float) = 1
        _Ambient("Ambient", Float) = 0
        _LightGamma("Gamma", Float) = .72
        _PixelsPerBlock("Pixels Per Block", Range(8,32)) = 16
        _Distortion("Wave Distortion", Range(0,1)) = .6
        _Reflection("Pixel Reflections", Range(0,1)) = .48
        _WaveSpeed("Wave Speed", Range(0,3)) = .75
        _Caustics("Caustics", Range(0,1)) = .3
        _SurfaceFoam("Surface Edge", Range(0,1)) = .45
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // Surface depth, waterfall flag, emission, reserved.
                float4 liquid : TEXCOORD1;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 world : TEXCOORD1;
                float4 liquid : TEXCOORD2;
                fixed4 color : COLOR;
            };
            sampler2D _MainTex, _LightTex;
            float4 _LightUVScaleOffset;
            fixed4 _Color;
            float _LayerBrightness, _Ambient, _LightGamma, _PixelsPerBlock;
            float _Distortion, _Reflection, _WaveSpeed, _Caustics, _SurfaceFoam;
            float _TelderDayFactor, _DebugFullBright;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.liquid = v.liquid;
                o.color = v.color * _Color;
                return o;
            }

            float wave(float2 p, float t)
            {
                return sin(p.x * 2.5 + p.y * 1.4 - t * 1.7) * .5
                     + sin(p.x * -1.1 + p.y * 3.7 + t) * .3
                     + sin(p.x * 5.1 + p.y * .8 - t * .6) * .2;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float ppb = max(8, _PixelsPerBlock);
                float2 p = (floor(i.world * ppb) + .5) / ppb;
                float t = floor(_Time.y * 24) / 24 * _WaveSpeed;
                float falling = saturate(i.liquid.y);
                float emissive = saturate(i.liquid.z);
                float depth = max(0, i.liquid.x);
                float shallow = saturate(1 - depth / 4);
                float2 flow = lerp(float2(.1, .02), float2(.05, -1.8), falling);
                float2 q = p - flow * t;

                float rawWave = wave(q, t);
                float strength = lerp(.25, 1.25, saturate(_Distortion));
                float h = rawWave * strength;
                float2 slope = float2(wave(q + float2(.12,0), t) - rawWave,
                                      wave(q + float2(0,.12), t) - rawWave) * strength;
                float2 warp = floor(slope * _Distortion * 5 + .5) / ppb;
                // Shadows remain anchored to the world, independently of distortion.
                float2 uv = i.uv + (p - i.world) / 32;
                fixed4 tex = tex2D(_MainTex, uv + warp / 32) * i.color;
                float2 lightUV = saturate(uv * _LightUVScaleOffset.xy + _LightUVScaleOffset.zw);
                float4 encoded = tex2D(_LightTex, lightUV);
                float day = saturate(_TelderDayFactor);
                float3 light = max(encoded.rgb, encoded.aaa * day);
                light = pow(saturate(light), max(.01, _LightGamma));
                light = max(light, _Ambient.xxx) * _LayerBrightness;
                light = max(light, emissive * .85);
                light = lerp(light, float3(1,1,1), saturate(_DebugFullBright));

                // Broad stepped wave planes and narrow moving highlights.
                float band = floor(saturate(h * .5 + .5) * 4) / 4;
                float3 baseColor = tex.rgb * (.86 + band * .24);
                float ridge = 1 - abs(sin(q.x * 3.7 + sin(q.y * 2.3 + t) - t));
                float caustic = step(.89, ridge) * (.35 + .65 * shallow) * _Caustics;
                baseColor += tex.rgb * caustic * (1 - emissive * .6);

                float fresnel = saturate(.35 + shallow * .4 + slope.x * 2.5);
                float specular = step(.64, h + slope.y * 1.8)
                               * step(.2, sin(q.x * 1.7 - q.y * 2.2 + t * .6));
                float3 sky = lerp(float3(.25,.38,.66), float3(.64,.84,.96), day);
                float3 reflected = lerp(sky, tex.rgb * 1.15, emissive);
                float reflection = _Reflection * fresnel * (.1 + specular * .65);
                baseColor = lerp(baseColor, reflected, reflection);

                // Only exposed surfaces receive a meniscus, never every tile seam.
                float edge = (1 - step(1.5 / ppb, depth)) * (1 - falling);
                float breakup = step(-.5, sin(p.x * 5 + t * 1.4));
                float3 foamColor = lerp(float3(.78,.94,1), float3(1,.76,.24), emissive);
                baseColor = lerp(baseColor, foamColor, edge * breakup * _SurfaceFoam);
                float streak = step(.84, sin(q.x * 16 + sin(q.y * 1.5 + t)));
                baseColor += tex.rgb * streak * falling * .16;

                float alpha = saturate(tex.a + edge * .14 + specular * _Reflection * .06);
                return fixed4(saturate(baseColor * light), alpha);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}

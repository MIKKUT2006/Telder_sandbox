
Shader "Game/PixelatedWaveLitSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _LightTex ("Light Texture", 2D) = "white" {}
        _LightUVScaleOffset ("Light UV Scale Offset", Vector) = (1,1,0,0)
        _LayerBrightness ("Layer Brightness", Float) = 1
        _Ambient ("Ambient", Range(0,1)) = 0.05
        _LightGamma ("Light Gamma", Range(0.01,3)) = 0.72

        [HideInInspector] _FurnitureWorldLightMode ("Furniture World Lighting", Float) = 0
        [HideInInspector] _FurnitureWorldLightUV ("Furniture World Light UV", Vector) = (0,0,0,0)
        [HideInInspector] _FurnitureFallbackMode ("Furniture Light Fallback", Float) = 0
        
        _WindStrength ("Wind Strength (pixels)", Range(0,8)) = 5
        _WindSpeed ("Wind Speed", Range(0,4)) = 1
        _WindFrequency ("Wind Frequency", Range(0,4)) = 1.1
        _BlockPixelSize ("Pixels Per Block", Float) = 16
        _WindWorldOffset ("World Offset", Vector) = (0,0,0,0)
        _WindPlantMode ("Whole Plant Sway (furniture)", Float) = 0
        _PlantModeTex ("Ground grass cells", 2D) = "black" {}
        _WindHanging ("Hanging vine", Float) = 0
        _WindRootY ("Vine ceiling", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
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
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _PlantModeTex;
            float4 _PlantContacts[24];
            float _WindHanging, _WindRootY;

            float plantContact(float2 p)
            {
                float reaction = 0;
                [loop] for (int n = 0; n < 24; n++)
                {
                    float4 c = _PlantContacts[n];
                    float2 d = (p - c.xy) * float2(1, .85);
                    float influence = saturate(1 - dot(d,d) / 1.69);
                    reaction += c.z * influence;
                }
                return clamp(reaction, -1.6, 1.6);
            }
            float4 _MainTex_TexelSize;
            sampler2D _LightTex;

            fixed4 _Color;
            float4 _LightUVScaleOffset;
            float4 _FurnitureWorldLightUV;
            float4 _WindWorldOffset;

            float _LayerBrightness;
            float _Ambient;
            float _LightGamma;
            float _FurnitureWorldLightMode;
            float _FurnitureFallbackMode;
            float _WindStrength;
            float _WindSpeed;
            float _WindFrequency;
            float _BlockPixelSize;
            float _WindPlantMode;
            float _DebugFullBright;
            float _TelderDayFactor;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.worldXY = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float texWidth = max(1.0, _MainTex_TexelSize.z);
                float texHeight = max(1.0, _MainTex_TexelSize.w);
                float blockPixels = max(1.0, _BlockPixelSize);

                // World-anchored sway with a pixel-art silhouette.
                // For furniture (including tall multi-block plants) normalize over
                // the ENTIRE sprite, so the bottom is rigid while the top bends.
                float2 pixel = uv * float2(texWidth, texHeight);
                float cellX = floor(pixel.x / blockPixels);
                float cellY = floor(pixel.y / blockPixels);
                float localY = frac(pixel.y / blockPixels);
                float wholePlant = step(0.5, _WindPlantMode);

                // Anchor EXACTLY two bottom texel rows. For tall furniture the
                // reference is the bottom of the whole sprite; for chunk tiles
                // it is the bottom of each 16px cell. Pixel centres (0.5, 1.5)
                // never move; row centre 2.5 is the first animated row.
                float pixelYInCell = pixel.y - cellY * blockPixels;
                float basePixelY = lerp(pixelYInCell, pixel.y, wholePlant);
                float plantPixels = lerp(blockPixels, texHeight, wholePlant);
                float height01 = saturate((basePixelY - 2.0) / max(1.0, plantPixels - 2.0));
                float heightWeight = step(2.0, basePixelY) *
                    lerp(0.22, 1.0, pow(height01, 0.78));

                float worldCellX = cellX + _WindWorldOffset.x;
                float worldCellY = cellY + _WindWorldOffset.y;
                float time = _Time.y;
                // Different spatial and temporal frequencies avoid a ruler-
                // straight, perfectly periodic wind wave.
                float gust = 0.72 + 0.28 * sin(time * 0.39 + worldCellX * 0.135);
                float phase = time * _WindSpeed +
                    worldCellX * 0.56 + worldCellY * 0.22;
                float sway = sin(phase * _WindFrequency) * gust;
                sway += 0.22 * sin(phase * 1.83 + height01 * 5.7 + time * 0.31);
                sway += 0.10 * sin(phase * 0.49 - height01 * 12.9);

                // Quantize the displacement to texel units; no subpixel blur.
                float ground = tex2D(_PlantModeTex, uv).r * (1 - wholePlant);
                float2 baseWorld = float2(worldCellX + .5, worldCellY + .25);
                baseWorld = lerp(baseWorld, float2(worldCellX + .5, worldCellY + 1.1), ground);
                float contact = plantContact(baseWorld);
                float weight = lerp(heightWeight, step(blockPixels - 3, pixelYInCell), ground);
                float displacement = sway * _WindStrength * lerp(1, .18, ground) + contact * 6;
                if (_WindHanging > .5)
                {
                    float depth = clamp(_WindRootY - i.worldXY.y, 0, 9);
                    float rootPhase = _WindWorldOffset.x * .56 + _WindRootY * .22;
                    displacement = sin(time * 1.25 + rootPhase + depth * .22) * min(3, depth * .65)
                        + plantContact(float2(_WindWorldOffset.x + .5, i.worldXY.y)) * min(7, depth * 2);
                    weight = saturate(depth * 2);
                }
                float offsetPixels = floor(clamp(displacement, -6, 6) * weight + 0.5);
                float sampleX = pixel.x - offsetPixels;
                // Chunk wave atlas is split into 16px tiles. Furniture plants
                // instead use the full sprite so the upper half may bend freely.
                float cellLeft = cellX * blockPixels;
                float chunkSampleX = clamp(sampleX, cellLeft + 0.5,
                                           cellLeft + blockPixels - 0.5);
                float plantSampleX = clamp(sampleX, 0.5, texWidth - 0.5);
                float2 sampleUV = float2(lerp(chunkSampleX, plantSampleX, wholePlant) / texWidth, uv.y);
                fixed4 color = tex2D(_MainTex, sampleUV) * i.color;

                if (color.a <= 0.001)
                    return fixed4(0, 0, 0, 0);

                // Chunk light UVs are used by terrain; furniture uses world UVs.
                float2 chunkLightUV =
                    uv * _LightUVScaleOffset.xy + _LightUVScaleOffset.zw;

                float2 furnitureLightUV =
                    i.worldXY * _FurnitureWorldLightUV.xy
                    + _FurnitureWorldLightUV.zw;

                float2 lightUV = lerp(
                    chunkLightUV,
                    saturate(furnitureLightUV),
                    saturate(_FurnitureWorldLightMode)
                );

                float4 encodedLight = tex2D(_LightTex, lightUV);

                // Preserve the existing lighting convention:
                // RGB carries block light, alpha carries sunlight.
                float day = (_TelderDayFactor > 0.001)
                    ? _TelderDayFactor
                    : 1.0;

                float3 light = max(encodedLight.rgb, encodedLight.aaa * day);
                light = lerp(
                    light,
                    float3(day, day, day),
                    saturate(_FurnitureFallbackMode)
                );

                light = pow(
                    saturate(light),
                    max(0.01, _LightGamma)
                );

                light = max(light, _Ambient.xxx);
                light *= _LayerBrightness;

                light = lerp(
                    light,
                    float3(1, 1, 1),
                    saturate(_DebugFullBright)
                );

                color.rgb *= saturate(light);
                return color;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}

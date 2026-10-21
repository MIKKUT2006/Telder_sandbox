// Pixel-art clouds with a soft, wide, PIXELATED edge blur (PS1 style).
//
// The sprite texture is a SIGNED DISTANCE field (alpha channel):
//   0.5 = on the cloud border, > 0.5 inside, < 0.5 outside,
//   one full step = _SdfRange texels. Built in ProceduralCloudSystem.
//
//   1. Every cloud is split into a grid of SQUARE pixels of the same world
//      size (_PixelSize), no matter how the sprite is scaled. The grid is
//      anchored to the cloud origin, so the cloud moves as one rigid piece.
//   2. EDGE BLUR: the distance to the border is converted into pixels, and
//      turned into a smooth coverage (0..1): soft outside the border (up to
//      _EdgeBlur pixels), firmer inside (_EdgeInner of that). The
//      coverage is then dithered into _EdgeAlphaLevels transparency steps.
//      Result: a wide, smooth fade that is still made of single pixels.
//   3. Flat cel-shading, lit from above: highlight on top, two shadow tones
//      below. The shading is dithered with the same coverage, so it fades
//      softly too.
//   4. Colors are quantized (_ColorLevels = 32 -> 5 bit per channel, like PS1).
//   5. Rain  -> storm palette, clouds grow by _RainGrow pixels, fuzzier edge.
//      Night -> tinted by the global _TelderDayFactor (same as other shaders).
//
// Assumptions: the cloud sprite is not rotated or flipped, its uv covers
// the whole texture (Sprite mesh type: Full Rect).
Shader "Game/PS1PixelCloud"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cloud Distance Field", 2D) = "white" {}

        _CloudColor ("Cloud Color", Color) = (0.88, 0.91, 0.98, 1)
        _StormColor ("Storm Color", Color) = (0.24, 0.27, 0.35, 1)
        _Rain ("Rain", Range(0, 1)) = 0

        // Size of ONE cloud pixel in world units (1 block = 1 unit).
        _PixelSize ("Pixel Size (world units)", Float) = 0.0833333

        // Set from ProceduralCloudSystem, must match the generated sprite.
        _SpriteSize ("Sprite Size (object units XY)", Vector) = (30, 15, 0, 0)
        _TexSize ("Texture Size (texels XY)", Vector) = (192, 96, 0, 0)
        _SdfRange ("Distance Field Range (texels)", Float) = 16

        // ---- Pixelated edge blur ----
        // Half-width of the soft border, in cloud pixels. 0 = hard edge.
        // Must stay below ~24 (limited by _SdfRange).
        _EdgeBlur ("Edge Blur Width (pixels)", Range(0, 24)) = 14
        // How far the blur reaches INTO the cloud, as a share of the width
        // outside. 1 = symmetric (very foggy), 0.5 = firm silhouette.
        _EdgeInner ("Edge Blur Inward Share", Range(0.1, 1)) = 0.5
        // Number of transparency steps inside the blur.
        // 1 = pure on/off dissolve, 8 = smooth.
        _EdgeAlphaLevels ("Edge Alpha Steps", Range(1, 16)) = 8
        // 0 = ordered Bayer dither (clean grain),
        // 1 = random noise stretched into vertical streaks (ragged).
        _EdgeRagged ("Edge Noise: Ordered <-> Ragged", Range(0, 1)) = 0.30
        // Length of the vertical streaks, in cloud pixels.
        _EdgeStreak ("Vertical Streak Length (pixels)", Range(1, 24)) = 4
        // 0 = static edge. >0 = the pattern re-rolls this many times per
        // second (retro "boiling"). Keep it low (2-4) or leave it at 0.
        _EdgeBoilFPS ("Edge Boil FPS (0 = static)", Range(0, 12)) = 0

        // In rain the clouds grow by this many pixels.
        _RainGrow ("Rain Growth (pixels)", Range(0, 16)) = 5

        // ---- Shading ----
        // Height of one shading band, in cloud pixels.
        _ShadeRows ("Shade Band Height (pixels)", Range(1, 16)) = 6
        _HighlightAmount ("Top Highlight", Range(0, 1)) = 0.60
        _ShadowTint ("Shadow Tint", Color) = (0.80, 0.84, 0.94, 1)
        _NightTint ("Night Tint", Color) = (0.30, 0.35, 0.55, 1)
        // Colors per channel. 32 = 5 bit (PS1 15-bit color). 64 = almost off.
        _ColorLevels ("Color Levels Per Channel", Range(4, 64)) = 32

        [PerRendererData] _Alpha ("Alpha", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            // Dynamic batching bakes vertices to world space and resets the
            // object matrix, which this shader needs for the pixel grid.
            "DisableBatching" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "PixelCloud"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Global, written by WeatherManager: ~0.26 at night .. 1 at noon.
            float _TelderDayFactor;

            CBUFFER_START(UnityPerMaterial)
                float4 _CloudColor;
                float4 _StormColor;
                float _Rain;
                float _PixelSize;
                float4 _SpriteSize;
                float4 _TexSize;
                float _SdfRange;
                float _EdgeBlur;
                float _EdgeInner;
                float _EdgeAlphaLevels;
                float _EdgeRagged;
                float _EdgeStreak;
                float _EdgeBoilFPS;
                float _RainGrow;
                float _ShadeRows;
                float _HighlightAmount;
                float4 _ShadowTint;
                float4 _NightTint;
                float _ColorLevels;
                float _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                // World-scale offset of this point from the cloud origin.
                float2 relWS : TEXCOORD1;
                // How many uv units one world unit covers on this cloud.
                float2 uvPerWorld : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;

                float4x4 m = GetObjectToWorldMatrix();

                // Rotation/scale only (no translation): a small, precise
                // number even when the cloud is far away from world origin.
                output.relWS = mul((float3x3)m, input.positionOS.xyz).xy;

                float2 scale = float2(length(m._m00_m10_m20),
                                      length(m._m01_m11_m21));
                float2 sizeWS = max(_SpriteSize.xy * scale, float2(0.0001, 0.0001));
                output.uvPerWorld = 1.0 / sizeWS;
                return output;
            }

            // Encoded signed distance (0.5 = on the border).
            float Field(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).a;
            }

            // ---- dither helpers (all work on integer pixel coordinates) ----

            // Sine-free hash, identical on every GPU.
            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Bayer2(float2 p)
            {
                return frac(p.x * 0.5 + p.y * p.y * 0.75);
            }

            // Ordered 4x4 Bayer matrix, values in (0, 1).
            float Bayer4(float2 c)
            {
                float2 a = c - 2.0 * floor(c * 0.5);
                float2 f = floor(c * 0.5);
                float2 b = f - 2.0 * floor(f * 0.5);
                return Bayer2(a) + 0.25 * Bayer2(b) + 1.0 / 32.0;
            }

            // Dither value for a pixel: Bayer pattern blended with noise that
            // is constant along short vertical runs (-> vertical streaks).
            float EdgeNoise(float2 cell, float streakLen, float frameId)
            {
                float ordered = Bayer4(cell);

                float colPhase = Hash12(float2(cell.x, 7.0));
                float run = floor((cell.y + colPhase * streakLen) / streakLen);
                float ragged = Hash12(float2(cell.x + 31.0 * frameId, run + 17.0));

                return min(lerp(ordered, ragged, _EdgeRagged), 0.999);
            }

            // Smooth coverage 0..1 of a pixel from the encoded distance.
            // 'sdScale' converts (encoded - 0.5) into pixels.
            float Coverage(float field, float sdScale, float grow, float band)
            {
                float sdPixels = (field - 0.5) * sdScale + grow;
                float p = smoothstep(-band, band * max(_EdgeInner, 0.05), sdPixels);

                // Beyond the encoded range the field is clamped: force empty.
                return p * saturate((field - 0.004) * 100.0);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float pixelSize = max(_PixelSize, 0.001);
                float rain = saturate(_Rain);

                // Square pixel grid in world units, anchored to the cloud.
                float2 cell = floor(input.relWS / pixelSize);
                float2 cellRel = (cell + 0.5) * pixelSize;

                // uv size of one cloud pixel.
                float2 cellUV = pixelSize * input.uvPerWorld;

                // Sample the field once per cloud pixel (at its center).
                float2 uvC = input.uv + (cellRel - input.relWS) * input.uvPerWorld;

                // ---- distance in texels -> distance in cloud pixels ----
                // The direction of the border (field gradient) is needed
                // because the sprite can be stretched differently in X and Y.
                float2 texUV = 1.0 / _TexSize.xy;
                float f0 = Field(uvC);
                float gx = Field(uvC + float2(texUV.x, 0.0)) - f0;
                float gy = Field(uvC + float2(0.0, texUV.y)) - f0;
                float gl = sqrt(gx * gx + gy * gy);
                float2 n = (gl > 1e-4) ? float2(gx, gy) / gl : float2(0.0, 1.0);

                // World size of one texel (x, y) on this cloud.
                float2 texelWorld = 1.0 / (input.uvPerWorld * _TexSize.xy);

                // World distance between two distance-field steps along the
                // border normal.
                float worldPerTexel = 1.0 / max(length(n / texelWorld), 1e-4);

                float sdScale = 2.0 * _SdfRange * worldPerTexel / pixelSize;

                float grow = _RainGrow * rain;

                // Rain: fuzzier border, longer streaks.
                float band = max(_EdgeBlur, 0.001) * lerp(1.0, 1.4, rain);
                float streakLen = max(_EdgeStreak, 1.0) * lerp(1.0, 1.8, rain);
                float frameId = (_EdgeBoilFPS > 0.0) ? floor(_Time.y * _EdgeBoilFPS) : 0.0;

                // ---- center pixel: smooth coverage -> dithered alpha steps ----
                float pC = Coverage(f0, sdScale, grow, band);

                // Never let the blur run into the straight edge of the quad.
                float2 borderCells = min(uvC, 1.0 - uvC) / cellUV;
                pC *= saturate(min(borderCells.x, borderCells.y) / 2.0);

                float steps = max(1.0, floor(_EdgeAlphaLevels + 0.5));
                float noiseC = EdgeNoise(cell, streakLen, frameId);
                float cover = floor(pC * steps + noiseC) / steps;

                clip(cover - 1e-4);

                // ---- shading: is there sky 1 or 2 bands above / below? ----
                float rows = max(1.0, floor(_ShadeRows + 0.5));
                float rowUV = rows * cellUV.y;

                float pUp = Coverage(Field(uvC + float2(0.0, rowUV)), sdScale, grow, band);
                float pLow1 = Coverage(Field(uvC - float2(0.0, rowUV)), sdScale, grow, band);
                float pLow2 = Coverage(Field(uvC - float2(0.0, rowUV * 2.0)), sdScale, grow, band);

                bool isTop  = pUp   <= EdgeNoise(cell + float2(0.0,  rows), streakLen, frameId);
                bool isLow1 = pLow1 <= EdgeNoise(cell - float2(0.0,  rows), streakLen, frameId);
                bool isLow2 = pLow2 <= EdgeNoise(cell - float2(0.0,  rows * 2.0), streakLen, frameId);

                float3 baseCol = lerp(_CloudColor.rgb, _StormColor.rgb, rain);

                // Storm clouds are dark, a bright rim would look too harsh.
                float highlight = _HighlightAmount * lerp(1.0, 0.30, rain);
                float3 hiCol  = lerp(baseCol, float3(1.0, 1.0, 1.0), highlight);
                float3 shCol  = baseCol * _ShadowTint.rgb;
                float3 sh2Col = shCol * _ShadowTint.rgb;

                float3 col = baseCol;
                if (isLow2) col = shCol;
                if (isLow1) col = sh2Col;
                if (isTop)  col = hiCol;

                // Night: same convention as ChunkLitSprite / TelderPixelFog.
                float day = (_TelderDayFactor > 0.001) ? _TelderDayFactor : 1.0;
                float dayT = saturate((day - 0.26) / 0.74);
                col *= lerp(_NightTint.rgb, float3(1.0, 1.0, 1.0), dayT);

                // PS1-style color depth.
                float levels = max(_ColorLevels, 2.0) - 1.0;
                col = floor(saturate(col) * levels + 0.5) / levels;

                float alpha = saturate(_Alpha * lerp(_CloudColor.a, _StormColor.a, rain)) * cover;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

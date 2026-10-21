Shader "Game/TelderPixelFog" {
 Properties {
  _FogColor("Fog Color",Color)=(.7,.75,.8,1)
  _Intensity("Intensity",Range(0,1.5))=.6
  _PixelSize("Pixel Size",Float)=0.25
  _Speed("Speed",Float)=.17
 }
 SubShader {
  Tags {"Queue"="Transparent+150" "RenderType"="Transparent"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct v { float4 pos:POSITION; float2 uv:TEXCOORD0; };
   struct f { float4 pos:SV_POSITION; float2 world:TEXCOORD0; float2 uv:TEXCOORD1; };
   float4 _FogColor; float _Intensity, _PixelSize, _Speed, _TelderDayFactor;
   f vert(v a){ f o; o.pos=UnityObjectToClipPos(a.pos); o.world=mul(unity_ObjectToWorld,a.pos).xy; o.uv=a.uv; return o; }
   float hash(float2 p){ return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p){ float2 i=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x), lerp(hash(i+float2(0,1)),hash(i+1),f.x), f.y); }
   fixed4 frag(f i):SV_Target {
     float pixelScale=max(_PixelSize,.05);
     float2 cell=floor(i.world/pixelScale)*pixelScale;
     float clock=floor(_Time.y*12)/12;
     float2 flowA=cell*0.10+float2(clock*_Speed,clock*_Speed*.18);
     float2 flowB=cell*0.18+float2(-clock*_Speed*.42,clock*_Speed*.11);
     float layerA=noise(flowA);
     float layerB=noise(flowB);
     float layerC=noise(flowA*2.15+13.7);
     float cloud=layerA*.48+layerB*.34+layerC*.18;
     cloud=smoothstep(.34,.82,cloud);

     // Soft band near the ground with gentle cut-off at quad edges.
     float localHeight=floor(i.uv.y*32)/32;
     float vertical = (.7+.3*saturate(localHeight*8)) * (1.0 - smoothstep(0.45,0.98,localHeight));
     float edge = 1;
     float wisps = noise(cell*0.27+float2(clock*_Speed*.3,0.0));
     float alpha = cloud * vertical * edge;
     alpha *= lerp(0.72,1.0,wisps);
     alpha *= saturate(_Intensity) * 0.72;

     fixed3 c = _FogColor.rgb * lerp(.52, 1.0, saturate(_TelderDayFactor));
     alpha=floor(saturate(alpha)*10+.5)/10;
     return fixed4(c, alpha);
   }
   ENDCG
  }
 }
 Fallback Off
}

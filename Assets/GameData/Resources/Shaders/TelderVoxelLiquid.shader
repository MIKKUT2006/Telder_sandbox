Shader "Game/TelderVoxelLiquid"
{
 Properties {_Color("Liquid",Color)=(.15,.5,.9,.7) _Glow("Emission",Float)=0}
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   #include "TelderHeldLight.cginc"
   float4 _Color;float _Glow,_TelderDayFactor;
   struct appdata{float4 vertex:POSITION;float4 color:COLOR;float3 normal:NORMAL;};
   struct v2f{float4 pos:SV_POSITION;float3 world:TEXCOORD0;float4 light:COLOR;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.light=v.color;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float3 p=floor(i.world*16)/16;float t=floor(_Time.y*16)/16;
    float wave=sin(p.x*2.4+p.z*1.7+t*1.2)+sin(p.z*4.2-p.x*.8-t*.9);
    float glint=step(1.35,wave)*.22;float3 light=max(i.light.rgb,i.light.aaa*saturate(_TelderDayFactor));
    light=max(light,TelderHeldLight(i.world+float3(0,.03,0)));
    light=max(light,float3(.05,.05,.06));light=max(light,_Glow.xxx);
    float3 color=(_Color.rgb+glint*float3(.6,.8,1))*light;
    return fixed4(color,_Color.a);
   }
   ENDCG
  }
 }
}

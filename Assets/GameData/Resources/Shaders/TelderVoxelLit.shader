Shader "Game/TelderVoxelLit"
{
 Properties { _MainTex("Block texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _UseVertexLighting("Vertex light",Float)=1 }
 SubShader
 {
  Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"}
  Cull Off ZWrite On
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   #include "TelderHeldLight.cginc"
   sampler2D _MainTex;float4 _Color,_VoxelSunDirection,_VoxelFogColor;
   float4 _MainTex_ST;float _UseVertexLighting;
   float _VoxelWeatherFog,_VoxelFogHeight;
   float _TelderDayFactor,_VoxelFogStart,_VoxelFogEnd;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;float4 color:COLOR;float2 ao:TEXCOORD1;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;float4 light:COLOR;float ao:TEXCOORD3;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.uv=v.uv*_MainTex_ST.xy+_MainTex_ST.zw;o.normal=UnityObjectToWorldNormal(v.normal);o.light=v.color;o.ao=v.ao.x;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    fixed4 c=tex2D(_MainTex,i.uv)*_Color;clip(c.a-.12);
    float sunFacing=.45+.55*saturate(dot(normalize(i.normal),normalize(_VoxelSunDirection.xyz+float3(.001,0,0))));
    float3 light=max(i.light.rgb,i.light.aaa*saturate(_TelderDayFactor)*sunFacing);
    light=max(light,TelderHeldLight(i.world+normalize(i.normal)*.03));
    light=pow(max(light,float3(.035,.035,.04)),.8)*max(.5,i.ao);
    c.rgb*=lerp(float3(1,1,1),light,saturate(_UseVertexLighting));
    float fog=step(.1,_VoxelFogEnd)*saturate((distance(i.world,_WorldSpaceCameraPos)-_VoxelFogStart)/max(1,_VoxelFogEnd-_VoxelFogStart));
    float3 p=floor(i.world*4)/4;float pixel=frac(sin(dot(p.xz,float2(12.9898,78.233)))*43758.5453);
    float groundFog=saturate(_VoxelWeatherFog)*saturate((_VoxelFogHeight+5-p.y)/8)*saturate(distance(i.world,_WorldSpaceCameraPos)/32);
    fog=max(fog,floor(saturate(groundFog+pixel*.08)*8)/8);
    c.rgb=lerp(c.rgb,_VoxelFogColor.rgb,fog);return c;
   }
   ENDCG
  }
 }
}

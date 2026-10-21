Shader "Skybox/TelderPixelSky3D"
{
 Properties { _SkyTint("Sky",Color)=(.45,.65,1,1) _GroundColor("Horizon",Color)=(.5,.6,.7,1) }
 SubShader
 {
  Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
  Cull Off ZWrite Off
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float4 _SkyTint,_GroundColor,_VoxelSunDirection;float _TelderDayFactor;
   struct appdata{float4 vertex:POSITION;};struct v2f{float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=mul((float3x3)unity_ObjectToWorld,v.vertex.xyz);return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   fixed4 frag(v2f i):SV_Target
   {
    float3 dir=normalize(i.dir);float2 uv=float2(atan2(dir.z,dir.x)/6.2831853+.5,asin(dir.y)/3.1415926+.5);
    uv=(floor(uv*float2(512,256))+.5)/float2(512,256);
    float longitude=(uv.x-.5)*6.2831853,latitude=(uv.y-.5)*3.1415926;
    dir=float3(cos(latitude)*cos(longitude),sin(latitude),cos(latitude)*sin(longitude));
    float day=saturate(_TelderDayFactor),night=1-smoothstep(.12,.45,day);
    float3 sky=lerp(_GroundColor.rgb,_SkyTint.rgb,saturate(dir.y*.75+.25))*lerp(.12,1,day);
    sky+=float3(.15,.19,.30)*night*saturate(dir.y);
    float stars=step(.9985,hash(floor(uv*float2(512,256))))*night*step(0,dir.y);
    sky+=float3(.8,.86,1)*stars;
    float3 sun=normalize(_VoxelSunDirection.xyz+float3(.0001,0,0));
    float sd=length(dir-sun),md=length(dir+sun);
    sky+=float3(1,.85,.5)*step(sd,.05)+float3(.55,.65,.9)*step(md,.042)*(.7+.3*hash(floor(uv*256)));
    float clock=floor(_Time.y*12)/12;
    float2 cloud=floor((dir.xz/max(.12,dir.y)*70+_WorldSpaceCameraPos.xz+float2(clock*.65,clock*.16))*.25)/.25;
    float n=noise(cloud*.018)*.65+noise(cloud*.042+13)*.35;
    float cover=step(.56,n)*step(.07,dir.y);
    float shade=floor(saturate((n-.5)*5)*3)/3;
    float3 cloudColor=lerp(float3(.52,.58,.66),float3(1,1,.96),shade)*lerp(.22,1,day);
    sky=lerp(sky,cloudColor,cover*.88);return fixed4(sky,1);
   }
   ENDCG
  }
 }
}

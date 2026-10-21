#ifndef TELDER_HELD_LIGHT_INCLUDED
#define TELDER_HELD_LIGHT_INCLUDED
sampler3D _TelderHeldLightVolume;
float4 _TelderHeldLightMin;
float _TelderHeldLightActive;
float3 TelderHeldLight(float3 world)
{
    if(_TelderHeldLightActive<.5)return float3(0,0,0);
    float3 uv=(floor(world)-_TelderHeldLightMin.xyz+.5)/31;
    if(any(uv<0)||any(uv>1))return float3(0,0,0);
    return tex3D(_TelderHeldLightVolume,uv).rgb;
}
#endif

Shader "Game/TelderPixelCelestialRays"
{
    Properties
    {
        _RayMask("Sky visibility",2D)="black" {}
        _RayColor("Ray tint",Color)=(1,.88,.57,.15)
        _PixelsPerBlock("Pixels per block",Float)=4
        _RaySource("Celestial world position",Vector)=(0,0,0,0)
        _RayPolar("Fan half angle and inverse radius",Vector)=(1.48,.02,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _RayMask;
            fixed4 _RayColor;
            float4 _RaySource,_RayPolar;
            float _PixelsPerBlock;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION; float2 world:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xy;
                return o;
            }
            float hash21(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            fixed4 frag(v2f i):SV_Target
            {
                float ppb=max(2,_PixelsPerBlock);
                float2 p=(floor(i.world*ppb)+.5)/ppb;
                float2 delta=p-_RaySource.xy;
                float angle=atan2(delta.x,-delta.y);
                float beam=angle*18;
                float2 uv=float2(angle/(2*_RayPolar.x)+.5,length(delta)*_RayPolar.y);
                float bounds=step(0,uv.x)*step(0,uv.y)*step(uv.x,1)*step(uv.y,1);
                float2 visibility=tex2D(_RayMask,saturate(uv)).rg;
                float band=floor((sin(beam*2.4)+sin(beam*5.7+.4))*.75+2)/4;
                band=saturate(.2+band*.8);
                float shimmer=.94+.06*sin(_Time.y*.6+beam*.7);
                float dust=step(.995,hash21(floor(p*ppb)))*(.5+.5*sin(_Time.y*1.2+p.y));
                float originFade=saturate(length(delta)*.8);
                float alpha=originFade*_RayColor.a*visibility.r*(.22+visibility.g*.78)
                    *band*shimmer*bounds;
                alpha+=dust*visibility.r*_RayColor.a*.18*bounds;
                return fixed4(_RayColor.rgb,saturate(alpha));
            }
            ENDCG
        }
    }
    Fallback Off
}


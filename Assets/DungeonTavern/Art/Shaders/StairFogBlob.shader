Shader "DungeonTavern/Stair Fog Blob"
{
    Properties
    {
        _Color("Fog Color", Color) = (0.02,0.025,0.04,0.6)
        _Density("Density", Range(0,2)) = 1
        _NoiseOffset("Noise Offset", Vector) = (0,0,0,0)
        _Speed("Drift Speed", Float) = 0.12
        _FogBoundsEnabled("Bounded Fog", Float) = 0
        _FogBoundsMin("Fog Minimum", Vector) = (0,0,0,0)
        _FogBoundsMax("Fog Maximum", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+150" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Density;
                float4 _NoiseOffset;
                float _Speed;
                float _FogBoundsEnabled;
                float4 _FogBoundsMin, _FogBoundsMax;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; };
            Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);return o;}
            float Hash21(float2 p){p=frac(p*float2(123.34,456.21));p+=dot(p,p+45.32);return frac(p.x*p.y);}
            float Noise(float2 p){float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);float a=Hash21(c),b=Hash21(c+float2(1,0)),d=Hash21(c+float2(0,1)),e=Hash21(c+1);return lerp(lerp(a,b,f.x),lerp(d,e,f.x),f.y);}
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.uv*2.0-1.0;
                float radial=1.0-smoothstep(0.34,1.0,length(p));
                float time=_Time.y*_Speed;
                float n1=Noise(i.uv*3.2+_NoiseOffset.xy+float2(time*.22,time*.13));
                float n2=Noise(i.uv*6.1+_NoiseOffset.zw-float2(time*.11,time*.19));
                float cloud=lerp(.52,1.0,saturate(n1*.68+n2*.32));
                float alpha=_Color.a*radial*cloud*_Density;
                if(_FogBoundsEnabled>.5)
                {
                    float3 edge=min(i.positionWS-_FogBoundsMin.xyz,_FogBoundsMax.xyz-i.positionWS);
                    clip(min(edge.x,min(edge.y,edge.z)));
                    alpha*=smoothstep(0,.12,min(edge.x,min(edge.y,edge.z)));
                }
                return half4(_Color.rgb*lerp(.70,1.28,n1),alpha);
            }
            ENDHLSL
        }
    }
}

Shader "DungeonTavern/Wall Cutout Unlit"
{
    Properties
    {
        _BaseMap("Texture",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _Cutoff("Alpha Cutoff",Range(0,1))=.5
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Cull Off ZWrite On
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "../TavernWallCutout.hlsl"
        #include "../TavernWallSection.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;half4 _BaseColor;float _Cutoff;float _TavernSurfaceOnly;
        CBUFFER_END
        struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
        struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD1;};
        Varyings Vert(Attributes v){Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);return o;}
        half4 Frag(Varyings i,FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC, out float depth:SV_Depth):SV_Target
        {
            depth=i.positionCS.z;
            if (!IS_FRONT_VFACE(facing,true,false) && _TavernSurfaceOnly < .5)
            {
                float3 section=TavernSectionPoint(i.positionWS);
                depth=TavernSectionDepth(section);
                return TavernSectionColor(section);
            }
            TavernWallClip(i.positionWS);
            half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
            clip(c.a-_Cutoff);
            return c;
        }
        ENDHLSL
        Pass
        {
            Name "Unlit" Tags{"LightMode"="SRPDefaultUnlit"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        // Original geometry continues casting shadows; the view-dependent hole does not affect lighting.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}

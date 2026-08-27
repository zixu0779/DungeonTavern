Shader "DungeonTavern/Cup Magic Particle"
{
    Properties { _Tint("Tint", Color) = (1,1,1,1) _Color("Color", Color) = (1,1,1,1) _Ring("Ring", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint; half4 _Color; half _Ring;
            CBUFFER_END
            Varyings Vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; o.color=v.color*_Tint*_Color; return o; }
            half4 Frag(Varyings i):SV_Target { float2 p=i.uv*2-1; half fade= _Ring > .5 ? exp(-pow((length(p)-.82)/.07,2)) : pow(saturate(1-dot(p,p)),2); return half4(i.color.rgb,i.color.a*fade); }
            ENDHLSL
        }
    }
}

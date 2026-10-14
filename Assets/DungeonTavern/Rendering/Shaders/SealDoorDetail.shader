Shader "DungeonTavern/Seal Door Detail"
{
 Properties { _BaseMap("Original door texture",2D)="white"{} }
 SubShader {
 Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 float4 _BaseMap_TexelSize;
 struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
 struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target {
 half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
 float2 d=_BaseMap_TexelSize.xy*5;
 half3 blur=(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv+float2(d.x,d.y)).rgb+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv+float2(-d.x,d.y)).rgb+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv+float2(d.x,-d.y)).rgb+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv-d).rgb)*.25;
 // Original samples only: sharpen luminance without inventing cracks or shifting hue.
 half detail=dot(c.rgb-blur,half3(.2126,.7152,.0722));
 half lum=max(dot(c.rgb,half3(.2126,.7152,.0722)),.0001);
 c.rgb*=clamp((lum+detail*1.5)/lum,.65,1.35);
 return c;
 }
 ENDHLSL
 }
 }
}

Shader "DungeonTavern/Seal Door Surface" {
Properties { [HideInInspector] _TavernCuttable("Wall cutout",Float)=0 _BaseMap("Restored door",2D)="white"{}}
SubShader {Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Pass {
Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../TavernWallCutout.hlsl"
#include "../TavernWallSection.hlsl"
float _TavernSurfaceOnly;
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.world=TransformObjectToWorld(a.p.xyz);return o;}
half4 frag(V i,FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC,out float depth:SV_Depth):SV_Target{depth=i.p.z;
if(!IS_FRONT_VFACE(facing,true,false)&&_TavernSurfaceOnly<.5&&_TavernCutGroup>0){float3 section=TavernSectionPoint(i.world);depth=TavernSectionDepth(section);return TavernSectionColor(section);}
TavernWallClip(i.world);half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);c.rgb*=lerp(1,.78,smoothstep(.84,1,i.uv.y));return c;}
ENDHLSL
}}}

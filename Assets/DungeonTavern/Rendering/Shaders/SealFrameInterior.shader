Shader "DungeonTavern/Seal Frame Interior"
{
 Properties { [HideInInspector] _TavernCuttable("Wall cutout",Float)=0  _BaseMap("Stone",2D)="white"{} _Interior("Interior",Float)=1 }
 SubShader { Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Pass {
 Cull Off
HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../TavernWallCutout.hlsl"
#include "../TavernWallSection.hlsl"
float _TavernSurfaceOnly;
#include "SealStoneEdges.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float _Interior;
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float3 n:NORMAL;};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 n:TEXCOORD2;};
 V vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.uv=a.uv;o.n=TransformObjectToWorldNormal(a.n);return o;}
 half4 frag(V i,FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC,out float depth:SV_Depth):SV_Target {
 depth=i.p.z;
if(!IS_FRONT_VFACE(facing,true,false)&&_TavernSurfaceOnly<.5&&_TavernCutGroup>0){float3 section=TavernSectionPoint(i.world);depth=TavernSectionDepth(section);return TavernSectionColor(section);}
TavernWallClip(i.world);
 half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
 c.rgb*=lerp(1,1.55,saturate(i.n.y));
 if(_Interior>.5)c.rgb*=lerp(.9,.38,smoothstep(2.25,3.0,i.world.y));
 float zdist=min(min(abs(i.world.z-12.87),abs(i.world.z-15.25)),min(abs(i.world.z-22.05),abs(i.world.z-24.43)));
 float xdist=min(abs(i.world.x-49.689),abs(i.world.x-50.397));
 float distanceToEdge=abs(i.n.x)>.8?zdist:xdist;

 float rim=SealEdge(distanceToEdge)*step(i.world.y,2.99)*step(abs(i.n.y),.2);
 c.rgb=SealStoneHighlight(c.rgb,rim);
 return c;}
 ENDHLSL
 }}
}

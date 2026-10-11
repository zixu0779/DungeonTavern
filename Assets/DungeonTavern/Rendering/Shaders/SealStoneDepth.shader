Shader "DungeonTavern/Seal Stone Depth" {
Properties { [HideInInspector] _TavernCuttable("Wall cutout",Float)=0 _BaseMap("Stone",2D)="white"{} _Depth("Authored depth",Float)=1 _SrcBlend("Source",Float)=1 _DstBlend("Destination",Float)=0 _ZWrite("Depth write",Float)=1}
SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {
Cull Off Blend [_SrcBlend] [_DstBlend] ZWrite [_ZWrite]
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../TavernWallCutout.hlsl"
#include "../TavernWallSection.hlsl"
float _TavernSurfaceOnly;
#include "SealStoneEdges.hlsl"
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float _Depth;
struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float2 xy:TEXCOORD1;float2 zi:TEXCOORD2;float4 color:COLOR;float4 size:TEXCOORD4;};
struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float2 uv:TEXCOORD1;float3 box:TEXCOORD2;float id:TEXCOORD3;float4 color:COLOR;float4 size:TEXCOORD4;float3 world:TEXCOORD5;};
V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.box=float3(a.xy,a.zi.x);o.id=a.zi.y;o.color=a.color;o.size=a.size;o.world=TransformObjectToWorld(a.p.xyz);return o;}
half4 frag(V i,FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC,out float depth:SV_Depth):SV_Target{depth=i.p.z;
if(!IS_FRONT_VFACE(facing,true,false)&&_TavernSurfaceOnly<.5&&_TavernCutGroup>0){float3 section=TavernSectionPoint(i.world);depth=TavernSectionDepth(section);return TavernSectionColor(section);}
TavernWallClip(i.world);half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*i.color;if(_Depth<.5)return c;
float3 n=normalize(i.n);
bool shaft=abs(i.id-3)<.1||abs(i.id-7)<.1;
bool cap=abs(i.id-4)<.1||abs(i.id-8)<.1||i.id<.1;
bool lower=abs(i.id-1)<.1||abs(i.id-5)<.1;
float3 size=i.size.xyz;
float3 d=min(i.box,1-i.box)*size;
float shade=lerp(.84,1,abs(n.x));if(n.y>.3)shade=lerp(shade,1.55,n.y);if(n.y<-.3)shade=lerp(shade,.23,-n.y);
// Same world-space width on both adjacent faces, including horizontal tops.
float faceDistance=max(min(d.x,d.y),min(max(d.x,d.y),d.z));
float rim=SealEdge(faceDistance);
float contact=shaft?1-smoothstep(.035,.16,(1-i.box.y)*size.y):cap?1-smoothstep(0,.04,i.box.y*size.y):0;
if(n.y<-.2)rim=0;
// Contact on the exposed capital side where it emerges through the wall top.
if(cap&&abs(n.z)>.6&&i.world.x>49.68&&i.world.x<50.41)contact=max(contact,SealContact(abs(i.world.y-4.2))*.22);
// The top of each foot receives the contact shadow of the tier standing on it.
if(!shaft&&!cap&&n.y>.8){float2 nextHalf=lower?float2(1.566,1.06)*.5:float2(1.286,.68)*.5;float2 q=abs((i.box.xz-.5)*size.xz)-nextHalf;float outside=max(q.x,q.y);contact=max(contact,1-smoothstep(0,.14,max(0,outside)));}
if(shaft&&i.box.y<.04)contact=max(contact,1-smoothstep(0,.055,i.box.y*size.y));
c.rgb*=shade;
c.rgb=SealStoneHighlight(c.rgb,rim*(1-contact));
c.rgb*=1-contact*.93;
if(!shaft&&!cap)c.rgb*=lerp(.24,1,smoothstep(.004,.04,i.box.y*size.y)); return c;}
ENDHLSL
}}}

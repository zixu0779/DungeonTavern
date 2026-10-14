Shader "DungeonTavern/Seal Wall Edges" {
Properties { [HideInInspector] _TavernCuttable("Wall cutout",Float)=0 _BaseMap("Stone",2D)="white"{} _ArchFrame("Arch frame",Float)=0 _Corner("Corner",Float)=0 _TopGain("Top brightness",Float)=1.55 _West("West wall",Float)=0}
SubShader {Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Pass {
Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "../TavernWallCutout.hlsl"
#include "../TavernWallSection.hlsl"
float _TavernSurfaceOnly;
#include "SealStoneEdges.hlsl"
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);float _ArchFrame;float _Corner;float _TopGain;float _West;
struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float2 edge:TEXCOORD3;float4 bounds:TEXCOORD4;};
struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float2 uv:TEXCOORD1;float2 edge:TEXCOORD2;float3 world:TEXCOORD3;float4 bounds:TEXCOORD4;};
V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.edge=a.edge;o.bounds=a.bounds;o.world=TransformObjectToWorld(a.p.xyz);return o;}
float contactRect(float2 p,float2 center,float2 halfSize){float2 q=abs(p-center)-halfSize;return 1-smoothstep(0,.14,max(max(q.x,q.y),0));}
half4 frag(V i,FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC,out float depth:SV_Depth):SV_Target{depth=i.p.z;
if(!IS_FRONT_VFACE(facing,true,false)&&_TavernSurfaceOnly<.5&&_TavernCutGroup>0){float3 section=TavernSectionPoint(i.world);depth=TavernSectionDepth(section);return TavernSectionColor(section);}
TavernWallClip(i.world);half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);float3 n=normalize(i.n);float up=saturate(n.y);float side=1-abs(n.y);c.rgb*=lerp(.86,_TopGain,up);
float dist=up>.8?min(min(i.bounds.x,i.bounds.y),min(i.bounds.z,i.bounds.w)):i.edge.x;
float raised=step(4.2,i.world.y);
if(_Corner>.5&&side>.5){
    float centerX=i.world.x+(i.bounds.y-i.bounds.x)*.5;
    float centerZ=i.world.z+(i.bounds.w-i.bounds.z)*.5;
    bool west=centerX<40;bool south=centerZ<18;
    float outerX=west?i.bounds.x:i.bounds.y;
    float outerZ=south?i.bounds.z:i.bounds.w;
    float outerEdge=max(outerX,outerZ);
    float everyEdge=abs(n.x)>abs(n.z)?min(i.bounds.z,i.bounds.w):min(i.bounds.x,i.bounds.y);
    dist=min(dist,raised>.5?everyEdge:outerEdge);
    // Only the two faces that actually meet the adjacent walls receive contact shade.
    float touchingX=west?i.bounds.y:i.bounds.x;
    float touchingZ=south?i.bounds.w:i.bounds.z;
    float attached=max((1-smoothstep(.001,.012,touchingX))*abs(n.x),(1-smoothstep(.001,.012,touchingZ))*abs(n.z));
    float joint=(1-smoothstep(0,.08,abs(i.world.y-4.2)))*attached;
    c.rgb*=1-joint*.30;
}
float edgeLight=max(SealEdge(dist),SealStairRim(i.world,n));
if(up>.8)edgeLight*=1-SealStairTopSeam(i.world);
if(_ArchFrame>.5)edgeLight=max(edgeLight,SealArchRim(i.world,n));
// Fixed wall contact shadows; local authoring data enables only the east wall.
if(i.edge.y>.5&&abs(n.x)>.8){float shadow=0;float2 p=i.world.zy;
for(int k=0;k<2;k++){float z=k==0?16.81:20.49;
shadow=max(shadow,contactRect(p,float2(z,.11),float2(.71,.11)));
shadow=max(shadow,contactRect(p,float2(z,.42),float2(.53,.20)));
shadow=max(shadow,contactRect(p,float2(z,2.2),float2(.34,1.58)));
shadow=max(shadow,contactRect(p,float2(z,4.055),float2(.44,.275)));}
shadow=max(shadow,contactRect(p,float2(22.94,4.055),float2(.54,.275)));c.rgb*=1-shadow*.85;}
float contact=max(SealRoomCorner(i.world,n),up>.8?SealWallTopContact(i.world):0);
contact=max(contact,max(SealStairCorner(i.world,n),SealStairRiserContact(i.world,n)));
contact=max(contact,SealArchJambContact(i.world,n,_ArchFrame));

c.rgb*=1-contact*.85;
// Keep exposed convex edges readable; suppress highlights only in actual concave contacts.
float exposedEdge=edgeLight*(1-contact);
c.rgb=SealStoneHighlight(c.rgb,exposedEdge);
return c;}
ENDHLSL
}}}

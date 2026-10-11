Shader "DungeonTavern/Seal Wall Band"
{
 Properties { _BaseMap("Reference",2D)="white"{} _WallMap("Wall",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_WallMap);SAMPLER(sampler_WallMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;
 CBUFFER_END
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float3 n:NORMAL;};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 n:TEXCOORD2;};
 V vert(A a){V v;v.world=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.world);v.uv=a.uv;v.n=TransformObjectToWorldNormal(a.n);return v;}
 half4 frag(V v):SV_Target{
 bool top=abs(v.n.y)>.7;
 float2 uv=top?float2(frac(v.world.z/1.55),frac(v.world.x/1.55)):float2(frac(v.world.z/1.55),saturate((v.world.y-3.88)/.322));
 // Sample the reference's flat top and front strips without the projecting block.
 float2 atlas=top?float2(lerp(.015,.345,uv.x),lerp(.57,.96,uv.y)):float2(lerp(.015,.345,uv.x),lerp(.035,.53,uv.y));
 half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,atlas)*_BaseColor;
 half4 wall=SAMPLE_TEXTURE2D(_WallMap,sampler_WallMap,v.world.zy/3);
 float edge=3.88+.055*sin(v.world.z*13)+.018*sin(v.world.z*31);
 float blend=top?1:smoothstep(edge,edge+.12,v.world.y);
 c.rgb=lerp(wall.rgb,c.rgb,blend);return c;
 }
 ENDHLSL } }
}

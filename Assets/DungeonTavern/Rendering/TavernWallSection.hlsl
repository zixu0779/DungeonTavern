#ifndef TAVERN_WALL_SECTION_INCLUDED
#define TAVERN_WALL_SECTION_INCLUDED
TEXTURE2D(_TavernBrickCore); SAMPLER(sampler_TavernBrickCore);
TEXTURE2D(_TavernStoneCore); SAMPLER(sampler_TavernStoneCore);
float4 _TavernWallMin, _TavernWallMax;
float4x4 _TavernWallWorldToLocal;
float _TavernStoneSection, _TavernSectionsEnabled;

// Back faces bound the far side of the existing wall, not a screen-space fill quad.
// ponytail: intended for wall slabs; disjoint/concave shells need separate slab meshes.
// Find the first surviving point along this ray inside the wall's local bounds.
float3 TavernSectionPoint(float3 back)
{
    if (!TavernCutCameraActive() || _TavernSectionsEnabled < .5 ||
        max(_TavernCutTransition0,_TavernCutTransition1)<=0) discard;
    if (TavernWallField(back)<0) discard;
    float3 ray = _TavernCutOrthographic>.5 ? _TavernCutForward.xyz : normalize(back-_WorldSpaceCameraPos);
    float3 local = mul(_TavernWallWorldToLocal,float4(back,1)).xyz;
    float3 toward = mul((float3x3)_TavernWallWorldToLocal,-ray);
    float3 extent = lerp(_TavernWallMin.xyz,_TavernWallMax.xyz,step(0,toward));
    float3 safeDirection = lerp(-1,1,step(0,toward))*max(abs(toward),.00001);
    float3 times = (extent-local) / safeDirection;
    times = lerp(times,10000,step(abs(toward),.00001));
    float length = min(times.x,min(times.y,times.z));
    if (length < .002 || length > 20) discard;
    float3 entry = back-ray*length;
    if (TavernWallField(entry)>=0) discard;
    float lo=0, hi=0;
    // Fixed upper bound; small walls use fewer samples. No CPU mesh rebuild each frame.
    int count = min(128,max(8,(int)ceil(length/.02)));
    bool found=false;
    [loop] for(int i=1;i<=count;i++)
    {
        hi=length*(float)i/count;
        if(TavernWallField(entry+ray*hi)>=0){found=true;break;}
        lo=hi;
    }
    if(!found) discard;
    [unroll] for(int j=0;j<6;j++)
    {
        float mid=(lo+hi)*.5;
        if(TavernWallField(entry+ray*mid)<0) lo=mid; else hi=mid;
    }
    return entry+ray*hi;
}
float TavernSectionDepth(float3 p)
{
    float4 clipPosition=TransformWorldToHClip(p);
    float depth=clipPosition.z/clipPosition.w;
    #if !UNITY_REVERSED_Z
        depth=(depth-UNITY_NEAR_CLIP_VALUE)/(1-UNITY_NEAR_CLIP_VALUE);
    #endif
    return depth;
}
half4 TavernSectionColor(float3 p)
{
    const float e=.006;
    float3 n=normalize(float3(TavernWallField(p+float3(e,0,0))-TavernWallField(p-float3(e,0,0)),
        TavernWallField(p+float3(0,e,0))-TavernWallField(p-float3(0,e,0)),
        TavernWallField(p+float3(0,0,e))-TavernWallField(p-float3(0,0,e)))+float3(.00001,0,0));
    float3 w=pow(abs(n),4);w/=max(w.x+w.y+w.z,.0001);
    // World-space metre scale keeps aggregate fixed when the camera or cut moves.
    float3 uv=p*1.3;
    half3 c;
    if(_TavernStoneSection>.5)
        c=SAMPLE_TEXTURE2D_LOD(_TavernStoneCore,sampler_TavernStoneCore,uv.yz,1).rgb*w.x+
          SAMPLE_TEXTURE2D_LOD(_TavernStoneCore,sampler_TavernStoneCore,uv.xz,1).rgb*w.y+
          SAMPLE_TEXTURE2D_LOD(_TavernStoneCore,sampler_TavernStoneCore,uv.xy,1).rgb*w.z;
    else
        c=SAMPLE_TEXTURE2D_LOD(_TavernBrickCore,sampler_TavernBrickCore,uv.yz,1).rgb*w.x+
          SAMPLE_TEXTURE2D_LOD(_TavernBrickCore,sampler_TavernBrickCore,uv.xz,1).rgb*w.y+
          SAMPLE_TEXTURE2D_LOD(_TavernBrickCore,sampler_TavernBrickCore,uv.xy,1).rgb*w.z;
    return half4(c*lerp(.7,.34,_TavernStoneSection),1);
}
#endif

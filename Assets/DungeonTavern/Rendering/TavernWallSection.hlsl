#ifndef TAVERN_WALL_SECTION_INCLUDED
#define TAVERN_WALL_SECTION_INCLUDED
TEXTURE2D(_SectionMap); SAMPLER(sampler_SectionMap);
float4 _SectionTint, _SectionST, _SectionU, _SectionV, _SectionRect, _SectionScale, _SectionOptions;
float _SectionNative;
float4 _SectionTriangles[288];
int _SectionTriangleCount;
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
    // Low-poly architectural meshes use their actual front intersection, including
    // mitred joints and hollow doorway outlines, rather than filling an enclosing box.
    if(_SectionTriangleCount>0)
    {
        float nearest=10000;
        [loop] for(int k=0;k<_SectionTriangleCount;k++)
        {
            float3 a=_SectionTriangles[k*3].xyz,b=_SectionTriangles[k*3+1].xyz,c=_SectionTriangles[k*3+2].xyz;
            float3 e=b-a,f=c-a,h=cross(toward,f);float det=dot(e,h);
            if(abs(det)<.000001)continue;
            float3 delta=local-a;float u=dot(delta,h)/det;if(u<-.00001||u>1.00001)continue;
            float3 q=cross(delta,e);float v=dot(toward,q)/det;if(v<-.00001||u+v>1.00001)continue;
            float t=dot(f,q)/det;if(t>.001)nearest=min(nearest,t);
        }
        if(nearest>9999)discard;
        length=nearest;entry=back-ray*length;
        if(TavernWallField(entry)>=0)discard;
    }
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
    half4 color=half4(1,1,1,1);
    if(_TavernStoneSection>.5)
    {
        const float e=.01;
        float3 n=float3(TavernWallField(p+float3(e,0,0))-TavernWallField(p-float3(e,0,0)),
                        TavernWallField(p+float3(0,e,0))-TavernWallField(p-float3(0,e,0)),
                        TavernWallField(p+float3(0,0,e))-TavernWallField(p-float3(0,0,e)));
        float3 w=pow(abs(n)+.00001,4);w/=max(w.x+w.y+w.z,1e-20);
        float density=max(length(_SectionU.xyz),length(_SectionV.xyz));float3 q=p*max(density,.25);
        color=(SAMPLE_TEXTURE2D_LOD(_SectionMap,sampler_SectionMap,q.yz*_SectionST.xy+_SectionST.zw,0)*w.x+
               SAMPLE_TEXTURE2D_LOD(_SectionMap,sampler_SectionMap,q.xz*_SectionST.xy+_SectionST.zw,0)*w.y+
               SAMPLE_TEXTURE2D_LOD(_SectionMap,sampler_SectionMap,q.xy*_SectionST.xy+_SectionST.zw,0)*w.z)*_SectionTint;
    }
    else
    {
        float4 local=mul(_TavernWallWorldToLocal,float4(p,1));
        float2 uv=float2(dot(local,_SectionU),dot(local,_SectionV));
        if(_SectionNative>.5)
        {
            float turns=_SectionOptions.x;
            bool odd=(turns>.5 && turns<1.5)||turns>2.5;
            uv=frac(uv*(odd?_SectionScale.yx:_SectionScale.xy));
            if(turns>=2.5)uv=float2(1-uv.y,uv.x);
            else if(turns>=1.5)uv=1-uv;
            else if(turns>=.5)uv=float2(uv.y,1-uv.x);
            if(_SectionOptions.y>.5)uv.x=1-uv.x;
            if(_SectionOptions.z>.5)uv.y=1-uv.y;
            uv=_SectionRect.xy+uv*_SectionRect.zw;
        }
        else uv=uv*_SectionST.xy+_SectionST.zw;
        color=SAMPLE_TEXTURE2D_LOD(_SectionMap,sampler_SectionMap,uv,0)*_SectionTint;
    }
    clip(color.a-.01);return color;
}
#endif

#ifndef TAVERN_WALL_CUTOUT_INCLUDED
#define TAVERN_WALL_CUTOUT_INCLUDED
// World-space sphere cutouts inspired by Brendan Sullivan's public BG3 breakdown.
// This is an original URP implementation, not the author's Unreal source.
float4 _TavernCutSphere0, _TavernCutSphere1;
float4 _TavernCutCamera, _TavernCutForward;
float _TavernCuttable;
float _TavernCutGroup;
float CutHash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
float CutNoise(float3 p)
{
    float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(lerp(CutHash(i),CutHash(i+float3(1,0,0)),f.x),lerp(CutHash(i+float3(0,1,0)),CutHash(i+float3(1,1,0)),f.x),f.y),
        lerp(lerp(CutHash(i+float3(0,0,1)),CutHash(i+float3(1,0,1)),f.x),lerp(CutHash(i+float3(0,1,1)),CutHash(i+float3(1,1,1)),f.x),f.y),f.z);
}
float _TavernCutOrthographic, _TavernCutTransition0, _TavernCutTransition1, _TavernCutPair;
// Closest point on the view segment: the sphere extends toward the camera.
// Orthographic rays must remain parallel, including for off-centre actors.
float CutChannel(float3 p, float4 sphere, float transition, float noise)
{
    if (sphere.w < .001 || transition <= 0) return 1;
    float3 start = _TavernCutCamera.xyz;
    if (_TavernCutOrthographic > .5)
        start = sphere.xyz - _TavernCutForward.xyz * max(0, dot(sphere.xyz-start, _TavernCutForward.xyz));
    float3 ab = sphere.xyz-start;
    float t = saturate(dot(p-start,ab) / max(dot(ab,ab), .0001));
    float d = distance(p, start+t*ab);
    // Broad fragmented dissolve band around the protected silhouette.
    float field = d-sphere.w+(noise-.5)*1.8;
    return lerp(sphere.w+.2,field,transition);
}
// A single volume field for exterior pixels and the reconstructed interior.
// Surface-normal-dependent noise would give the two sides different cut boundaries.
float TavernWallField(float3 p)
{
    // Keep noise constant through wall depth so filling does not heal the fragments.
    float3 noisePosition=p-dot(p,_TavernCutForward.xyz)*_TavernCutForward.xyz;
    // Membership fades across the existing complete footprint, never rescales it.
    // World-projected stippling stays identical through wall depth and section filling.
    if (_TavernCutGroup <= 0 || (_TavernCutGroup < 1 &&
        CutHash(floor(noisePosition*80)) >= _TavernCutGroup)) return 1;
    float noise = (CutNoise(noisePosition * 6)*.75 + CutNoise(noisePosition * 14)*.25);
    float field = min(CutChannel(p,_TavernCutSphere0,_TavernCutTransition0,noise),
                      CutChannel(p,_TavernCutSphere1,_TavernCutTransition1,noise));
    if (_TavernCutPair>.001 && min(_TavernCutSphere0.w,_TavernCutSphere1.w)>.001)
    {
        // A continuous bridge in the camera plane joins both body silhouettes.
        // Only wall groups hit by either actor's probes receive this shared opening.
        float3 ab=_TavernCutSphere1.xyz-_TavernCutSphere0.xyz;
        float3 offset=p-_TavernCutSphere0.xyz;
        ab-=dot(ab,_TavernCutForward.xyz)*_TavernCutForward.xyz;
        offset-=dot(offset,_TavernCutForward.xyz)*_TavernCutForward.xyz;
        float t=saturate(dot(offset,ab)/max(dot(ab,ab),.0001));
        float4 bridge=lerp(_TavernCutSphere0,_TavernCutSphere1,t);
        field=min(field,CutChannel(p,bridge,min(_TavernCutTransition0,_TavernCutTransition1)*_TavernCutPair,noise));
    }
    return field;
}
bool TavernCutCameraActive()
{
    return _TavernCuttable > .5 && distance(_WorldSpaceCameraPos,_TavernCutCamera.xyz)<.1;
}
void TavernWallClip(float3 positionWS)
{
    if (TavernCutCameraActive()) clip(TavernWallField(positionWS));
}
#endif

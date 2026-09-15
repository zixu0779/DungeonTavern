#ifndef TAVERN_WALL_CUTOUT_INCLUDED
#define TAVERN_WALL_CUTOUT_INCLUDED
// World-space sphere cutouts inspired by Brendan Sullivan's public BG3 breakdown.
// This is an original URP implementation, not the author's Unreal source.
float4 _TavernCutSphere0, _TavernCutSphere1;
float4 _TavernCutActor0, _TavernCutActor1;
float4 _TavernCutCamera, _TavernCutForward;
float _TavernCuttable;
float CutHash(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
float CutNoise(float3 p)
{
    float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(lerp(CutHash(i),CutHash(i+float3(1,0,0)),f.x),lerp(CutHash(i+float3(0,1,0)),CutHash(i+float3(1,1,0)),f.x),f.y),
        lerp(lerp(CutHash(i+float3(0,0,1)),CutHash(i+float3(1,0,1)),f.x),lerp(CutHash(i+float3(0,1,1)),CutHash(i+float3(1,1,1)),f.x),f.y),f.z);
}
float _TavernCutOrthographic, _TavernCutTransition0, _TavernCutTransition1;
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
    float mask = smoothstep(0, sphere.w, d);
    // Independent transition, as in the video's Lerp(mask, 1, 1-transition).
    // Noise breaks up a broad edge instead of wobbling a hard spherical cut.
    return lerp(1, mask, transition) - lerp(.38, .68, noise);
}
// A single volume field for exterior pixels and the reconstructed interior.
// Surface-normal-dependent noise would give the two sides different cut boundaries.
float TavernWallField(float3 p)
{
    float noise = CutNoise(p * 13);
    return min(CutChannel(p,_TavernCutSphere0,_TavernCutTransition0,noise),
               CutChannel(p,_TavernCutSphere1,_TavernCutTransition1,noise));
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

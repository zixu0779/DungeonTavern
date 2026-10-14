#ifndef SEAL_STONE_EDGES_INCLUDED
#define SEAL_STONE_EDGES_INCLUDED
// Preserve authored width at close range; integrate subpixel coverage at grazing angles.
// Box-filter the authored smooth edge profile analytically, preserving energy below one pixel.
float SealEdgeIntegral(float x)
{
    float u=saturate((x-.004)/.016);
    return min(max(x,0),.004)+.016*(u-u*u*u+.5*u*u*u*u);
}
float SealEdgePrimitive(float x){return sign(x)*SealEdgeIntegral(abs(x));}
float SealEdge(float distanceToEdge)
{
    float halfPixel=max(fwidth(distanceToEdge)*.5,.00005);
    return saturate((SealEdgePrimitive(distanceToEdge+halfPixel)-SealEdgePrimitive(distanceToEdge-halfPixel))/(2*halfPixel));
}
float3 SealStoneHighlight(float3 stone,float coverage)
{
    // Preserve hue and stone texture instead of painting a flat grey stripe.
    float3 raised=min(stone*1.65+float3(.018,.022,.023),float3(.127438,.171441,.181164));
    return lerp(stone,max(stone,raised),coverage);
}
float SealContact(float distanceToContact)
{
    return 1-smoothstep(.018,.16,max(0,distanceToContact));
}
float SealTopRect(float2 p,float2 center,float2 halfSize)
{
    float2 q=abs(p-center)-halfSize;
    return SealContact(max(q.x,q.y));
}
// Fixed B1 wall authoring coordinates, matching the integrated whole-wall meshes.
float SealWallTopContact(float3 p)
{
    float contact=0;
    float east=0;
    float2 capCenters[3]={float2(50.043,16.81),float2(50.043,20.49),float2(50.043,22.94)};
    for(int j=0;j<3;j++){
        float2 halfSize=j==2?float2(.47,.54):float2(.695,.44);
        float2 q=abs(p.xz-capCenters[j])-halfSize;
        east=max(east,1-smoothstep(.018,.24,max(0,max(q.x,q.y))));
    }
    contact=max(contact,SealTopRect(p.xz,float2(50.043,11.4),float2(.35,.35)));
    contact=max(contact,SealTopRect(p.xz,float2(50.043,25.9),float2(.35,.35)));
    contact=max(contact,SealTopRect(p.xz,float2(33.543,11.4),float2(.35,.35)));
    float steps=0;
    if(p.x<33.895&&p.x>33.191){
        steps=max(steps,SealContact(abs(p.z-18.9))*(1-smoothstep(.01,.06,abs(p.y-4.2))));
        steps=max(steps,SealContact(abs(p.z-20.3))*(1-smoothstep(.01,.06,abs(p.y-4.7))));
        steps=max(steps,SealContact(abs(p.z-21.7))*(1-smoothstep(.01,.06,abs(p.y-5.2))));
    }
    if(p.z>25.548&&p.z<26.252){
        steps=max(steps,SealContact(abs(p.x-38.68))*(1-smoothstep(.01,.06,abs(p.y-4.2))));
        steps=max(steps,SealContact(abs(p.x-37.28))*(1-smoothstep(.01,.06,abs(p.y-4.7))));
        steps=max(steps,SealContact(abs(p.x-35.88))*(1-smoothstep(.01,.06,abs(p.y-5.2))));
    }
    float heightBand=1-smoothstep(.01,.06,abs(p.y-4.2));
    return max(max(steps,contact*heightBand)*.45,east*heightBand*.82);
}
float SealRoomCorner(float3 p,float3 n)
{
    float dx=min(abs(p.x-49.693),abs(p.x-33.893));
    float dz=min(abs(p.z-11.75),abs(p.z-25.55));
    // Same smooth falloff as pillar-to-wall contacts, without a hard plane cutoff.
    float onX=1-smoothstep(.01,.10,dx);
    float onZ=1-smoothstep(.01,.10,dz);
    float xContact=(1-smoothstep(0,.14,dz))*onX*abs(n.x);
    float zContact=(1-smoothstep(0,.14,dx))*onZ*abs(n.z);
    float wallHeight=(p.x<34&&p.z>25)?2.0:4.2;
    return max(xContact,zContact)*(1-smoothstep(wallHeight-.02,wallHeight,p.y))*.71;
}
// Contacts and exposed risers follow the current stair enclosure, independently of pillars.
float SealStairCorner(float3 p,float3 n)
{
    float result=0;
    float2 corners[3]={float2(31.862,25.55),float2(31.862,23.2),float2(33.193,22.5)};
    for(int k=0;k<3;k++){
        float2 d=abs(p.xz-corners[k]);
        float a=SealContact(d.y)*(1-smoothstep(.008,.04,d.x))*abs(n.x);
        float b=SealContact(d.x)*(1-smoothstep(.008,.04,d.y))*abs(n.z);
        result=max(result,max(a,b));
    }
    return result*.71*(1-smoothstep(5.68,5.7,p.y));
}
float SealStairRiserContact(float3 p,float3 n)
{
    float c=0;
    for(int k=0;k<3;k++){
        float low=4.2+.5*k;
        float wz=18.9+1.4*k;
        float nx=38.68-1.4*k;
        float band=step(low-.003,p.y)*SealContact(p.y-low);
        if(p.x>33.191&&p.x<33.895)c=max(c,band*(1-smoothstep(.005,.025,abs(p.z-wz)))*abs(n.z));
        if(p.z>25.548&&p.z<26.252)c=max(c,band*(1-smoothstep(.005,.025,abs(p.x-nx)))*abs(n.x));
    }
    return c*.45;
}
float SealStairRim(float3 p,float3 n)
{
    float d=100;
    for(int k=0;k<3;k++){
        float low=4.2+.5*k, high=low+.5;
        if(p.y<low||p.y>high+.002)continue;
        float wz=18.9+1.4*k,nx=38.68-1.4*k;
        if(p.x>33.191&&p.x<33.895){
            if(abs(n.x)>.8)d=min(d,abs(p.z-wz));
            if(abs(n.z)>.8&&abs(p.z-wz)<.002)d=min(d,min(abs(p.x-33.193),abs(p.x-33.893)));
        }
        if(p.z>25.548&&p.z<26.252){
            if(abs(n.z)>.8)d=min(d,abs(p.x-nx));
            if(abs(n.x)>.8&&abs(p.x-nx)<.002)d=min(d,min(abs(p.z-25.55),abs(p.z-26.25)));
        }
    }
    return SealEdge(d);
}
float SealStairTopSeam(float3 p)
{
    if(abs(p.y-5.7)>.003)return 0;
    float seam=0;
    if(p.x>31.16&&p.x<31.865)seam=max(seam,1-smoothstep(.002,.025,abs(p.z-25.55)));
    if(p.z>22.5&&p.z<23.2){
        seam=max(seam,1-smoothstep(.002,.025,abs(p.x-31.862)));
        seam=max(seam,1-smoothstep(.002,.025,abs(p.x-33.193)));
    }
    return seam;
}
float SealArchRim(float3 p,float3 n)
{
    float2 q=float2(p.z-24.31,p.y-4.1);
    float across=abs(q.x), inner=.82, outer=1.16;
    if(q.y>0){
        float angle=atan2(q.y,q.x);
        float sector=3.14159265/11;
        float middle=(floor(angle/sector)+.5)*sector;
        across=dot(q,float2(cos(middle),sin(middle)));
        float factor=cos(sector*.5-.008);
        inner*=factor;outer*=factor;
    }
    float outline=min(abs(across-inner),abs(across-outer));
    float depthEdge=min(abs(p.x-32.052),abs(p.x-31.142));
    return SealEdge(abs(n.x)>.8?outline:depthEdge);
}
// Visual contact only along the straight jambs; the curved crown stays unchanged.
float SealArchJambContact(float3 p,float3 n,float frame)
{
    float height=step(2.055,p.y)*(1-smoothstep(4.04,4.1,p.y));
    if(height==0)return 0;
    float shade=0;
    if(frame>.5){
        float outer=abs(abs(p.z-24.31)-1.16);
        shade=SealContact(outer)*(1-smoothstep(.20,.32,abs(p.x-32.052)));
    }else if(p.x>31.86&&p.x<32.3){
        float onWall=1-smoothstep(.006,.025,min(abs(p.z-23.2),abs(p.z-25.55)));
        shade=onWall*abs(n.z)*SealContact(abs(p.x-32.052));
    }
    return shade*height*.71;
}
#endif

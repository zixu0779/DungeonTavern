using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class StairMasonrySample
{
 const string ScenePath="Assets/Scenes/SealRoom/SealRoom_B1.unity";
 static Scene Open(){var scene=SceneManager.GetSceneByPath(ScenePath);return scene.isLoaded?scene:EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);}
 [MenuItem("Tools/Environment/Audit Stair Masonry")]
 static void Audit(){
  var scene=Open();string result="";
  foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))){
   if(!r.name.Contains("Stair")&&!r.name.Contains("Arch")&&!r.name.Contains("Return")&&!r.name.Contains("NorthSide")&&!r.name.Contains("RearEnd"))continue;
   var f=r.GetComponent<MeshFilter>();
   result+=$"{r.name} parent={r.transform.parent.name} pos={r.transform.position:F3} rot={r.transform.eulerAngles:F2} scale={r.transform.lossyScale:F3} bounds={r.bounds.min:F3}..{r.bounds.max:F3} mesh={AssetDatabase.GetAssetPath(f.sharedMesh)} localbounds={f.sharedMesh.bounds} materials={string.Join(",",r.sharedMaterials.Select(AssetDatabase.GetAssetPath))}\n";
   if(r.name=="RubbleWall_Arch_6m")File.WriteAllText("/tmp/stair-tunnel-vertices.json",JsonUtility.ToJson(new Vertices{points=f.sharedMesh.vertices.Select(r.transform.TransformPoint).ToArray()}));
  }
  File.WriteAllText("/tmp/stair-masonry-audit.txt",result);
 }
 [System.Serializable] class Vertices{public Vector3[] points;}
 static string Folder="Assets/DungeonTavern/Art/Environment/Architecture/Walls/StoneWall/StairPassageSample";
 static Material[] rubble,trim;
 static MeshRenderer Find(Scene s,string name)=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Single(r=>r.name==name);
 static Material[] Materials(Material source,string name,float boost){
  return new[]{1.08f,1.4f,.88f}.Select((f,i)=>{var m=new Material(source){name=name+i};m.SetColor("_BaseColor",source.GetColor("_BaseColor")*new Color(f*boost,f*boost,f*boost,1));AssetDatabase.CreateAsset(m,$"{Folder}/{name}{i}.mat");return m;}).ToArray();
 }
 static int Shade(Vector3 n)=>n.y>.45f?1:Vector3.Dot(n,new Vector3(.8f,0,-.6f))<-.3f?2:0;
 static Mesh Mesh(string name,Vector3[] v,int[] tris,Vector2[] uv,Transform local){
  var mesh=new Mesh{name=name};var bins=new[]{new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>()};
  for(int i=0;i<tris.Length;i+=3){var n=Vector3.Cross(v[tris[i+1]]-v[tris[i]],v[tris[i+2]]-v[tris[i]]).normalized;bins[Shade(n)].AddRange(new[]{tris[i],tris[i+1],tris[i+2]});}
  mesh.vertices=local?v.Select(local.InverseTransformPoint).ToArray():v;mesh.uv=uv;mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(bins[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,$"{Folder}/{name}.asset");return mesh;
 }
 static Mesh Solid(string name,Vector3[] corners,Transform local){
  int[][] faces={new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,1,5,4},new[]{3,7,6,2},new[]{0,4,7,3},new[]{1,2,6,5}};
  var vs=new System.Collections.Generic.List<Vector3>();var ts=new System.Collections.Generic.List<int>();var uv=new System.Collections.Generic.List<Vector2>();
  var center=corners.Aggregate(Vector3.zero,(sum,p)=>sum+p)/8f;
  foreach(var face in faces){if(Vector3.Dot(Vector3.Cross(corners[face[1]]-corners[face[0]],corners[face[3]]-corners[face[0]]),corners[face[0]]-center)<0)System.Array.Reverse(face);int start=vs.Count;var a=corners[face[0]];var b=corners[face[1]];var d=corners[face[3]];float w=Vector3.Distance(a,b),h=Vector3.Distance(a,d);vs.AddRange(face.Select(i=>corners[i]));uv.AddRange(new[]{new Vector2(0,0),new Vector2(w,0),new Vector2(w,h),new Vector2(0,h)});ts.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});}
  return Mesh(name,vs.ToArray(),ts.ToArray(),uv.ToArray(),local);
 }
 static Vector3[] Box(Vector3 a,Vector3 b)=>new[]{new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z)};
 static void Add(Transform parent,string name,Vector3[] corners,Material[] mats,bool collide=false){
  var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Stair masonry sample");go.transform.SetParent(parent,true);var f=go.AddComponent<MeshFilter>();f.sharedMesh=Solid(name,corners,null);go.AddComponent<MeshRenderer>().sharedMaterials=mats;if(collide)go.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
 }
 static void ShadeExisting(MeshRenderer r){
  var f=r.GetComponent<MeshFilter>();var old=f.sharedMesh;var v=old.vertices.Select(r.transform.TransformPoint).ToArray();Undo.RecordObject(f,"Stair masonry sample");Undo.RecordObject(r,"Stair masonry sample");f.sharedMesh=Mesh(r.name+"_Faces",v,old.triangles,old.uv,r.transform);r.sharedMaterials=rubble;
 }
 [MenuItem("Tools/Environment/Build Stair Masonry Sample")]
 static void Build(){
  if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode before authoring");
  var scene=Open();if(AssetDatabase.IsValidFolder(Folder))throw new System.Exception("Sample already exists; restore before rebuilding");
  EditorSceneManager.SaveScene(scene);File.Copy(ScenePath,"/tmp/SealRoom_B1-before-masonry.unity",true);
  AssetDatabase.CreateFolder("Assets/DungeonTavern/Art/Environment/Architecture/Walls/StoneWall","StairPassageSample");
  var arch=Find(scene,"RubbleWall_Arch_6m");var rear=Find(scene,"RearEnd");
  rubble=Materials(arch.sharedMaterial,"Rubble",1);
  trim=Materials(AssetDatabase.LoadAssetAtPath<Material>("Assets/DungeonTavern/Art/Environment/Architecture/Doors/StoneGate/MAT_StoneGate_Door.mat"),"DressStone",1.08f);
  foreach(string name in new[]{"RubbleWall_Arch_6m","NorthSide","RubbleWall_Return_6m"})ShadeExisting(Find(scene,name));
  // Trim the exposed rear end; actual geometry is shortened, not hidden by overlap.
  var min=rear.bounds.min;var max=rear.bounds.max;min.z=22.77f;max.z=25.65f;
  Undo.RecordObject(rear.GetComponent<MeshFilter>(),"Trim rear wall");Undo.RecordObject(rear,"Shade rear wall");
  rear.GetComponent<MeshFilter>().sharedMesh=Solid("RearWallTrimmed",Box(min,max),rear.transform);rear.sharedMaterials=rubble;
  foreach(var c in rear.GetComponents<BoxCollider>()){Undo.RecordObject(c,"Trim rear collision");c.center=rear.transform.InverseTransformPoint((min+max)*.5f);c.size=Vector3.Scale(max-min,new Vector3(1/rear.transform.lossyScale.x,1/rear.transform.lossyScale.y,1/rear.transform.lossyScale.z));}
  foreach(var c in rear.GetComponents<MeshCollider>()){Undo.RecordObject(c,"Trim rear collision");c.sharedMesh=rear.GetComponent<MeshFilter>().sharedMesh;}
  var shell=new GameObject("StairPassage_MasonrySample");SceneManager.MoveGameObjectToScene(shell,scene);shell.transform.SetParent(rear.transform.parent,true);Undo.RegisterCreatedObjectUndo(shell,"Stair masonry sample");
  Add(shell.transform,"Passage_SouthReturn",Box(new Vector3(31.252f,0,22.77f),new Vector3(33.293f,6.1f,23.07f)),rubble,true);
  // Dress the existing opening: same clear width, sill and arch profile.
  var frame=new GameObject("StairOpening_DressedStone");SceneManager.MoveGameObjectToScene(frame,scene);frame.transform.SetParent(arch.transform.parent,true);Undo.RegisterCreatedObjectUndo(frame,"Stair masonry sample");
  for(int side=0;side<2;side++)for(int row=0;row<4;row++){
   float z=side==0?22.76f:25.13f;float y=2.04f+row*.505f;
   Add(frame.transform,$"Jamb_{side}_{row}",Box(new Vector3(33.793f,y+.009f,z),new Vector3(33.94f,y+.496f,z+.31f)),trim);
  }
  for(int i=0;i<11;i++){
   float a=(i*Mathf.PI/11)+.006f,b=((i+1)*Mathf.PI/11)-.006f;
   Vector3 P(float angle,float extra,float x)=>new Vector3(x,4.06f+(1.08f+extra)*Mathf.Sin(angle),24.1f+(1.03f+extra)*Mathf.Cos(angle));
   Add(frame.transform,$"ArchStone_{i:00}",new[]{P(a,0,33.793f),P(a,0,33.94f),P(a,.31f,33.94f),P(a,.31f,33.793f),P(b,0,33.793f),P(b,0,33.94f),P(b,.31f,33.94f),P(b,.31f,33.793f)},trim);
  }
  // Cap stones have real thickness; no coplanar decorative overlays.
  for(int i=0;i<9;i++){float z=18.65f+i*(7.5f/9);Add(frame.transform,$"WestCoping_{i:00}",Box(new Vector3(33.25f,6.1f,z+.012f),new Vector3(33.84f,6.25f,z+7.5f/9-.012f)),trim);}
  for(int i=0;i<4;i++){float z=22.77f+i*(2.88f/4);Add(shell.transform,$"RearCoping_{i}",Box(new Vector3(30.71f,6.1f,z+.012f),new Vector3(31.29f,6.25f,z+.708f)),trim);}
  for(int i=0;i<3;i++){float x=31.3f+i*(1.95f/3);Add(shell.transform,$"ReturnCoping_{i}",Box(new Vector3(x+.012f,6.1f,22.73f),new Vector3(x+.638f,6.25f,23.11f)),trim);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText("/tmp/stair-masonry-build.txt","Saved sample; rear shortened to z=22.77..25.65, return joins x=31.252..33.293; opening unchanged. Backup: /tmp/SealRoom_B1-before-masonry.unity");
 }
 [MenuItem("Tools/Environment/Finish Stair Masonry Joints")]
 static void FinishJoints(){
  if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode first");
  var scene=Open();var rear=Find(scene,"RearEnd");var side=Find(scene,"Passage_SouthReturn");
  var group=new GameObject("StairPassageReturnWall");SceneManager.MoveGameObjectToScene(group,scene);group.transform.SetParent(rear.transform.parent.parent,true);group.AddComponent<DungeonTavern.Prototypes.Rotation25D.WallCutoutGroup>();Undo.RegisterCreatedObjectUndo(group,"Separate perpendicular wall group");
  foreach(var t in side.transform.parent.GetComponentsInChildren<Transform>().Where(t=>t.name=="Passage_SouthReturn"||t.name.StartsWith("ReturnCoping_")).ToArray())Undo.SetTransformParent(t,group.transform,"Separate perpendicular wall group");
  foreach(var r in new[]{rear,side}){var mesh=r.GetComponent<MeshFilter>().sharedMesh;mesh.uv=mesh.uv.Select(uv=>uv*.3f).ToArray();EditorUtility.SetDirty(mesh);}
  trim=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/DressStone{i}.mat")).ToArray();
  var north=Find(scene,"NorthSide");
  for(int i=0;i<2;i++){float x=31.3f+i*.975f;Add(north.transform.parent,$"NorthPassageCoping_{i}",Box(new Vector3(x+.012f,6.1f,25.61f),new Vector3(x+.963f,6.25f,26.19f)),trim);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
 [MenuItem("Tools/Environment/Audit B1 Masonry Rollout")]
 static void AuditAll(){
  var scene=Open();string report="";
  foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))){
   if(!r.GetComponentInParent<DungeonTavern.Prototypes.Rotation25D.WallCutoutGroup>()&&!r.name.StartsWith("FogBlob"))continue;
   report+=$"{r.name} group={r.GetComponentInParent<DungeonTavern.Prototypes.Rotation25D.WallCutoutGroup>()?.name} active={r.gameObject.activeInHierarchy} bounds={r.bounds.min:F3}..{r.bounds.max:F3} materials={string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null"))}\n";
  }
  var fog=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="BlackFog_ToMain");
  foreach(var t in fog.GetComponentsInChildren<Transform>())report+=$"FOG {t.name} position={t.position:F3} rot={t.eulerAngles:F2} scale={t.lossyScale:F3}\n";
  File.WriteAllText("/tmp/b1-rollout-audit.txt",report);
 }
 struct Roof {public Rect rect;public float y;public Transform group;}
 static System.Collections.Generic.List<Roof> roofChecks=new();
 [MenuItem("Tools/Environment/Apply B1 Stone Masonry")]
 static void Rollout(){
  if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode first");
  var scene=Open();EditorSceneManager.SaveScene(scene);File.Copy(ScenePath,"/tmp/SealRoom_B1-before-full-masonry.unity",true);
  const string dest="Assets/DungeonTavern/Art/Environment/Architecture/Walls/StoneWall/B1StoneMasonry";
  if(AssetDatabase.IsValidFolder(dest))throw new System.Exception("Rollout already applied");
  AssetDatabase.CreateFolder("Assets/DungeonTavern/Art/Environment/Architecture/Walls/StoneWall","B1StoneMasonry");
  rubble=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Rubble{i}.mat")).ToArray();
  trim=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/DressStone{i}.mat")).ToArray();
  Folder=dest;
  var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
  var walls=all.Where(r=>r.gameObject.activeInHierarchy&&r.enabled&&r.GetComponentInParent<DungeonTavern.Prototypes.Rotation25D.WallCutoutGroup>()&&r.bounds.size.sqrMagnitude>.01f&&r.sharedMaterials.Any(m=>m&&(m.name=="MAT_RubbleStone"||m.name=="Rubble0"))).ToArray();
  AssetDatabase.StartAssetEditing();
  try{
   int changed=0;roofChecks.Clear();
   foreach(var r in walls){
    var b=r.bounds;roofChecks.Add(new Roof{rect=Rect.MinMaxRect(b.min.x,b.min.z,b.max.x,b.max.z),y=b.max.y,group=r.GetComponentInParent<DungeonTavern.Prototypes.Rotation25D.WallCutoutGroup>().transform});
    if(r.sharedMaterials[0].name=="Rubble0")continue;
    var f=r.GetComponent<MeshFilter>();var m=f.sharedMesh;Undo.RecordObject(f,"Stone face shading");Undo.RecordObject(r,"Stone face shading");f.sharedMesh=Mesh($"Wall_{changed++}_{r.name}",m.vertices.Select(r.transform.TransformPoint).ToArray(),m.triangles,m.uv,r.transform);r.sharedMaterials=rubble;
   }
   foreach(var r in all.Where(r=>r.name.Contains("Coping")))Undo.DestroyObjectImmediate(r.gameObject);
   float Q(float x)=>Mathf.Round(x*1000)/1000;
   var xs=roofChecks.SelectMany(r=>new[]{Q(r.rect.xMin),Q(r.rect.xMax)}).Distinct().OrderBy(x=>x).ToArray();
   var zs=roofChecks.SelectMany(r=>new[]{Q(r.rect.yMin),Q(r.rect.yMax)}).Distinct().OrderBy(x=>x).ToArray();
   var keys=new int[xs.Length-1,zs.Length-1];var used=new bool[xs.Length-1,zs.Length-1];
   for(int x=0;x<xs.Length-1;x++)for(int z=0;z<zs.Length-1;z++){
    int best=-1;var p=new Vector2((xs[x]+xs[x+1])*.5f,(zs[z]+zs[z+1])*.5f);
    for(int i=0;i<roofChecks.Count;i++)if(roofChecks[i].rect.Contains(p)&&(best<0||roofChecks[i].y>roofChecks[best].y))best=i;
    keys[x,z]=best;
   }
   bool Same(int a,int b)=>a>=0&&b>=0&&Mathf.Abs(roofChecks[a].y-roofChecks[b].y)<.002f&&roofChecks[a].group==roofChecks[b].group;
   int count=0;
   for(int z=0;z<zs.Length-1;z++)for(int x=0;x<xs.Length-1;x++){
    int key=keys[x,z];if(key<0||used[x,z])continue;
    int endX=x+1,endZ=z+1;
    while(endX<xs.Length-1&&!used[endX,z]&&Same(key,keys[endX,z]))endX++;
    while(endZ<zs.Length-1&&Enumerable.Range(x,endX-x).All(xx=>!used[xx,endZ]&&Same(key,keys[xx,endZ])))endZ++;
    for(int xx=x;xx<endX;xx++)for(int zz=z;zz<endZ;zz++)used[xx,zz]=true;
    var roof=roofChecks[key];bool alongX=xs[endX]-xs[x]>zs[endZ]-zs[z];float length=alongX?xs[endX]-xs[x]:zs[endZ]-zs[z];int blocks=Mathf.CeilToInt(length/.95f);
    for(int i=0;i<blocks;i++){
     var a=new Vector3(xs[x],roof.y,zs[z]);var b=new Vector3(xs[endX],roof.y+.30f,zs[endZ]);
     float start=length*i/blocks+(i>0?.006f:0),end=length*(i+1)/blocks-(i<blocks-1?.006f:0);
     if(alongX){a.x+=start;b.x=xs[x]+end;}else{a.z+=start;b.z=zs[z]+end;}
     Add(roof.group,$"B1Coping_{count++:000}",Box(a,b),trim);
    }
   }
   ConfigureFog(scene);
   File.WriteAllText("/tmp/b1-masonry-rollout.txt",$"Shaded {changed} meshes, covered {roofChecks.Count} wall tops using {count} non-overlapping coping stones, 0.30 m thick.\n");
  }finally{AssetDatabase.StopAssetEditing();}
  TrimStair(scene);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
 static void ConfigureFog(Scene scene){
  var fog=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="BlackFog_ToMain");
  Undo.RecordObject(fog,"Contain stair fog");fog.SetPositionAndRotation(Vector3.zero,Quaternion.identity);fog.localScale=Vector3.one;
  var materials=new System.Collections.Generic.Dictionary<Material,Material>();int i=0;
  foreach(var r in fog.GetComponentsInChildren<MeshRenderer>()){
   Undo.RecordObject(r.transform,"Place exterior fog");Undo.RecordObject(r,"Exterior fog boundary");var size=r.localBounds.size;
   r.transform.SetPositionAndRotation(new Vector3(33.1f-(i/3)*.34f,3.6f,23.42f+(i%3)*.8f),Quaternion.identity);
   r.transform.localScale=new Vector3(1.6f/size.x,3.7f/size.y,2.2f/size.z);
   var source=r.sharedMaterial;
   if(!materials.TryGetValue(source,out var m)){
    m=new Material(source){name="B1_"+source.name};m.SetFloat("_FogBoundsEnabled",1);m.SetVector("_FogBoundsMin",new Vector4(31.26f,2.04f,23.08f,0));m.SetVector("_FogBoundsMax",new Vector4(33.25f,5.3f,25.64f,0));m.SetFloat("_Density",1.3f);
    AssetDatabase.CreateAsset(m,$"{Folder}/{m.name}.mat");materials.Add(source,m);
   }
   r.sharedMaterial=m;i++;
  }
 }
 struct Vertex {public Vector3 p,n;public Vector2 uv;public static Vertex Lerp(Vertex a,Vertex b,float t)=>new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
 static System.Collections.Generic.List<Vertex> Clip(System.Collections.Generic.List<Vertex> poly,int axis,float edge,bool greater){
  var output=new System.Collections.Generic.List<Vertex>();if(poly.Count==0)return output;var prev=poly[poly.Count-1];float d0=(prev.p[axis]-edge)*(greater?1:-1);
  foreach(var v in poly){float d1=(v.p[axis]-edge)*(greater?1:-1);if((d0>=0)!=(d1>=0))output.Add(Vertex.Lerp(prev,v,d0/(d0-d1)));if(d1>=0)output.Add(v);prev=v;d0=d1;}return output;
 }
 static void TrimStair(Scene scene){
  var stair=Find(scene,"Stair_Stone_B1_Ascending");var filter=stair.GetComponent<MeshFilter>();string path=AssetDatabase.GetAssetPath(filter.sharedMesh);var importer=(ModelImporter)AssetImporter.GetAtPath(path);bool readable=importer.isReadable;
  try{
   importer.isReadable=true;importer.SaveAndReimport();var old=filter.sharedMesh;var vs=old.vertices;var ns=old.normals;var uvs=old.uv;var ids=old.triangles;
   var verts=new System.Collections.Generic.List<Vector3>();var norms=new System.Collections.Generic.List<Vector3>();var tex=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();int removed=0;
   void Emit(System.Collections.Generic.List<Vertex> p){for(int j=1;j<p.Count-1;j++)foreach(var v in new[]{p[0],p[j],p[j+1]}){triangles.Add(verts.Count);verts.Add(stair.transform.InverseTransformPoint(v.p));norms.Add(v.n);tex.Add(v.uv);}}
   for(int t=0;t<ids.Length;t+=3){var p=new System.Collections.Generic.List<Vertex>();for(int j=0;j<3;j++){int k=ids[t+j];p.Add(new Vertex{p=stair.transform.TransformPoint(vs[k]),n=ns[k],uv=uvs[k]});}
    if(p.Any(v=>v.p.x<33.31f&&v.p.z<22.79f))removed++;
    Emit(Clip(p,0,33.31f,true));Emit(Clip(Clip(p,0,33.31f,false),2,22.79f,true));
   }
   var mesh=new Mesh{name="Stair_B1_InsideWallFootprint",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(verts);mesh.SetNormals(norms);mesh.SetUVs(0,tex);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,$"{Folder}/Stair_InsideWallFootprint.asset");Undo.RecordObject(filter,"Trim exterior stair overhang");filter.sharedMesh=mesh;
   File.AppendAllText("/tmp/b1-masonry-rollout.txt",$"Clipped {removed} stair triangles crossing exterior footprint; original model and prefab retained.\n");
  }finally{importer.isReadable=readable;importer.SaveAndReimport();}
 }
}

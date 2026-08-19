using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DungeonTavern.Tavern25D;

[InitializeOnLoad]
public static class BarPrefabAuthoring
{
    static GameObject testRoot, testAgent;
    static DoorStateController[] testDoors;
    static int testStage;
    static double nextTestTime;
    static BarPrefabAuthoring() { EditorApplication.playModeStateChanged += OnPlayState; }
    [MenuItem("Tools/Dungeon Tavern/Bar/Test Automatic Gates")]
    public static void TestAutomaticGates()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Start test outside Play Mode.");
        SessionState.SetBool("BarGateTest",true);
        EditorApplication.EnterPlaymode();
    }
    static void OnPlayState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool("BarGateTest",false)) return;
        if (state==PlayModeStateChange.EnteredPlayMode)
        {
            testRoot=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CombinedPath));
            testRoot.transform.position=new Vector3(1000,0,1000);
            testDoors=testRoot.GetComponentsInChildren<DoorStateController>();
            testAgent=new GameObject("BarGateTestAgent");
            testAgent.transform.position=new Vector3(1000,3,1010);
            testAgent.AddComponent<DoorPassageAgent>();
            testAgent.AddComponent<SphereCollider>().radius=.15f;
            var body=testAgent.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
            testStage=0;nextTestTime=Time.time+1;
            EditorApplication.update+=TickTest;
        }
        if (state==PlayModeStateChange.EnteredEditMode) { SessionState.SetBool("BarGateTest",false);EditorApplication.update-=TickTest; }
    }
    static void TickTest()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || Time.time<nextTestTime) return;
        try
        {
            int index=testStage/4, step=testStage%4;
            if (index==2) { FinishTest("PASS: both automatic gates opened via physics trigger, rotated 95 degrees independently, stayed open while occupied, and closed with passage collision restored after exit.");return; }
            var door=testDoors[index];var other=testDoors[1-index];var pivot=door.transform.Find("Pivot");
            if(step==0)
            {
                if(door.IsOpen || !door.BlockingCollider.enabled) throw new InvalidOperationException("Initial collision state");
                testAgent.transform.position=door.transform.TransformPoint(new Vector3(-.44f,.5f,0));Physics.SyncTransforms();
            }
            if(step==1 || step==2)
            {
                if(!door.IsOpen || door.BlockingCollider.enabled || Mathf.Abs(Quaternion.Angle(pivot.localRotation,Quaternion.identity)-95)>.1f || other.IsOpen) throw new InvalidOperationException($"Automatic opening failed: gate={door.name}, open={door.IsOpen}, blocker={door.BlockingCollider.enabled}, angle={Quaternion.Angle(pivot.localRotation,Quaternion.identity)}, otherOpen={other.IsOpen}, time={Time.time}");
                if(step==2) {testAgent.transform.position=door.transform.TransformPoint(new Vector3(-.44f,.5f,4));Physics.SyncTransforms();}
            }
            if(step==3 && (door.IsOpen || !door.BlockingCollider.enabled || Quaternion.Angle(pivot.localRotation,Quaternion.identity)>.1f)) throw new InvalidOperationException("Automatic closing failed");
            testStage++;nextTestTime=Time.time+1.2;
        }
        catch(Exception e) {FinishTest("FAIL: "+e);}
    }
    static void FinishTest(string message)
    {
        EditorApplication.update-=TickTest;
        File.WriteAllText("/tmp/dt-bar-runtime-test.txt",message);
        UnityEngine.Object.Destroy(testAgent);UnityEngine.Object.Destroy(testRoot);
        EditorApplication.ExitPlaymode();
    }
    const string Models = "Assets/DungeonTavern/Art/Models/";
    const string CombinedPath = Models + "Bar_Assembly/Bar_Assembly.prefab";

    [MenuItem("Tools/Dungeon Tavern/Bar/Build Combined Prefab")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Build outside Play Mode.");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Bar_Assembly");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var counter = Model("Bar_Wood_LShape", root.transform);
            FitHeight(counter, 1.1f);
            var b = BoundsOf(counter);
            counter.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            b = BoundsOf(counter);
            // Generated model has its own proportions; keep uniform scale throughout.
            var mesh = counter.GetComponentInChildren<MeshFilter>();
            var points = mesh.sharedMesh.vertices.Select(v => mesh.transform.TransformPoint(v)).ToArray();
            bool shortOnLeft = points.Count(v => v.x < b.min.x + b.size.x * .2f) > points.Count(v => v.x > b.max.x - b.size.x * .2f);
            bool longOnFront = points.Count(v => v.z < b.min.z + b.size.z * .2f) > points.Count(v => v.z > b.max.z - b.size.z * .2f);
            float sx = shortOnLeft ? -1 : 1, sz = longOnFront ? -1 : 1;
            // Measure the counter cross-section at the free ends, away from the corner.
            var longEnd = points.Where(v => (sx < 0 ? v.x > b.max.x - b.size.x*.08f : v.x < b.min.x + b.size.x*.08f)).ToArray();
            var shortEnd = points.Where(v => (sz < 0 ? v.z > b.max.z - b.size.z*.08f : v.z < b.min.z + b.size.z*.08f)).ToArray();
            float longZ = (longEnd.Min(v=>v.z) + longEnd.Max(v=>v.z))*.5f;
            float shortX = (shortEnd.Min(v=>v.x) + shortEnd.Max(v=>v.x))*.5f;
            float longDepth = longEnd.Max(v=>v.z) - longEnd.Min(v=>v.z);
            float shortWidth = shortEnd.Max(v=>v.x) - shortEnd.Min(v=>v.x);
            Box(root.transform,"Counter_Long_Collision",new Vector3(b.center.x,.55f,longZ),new Vector3(b.size.x,1.1f,longDepth)).gameObject.AddComponent<DungeonTavern.Prototypes.Rotation25D.CounterVaultObstacle>();
            Box(root.transform,"Counter_Short_Collision",new Vector3(shortX,.55f,b.center.z),new Vector3(shortWidth,1.1f,b.size.z)).gameObject.AddComponent<DungeonTavern.Prototypes.Rotation25D.CounterVaultObstacle>();
            float eastYaw = sx < 0 ? 180 : 0;
            float northYaw = sz < 0 ? 90 : -90;
            MakeGate(root.transform,"Gate_East",new Vector3(sx < 0 ? b.max.x : b.min.x,0,longZ),eastYaw,sx*sz < 0 ? -95 : 95);
            MakeGate(root.transform,"Gate_North",new Vector3(shortX,0,sz < 0 ? b.max.z : b.min.z),northYaw,sx*sz < 0 ? 95 : -95);
            Directory.CreateDirectory(Path.GetDirectoryName(CombinedPath));
            PrefabUtility.SaveAsPrefabAsset(root,CombinedPath);
            Validate(root);
            Export(root,"/tmp/dt-bar-closed.json");
            foreach (var door in root.GetComponentsInChildren<DoorStateController>()) door.Open();
            Export(root,"/tmp/dt-bar-open.json");
            foreach (var door in root.GetComponentsInChildren<DoorStateController>()) door.Close();
            File.WriteAllText("/tmp/dt-bar-build.txt",$"PASS: {CombinedPath}\nTwo independent hinged gates and self-contained automatic triggers.\nCounter bounds: {b}\nshortOnLeft={shortOnLeft}, longOnFront={longOnFront}\nlongDepth={longDepth}, shortWidth={shortWidth}\n");
            AssetDatabase.SaveAssets();
            Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(CombinedPath);
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
    }
    static Transform Model(string name, Transform parent)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Models+name+"/"+name+".prefab");
        if (!asset) throw new InvalidOperationException("Missing model: "+name);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
        go.name=name;
        return go.transform;
    }
    static Bounds BoundsOf(Transform t) => t.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
    static void FitHeight(Transform t,float height) { t.localScale *= height/BoundsOf(t).size.y; }
    static BoxCollider Box(Transform parent,string name,Vector3 center,Vector3 size)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        var box=go.AddComponent<BoxCollider>();box.center=center;box.size=size;return box;
    }
    static void MakeGate(Transform parent,string name,Vector3 counterEnd,float yaw,float angle)
    {
        // Local -X points away from the bar towards the wall-mounted hinge post.
        const float gap=.88f;
        var gate=new GameObject(name);gate.transform.SetParent(parent,false);
        gate.transform.localPosition=counterEnd;gate.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var post=Model("Bar_StaffGate_WallHingePost",gate.transform);FitHeight(post,1.1f);
        // Measure in an unrotated frame before placing the gate assembly.
        gate.transform.localRotation=Quaternion.identity;
        var pb=BoundsOf(post);
        post.position += new Vector3(counterEnd.x-gap-pb.size.x*.5f,0,counterEnd.z)-new Vector3(pb.center.x,pb.min.y,pb.center.z);
        var pivot=new GameObject("Pivot").transform;pivot.SetParent(gate.transform,false);pivot.localPosition=new Vector3(-gap,0,0);
        var leaf=Model("Bar_StaffGate_DoorLeaf",pivot);
        leaf.localRotation=Quaternion.Euler(0,180,0)*leaf.localRotation;
        FitHeight(leaf,1.03f);
        var lb=BoundsOf(leaf);
        // Rotate the source so its positive-X hinge hardware meets the fixed post.
        leaf.position += pivot.position + new Vector3(.015f,.04f,0)-new Vector3(lb.min.x,lb.min.y,lb.center.z);
        var blocker=Box(gate.transform,"PassageBlocker",new Vector3(-gap*.5f,.55f,0),new Vector3(gap,1.1f,.22f));
        var door=gate.AddComponent<DoorStateController>();door.ConfigureHinged(pivot,null,blocker,angle,0,false);
        var trigger=Box(gate.transform,"AutomaticTrigger",new Vector3(-gap*.5f,1,0),new Vector3(gap+.4f,2,2.8f));trigger.isTrigger=true;
        var rb=trigger.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        trigger.gameObject.AddComponent<AutomaticDoorTrigger>().Configure(door,.45f);
        var modifier=gate.AddComponent<Unity.AI.Navigation.NavMeshModifier>();modifier.ignoreFromBuild=true;modifier.applyToChildren=true;
        gate.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var fixedPostCollider=post.gameObject.AddComponent<BoxCollider>();
        var mf=post.GetComponent<MeshFilter>();fixedPostCollider.center=mf.sharedMesh.bounds.center;fixedPostCollider.size=mf.sharedMesh.bounds.size;
    }
    static void Validate(GameObject root)
    {
        var doors=root.GetComponentsInChildren<DoorStateController>();
        if (doors.Length!=2 || root.GetComponentsInChildren<AutomaticDoorTrigger>().Length!=2) throw new InvalidOperationException("Expected two gates.");
        foreach(var door in doors)
        {
            var pivot=door.transform.Find("Pivot");var post=door.transform.Find("Bar_StaffGate_WallHingePost");var before=post.localToWorldMatrix;
            door.Close();if(!door.BlockingCollider.enabled || Quaternion.Angle(pivot.localRotation,Quaternion.identity)>.01f) throw new InvalidOperationException("Closed gate invalid");
            door.Open();if(door.BlockingCollider.enabled || Mathf.Abs(Quaternion.Angle(pivot.localRotation,Quaternion.identity)-95)>.01f || before!=post.localToWorldMatrix) throw new InvalidOperationException("Hinged gate invalid");
            var so=new SerializedObject(door.GetComponentInChildren<AutomaticDoorTrigger>());
            if(so.FindProperty("door").objectReferenceValue!=door) throw new InvalidOperationException("Trigger references wrong gate");
            door.Close();
        }
    }
    [Serializable] class MeshExport { public float[] vertices; public float[] uv; public int[] triangles; public string texture; }
    [Serializable] class ExportData { public MeshExport[] meshes; }
    static void Export(GameObject root,string path)
    {
        var data=new ExportData {meshes=root.GetComponentsInChildren<MeshFilter>().Select(m=>new MeshExport {
            vertices=m.sharedMesh.vertices.SelectMany(v=>{var p=m.transform.TransformPoint(v);return new[]{p.x,p.y,p.z};}).ToArray(),
            uv=m.sharedMesh.uv.SelectMany(v=>new[]{v.x,v.y}).ToArray(),triangles=m.sharedMesh.triangles,
            texture=Path.GetFullPath(AssetDatabase.GetAssetPath(m.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap")))
        }).ToArray()};File.WriteAllText(path,JsonUtility.ToJson(data));
    }

}

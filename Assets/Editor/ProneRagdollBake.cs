using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ProneRagdollBake {
 const string Folder="Assets/DungeonTavern/Gameplay/Animations/";
 [MenuItem("Tools/Characters/Bake Physical Prone")]
 public static void Run(){
 foreach(var name in new[]{"Prone","WakeUp"}){var backup=Path.Combine(Path.GetTempPath(),name+"-before-ragdoll.anim");if(File.Exists(backup)){File.Copy(backup,Folder+name+".anim",true);AssetDatabase.ImportAsset(Folder+name+".anim");}}

 var scene=EditorSceneManager.NewPreviewScene();
 var meshes=new List<Mesh>();
 try{
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/Protagonist/Protagonist.prefab"));SceneManager.MoveGameObjectToScene(go,scene);
 var animator=go.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();var prone=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Prone.anim");var wake=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"WakeUp.anim");
 var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,prone);p.SetApplyFootIK(false);AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(p);graph.Play();graph.Evaluate(0);graph.Destroy();animator.enabled=false;
 var ids=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
 var bones=ids.Select(animator.GetBoneTransform).ToArray();var points=bones.ToDictionary(b=>b,b=>new List<Vector3>());
 foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()){
 var mesh=new Mesh();meshes.Add(mesh);skin.BakeMesh(mesh,false);var verts=mesh.vertices;var weights=skin.sharedMesh.boneWeights;var scale=skin.transform.lossyScale;
 for(int i=0;i<verts.Length;i++){
 var w=weights[i];int index=w.boneIndex0;float weight=w.weight0;if(w.weight1>weight){index=w.boneIndex1;weight=w.weight1;}if(w.weight2>weight){index=w.boneIndex2;weight=w.weight2;}if(w.weight3>weight)index=w.boneIndex3;
 var bone=skin.bones[index];while(bone&&!points.ContainsKey(bone))bone=bone.parent;if(!bone)continue;
 var world=skin.transform.TransformPoint(new Vector3(verts[i].x/scale.x,verts[i].y/scale.y,verts[i].z/scale.z));points[bone].Add(Quaternion.Inverse(bone.rotation)*(world-bone.position));
 }
 }
 var bodies=new Dictionary<Transform,Rigidbody>();var colliders=new List<Collider>();
 foreach(var bone in bones){
 var proxy=new GameObject(bone.name+"_physics");SceneManager.MoveGameObjectToScene(proxy,scene);proxy.transform.SetPositionAndRotation(bone.position+Vector3.up*.3f,bone.rotation);
 var rb=proxy.AddComponent<Rigidbody>();rb.mass=bone==bones[3]?2:bone.name.StartsWith("Hand")?3:1;if(bone==bones[0]||bone==bones[1]||bone==bones[2])rb.constraints=RigidbodyConstraints.FreezeRotation;rb.linearDamping=1;rb.angularDamping=3;rb.solverIterations=20;rb.solverVelocityIterations=10;rb.maxAngularVelocity=5;bodies[bone]=rb;
 var cloud=points[bone];var bounds=new Bounds(Vector3.zero,Vector3.one*.05f);if(cloud.Count>0){bounds=new Bounds(cloud[0],Vector3.zero);foreach(var v in cloud)bounds.Encapsulate(v);}
 Collider collider;
 if(cloud.Count>=4){var hull=new Mesh();meshes.Add(hull);var vertices=cloud.Where((v,i)=>i%Math.Max(1,cloud.Count/180)==0).ToArray();hull.vertices=vertices;var triangles=new List<int>();for(int i=1;i<vertices.Length-1;i++){triangles.Add(0);triangles.Add(i);triangles.Add(i+1);}hull.triangles=triangles.ToArray();var shape=proxy.AddComponent<MeshCollider>();shape.sharedMesh=hull;shape.convex=true;collider=shape;}
 else {var box=proxy.AddComponent<BoxCollider>();box.center=bounds.center;box.size=Vector3.Max(bounds.size,Vector3.one*.045f);collider=box;}
 collider.contactOffset=.002f;colliders.Add(collider);
 }
 foreach(var bone in bones){var parent=bone.parent;while(parent&&!bodies.ContainsKey(parent))parent=parent.parent;if(!parent)continue;
 var joint=bodies[bone].gameObject.AddComponent<CharacterJoint>();joint.connectedBody=bodies[parent];joint.axis=Vector3.right;joint.swingAxis=Vector3.forward;joint.lowTwistLimit=new SoftJointLimit{limit=-45};joint.highTwistLimit=new SoftJointLimit{limit=45};joint.swing1Limit=new SoftJointLimit{limit=60};joint.swing2Limit=new SoftJointLimit{limit=45};if(bone.name.StartsWith("Hand")){joint.lowTwistLimit=new SoftJointLimit{limit=-90};joint.highTwistLimit=new SoftJointLimit{limit=90};joint.swing1Limit=new SoftJointLimit{limit=90};joint.swing2Limit=new SoftJointLimit{limit=90};}joint.enableProjection=true;joint.projectionDistance=.01f;
 }
 // The production silhouette overlaps at shoulders and clothing; avoid self-collision explosions.
 for(int i=0;i<colliders.Count;i++)for(int j=i+1;j<colliders.Count;j++)Physics.IgnoreCollision(colliders[i],colliders[j]);
 var floor=new GameObject("HardGround");SceneManager.MoveGameObjectToScene(floor,scene);var ground=floor.AddComponent<BoxCollider>();ground.size=new Vector3(10,.2f,10);floor.transform.position=new Vector3(0,-.1f,0);
 var physics=scene.GetPhysicsScene();if(physics==Physics.defaultPhysicsScene)throw new Exception("Preview physics not isolated");for(int i=0;i<1200;i++)physics.Simulate(1f/120);
 foreach(var bone in bones)bone.SetPositionAndRotation(bodies[bone].position,bodies[bone].rotation);
 using(var handler=new HumanPoseHandler(animator.avatar,go.transform)){
 var pose=new HumanPose();handler.GetHumanPose(ref pose);
 var positions=bones.Select(b=>b.position).ToArray();handler.SetHumanPose(ref pose);float error=bones.Select((b,i)=>Vector3.Distance(b.position,positions[i])).Max();Debug.Log("Humanoid roundtrip maximum bone error="+error);
 var oldRotation=new Quaternion(AnimationUtility.GetEditorCurve(prone,EditorCurveBinding.FloatCurve("",typeof(Animator),"RootQ.x")).Evaluate(0),AnimationUtility.GetEditorCurve(prone,EditorCurveBinding.FloatCurve("",typeof(Animator),"RootQ.y")).Evaluate(0),AnimationUtility.GetEditorCurve(prone,EditorCurveBinding.FloatCurve("",typeof(Animator),"RootQ.z")).Evaluate(0),AnimationUtility.GetEditorCurve(prone,EditorCurveBinding.FloatCurve("",typeof(Animator),"RootQ.w")).Evaluate(0));if(Quaternion.Dot(oldRotation,pose.bodyRotation)<0)pose.bodyRotation=new Quaternion(-pose.bodyRotation.x,-pose.bodyRotation.y,-pose.bodyRotation.z,-pose.bodyRotation.w);
 var values=new Dictionary<string,float>();for(int i=0;i<HumanTrait.MuscleCount;i++)values[HumanTrait.MuscleName[i]]=pose.muscles[i];
 values["RootT.x"]=pose.bodyPosition.x;values["RootT.y"]=pose.bodyPosition.y;values["RootT.z"]=pose.bodyPosition.z;values["RootQ.x"]=pose.bodyRotation.x;values["RootQ.y"]=pose.bodyRotation.y;values["RootQ.z"]=pose.bodyRotation.z;values["RootQ.w"]=pose.bodyRotation.w;
 foreach(var clip in new[]{prone,wake}){string backup=Path.Combine(Path.GetTempPath(),clip.name+"-before-ragdoll.anim");if(!File.Exists(backup))File.Copy(AssetDatabase.GetAssetPath(clip),backup);Undo.RecordObject(clip,"Bake physical prone");}
 foreach(var pair in values){var binding=EditorCurveBinding.FloatCurve("",typeof(Animator),pair.Key);var old=AnimationUtility.GetEditorCurve(prone,binding);float delta=pair.Value-(old?.Evaluate(0)??0);AnimationUtility.SetEditorCurve(prone,binding,AnimationCurve.Constant(0,prone.length,pair.Value));var c=AnimationUtility.GetEditorCurve(wake,binding);if(c==null)continue;var keys=c.keys;for(int i=0;i<keys.Length;i++){float t=keys[i].time/wake.length;keys[i].value+=delta*(1-t)*(1-t);keys[i].inTangent-=2*delta*(1-t)/wake.length;keys[i].outTangent-=2*delta*(1-t)/wake.length;}c.keys=keys;AnimationUtility.SetEditorCurve(wake,binding,c);}
 }
 foreach(var clip in new[]{prone,wake})EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();
 animator.enabled=true;animator.Rebind();var verify=PlayableGraph.Create();verify.SetTimeUpdateMode(DirectorUpdateMode.Manual);var sample=AnimationClipPlayable.Create(verify,prone);sample.SetApplyFootIK(false);AnimationPlayableOutput.Create(verify,"verify",animator).SetSourcePlayable(sample);verify.Play();verify.Evaluate(0);
 float replayError=bones.Max(b=>Vector3.Distance(b.position,bodies[b].position));verify.Destroy();if(replayError>.015f)throw new Exception("Baked bone position error: "+replayError);Debug.Log("Prone replay PASS; maximum bone position error="+replayError);
 File.WriteAllLines("/tmp/prone-physics-report.txt",bones.Select(b=>b.name+" collider bottom="+bodies[b].GetComponent<Collider>().bounds.min.y+" speed="+bodies[b].linearVelocity.magnitude));Debug.Log("Physical prone baked after 10 seconds of isolated simulation.");
 }finally{foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);EditorSceneManager.ClosePreviewScene(scene);}
 }
}

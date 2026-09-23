using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
public static class GetUpRebuild {
 static HumanPose Read(AnimationClip c,float t){float V(string n)=>AnimationUtility.GetEditorCurve(c,EditorCurveBinding.FloatCurve("",typeof(Animator),n))?.Evaluate(t)??0;return new HumanPose{bodyPosition=new Vector3(V("RootT.x"),V("RootT.y"),V("RootT.z")),bodyRotation=new Quaternion(V("RootQ.x"),V("RootQ.y"),V("RootQ.z"),V("RootQ.w")),muscles=HumanTrait.MuscleName.Select(V).ToArray()};}
 static float Ease(float a,float b,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
 static void Solve(Transform upper,Transform lower,Transform tip,Vector3 target,Vector3 pole){
 Vector3 root=upper.position;float a=Vector3.Distance(root,lower.position),b=Vector3.Distance(lower.position,tip.position);var delta=target-root;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);var direction=delta.normalized;var bend=Vector3.ProjectOnPlane(pole-root,direction).normalized;if(bend.sqrMagnitude<.01f)bend=Vector3.Cross(direction,Vector3.right).normalized;
 float along=(a*a-b*b+d*d)/(2*d);var elbow=root+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));upper.rotation=Quaternion.FromToRotation(lower.position-root,elbow-root)*upper.rotation;lower.rotation=Quaternion.FromToRotation(tip.position-lower.position,target-lower.position)*lower.rotation;
 }
 [MenuItem("Tools/Characters/Rebuild GetUp Support")]
 public static void Run(){
 const string path="Assets/DungeonTavern/Gameplay/Animations/";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path+"GetUp.anim");var wake=AssetDatabase.LoadAssetAtPath<AnimationClip>(path+"WakeUp.anim");string backup=Path.Combine(Path.GetTempPath(),"GetUp-approved-end-before-rebuild.anim");if(!File.Exists(backup))File.Copy(AssetDatabase.GetAssetPath(clip),backup);
 File.Copy(backup,path+"GetUp.anim",true);AssetDatabase.ImportAsset(path+"GetUp.anim");
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/Protagonist/Protagonist.prefab"));var animator=go.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
 var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var output=AnimationPlayableOutput.Create(graph,"sample",animator);graph.Play();
 using(var handler=new HumanPoseHandler(animator.avatar,go.transform))try{
 var start=new HumanPose();var end=new HumanPose();var p=AnimationClipPlayable.Create(graph,wake);output.SetSourcePlayable(p);p.SetTime(wake.length);graph.Evaluate(0);start=Read(wake,wake.length);handler.SetHumanPose(ref start);
 var ids=new[]{HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot};var tips=ids.Select(animator.GetBoneTransform).ToArray();var first=tips.Select(t=>t.position).ToArray();var firstRot=tips.Select(t=>t.rotation).ToArray();
 var q=AnimationClipPlayable.Create(graph,clip);output.SetSourcePlayable(q);q.SetTime(clip.length);graph.Evaluate(0);end=Read(clip,clip.length);handler.SetHumanPose(ref end);var last=tips.Select(t=>t.position).ToArray();var lastRot=tips.Select(t=>t.rotation).ToArray();Debug.Log("Rebuild start="+start.bodyPosition+" end="+end.bodyPosition+" feet="+first[2]+" to="+last[2]);graph.Stop();animator.enabled=false;
 int frames=166;var poses=new HumanPose[frames];float handError=0;
 for(int i=0;i<frames;i++){
 float t=i/(float)(frames-1);float rise=Ease(.12f,.93f,t);var pose=new HumanPose{bodyPosition=Vector3.Lerp(start.bodyPosition,end.bodyPosition,rise),bodyRotation=Quaternion.Slerp(start.bodyRotation,end.bodyRotation,rise),muscles=new float[HumanTrait.MuscleCount]};
 for(int j=0;j<pose.muscles.Length;j++)pose.muscles[j]=Mathf.Lerp(start.muscles[j],end.muscles[j],Ease(.15f,.95f,t));
 handler.SetHumanPose(ref pose);
 if(t>0&&t<.72f){Vector3 correction=Vector3.zero;for(int side=0;side<2;side++){var shoulder=animator.GetBoneTransform(side==0?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);var elbow=animator.GetBoneTransform(side==0?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);float reach=(Vector3.Distance(shoulder.position,elbow.position)+Vector3.Distance(elbow.position,tips[side].position))*.9f;var d=shoulder.position-first[side];if(d.magnitude>reach)correction+=d.normalized*(d.magnitude-reach)*.5f;}pose.bodyPosition-=correction/animator.humanScale*Ease(0,.08f,t)*(1-Ease(.48f,.72f,t));handler.SetHumanPose(ref pose);}
 var basePosition=pose.bodyPosition;var baseRotation=pose.bodyRotation;var baseMuscles=(float[])pose.muscles.Clone();
 if(i>0&&t<1){
 for(int side=0;side<2;side++){
 bool left=side==0;var hand=tips[side];var foot=tips[side+2];float step=Ease(left?.37f:.25f,left?.8f:.62f,t);
 var footTarget=Vector3.Lerp(first[side+2],last[side+2],step);footTarget.y+=.06f*Mathf.Sin(Mathf.PI*step);
 Solve(animator.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg),animator.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg),foot,footTarget,go.transform.position+new Vector3(left?-.25f:.25f,.1f,1));foot.rotation=Quaternion.Slerp(firstRot[side+2],lastRot[side+2],step);
 float release=Ease(left?.48f:.57f,left?.83f:.9f,t);var handTarget=Vector3.Lerp(first[side],hand.position,release);
 Solve(animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm),animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm),hand,handTarget,go.transform.position+new Vector3(left?-.65f:.65f,.2f,-.2f));hand.rotation=Quaternion.Slerp(firstRot[side],lastRot[side],release);
 if(release==0)handError=Mathf.Max(handError,Vector3.Distance(hand.position,handTarget));
 }
 if(t<1){
 var low=new[]{float.PositiveInfinity,float.PositiveInfinity};
 foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh,false);var vertices=mesh.vertices;var weights=skin.sharedMesh.boneWeights;var scale=skin.transform.lossyScale;
 for(int v=0;v<vertices.Length;v++){var w=weights[v];for(int side=0;side<2;side++){string footName=side==0?"Foot.L":"Foot.R";if(!((skin.bones[w.boneIndex0].name==footName&&w.weight0>.25f)||(skin.bones[w.boneIndex1].name==footName&&w.weight1>.25f)))continue;var x=vertices[v];low[side]=Mathf.Min(low[side],skin.transform.TransformPoint(new Vector3(x.x/scale.x,x.y/scale.y,x.z/scale.z)).y);}}UnityEngine.Object.DestroyImmediate(mesh);}
 for(int side=0;side<2;side++){bool left=side==0;var foot=tips[side+2];var rotation=foot.rotation;var target=foot.position+Vector3.up*Mathf.Max(0,-.00038f-low[side])*(1-Ease(.8f,.96f,t));Solve(animator.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg),animator.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg),foot,target,go.transform.position+new Vector3(left?-.25f:.25f,.1f,1));foot.rotation=rotation;}
 }
 handler.GetHumanPose(ref pose);
 float weight=Ease(0,.08f,t)*(1-Ease(.82f,1,t));pose.bodyPosition=Vector3.Lerp(basePosition,pose.bodyPosition,weight);pose.bodyRotation=Quaternion.Slerp(baseRotation,pose.bodyRotation,weight);for(int m=0;m<pose.muscles.Length;m++)pose.muscles[m]=Mathf.Lerp(baseMuscles[m],pose.muscles[m],weight);
 }
 if(i==0)pose=start;if(i==frames-1)pose=end;poses[i]=pose;
 }
 // Limit rapid IK branch changes before baking; preserve the exact endpoint poses.
 for(int m=0;m<HumanTrait.MuscleCount;m++){
 for(int i=1;i<frames-1;i++)poses[i].muscles[m]=Mathf.Clamp(poses[i].muscles[m],poses[i-1].muscles[m]-.06f,poses[i-1].muscles[m]+.06f);
 for(int i=frames-2;i>0;i--)poses[i].muscles[m]=Mathf.Clamp(poses[i].muscles[m],poses[i+1].muscles[m]-.06f,poses[i+1].muscles[m]+.06f);
 for(int pass=0;pass<3;pass++){var values=poses.Select(x=>x.muscles[m]).ToArray();for(int i=1;i<frames-1;i++)poses[i].muscles[m]=values[i-1]*.25f+values[i]*.5f+values[i+1]*.25f;}
 }
 Undo.RecordObject(clip,"Rebuild supported get-up");
 for(int j=0;j<HumanTrait.MuscleCount+7;j++){
 string name=j<HumanTrait.MuscleCount?HumanTrait.MuscleName[j]:new[]{"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"}[j-HumanTrait.MuscleCount];var keys=new Keyframe[frames];
 Quaternion previous=poses[0].bodyRotation;
 for(int i=0;i<frames;i++){var pose=poses[i];var rot=pose.bodyRotation;if(Quaternion.Dot(previous,rot)<0)rot=new Quaternion(-rot.x,-rot.y,-rot.z,-rot.w);previous=rot;float value=j<HumanTrait.MuscleCount?pose.muscles[j]:j<HumanTrait.MuscleCount+3?pose.bodyPosition[j-HumanTrait.MuscleCount]:rot[j-HumanTrait.MuscleCount-3];keys[i]=new Keyframe(clip.length*i/(frames-1),value);}
 var curve=new AnimationCurve(keys);for(int k=0;k<frames;k++)curve.SmoothTangents(k,0);AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),name),curve);
 }EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();Debug.Log("Supported GetUp rebuilt. Max planted hand target error="+handError);
 }finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(go);}
 }
}

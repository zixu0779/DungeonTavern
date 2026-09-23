using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DungeonTavern.Tavern25D;

public static class ProtagonistRigCheck
{
    [MenuItem("Tools/Characters/Check Protagonist Rig")]
    public static void Run()
    {
        const string path="Assets/DungeonTavern/Art/Characters/Protagonist/Protagonist.prefab";
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        try
        {
            var animator=go.GetComponent<Animator>();
            if(!animator.avatar.isValid || !animator.isHuman)throw new Exception("Invalid Humanoid avatar");
            var cape=go.GetComponent<ProtagonistCapeMotion>();
            var front=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="Cape.Front.L");
            var thigh=animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var rest=front.localRotation;var thighRest=thigh.localRotation;
            thigh.Rotate(go.transform.right,-60,Space.World);cape.Evaluate();
            if(Quaternion.Angle(rest,front.localRotation)<40)throw new Exception("Cape did not lift for raised thigh");
            thigh.localRotation=thighRest;cape.Evaluate();
            if(Quaternion.Angle(rest,front.localRotation)>.1f)throw new Exception("Cape did not return to rest");
            foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                if(skin.bones.Any(b=>!b)||!skin.sharedMaterial||!skin.sharedMaterial.GetTexture("_BaseMap"))throw new Exception("Missing skin bone or material texture");
            File.WriteAllText(Path.Combine(Path.GetTempPath(),"protagonist-rig-check.txt"),"PASS Humanoid avatar, cape lift/rest, skin bones, material texture");
            Debug.Log("Protagonist rig check passed");
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}

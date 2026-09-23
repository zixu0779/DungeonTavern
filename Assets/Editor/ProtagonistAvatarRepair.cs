using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class ProtagonistAvatarRepair {
 [MenuItem("Tools/Characters/Inspect Avatar Pose Tools")]
 public static void Inspect(){
 var type=typeof(Editor).Assembly.GetType("UnityEditor.AvatarSetupTool");
 File.WriteAllLines("/tmp/avatar-pose-api.txt",type.GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Select(m=>m.ToString()));
 }
}

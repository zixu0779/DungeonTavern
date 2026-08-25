using System;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class TavernSignClosingCheck
{
    [MenuItem("Tools/Dungeon Tavern/Props/Check Tavern Sign Closing")]
    private static void Check()
    {
        const string folder = "Assets/DungeonTavern/Art/Models/TavernSign/";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "Animations/TavernSign_Closing.anim");
        var open = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "Animations/TavernSign_Open.anim");
        var closed = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "Animations/TavernSign_Closed.anim");
        var root = PrefabUtility.LoadPrefabContents(folder + "TavernSign.prefab");
        try
        {
            var pivot = root.transform.Find("BoardPivot");
            var frame = root.transform.Find("TavernSign_FixedFrame");
            if (!pivot.Find("TavernSign_RotatingVBoard")) throw new Exception("Moving board must be under BoardPivot.");
            if (!frame) throw new Exception("FixedFrame must be outside BoardPivot.");
            var fixedMatrix = frame.localToWorldMatrix;
            open.SampleAnimation(root, 0);
            var startPosition = pivot.localPosition;
            var startRotation = pivot.localRotation;
            clip.SampleAnimation(root, 0);
            if (Vector3.Distance(startPosition, pivot.localPosition) > .0001f || Quaternion.Angle(startRotation, pivot.localRotation) > .01f)
                throw new Exception("Closing start does not match Open.");
            float previousY = pivot.localPosition.y;
            for (int i = 0; i <= 100; i++)
            {
                clip.SampleAnimation(root, clip.length * i / 100f);
                if (pivot.localPosition.y < previousY - .0001f || Mathf.Abs(pivot.localPosition.z - startPosition.z) > .0001f)
                    throw new Exception("Axis moved down or departed from slot.");
                previousY = pivot.localPosition.y;
                for (int k = 0; k < 16; k++)
                    if (Mathf.Abs(fixedMatrix[k] - frame.localToWorldMatrix[k]) > .0001f)
                        throw new Exception("Fixed frame moved.");
            }
            var endPosition = pivot.localPosition;
            var endRotation = pivot.localRotation;
            closed.SampleAnimation(root, 0);
            if (Vector3.Distance(endPosition, pivot.localPosition) > .0001f || Quaternion.Angle(endRotation, pivot.localRotation) > .01f)
                throw new Exception("Closing end does not match Closed.");
            File.WriteAllText("/tmp/dt-sign-closing-validation.txt", "PASS: endpoints match authored states; monotonic lift; fixed slot depth; stationary frame.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}

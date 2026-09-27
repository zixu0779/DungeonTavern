using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Exercises the real controller with measured-speed profiles, without touching a scene actor.
public static class LocomotionStopCheck
{
    [MenuItem("Tools/Characters/Check Locomotion Stop")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Run this isolated check outside Play Mode");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonTavern/Art/Characters/Protagonist/Protagonist.prefab");
        var rows = new List<string> { "profile,frame,actualSpeed,walkRate,state,transition,normalizedTime" };
        const float dt = 1f / 60;
        foreach (string profile in new[] { "gradual", "abrupt-0.1", "abrupt-0.4", "abrupt-0.7" })
        {
            var go = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var animator = go.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.applyRootMotion = false; animator.Rebind();
                animator.SetFloat("Speed", 3.25f); animator.SetFloat("WalkRate", 1);
                float phase = profile == "gradual" ? .2f : float.Parse(profile.Substring(7), System.Globalization.CultureInfo.InvariantCulture);
                animator.Play("Walk", 0, phase); animator.Update(0);
                var clip = animator.runtimeAnimatorController.animationClips.Single(c => c.name == "Walk");
                float previous = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                int firstZero = -1, settled = -1;
                for (int i = 0; i < 75; i++)
                {
                    float speed = i < 12 ? 3.25f : profile == "gradual" ? 3.25f * Mathf.Clamp01(1 - (i - 11) / 24f) : 0;
                    if (speed == 0 && firstZero < 0) firstZero = i;
                    go.transform.position += Vector3.forward * (speed * dt);
                    animator.SetFloat("Speed", speed); animator.SetFloat("WalkRate", speed / 3.25f); animator.Update(dt);
                    var state = animator.GetCurrentAnimatorStateInfo(0); bool transition = animator.IsInTransition(0);
                    if (state.IsName("Walk") && !transition && speed > .08f)
                    {
                        float expected = dt * speed / 3.25f / clip.length;
                        if (Mathf.Abs(state.normalizedTime - previous - expected) > .001f) throw new Exception("Walk phase failed to track actual speed");
                    }
                    if (firstZero >= 0 && settled < 0 && state.IsName("Idle") && !transition) settled = i;
                    rows.Add($"{profile},{i},{speed:F4},{animator.GetFloat("WalkRate"):F4},{(state.IsName("Walk") ? "Walk" : "Idle")},{transition},{state.normalizedTime:F5}");
                    previous = state.normalizedTime;
                }
                if (settled < 0 || (settled - firstZero) * dt > .12f) throw new Exception("Stop transition exceeded 120 ms: " + profile);
                rows.Add($"# PASS {profile}: settled={(settled-firstZero)*dt:F4}s after zero speed; Walk phase tracks slowdown");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        File.WriteAllLines("/tmp/locomotion-stop-check.csv", rows);
        Debug.Log("Locomotion stop check passed: slowdown and three abrupt-stop phases");
    }
}

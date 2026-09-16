using UnityEditor;
using UnityEngine;
using DungeonTavern.Prototypes.Rotation25D;

// AnimationMode previews the runtime clip without saving posed bones into the model.
[InitializeOnLoad]
public static class OpeningPosePreview
{
    static GameObject model;
    static AnimationClip clip;
    static OpeningPosePreview()
    {
        EditorApplication.playModeStateChanged += _ => Stop();
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
    }
    [MenuItem("Tools/Characters/Preview Opening Pose")]
    static void Start()
    {
        if (EditorApplication.isPlaying) return;
        var player = Object.FindAnyObjectByType<PrototypePlayerMover>();
        if (!player) { Debug.LogWarning("Open SealRoom_B1 to preview its protagonist."); return; }
        Stop();
        model = player.GetComponentInChildren<Animator>().gameObject;
        clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DungeonTavern/Gameplay/Animations/Prone.anim");
        AnimationMode.StartAnimationMode();
        EditorApplication.update += Sample;
        Selection.activeGameObject = player.gameObject;
        Sample();
    }
    static void Sample()
    {
        if (!model || !clip || !AnimationMode.InAnimationMode()) { Stop(); return; }
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(model, clip, 0);
        AnimationMode.EndSampling();
        SceneView.RepaintAll();
    }
    [MenuItem("Tools/Characters/Stop Opening Pose Preview")]
    static void Stop()
    {
        EditorApplication.update -= Sample;
        if (model && AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
        model = null;
        clip = null;
    }
}

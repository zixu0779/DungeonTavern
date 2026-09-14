using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    public sealed class CupDispenserActivation : MonoBehaviour
    {
        [SerializeField, Tooltip("运行时切换：圆环升起并发光；关闭后回到当前摆放位置。")]
        private bool activated;
        [SerializeField] private Transform halo;
        [SerializeField] private Renderer haloRenderer;
        [SerializeField] private Material energizedMaterial;
        [SerializeField] private Transform effectsRoot;
        [SerializeField] private ParticleSystem[] particles;
        [SerializeField] private Light accentLight;
        [SerializeField, Min(0)] private float liftHeight = .45f;
        [SerializeField, Min(.05f)] private float transitionDuration = 1.1f;
        [SerializeField, ColorUsage(false, true)] private Color glowColor = new Color(.2f, 1.8f, 3f, 1);
        [SerializeField, Min(0)] private float lightIntensity = .5f;

        private Renderer[] effectRenderers;
        private Material restingMaterial;
        private Vector3 restingPosition, restingEffectsPosition;
        private float progress;
        private bool initialized;
        private MaterialPropertyBlock glowBlock, effectBlock;
        public bool IsActivated => activated;
        public Vector3 GuidancePosition => halo ? halo.parent.TransformPoint(initialized?restingPosition:halo.localPosition) : transform.position;

        private void OnEnable()
        {
            if (!halo || !haloRenderer || !effectsRoot || !energizedMaterial) return;
            restingMaterial = haloRenderer.sharedMaterial;
            haloRenderer.sharedMaterial = energizedMaterial;
            restingPosition = halo.localPosition;
            restingEffectsPosition = effectsRoot.localPosition;
            glowBlock = new MaterialPropertyBlock();
            effectBlock = new MaterialPropertyBlock();
            effectRenderers = effectsRoot.GetComponentsInChildren<Renderer>(true);
            initialized = true;
            progress = activated ? 1 : 0;
            Apply();
        }

        private void Update() => Advance(Time.deltaTime);

        private void Advance(float deltaTime)
        {
            if (!initialized) return;
            progress = Mathf.MoveTowards(progress, activated ? 1 : 0, deltaTime / Mathf.Max(.05f, transitionDuration));
            Apply();
        }

        private void Apply()
        {
            float amount = Mathf.SmoothStep(0, 1, progress);
            halo.localPosition = restingPosition + Vector3.up * (liftHeight * amount);
            effectsRoot.localPosition = restingEffectsPosition + Vector3.up * (liftHeight * amount);
            haloRenderer.GetPropertyBlock(glowBlock);
            glowBlock.SetColor("_EmissionColor", glowColor * amount);
            haloRenderer.SetPropertyBlock(glowBlock);
            effectBlock.SetColor("_Tint", new Color(1, 1, 1, amount));
            foreach (var renderer in effectRenderers) renderer.SetPropertyBlock(effectBlock);
            foreach (var system in particles)
            {
                if (!system) continue;
                if (amount > .001f && !system.isPlaying) system.Play(true);
                else if (amount <= .001f && (system.isPlaying || system.particleCount > 0))
                    system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (accentLight)
            {
                accentLight.enabled = amount > .001f;
                accentLight.intensity = lightIntensity * amount;
            }
        }

        public void SetActivated(bool value) => activated = value;
        [ContextMenu("Activate (Play Mode)")]
        public void Activate() { if (Application.isPlaying) SetActivated(true); }
        [ContextMenu("Deactivate (Play Mode)")]
        public void Deactivate() { if (Application.isPlaying) SetActivated(false); }

        private void OnDisable()
        {
            if (!initialized) return;
            if (halo) halo.localPosition = restingPosition;
            if (effectsRoot) effectsRoot.localPosition = restingEffectsPosition;
            if (haloRenderer) { haloRenderer.sharedMaterial = restingMaterial; haloRenderer.SetPropertyBlock(null); }
            foreach (var renderer in effectRenderers) if (renderer) renderer.SetPropertyBlock(null);
            foreach (var system in particles)
                if (system) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (accentLight) { accentLight.enabled = false; accentLight.intensity = 0; }
            initialized = false;
        }
    }
}

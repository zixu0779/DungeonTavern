using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DungeonTavern.Prototypes.Rotation25D
{
    public sealed class DialogueOcclusionFader : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 0.9f)] private float fadedAlpha = 0.22f;

        private sealed class RendererState
        {
            public Renderer Renderer;
            public Material[] OriginalMaterials;
            public Material[] RuntimeMaterials;
            public Color SpriteColor;
        }

        private readonly Dictionary<Renderer, RendererState> faded = new();
        private readonly HashSet<Renderer> requested = new();

        public void SetOccluders(IEnumerable<Renderer> renderers)
        {
            requested.Clear();
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                    requested.Add(renderer);
            }

            var restore = new List<Renderer>();
            foreach (Renderer renderer in faded.Keys)
            {
                if (!requested.Contains(renderer))
                    restore.Add(renderer);
            }
            for (int index = 0; index < restore.Count; index++)
                Restore(restore[index]);

            foreach (Renderer renderer in requested)
            {
                if (!faded.ContainsKey(renderer))
                    Fade(renderer);
            }
        }

        public void RestoreAll()
        {
            var renderers = new List<Renderer>(faded.Keys);
            for (int index = 0; index < renderers.Count; index++)
                Restore(renderers[index]);
            requested.Clear();
        }

        private void Fade(Renderer renderer)
        {
            RendererState state = new()
            {
                Renderer = renderer,
                OriginalMaterials = renderer.sharedMaterials
            };

            if (renderer is SpriteRenderer sprite)
            {
                state.SpriteColor = sprite.color;
                Color fadedColor = sprite.color;
                fadedColor.a = Mathf.Min(fadedColor.a, fadedAlpha);
                sprite.color = fadedColor;
            }
            else
            {
                state.RuntimeMaterials = new Material[state.OriginalMaterials.Length];
                for (int index = 0; index < state.OriginalMaterials.Length; index++)
                {
                    Material original = state.OriginalMaterials[index];
                    if (original == null)
                        continue;
                    Material runtime = new(original) { name = $"{original.name}_DialogueFade" };
                    ConfigureTransparent(runtime, fadedAlpha);
                    state.RuntimeMaterials[index] = runtime;
                }
                renderer.materials = state.RuntimeMaterials;
            }
            faded.Add(renderer, state);
        }

        private void Restore(Renderer renderer)
        {
            if (!faded.Remove(renderer, out RendererState state))
                return;

            if (renderer != null)
            {
                if (renderer is SpriteRenderer sprite)
                    sprite.color = state.SpriteColor;
                else
                    renderer.sharedMaterials = state.OriginalMaterials;
            }

            if (state.RuntimeMaterials == null)
                return;
            for (int index = 0; index < state.RuntimeMaterials.Length; index++)
            {
                if (state.RuntimeMaterials[index] != null)
                    Destroy(state.RuntimeMaterials[index]);
            }
        }

        private static void ConfigureTransparent(Material material, float alpha)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)RenderQueue.Transparent;

            FadeColor(material, "_BaseColor", alpha);
            FadeColor(material, "_Color", alpha);
        }

        private static void FadeColor(Material material, string property, float alpha)
        {
            if (!material.HasProperty(property))
                return;
            Color color = material.GetColor(property);
            color.a = Mathf.Min(color.a, alpha);
            material.SetColor(property, color);
        }

        private void OnDisable()
        {
            RestoreAll();
        }
    }
}

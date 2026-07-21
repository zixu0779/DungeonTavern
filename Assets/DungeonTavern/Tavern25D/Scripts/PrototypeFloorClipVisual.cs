using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class PrototypeFloorClipVisual : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Sprite sourceSprite;
        [SerializeField] private Color tint = Color.white;

        public Sprite SourceSprite
        {
            get => sourceSprite;
            set
            {
                sourceSprite = value;
                Apply();
            }
        }

        public Color Tint
        {
            get => tint;
            set
            {
                tint = value;
                Apply();
            }
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                return;

            MaterialPropertyBlock block = new();
            block.SetColor(BaseColorId, tint);

            if (sourceSprite == null)
            {
                block.SetTexture(BaseMapId, Texture2D.whiteTexture);
                block.SetVector(BaseMapStId, new Vector4(1f, 1f, 0f, 0f));
            }
            else
            {
                Texture2D texture = sourceSprite.texture;
                Rect rect = sourceSprite.textureRect;
                block.SetTexture(BaseMapId, texture);
                block.SetVector(
                    BaseMapStId,
                    new Vector4(
                        rect.width / texture.width,
                        rect.height / texture.height,
                        rect.x / texture.width,
                        rect.y / texture.height));
            }

            meshRenderer.SetPropertyBlock(block);
        }
    }
}

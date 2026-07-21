using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class EditableWallFaceSprites : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Drag a Sprite into each wall face")]
        [SerializeField] private Sprite front;
        [SerializeField] private Sprite back;
        [SerializeField] private Sprite left;
        [SerializeField] private Sprite right;
        [SerializeField] private Sprite top;

        [Header("Face options")]
        [Tooltip("The back face reuses Front and mirrors it horizontally.")]
        [SerializeField] private bool mirrorBackHorizontally = true;

        public Sprite Front
        {
            get => front;
            set
            {
                front = value;
                Apply();
            }
        }

        public Sprite Back
        {
            get => back;
            set
            {
                back = value;
                Apply();
            }
        }

        public Sprite Left
        {
            get => left;
            set
            {
                left = value;
                Apply();
            }
        }

        public Sprite Right
        {
            get => right;
            set
            {
                right = value;
                Apply();
            }
        }

        public Sprite Top
        {
            get => top;
            set
            {
                top = value;
                Apply();
            }
        }

        public bool MirrorBackHorizontally
        {
            get => mirrorBackHorizontally;
            set
            {
                mirrorBackHorizontally = value;
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

            ApplyFace(meshRenderer, 0, front, false);
            ApplyFace(meshRenderer, 1, back, mirrorBackHorizontally);
            ApplyFace(meshRenderer, 2, left, false);
            ApplyFace(meshRenderer, 3, right, false);
            ApplyFace(meshRenderer, 4, top, false);
        }

        private static void ApplyFace(
            Renderer renderer,
            int materialIndex,
            Sprite sprite,
            bool mirrorHorizontally)
        {
            MaterialPropertyBlock block = new();
            block.SetColor(BaseColorId, sprite != null ? Color.white : Color.clear);

            if (sprite == null)
            {
                block.SetTexture(BaseMapId, Texture2D.whiteTexture);
                block.SetVector(BaseMapStId, new Vector4(1f, 1f, 0f, 0f));
            }
            else
            {
                Texture2D texture = sprite.texture;
                Rect rect = sprite.textureRect;
                float scaleX = rect.width / texture.width;
                float offsetX = rect.x / texture.width;
                if (mirrorHorizontally)
                {
                    scaleX = -scaleX;
                    offsetX = (rect.x + rect.width) / texture.width;
                }

                block.SetTexture(BaseMapId, texture);
                block.SetVector(
                    BaseMapStId,
                    new Vector4(
                        scaleX,
                        rect.height / texture.height,
                        offsetX,
                        rect.y / texture.height));
            }

            renderer.SetPropertyBlock(block, materialIndex);
        }
    }
}

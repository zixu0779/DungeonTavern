using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class EditableWallFaceSprites : MonoBehaviour
    {
        public enum QuarterTurn
        {
            None = 0,
            Clockwise90 = 1,
            Rotate180 = 2,
            Clockwise270 = 3
        }

        [System.Serializable]
        private struct FaceUvOptions
        {
            public QuarterTurn rotation;
            public bool flipX;
            public bool flipY;
        }

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SpriteRectId = Shader.PropertyToID("_SpriteRect");
        private static readonly int FaceUvScaleId = Shader.PropertyToID("_FaceUvScale");
        private static readonly int FaceRotationId = Shader.PropertyToID("_FaceRotation");
        private static readonly int FlipXId = Shader.PropertyToID("_FlipX");
        private static readonly int FlipYId = Shader.PropertyToID("_FlipY");

        [Header("Drag a Sprite into each wall face")]
        [SerializeField] private Sprite front;
        [SerializeField] private Sprite back;
        [SerializeField] private Sprite left;
        [SerializeField] private Sprite right;
        [SerializeField] private Sprite top;

        [Header("Optional doorway inner faces")]
        [Tooltip("The inner vertical surface on the left side of a doorway. Falls back to Left when empty.")]
        [SerializeField] private Sprite leftJamb;
        [Tooltip("The inner vertical surface on the right side of a doorway. Falls back to Left when empty.")]
        [SerializeField] private Sprite rightJamb;

        [Header("Face options")]
        [Tooltip("The back face reuses Front and mirrors it horizontally.")]
        [SerializeField] private bool mirrorBackHorizontally = true;

        [Header("Per-face direction")]
        [SerializeField] private FaceUvOptions frontDirection;
        [SerializeField] private FaceUvOptions backDirection;
        [SerializeField] private FaceUvOptions leftDirection;
        [SerializeField] private FaceUvOptions rightDirection;
        [SerializeField] private FaceUvOptions topDirection;
        [SerializeField] private FaceUvOptions leftJambDirection;
        [SerializeField] private FaceUvOptions rightJambDirection;

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

            ApplyFace(meshRenderer, 0, front, false, frontDirection);
            ApplyFace(meshRenderer, 1, back, mirrorBackHorizontally, backDirection);
            ApplyFace(meshRenderer, 2, left, false, leftDirection);
            ApplyFace(meshRenderer, 3, right, false, rightDirection);
            ApplyFace(meshRenderer, 4, top, false, topDirection);
            ApplyFace(meshRenderer, 5, leftJamb != null ? leftJamb : left, false, leftJambDirection);
            ApplyFace(meshRenderer, 6, rightJamb != null ? rightJamb : left, false, rightJambDirection);
        }

        private static void ApplyFace(
            Renderer renderer,
            int materialIndex,
            Sprite sprite,
            bool mirrorHorizontally,
            FaceUvOptions direction)
        {
            if (materialIndex < 0 || materialIndex >= renderer.sharedMaterials.Length)
                return;

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

                block.SetVector(
                    SpriteRectId,
                    new Vector4(
                        rect.x / texture.width,
                        rect.y / texture.height,
                        rect.width / texture.width,
                        rect.height / texture.height));
                block.SetVector(
                    FaceUvScaleId,
                    new Vector4(
                        sprite.pixelsPerUnit / rect.width,
                        sprite.pixelsPerUnit / rect.height,
                        0f,
                        0f));
            }


            block.SetFloat(FaceRotationId, (float)direction.rotation);
            block.SetFloat(FlipXId, (direction.flipX ^ mirrorHorizontally) ? 1f : 0f);
            block.SetFloat(FlipYId, direction.flipY ? 1f : 0f);

            renderer.SetPropertyBlock(block, materialIndex);
        }
    }
}

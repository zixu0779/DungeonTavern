using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PrototypeSpriteDepthSort : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private int bias;

        private SpriteRenderer spriteRenderer;

        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }

        public int Bias
        {
            get => bias;
            set => bias = value;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (cameraTransform == null)
                return;

            float depth = Vector3.Dot(
                cameraTransform.forward,
                spriteRenderer.bounds.center - cameraTransform.position);
            spriteRenderer.sortingOrder = 10000 - Mathf.RoundToInt(depth * 100f) + bias;
        }
    }
}

using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    public sealed class PrototypeBillboard : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;

        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (cameraTransform == null)
                return;

            Vector3 direction = cameraTransform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}

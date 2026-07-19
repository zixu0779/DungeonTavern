using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    public sealed class PrototypeCardinalSprite : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private SpriteRenderer northSouthFace;
        [SerializeField] private SpriteRenderer eastWestFace;

        public void Configure(
            Transform targetCamera,
            SpriteRenderer northSouth,
            SpriteRenderer eastWest)
        {
            cameraTransform = targetCamera;
            northSouthFace = northSouth;
            eastWestFace = eastWest;
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (cameraTransform == null || northSouthFace == null || eastWestFace == null)
                return;

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            bool viewAlongZ = Mathf.Abs(forward.z) >= Mathf.Abs(forward.x);
            northSouthFace.enabled = viewAlongZ;
            eastWestFace.enabled = !viewAlongZ;
        }
    }
}

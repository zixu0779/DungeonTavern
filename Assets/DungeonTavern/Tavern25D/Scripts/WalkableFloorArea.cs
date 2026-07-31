using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WalkableFloorArea : MonoBehaviour
    {
        [Header("Editable walkable bounds")]
        [SerializeField] private Vector3 size = new(49f, 0.3f, 32f);
        [SerializeField] private bool showInSceneView = true;
        [SerializeField] private Color gizmoColor = new(0.15f, 0.85f, 0.45f, 0.18f);
        [SerializeField, HideInInspector] private MeshRenderer previewRenderer;
        [SerializeField, HideInInspector] private bool initialized;

        public Vector3 Size => size;

        public void Initialize(Vector3 defaultSize, MeshRenderer renderer)
        {
            if (!initialized)
            {
                size = defaultSize;
                initialized = true;
            }
            previewRenderer = renderer;
            SyncRepresentation();
        }

        private void OnEnable()
        {
            SyncRepresentation();
        }

        private void OnValidate()
        {
            size.x = Mathf.Max(0.1f, size.x);
            size.y = Mathf.Max(0.05f, size.y);
            size.z = Mathf.Max(0.1f, size.z);
            SyncRepresentation();
        }

        private void Update()
        {
            if (previewRenderer != null)
                previewRenderer.enabled = showInSceneView && !Application.isPlaying;
        }

        private void SyncRepresentation()
        {
            BoxCollider floor = GetComponent<BoxCollider>();
            floor.center = Vector3.zero;
            floor.size = size;
            floor.isTrigger = false;

            if (previewRenderer == null)
                return;

            Transform preview = previewRenderer.transform;
            preview.localPosition = new Vector3(0f, size.y * 0.5f + 0.04f, 0f);
            preview.localRotation = Quaternion.identity;
            preview.localScale = new Vector3(size.x, 0.02f, size.z);
            previewRenderer.enabled = showInSceneView && !Application.isPlaying;
        }

        private void OnDrawGizmos()
        {
            if (!showInSceneView)
                return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 footprintCenter = new(0f, size.y * 0.5f + 0.04f, 0f);
            Vector3 footprintSize = new(size.x, 0.02f, size.z);
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(footprintCenter, footprintSize);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
            Gizmos.DrawWireCube(footprintCenter, footprintSize);
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
}

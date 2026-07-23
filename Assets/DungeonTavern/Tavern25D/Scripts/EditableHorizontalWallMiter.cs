using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    public enum WallMiterInterface
    {
        Square = 0,
        OneToOneUp = 1,
        OneToOneDown = 2,
        TwoToOneUp = 3,
        TwoToOneDown = 4,
        [InspectorName("Horizontal (+X)")]
        Horizontal = 5,
        [InspectorName("Vertical (+Z)")]
        Vertical = 6,
        [InspectorName("Horizontal Reverse (-X)")]
        HorizontalReverse = 7,
        [InspectorName("Vertical Reverse (-Z)")]
        VerticalReverse = 8
    }

    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class EditableHorizontalWallMiter : MonoBehaviour
    {
        private const int InterfaceCount = 9;

        [Header("Neighbour wall direction at each endpoint")]
        [Tooltip(
            "T-junction rule: keep both collinear wall endpoints Square, and set " +
            "only the branching wall endpoint to Horizontal/Vertical as appropriate.")]
        [SerializeField] private WallMiterInterface startInterface;
        [SerializeField] private WallMiterInterface endInterface;
        [Header("Generated interface meshes")]
        [Tooltip("Used while this wall instance runs horizontally.")]
        [SerializeField] private Mesh[] interfaceMeshes;
        [Tooltip("Used while this wall instance is rotated vertically.")]
        [SerializeField] private Mesh[] verticalInterfaceMeshes;

        private bool orientationInitialized;
        private bool wasVertical;

        public WallMiterInterface StartInterface
        {
            get => startInterface;
            set
            {
                startInterface = value;
                Apply();
            }
        }

        public WallMiterInterface EndInterface
        {
            get => endInterface;
            set
            {
                endInterface = value;
                Apply();
            }
        }

        public void Configure(Mesh[] horizontalMeshes, Mesh[] verticalMeshes)
        {
            interfaceMeshes = horizontalMeshes;
            verticalInterfaceMeshes = verticalMeshes;
            startInterface = WallMiterInterface.Square;
            endInterface = WallMiterInterface.Square;
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Update()
        {
            bool isVertical = IsVertical();
            if (!orientationInitialized || isVertical != wasVertical)
                Apply();
        }

        public void Apply()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            bool isVertical = IsVertical();
            Mesh[] meshes = isVertical ? verticalInterfaceMeshes : interfaceMeshes;
            if (filter == null ||
                meshes == null ||
                meshes.Length != InterfaceCount * InterfaceCount)
            {
                return;
            }

            int index = (int)startInterface * InterfaceCount + (int)endInterface;
            filter.sharedMesh = meshes[index];
            wasVertical = isVertical;
            orientationInitialized = true;
        }

        private bool IsVertical()
        {
            float lineAngle = Mathf.Repeat(transform.eulerAngles.y, 180f);
            return lineAngle > 45f && lineAngle < 135f;
        }
    }
}

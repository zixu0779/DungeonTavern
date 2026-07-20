using UnityEngine;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class EditableWallMiter : MonoBehaviour
    {
        private const int InterfaceCount = 7;

        [Header("Neighbour wall type at each endpoint")]
        [Tooltip("Square keeps the original flat endpoint.")]
        [SerializeField] private WallMiterInterface startInterface;
        [Tooltip("Square keeps the original flat endpoint.")]
        [SerializeField] private WallMiterInterface endInterface;
        [SerializeField] private Mesh[] interfaceMeshes;

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

        public void Configure(Mesh[] meshes)
        {
            interfaceMeshes = meshes;
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

        public void Apply()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter == null ||
                interfaceMeshes == null ||
                interfaceMeshes.Length != InterfaceCount * InterfaceCount)
            {
                return;
            }

            int index = (int)startInterface * InterfaceCount + (int)endInterface;
            filter.sharedMesh = interfaceMeshes[index];
        }
    }
}

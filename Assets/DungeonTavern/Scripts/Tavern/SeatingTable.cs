using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public enum SeatingTableType { Long, SmallRound, LargeRound }
    public enum CustomerSeatingKind { Solitary, Sociable, Party }

    public sealed class SeatingTable : MonoBehaviour
    {
        [SerializeField] private SeatingTableType tableType;
        [SerializeField] private SeatPoint[] seats = System.Array.Empty<SeatPoint>();
        public SeatingTableType TableType => tableType;
        public SeatPoint[] Seats => seats;
        private MeshFilter surfaceMesh;
        private MeshFilter SurfaceMesh
        {
            get
            {
                if(surfaceMesh)return surfaceMesh;
                float largest=0;
                foreach(var mesh in GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer=mesh.GetComponent<Renderer>();if(!renderer||!mesh.sharedMesh)continue;
                    float area=renderer.bounds.size.x*renderer.bounds.size.z;
                    if(area>largest){largest=area;surfaceMesh=mesh;}
                }
                return surfaceMesh;
            }
        }
        public Vector3 SurfaceCenter => SurfaceMesh?SurfaceMesh.transform.TransformPoint(SurfaceMesh.sharedMesh.bounds.center):transform.position;
        public Vector3 FacingFrom(Vector3 seat)
        {
            var toward=Vector3.ProjectOnPlane(SurfaceCenter-seat,Vector3.up);
            if(tableType!=SeatingTableType.Long||!SurfaceMesh)return toward.normalized;
            var mesh=SurfaceMesh;var extents=mesh.sharedMesh.bounds.extents;
            // Imported tables lie in local XY, not necessarily XZ. Use the mesh's two horizontal axes.
            var a=Vector3.ProjectOnPlane(mesh.transform.TransformVector(Vector3.right*extents.x),Vector3.up);
            var b=Vector3.ProjectOnPlane(mesh.transform.TransformVector(Vector3.up*extents.y),Vector3.up);
            var c=Vector3.ProjectOnPlane(mesh.transform.TransformVector(Vector3.forward*extents.z),Vector3.up);
            if(b.sqrMagnitude>a.sqrMagnitude)(a,b)=(b,a);
            if(c.sqrMagnitude>a.sqrMagnitude){b=a;a=c;}else if(c.sqrMagnitude>b.sqrMagnitude)b=c;
            if(b.sqrMagnitude<.0001f)return toward.normalized;
            var normal=Mathf.Abs(Vector3.Dot(toward,a))/a.sqrMagnitude>Mathf.Abs(Vector3.Dot(toward,b))/b.sqrMagnitude?a.normalized:b.normalized;
            return Vector3.Dot(toward,normal)>=0?normal:-normal;
        }
        public bool IsEmpty
        {
            get { foreach (var seat in seats) if (seat != null && !seat.IsAvailable) return false; return true; }
        }
        public void Configure(SeatingTableType type, SeatPoint[] points) { tableType = type; seats = points; }
    }
}

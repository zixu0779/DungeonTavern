using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WalkableFloorArea : MonoBehaviour
    {
        [Header("Editable walkable bounds")]
        [SerializeField] private Vector3 size = new(49f, 0.3f, 32f);
        [SerializeField, HideInInspector] private bool initialized;

        public Vector3 Size => size;

        public void Initialize(Vector3 defaultSize)
        {
            if (!initialized)
            {
                size = defaultSize;
                initialized = true;
            }
            SyncCollider();
        }

        private void OnEnable()
        {
            SyncCollider();
        }

        private void OnValidate()
        {
            size.x = Mathf.Max(0.1f, size.x);
            size.y = Mathf.Max(0.05f, size.y);
            size.z = Mathf.Max(0.1f, size.z);
            SyncCollider();
        }

        private void SyncCollider()
        {
            BoxCollider floor = GetComponent<BoxCollider>();
            floor.center = Vector3.zero;
            floor.size = size;
            floor.isTrigger = false;
        }
    }
}

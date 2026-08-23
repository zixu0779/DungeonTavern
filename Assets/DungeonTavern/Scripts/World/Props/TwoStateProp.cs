using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    [RequireComponent(typeof(Animator))]
    public sealed class TwoStateProp : MonoBehaviour
    {
        [SerializeField, Tooltip("Requested state. A running transition finishes before the next one starts.")]
        private bool open;
        private Animator animator;
        private static readonly int OpenParameter = Animator.StringToHash("Open");

        public bool IsOpen => open;

        private void OnEnable()
        {
            animator = GetComponent<Animator>();
            if (animator.runtimeAnimatorController == null) return;
            animator.SetBool(OpenParameter, open);
            animator.Play(open ? "Open" : "Closed", 0, 0f);
        }

        public void SetOpen(bool value)
        {
            open = value;
            if (animator != null && isActiveAndEnabled)
                animator.SetBool(OpenParameter, open);
        }

        private void OnValidate()
        {
            if (Application.isPlaying) SetOpen(open);
        }

        [ContextMenu("Open (Play Mode)")]
        public void Open() => SetOpen(true);

        [ContextMenu("Close (Play Mode)")]
        public void Close() => SetOpen(false);
    }
}

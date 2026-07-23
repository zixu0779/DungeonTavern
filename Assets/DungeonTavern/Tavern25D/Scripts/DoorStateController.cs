using System;
using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    [ExecuteAlways]
    public sealed class DoorStateController : MonoBehaviour
    {
        [Header("Authored visual states")]
        [SerializeField] private GameObject closedVisual;
        [SerializeField] private GameObject openVisual;

        [Header("Passage")]
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private bool isOpen;

        public bool IsOpen => isOpen;

        public event Action<bool> StateChanged;

        private void OnEnable()
        {
            ApplyState();
        }

        private void OnValidate()
        {
            ApplyState();
        }

        public void Configure(
            GameObject closedState,
            GameObject openState,
            Collider passageBlocker,
            bool initiallyOpen)
        {
            closedVisual = closedState;
            openVisual = openState;
            blockingCollider = passageBlocker;
            isOpen = initiallyOpen;
            ApplyState();
        }

        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        [ContextMenu("Toggle Door")]
        public void Toggle()
        {
            SetOpen(!isOpen);
        }

        [ContextMenu("Open Door")]
        private void OpenFromContextMenu()
        {
            Open();
        }

        [ContextMenu("Close Door")]
        private void CloseFromContextMenu()
        {
            Close();
        }

        public void SetOpen(bool open)
        {
            bool changed = isOpen != open;
            isOpen = open;
            ApplyState();

            if (changed && Application.isPlaying)
                StateChanged?.Invoke(isOpen);
        }

        private void ApplyState()
        {
            if (closedVisual != null)
                closedVisual.SetActive(!isOpen);
            if (openVisual != null)
                openVisual.SetActive(isOpen);
            if (blockingCollider != null)
                blockingCollider.enabled = !isOpen;
        }
    }
}

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

        [Header("Optional hinged leaves")]
        [SerializeField] private Transform leftHinge;
        [SerializeField] private Transform rightHinge;
        [SerializeField] private float leftOpenAngle = 100f;
        [SerializeField] private float rightOpenAngle = -100f;

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

        public void ConfigureHinged(
            Transform leftDoorHinge,
            Transform rightDoorHinge,
            Collider passageBlocker,
            float leftAngle,
            float rightAngle,
            bool initiallyOpen)
        {
            closedVisual = null;
            openVisual = null;
            leftHinge = leftDoorHinge;
            rightHinge = rightDoorHinge;
            blockingCollider = passageBlocker;
            leftOpenAngle = leftAngle;
            rightOpenAngle = rightAngle;
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

            if (leftHinge != null)
                leftHinge.localRotation = Quaternion.Euler(
                    0f,
                    isOpen ? leftOpenAngle : 0f,
                    0f);
            if (rightHinge != null)
                rightHinge.localRotation = Quaternion.Euler(
                    0f,
                    isOpen ? rightOpenAngle : 0f,
                    0f);
        }
    }
}

using System;
using System.Collections;
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
        [SerializeField] private Vector3 leftOpenEuler;
        [SerializeField] private Vector3 rightOpenEuler;
        [SerializeField] private bool useOpenEuler;
        [SerializeField, Min(0f)] private float transitionDuration = 0.22f;

        private Coroutine transition;
        private Collider[] movingLeafColliders = Array.Empty<Collider>();

        public bool IsLeaf(Transform item) =>
            (leftHinge && item.IsChildOf(leftHinge)) || (rightHinge && item.IsChildOf(rightHinge));

        public bool IsOpen => isOpen;
        public bool IsTransitioning => transition != null;
        public Collider BlockingCollider => blockingCollider;

        public event Action<bool> StateChanged;

        private void OnEnable()
        {
            CacheMovingLeafColliders();
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
            CacheMovingLeafColliders();
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
            useOpenEuler = false;
            isOpen = initiallyOpen;
            CacheMovingLeafColliders();
            ApplyState();
        }

        public void ConfigureUpwardHinged(
            Transform hinge,
            Collider passageBlocker,
            Vector3 openEuler,
            bool initiallyOpen)
        {
            closedVisual = null;
            openVisual = null;
            leftHinge = hinge;
            rightHinge = null;
            blockingCollider = passageBlocker;
            leftOpenEuler = openEuler;
            rightOpenEuler = Vector3.zero;
            useOpenEuler = true;
            isOpen = initiallyOpen;
            CacheMovingLeafColliders();
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

            if (Application.isPlaying && transitionDuration > 0f && HasHingedLeaves)
            {
                if (transition != null)
                    StopCoroutine(transition);
                transition = StartCoroutine(AnimateHinges());
            }
            else
            {
                ApplyState();
            }

            if (changed && Application.isPlaying)
                StateChanged?.Invoke(isOpen);
        }

        private bool HasHingedLeaves => leftHinge != null || rightHinge != null;

        private IEnumerator AnimateHinges()
        {
            // A rotating solid leaf can wedge a CharacterController between the leaf and
            // its frame. The closed passage blocker supplies collision while shut; the
            // animated visual leaves stay non-solid for the whole transition/open state.
            SetPassageCollision(false);

            Quaternion leftStart = leftHinge != null ? leftHinge.localRotation : Quaternion.identity;
            Quaternion rightStart = rightHinge != null ? rightHinge.localRotation : Quaternion.identity;
            Quaternion leftTarget = Quaternion.Euler(isOpen
                ? (useOpenEuler ? leftOpenEuler : new Vector3(0f, leftOpenAngle, 0f))
                : Vector3.zero);
            Quaternion rightTarget = Quaternion.Euler(isOpen
                ? (useOpenEuler ? rightOpenEuler : new Vector3(0f, rightOpenAngle, 0f))
                : Vector3.zero);
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                t = t * t * (3f - 2f * t);
                if (leftHinge != null)
                    leftHinge.localRotation = Quaternion.Slerp(leftStart, leftTarget, t);
                if (rightHinge != null)
                    rightHinge.localRotation = Quaternion.Slerp(rightStart, rightTarget, t);
                yield return null;
            }

            if (leftHinge != null)
                leftHinge.localRotation = leftTarget;
            if (rightHinge != null)
                rightHinge.localRotation = rightTarget;
            if (!isOpen)
                SetPassageCollision(true);
            transition = null;
        }

        private void ApplyState()
        {
            if (closedVisual != null)
                closedVisual.SetActive(!isOpen);
            if (openVisual != null)
                openVisual.SetActive(isOpen);
            SetPassageCollision(!isOpen);

            if (leftHinge != null)
                leftHinge.localRotation = Quaternion.Euler(isOpen
                    ? (useOpenEuler ? leftOpenEuler : new Vector3(0f, leftOpenAngle, 0f))
                    : Vector3.zero);
            if (rightHinge != null)
                rightHinge.localRotation = Quaternion.Euler(isOpen
                    ? (useOpenEuler ? rightOpenEuler : new Vector3(0f, rightOpenAngle, 0f))
                    : Vector3.zero);
        }

        private void CacheMovingLeafColliders()
        {
            var colliders = new System.Collections.Generic.List<Collider>();
            CollectLeafColliders(leftHinge, colliders);
            CollectLeafColliders(rightHinge, colliders);
            movingLeafColliders = colliders.ToArray();
        }

        private void CollectLeafColliders(Transform hinge, System.Collections.Generic.List<Collider> results)
        {
            if (hinge == null)
                return;
            Collider[] found = hinge.GetComponentsInChildren<Collider>(true);
            for (int index = 0; index < found.Length; index++)
            {
                if (found[index] != blockingCollider && !results.Contains(found[index]))
                    results.Add(found[index]);
            }
        }

        private void SetPassageCollision(bool closed)
        {
            if (blockingCollider != null)
                blockingCollider.enabled = closed;
            for (int index = 0; index < movingLeafColliders.Length; index++)
            {
                if (movingLeafColliders[index] != null)
                    movingLeafColliders[index].enabled = false;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [DefaultExecutionOrder(-50)]
    public sealed class PrototypeCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField, Min(0.05f)] private float rotationDuration = 0.22f;
        [SerializeField, Range(0f, 90f)] private float cardinalYawOffset = 45f;

        private float startYaw;
        private float targetYaw;
        private float rotationElapsed;
        private bool rotating;

        public Transform FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        public float CurrentCardinalYaw => Mathf.Repeat(targetYaw, 360f);

        private void Awake()
        {
            targetYaw = SnapCardinalYaw(transform.eulerAngles.y);
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.wasPressedThisFrame)
                    RotateLeft();
                else if (keyboard.eKey.wasPressedThisFrame)
                    RotateRight();
            }

            UpdateRotation();
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                PrototypePlayerMover player = FindAnyObjectByType<PrototypePlayerMover>();
                if (player != null)
                    followTarget = player.transform;
            }
            if (followTarget == null)
                return;

            Vector3 desired = followTarget.position;
            desired.y = 0f;
            transform.position = desired;
        }

        public void RotateLeft()
        {
            BeginRotation(targetYaw - 90f);
        }

        public void RotateRight()
        {
            BeginRotation(targetYaw + 90f);
        }

        private void BeginRotation(float newTargetYaw)
        {
            startYaw = transform.eulerAngles.y;
            targetYaw = SnapCardinalYaw(newTargetYaw);
            rotationElapsed = 0f;
            rotating = true;
        }

        private float SnapCardinalYaw(float yaw)
        {
            return Mathf.Round((yaw - cardinalYawOffset) / 90f) * 90f +
                   cardinalYawOffset;
        }

        private void UpdateRotation()
        {
            if (!rotating)
                return;

            rotationElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(rotationElapsed / rotationDuration);
            float eased = t * t * (3f - 2f * t);
            float yaw = Mathf.LerpAngle(startYaw, targetYaw, eased);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (t < 1f)
                return;

            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            rotating = false;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [DefaultExecutionOrder(-50)]
    public sealed class PrototypeCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField, Min(0.05f)] private float rotationDuration = 0.22f;
        [SerializeField, Min(0f)] private float followSharpness = 18f;

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
            targetYaw = Mathf.Round(transform.eulerAngles.y / 90f) * 90f;
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
                return;

            Vector3 desired = followTarget.position;
            desired.y = 0f;
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
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
            targetYaw = Mathf.Round(newTargetYaw / 90f) * 90f;
            rotationElapsed = 0f;
            rotating = true;
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

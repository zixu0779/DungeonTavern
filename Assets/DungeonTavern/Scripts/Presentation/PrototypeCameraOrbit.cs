using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using DungeonTavern.Gameplay.Interaction;

namespace DungeonTavern.Prototypes.Rotation25D
{
    [DefaultExecutionOrder(-50)]
    public sealed class PrototypeCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;

        [SerializeField, Min(1f), Tooltip("Minimum orthographic camera distance; moving back preserves framing while preventing nearby tall walls from crossing the near plane.")]
        private float minimumCameraDistance = 40f;

        [SerializeField, Min(.05f)] private float rotationDuration = .22f;
        [SerializeField, Min(.05f), Tooltip("Seconds to pan between the player and a customer when pressing C.")]
        private float customerTransitionDuration = .65f;
        private bool transitioningFollow;
        private Vector3 followTransitionStart;
        private float followTransitionElapsed;
        private float startYaw, rotationElapsed;
        private bool rotating;
        private float targetYaw;
        private bool dialogueFraming;
        private bool entranceFraming;
        private bool entrancePanning, entranceReturning;
        public bool EntranceWasCanceled { get; private set; }
        private Quaternion preEntranceRotation;
        private float preEntranceSize, preEntranceYaw;
        public bool EntranceFraming => entranceFraming;
        private Transform dialogueLeft;
        private Transform dialogueRight;
        private float explorationSize;
        private Camera gameCamera;
        private Vector3 preDialoguePosition;
        private Quaternion preDialogueRotation;
        private float preDialogueSize;
        private float preDialogueTargetYaw;
        private float dialogueTargetYaw;
        private DialogueOcclusionFader occlusionFader;
        private float nextOcclusionCheck;
        private bool followingCustomer;
        private CustomerServicePoint followedCustomer;
        private Transform returnTarget;
        private Animator customerAnimator;
        private float seatedHold;

        public Transform FollowTarget
        {
            get => followTarget;
            set { StopCustomerFollow(); transitioningFollow = false; followTarget = value; }
        }

        public float CurrentCardinalYaw => Mathf.Repeat(targetYaw, 360f);

        private void Awake()
        {
            targetYaw = SnapYaw(transform.eulerAngles.y);
            transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            ApplyIsometricProjection();
            gameCamera = GetComponentInChildren<Camera>();
            occlusionFader = GetComponent<DialogueOcclusionFader>();
            if (occlusionFader == null)
                occlusionFader = gameObject.AddComponent<DialogueOcclusionFader>();
            if (gameCamera != null)
                explorationSize = gameCamera.orthographicSize;
        }

        public void ApplyIsometricProjection()
        {
            var camera = GetComponentInChildren<Camera>(true);
            if (camera == null || camera.transform.parent != transform)
                throw new System.InvalidOperationException("Isometric camera must be a direct child of the orbit rig.");
            var position = camera.transform.localPosition;
            var forward = camera.transform.localRotation * Vector3.forward;
            var target = Mathf.Abs(forward.y) > .0001f
                ? position + forward * (-position.y / forward.y) : Vector3.zero;
            float distance = Mathf.Max(minimumCameraDistance, Vector3.Distance(position, target));
            float pitch = 45f;
            camera.orthographic = true;
            camera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            camera.transform.localPosition = target - camera.transform.localRotation * Vector3.forward * distance;
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }

        private void Update()
        {
            if (DungeonTavern.Gameplay.Interaction.GamePauseMenu.IsPaused) return;
            if (entranceFraming)
            {
                var keys = Keyboard.current;
                if (keys != null && (keys.wKey.isPressed || keys.aKey.isPressed || keys.sKey.isPressed || keys.dKey.isPressed
                    || keys.qKey.isPressed || keys.eKey.isPressed)) RequestEntranceReturn();
                return;
            }
            Keyboard keyboard = Keyboard.current;
            if (dialogueFraming)
            {
                UpdateDialogueFraming();
                return;
            }
            if (Time.timeScale <= 0f) return;
            if (keyboard?.cKey.wasPressedThisFrame == true) ToggleCustomerFollow();
            UpdateCustomerFollow(Time.deltaTime);
            if (keyboard?.qKey.wasPressedThisFrame == true) RotateLeft();
            else if (keyboard?.eKey.wasPressedThisFrame == true) RotateRight();
            UpdateRotation();

        }

        private void LateUpdate()
        {
            if (dialogueFraming || entranceFraming)
                return;
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
            UpdateFollowPosition(desired, Time.deltaTime);
        }

        private void BeginFollowTransition()
        {
            followTransitionStart = transform.position;
            followTransitionElapsed = 0f;
            transitioningFollow = true;
        }

        private void UpdateFollowPosition(Vector3 desired, float seconds)
        {
            if (!transitioningFollow) { transform.position = desired; return; }
            followTransitionElapsed += seconds;
            float t = Mathf.Clamp01(followTransitionElapsed / Mathf.Max(.05f, customerTransitionDuration));
            transform.position = Vector3.Lerp(followTransitionStart, desired, t * t * (3f - 2f * t));
            if (t >= 1f) transitioningFollow = false;
        }

        public IEnumerator FrameEntrance(Transform view, float size, float seconds)
        {
            if (dialogueFraming) EndDialogueFraming();
            preEntranceRotation = Quaternion.Euler(0, targetYaw, 0);
            preEntranceYaw = targetYaw;
            preEntranceSize = gameCamera.orthographicSize;
            entranceFraming = true;
            EntranceWasCanceled = false;
            entranceReturning = false;
            rotating = false;
            entrancePanning = true;
            yield return PanTo(view.position, view.rotation, size, seconds);
            entrancePanning = false;
            if (EntranceWasCanceled) yield return ReturnFromEntrance(.35f);
        }

        public void RequestEntranceReturn()
        {
            if (!entranceFraming || EntranceWasCanceled || entranceReturning) return;
            EntranceWasCanceled = true;
            if (!entrancePanning) StartCoroutine(ReturnFromEntrance(.35f));
        }

        public IEnumerator ReturnFromEntrance(float seconds)
        {
            if (!entranceFraming) yield break;
            if (entranceReturning)
            {
                while (entranceFraming) yield return null;
                yield break;
            }
            entranceReturning = true;
            Vector3 target = followTarget == null ? transform.position : followTarget.position;
            target.y = 0;
            yield return PanTo(target, preEntranceRotation, preEntranceSize, seconds);
            CancelEntranceFraming();
        }

        public void CancelEntranceFraming()
        {
            if (!entranceFraming) return;
            entranceFraming = false;
            targetYaw = preEntranceYaw;
            transform.rotation = preEntranceRotation;
            gameCamera.orthographicSize = preEntranceSize;
            if (followTarget != null)
                transform.position = new Vector3(followTarget.position.x, 0, followTarget.position.z);
        }

        private IEnumerator PanTo(Vector3 position, Quaternion rotation, float size, float seconds)
        {
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            float startSize = gameCamera.orthographicSize;
            for (float elapsed = 0; elapsed < seconds; elapsed += Time.deltaTime)
            {
                if (EntranceWasCanceled && !entranceReturning) yield break;
                float t = Mathf.SmoothStep(0, 1, elapsed / seconds);
                transform.SetPositionAndRotation(Vector3.Lerp(startPosition, position, t), Quaternion.Slerp(startRotation, rotation, t));
                gameCamera.orthographicSize = Mathf.Lerp(startSize, size, t);
                yield return null;
            }
            transform.SetPositionAndRotation(position, rotation);
            gameCamera.orthographicSize = size;
        }

        public void RotateLeft()
        {
            RotateBy(-90f);
        }

        // Presentation-only shortcut: keep the same customer after they leave the queue.
        public void ToggleCustomerFollow()
        {
            if (followingCustomer) { StopCustomerFollow(); return; }
            if (dialogueFraming || entranceFraming) return;
            var queue = FindAnyObjectByType<ServiceOrderQueue>();
            BeginCustomerFollow(queue == null ? null : queue.FirstCustomer);
        }

        private void BeginCustomerFollow(CustomerServicePoint customer)
        {
            if (customer == null || !customer.isActiveAndEnabled || customer.State is not
                (CustomerOrderState.Entering or CustomerOrderState.QueueingForOrder
                or CustomerOrderState.Ordering or CustomerOrderState.ShowingOrder)) return;
            returnTarget = followTarget;
            followedCustomer = customer;
            customerAnimator = customer.GetComponentInChildren<Animator>();
            seatedHold = 0f;
            followingCustomer = true;
            followTarget = customer.transform;
            BeginFollowTransition();
        }

        private void UpdateCustomerFollow(float seconds)
        {
            if (!followingCustomer) return;
            if (followedCustomer == null || !followedCustomer.isActiveAndEnabled ||
                followedCustomer.State is CustomerOrderState.Inactive or CustomerOrderState.Leaving or CustomerOrderState.Finished)
            { StopCustomerFollow(); return; }
            if (followedCustomer.State is not (CustomerOrderState.WaitingForFood
                or CustomerOrderState.Eating or CustomerOrderState.AwaitingSettlement)) return;
            // WaitingForFood starts before SitDown finishes. Wait for the visible seated pose.
            bool seated = customerAnimator == null || !customerAnimator.isActiveAndEnabled ||
                customerAnimator.runtimeAnimatorController == null || followedCustomer.AssignedSeat?.IsStanding == true ||
                (!customerAnimator.IsInTransition(0) && customerAnimator.GetCurrentAnimatorStateInfo(0).IsName("SeatedIdle"));
            if (!seated) { seatedHold = 0f; return; }
            seatedHold += seconds;
            if (seatedHold >= 1f) StopCustomerFollow();
        }

        private void StopCustomerFollow()
        {
            if (!followingCustomer) return;
            followTarget = returnTarget;
            BeginFollowTransition();
            followingCustomer = false;
            followedCustomer = null;
            returnTarget = null;
            customerAnimator = null;
            seatedHold = 0f;
        }

        private void OnDisable()
        {
            StopCustomerFollow();
            transitioningFollow = false;
        }

        public void RotateRight()
        {
            RotateBy(90f);
        }

        public void BeginDialogueFraming(Transform leftCharacter, Transform rightCharacter)
        {
            if (leftCharacter == null || rightCharacter == null)
                return;
            StopCustomerFollow();
            transitioningFollow = false;
            if (!dialogueFraming)
            {
                preDialoguePosition = transform.position;
                preDialogueRotation = transform.rotation;
                preDialogueTargetYaw = targetYaw;
                preDialogueSize = gameCamera != null ? gameCamera.orthographicSize : explorationSize;
            }
            rotating = false;
            dialogueLeft = leftCharacter;
            dialogueRight = rightCharacter;
            dialogueTargetYaw = ChooseDialogueYaw(leftCharacter, rightCharacter);
            nextOcclusionCheck = 0f;
            dialogueFraming = true;
        }

        public void EndDialogueFraming()
        {
            if (!dialogueFraming)
                return;

            dialogueFraming = false;
            dialogueLeft = null;
            dialogueRight = null;
            if (gameCamera != null)
                gameCamera.orthographicSize = preDialogueSize;
            transform.SetPositionAndRotation(preDialoguePosition, preDialogueRotation);
            targetYaw = preDialogueTargetYaw;
            occlusionFader?.RestoreAll();
        }

        private void UpdateDialogueFraming()
        {
            if (dialogueLeft == null || dialogueRight == null)
            {
                EndDialogueFraming();
                return;
            }

            Vector3 midpoint = (dialogueLeft.position + dialogueRight.position) * 0.5f;
            midpoint.y = 0f;
            transform.position = Vector3.Lerp(transform.position, midpoint, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(0f, dialogueTargetYaw, 0f),
                1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

            if (gameCamera != null)
                gameCamera.orthographicSize = Mathf.Lerp(gameCamera.orthographicSize, 3.25f, 1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));

            if (Time.unscaledTime >= nextOcclusionCheck)
            {
                nextOcclusionCheck = Time.unscaledTime + 0.12f;
                UpdateDialogueOccluders();
            }
        }

        private float ChooseDialogueYaw(Transform leftCharacter, Transform rightCharacter)
        {
            Vector3 leftToRight = rightCharacter.position - leftCharacter.position;
            leftToRight.y = 0f;
            float preferred = leftToRight.sqrMagnitude > 0.01f
                ? Mathf.Atan2(leftToRight.x, leftToRight.z) * Mathf.Rad2Deg - 90f
                : transform.eulerAngles.y;
            float[] headings = { 45f, 135f, 225f, 315f };
            float bestYaw = preferred;
            float bestScore = float.PositiveInfinity;
            for (int index = 0; index < headings.Length; index++)
            {
                float candidate = headings[index];
                int blockers = CountCandidateBlockers(candidate, leftCharacter, rightCharacter);
                float score = blockers * 1000f + Mathf.Abs(Mathf.DeltaAngle(preferred, candidate));
                if (score < bestScore)
                {
                    bestScore = score;
                    bestYaw = candidate;
                }
            }
            return bestYaw;
        }

        private int CountCandidateBlockers(float yaw, Transform leftCharacter, Transform rightCharacter)
        {
            if (gameCamera == null)
                return 0;
            Vector3 midpoint = (leftCharacter.position + rightCharacter.position) * 0.5f;
            midpoint.y = 0f;
            Vector3 cameraLocal = transform.InverseTransformPoint(gameCamera.transform.position);
            Vector3 cameraPosition = Matrix4x4.TRS(midpoint, Quaternion.Euler(0f, yaw, 0f), transform.lossyScale)
                .MultiplyPoint3x4(cameraLocal);
            return CountBlockers(cameraPosition, GetLookPoint(leftCharacter), leftCharacter, rightCharacter)
                + CountBlockers(cameraPosition, GetLookPoint(rightCharacter), leftCharacter, rightCharacter);
        }

        private static int CountBlockers(Vector3 origin, Vector3 target, Transform leftCharacter, Transform rightCharacter)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
                return 0;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            int blockers = 0;
            for (int index = 0; index < hits.Length; index++)
            {
                Transform hit = hits[index].collider.transform;
                if (!hit.IsChildOf(leftCharacter) && !hit.IsChildOf(rightCharacter))
                    blockers++;
            }
            return blockers;
        }

        private void UpdateDialogueOccluders()
        {
            if (gameCamera == null || dialogueLeft == null || dialogueRight == null)
                return;
            var renderers = new HashSet<Renderer>();
            CollectOccluders(gameCamera.transform.position, GetLookPoint(dialogueLeft), renderers);
            CollectOccluders(gameCamera.transform.position, GetLookPoint(dialogueRight), renderers);
            occlusionFader?.SetOccluders(renderers);
        }

        private void CollectOccluders(Vector3 origin, Vector3 target, HashSet<Renderer> results)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
                return;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int index = 0; index < hits.Length; index++)
            {
                Transform hit = hits[index].collider.transform;
                if (hit.IsChildOf(dialogueLeft) || hit.IsChildOf(dialogueRight) || hit.IsChildOf(transform))
                    continue;
                Renderer renderer = hit.GetComponentInParent<Renderer>() ?? hit.GetComponentInChildren<Renderer>();
                if (renderer != null)
                    results.Add(renderer);
            }
        }

        private static Vector3 GetLookPoint(Transform character)
        {
            Renderer renderer = character.GetComponentInChildren<Renderer>();
            if (renderer != null)
                return new Vector3(renderer.bounds.center.x, renderer.bounds.max.y * 0.75f + renderer.bounds.center.y * 0.25f, renderer.bounds.center.z);
            return character.position + Vector3.up * 1.2f;
        }

        private static float SnapYaw(float yaw) => Mathf.Repeat(45f + Mathf.Round((yaw - 45f) / 90f) * 90f, 360f);

        public void RotateBy(float deltaDegrees)
        {
            if (dialogueFraming || entranceFraming) return;
            startYaw = transform.eulerAngles.y;
            targetYaw = SnapYaw(targetYaw + deltaDegrees);
            rotationElapsed = 0f;
            rotating = true;
        }
        private void UpdateRotation()
        {
            if (!rotating) return;
            rotationElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(rotationElapsed / rotationDuration);
            float eased = t * t * (3f - 2f * t);
            transform.rotation = Quaternion.Euler(0, Mathf.LerpAngle(startYaw, targetYaw, eased), 0);
            if (t >= 1f) rotating = false;
        }
    }
}

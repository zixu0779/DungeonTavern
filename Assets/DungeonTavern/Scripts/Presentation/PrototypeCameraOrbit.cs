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
        private float dialogueAge, entranceDuration=1.3f;
        private const float DialogueDuration=1.3f;
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
            if (DungeonTavern.Gameplay.Interaction.GamePauseMenu.IsPaused || DungeonTavern.UI.TavernUI.WindowOpen) return;
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
            entranceDuration=seconds;
            entranceFraming = true;
            EntranceWasCanceled = false;
            entranceReturning = false;
            rotating = false;
            entrancePanning = true;
            yield return PanTo(view.position, view.rotation, size, seconds);
            entrancePanning = false;
            if (EntranceWasCanceled) yield return ReturnFromEntrance(entranceDuration);
        }

        public void RequestEntranceReturn()
        {
            if (!entranceFraming || EntranceWasCanceled || entranceReturning) return;
            EntranceWasCanceled = true;
            if (!entrancePanning) StartCoroutine(ReturnFromEntrance(entranceDuration));
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
                if(entranceReturning&&followTarget)position=new Vector3(followTarget.position.x,0,followTarget.position.z);
                float t = Mathf.SmoothStep(0, 1, elapsed / seconds);
                transform.SetPositionAndRotation(Vector3.Lerp(startPosition, position, t), Quaternion.Slerp(startRotation, rotation, t));
                gameCamera.orthographicSize = Mathf.Lerp(startSize, size, t);
                yield return null;
            }
            if(entranceReturning&&followTarget)position=new Vector3(followTarget.position.x,0,followTarget.position.z);
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
            dialogueAge=0;
            occlusionFader?.RestoreAll();
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
            dialogueAge+=Time.deltaTime;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(dialogueAge/DialogueDuration));
            transform.position=Vector3.Lerp(preDialoguePosition,midpoint,t);
            transform.rotation=Quaternion.Slerp(preDialogueRotation,Quaternion.Euler(0,dialogueTargetYaw,0),t);
            if(gameCamera)gameCamera.orthographicSize=Mathf.Lerp(preDialogueSize,3.25f,t);
        }

        private float ChooseDialogueYaw(Transform leftCharacter, Transform rightCharacter)
        {
            Vector3 leftToRight = rightCharacter.position - leftCharacter.position;
            leftToRight.y = 0f;
            float preferred = leftToRight.sqrMagnitude > 0.01f
                ? Mathf.Atan2(leftToRight.x, leftToRight.z) * Mathf.Rad2Deg - 90f
                : transform.eulerAngles.y;
            float opposite=Mathf.Repeat(preferred+180,360);
            float current=transform.eulerAngles.y;
            float near=Mathf.Abs(Mathf.DeltaAngle(current,preferred))<=Mathf.Abs(Mathf.DeltaAngle(current,opposite))?preferred:opposite;
            float far=Mathf.Repeat(near+180,360);
            // Only the two perpendicular views qualify. Compare all 18 sight lines; ties favour the shorter turn.
            int nearBlocked = CountCandidateBlockers(near, leftCharacter, rightCharacter);
            int farBlocked = CountCandidateBlockers(far, leftCharacter, rightCharacter);
            return farBlocked < nearBlocked ? far : near;
        }

        private int CountCandidateBlockers(float yaw, Transform leftCharacter, Transform rightCharacter)
        {
            if(gameCamera==null)return 0;
            Vector3 midpoint=(leftCharacter.position+rightCharacter.position)*.5f;midpoint.y=0;
            Quaternion rotation=Quaternion.Euler(0,yaw,0);
            Vector3 cameraPosition=midpoint+rotation*Vector3.Scale(gameCamera.transform.localPosition,transform.lossyScale);
            Vector3 forward=rotation*gameCamera.transform.localRotation*Vector3.forward;
            return CharacterBlocked(cameraPosition,forward,leftCharacter,leftCharacter,rightCharacter)
                +CharacterBlocked(cameraPosition,forward,rightCharacter,leftCharacter,rightCharacter);
        }
        private static int CharacterBlocked(Vector3 cameraPosition,Vector3 forward,Transform character,Transform left,Transform right)
        {
            // Use the complete visible model, not whichever renderer happens to be first.
            Bounds bounds = new Bounds(character.position + Vector3.up, new Vector3(.5f, 2f, .5f));
            bool found = false;
            foreach (var renderer in character.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer)
                    continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            Vector3 screenRight = Vector3.Cross(Vector3.up, forward).normalized;
            float halfWidth = Mathf.Abs(screenRight.x) * bounds.extents.x + Mathf.Abs(screenRight.z) * bounds.extents.z;
            // Count occluded samples, not individual colliders: multiple walls on one ray count once.
            int blocked = 0;
            for (int row = 0; row < 3; row++)
            for (int column = -1; column <= 1; column++)
            {
                Vector3 point = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * (.35f + row * .25f), bounds.center.z)
                    + screenRight * (halfWidth * .65f * column);
                float distance = Vector3.Dot(point - cameraPosition, forward);
                if (distance <= 0) continue;
                Vector3 origin = point - forward * distance;
                foreach (var hit in Physics.RaycastAll(origin, forward, distance, ~0, QueryTriggerInteraction.Ignore))
                {
                    var collider = hit.collider;
                    var t = collider.transform;
                    if (t.IsChildOf(left) || t.IsChildOf(right) || collider.GetComponentInParent<CharacterController>()
                        || collider.GetComponentInParent<UnityEngine.AI.NavMeshAgent>()) continue;
                    if (collider.attachedRigidbody && !collider.attachedRigidbody.isKinematic) continue;
                    // Collision proxies can be siblings of the rendered wall; do not require a renderer here.
                    blocked++;
                    break;
                }
            }
            return blocked;
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

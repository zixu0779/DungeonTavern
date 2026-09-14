using System;
using System.Collections;
using UnityEngine.AI;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class Day1EveActor : InteractionPoint
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.25f;
        [SerializeField, Min(0.5f)] private float conversationRange = 1.35f;
        [SerializeField, Min(0.5f)] private float minimumSpacing = 0.65f;
        [SerializeField, Min(0.05f)] private float conversationClearanceRadius = 0.2f;
        [SerializeField, Min(0.1f)] private float sightLowerHeight = 0.35f;
        [SerializeField, Min(0.2f)] private float sightUpperHeight = 1.15f;
        [SerializeField] private LayerMask conversationBlockers = ~0;

        private WorldSpeechBubble bubble;
        private Transform[] counterGroups;
        private NpcApproachSpeech approachSpeech;
        private Transform player;
        [SerializeField] private Transform sceneEntranceWaitPoint;
        private Vector3 entranceWaitPosition;
        private CharacterController movementController;
        private NpcNavigator navigator;
        private bool arriving;
        private bool conversationStarted;
        private bool playerInTavernArea = true;
        private bool guidingToOpeningSwitch;
        private bool openingGuidanceReady;
        private Transform openingGuidePoint;
        private string openingGuidanceLine;
        private bool turningToOpeningSwitch;
        private const float TriggerRange = 4.2f;
        public const float ApproachSeconds = .5f;

        public bool IsOpeningGuidanceReady => openingGuidanceReady;

        public event Action ConversationRequested;

        public void Configure(Transform destination)
        {
            sceneEntranceWaitPoint = destination;
            if (destination != null)
                entranceWaitPosition = destination.position;
        }

        private void Awake()
        {
            // In the Bar prefab, vault markers and imported mesh colliders are siblings.
            // Treat that furniture group consistently, without exempting neighbouring wall groups.
            counterGroups=Array.ConvertAll(FindObjectsByType<CounterVaultObstacle>(FindObjectsInactive.Include),
                marker=>marker.transform.parent?marker.transform.parent:marker.transform);
            PrototypePlayerMover playerMover = FindAnyObjectByType<PrototypePlayerMover>();
            float playerSpeed = playerMover == null ? 3.25f : playerMover.MoveSpeed;
            moveSpeed = playerSpeed * 1.08f;
            bubble = GetComponent<WorldSpeechBubble>();
            approachSpeech = GetComponent<NpcApproachSpeech>();
            if (approachSpeech == null)
                approachSpeech = gameObject.AddComponent<NpcApproachSpeech>();
            if (sceneEntranceWaitPoint == null)
                sceneEntranceWaitPoint = GameObject.Find("EveDay1Conversation")?.transform;
            entranceWaitPosition = sceneEntranceWaitPoint != null
                ? sceneEntranceWaitPoint.position
                : transform.position;
            movementController = GetComponent<CharacterController>();
            if (movementController == null)
                movementController = gameObject.AddComponent<CharacterController>();
            movementController.radius = 0.28f;
            movementController.height = 1.5f;
            movementController.center = new Vector3(0f, 0.75f, 0f);
            movementController.stepOffset = 0.25f;
            navigator = GetComponent<NpcNavigator>();
            if (navigator == null)
                navigator = gameObject.AddComponent<NpcNavigator>();
            navigator.Configure(moveSpeed, minimumSpacing);
            if (GetComponent<DoorPassageAgent>() == null)
                gameObject.AddComponent<DoorPassageAgent>();

            foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
            {
                sprite.sortingOrder = Mathf.Max(sprite.sortingOrder, 100);
                sprite.rendererPriority = Mathf.Max(sprite.rendererPriority, 100);
                Vector3 local = sprite.transform.localPosition;
                sprite.transform.localPosition = new Vector3(local.x, Mathf.Max(local.y, 0.01f), local.z);
            }
            gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            PlayerAreaTransition.Started += OnPlayerAreaTransitionStarted;
            PlayerAreaTransition.Completed += OnPlayerAreaTransitionCompleted;
        }

        protected override void OnDisable()
        {
            PlayerAreaTransition.Started -= OnPlayerAreaTransitionStarted;
            PlayerAreaTransition.Completed -= OnPlayerAreaTransitionCompleted;
            base.OnDisable();
        }

        public void BeginArrival()
        {
            gameObject.SetActive(true);
            arriving = true;
            conversationStarted = false;
            player = FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.PrototypePlayerMover>()?.transform;
            approachSpeech.BeginApproach("你终于回来了。");
        }

        private void Update()
        {
            if (guidingToOpeningSwitch)
            {
                UpdateOpeningSwitchGuidance();
                return;
            }
            if (!arriving)
                return;

            player ??= FindAnyObjectByType<DungeonTavern.Prototypes.Rotation25D.PrototypePlayerMover>()?.transform;
            if (player == null)
                return;

            if (!playerInTavernArea)
            {
                navigator.MoveTo(sceneEntranceWaitPoint != null
                    ? sceneEntranceWaitPoint.position
                    : entranceWaitPosition, minimumSpacing);
                return;
            }

            navigator.MoveTo(player.position, minimumSpacing);
            if (CanStartConversation())
            {
                arriving = false;
                navigator.Stop(true);
                StartCoroutine(SettleBeforeConversation());
            }
        }

        private void OnPlayerAreaTransitionCompleted(string loadedScene, string unloadedScene)
        {
            if (!arriving)
                return;

            if (!string.IsNullOrWhiteSpace(loadedScene)
                && !string.Equals(loadedScene, "Tavern_Main", StringComparison.Ordinal))
            {
                playerInTavernArea = false;
            }
            else if (!string.IsNullOrWhiteSpace(unloadedScene)
                     || string.Equals(loadedScene, "Tavern_Main", StringComparison.Ordinal))
            {
                playerInTavernArea = true;
            }
        }

        private void OnPlayerAreaTransitionStarted(string loadedScene, string unloadedScene)
        {
            if (!arriving)
                return;
            if (!string.IsNullOrWhiteSpace(loadedScene)
                && !string.Equals(loadedScene, "Tavern_Main", StringComparison.Ordinal))
            {
                playerInTavernArea = false;
            }
        }

        private bool CanStartConversation()
        {
            return playerInTavernArea && Vector3.Distance(player.position,transform.position)<=TriggerRange
                && HasClearConversationLineOfSight();
        }

        private bool HasClearConversationLineOfSight()
        {
            var delta=player.position-transform.position;delta.y=0;
            foreach(var hit in Physics.CapsuleCastAll(transform.position+Vector3.up*sightLowerHeight,
                transform.position+Vector3.up*sightUpperHeight,conversationClearanceRadius,delta.normalized,delta.magnitude,conversationBlockers,QueryTriggerInteraction.Ignore))
            {
                var t=hit.transform;
                if(t.IsChildOf(transform)||t.IsChildOf(player)||t.GetComponentInParent<CharacterController>())continue;
                if(t.GetComponentInParent<CounterVaultObstacle>() || Array.Exists(counterGroups,group=>group&&t.IsChildOf(group)))continue;
                return false;
            }
            return true;
        }

        private IEnumerator SettleBeforeConversation()
        {
            var mover=player.GetComponent<PrototypePlayerMover>();
            bool wasEnabled=mover.MovementInputEnabled;mover.MovementInputEnabled=false;
            var start=transform.position;var end=start;var delta=player.position-start;delta.y=0;
            float distance=delta.magnitude;
            // A straight, short approach cannot route around the counter or start by moving away.
            float length=Mathf.Min(moveSpeed*ApproachSeconds,Mathf.Max(0,distance-conversationRange));
            if(length>.05f && NavMesh.SamplePosition(start,out var source,.5f,NavMesh.AllAreas))
            {
                end=start+delta.normalized*length;
                if(NavMesh.Raycast(source.position,end,out var boundary,NavMesh.AllAreas))
                    end=boundary.position-delta.normalized*.15f;
                if(!NavMesh.SamplePosition(end,out var sample,.25f,NavMesh.AllAreas))end=start;
                else end=sample.position;
                var path=new NavMeshPath();
                if(!NavMesh.CalculatePath(source.position,end,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)end=start;
                else foreach(var corner in path.corners)
                    if(Vector3.ProjectOnPlane(corner-player.position,Vector3.up).magnitude>distance+.02f) {end=start;break;}
            }
            float elapsed=0;
            while(elapsed<ApproachSeconds && Vector3.Distance(transform.position,end)>.12f)
            {
                elapsed+=Time.deltaTime;
                if(Vector3.ProjectOnPlane(transform.position-player.position,Vector3.up).magnitude>distance+.02f)break;
                navigator.MoveTo(end,.1f);
                yield return null;
            }
            navigator.Stop(true);
            if(!HasClearConversationLineOfSight())
            {mover.MovementInputEnabled=wasEnabled;arriving=true;yield break;}
            StartConversation();
        }

        public void ShowBubble(string text)
        {
            bubble?.Show(text);
        }

        public void BeginOpeningSwitchGuidance(Transform guidePoint, string text)
        {
            turningToOpeningSwitch = false;
            openingGuidePoint = guidePoint;
            openingGuidanceLine = text;
            openingGuidanceReady = false;
            guidingToOpeningSwitch = guidePoint != null;
            bubble?.Hide();
            if (guidePoint == null)
            {
                openingGuidanceReady = true;
                bubble?.Show(text);
            }
        }

        public void CompleteOpeningSwitchGuidance()
        {
            guidingToOpeningSwitch = false;
            openingGuidanceReady = false;
            openingGuidePoint = null;
            openingGuidanceLine = string.Empty;
            bubble?.Hide();
            navigator.Stop(true);
        }

        private void UpdateOpeningSwitchGuidance()
        {
            if (openingGuidePoint == null)
                return;
            if (!turningToOpeningSwitch)
            {
                navigator.MoveTo(openingGuidePoint.position, 0.15f);
                if (!navigator.HasArrived(0.2f)) return;
                navigator.Stop(true);
                turningToOpeningSwitch = true;
            }
            var lever = FindAnyObjectByType<FloorLeverPoint>();
            var direction = lever != null ? lever.transform.position - transform.position : openingGuidePoint.forward;
            direction.y = 0;
            if (direction.sqrMagnitude > .001f)
            {
                var target = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 180f * Time.deltaTime);
                if (Quaternion.Angle(transform.rotation, target) > 1f) return;
            }
            guidingToOpeningSwitch = false;
            openingGuidanceReady = true;
            navigator.Stop(true);
            bubble?.Show(openingGuidanceLine);
        }

        public override string GetPrompt(PlayerHands hands)
        {
            return string.Empty;
        }

        public override bool Interact(PlayerHands hands)
        {
            return false;
        }

        private void StartConversation()
        {
            if (conversationStarted)
                return;
            conversationStarted = true;
            approachSpeech.BeginDialogue();
            ConversationRequested?.Invoke();
        }
    }
}

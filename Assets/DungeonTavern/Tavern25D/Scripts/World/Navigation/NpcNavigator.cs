using UnityEngine;
using UnityEngine.AI;

namespace DungeonTavern.Tavern25D
{
    [DisallowMultipleComponent]
    public sealed class NpcNavigator : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;
        [SerializeField, Min(0.01f)] private float destinationMoveThreshold = 0.2f;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 1.5f;

        private NavMeshAgent agent;
        private Vector3 lastDestination;
        private float nextRepathTime;
        private bool hasDestination;

        public bool HasCompletePath => agent != null
            && agent.enabled
            && agent.isOnNavMesh
            && !agent.pathPending
            && agent.pathStatus == NavMeshPathStatus.PathComplete;

        public float RemainingDistance => HasCompletePath
            ? agent.remainingDistance
            : float.PositiveInfinity;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = gameObject.AddComponent<NavMeshAgent>();
            ConfigurePhysicsBody();
        }

        public void Configure(float speed, float stoppingDistance)
        {
            EnsureAgent();
            agent.speed = Mathf.Max(0.1f, speed);
            agent.acceleration = Mathf.Max(24f, agent.speed * 8f);
            agent.angularSpeed = 720f;
            agent.radius = 0.28f;
            agent.height = 1.5f;
            agent.baseOffset = 0f;
            agent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
            agent.autoRepath = true;
            agent.autoBraking = true;
            ConfigurePhysicsBody();
        }

        public bool MoveTo(Vector3 worldDestination, float stoppingDistance)
        {
            EnsureAgent();
            if (!EnsureOnNavMesh())
                return false;

            agent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
            bool destinationChanged = !hasDestination
                || (worldDestination - lastDestination).sqrMagnitude
                >= destinationMoveThreshold * destinationMoveThreshold;
            if (destinationChanged || Time.time >= nextRepathTime)
            {
                if (!NavMesh.SamplePosition(worldDestination, out NavMeshHit hit, navMeshSampleRadius, agent.areaMask))
                {
                    Stop(true);
                    return false;
                }

                hasDestination = agent.SetDestination(hit.position);
                if (!hasDestination)
                    return false;
                lastDestination = worldDestination;
                nextRepathTime = Time.time + repathInterval;
            }

            agent.isStopped = false;
            return true;
        }

        public bool HasArrived(float tolerance)
        {
            return HasCompletePath
                && RemainingDistance <= Mathf.Max(tolerance, agent.stoppingDistance)
                && (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.01f);
        }

        public void Stop(bool clearPath = false)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;
            agent.isStopped = true;
            if (clearPath)
            {
                agent.ResetPath();
                hasDestination = false;
            }
        }

        private void EnsureAgent()
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>() ?? gameObject.AddComponent<NavMeshAgent>();
        }

        private bool EnsureOnNavMesh()
        {
            if (agent.enabled && agent.isOnNavMesh)
                return true;
            if (!agent.enabled
                || !NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, agent.areaMask))
                return false;
            return agent.Warp(hit.position);
        }

        private void ConfigurePhysicsBody()
        {
            CharacterController legacyController = GetComponent<CharacterController>();
            if (legacyController != null)
                legacyController.enabled = false;

            CapsuleCollider bodyCollider = GetComponent<CapsuleCollider>();
            if (bodyCollider == null)
                bodyCollider = gameObject.AddComponent<CapsuleCollider>();
            bodyCollider.radius = 0.28f;
            bodyCollider.height = 1.5f;
            bodyCollider.center = new Vector3(0f, 0.75f, 0f);
            bodyCollider.direction = 1;
            bodyCollider.isTrigger = false;

            Rigidbody physicsBody = GetComponent<Rigidbody>();
            if (physicsBody == null)
                physicsBody = gameObject.AddComponent<Rigidbody>();
            physicsBody.isKinematic = true;
            physicsBody.useGravity = false;
            physicsBody.interpolation = RigidbodyInterpolation.Interpolate;
            physicsBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }
}

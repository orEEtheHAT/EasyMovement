using UnityEngine;
using UnityEngine.AI;

namespace EasyFootsteps
{
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshFootstepSource : MonoBehaviour, IFootstepMotionSource
    {
        [Tooltip("Layers used to find the surface under the agent.")]
        [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;
        [Tooltip("How far below the agent pivot to look for a surface.")]
        [SerializeField, Min(0f)] private float groundProbeDepth = 2f;
        // Keep the serialized v1.0 selection when a named state has never been assigned.
        [SerializeField, HideInInspector] private CharacterStepController.MovementMode mode;
        [SerializeField] private string stateId;
        private NavMeshAgent agent;

        public string StateId
        {
            get => string.IsNullOrWhiteSpace(stateId) ? FootstepState.IdFor(mode) : stateId;
            set => stateId = value?.Trim();
        }

        public CharacterStepController.MovementMode Mode
        {
            get => FootstepState.LegacyMode(StateId);
            set
            {
                mode = value;
                StateId = FootstepState.IdFor(value);
            }
        }

        private void Awake() => agent = GetComponent<NavMeshAgent>();

        public Collider FindGround() => FootstepGround.Probe(transform.position, transform.up, groundLayers, groundProbeDepth);

        public bool TryGetGroundedMotion(out FootstepMotion motion)
        {
            motion = default;
            if (!agent || !agent.isActiveAndEnabled || !agent.isOnNavMesh || agent.isOnOffMeshLink)
                return false;

            float speed = Vector3.ProjectOnPlane(agent.velocity, transform.up).magnitude;
            motion = new FootstepMotion(speed, agent.speed, StateId, FindGround());
            return true;
        }
    }
}

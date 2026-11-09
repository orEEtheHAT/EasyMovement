using UnityEngine;
using UnityEngine.AI;

namespace EasyFootsteps
{
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshFootstepSource : FootstepMotionSource
    {
        [SerializeField] private CharacterStepController.MovementMode mode;
        private NavMeshAgent agent;

        public CharacterStepController.MovementMode Mode { get => mode; set => mode = value; }

        private void Awake() => agent = GetComponent<NavMeshAgent>();

        public override bool TryGetGroundedMotion(out FootstepMotion motion)
        {
            motion = default;
            if (!agent || !agent.isActiveAndEnabled || !agent.isOnNavMesh || agent.isOnOffMeshLink)
                return false;

            float speed = Vector3.ProjectOnPlane(agent.velocity, transform.up).magnitude;
            motion = new FootstepMotion(speed, agent.speed, mode, FindGround());
            return true;
        }
    }
}

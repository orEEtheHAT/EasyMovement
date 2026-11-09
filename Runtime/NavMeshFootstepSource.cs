using UnityEngine;
using UnityEngine.AI;

namespace EasyFootsteps
{
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public sealed class NavMeshFootstepSource : SelectableFootstepMotionSource
    {
        private NavMeshAgent agent;

        private void Awake() => agent = GetComponent<NavMeshAgent>();

        public override bool TryGetGroundedMotion(out FootstepMotion motion)
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

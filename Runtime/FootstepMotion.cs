using UnityEngine;

namespace EasyFootsteps
{
    /// <summary>One sample of grounded movement for the step cycle.</summary>
    public readonly struct FootstepMotion
    {
        public readonly float PlanarSpeed;
        public readonly float MaxSpeed;
        public readonly CharacterStepController.MovementMode Mode;
        public readonly Collider Ground;
        public readonly string StateId;

        public FootstepMotion(float planarSpeed, float maxSpeed, CharacterStepController.MovementMode mode, Collider ground)
        {
            PlanarSpeed = planarSpeed;
            MaxSpeed = maxSpeed;
            StateId = FootstepState.IdFor(mode);
            Mode = mode;
            Ground = ground;
        }

        public FootstepMotion(float planarSpeed, float maxSpeed, string stateId, Collider ground)
            : this(planarSpeed, maxSpeed, FootstepState.LegacyMode(stateId), ground)
        {
            StateId = stateId;
        }
    }

    public interface IFootstepMotionSource
    {
        bool TryGetGroundedMotion(out FootstepMotion motion);
        Collider FindGround();
    }

    public static class FootstepGround
    {
        public static Collider Probe(Vector3 pivot, Vector3 up, LayerMask layers, float depth)
        {
            Vector3 origin = pivot + up * 0.35f;
            return Physics.Raycast(origin, -up, out RaycastHit hit, depth + 0.35f,
                layers, QueryTriggerInteraction.Ignore) ? hit.collider : null;
        }
    }
}

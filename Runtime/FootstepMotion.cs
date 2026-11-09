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
            : this(planarSpeed, maxSpeed, FootstepState.IdFor(mode), ground) => Mode = mode;

        public FootstepMotion(float planarSpeed, float maxSpeed, string stateId, Collider ground)
        {
            PlanarSpeed = planarSpeed;
            MaxSpeed = maxSpeed;
            StateId = stateId;
            Mode = FootstepState.LegacyMode(stateId);
            Ground = ground;
        }
    }

    public interface IFootstepMotionSource
    {
        bool TryGetGroundedMotion(out FootstepMotion motion);
        Collider FindGround();
    }

    /// <summary>For sources that need a physics query to identify the surface at foot contact.</summary>
    public interface IDeferredFootstepGround : IFootstepMotionSource
    {
        bool TryGetMotionWithoutGround(out FootstepMotion motion);
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

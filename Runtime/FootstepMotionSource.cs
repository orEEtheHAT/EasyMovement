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

    public abstract class FootstepMotionSource : MonoBehaviour
    {
        [Tooltip("Layers used to find the surface under the character.")]
        [SerializeField] protected LayerMask groundLayers = Physics.DefaultRaycastLayers;
        [Tooltip("How far below the character pivot to look for a surface.")]
        [SerializeField, Min(0f)] private float groundProbeDepth = 2f;

        public abstract bool TryGetGroundedMotion(out FootstepMotion motion);

        public virtual Collider FindGround()
        {
            return ProbeGround(transform.up, groundLayers);
        }

        protected Collider ProbeGround(Vector3 up, LayerMask layers)
        {
            // Starting inside the character avoids hitting its own capsule.
            Vector3 origin = transform.position + up * 0.35f;
            return Physics.Raycast(origin, -up, out RaycastHit hit, groundProbeDepth + 0.35f,
                layers, QueryTriggerInteraction.Ignore) ? hit.collider : null;
        }
    }
}

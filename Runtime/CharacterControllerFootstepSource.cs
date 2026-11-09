using UnityEngine;

namespace EasyFootsteps
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class CharacterControllerFootstepSource : SelectableFootstepMotionSource
    {
        [SerializeField, Min(0.01f)] private float referenceSpeed = 5f;
        private CharacterController controller;

        private void Awake() => controller = GetComponent<CharacterController>();

        public override bool TryGetGroundedMotion(out FootstepMotion motion)
        {
            motion = default;
            if (!controller || !controller.enabled || !controller.isGrounded)
                return false;

            float speed = Vector3.ProjectOnPlane(controller.velocity, transform.up).magnitude;
            motion = new FootstepMotion(speed, referenceSpeed, StateId, FindGround());
            return true;
        }
    }
}

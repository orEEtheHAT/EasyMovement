using UnityEngine;

namespace EasyFootsteps
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class CharacterControllerFootstepSource : FootstepMotionSource
    {
        [SerializeField, Min(0.01f)] private float referenceSpeed = 5f;
        [SerializeField] private CharacterStepController.MovementMode mode;
        private CharacterController controller;

        public CharacterStepController.MovementMode Mode { get => mode; set => mode = value; }

        private void Awake() => controller = GetComponent<CharacterController>();

        public override bool TryGetGroundedMotion(out FootstepMotion motion)
        {
            motion = default;
            if (!controller || !controller.enabled || !controller.isGrounded)
                return false;

            float speed = Vector3.ProjectOnPlane(controller.velocity, transform.up).magnitude;
            motion = new FootstepMotion(speed, referenceSpeed, mode, FindGround());
            return true;
        }
    }
}

using EasyFootsteps;
using ECM2;
using UnityEngine;

namespace EasyMovement.FirstPerson
{
    [DisallowMultipleComponent, RequireComponent(typeof(Character))]
    public class EcmFootstepSource : FootstepMotionSource
    {
        [SerializeField] private Character character;
        [SerializeField] private string walkState = "Walk";
        [SerializeField] private string sprintState = "Sprint";
        [SerializeField] private string crouchState = "Crouch";

        private ICharacterMovementStateProvider stateProvider;

        protected virtual void Awake()
        {
            if (!character) character = GetComponent<Character>();
            stateProvider = character.GetComponent<ICharacterMovementStateProvider>();
        }

        protected virtual string GetGroundedStateId(Character motor)
        {
            if (motor.IsCrouched()) return crouchState;
            return stateProvider != null && stateProvider.IsSprinting() ? sprintState : walkState;
        }

        public override bool TryGetGroundedMotion(out FootstepMotion motion)
        {
            motion = default;
            if (!character || !character.isActiveAndEnabled || !character.characterMovement ||
                !character.IsWalking() || !character.IsGrounded()) return false;

            Vector3 velocity = Vector3.ProjectOnPlane(character.GetVelocity(), character.GetUpVector());
            motion = new FootstepMotion(velocity.magnitude, character.GetMaxSpeed(),
                GetGroundedStateId(character), character.characterMovement.groundCollider);
            return true;
        }

        public override Collider FindGround()
        {
            if (!character || !character.transform || !character.characterMovement) return base.FindGround();
            var movement = character.characterMovement;
            return movement.groundCollider ? movement.groundCollider : ProbeGround(character.GetUpVector(), movement.collisionLayers);
        }
    }
}

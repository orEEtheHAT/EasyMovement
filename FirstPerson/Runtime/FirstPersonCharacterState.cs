using System;
using UnityEngine;

namespace EasyMovement.FirstPerson
{
    /// <summary>
    /// A JSON-compatible pose and gait-request snapshot. It does not own storage, input settings,
    /// velocity, active input blocks, transient movement modes or external gameplay state.
    /// </summary>
    [Serializable]
    public sealed class FirstPersonCharacterState
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public float ViewPitch;
        public bool CrouchRequested;
        public bool SprintRequested;
        public string CustomStateId = string.Empty;
    }

    public partial class FirstPersonCharacterController
    {
        /// <summary>Captures world pose, local view pitch and configured gait requests.</summary>
        public FirstPersonCharacterState CaptureState()
        {
            if (!characterMovement) throw new InvalidOperationException("Initialize the character before capturing its state.");
            return new FirstPersonCharacterState
            {
                Position = GetPosition(),
                Rotation = GetPendingRotation(),
                ViewPitch = LookPivot ? -Mathf.DeltaAngle(0f, LookPivot.localEulerAngles.x) : pitch,
                CrouchRequested = crouchInputPressed,
                SprintRequested = sprintRequested,
                CustomStateId = requestedState != null ? requestedState.stateId : string.Empty
            };
        }

        /// <summary>
        /// Validates the complete snapshot before teleporting into normal grounded/falling locomotion.
        /// Stops velocity, jump, accumulated forces and pending yaw. Gaits obey current capabilities
        /// and capsule clearance. Input blocks are preserved. Suspend external providers while restoring.
        /// Exit noclip and enable the controller first. Saved positions must be valid for the current level.
        /// </summary>
        public bool TryRestoreState(FirstPersonCharacterState state, out string error)
        {
            error = null;
            if (state == null) error = "The character state is missing.";
            else if (state.Version != FirstPersonCharacterState.CurrentVersion) error = "Unsupported character state version.";
            else if (!Finite(state.Position.x) || !Finite(state.Position.y) || !Finite(state.Position.z) ||
                     !Finite(state.Rotation.x) || !Finite(state.Rotation.y) || !Finite(state.Rotation.z) ||
                     !Finite(state.Rotation.w) || !Finite(state.ViewPitch)) error = "Character pose values must be finite.";
            else if (Quaternion.Dot(state.Rotation, state.Rotation) < 0.000001f ||
                     !Finite(Quaternion.Dot(state.Rotation, state.Rotation))) error = "Character rotation must be a non-zero quaternion.";
            else if (state.ViewPitch < -180f || state.ViewPitch > 180f) error = "View pitch must be between -180 and 180 degrees.";
            else if (!isActiveAndEnabled || !characterMovement || !characterMovement.enabled) error = "Enable the initialized character before restoring its state.";
            else if (IsNoclipActive) error = "Exit noclip before restoring the character state.";
            else if (state.CrouchRequested && !canEverCrouch) error = "This character does not allow the saved crouch request.";
            else if (state.SprintRequested && !allowSprint) error = "This character does not allow the saved sprint request.";
            var custom = error == null && !string.IsNullOrEmpty(state.CustomStateId) ? FindCustomState(state.CustomStateId) : null;
            if (error == null && !string.IsNullOrEmpty(state.CustomStateId) && custom == null)
                error = "The saved custom movement state is not configured on this character: " + state.CustomStateId;
            if (error != null) return false;

            ManualInputReset?.Invoke();
            // Consume through the inherited extension point; ECM2's private accumulator must not survive restore.
            ConsumeRotationInput();
            StopJumping();
            SetMovementDirection(Vector3.zero);
            SetVelocity(Vector3.zero);
            characterMovement.ClearAccumulatedForces();
            ClearCurrentGround();
            var interpolation = characterMovement.interpolation;
            try
            {
                characterMovement.interpolation = RigidbodyInterpolation.None;
                SetRotation(state.Rotation.normalized);
                SetPosition(state.Position, updateGround: true);
            }
            finally { characterMovement.interpolation = interpolation; }
            SetMovementMode(IsGrounded() ? MovementMode.Walking : MovementMode.Falling);
            pitch = state.ViewPitch;
            if (LookPivot) LookPivot.localRotation = Quaternion.AngleAxis(-pitch, Vector3.right);
            requestedState = custom;
            sprintRequested = state.SprintRequested;
            sprinting = false;
            if (state.CrouchRequested) Crouch(); else UnCrouch();
            CheckCrouchInput();
            UpdateView();
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

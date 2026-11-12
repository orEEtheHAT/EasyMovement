using System;
using System.Collections.Generic;
using EasyFootsteps;
using ECM2;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace EasyMovement.FirstPerson
{
    [DisallowMultipleComponent]
    public partial class FirstPersonCharacterController : Character, IFootstepMotionSource
    {
        [Serializable]
        private sealed class CustomState
        {
            [FootstepStateId] public string stateId = string.Empty;
            [Min(0f)] public float maxSpeed = 3f;
            public CinemachineCamera view = null;
        }

        private enum Gait { Walk, Sprint, Crouch, Custom }
        [Header("View")]
        [FormerlySerializedAs("cameraTarget")]
        [SerializeField, Tooltip("Eye-height pivot followed by the Cinemachine views. Pitch rotates this pivot.")] private GameObject lookPivot;
        public CinemachineCamera defaultCamera;
        public CinemachineCamera crouchedCamera;
        public CinemachineCamera sprintCamera;

        [SerializeField] private bool allowSprint = true;
        [SerializeField, Min(0f)] private float _maxSprintSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float _minSprintInput = 0.1f;

        [SerializeField, FootstepStateId] private string walkState = "Walk";
        [SerializeField, FootstepStateId] private string sprintState = "Sprint";
        [SerializeField, FootstepStateId] private string crouchState = "Crouch";
        [SerializeField] private CustomState[] customStates = Array.Empty<CustomState>();
        [Tooltip("Distance below the pivot to search if ECM2 has no ground collider for an external step.")]
        [SerializeField, Min(0f)] private float groundProbeDepth = 2f;

        [SerializeField] private bool allowNoclip = true;
        [SerializeField, Min(1f)] private float noclipBoostMultiplier = 3f;
        private MovementMode modeBeforeNoclip;
        private int customModeBeforeNoclip;
        private bool collisionsBeforeNoclip, constraintBeforeNoclip;
        private Vector3 noclipEntryPosition;
        public bool IsNoclipActive { get; private set; }

        private bool sprintRequested;
        private bool sprinting;
        private float pitch;
        private CustomState requestedState;
        private CinemachineCamera lastView;
        private bool refreshSettings;

        private readonly List<InputBlock> inputBlocks = new List<InputBlock>();

        /// <summary>Whether manual input readers must yield. Scripted motor commands remain available.</summary>
        public bool InputBlocked
        {
            get
            {
                RemoveInactiveInputBlocks();
                return inputBlocks.Count != 0;
            }
        }

        /// <summary>Raised only when the effective input block changes.</summary>
        public event Action<bool> InputBlockedChanged;
        internal event Action ManualInputReset;

        /// <summary>
        /// Blocks manual readers until this independent handle is disposed or its owner becomes inactive.
        /// A Behaviour must be enabled; a GameObject or Component must be active in the hierarchy.
        /// Other Unity objects remain owners until destroyed. Inactive owners/controllers return an empty handle.
        /// Disabling the controller releases all handles; callers acquire again after enabling it.
        /// This does not stop or reset the motor's direction, velocity, rotation or movement mode.
        /// </summary>
        public IDisposable AcquireInputBlock(UnityEngine.Object owner)
        {
            if (!owner) throw new ArgumentNullException(nameof(owner));
            RemoveInactiveInputBlocks();
            if (!isActiveAndEnabled || !IsInputBlockOwnerActive(owner)) return InputBlock.Empty;
            var block = new InputBlock(this, owner);
            bool wasBlocked = inputBlocks.Count != 0;
            inputBlocks.Add(block);
            if (!wasBlocked) InputBlockedChanged?.Invoke(true);
            return block;
        }

        private sealed class InputBlock : IDisposable
        {
            internal static readonly InputBlock Empty = new InputBlock(null, null);
            internal FirstPersonCharacterController Controller;
            internal UnityEngine.Object Owner;

            internal InputBlock(FirstPersonCharacterController controller, UnityEngine.Object owner)
            {
                Controller = controller;
                Owner = owner;
            }

            public void Dispose()
            {
                var controller = Controller;
                if (controller) controller.ReleaseInputBlock(this);
                Controller = null;
                Owner = null;
            }
        }

        private static bool IsInputBlockOwnerActive(UnityEngine.Object owner)
        {
            if (!owner) return false;
            if (owner is Behaviour behaviour) return behaviour.isActiveAndEnabled;
            if (owner is GameObject gameObject) return gameObject.activeInHierarchy;
            if (owner is Component component) return component.gameObject.activeInHierarchy;
            return true;
        }

        private void ReleaseInputBlock(InputBlock block)
        {
            if (!inputBlocks.Remove(block)) return;
            block.Controller = null;
            block.Owner = null;
            if (inputBlocks.Count == 0) InputBlockedChanged?.Invoke(false);
        }

        private void RemoveInactiveInputBlocks()
        {
            bool wasBlocked = inputBlocks.Count != 0;
            for (int i = inputBlocks.Count - 1; i >= 0; i--)
            {
                var block = inputBlocks[i];
                if (IsInputBlockOwnerActive(block.Owner)) continue;
                block.Controller = null;
                block.Owner = null;
                inputBlocks.RemoveAt(i);
            }
            if (wasBlocked && inputBlocks.Count == 0) InputBlockedChanged?.Invoke(false);
        }

        private void ClearInputBlocks()
        {
            if (inputBlocks.Count == 0) return;
            foreach (var block in inputBlocks)
            {
                block.Controller = null;
                block.Owner = null;
            }
            inputBlocks.Clear();
            InputBlockedChanged?.Invoke(false);
        }

        // Use Update as well as the getter so inactive-owner cleanup continues when simulation is paused.
        // Derived controllers overriding Update must call base.Update().
        protected virtual void Update() => RemoveInactiveInputBlocks();

        private Gait CurrentGait
        {
            get
            {
                if (!IsWalking()) return Gait.Walk;
                if (IsCrouched()) return Gait.Crouch;
                if (requestedState != null) return Gait.Custom;
                return sprinting ? Gait.Sprint : Gait.Walk;
            }
        }

        public string ActiveStateId => IsNoclipActive ? "NoClip" : GetGroundedStateId();

        public void ToggleNoclip() => TrySetNoclip(!IsNoclipActive);

        public bool TrySetNoclip(bool active)
        {
            if (active == IsNoclipActive) return true;
            if (!isActiveAndEnabled || !characterMovement || !characterMovement.enabled) return false;

            if (!active)
            {
                if (NoclipExitBlocked()) return false;
                ExitNoclip();
                return true;
            }
            if (!allowNoclip || IsPaused()) return false;

            modeBeforeNoclip = GetMovementMode();
            customModeBeforeNoclip = GetCustomMovementMode();
            collisionsBeforeNoclip = characterMovement.detectCollisions;
            constraintBeforeNoclip = characterMovement.constrainToGround;
            noclipEntryPosition = GetPosition();
            StopJumping();
            UnCrouch();
            sprinting = false;
            SetMovementDirection(Vector3.zero);
            SetVelocity(Vector3.zero);
            characterMovement.ClearAccumulatedForces();
            IsNoclipActive = true;
            characterMovement.detectCollisions = false;
            SetMovementMode(MovementMode.Flying);
            characterMovement.constrainToGround = false;
            UpdateView();
            return true;
        }

        private bool NoclipExitBlocked()
        {
            if (!collisionsBeforeNoclip) return false;
            characterMovement.OverlapTest(characterMovement.collisionLayers, QueryTriggerInteraction.Ignore, out int count);
            return count > 0;
        }

        private void ExitNoclip()
        {
            IsNoclipActive = false;
            SetMovementDirection(Vector3.zero);
            SetVelocity(Vector3.zero);
            characterMovement.ClearAccumulatedForces();
            characterMovement.detectCollisions = collisionsBeforeNoclip;
            var mode = modeBeforeNoclip;
            if (mode == MovementMode.Walking || mode == MovementMode.Falling || mode == MovementMode.Swimming)
                mode = IsInWaterPhysicsVolume() ? MovementMode.Swimming : MovementMode.Falling;
            SetMovementMode(mode, customModeBeforeNoclip);
            if (mode == modeBeforeNoclip ||
                (mode == MovementMode.Falling && modeBeforeNoclip == MovementMode.Walking))
                characterMovement.constrainToGround = constraintBeforeNoclip;
            if (mode == MovementMode.Falling && collisionsBeforeNoclip)
            {
                characterMovement.SetPosition(GetPosition(), updateGround: true);
                if (IsGrounded()) SetMovementMode(MovementMode.Walking);
            }
            UpdateView();
        }

        protected override void FlyingMovementMode(float deltaTime)
        {
            if (IsNoclipActive)
                SetVelocity(Vector3.ClampMagnitude(GetMovementDirection(), 1f) * GetMaxSpeed());
            else
                base.FlyingMovementMode(deltaTime);
        }

        public Transform LookPivot
        {
            get => lookPivot ? lookPivot.transform : null;
            set
            {
                lookPivot = value ? value.gameObject : null;
                if (value) pitch = -Mathf.DeltaAngle(0f, value.localEulerAngles.x);
            }
        }

        [Obsolete("Use LookPivot instead.")]
        public GameObject cameraTarget
        {
            get => lookPivot;
            set => LookPivot = value ? value.transform : null;
        }

        private CustomState FindCustomState(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var state in customStates ?? Array.Empty<CustomState>())
                if (state != null && string.Equals(state.stateId, id, StringComparison.OrdinalIgnoreCase))
                    return state;
            return null;
        }

        // The void signature also works as a UnityEvent listener.
        public void SetCustomState(string id) => TrySetCustomState(id);

        public bool TrySetCustomState(string id)
        {
            var state = FindCustomState(id);
            if (state == null) return false;
            if (state == requestedState) return true;
            requestedState = state;
            sprinting = false;
            UpdateView();
            return true;
        }

        public void ClearCustomState()
        {
            if (requestedState == null) return;
            requestedState = null;
            UpdateView();
        }

        public float MaxSprintSpeed
        {
            get => _maxSprintSpeed;
            set => _maxSprintSpeed = Mathf.Max(0f, value);
        }

        public bool IsSprinting() => sprinting;
        public void Sprint() => sprintRequested = true;
        public void StopSprinting() => sprintRequested = false;

        public override float GetMaxSpeed()
        {
            if (IsNoclipActive) return maxFlySpeed * (sprintRequested ? noclipBoostMultiplier : 1f);
            return CurrentGait switch
            {
                Gait.Custom => requestedState.maxSpeed,
                Gait.Sprint => MaxSprintSpeed,
                _ => base.GetMaxSpeed()
            };
        }

        protected virtual string GetGroundedStateId()
        {
            return CurrentGait switch
            {
                Gait.Crouch => crouchState,
                Gait.Custom => requestedState.stateId,
                Gait.Sprint => sprintState,
                _ => walkState
            };
        }

        public bool TryGetGroundedMotion(out FootstepMotion motion)
        {
            motion = default;
            if (IsNoclipActive || !isActiveAndEnabled || !characterMovement || !IsWalking() || !IsGrounded()) return false;

            float speed = Vector3.ProjectOnPlane(GetVelocity(), GetUpVector()).magnitude;
            motion = new FootstepMotion(speed, GetMaxSpeed(), GetGroundedStateId(), characterMovement.groundCollider);
            return true;
        }

        public Collider FindGround()
        {
            if (characterMovement && characterMovement.groundCollider) return characterMovement.groundCollider;
            // The ECM2 transform cache is initialized in play mode. Preview instances also support external steps.
            Transform pivot = gameObject.transform;
            int layers = characterMovement ? characterMovement.collisionLayers.value : Physics.DefaultRaycastLayers;
            return FootstepGround.Probe(pivot.position, pivot.up, layers, groundProbeDepth);
        }

        public void AddControlYawInput(float degrees) => AddYawInput(degrees);

        public void AddControlPitchInput(float degrees, float minimum = -80f, float maximum = 80f)
        {
            if (!lookPivot || degrees == 0f) return;
            pitch = Mathf.Clamp(pitch + degrees, minimum, maximum);
            LookPivot.localRotation = Quaternion.AngleAxis(-pitch, Vector3.right);
        }

        protected override void Start()
        {
            base.Start();
            if (lookPivot)
                pitch = -Mathf.DeltaAngle(0f, LookPivot.localEulerAngles.x);
            SetRotationMode(RotationMode.None);
            UpdateView();
        }

        protected override void OnBeforeSimulationUpdate(float deltaTime)
        {
            base.OnBeforeSimulationUpdate(deltaTime);
            if (IsNoclipActive && !allowNoclip) TrySetNoclip(false);
            if (IsNoclipActive && !IsFlying()) SetMovementMode(MovementMode.Flying);
            if (refreshSettings)
            {
                refreshSettings = false;
                if (requestedState != null) requestedState = FindCustomState(requestedState.stateId);
                UpdateView();
            }
            float inputSquared = GetMovementDirection().sqrMagnitude;
            bool next = allowSprint && sprintRequested && requestedState == null && IsWalking() &&
                        !IsCrouched() && inputSquared > 0f && inputSquared >= _minSprintInput * _minSprintInput;
            if (sprinting == next) return;
            sprinting = next;
            UpdateView();
        }

        protected override void OnCrouched()
        {
            base.OnCrouched();
            UpdateView();
        }

        protected override void OnUnCrouched()
        {
            base.OnUnCrouched();
            UpdateView();
        }

        protected override void OnMovementModeChanged(MovementMode previousMode, int previousCustomMode)
        {
            base.OnMovementModeChanged(previousMode, previousCustomMode);
            UpdateView();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateView();
        }

        protected override void OnDisable()
        {
            ManualInputReset?.Invoke();
            ClearInputBlocks();
            if (IsNoclipActive && characterMovement)
            {
                // A disabled motor cannot keep flying out of a wall. Restore a valid position before enabling collisions.
                if (NoclipExitBlocked()) characterMovement.SetPosition(noclipEntryPosition);
                ExitNoclip();
            }
            base.OnDisable();
            sprintRequested = false;
            sprinting = false;
            requestedState = null;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            refreshSettings = true;
        }

        private void UpdateView()
        {
            var view = CurrentGait switch
            {
                Gait.Crouch => crouchedCamera,
                Gait.Sprint => sprintCamera,
                Gait.Custom => requestedState.view,
                _ => defaultCamera
            };
            if (!view) view = defaultCamera;

            if (lastView != view) SetView(lastView, view);
            lastView = view;
            SetView(defaultCamera, view);
            SetView(crouchedCamera, view);
            SetView(sprintCamera, view);
            foreach (var state in customStates ?? Array.Empty<CustomState>())
                if (state != null) SetView(state.view, view);
        }

        private static void SetView(CinemachineCamera candidate, CinemachineCamera activeView)
        {
            if (candidate && candidate.gameObject.activeSelf != (candidate == activeView))
                candidate.gameObject.SetActive(candidate == activeView);
        }
    }
}

using System;
using EasyFootsteps;
using ECM2;
using Unity.Cinemachine;
using UnityEngine;

namespace EasyMovement.FirstPerson
{
    [DisallowMultipleComponent]
    public class FirstPersonCharacterController : Character, IFootstepMotionSource
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
        public GameObject cameraTarget;
        public CinemachineCamera defaultCamera;
        public CinemachineCamera crouchedCamera;
        public CinemachineCamera sprintCamera;
        [SerializeField] private bool lockCursorOnStart = true;

        [SerializeField] private bool allowSprint = true;
        [SerializeField, Min(0f)] private float _maxSprintSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float _minSprintInput = 0.1f;

        [SerializeField, FootstepStateId] private string walkState = "Walk";
        [SerializeField, FootstepStateId] private string sprintState = "Sprint";
        [SerializeField, FootstepStateId] private string crouchState = "Crouch";
        [SerializeField] private CustomState[] customStates = Array.Empty<CustomState>();
        [Tooltip("Distance below the pivot to search if ECM2 has no ground collider for an external step.")]
        [SerializeField, Min(0f)] private float groundProbeDepth = 2f;

        private bool sprintRequested;
        private bool sprinting;
        private float pitch;
        private CustomState requestedState;
        private CinemachineCamera lastView;
        private bool refreshSettings;

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

        public string ActiveStateId => GetGroundedStateId();

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

        public override float GetMaxSpeed() => CurrentGait switch
        {
            Gait.Custom => requestedState.maxSpeed,
            Gait.Sprint => MaxSprintSpeed,
            _ => base.GetMaxSpeed()
        };

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
            if (!isActiveAndEnabled || !characterMovement || !IsWalking() || !IsGrounded()) return false;

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
            if (!cameraTarget || degrees == 0f) return;
            pitch = Mathf.Clamp(pitch + degrees, minimum, maximum);
            cameraTarget.transform.localRotation = Quaternion.AngleAxis(-pitch, Vector3.right);
        }

        protected override void Start()
        {
            base.Start();
            if (!camera) camera = Camera.main;
            if (cameraTarget)
                pitch = -Mathf.DeltaAngle(0f, cameraTarget.transform.localEulerAngles.x);
            SetRotationMode(RotationMode.None);
            UpdateView();
            if (lockCursorOnStart) Cursor.lockState = CursorLockMode.Locked;
        }

        protected override void OnBeforeSimulationUpdate(float deltaTime)
        {
            base.OnBeforeSimulationUpdate(deltaTime);
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

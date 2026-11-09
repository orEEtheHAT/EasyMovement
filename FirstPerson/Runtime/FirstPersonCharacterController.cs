using EasyFootsteps;
using ECM2;
using Unity.Cinemachine;
using UnityEngine;

namespace EasyMovement.FirstPerson
{
    [DisallowMultipleComponent]
    public class FirstPersonCharacterController : Character, IFootstepMotionSource
    {
        [Header("View")]
        public GameObject cameraTarget;
        public CinemachineCamera defaultCamera;
        public CinemachineCamera crouchedCamera;
        public CinemachineCamera sprintCamera;
        [SerializeField] private bool lockCursorOnStart = true;

        [Header("Sprint")]
        [SerializeField, Min(0f)] private float _maxSprintSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float _minSprintInput = 0.1f;

        [Header("Footsteps")]
        [SerializeField] private string walkState = "Walk";
        [SerializeField] private string sprintState = "Sprint";
        [SerializeField] private string crouchState = "Crouch";
        [Tooltip("Distance below the pivot to search if ECM2 has no ground collider for an external step.")]
        [SerializeField, Min(0f)] private float groundProbeDepth = 2f;

        private bool sprintRequested;
        private bool sprinting;
        private float pitch;

        public float MaxSprintSpeed
        {
            get => _maxSprintSpeed;
            set => _maxSprintSpeed = Mathf.Max(0f, value);
        }

        public bool IsSprinting() => sprinting;
        public void Sprint() => sprintRequested = true;
        public void StopSprinting() => sprintRequested = false;

        public override float GetMaxSpeed() => sprinting ? MaxSprintSpeed : base.GetMaxSpeed();

        protected virtual string GetGroundedStateId()
        {
            if (IsCrouched()) return crouchState;
            return sprinting ? sprintState : walkState;
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
            float input = GetMovementDirection().magnitude;
            bool next = sprintRequested && IsWalking() && !IsCrouched() && input > 0f && input >= _minSprintInput;
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
        }

        private void UpdateView()
        {
            var view = defaultCamera;
            if (IsCrouched() && crouchedCamera) view = crouchedCamera;
            else if (sprinting && sprintCamera) view = sprintCamera;

            SetView(defaultCamera, view);
            SetView(crouchedCamera, view);
            SetView(sprintCamera, view);
        }

        private static void SetView(CinemachineCamera candidate, CinemachineCamera activeView)
        {
            if (candidate && candidate.gameObject.activeSelf != (candidate == activeView))
                candidate.gameObject.SetActive(candidate == activeView);
        }
    }
}

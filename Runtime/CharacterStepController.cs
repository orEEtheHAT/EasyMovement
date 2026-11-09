using System;
using EasyFeedbacks;
using UnityEngine;

namespace EasyFootsteps
{
    [DisallowMultipleComponent]
    public sealed class CharacterStepController : MonoBehaviour, ISerializationCallbackReceiver
    {
        public enum StepSourceMode
        {
            DistanceBased,
            ExternalEvents
        }

        public enum MovementMode
        {
            Walk,
            Sprint,
            Crouch,
            Custom
        }

        public readonly struct StepEvent
        {
            public StepEvent(
                MovementMode mode,
                Collider groundCollider,
                float speed,
                float stepDistance,
                float movementAmount,
                int stepIndex,
                int stepSide)
            {
                StateId = FootstepState.IdFor(mode);
                Mode = mode;
                GroundCollider = groundCollider;
                Speed = speed;
                StepDistance = stepDistance;
                MovementAmount = movementAmount;
                StepIndex = stepIndex;
                StepSide = stepSide;
            }

            public StepEvent(string stateId, Collider groundCollider, float speed, float stepDistance,
                float movementAmount, int stepIndex, int stepSide)
                : this(FootstepState.LegacyMode(stateId), groundCollider, speed, stepDistance, movementAmount, stepIndex, stepSide)
            {
                StateId = stateId;
            }

            public string StateId { get; }
            public MovementMode Mode { get; }
            public Collider GroundCollider { get; }
            public float Speed { get; }
            public float StepDistance { get; }
            public float MovementAmount { get; }
            public int StepIndex { get; }
            public int StepSide { get; }
        }

#pragma warning disable CS0649
        [Serializable]
        private struct SurfaceFeedback
        {
            public string name;
            public PhysicsMaterial physicsMaterial;
            public FeedbackPreset feedback;
        }
#pragma warning restore CS0649

        [Header("References")]
        [SerializeField] private FootstepMotionSource _motionSource;

        [Header("Step Source")]
        [SerializeField] private StepSourceMode _stepSourceMode = StepSourceMode.DistanceBased;
        [SerializeField] private bool _validateExternalSteps = true;

        [Header("Step Timing")]
        [SerializeField, Min(0f)] private float _minMoveSpeed = 0.2f;
        [SerializeField] private FootstepState[] _states;
        [SerializeField, HideInInspector] private bool _statesInitialized;

        // v1.0 field paths are still used by existing prefab and scene overrides.
        [SerializeField, HideInInspector, Min(0.01f)] private float _walkStepDistance = 1.95f;
        [SerializeField, HideInInspector, Min(0.01f)] private float _sprintStepDistance = 2.25f;
        [SerializeField, HideInInspector, Min(0.01f)] private float _crouchStepDistance = 1.4f;
        [SerializeField, Range(0f, 0.95f)] private float _startStepPhase = 0.5f;

        [Header("Head Bob")]
        [SerializeField] private bool _enableHeadBob = true;
        [SerializeField] private Transform _headBobTarget;
        [SerializeField] private AnimationCurve _headBobHorizontalCurve = CreateDefaultHeadBobHorizontalCurve();
        [SerializeField] private AnimationCurve _headBobVerticalCurve = CreateDefaultHeadBobVerticalCurve();
        [SerializeField, Min(0f)] private float _headBobHorizontalAmplitude = 0.05f;
        [SerializeField, Min(0f)] private float _headBobVerticalAmplitude = 0.05f;
        [SerializeField, HideInInspector, Min(0f)] private float _headBobCrouchMultiplier = 0.55f;
        [SerializeField, HideInInspector, Min(0f)] private float _headBobSprintAmplitudeMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float _headBobStepImpactAmplitude = 0.05f;
        [SerializeField, Min(0f)] private float _headBobStepImpactDecaySpeed = 0.24f;
        [SerializeField, Min(0f)] private float _headBobBlendInSpeed = 10f;
        [SerializeField, Min(0f)] private float _headBobBlendOutSpeed = 14f;
        [SerializeField, Min(0.001f)] private float _headBobPositionSmoothTime = 0.04f;

        [Header("Step Feedback")]
        [SerializeField] private bool _enableStepFeedback = true;
        [Tooltip("Used if no surface override or physics material has a feedback assigned.")]
        [SerializeField] private FeedbackPreset _defaultFeedback;
        [SerializeField] private SurfaceFeedback[] _surfaces = Array.Empty<SurfaceFeedback>();
        [SerializeField, HideInInspector, Min(0f)] private float _walkVolume = 0.75f;
        [SerializeField, HideInInspector, Min(0f)] private float _sprintVolume = 1f;
        [SerializeField, HideInInspector, Min(0f)] private float _crouchVolume = 0.45f;

        private float _distanceSinceLastStep;
        private int _stepIndex;
        private int _stepSide = 1;

        private Vector3 _headBobStartLocalPosition;
        private Vector3 _headBobPositionVelocity;
        private float _headBobAmount;
        private float _headBobStepImpact;
        private bool _hasHeadBobStartPosition;
        private Transform _cachedHeadBobTarget;
        private bool _headBobWasApplied;

        public event Action<StepEvent> Stepped;

        public StepSourceMode SourceMode => _stepSourceMode;
        private FootstepState _activeState;
        public string CurrentStateId => _activeState.id;
        public MovementMode CurrentMode => CurrentStateId == null ? MovementMode.Walk : FootstepState.LegacyMode(CurrentStateId);
        public Collider GroundCollider { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float CurrentStepDistance { get; private set; } = 1f;
        public float MovementAmount { get; private set; }
        public float StepPhase { get; private set; }
        public int StepSide => _stepSide;
        public bool IsStepping { get; private set; }

        public bool GenerateSteps { get; set; } = true;
        public bool ApplyHeadBob { get; set; } = true;

        private static AnimationCurve CreateDefaultHeadBobHorizontalCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, Mathf.PI, Mathf.PI),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f, -Mathf.PI, -Mathf.PI));
        }

        private static AnimationCurve CreateDefaultHeadBobVerticalCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, -1f),
                new Keyframe(0.25f, -0.5f, Mathf.PI, Mathf.PI),
                new Keyframe(0.5f, 0f),
                new Keyframe(0.75f, -0.5f, -Mathf.PI, -Mathf.PI),
                new Keyframe(1f, -1f));
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize() => EnsureStateList();

        private void EnsureStateList()
        {
            if (_statesInitialized)
                return;

            _states = FootstepState.Defaults(true);
            _statesInitialized = true;
        }

        private void Reset()
        {
            _states = FootstepState.Defaults(false);
            _statesInitialized = true;
            _motionSource = GetComponent<FootstepMotionSource>();
        }

        private void Awake()
        {
            EnsureStateList();
            if (!_motionSource)
                _motionSource = GetComponent<FootstepMotionSource>();

            CacheHeadBobStartPosition();
        }

        private void Update()
        {
            if (!GenerateSteps)
            {
                IsStepping = false;
                MovementAmount = 0f;
                StepPhase = 0f;
                return;
            }

            if (!RefreshState())
            {
                ResetCycle();
                return;
            }

            if (CurrentSpeed < _minMoveSpeed)
            {
                IsStepping = false;
                MovementAmount = 0f;
                StepPhase = CalculateStepPhase();
                return;
            }

            if (_stepSourceMode == StepSourceMode.ExternalEvents)
            {
                IsStepping = true;
                UpdateExternalCycle();
                return;
            }

            UpdateDistanceBasedCycle();
        }

        private void LateUpdate()
        {
            if (!ApplyHeadBob)
            {
                if (_headBobWasApplied)
                    ResetHeadBob();

                return;
            }

            UpdateHeadBob();
        }

        private void OnDisable()
        {
            ResetHeadBob();
        }

        public void TriggerStep()
        {
            TriggerStep(0);
        }

        public void TriggerStep(int stepSide)
        {
            if (!GenerateSteps)
                return;

            if (_stepSourceMode != StepSourceMode.ExternalEvents)
                return;

            if (!RefreshState())
                return;

            if (_validateExternalSteps && CurrentSpeed < _minMoveSpeed)
                return;

            _distanceSinceLastStep = 0f;
            StepPhase = 0f;
            IsStepping = true;

            EmitStep(stepSide);
        }

        public void TriggerLeftStep()
        {
            TriggerStep(-1);
        }

        public void TriggerRightStep()
        {
            TriggerStep(1);
        }

        public void PlayExternalStep(MovementMode mode, int stepSide)
        {
            PlayExternalStep(FootstepState.IdFor(mode), stepSide);
        }

        public void PlayExternalStep(string stateId, int stepSide)
        {
            if (!SelectState(stateId))
            {
                ResetCycle();
                return;
            }

            GroundCollider = FindGroundCollider();
            CurrentSpeed = 0f;
            MovementAmount = _activeState.externalMovementAmount;
            _stepIndex++;
            _stepSide = stepSide == 0 ? -_stepSide : NormalizeStepSide(stepSide);

            StepPhase = 0f;
            IsStepping = true;

            StepEvent stepEvent = new StepEvent(
                CurrentStateId,
                GroundCollider,
                CurrentSpeed,
                CurrentStepDistance,
                MovementAmount,
                _stepIndex,
                _stepSide);

            PlayStepFeedback(stepEvent);
            Stepped?.Invoke(stepEvent);
        }

        private bool RefreshState()
        {
            if (!_motionSource || !_motionSource.isActiveAndEnabled || !_motionSource.TryGetGroundedMotion(out FootstepMotion motion))
                return false;

            GroundCollider = motion.Ground;
            CurrentSpeed = Mathf.Max(0f, motion.PlanarSpeed);
            if (!SelectState(motion.StateId))
                return false;
            MovementAmount = Mathf.Clamp01(CurrentSpeed / Mathf.Max(motion.MaxSpeed, 0.01f));
            return true;
        }

        private void UpdateDistanceBasedCycle()
        {
            if (!IsStepping && _distanceSinceLastStep <= 0f)
                _distanceSinceLastStep = CurrentStepDistance * _startStepPhase;

            IsStepping = true;
            _distanceSinceLastStep += CurrentSpeed * Time.deltaTime;

            while (_distanceSinceLastStep >= CurrentStepDistance)
            {
                _distanceSinceLastStep -= CurrentStepDistance;
                EmitStep(0);
            }

            StepPhase = CalculateStepPhase();
        }

        private void UpdateExternalCycle()
        {
            _distanceSinceLastStep += CurrentSpeed * Time.deltaTime;
            _distanceSinceLastStep = Mathf.Min(_distanceSinceLastStep, CurrentStepDistance);
            StepPhase = CalculateStepPhase();
        }

        private void EmitStep(int stepSide)
        {
            _stepIndex++;
            _stepSide = stepSide == 0 ? -_stepSide : NormalizeStepSide(stepSide);

            AddHeadBobStepImpact();

            StepEvent stepEvent = new StepEvent(
                CurrentStateId,
                GroundCollider,
                CurrentSpeed,
                CurrentStepDistance,
                MovementAmount,
                _stepIndex,
                _stepSide);

            PlayStepFeedback(stepEvent);
            Stepped?.Invoke(stepEvent);
        }

        private bool SelectState(string requestedId)
        {
            EnsureStateList();
            FootstepState selected = default;
            foreach (var state in _states ?? Array.Empty<FootstepState>())
            {
                if (string.IsNullOrWhiteSpace(state.id))
                    continue;
                if (selected.id == null)
                    selected = state;
                if (string.Equals(state.id, requestedId, StringComparison.OrdinalIgnoreCase))
                {
                    selected = state;
                    break;
                }
            }

            if (selected.id == null)
            {
                _activeState = default;
                return false;
            }

            switch (selected.legacySlot)
            {
                case 1:
                    selected.stepDistance = _walkStepDistance;
                    selected.feedbackVolume = _walkVolume;
                    break;
                case 2:
                    selected.stepDistance = _sprintStepDistance;
                    selected.feedbackVolume = _sprintVolume;
                    selected.headBobMultiplier = _headBobSprintAmplitudeMultiplier;
                    break;
                case 3:
                    selected.stepDistance = _crouchStepDistance;
                    selected.feedbackVolume = _crouchVolume;
                    selected.headBobMultiplier = _headBobCrouchMultiplier;
                    break;
            }
            float distance = Mathf.Max(0.01f, selected.stepDistance);
            if (_activeState.id != null && distance != CurrentStepDistance)
                _distanceSinceLastStep = CalculateStepPhase() * distance;

            _activeState = selected;
            CurrentStepDistance = distance;
            return true;
        }

        private float CalculateStepPhase()
        {
            return Mathf.Clamp01(_distanceSinceLastStep / Mathf.Max(CurrentStepDistance, 0.01f));
        }

        private int NormalizeStepSide(int stepSide)
        {
            return stepSide < 0 ? -1 : 1;
        }

        private void ResetCycle()
        {
            GroundCollider = null;
            CurrentSpeed = 0f;
            MovementAmount = 0f;
            StepPhase = 0f;
            IsStepping = false;
            _distanceSinceLastStep = 0f;
        }

        private Collider FindGroundCollider()
        {
            if (_motionSource && _motionSource.isActiveAndEnabled)
                return _motionSource.FindGround();

            Vector3 up = transform.up;
            return Physics.Raycast(transform.position + up * 0.35f, -up, out RaycastHit hit, 2f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.collider : null;
        }

        private void UpdateHeadBob()
        {
            if (!_enableHeadBob || !_headBobTarget)
            {
                if (_headBobWasApplied)
                    ResetHeadBob();

                return;
            }

            CacheHeadBobStartPosition();
            _headBobWasApplied = true;

            float targetAmount = IsStepping ? MovementAmount : 0f;
            float blendSpeed = targetAmount > _headBobAmount ? _headBobBlendInSpeed : _headBobBlendOutSpeed;

            _headBobAmount = Mathf.MoveTowards(
                _headBobAmount,
                targetAmount,
                blendSpeed * Time.deltaTime);

            float amplitudeMultiplier = _activeState.headBobMultiplier;

            Vector3 offset = CalculateHeadBobOffset(StepPhase, StepSide) *
                             (_headBobAmount * amplitudeMultiplier);

            if (_headBobStepImpact > 0f)
            {
                offset += Vector3.down * _headBobStepImpact;
                _headBobStepImpact = Mathf.MoveTowards(
                    _headBobStepImpact,
                    0f,
                    _headBobStepImpactDecaySpeed * Time.deltaTime);
            }

            MoveHeadBobTarget(_headBobStartLocalPosition + offset);
        }

        private void AddHeadBobStepImpact()
        {
            if (!_enableHeadBob || !_headBobTarget)
                return;

            float movementAmount = Mathf.Max(MovementAmount, 0.25f);
            _headBobStepImpact = Mathf.Max(
                _headBobStepImpact,
                _headBobStepImpactAmplitude * movementAmount);
        }

        private Vector3 CalculateHeadBobOffset(float stepPhase, int stepSide)
        {
            float phase = Mathf.Repeat(stepPhase, 1f);
            int side = stepSide < 0 ? -1 : 1;

            float horizontal = _headBobHorizontalCurve != null
                ? _headBobHorizontalCurve.Evaluate(phase)
                : Mathf.Sin(phase * Mathf.PI);

            float vertical = _headBobVerticalCurve != null
                ? _headBobVerticalCurve.Evaluate(phase)
                : -((Mathf.Cos(phase * Mathf.PI * 2f) + 1f) * 0.5f);

            float x = horizontal * side * _headBobHorizontalAmplitude;
            float y = vertical * _headBobVerticalAmplitude;

            return new Vector3(x, y, 0f);
        }

        private void MoveHeadBobTarget(Vector3 targetLocalPosition)
        {
            if (_headBobPositionSmoothTime <= 0f)
            {
                _headBobTarget.localPosition = targetLocalPosition;
                return;
            }

            _headBobTarget.localPosition = Vector3.SmoothDamp(
                _headBobTarget.localPosition,
                targetLocalPosition,
                ref _headBobPositionVelocity,
                _headBobPositionSmoothTime);
        }

        private void CacheHeadBobStartPosition()
        {
            if (!_headBobTarget)
                return;

            if (_cachedHeadBobTarget != _headBobTarget)
            {
                _cachedHeadBobTarget = _headBobTarget;
                _hasHeadBobStartPosition = false;
                _headBobPositionVelocity = Vector3.zero;
                _headBobAmount = 0f;
                _headBobStepImpact = 0f;
            }

            if (_hasHeadBobStartPosition)
                return;

            _headBobStartLocalPosition = _headBobTarget.localPosition;
            _hasHeadBobStartPosition = true;
        }

        private void ResetHeadBob()
        {
            if (_headBobTarget && _hasHeadBobStartPosition)
                _headBobTarget.localPosition = _headBobStartLocalPosition;

            _headBobPositionVelocity = Vector3.zero;
            _headBobAmount = 0f;
            _headBobStepImpact = 0f;
            _headBobWasApplied = false;
        }

        private void PlayStepFeedback(StepEvent step)
        {
            if (!_enableStepFeedback)
                return;

            FeedbackPreset preset = ResolveFeedback(step.GroundCollider);
            if (preset)
            {
                float volume = _activeState.feedbackVolume;
                FeedbackService.Play(preset, FeedbackContext.From(transform, null, 1f, volume, Vector3.one));
            }
        }

        private FeedbackPreset ResolveFeedback(Collider ground)
        {
            if (!ground)
                return _defaultFeedback;

            var surface = ground.GetComponentInParent<FootstepSurface>();
            if (surface && surface.Feedback)
                return surface.Feedback;

            foreach (var entry in _surfaces)
            {
                if (entry.physicsMaterial && entry.physicsMaterial == ground.sharedMaterial && entry.feedback)
                    return entry.feedback;
            }
            return _defaultFeedback;
        }

    }
}

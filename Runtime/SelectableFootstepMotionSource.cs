using UnityEngine;

namespace EasyFootsteps
{
    public abstract class SelectableFootstepMotionSource : FootstepMotionSource
    {
        // Kept for sources serialized before named states were introduced.
        [SerializeField, HideInInspector] private CharacterStepController.MovementMode mode;
        [SerializeField, Tooltip("Movement state ID from the step controller. When empty, use the original mode (Walk by default).")]
        private string stateId;

        public string StateId
        {
            get => string.IsNullOrWhiteSpace(stateId) ? FootstepState.IdFor(mode) : stateId;
            set => stateId = value?.Trim();
        }

        public CharacterStepController.MovementMode Mode
        {
            get => FootstepState.LegacyMode(StateId);
            set
            {
                mode = value;
                StateId = FootstepState.IdFor(value);
            }
        }
    }
}

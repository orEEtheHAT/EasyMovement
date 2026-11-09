using System;
using UnityEngine;

namespace EasyFootsteps
{
    [Serializable]
    internal struct FootstepState
    {
        public string id;
        [Min(0.01f)] public float stepDistance;
        [Range(0f, 1f)] public float feedbackVolume;
        [Min(0f)] public float headBobMultiplier;
        [Range(0f, 1f)] public float externalMovementAmount;

        // Migrated rows keep the original serialized property paths for prefab variant overrides.
        [HideInInspector] public int legacySlot;

        public FootstepState(string id, float distance, float volume, float bob, int slot)
        {
            this.id = id;
            stepDistance = distance;
            feedbackVolume = volume;
            headBobMultiplier = bob;
            externalMovementAmount = volume;
            legacySlot = slot;
        }

        public static FootstepState[] Defaults(bool migrate)
        {
            return new[]
            {
                new FootstepState("Walk", 1.95f, 0.75f, 1f, migrate ? 1 : 0),
                new FootstepState("Sprint", 2.25f, 1f, 1.5f, migrate ? 2 : 0),
                new FootstepState("Crouch", 1.4f, 0.45f, 0.55f, migrate ? 3 : 0)
            };
        }

        internal static string IdFor(CharacterStepController.MovementMode mode)
        {
            return mode switch
            {
                CharacterStepController.MovementMode.Walk => "Walk",
                CharacterStepController.MovementMode.Sprint => "Sprint",
                CharacterStepController.MovementMode.Crouch => "Crouch",
                _ => null
            };
        }

        internal static CharacterStepController.MovementMode LegacyMode(string id)
        {
            if (string.Equals(id, "Walk", StringComparison.OrdinalIgnoreCase)) return CharacterStepController.MovementMode.Walk;
            if (string.Equals(id, "Sprint", StringComparison.OrdinalIgnoreCase)) return CharacterStepController.MovementMode.Sprint;
            if (string.Equals(id, "Crouch", StringComparison.OrdinalIgnoreCase)) return CharacterStepController.MovementMode.Crouch;
            return CharacterStepController.MovementMode.Custom;
        }
    }
}

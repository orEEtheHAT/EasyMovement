# EasyFootsteps

Distance-based and animation-driven footsteps for Unity 6.3. Steps can play EasyFeedbacks presets and move an optional head-bob target.

## Installation

1. In Package Manager, install EasyFeedbacks from `https://github.com/orEEtheHAT/Easy-Feedbacks.git#v1.7.0`.
2. Install EasyFootsteps from `https://github.com/orEEtheHAT/EasyFootsteps.git#v1.1.0` or select its `package.json` with **Install package from disk**.
3. For presets with audio layers, add an EasyFeedbacks `AudioManager` to the scene.

## Setup

Add `CharacterStepController` and a movement source to the moving character. The step controller finds a source on the same object at startup. You can also assign one explicitly.

- For a `NavMeshAgent`, use `NavMeshFootstepSource`. It samples the agent's actual velocity while on the mesh and suspends the cycle on off-mesh links.
- For a built-in `CharacterController`, use `CharacterControllerFootstepSource`. Set its reference speed to the character's normal top speed. Both built-in sources expose `StateId` for choosing a configured state.
- For another motor, derive a component from `FootstepMotionSource`. Return true from `TryGetGroundedMotion` while grounded and supply planar speed in metres per second, reference speed, movement mode and the ground collider.

Assign a default `FeedbackPreset` and any physics material overrides. A `FootstepSurface` on the ground collider or a parent takes priority over material entries. Unmatched surfaces use the default preset. For a NavMesh baked without physics colliders, the default preset is used.

In **Step Timing → Movement States**, use **+** to add a row, expand it and set a unique **State ID**. Each row controls step distance in metres, feedback volume, head-bob amplitude and movement amount for explicitly triggered steps. Remove unused states with **−**. Walk (1.95 m), Sprint (2.25 m) and Crouch (1.4 m) are the starting values. Upgrading from 1.0 preserves existing values and prefab overrides.

For example, add a state named `Patrol` with a step distance of 2.6 m and set `navMeshSource.StateId = "Patrol"` from the AI when it starts patrolling. Custom motors pass the ID to `new FootstepMotion(speed, maxSpeed, stateId, ground)`. IDs match without regard to case. When a requested state is missing, the first named row supplies the settings. An empty list disables all steps.

Use `CurrentStateId` and `StepEvent.StateId` to read the resolved ID, or call `PlayExternalStep("Patrol", -1)` for an explicit left step. The 1.0 enum API still supports its three built-in names and reports `MovementMode.Custom` for other states.

Assign a child transform to **Head Bob Target** to apply the step curves. For animation-driven timing, select **External Events** and call `TriggerLeftStep` or `TriggerRightStep` from animation events on the character. Subscribe to `Stepped` for the movement mode, foot side, speed and surface on each step.

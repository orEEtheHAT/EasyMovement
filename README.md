# EasyFootsteps

Distance-based and animation-driven footsteps for Unity 6.3. Steps can play EasyFeedbacks presets and move an optional head-bob target.

## Installation

1. In Package Manager, install EasyFeedbacks from `https://github.com/orEEtheHAT/Easy-Feedbacks.git#v1.7.0`.
2. Install EasyFootsteps from its Git URL or select its `package.json` with **Install package from disk**.
3. For presets with audio layers, add an EasyFeedbacks `AudioManager` to the scene.

## Setup

Add `CharacterStepController` and a movement source to the moving character. The step controller finds a source on the same object at startup. You can also assign one explicitly.

- For a `NavMeshAgent`, use `NavMeshFootstepSource`. It samples the agent's actual velocity while on the mesh and suspends the cycle on off-mesh links.
- For a built-in `CharacterController`, use `CharacterControllerFootstepSource`. Set its reference speed to the character's normal top speed. Both built-in sources expose a `Mode` property for walk, sprint and crouch.
- For another motor, derive a component from `FootstepMotionSource`. Return true from `TryGetGroundedMotion` while grounded and supply planar speed in metres per second, reference speed, movement mode and the ground collider.

Assign a default `FeedbackPreset` and any physics material overrides. A `FootstepSurface` on the ground collider or a parent takes priority over material entries. Unmatched surfaces use the default preset. For a NavMesh baked without physics colliders, the default preset is used.

In distance mode, the initial walk, sprint and crouch step lengths are 1.95, 2.25 and 1.4 metres. Assign a child transform to **Head Bob Target** to apply the step curves. For animation-driven timing, select **External Events** and call `TriggerLeftStep` or `TriggerRightStep` from animation events on the character. Subscribe to `Stepped` for the movement mode, foot side, speed and surface on each step.

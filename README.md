# EasyMovement

EasyMovement combines a first-person player for Easy Character Movement 2 (ECM2) with reusable footsteps for players and NPCs. The two modules communicate through a movement source. A NavMesh NPC uses the footsteps module directly.

## Installation

1. In Unity 6.3, import your licensed **Easy Character Movement 2** from the Unity Asset Store. Its `ECM2` runtime assembly is required to compile the first-person module. The package was tested with ECM2 1.4.3, without its examples.
2. In Package Manager, use **Install package from git URL** to install EasyFeedbacks from `https://github.com/orEEtheHAT/Easy-Feedbacks.git#v1.7.0`.
3. Install `https://github.com/orEEtheHAT/EasyMovement.git#v2.0.0`. Set **Active Input Handling** to **Input System Package (New)** or **Both**.

Upgrading from standalone EasyFootsteps 1.x? Replace its project manifest entry with `"com.oreethehat.easy-movement": "https://github.com/orEEtheHAT/EasyMovement.git#v2.0.0"` in the same edit. Existing footsteps scripts keep their GUIDs, namespace and assembly name. Both distributions contain these components, so install only one.

## First-person player

Drag **FirstPerson/Prefabs/First Person Player.prefab** into a scene with a floor collider. The prefab includes an ECM2 motor, a camera and AudioListener, a Cinemachine brain, three views, a movement source and a step controller. Use one active output camera/listener. A scene that already has its own output camera can use that camera and brain instead.

The supplied **First Person Controls** input asset supports WASD and mouse, Space to jump, Shift to sprint, and C or Ctrl to crouch. Gamepad bindings include both sticks, south button to jump, left stick press to sprint and east button to crouch. To use another input asset, assign it to the input component and select the map containing `Move`, `Look`, `Jump`, `Sprint` and `Crouch`. The player consumes the ECM2 public API and does not need the ECM2 example input component.

The starter uses speeds of 5 m/s for walking, 8 m/s for sprinting, and 2.5 m/s for crouching. The sprint view uses a 65° field of view, and the crouch view lowers the camera by 0.65 m. Select **EcmFootstepSource** to set the footstep IDs used for each gait. For other gait rules, override `GetGroundedStateId` in your own subclass.

Assign your EasyFeedbacks presets on the step controller. For sound, place one EasyFeedbacks `AudioManager` at the scene root. Feedback assets, surface materials and recordings belong to the consuming project.

## Footsteps on other characters

For a NavMesh NPC, add **NavMeshFootstepSource** and **CharacterStepController** to the object with the `NavMeshAgent`. When the AI changes gait, set the source's `StateId`, for example `source.StateId = "Patrol";`. The source samples actual velocity and suspends steps while traversing off-mesh links. The NPC only needs the NavMesh components and the footsteps module.

The step controller finds a movement source on the same object by default. You can also assign a specific source in the inspector. If an older EasyFootsteps prefab used Unity's built-in character motor, switch it to an ECM2 or NavMesh source when migrating to 2.0.

Assign a default `FeedbackPreset` and any physics material overrides. A `FootstepSurface` on the ground collider or a parent takes priority over material entries. Unmatched surfaces use the default preset. For a NavMesh baked without physics colliders, the default preset is used.

In **Step Timing → Movement States**, use **+** to add a row, expand it and set a unique **State ID**. Each row controls step distance in metres, feedback volume, head-bob amplitude and movement amount for explicitly triggered steps. Remove unused states with **−**. Walk (1.95 m), Sprint (2.25 m) and Crouch (1.4 m) are the starting values. Upgrading from 1.0 preserves existing values and prefab overrides.

For example, add a state named `Patrol` with a step distance of 2.6 m and set `navMeshSource.StateId = "Patrol"` from the AI when it starts patrolling. Custom motors pass the ID to `new FootstepMotion(speed, maxSpeed, stateId, ground)`. IDs match without regard to case. When a requested state is missing, the first named row supplies the settings. An empty list disables all steps.

Use `CurrentStateId` and `StepEvent.StateId` to read the resolved ID, or call `PlayExternalStep("Patrol", -1)` for an explicit left step. The 1.0 enum API still supports its three built-in names and reports `MovementMode.Custom` for other states.

Assign a child transform to **Head Bob Target** to apply the step curves. For animation-driven timing, select **External Events** and call `TriggerLeftStep` or `TriggerRightStep` from animation events on the character. Subscribe to `Stepped` for the movement mode, foot side, speed and surface on each step.

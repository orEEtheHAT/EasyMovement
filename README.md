# EasyMovement

EasyMovement combines a first-person player for Easy Character Movement 2 (ECM2) with reusable footsteps for players and NPCs. The player provides its movement directly to the step controller. NPCs use a NavMesh source.

## Installation

1. In Unity 6.3, import your licensed **Easy Character Movement 2** from the Unity Asset Store. Its `ECM2` runtime assembly is required to compile the first-person module. The package was tested with ECM2 1.4.3, without its examples.
2. In Package Manager, use **Install package from git URL** to install EasyFeedbacks from `https://github.com/orEEtheHAT/Easy-Feedbacks.git#v1.7.0`.
3. Install `https://github.com/orEEtheHAT/EasyMovement.git#v4.0.1`. Set **Active Input Handling** to **Input System Package (New)** or **Both**.

Upgrading from standalone EasyFootsteps 1.x? Replace its project manifest entry with `"com.oreethehat.easy-movement": "https://github.com/orEEtheHAT/EasyMovement.git#v4.0.1"` in the same edit. Existing footsteps scripts keep their GUIDs, namespace and assembly name. Both distributions contain these components, so install only one.

Upgrading a custom 2.0 player? Before updating, note any customized gait IDs and ground probe depth from its `EcmFootstepSource`. Remove that component or its missing script entry and transfer the gait IDs to **First Person Character Controller → Movement States** and the probe depth to **ECM2 Motor and Ground Settings**. Steps automatically find a controller on the same object. For a separate source object, assign its player controller under **Motion Source**. The package sample keeps its asset GUID, including for prefab variants. Custom movement sources can implement `IFootstepMotionSource`.

## First-person player

In **Packages → EasyMovement → FirstPerson → Prefabs**, place **FP_MainCamera_Sample** in the scene once. It contains the tagged Unity camera, AudioListener and Cinemachine brain. A scene that already has these on its output camera can use that instead. Then place or spawn **FP_Player_Sample** at the desired position and rotation on a floor collider. Its three Cinemachine views follow the player's **Look Pivot** and drive the scene camera through the brain. Respawning the player keeps the scene camera alive.

Movement uses the player's local forward and right. Mouse and stick look turn the body horizontally and tilt the eye-height **Look Pivot** vertically. Your gameplay and UI modes control cursor capture and visibility. When upgrading from 3.x, add a scene camera if you relied on the output inside the player. Existing look-target references migrate automatically.

The supplied **First Person Controls** input asset supports WASD and mouse, Space to jump, Shift to sprint, and C or Ctrl to crouch. Gamepad bindings include both sticks, south button to jump, left stick press to sprint and east button to crouch. To use another input asset, assign it to the input component and select the map containing `Move`, `Look`, `Jump`, `Sprint` and `Crouch`. The player consumes the ECM2 public API and does not need the ECM2 example input component.

The starter collides with the Default layer and inherits platform translation, rotation and velocity. Adjust ECM2's collision mask for other ground layers. Its three player cameras accept Cinemachine impulses and use a 0.5-second Hard Out transition. The starter uses speeds of 5 m/s for walking, 8 m/s for sprinting, and 2.5 m/s for crouching. The sprint view uses a 65° field of view, and the crouch view lowers the camera by 0.65 m. Open **First Person Character Controller → Movement States**. Each gait shows its speed, camera and a dropdown populated from the character’s step states. You can turn sprint or crouch off here. Other ECM2 settings are under **ECM2 Motor and Ground Settings**.

To add a gait such as `Injured`, create its profile in **Character Step Controller → Movement States**, then add it under **Custom States** on the player. Choose its speed, step profile and optional camera. In Play Mode, use **Test State in Play Mode** to try it. Gameplay can request it with `TrySetCustomState("Injured")`. UnityEvents can call `SetCustomState(string)` and `ClearCustomState()`. Crouching takes priority. A requested custom state resumes on returning to upright ground movement, and clears when the controller is disabled. A custom state without a camera uses the walk camera.

Assign your EasyFeedbacks presets on the step controller. Under **Step Feedback → Step Sound Origin**, choose where a footstep preset plays. The sample assigns **Character/Footstep Origin** at the bottom of its capsule. Moving this child changes the playback point for distance-based, animation and explicit steps. The ground collider still selects the surface preset. An empty origin uses the step controller's transform. Place one EasyFeedbacks `AudioManager` at the scene root for audio. Feedback assets, surface materials and recordings belong to the consuming project.

## NoClip

With **Allow NoClip** enabled, press **F8** or click the **right gamepad stick** to toggle collision-free flight of the player body. The `ToggleNoclip` action is optional on project input maps. Move forward/back along the look direction, strafe with Move, ascend with Jump, descend with Crouch and boost with Sprint. **Flight Speed** and **Boost Multiplier** are on the player inspector.

Toggle again in open space to resume regular movement at the current position. In midair the character falls from there. If the capsule overlaps solid geometry, flight stays active until there is room to exit. Disabling the motor forcibly restores collisions. If blocked during disable, it first returns to the flight entry position. Use `TrySetNoclip(bool)` to check whether a transition succeeded, or bind `ToggleNoclip()` to a UnityEvent.

## Footsteps on other characters

For a NavMesh NPC, add **NavMeshFootstepSource** and **CharacterStepController** to the object with the `NavMeshAgent`. When the AI changes gait, set the source's `StateId`, for example `source.StateId = "Patrol";`. The source samples actual velocity and suspends steps while traversing off-mesh links. It probes for a surface on foot contact or an explicit ground query. Reading the step controller's `GroundCollider` multiple times in one motion sample reuses its first result. The NPC only needs the NavMesh components and the footsteps module.

The step controller finds a movement source on the same object by default. You can also assign a specific source in the inspector. If an older EasyFootsteps prefab used Unity's built-in character motor, switch it to an ECM2 or NavMesh source when migrating to 2.0.

Assign a default `FeedbackPreset` and any physics material overrides. A `FootstepSurface` on the ground collider or a parent takes priority over material entries. Unmatched surfaces use the default preset. For a NavMesh baked without physics colliders, the default preset is used.

In **Step Timing → Movement States**, use **+** to add a row, expand it and set a unique **State ID**. Each row controls step distance in metres, feedback volume, head-bob amplitude and movement amount for explicitly triggered steps. Remove unused states with **−**. Walk (1.95 m), Sprint (2.25 m) and Crouch (1.4 m) are the starting values. Upgrading from 1.0 preserves existing values and prefab overrides.

For example, add a state named `Patrol` with a step distance of 2.6 m and set `navMeshSource.StateId = "Patrol"` from the AI when it starts patrolling. Custom motors pass the ID to `new FootstepMotion(speed, maxSpeed, stateId, ground)`. IDs match without regard to case. When a requested state is missing, the first named row supplies the settings. An empty list disables all steps.

Use `CurrentStateId` and `StepEvent.StateId` to read the resolved ID, or call `PlayExternalStep("Patrol", -1)` for an explicit left step. The 1.0 enum API still supports its three built-in names and reports `MovementMode.Custom` for other states.

Assign a child transform to **Head Bob Target** to apply the step curves. After the camera settles, the step controller releases the target and uses its current position as the starting point for the next walk. For animation-driven timing, select **External Events** and call `TriggerLeftStep` or `TriggerRightStep` from animation events on the character. Subscribe to `Stepped` for the movement mode, foot side, speed and surface on each step.

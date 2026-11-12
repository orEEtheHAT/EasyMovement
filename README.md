# EasyMovement 5.1.0

EasyMovement is a first-person movement package for Unity 6.3 URP built on Easy Character Movement 2 (ECM2). It includes a player prefab, a separate Cinemachine scene camera, and surface-based footsteps for players and NavMesh NPCs. The footsteps use your EasyFeedbacks presets.

## Installation

1. Import **Easy Character Movement 2** from your Unity Asset Store purchases. EasyMovement uses the ECM2 runtime assembly. The player has been tested with ECM2 1.4.3.
2. Open **Window → Package Manager → + → Install package from git URL** and install EasyFeedbacks:
   ```text
   https://github.com/orEEtheHAT/Easy-Feedbacks.git#v1.8.0
   ```
3. Use the same command to install EasyMovement:
   ```text
   https://github.com/orEEtheHAT/EasyMovement.git#v5.1.0
   ```
4. Set **Active Input Handling** to **Input System Package (New)** or **Both**. EasyMovement declares URP 17.3.0 as a dependency. Select a URP asset in the project's Graphics settings or the active Quality level.

## First scene

Find the prefabs under **Packages → EasyMovement → FirstPerson → Prefabs**.

- **FP_MainCamera_Sample** is the scene's output camera, with an AudioListener and a Cinemachine brain. Place it once per local view. If your scene already has a configured output camera, you can use it instead. The sample has post-processing and high-quality temporal anti-aliasing enabled. Its **FP_MainCamera_BlenderSettings** asset starts empty, so the brain uses a **Hard Out** blend of **0.5 s**.
- **FP_Player_Sample** can be placed or spawned on a floor collider on the **Default** layer. Its three Cinemachine views follow the player's **Look Pivot**. Spawning a new player leaves the scene camera in place. Adjust the ECM2 collision mask if your ground uses another layer.
- For footstep audio, place one EasyFeedbacks **AudioManager** at the scene root and assign a `FeedbackPreset` in **Character Step Controller → Step Feedback → Default Feedback**. The sample's **Step Sound Origin** is a child at the bottom of the capsule.

## Player controls and states

The **First Person Controls** input asset supports WASD and mouse, Space to jump, Shift to sprint, and C or Ctrl to crouch. On a gamepad, use the left stick to move, the right stick to look, the south button to jump, the east button to crouch and the left stick press to sprint. You can assign your own input asset and select a map containing `Move`, `Look`, `Jump`, `Sprint` and `Crouch`. Movement follows the player's body. Gameplay and UI modes control cursor capture.

In **First Person Character Controller → Movement States**, each gait has a speed, camera, and step profile. The sample walks at **5 m/s**, sprints at **8 m/s**, and crouches at **2.5 m/s**. The sprint view uses a 65° field of view, and the crouch view lowers the camera by 0.65 m. Sprint and crouch can be disabled independently. The player inherits platform translation, rotation, and velocity. **ECM2 Motor and Ground Settings** exposes the other motor settings.

To add a gait such as `Injured`, first create it under **Character Step Controller → Movement States**. Then add it under **Custom States** on the player and choose its speed, step profile, and optional camera. Try it in Play Mode with **Test State in Play Mode**. Gameplay can call `TrySetCustomState("Injured")`. UnityEvents can call `SetCustomState(string)` and `ClearCustomState()`. Crouching takes priority, and the custom gait resumes when the player returns to upright ground movement. Disabling the controller clears it. A custom state without an assigned camera uses the walk camera.

## Scripted movement and input blocks

An interaction, cutscene or UI adapter can temporarily take over from manual input:

```csharp
IDisposable inputBlock = character.AcquireInputBlock(this);
// Your script can still call SetMovementDirection, SetRotation and other motor methods.
// Stop your scripted movement when appropriate, then give manual input back:
inputBlock.Dispose();
```

Store the handle for the whole operation, including coroutines, and dispose it in the owner's cleanup. Each handle is independent: releasing one cannot release another operation's block. Disposing twice is harmless. The controller exposes `InputBlocked` and `InputBlockedChanged` for custom readers. Check the property in both your frame update and every input callback; do not write a zero movement vector while blocked. Acquire the block **before** sending scripted movement or button requests. A block does not stop the motor or clear its current direction or velocity; the controlling script decides how to move or stop it.

The built-in reader releases its own Jump/Crouch/Sprint requests once when blocked. Blocked callbacks and disabling a blocked reader do not cancel commands subsequently issued by a script. Move and Look resume immediately after the last block ends. Buttons held across a block, controller disable or reader reconnect must be released and pressed again, including NoClip ascent/descent and toggle. Existing NoClip mode is preserved by input blocks.

Destroyed owners are removed automatically. A disabled `Behaviour`, inactive `GameObject`, or component on an inactive object also expires on the next controller update or `InputBlocked` read. For other `UnityEngine.Object` owners, destruction is the lifetime boundary. Controller disable clears every handle; after pooling, acquire a new handle. Acquiring with an inactive owner/controller returns an empty handle; a null or destroyed owner is an argument error. Derived controllers that implement `Update` must call `base.Update()` to retain automatic owner cleanup when there are no input readers.

## Capturing a player's pose

`CaptureState()` returns a serializable `FirstPersonCharacterState`. Use it with your game's save system:

```csharp
FirstPersonCharacterState saved = character.CaptureState();
// Optional: JsonUtility.ToJson(saved), and FromJson<FirstPersonCharacterState>(json).

using (character.AcquireInputBlock(this))
{
    if (!character.TryRestoreState(saved, out string error))
        Debug.LogWarning(error);
}
```

The snapshot contains world position and rotation (including pending yaw), local view pitch, crouch/sprint requests and the configured custom gait ID. Restore validates the complete snapshot before changing the character. It teleports into normal grounded/falling movement, stops jump, velocity and accumulated forces, clears pending rotation input, preserves existing input blocks and reapplies the gait requests. Crouch still respects capsule clearance, and sprint becomes active when there is movement. Enable the controller and exit NoClip before restoring. Pause other movement providers during restore and choose a saved position valid for the current level.

This is a pose and gait snapshot, not a full physics replay: velocity, platforms, swimming/flight modes, active interactions, input blocks and input sensitivity are not saved. There is no automatic storage or global save manager. EasyInput owns player input settings. Unknown custom gait IDs, unsupported versions, invalid numeric values and unsupported gait capabilities produce a readable error without a partial restore.

## NoClip

With **Allow NoClip** enabled, **F8** or a click of the **right gamepad stick** toggles collision-free flight. The `ToggleNoclip` action is optional in your own input maps. Move forward or back along the look direction, strafe with Move, ascend with Jump, descend with Crouch and boost with Sprint. Tune **Flight Speed** and **Boost Multiplier** in the player inspector.

Toggle again in open space to resume regular movement at the current position. In midair the character falls from there. If its capsule overlaps solid geometry, flight stays active until there is room to exit. Disabling the motor restores collisions and uses the flight entry position if the current position is blocked. Code can call `TrySetNoclip(bool)` to check whether a transition succeeded. Use `ToggleNoclip()` for a UnityEvent.

## Footsteps

EasyFeedbacks **1.8.0+** is required. For player-owned footstep output, assign an active `IFeedbackContextProvider` component to **Character Step Controller → Context Provider**. For example, import EasyInput **0.2.0+**'s **EasyFeedbacks** sample, add `PlayerFeedbackContext` with this player's `PlayerInputSettings`, and assign it here. EasyInput remains optional. The provider receives the complete step context, including authored volume and position; its returned explicit gamepad or `WithoutGamepad()` choice is preserved.

An empty, invalid, disabled or destroyed provider retains legacy playback, including the `Gamepad.current` fallback. The inspector explains invalid or disabled assignments. Keep the provider active when player-specific routing is required; the step controller does not create managers or store feedback preferences.

For a NavMesh NPC, add **NavMeshFootstepSource** and **CharacterStepController** to the object with the `NavMeshAgent`. The step controller finds a movement source on the same object by default. For a source on another object, assign it in **Motion Source**. The NavMesh source uses the agent's actual speed, suspends steps on off-mesh links, and probes for a surface at foot contact or when you query the ground collider.

Use **+** under **Step Timing → Movement States** to add a named profile. Each profile defines the step distance, feedback volume, head-bob multiplier, and movement amount for explicit steps. The starting distances are **1.95 m** for Walk, **2.25 m** for Sprint and **1.4 m** for Crouch. For example, add `Patrol` with a distance of 2.6 m and set `navMeshSource.StateId = "Patrol"` when the AI starts patrolling. Matching is case-insensitive. A missing state uses the first named profile. An empty list disables footsteps.

Assign a default `FeedbackPreset` and optional physics material overrides. A **FootstepSurface** on the ground collider or its parent takes priority over material entries. Unmatched surfaces and a NavMesh without ground colliders use the default preset. Moving the **Step Sound Origin** changes where the feedback plays. The ground collider still determines the surface. An unassigned origin uses the step controller's transform.

Assign a child transform to **Head Bob Target** to apply the step curves. Once the camera settles, the next walk starts from the target's current position. For animation-driven timing, select **External Events** and call `TriggerLeftStep` or `TriggerRightStep` from animation events. For an explicit step, call `PlayExternalStep("Patrol", -1)`. Subscribe to `Stepped` for the state ID, foot side, speed and surface. `CurrentStateId` and `StepEvent.StateId` expose the resolved state.

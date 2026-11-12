using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EasyMovement.FirstPerson
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FirstPersonCharacterController))]
    public sealed class FirstPersonCharacterControllerInput : MonoBehaviour
    {
        [SerializeField] private FirstPersonCharacterController _character;
        [SerializeField] private InputActionAsset _inputActionsAsset;
        [SerializeField] private string actionMap = "Player";

        [Header("Look")]
        public bool invertLook = true;
        public Vector2 sensitivity = new Vector2(0.08f, 0.08f);
        [Min(0f)] public float gamepadLookSpeed = 160f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        private InputAction move, look, jump, crouch, sprint, toggleNoclip;
        private readonly List<InputAction> enabledHere = new List<InputAction>(6);
        private bool holdingJump, holdingCrouch, holdingSprint;
        private bool waitForJumpRelease, waitForCrouchRelease, waitForSprintRelease, waitForNoclipRelease;
        private bool wroteMovement;
        private Vector3 lastMovement;

        private bool CanReadInput => isActiveAndEnabled && _character &&
                                     _character.isActiveAndEnabled && !_character.InputBlocked;

        public FirstPersonCharacterController firstPersonCharacter => _character;
        public InputAction lookInputAction => look;
        public InputAction sprintInputAction => sprint;
        public InputActionAsset inputActionsAsset
        {
            get => _inputActionsAsset;
            set
            {
                if (_inputActionsAsset == value) return;
                Disconnect();
                _inputActionsAsset = value;
                if (isActiveAndEnabled) Connect();
            }
        }

        private void Awake()
        {
            if (!_character) _character = GetComponent<FirstPersonCharacterController>();
        }

        private void OnEnable() => Connect();
        private void OnDisable() => Disconnect();

        private void Connect()
        {
            if (_character)
            {
                _character.InputBlockedChanged += OnInputBlockedChanged;
                _character.ManualInputReset += OnManualInputReset;
            }
            var map = _inputActionsAsset ? _inputActionsAsset.FindActionMap(actionMap) : null;
            if (map == null) return;
            move = map.FindAction("Move");
            look = map.FindAction("Look");
            jump = map.FindAction("Jump");
            crouch = map.FindAction("Crouch");
            sprint = map.FindAction("Sprint");
            toggleNoclip = map.FindAction("ToggleNoclip");
            if (toggleNoclip != null)
            {
                toggleNoclip.performed += OnNoclipToggle;
                toggleNoclip.canceled += OnNoclipCanceled;
                EnableIfNeeded(toggleNoclip);
            }
            Listen(jump);
            Listen(crouch);
            Listen(sprint);
            EnableIfNeeded(move);
            EnableIfNeeded(look);
            // Initial-state callbacks must not replay a button already held while this reader was disabled.
            waitForJumpRelease = waitForCrouchRelease = waitForSprintRelease = waitForNoclipRelease = true;
        }

        private void Listen(InputAction action)
        {
            if (action == null) return;
            action.started += OnButton;
            action.canceled += OnButton;
            EnableIfNeeded(action);
        }

        private void EnableIfNeeded(InputAction action)
        {
            if (action == null || action.enabled) return;
            action.Enable();
            enabledHere.Add(action);
        }

        private void Unlisten(InputAction action)
        {
            if (action == null) return;
            action.started -= OnButton;
            action.canceled -= OnButton;
        }

        private void Disconnect()
        {
            if (_character)
            {
                _character.InputBlockedChanged -= OnInputBlockedChanged;
                _character.ManualInputReset -= OnManualInputReset;
            }
            Unlisten(jump);
            Unlisten(crouch);
            Unlisten(sprint);
            if (toggleNoclip != null)
            {
                toggleNoclip.performed -= OnNoclipToggle;
                toggleNoclip.canceled -= OnNoclipCanceled;
            }
            foreach (var action in enabledHere) action.Disable();
            enabledHere.Clear();
            ReleaseButtons();
            // A blocked reader never writes even zero: the active scripted provider owns the motor.
            // Outside a handoff, clear only a direction that still matches this reader's last write.
            if (_character && !_character.InputBlocked && wroteMovement &&
                _character.GetMovementDirection() == lastMovement)
                _character.SetMovementDirection(Vector3.zero);
            wroteMovement = false;
            move = look = jump = crouch = sprint = toggleNoclip = null;
        }

        private void OnNoclipToggle(InputAction.CallbackContext context)
        {
            if (CanReadInput && !waitForNoclipRelease) _character.ToggleNoclip();
        }

        private void OnNoclipCanceled(InputAction.CallbackContext context) => waitForNoclipRelease = false;

        private void OnInputBlockedChanged(bool blocked)
        {
            if (blocked)
            {
                ReleaseButtons();
                wroteMovement = false;
            }
            RequireHeldButtonsToRelease();
        }

        private void OnManualInputReset()
        {
            ReleaseButtons();
            wroteMovement = false;
            RequireHeldButtonsToRelease();
        }

        private void RequireHeldButtonsToRelease()
        {
            waitForJumpRelease = jump?.IsPressed() == true;
            waitForCrouchRelease = crouch?.IsPressed() == true;
            waitForSprintRelease = sprint?.IsPressed() == true;
            waitForNoclipRelease = toggleNoclip?.IsPressed() == true;
        }

        private void ReleaseButtons()
        {
            if (_character)
            {
                if (holdingJump) _character.StopJumping();
                if (holdingCrouch) _character.UnCrouch();
                if (holdingSprint) _character.StopSprinting();
            }
            holdingJump = holdingCrouch = holdingSprint = false;
        }

        private void OnButton(InputAction.CallbackContext context)
        {
            bool pressed = context.started;
            if (!CanReadInput)
            {
                // Track the physical release, but never cancel a scripted provider's button intents.
                if (context.action == jump) waitForJumpRelease = pressed;
                else if (context.action == crouch) waitForCrouchRelease = pressed;
                else if (context.action == sprint) waitForSprintRelease = pressed;
                return;
            }
            if (context.action == jump)
            {
                if (waitForJumpRelease) { if (!pressed) waitForJumpRelease = false; return; }
                if (!pressed && !holdingJump) return;
                holdingJump = pressed && !_character.IsNoclipActive;
                if (_character.IsNoclipActive) return;
                if (pressed) _character.Jump(); else _character.StopJumping();
            }
            else if (context.action == crouch)
            {
                if (waitForCrouchRelease) { if (!pressed) waitForCrouchRelease = false; return; }
                if (!pressed && !holdingCrouch) return;
                holdingCrouch = pressed && !_character.IsNoclipActive;
                if (_character.IsNoclipActive) return;
                if (pressed) _character.Crouch(); else _character.UnCrouch();
            }
            else if (context.action == sprint)
            {
                if (waitForSprintRelease) { if (!pressed) waitForSprintRelease = false; return; }
                if (!pressed && !holdingSprint) return;
                holdingSprint = pressed;
                if (pressed) _character.Sprint(); else _character.StopSprinting();
            }
        }

        public Vector2 GetLookInput() => CanReadInput ? look?.ReadValue<Vector2>() ?? Vector2.zero : Vector2.zero;

        private void Update()
        {
            if (!CanReadInput)
            {
                ReleaseButtons();
                RequireHeldButtonsToRelease();
                wroteMovement = false;
                return;
            }
            if (jump?.IsPressed() != true) waitForJumpRelease = false;
            if (crouch?.IsPressed() != true) waitForCrouchRelease = false;
            if (sprint?.IsPressed() != true) waitForSprintRelease = false;
            if (toggleNoclip?.IsPressed() != true) waitForNoclipRelease = false;
            Vector2 axes = move?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 delta = GetLookInput();
            if (look?.activeControl?.device is Gamepad)
                delta *= gamepadLookSpeed * Time.deltaTime;
            else
                delta = Vector2.Scale(delta, sensitivity);
            _character.AddControlYawInput(delta.x);
            _character.AddControlPitchInput(invertLook ? delta.y : -delta.y, minPitch, maxPitch);

            Transform body = _character.transform;
            if (_character.IsNoclipActive)
            {
                Transform aim = _character.LookPivot ? _character.LookPivot : body;
                float vertical = (!waitForJumpRelease && jump?.IsPressed() == true ? 1f : 0f) -
                                 (!waitForCrouchRelease && crouch?.IsPressed() == true ? 1f : 0f);
                lastMovement = body.right * axes.x + aim.forward * axes.y + body.up * vertical;
            }
            else
            {
                lastMovement = body.TransformDirection(new Vector3(axes.x, 0f, axes.y));
            }
            _character.SetMovementDirection(lastMovement);
            wroteMovement = true;
        }
    }
}

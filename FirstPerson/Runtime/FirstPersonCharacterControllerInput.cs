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
        private readonly List<InputAction> enabledHere = new List<InputAction>(5);
        private bool holdingJump, holdingCrouch, holdingSprint;

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
                EnableIfNeeded(toggleNoclip);
            }
            Listen(jump);
            Listen(crouch);
            Listen(sprint);
            EnableIfNeeded(move);
            EnableIfNeeded(look);
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
            Unlisten(jump);
            Unlisten(crouch);
            Unlisten(sprint);
            if (toggleNoclip != null) toggleNoclip.performed -= OnNoclipToggle;
            foreach (var action in enabledHere) action.Disable();
            enabledHere.Clear();
            if (_character)
            {
                if (holdingJump) _character.StopJumping();
                if (holdingCrouch) _character.UnCrouch();
                if (holdingSprint) _character.StopSprinting();
                _character.SetMovementDirection(Vector3.zero);
            }
            holdingJump = holdingCrouch = holdingSprint = false;
            move = look = jump = crouch = sprint = toggleNoclip = null;
        }

        private void OnNoclipToggle(InputAction.CallbackContext context)
        {
            if (_character && _character.isActiveAndEnabled) _character.ToggleNoclip();
        }

        private void OnButton(InputAction.CallbackContext context)
        {
            if (!_character) return;
            bool pressed = context.started;
            if (context.action == jump)
            {
                holdingJump = pressed && !_character.IsNoclipActive;
                if (_character.IsNoclipActive) return;
                if (pressed) _character.Jump(); else _character.StopJumping();
            }
            else if (context.action == crouch)
            {
                holdingCrouch = pressed && !_character.IsNoclipActive;
                if (_character.IsNoclipActive) return;
                if (pressed) _character.Crouch(); else _character.UnCrouch();
            }
            else if (context.action == sprint)
            {
                holdingSprint = pressed;
                if (pressed) _character.Sprint(); else _character.StopSprinting();
            }
        }

        public Vector2 GetLookInput() => look?.ReadValue<Vector2>() ?? Vector2.zero;

        private void Update()
        {
            if (!_character || !_character.isActiveAndEnabled) return;
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
                float vertical = (jump?.IsPressed() == true ? 1f : 0f) - (crouch?.IsPressed() == true ? 1f : 0f);
                _character.SetMovementDirection(body.right * axes.x + aim.forward * axes.y + body.up * vertical);
            }
            else
            {
                _character.SetMovementDirection(body.TransformDirection(new Vector3(axes.x, 0f, axes.y)));
            }
        }
    }
}

using My.Scripts.Core.Utility;
using My.Scripts.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace My.Scripts.Input
{
    public class GameInput : Singleton<GameInput>
    {
        #region Private Fields

        private InputActions _inputActions;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            InitializeInputActions();
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupInputActions();
        }

        #endregion

        #region Public Methods Ч Input State

        public bool IsUpActionPressed()
        {
            return _inputActions?.Player.LanderUp.IsPressed() ?? false;
        }

        public bool IsLeftActionPressed()
        {
            return _inputActions?.Player.LanderLeft.IsPressed() ?? false;
        }

        public bool IsRightActionPressed()
        {
            return _inputActions?.Player.LanderRight.IsPressed() ?? false;
        }

        public bool IsRestartActionPressed()
        {
            return _inputActions?.Player.RestartLevel.IsPressed() ?? false;
        }

        public Vector2 GetMovementInputVector2()
        {
            return _inputActions?.Player.Movement.ReadValue<Vector2>() ?? Vector2.zero;
        }

        #endregion

        #region Public Methods Ч Input Control

        public void EnableInput()
        {
            _inputActions?.Enable();
        }

        public void DisableInput()
        {
            _inputActions?.Disable();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void InitializeInputActions()
        {
            _inputActions = new InputActions();

            // ѕодписываемс€ на событи€ кнопок
            _inputActions.Player.Menu.performed += OnMenuPerformed;
            _inputActions.Player.RestartLevel.performed += OnRestartPerformed;
        }

        private void CleanupInputActions()
        {
            if (_inputActions == null) return;

            // ќтписываемс€ от событий
            _inputActions.Player.Menu.performed -= OnMenuPerformed;
            _inputActions.Player.RestartLevel.performed -= OnRestartPerformed;

            _inputActions.Disable();
            _inputActions.Dispose();
            _inputActions = null;
        }

        #endregion

        #region Private Methods Ч Input Callbacks

        private void OnMenuPerformed(InputAction.CallbackContext context)
        {
            EventManager.Instance?.Broadcast(GameEvents.MenuButtonPressed);
        }

        private void OnRestartPerformed(InputAction.CallbackContext context)
        {
            EventManager.Instance?.Broadcast(GameEvents.RestartButtonPressed);
        }

        #endregion
    }
}
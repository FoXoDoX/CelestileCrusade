using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public event EventHandler OnMenuButtonPressed;

    private InputActions inputActions;

    public void Awake()
    {
        Instance = this;

        inputActions = new InputActions();
        inputActions.Enable();

        inputActions.Player.Menu.performed += Menu_performed;
    }

    private void Menu_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnMenuButtonPressed?.Invoke(this, EventArgs.Empty);
    }

    private void OnDestroy()
    {
        inputActions.Disable();
    }

    public bool IsUpActionPressed()
    {
        return inputActions.Player.LanderUp.IsPressed();
    }

    public bool IsLeftActionPressed()
    {
        return inputActions.Player.LanderLeft.IsPressed();
    }

    public bool IsRightActionPressed()
    {
        return inputActions.Player.LanderRight.IsPressed();
    }

    public bool IsRestartActionPressed()
    {
        return inputActions.Player.RestartLevel.IsPressed();
    }

    public Vector2 GetMovementInputVector2()
    {
        return inputActions.Player.Movement.ReadValue<Vector2>();
    }
}

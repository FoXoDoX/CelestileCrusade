using My.Scripts.Core.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace My.Scripts.UI
{
    /// <summary>
    /// Восстанавливает последний выбранный UI-элемент только при нажатии
    /// навигационных клавиш (стрелки, WASD, геймпад), а НЕ каждый кадр.
    /// Живёт между сценами.
    /// </summary>
    public class UIButtonSelectionRestorer : PrivatePersistentSingleton<UIButtonSelectionRestorer>
    {
        private GameObject _lastSelected;

        private void Update()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            var current = eventSystem.currentSelectedGameObject;

            // Запоминаем последний валидный выбранный элемент
            if (current != null && current.activeInHierarchy)
            {
                _lastSelected = current;
                return;
            }

            // При смене сцены старые объекты уничтожаются —
            // Unity's == null вернёт true для destroyed объектов
            if (_lastSelected == null) return;

            // Ничего не выбрано — восстанавливаем только по навигационному вводу
            if (_lastSelected.activeInHierarchy && IsNavigationInputPressed())
            {
                eventSystem.SetSelectedGameObject(_lastSelected);
            }
        }

        private bool IsNavigationInputPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.wasPressedThisFrame ||
                    keyboard.aKey.wasPressedThisFrame ||
                    keyboard.sKey.wasPressedThisFrame ||
                    keyboard.dKey.wasPressedThisFrame ||
                    keyboard.upArrowKey.wasPressedThisFrame ||
                    keyboard.downArrowKey.wasPressedThisFrame ||
                    keyboard.leftArrowKey.wasPressedThisFrame ||
                    keyboard.rightArrowKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.dpad.up.wasPressedThisFrame ||
                    gamepad.dpad.down.wasPressedThisFrame ||
                    gamepad.dpad.left.wasPressedThisFrame ||
                    gamepad.dpad.right.wasPressedThisFrame ||
                    gamepad.leftStick.up.wasPressedThisFrame ||
                    gamepad.leftStick.down.wasPressedThisFrame ||
                    gamepad.leftStick.left.wasPressedThisFrame ||
                    gamepad.leftStick.right.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
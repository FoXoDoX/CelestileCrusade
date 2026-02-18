using My.Scripts.Core.Utility;
using UnityEngine;

public class CursorAutoHide : PersistentSingleton<CursorAutoHide>
{
    [SerializeField] private float _hideDelay = 3f;

    private float _lastMoveTime;
    private Vector2 _lastMousePosition;
    private bool _isHidden;

    private void Start()
    {
        _lastMoveTime = Time.unscaledTime;
        _lastMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        Vector2 currentMousePosition = Input.mousePosition;

        bool mouseMoved = currentMousePosition != _lastMousePosition;

        if (mouseMoved)
        {
            _lastMousePosition = currentMousePosition;
            _lastMoveTime = Time.unscaledTime;

            ShowCursor();
        }
        else if (!_isHidden && Time.unscaledTime - _lastMoveTime >= _hideDelay)
        {
            HideCursor();
        }
    }

    private void ShowCursor()
    {
        if (_isHidden)
        {
            Cursor.visible = true;
            _isHidden = false;
        }
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        _isHidden = true;
    }

    private void OnDisable()
    {
        // Гарантируем что курсор виден когда скрипт выключен
        Cursor.visible = true;
        _isHidden = false;
    }
}
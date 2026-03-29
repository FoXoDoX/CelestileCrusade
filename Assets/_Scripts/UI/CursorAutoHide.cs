using My.Scripts.EventBus;
using My.Scripts.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorAutoHide : MonoBehaviour
{
    [SerializeField] private float _hideDelay = 3f;

    private float _lastMoveTime;
    private Vector2 _lastMousePosition;
    private bool _isHidden;
    private int _suspendCount;
    private bool _isSubscribed;

    private bool IsSuspended => _suspendCount > 0;

    #region Unity Lifecycle

    private void Start()
    {
        _lastMousePosition = GetMousePosition();
        SubscribeToEvents();
        CheckInitialTutorialState();

        if (IsSuspended)
        {
            ShowCursor();
        }
        else
        {
            HideCursor();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        _isHidden = false;
    }

    private void Update()
    {
        if (IsSuspended) return;

        Vector2 currentMousePosition = GetMousePosition();
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

    #endregion

    #region Private Methods Ч Initialization

    private void CheckInitialTutorialState()
    {
        var tutorialManager = FindFirstObjectByType<TutorialManager>();
        if (tutorialManager != null && tutorialManager.IsTutorialActive)
        {
            _suspendCount++;
        }
    }

    #endregion

    #region Private Methods Ч Event Subscription

    private void SubscribeToEvents()
    {
        if (_isSubscribed) return;
        if (!EventManager.HasInstance) return;

        var em = EventManager.Instance;
        em.AddHandler(GameEvents.GamePaused, OnSuspend);
        em.AddHandler(GameEvents.GameUnpaused, OnResume);
        em.AddHandler(GameEvents.TutorialStarted, OnSuspend);
        em.AddHandler(GameEvents.TutorialCompleted, OnResume);

        _isSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!_isSubscribed) return;
        if (!EventManager.HasInstance) return;

        var em = EventManager.Instance;
        em.RemoveHandler(GameEvents.GamePaused, OnSuspend);
        em.RemoveHandler(GameEvents.GameUnpaused, OnResume);
        em.RemoveHandler(GameEvents.TutorialStarted, OnSuspend);
        em.RemoveHandler(GameEvents.TutorialCompleted, OnResume);

        _isSubscribed = false;
    }

    #endregion

    #region Private Methods Ч Event Handlers

    private void OnSuspend()
    {
        _suspendCount++;
        ShowCursor();
    }

    private void OnResume()
    {
        _suspendCount = Mathf.Max(0, _suspendCount - 1);

        if (!IsSuspended)
        {
            // —инхронизируем позицию, чтобы не было ложного "движени€"
            _lastMousePosition = GetMousePosition();
            _lastMoveTime = Time.unscaledTime;
        }
    }

    #endregion

    #region Private Methods Ч Cursor

    private Vector2 GetMousePosition()
    {
        if (Mouse.current == null) return Vector2.zero;
        return Mouse.current.position.ReadValue();
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

    #endregion
}
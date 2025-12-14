using My.Scripts.Core.Data;
using My.Scripts.Core.Utility;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Levels;
using My.Scripts.Gameplay.Player;
using My.Scripts.Input;
using Unity.Cinemachine;
using UnityEngine;

namespace My.Scripts.Gameplay.CameraUtility
{
    /// <summary>
    /// Управление камерой для предпросмотра уровня до начала игры.
    /// Позволяет перемещать и зумить камеру в пределах уровня.
    /// </summary>
    public class LevelPreviewCamera : MonoBehaviour
    {
        #region Constants

        private const float DEFAULT_ZOOM_SPEED = 5f;
        private const float DEFAULT_PAN_SPEED = 0.5f;
        private const float DEFAULT_MIN_ZOOM = 5f;
        private const float ZOOM_SMOOTHING = 10f;

        #endregion

        #region Serialized Fields

        [Header("References")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraTarget;

        [Header("Zoom Settings")]
        [SerializeField] private float _zoomSpeed = DEFAULT_ZOOM_SPEED;
        [SerializeField] private float _minZoom = DEFAULT_MIN_ZOOM;

        [Header("Pan Settings")]
        [SerializeField] private float _panSpeed = DEFAULT_PAN_SPEED;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogs = true;

        #endregion

        #region Private Fields

        private GameLevel _currentLevel;

        private float _maxZoom;
        private float _targetZoom;
        private Vector3 _targetPosition;
        private Vector3 _levelCenter;
        private Vector2 _levelBoundsHalfSize;

        // Сохраняем оригинальные настройки камеры
        private Transform _originalTrackingTarget;
        private float _originalOrthographicSize;

        private bool _isInitialized;
        private bool _isPreviewActive;

        #endregion

        #region Properties

        public bool IsPreviewActive => _isPreviewActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Log("Awake called");
            ValidateReferences();
        }

        private void OnEnable()
        {
            Log("OnEnable called");
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            Log("OnDisable called");
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (!_isPreviewActive) return;
            if (!GameInput.HasInstance) return;

            HandleZoomInput();
            HandlePanInput();
            UpdateCamera();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализирует превью камеру для указанного уровня.
        /// </summary>
        public void Initialize(GameLevel level)
        {
            Log($"Initialize called with level: {(level != null ? level.name : "NULL")}");

            if (level == null)
            {
                Debug.LogError($"[{nameof(LevelPreviewCamera)}] Level is null!");
                return;
            }

            _currentLevel = level;

            // Устанавливаем параметры уровня
            _maxZoom = level.GetZoomedOutOrthographicSize();
            _levelCenter = level.GetCameraStartTargetTransform().position;

            Log($"MaxZoom: {_maxZoom}, MinZoom: {_minZoom}, LevelCenter: {_levelCenter}");

            // Вычисляем границы для панорамирования
            CalculateLevelBounds();

            // Устанавливаем начальные значения
            _targetZoom = _maxZoom;
            _targetPosition = _levelCenter;

            // Позиционируем target
            if (_cameraTarget != null)
            {
                _cameraTarget.position = _targetPosition;
                Log($"CameraTarget position set to: {_targetPosition}");
            }

            // === КЛЮЧЕВОЕ ИЗМЕНЕНИЕ ===
            // Сохраняем оригинальные настройки и переключаем камеру на наш target
            if (_cinemachineCamera != null)
            {
                // Сохраняем оригинальный target
                _originalTrackingTarget = _cinemachineCamera.Target.TrackingTarget;
                _originalOrthographicSize = _cinemachineCamera.Lens.OrthographicSize;

                Log($"Saved original tracking target: {(_originalTrackingTarget != null ? _originalTrackingTarget.name : "NULL")}");
                Log($"Saved original orthographic size: {_originalOrthographicSize}");

                // Переключаем на наш preview target
                _cinemachineCamera.Target.TrackingTarget = _cameraTarget;
                _cinemachineCamera.Lens.OrthographicSize = _targetZoom;

                Log($"Switched camera to preview target: {_cameraTarget.name}");
                Log($"Set orthographic size to: {_targetZoom}");
            }

            _isInitialized = true;
            _isPreviewActive = true;

            Log($"Initialize complete. IsInitialized: {_isInitialized}, IsPreviewActive: {_isPreviewActive}");
        }

        /// <summary>
        /// Сбрасывает камеру в начальное положение.
        /// </summary>
        public void ResetToDefault()
        {
            if (!_isInitialized) return;

            _targetZoom = _maxZoom;
            _targetPosition = _levelCenter;
            Log("Reset to default");
        }

        #endregion

        #region Private Methods — Initialization

        private void ValidateReferences()
        {
            if (_cinemachineCamera == null)
            {
                _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
                Log($"CinemachineCamera found by FindFirstObjectByType: {(_cinemachineCamera != null ? _cinemachineCamera.name : "NULL")}");
            }

            if (_cameraTarget == null)
            {
                Log("Camera target not assigned. Creating one...");
                CreateCameraTarget();
            }
        }

        private void CreateCameraTarget()
        {
            GameObject targetObj = new GameObject("PreviewCameraTarget");
            _cameraTarget = targetObj.transform;

            if (_cinemachineCamera != null)
            {
                _cameraTarget.position = _cinemachineCamera.transform.position;
            }

            Log($"Created CameraTarget: {targetObj.name} at position {_cameraTarget.position}");
        }

        private void CalculateLevelBounds()
        {
            if (_currentLevel == null) return;

            float aspectRatio = GetAspectRatio();
            float height = _maxZoom;
            float width = height * aspectRatio;

            _levelBoundsHalfSize = new Vector2(width, height);
            Log($"Level bounds calculated. AspectRatio: {aspectRatio}, Bounds: {_levelBoundsHalfSize}");
        }

        private float GetAspectRatio()
        {
            if (Screen.height == 0) return 16f / 9f;
            return (float)Screen.width / Screen.height;
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToEvents()
        {
            EventManager.Instance?.AddHandler<LanderStateData>(
                GameEvents.LanderStateChanged,
                OnLanderStateChanged
            );
            Log("Subscribed to LanderStateChanged event");
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Instance?.RemoveHandler<LanderStateData>(
                GameEvents.LanderStateChanged,
                OnLanderStateChanged
            );
        }

        private void OnLanderStateChanged(LanderStateData data)
        {
            if (data.State != Lander.State.WaitingToStart)
            {
                EndPreview();
            }
        }

        /// <summary>
        /// Завершает превью и восстанавливает оригинальные настройки камеры.
        /// </summary>
        private void EndPreview()
        {
            if (!_isPreviewActive) return;

            _isPreviewActive = false;

            // === ВОССТАНАВЛИВАЕМ ОРИГИНАЛЬНЫЕ НАСТРОЙКИ ===
            if (_cinemachineCamera != null && _originalTrackingTarget != null)
            {
                _cinemachineCamera.Target.TrackingTarget = _originalTrackingTarget;
                Log($"Restored original tracking target: {_originalTrackingTarget.name}");
            }

            // Уничтожаем созданный target
            if (_cameraTarget != null && _cameraTarget.name == "PreviewCameraTarget")
            {
                Destroy(_cameraTarget.gameObject);
                Log("Destroyed preview camera target");
            }

            Log("Preview ended. Game started.");
        }

        #endregion

        #region Private Methods — Input Handling

        private void HandleZoomInput()
        {
            float scrollDelta = GameInput.Instance.GetCameraZoomInput();

            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                float oldZoom = _targetZoom;

                _targetZoom -= scrollDelta * _zoomSpeed * 0.5f;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);

                Log($"Zoom: {oldZoom:F2} -> {_targetZoom:F2} (delta: {scrollDelta:F2})");

                ClampTargetPosition();
            }
        }

        private void HandlePanInput()
        {
            bool isPanning = GameInput.Instance.IsCameraPanActivated();

            if (!isPanning) return;

            Vector2 panDelta = GameInput.Instance.GetCameraPanInput();

            if (panDelta.sqrMagnitude > 0.01f)
            {
                float currentZoom = _cinemachineCamera != null
                    ? _cinemachineCamera.Lens.OrthographicSize
                    : _targetZoom;

                float zoomFactor = currentZoom / _maxZoom;

                // Применяем движение напрямую (без Lerp)
                Vector3 panMovement = new Vector3(
                    -panDelta.x * _panSpeed * zoomFactor * 0.1f,
                    -panDelta.y * _panSpeed * zoomFactor * 0.1f,
                    0f
                );

                _targetPosition += panMovement;
                ClampTargetPosition();

                // === МГНОВЕННО ПРИМЕНЯЕМ ПОЗИЦИЮ ===
                if (_cameraTarget != null)
                {
                    _cameraTarget.position = _targetPosition;
                }
            }
        }

        #endregion

        #region Private Methods — Camera Control

        private void UpdateCamera()
        {
            if (_cinemachineCamera == null) return;
            if (_cameraTarget == null) return;

            // Плавно интерполируем только зум
            float currentZoom = _cinemachineCamera.Lens.OrthographicSize;
            float newZoom = Mathf.Lerp(currentZoom, _targetZoom, Time.deltaTime * ZOOM_SMOOTHING);
            _cinemachineCamera.Lens.OrthographicSize = newZoom;

            // Позиция обновляется мгновенно в HandlePanInput()
            // Но если pan не активен, всё равно синхронизируем позицию
            if (!GameInput.Instance.IsCameraPanActivated())
            {
                _cameraTarget.position = _targetPosition;
            }
        }

        private void ClampTargetPosition()
        {
            if (_currentLevel == null) return;

            float aspectRatio = GetAspectRatio();
            float viewHalfWidth = _targetZoom * aspectRatio;
            float viewHalfHeight = _targetZoom;

            float minX = _levelCenter.x - (_levelBoundsHalfSize.x - viewHalfWidth);
            float maxX = _levelCenter.x + (_levelBoundsHalfSize.x - viewHalfWidth);
            float minY = _levelCenter.y - (_levelBoundsHalfSize.y - viewHalfHeight);
            float maxY = _levelCenter.y + (_levelBoundsHalfSize.y - viewHalfHeight);

            if (minX > maxX)
            {
                _targetPosition.x = _levelCenter.x;
            }
            else
            {
                _targetPosition.x = Mathf.Clamp(_targetPosition.x, minX, maxX);
            }

            if (minY > maxY)
            {
                _targetPosition.y = _levelCenter.y;
            }
            else
            {
                _targetPosition.y = Mathf.Clamp(_targetPosition.y, minY, maxY);
            }
        }

        #endregion

        #region Private Methods — Debug

        private void Log(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[LevelPreviewCamera] {message}");
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _zoomSpeed = Mathf.Max(0.1f, _zoomSpeed);
            _panSpeed = Mathf.Max(0.01f, _panSpeed);
            _minZoom = Mathf.Max(1f, _minZoom);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_isInitialized) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_levelCenter, new Vector3(_levelBoundsHalfSize.x * 2, _levelBoundsHalfSize.y * 2, 0));

            if (_cinemachineCamera != null)
            {
                float aspectRatio = GetAspectRatio();
                float currentZoom = _cinemachineCamera.Lens.OrthographicSize;
                float viewWidth = currentZoom * 2 * aspectRatio;
                float viewHeight = currentZoom * 2;

                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_targetPosition, new Vector3(viewWidth, viewHeight, 0));
            }
        }
#endif

        #endregion
    }
}
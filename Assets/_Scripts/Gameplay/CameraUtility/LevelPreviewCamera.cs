using My.Scripts.Core.Data;
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

        // Референсное соотношение сторон (Full HD)
        private const float REFERENCE_ASPECT_RATIO = 16f / 9f;

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
        [SerializeField] private bool _enableDebugLogs = false;

        #endregion

        #region Private Fields

        private GameLevel _currentLevel;

        private float _maxZoom;
        private float _baseMaxZoom; // Оригинальный zoom из уровня (для Full HD)
        private float _targetZoom;
        private Vector3 _targetPosition;
        private Vector3 _levelCenter;
        private Vector2 _levelBoundsHalfSize;

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

            // Получаем базовый zoom (рассчитанный для Full HD)
            _baseMaxZoom = level.GetZoomedOutOrthographicSize();

            // Адаптируем zoom под текущее разрешение
            _maxZoom = CalculateAdaptedZoom(_baseMaxZoom);

            _levelCenter = level.GetCameraStartTargetTransform().position;

            Log($"BaseMaxZoom: {_baseMaxZoom}, AdaptedMaxZoom: {_maxZoom}, MinZoom: {_minZoom}, LevelCenter: {_levelCenter}");

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

            // Переключаем камеру на наш preview target
            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.Target.TrackingTarget = _cameraTarget;
                _cinemachineCamera.Lens.OrthographicSize = _targetZoom;

                Log($"Switched camera to preview target: {_cameraTarget.name}");
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

            if (_cameraTarget != null)
            {
                _cameraTarget.position = _targetPosition;
            }

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

        /// <summary>
        /// Вычисляет адаптированный zoom для сохранения одинаковой ширины обзора.
        /// </summary>
        private float CalculateAdaptedZoom(float baseZoom)
        {
            float currentAspect = GetAspectRatio();

            // Вычисляем ширину обзора для референсного разрешения (Full HD)
            // width = orthographicSize * 2 * aspectRatio
            // Для Full HD: referenceWidth = baseZoom * 2 * (16/9)
            float referenceWidth = baseZoom * 2f * REFERENCE_ASPECT_RATIO;

            // Вычисляем новый orthographicSize, чтобы сохранить ту же ширину
            // referenceWidth = newZoom * 2 * currentAspect
            // newZoom = referenceWidth / (2 * currentAspect)
            float adaptedZoom = referenceWidth / (2f * currentAspect);

            Log($"Aspect adaptation: CurrentAspect={currentAspect:F3}, ReferenceAspect={REFERENCE_ASPECT_RATIO:F3}, " +
                $"ReferenceWidth={referenceWidth:F2}, BaseZoom={baseZoom:F2}, AdaptedZoom={adaptedZoom:F2}");

            return adaptedZoom;
        }

        private void CalculateLevelBounds()
        {
            if (_currentLevel == null) return;

            // Используем референсные границы (для Full HD), чтобы pan был консистентным
            float height = _baseMaxZoom;
            float width = height * REFERENCE_ASPECT_RATIO;

            _levelBoundsHalfSize = new Vector2(width, height);
            Log($"Level bounds calculated. ReferenceAspect: {REFERENCE_ASPECT_RATIO}, Bounds: {_levelBoundsHalfSize}");
        }

        private float GetAspectRatio()
        {
            if (Screen.height == 0) return REFERENCE_ASPECT_RATIO;
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
            Log($"LanderStateChanged: {data.State}");

            if (data.State != Lander.State.WaitingToStart)
            {
                EndPreview();
            }
        }

        /// <summary>
        /// Завершает превью и переключает камеру на Lander.
        /// </summary>
        private void EndPreview()
        {
            if (!_isPreviewActive) return;

            _isPreviewActive = false;
            _isInitialized = false;

            Log("Ending preview...");

            // Переключаем камеру на Lander напрямую
            if (_cinemachineCamera != null && Lander.HasInstance)
            {
                _cinemachineCamera.Target.TrackingTarget = Lander.Instance.transform;
                Log($"Camera now tracking Lander at {Lander.Instance.transform.position}");
            }
            else
            {
                Log($"Cannot switch to Lander. Camera: {_cinemachineCamera != null}, Lander: {Lander.HasInstance}");
            }

            // Уничтожаем созданный target
            if (_cameraTarget != null && _cameraTarget.name == "PreviewCameraTarget")
            {
                Destroy(_cameraTarget.gameObject);
                _cameraTarget = null;
                Log("Destroyed preview camera target");
            }

            Log("Preview ended. Camera switched to Lander.");
        }

        #endregion

        #region Private Methods — Input Handling

        private void HandleZoomInput()
        {
            float scrollDelta = GameInput.Instance.GetCameraZoomInput();

            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                _targetZoom -= scrollDelta * _zoomSpeed * 0.5f;

                // Адаптируем minZoom под текущее разрешение
                float adaptedMinZoom = CalculateAdaptedZoom(_minZoom);
                _targetZoom = Mathf.Clamp(_targetZoom, adaptedMinZoom, _maxZoom);

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

                Vector3 panMovement = new Vector3(
                    -panDelta.x * _panSpeed * zoomFactor * 0.1f,
                    -panDelta.y * _panSpeed * zoomFactor * 0.1f,
                    0f
                );

                _targetPosition += panMovement;
                ClampTargetPosition();

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

            // Позиция — мгновенно
            _cameraTarget.position = _targetPosition;
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
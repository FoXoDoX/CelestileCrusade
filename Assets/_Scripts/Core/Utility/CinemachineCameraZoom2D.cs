using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace My.Scripts.Core.Utility
{
    public class CinemachineCameraZoom2D : Singleton<CinemachineCameraZoom2D>
    {
        #region Constants

        private const float DEFAULT_ORTHOGRAPHIC_SIZE = 12f;
        private const float ZOOM_SPEED = 2f;

        #endregion

        #region Serialized Fields

        [Header("Camera Reference")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;

        [Header("Settings")]
        [SerializeField] private float _zoomSpeed = ZOOM_SPEED;

        #endregion

        #region Private Fields

        private float _targetOrthographicSize = DEFAULT_ORTHOGRAPHIC_SIZE;
        private Lander.State _currentLanderState;

        #endregion

        #region Properties

        public float CurrentOrthographicSize => _cinemachineCamera != null
            ? _cinemachineCamera.Lens.OrthographicSize
            : 0f;

        public float TargetOrthographicSize => _targetOrthographicSize;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateCameraZoom();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ”станавливает целевой размер камеры дл€ плавного зума.
        /// </summary>
        public void SetTargetOrthographicSize(float size)
        {
            _targetOrthographicSize = Mathf.Max(0.1f, size); // «ащита от некорректных значений
        }

        /// <summary>
        /// ”станавливает нормальный размер камеры (алиас дл€ SetTargetOrthographicSize).
        /// </summary>
        public void SetNormalOrthographicSize(float size)
        {
            SetTargetOrthographicSize(size);
        }

        /// <summary>
        /// ћгновенно устанавливает размер камеры без анимации.
        /// </summary>
        public void SetOrthographicSizeImmediate(float size)
        {
            if (_cinemachineCamera == null) return;

            _targetOrthographicSize = Mathf.Max(0.1f, size);
            _cinemachineCamera.Lens.OrthographicSize = _targetOrthographicSize;
        }

        #endregion

        #region Private Methods Ч Initialization

        private void ValidateReferences()
        {
            if (_cinemachineCamera == null)
            {
                Debug.LogError($"[{nameof(CinemachineCameraZoom2D)}] CinemachineCamera reference is missing!", this);
            }
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToEvents()
        {
            EventManager.Instance?.AddHandler<LanderStateData>(
                GameEvents.LanderStateChanged,
                OnLanderStateChanged
            );
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Instance?.RemoveHandler<LanderStateData>(
                GameEvents.LanderStateChanged,
                OnLanderStateChanged
            );
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnLanderStateChanged(LanderStateData data)
        {
            _currentLanderState = data.State;
        }

        #endregion

        #region Private Methods Ч Camera Control

        private void UpdateCameraZoom()
        {
            if (_cinemachineCamera == null) return;

            float currentSize = _cinemachineCamera.Lens.OrthographicSize;
            float newSize = Mathf.Lerp(currentSize, _targetOrthographicSize, Time.deltaTime * _zoomSpeed);

            _cinemachineCamera.Lens.OrthographicSize = newSize;
        }

        #endregion
    }
}
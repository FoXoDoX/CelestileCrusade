using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using UnityEngine;

namespace My.Scripts.Environment.Terrain
{
    public class TerrainRotator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Rotation Settings")]
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private Vector3 _rotationAxis = Vector3.forward;

        [Header("Center Override")]
        [Tooltip("If assigned, uses this transform's position as rotation center")]
        [SerializeField] private Transform _customRotationCenter;

        #endregion

        #region Private Fields

        private Vector3 _rotationCenter;
        private bool _isRotating = true;

        #endregion

        #region Properties

        public float RotationSpeed
        {
            get => _rotationSpeed;
            set => _rotationSpeed = value;
        }

        public bool IsRotating
        {
            get => _isRotating;
            set => _isRotating = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CalculateRotationCenter();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (_isRotating)
            {
                Rotate();
            }
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToEvents()
        {
            EventManager.Instance?.AddHandler<LanderLandedData>(
                GameEvents.LanderLanded,
                OnLanderLanded
            );
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Instance?.RemoveHandler<LanderLandedData>(
                GameEvents.LanderLanded,
                OnLanderLanded
            );
        }

        #endregion

        #region Private Methods — Event Handlers

        private void OnLanderLanded(LanderLandedData data)
        {
            if (data.LandingType != Lander.LandingType.Success) return;
            if (!Lander.HasInstance) return;

            // Привязываем Lander к вращающейся платформе
            Lander.Instance.transform.SetParent(transform);
        }

        #endregion

        #region Private Methods — Rotation

        private void Rotate()
        {
            float angle = _rotationSpeed * Time.deltaTime;
            transform.RotateAround(_rotationCenter, _rotationAxis, angle);
        }

        private void CalculateRotationCenter()
        {
            // Приоритет: кастомный центр
            if (_customRotationCenter != null)
            {
                _rotationCenter = _customRotationCenter.position;
                return;
            }

            // Попытка использовать Renderer
            if (TryGetComponent(out Renderer renderer))
            {
                _rotationCenter = renderer.bounds.center;
                return;
            }

            // Попытка использовать Collider
            if (TryGetComponent(out Collider collider))
            {
                _rotationCenter = collider.bounds.center;
                return;
            }

            // Попытка использовать Collider2D
            if (TryGetComponent(out Collider2D collider2D))
            {
                _rotationCenter = collider2D.bounds.center;
                return;
            }

            // Fallback: позиция объекта
            _rotationCenter = transform.position;
            Debug.LogWarning(
                $"[{nameof(TerrainRotator)}] Could not find Renderer or Collider. " +
                $"Using transform position as rotation center.",
                this
            );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Останавливает вращение.
        /// </summary>
        public void StopRotation()
        {
            _isRotating = false;
        }

        /// <summary>
        /// Возобновляет вращение.
        /// </summary>
        public void StartRotation()
        {
            _isRotating = true;
        }

        /// <summary>
        /// Переключает состояние вращения.
        /// </summary>
        public void ToggleRotation()
        {
            _isRotating = !_isRotating;
        }

        /// <summary>
        /// Устанавливает новый центр вращения.
        /// </summary>
        public void SetRotationCenter(Vector3 center)
        {
            _rotationCenter = center;
        }

        /// <summary>
        /// Пересчитывает центр вращения на основе текущих компонентов.
        /// </summary>
        public void RecalculateRotationCenter()
        {
            CalculateRotationCenter();
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Показываем центр вращения в редакторе
            Vector3 center = Application.isPlaying
                ? _rotationCenter
                : CalculateRotationCenterEditor();

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, 0.5f);

            // Показываем ось вращения
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + _rotationAxis.normalized * 2f);
        }

        private Vector3 CalculateRotationCenterEditor()
        {
            if (_customRotationCenter != null)
                return _customRotationCenter.position;

            if (TryGetComponent(out Renderer renderer))
                return renderer.bounds.center;

            if (TryGetComponent(out Collider collider))
                return collider.bounds.center;

            if (TryGetComponent(out Collider2D collider2D))
                return collider2D.bounds.center;

            return transform.position;
        }

        private void OnValidate()
        {
            // Нормализуем ось вращения
            if (_rotationAxis != Vector3.zero)
            {
                _rotationAxis = _rotationAxis.normalized;
            }
            else
            {
                _rotationAxis = Vector3.forward;
            }
        }
#endif

        #endregion
    }
}
using UnityEngine;
using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using My.Scripts.Managers;

namespace My.Scripts.Environment.Light
{
    /// <summary>
    /// Зона пещеры, которая регистрируется в LightingManager.
    /// Рассчитывает интенсивность освещения на основе глубины проникновения игрока.
    /// Требует Collider2D с включённым Is Trigger.
    /// </summary>
    public class CaveZone : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Настройки интенсивности")]
        [Tooltip("Интенсивность света снаружи пещеры")]
        [SerializeField] private float _outsideIntensity = 1f;

        [Tooltip("Интенсивность света в глубине пещеры")]
        [SerializeField] private float _caveIntensity = 0.3f;

        [Header("Настройки глубины")]
        [Tooltip("Расстояние от точки входа, на котором достигается максимальное затемнение")]
        [SerializeField] private float _darkeningDistance = 20f;

        #endregion

        #region Private Fields

        private bool _playerInside;
        private bool _isGameOver;
        private Vector3 _entryPosition;
        private float _lastCalculatedIntensity;

        #endregion

        #region Properties

        public bool IsPlayerInside => _playerInside;
        public float OutsideIntensity => _outsideIntensity;
        public float CaveIntensity => _caveIntensity;
        public float LastCalculatedIntensity => _lastCalculatedIntensity;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateSetup();
            _lastCalculatedIntensity = _outsideIntensity;
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();

            // При отключении убираем себя из менеджера только если игрок жив
            if (_playerInside && LightingManager.HasInstance && !_isGameOver)
            {
                LightingManager.Instance.RegisterZoneExit(this);
            }

            if (!_isGameOver)
            {
                _playerInside = false;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (LightingManager.HasInstance)
            {
                LightingManager.Instance.UnregisterZone(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<Lander>(out _)) return;

            _playerInside = true;

            if (Lander.HasInstance)
            {
                _entryPosition = Lander.Instance.transform.position;
            }

            if (LightingManager.HasInstance)
            {
                LightingManager.Instance.RegisterZoneEntry(this);
            }

            Debug.Log($"[CaveZone] Player entered {gameObject.name} at {_entryPosition}");
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent<Lander>(out _)) return;

            // Не выходим из зоны, если игрок погиб
            if (_isGameOver)
            {
                Debug.Log($"[CaveZone] Player died in {gameObject.name}, keeping zone active");
                return;
            }

            _playerInside = false;

            if (LightingManager.HasInstance)
            {
                LightingManager.Instance.RegisterZoneExit(this);
            }

            Debug.Log($"[CaveZone] Player exited {gameObject.name}");
        }

        #endregion

        #region Event Subscription

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

        private void OnLanderLanded(LanderLandedData data)
        {
            _isGameOver = true;
            Debug.Log($"[CaveZone] Game over detected, freezing lighting at {_lastCalculatedIntensity}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Рассчитывает текущую интенсивность освещения для этой зоны.
        /// Вызывается менеджером каждый кадр.
        /// </summary>
        public float CalculateCurrentIntensity()
        {
            if (!_playerInside)
            {
                _lastCalculatedIntensity = _outsideIntensity;
                return _lastCalculatedIntensity;
            }

            // Если игрок погиб — возвращаем последнее рассчитанное значение
            if (_isGameOver)
            {
                return _lastCalculatedIntensity;
            }

            if (!Lander.HasInstance)
            {
                return _lastCalculatedIntensity;
            }

            Vector3 playerPosition = Lander.Instance.transform.position;
            float distance = Vector2.Distance(_entryPosition, playerPosition);
            float depthProgress = Mathf.Clamp01(distance / _darkeningDistance);

            _lastCalculatedIntensity = Mathf.Lerp(_outsideIntensity, _caveIntensity, depthProgress);
            return _lastCalculatedIntensity;
        }

        /// <summary>
        /// Сбрасывает состояние зоны.
        /// </summary>
        public void ResetZone()
        {
            if (_playerInside && LightingManager.HasInstance)
            {
                LightingManager.Instance.RegisterZoneExit(this);
            }

            _playerInside = false;
            _isGameOver = false;
            _lastCalculatedIntensity = _outsideIntensity;
        }

        #endregion

        #region Private Methods

        private void ValidateSetup()
        {
            if (!LightingManager.HasInstance)
            {
                Debug.LogError($"[CaveZone] LightingManager не найден на сцене! " +
                               $"Добавьте его для работы {gameObject.name}");
            }

            if (_darkeningDistance <= 0f)
            {
                Debug.LogWarning($"[CaveZone] Darkening Distance должен быть больше 0 на {gameObject.name}");
            }

            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                Debug.LogError($"[CaveZone] Отсутствует Collider2D на {gameObject.name}");
            }
            else if (!collider.isTrigger)
            {
                Debug.LogWarning($"[CaveZone] Collider2D должен быть триггером на {gameObject.name}");
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.3f);

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            }

            if (_playerInside && Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_entryPosition, 0.5f);

                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawWireSphere(_entryPosition, _darkeningDistance);
            }
        }
#endif

        #endregion
    }
}
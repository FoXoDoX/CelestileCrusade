using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using My.Scripts.Core.Data;
using UnityEngine;

namespace My.Scripts.Environment.Hazards
{
    public class HotZone : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Настройки")]
        [Tooltip("Сколько энергии отнимается в секунду")]
        [SerializeField] private float _energyDrainPerSecond = 1f;

        #endregion

        #region Private Fields

        private bool _playerInside;
        private bool _isGameOver;

        #endregion

        #region Properties

        public bool IsPlayerInside => _playerInside;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateSetup();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();

            if (_playerInside)
            {
                UnregisterFromEffect();
                BroadcastHotZoneState(false);
            }

            _playerInside = false;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (_playerInside)
            {
                UnregisterFromEffect();
                BroadcastHotZoneState(false);
            }
        }

        private void Update()
        {
            if (!_playerInside) return;
            if (_isGameOver) return;
            if (!Lander.HasInstance) return;

            float drain = _energyDrainPerSecond * Time.deltaTime;
            Lander.Instance.AddEnergy(-drain);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<Lander>(out _)) return;

            _playerInside = true;
            BroadcastHotZoneState(true);
            RegisterToEffect();

            Debug.Log($"[HotZone] Player entered {gameObject.name}");
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent<Lander>(out _)) return;
            if (_isGameOver) return;

            _playerInside = false;
            BroadcastHotZoneState(false);
            UnregisterFromEffect();

            Debug.Log($"[HotZone] Player exited {gameObject.name}");
        }

        #endregion

        #region Event Broadcasting

        private void BroadcastHotZoneState(bool isInside)
        {
            EventManager.Instance?.Broadcast(
                GameEvents.HotZoneStateChanged,
                new HotZoneStateData(isInside)
            );
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
        }

        #endregion

        #region Private Methods

        private void RegisterToEffect()
        {
            var effect = FindFirstObjectByType<HotZoneEffect>();
            if (effect != null)
            {
                effect.RegisterZone(this);
            }
        }

        private void UnregisterFromEffect()
        {
            var effect = FindFirstObjectByType<HotZoneEffect>();
            if (effect != null)
            {
                effect.UnregisterZone(this);
            }
        }

        private void ValidateSetup()
        {
            if (_energyDrainPerSecond <= 0f)
            {
                Debug.LogWarning($"[HotZone] Drain rate должен быть > 0 на {gameObject.name}");
            }

            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                Debug.LogError($"[HotZone] Отсутствует Collider2D на {gameObject.name}");
            }
            else if (!collider.isTrigger)
            {
                Debug.LogWarning($"[HotZone] Collider2D должен быть триггером на {gameObject.name}");
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            }
        }
#endif

        #endregion
    }
}
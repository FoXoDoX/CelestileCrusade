using UnityEngine;
using My.Scripts.Gameplay.Player;

namespace My.Scripts.Environment.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(ParticleSystem))]
    public class AcidSplash : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Damage")]
        [SerializeField] private float _damageDelay = 0.5f;

        [Header("Collider Lifetime")]
        [SerializeField] private float _colliderLifetime = 1.5f;

        #endregion

        #region Private Fields

        private float _playerTimer;
        private float _colliderTimer;
        private bool _playerInside;
        private bool _colliderDisabled;

        private Collider2D _collider;
        private ParticleSystem _particleSystem;
        private AcidPoolEffect _acidPoolEffect;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;

            _particleSystem = GetComponent<ParticleSystem>();

            var main = _particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Destroy;

            _acidPoolEffect = FindFirstObjectByType<AcidPoolEffect>();
        }

        private void Update()
        {
            UpdateColliderLifetime();
            UpdatePlayerDamage();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_colliderDisabled) return;
            if (!IsPlayer(other)) return;

            _playerInside = true;
            _playerTimer = 0f;

            _acidPoolEffect?.RegisterSource();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;

            _playerInside = false;
            _playerTimer = 0f;

            _acidPoolEffect?.UnregisterSource();
        }

        private void OnDestroy()
        {
            if (_playerInside)
            {
                _acidPoolEffect?.UnregisterSource();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateColliderLifetime()
        {
            if (_colliderDisabled) return;

            _colliderTimer += Time.deltaTime;

            if (_colliderTimer >= _colliderLifetime)
            {
                DisableCollider();
            }
        }

        private void DisableCollider()
        {
            if (_playerInside)
            {
                _acidPoolEffect?.UnregisterSource();
            }

            _colliderDisabled = true;
            _playerInside = false;
            _playerTimer = 0f;
            _collider.enabled = false;
        }

        private void UpdatePlayerDamage()
        {
            if (!_playerInside) return;

            _playerTimer += Time.deltaTime;

            if (_playerTimer >= _damageDelay)
            {
                KillPlayer();
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            return other.TryGetComponent(out Lander _);
        }

        private void KillPlayer()
        {
            _playerInside = false;

            _acidPoolEffect?.UnregisterSource();

            if (!Lander.HasInstance) return;

            Lander.Instance.Kill();
        }

        #endregion
    }
}
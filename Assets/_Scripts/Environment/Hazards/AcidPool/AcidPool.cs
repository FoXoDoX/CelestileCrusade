using UnityEngine;
using My.Scripts.Gameplay.Player;

namespace My.Scripts.Environment.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class AcidPool : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _damageDelay = 1f;

        private float _timer;
        private bool _playerInside;
        private AcidPoolEffect _acidPoolEffect;

        private void Start()
        {
            _acidPoolEffect = FindFirstObjectByType<AcidPoolEffect>();
        }

        private void Update()
        {
            if (!_playerInside) return;

            _timer += Time.deltaTime;

            if (_timer >= _damageDelay)
            {
                KillPlayer();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;

            _playerInside = true;
            _timer = 0f;

            _acidPoolEffect?.RegisterSource();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;

            _playerInside = false;
            _timer = 0f;

            _acidPoolEffect?.UnregisterSource();
        }

        private bool IsPlayer(Collider2D other)
        {
            return other.TryGetComponent(out Lander _);
        }

        private void KillPlayer()
        {
            _playerInside = false;
            _timer = 0f;

            _acidPoolEffect?.UnregisterSource();

            if (!Lander.HasInstance) return;

            Lander.Instance.Kill();
        }
    }
}
using UnityEngine;

namespace My.Scripts.Environment.Hazards
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class TurretProjectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _lifetime = 20f;
        [SerializeField] private float _destroyAnimationDuration = 0.2f;

        private Rigidbody2D _rigidbody;
        private float _lifeTimer;
        private bool _isDestroying;

        private Vector3 _originalScale;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            _rigidbody.gravityScale = 0f;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _originalScale = transform.localScale;
        }

        public void LaunchTowards(Vector2 direction)
        {
            _rigidbody.linearVelocity = direction.normalized * _speed;
            _lifeTimer = _lifetime;
        }

        private void Update()
        {
            if (_isDestroying) return;

            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0)
            {
                DestroyProjectile();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision2D)
        {
            DestroyProjectile();
        }

        private void DestroyProjectile()
        {
            if (_isDestroying) return;
            _isDestroying = true;

            _rigidbody.linearVelocity = Vector2.zero;

            StartCoroutine(DestroyAnimation());
        }

        private System.Collections.IEnumerator DestroyAnimation()
        {
            float elapsed = 0f;

            while (elapsed < _destroyAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _destroyAnimationDuration;
                transform.localScale = Vector3.Lerp(_originalScale, Vector3.zero, t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
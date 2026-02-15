using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.KeyDoor;
using My.Scripts.Gameplay.LandingPads;
using My.Scripts.Gameplay.Pickups;
using My.Scripts.Gameplay.Player;
using My.Scripts.Managers;
using System;
using System.Collections;
using UnityEngine;

namespace My.Scripts.Gameplay.Crate
{
    /// <summary>
    /// Ящик на верёвке. Использует Distance Joint 2D для связи с игроком.
    /// </summary>
    public class CrateOnRope : MonoBehaviour
    {
        #region Constants

        private const int INITIAL_HEALTH = 3;
        private const float DELIVERY_TIME = 3f;
        private const float FUEL_PICKUP_AMOUNT = 15f;

        #endregion

        #region Serialized Fields

        [Header("Sprites")]
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _crackedSprite;
        [SerializeField] private Sprite _veryCrackedSprite;

        [Header("Vfx")]
        [SerializeField] private ParticleSystem _crackVfx;

        [Header("Physics — Basic")]
        [SerializeField] private float _mass = 2f;
        [SerializeField] private float _gravityScale = 3f;

        [Header("Physics — Damping")]
        [Tooltip("Сопротивление движению. Низкое = больше раскачивания")]
        [SerializeField] private float _linearDrag = 0.5f;

        [Tooltip("Сопротивление вращению")]
        [SerializeField] private float _angularDrag = 0.5f;

        [Header("Physics — Swing Behavior")]
        [Tooltip("Дополнительная сила, возвращающая ящик под игрока")]
        [SerializeField] private float _centeringForce = 5f;

        [Tooltip("Демпфирование горизонтальной скорости для плавного качания")]
        [SerializeField] private float _horizontalDamping = 0.98f;

        [Tooltip("Максимальная горизонтальная скорость")]
        [SerializeField] private float _maxHorizontalSpeed = 10f;

        [Header("Physics — Tilt")]
        [Tooltip("Максимальный угол наклона в градусах")]
        [SerializeField] private float _maxTiltAngle = 25f;

        [Tooltip("Влияние горизонтального смещения на наклон")]
        [SerializeField] private float _tiltFromOffset = 15f;

        [Tooltip("Влияние горизонтальной скорости на наклон")]
        [SerializeField] private float _tiltFromVelocity = 3f;

        [Tooltip("Скорость плавного поворота к целевому углу")]
        [SerializeField] private float _tiltSmoothing = 8f;

        [Header("Physics — Bounce")]
        [Tooltip("Сила отскока от препятствий")]
        [SerializeField] private float _bounceForce = 8f;

        [Tooltip("Минимальная скорость столкновения для отскока")]
        [SerializeField] private float _minImpactVelocity = 1f;

        [Tooltip("Дополнительный наклон при ударе (градусы)")]
        [SerializeField] private float _impactTiltBoost = 15f;

        [Tooltip("Длительность эффекта удара")]
        [SerializeField] private float _impactEffectDuration = 0.3f;

        [Tooltip("Подъём вверх при отскоке (0-1)")]
        [SerializeField] private float _bounceUpwardBias = 0.3f;

        #endregion

        #region Private Fields

        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private DistanceJoint2D _distanceJoint;
        private Transform _attachPoint;

        private CrateLandingPadArea _currentLandingArea;
        private Coroutine _deliveryCoroutine;

        private int _health = INITIAL_HEALTH;
        private float _deliveryProgress;
        private float _currentTiltAngle;
        private float _impactTiltEffect;
        private float _lastImpactTime;
        private bool _isInLandingArea;
        private bool _isProgressSoundPlaying;
        private bool _isDestroyed;

        #endregion

        #region Properties

        public Rigidbody2D Rigidbody => _rigidbody;
        public bool IsDestroyed => _isDestroyed;

        #endregion

        #region Events

        public event Action OnCrateCracked;
        public event Action OnCrateDestroyed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            SetupRigidbody();
        }

        private void FixedUpdate()
        {
            ApplySwingPhysics();
        }

        private void Update()
        {
            UpdateImpactEffect();
            ApplyTilt();
        }

        private void OnDisable()
        {
            StopProgressBarSound();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (TryHandleFuelPickup(other)) return;
            if (TryHandleCoinPickup(other)) return;
            if (TryHandleKeyPickup(other)) return;
            if (TryHandleLandingAreaEnter(other)) return;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            TryHandleLandingAreaExit(other);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Привязывает ящик к игроку через Distance Joint 2D.
        /// </summary>
        public void AttachToPlayer(Rigidbody2D attachPointRb, float ropeLength)
        {
            _attachPoint = attachPointRb.transform;

            if (_distanceJoint == null)
            {
                _distanceJoint = gameObject.AddComponent<DistanceJoint2D>();
            }

            _distanceJoint.connectedBody = attachPointRb;
            _distanceJoint.autoConfigureDistance = false;
            _distanceJoint.distance = ropeLength;
            _distanceJoint.maxDistanceOnly = true;
            _distanceJoint.enableCollision = false;
        }

        /// <summary>
        /// Отвязывает ящик от игрока.
        /// </summary>
        public void DetachFromPlayer()
        {
            _attachPoint = null;

            if (_distanceJoint != null)
            {
                Destroy(_distanceJoint);
                _distanceJoint = null;
            }
        }

        /// <summary>
        /// Наносит урон ящику.
        /// </summary>
        public void TakeDamage(int damage = 1)
        {
            if (_isDestroyed) return;

            _health -= damage;

            UpdateVisual();
            OnCrateCracked?.Invoke();
            EventManager.Instance?.Broadcast(GameEvents.CrateCracked);

            if (_health <= 0)
            {
                DestroyCrate();
            }
        }

        #endregion

        #region Private Methods — Initialization

        private void CacheComponents()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody2D>();
            }
        }

        private void SetupRigidbody()
        {
            if (_rigidbody == null) return;

            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody.mass = _mass;
            _rigidbody.linearDamping = _linearDrag;
            _rigidbody.angularDamping = _angularDrag;
            _rigidbody.gravityScale = _gravityScale;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        #endregion

        #region Private Methods — Swing Physics

        private void ApplySwingPhysics()
        {
            if (_attachPoint == null) return;
            if (_rigidbody == null) return;

            Vector2 attachPos = _attachPoint.position;
            Vector2 cratePos = _rigidbody.position;

            float horizontalOffset = cratePos.x - attachPos.x;

            float centeringForceX = -horizontalOffset * _centeringForce;
            _rigidbody.AddForce(new Vector2(centeringForceX, 0f));

            Vector2 velocity = _rigidbody.linearVelocity;
            velocity.x *= _horizontalDamping;

            velocity.x = Mathf.Clamp(velocity.x, -_maxHorizontalSpeed, _maxHorizontalSpeed);

            _rigidbody.linearVelocity = velocity;
        }

        #endregion

        #region Private Methods — Tilt

        private void ApplyTilt()
        {
            if (_attachPoint == null) return;
            if (_rigidbody == null) return;

            Vector2 attachPos = _attachPoint.position;
            Vector2 cratePos = transform.position;

            float horizontalOffset = cratePos.x - attachPos.x;
            float ropeLength = _distanceJoint != null ? _distanceJoint.distance : 5f;
            float normalizedOffset = horizontalOffset / ropeLength;

            float horizontalVelocity = _rigidbody.linearVelocity.x;

            float tiltFromPosition = -normalizedOffset * _tiltFromOffset;
            float tiltFromSpeed = -horizontalVelocity * _tiltFromVelocity;

            float targetTilt = tiltFromPosition + tiltFromSpeed + _impactTiltEffect;
            targetTilt = Mathf.Clamp(targetTilt, -_maxTiltAngle - _impactTiltBoost, _maxTiltAngle + _impactTiltBoost);

            _currentTiltAngle = Mathf.Lerp(_currentTiltAngle, targetTilt, _tiltSmoothing * Time.deltaTime);

            transform.rotation = Quaternion.Euler(0f, 0f, _currentTiltAngle);
        }

        private void UpdateImpactEffect()
        {
            if (_impactTiltEffect == 0f) return;

            float timeSinceImpact = Time.time - _lastImpactTime;
            float progress = timeSinceImpact / _impactEffectDuration;

            if (progress >= 1f)
            {
                _impactTiltEffect = 0f;
            }
            else
            {
                float decay = 1f - progress;
                _impactTiltEffect = _impactTiltEffect * decay * Mathf.Cos(progress * Mathf.PI * 4f);
            }
        }

        #endregion

        #region Private Methods — Collision & Bounce

        private void HandleCollision(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<CrateLandingPad>() != null) return;
            if (collision.gameObject.GetComponent<Player.Lander>() != null) return;

            ApplyBounce(collision);
            TakeDamage();
        }

        private void ApplyBounce(Collision2D collision)
        {
            if (_rigidbody == null) return;

            ContactPoint2D contact = collision.GetContact(0);
            Vector2 normal = contact.normal;
            float impactVelocity = collision.relativeVelocity.magnitude;

            if (impactVelocity < _minImpactVelocity) return;

            float bounceMultiplier = Mathf.Clamp01(impactVelocity / 10f) + 0.5f;

            Vector2 bounceDirection = normal;
            bounceDirection.y += _bounceUpwardBias;
            bounceDirection.Normalize();

            Vector2 bounceImpulse = bounceDirection * _bounceForce * bounceMultiplier;
            Instantiate(_crackVfx, transform.position, Quaternion.identity);
            _rigidbody.AddForce(bounceImpulse, ForceMode2D.Impulse);

            float impactSide = Mathf.Sign(contact.point.x - transform.position.x);
            _impactTiltEffect = impactSide * _impactTiltBoost * bounceMultiplier;
            _lastImpactTime = Time.time;
        }

        #endregion

        #region Private Methods — Pickup Handling

        private bool TryHandleCoinPickup(Collider2D other)
        {
            if (!other.TryGetComponent(out CoinPickup coinPickup)) return false;

            EventManager.Instance?.Broadcast(GameEvents.CoinPickup, new PickupEventData(transform.position));
            coinPickup.DestroySelf();
            return true;
        }

        private bool TryHandleFuelPickup(Collider2D other)
        {
            if (!other.TryGetComponent(out EnergyBookPickup EnergyBookPickup)) return false;

            if (Lander.HasInstance)
            {
                Lander.Instance.AddFuel(FUEL_PICKUP_AMOUNT);
            }

            EventManager.Instance?.Broadcast(GameEvents.EnergyBookPickup, new PickupEventData(transform.position));
            EnergyBookPickup.Pickup();
            return true;
        }

        private bool TryHandleKeyPickup(Collider2D other)
        {
            if (!other.TryGetComponent(out Key key)) return false;

            KeyHolder keyHolder = FindKeyHolder();

            if (keyHolder != null)
            {
                // Перемещаем ключ к KeyHolder, чтобы тот подобрал его сам
                key.transform.position = keyHolder.transform.position;

                // KeyHolder подберёт ключ через свой OnTriggerEnter2D
                return true;
            }

            // Fallback: уничтожаем ключ и отправляем событие
            Destroy(key.gameObject);
            EventManager.Instance?.Broadcast(GameEvents.KeyPickup);
            return true;
        }

        private KeyHolder FindKeyHolder()
        {
            if (Lander.HasInstance)
            {
                var holder = Lander.Instance.GetComponent<KeyHolder>();
                if (holder != null) return holder;

                holder = Lander.Instance.GetComponentInChildren<KeyHolder>();
                if (holder != null) return holder;
            }

            return FindFirstObjectByType<KeyHolder>();
        }

        #endregion

        #region Private Methods — Landing Area

        private bool TryHandleLandingAreaEnter(Collider2D other)
        {
            if (!other.TryGetComponent(out CrateLandingPadArea landingArea)) return false;

            var landingPad = landingArea.LandingPad;
            if (landingPad == null || !landingPad.CanAcceptCrates) return false;

            _currentLandingArea = landingArea;
            _isInLandingArea = true;

            RestartDeliveryCoroutine();
            return true;
        }

        private void TryHandleLandingAreaExit(Collider2D other)
        {
            if (!other.TryGetComponent(out CrateLandingPadArea landingArea)) return;
            if (_currentLandingArea != landingArea) return;

            CancelDelivery();
        }

        private void RestartDeliveryCoroutine()
        {
            if (_deliveryCoroutine != null)
            {
                StopCoroutine(_deliveryCoroutine);
                StopProgressBarSound();
            }

            _deliveryCoroutine = StartCoroutine(DeliveryRoutine());
        }

        private void CancelDelivery()
        {
            _isInLandingArea = false;
            _currentLandingArea?.LandingPad?.ResetDeliveryProgress();
            _currentLandingArea = null;
            _deliveryProgress = 0f;

            if (_deliveryCoroutine != null)
            {
                StopCoroutine(_deliveryCoroutine);
                _deliveryCoroutine = null;
                StopProgressBarSound();
            }
        }

        private IEnumerator DeliveryRoutine()
        {
            var landingArea = _currentLandingArea;
            var landingPad = landingArea?.LandingPad;

            if (landingPad == null) yield break;

            StartProgressBarSound();
            _deliveryProgress = 0f;

            while (_deliveryProgress < 1f)
            {
                if (!_isInLandingArea || !landingPad.CanAcceptCrates)
                {
                    StopProgressBarSound();
                    yield break;
                }

                _deliveryProgress += Time.deltaTime / DELIVERY_TIME;
                landingPad.UpdateDeliveryProgress(_deliveryProgress);
                yield return null;
            }

            if (landingPad.CanAcceptCrates)
            {
                landingArea.RegisterCrateDelivery();
                landingPad.ResetDeliveryProgress();
            }

            StopProgressBarSound();
            _deliveryCoroutine = null;
            _currentLandingArea = null;
        }

        #endregion

        #region Private Methods — Visual

        private void UpdateVisual()
        {
            if (_spriteRenderer == null) return;

            _spriteRenderer.sprite = _health switch
            {
                3 => _normalSprite,
                2 => _crackedSprite,
                1 => _veryCrackedSprite,
                _ => _spriteRenderer.sprite
            };
        }

        #endregion

        #region Private Methods — Destruction

        private void DestroyCrate()
        {
            if (_isDestroyed) return;

            _isDestroyed = true;
            CancelDelivery();
            DetachFromPlayer();

            OnCrateDestroyed?.Invoke();
            EventManager.Instance?.Broadcast(GameEvents.CrateDestroyed);

            Destroy(gameObject);
        }

        #endregion

        #region Private Methods — Sound

        private void StartProgressBarSound()
        {
            if (_isProgressSoundPlaying) return;

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.PlayProgressBarSound();
                _isProgressSoundPlaying = true;
            }
        }

        private void StopProgressBarSound()
        {
            if (!_isProgressSoundPlaying) return;

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.StopProgressBarSound();
                _isProgressSoundPlaying = false;
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _mass = Mathf.Max(0.1f, _mass);
            _linearDrag = Mathf.Max(0f, _linearDrag);
            _angularDrag = Mathf.Max(0f, _angularDrag);
            _gravityScale = Mathf.Max(0f, _gravityScale);
            _centeringForce = Mathf.Max(0f, _centeringForce);
            _horizontalDamping = Mathf.Clamp01(_horizontalDamping);
            _maxHorizontalSpeed = Mathf.Max(1f, _maxHorizontalSpeed);
            _maxTiltAngle = Mathf.Clamp(_maxTiltAngle, 0f, 45f);
            _tiltFromOffset = Mathf.Max(0f, _tiltFromOffset);
            _tiltFromVelocity = Mathf.Max(0f, _tiltFromVelocity);
            _tiltSmoothing = Mathf.Max(1f, _tiltSmoothing);
            _bounceForce = Mathf.Max(0f, _bounceForce);
            _minImpactVelocity = Mathf.Max(0f, _minImpactVelocity);
            _impactTiltBoost = Mathf.Max(0f, _impactTiltBoost);
            _impactEffectDuration = Mathf.Max(0.1f, _impactEffectDuration);
            _bounceUpwardBias = Mathf.Clamp01(_bounceUpwardBias);
        }
#endif

        #endregion
    }
}
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using UnityEngine;
using DG.Tweening;

namespace My.Scripts.Environment.Hazards
{
    public class Turret : MonoBehaviour
    {
        #region Constants

        private const float DEFAULT_FIRING_ARC = 90f;
        private const int GIZMO_ARC_SEGMENTS = 20;

        #endregion

        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Transform _rotatingPivot;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private GameObject _projectilePrefab;

        [Header("Rotation Settings")]
        [SerializeField] private float _rotationSpeed = 0.3f;
        [SerializeField] private Ease _rotationEase = Ease.OutBack;

        [Header("Combat Settings")]
        [SerializeField] private float _minFireInterval = 2f;
        [SerializeField] private float _maxFireInterval = 3f;
        [SerializeField] private float _triggerRadius = 25f;
        [SerializeField] private float _firingArcAngle = DEFAULT_FIRING_ARC;

        #endregion

        #region Private Fields

        private Transform _targetTransform;
        private CircleCollider2D _triggerCollider;
        private Tween _rotationTween;

        private float _initialRotation;
        private Vector2 _initialForward;
        private float _fireTimer;
        private bool _isActive;

        #endregion

        #region Properties

        public bool IsActive => _isActive;
        public float TriggerRadius => _triggerRadius;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeTriggerCollider();
            CacheInitialState();
        }

        private void Start()
        {
            CacheTarget();
        }

        private void Update()
        {
            if (!_isActive) return;

            UpdateActiveState();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsLander(other))
            {
                CheckPlayerPosition();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (IsLander(other))
            {
                CheckPlayerPosition();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsLander(other))
            {
                Deactivate();
            }
        }

        #endregion

        #region Events

        public event System.Action OnShoot;

        #endregion

        #region Private Methods Ч Initialization

        private void InitializeTriggerCollider()
        {
            _triggerCollider = gameObject.AddComponent<CircleCollider2D>();
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = _triggerRadius;
        }

        private void CacheInitialState()
        {
            if (_rotatingPivot != null)
            {
                _initialRotation = _rotatingPivot.eulerAngles.z;
                _initialForward = _rotatingPivot.up;
            }
        }

        private void CacheTarget()
        {
            if (Lander.HasInstance)
            {
                _targetTransform = Lander.Instance.transform;
            }
            else
            {
                Debug.LogWarning($"[{nameof(Turret)}] Lander not found!", this);
            }
        }

        #endregion

        #region Private Methods Ч Activation

        private bool IsLander(Collider2D other)
        {
            return other.TryGetComponent<Lander>(out _);
        }

        private void CheckPlayerPosition()
        {
            if (_targetTransform == null)
            {
                TryRecacheTarget();
                return;
            }

            bool inFiringArc = IsTargetInFiringArc(_targetTransform);

            if (inFiringArc && !_isActive)
            {
                Activate();
            }
            else if (!inFiringArc && _isActive)
            {
                Deactivate();
            }
        }

        private void TryRecacheTarget()
        {
            if (Lander.HasInstance)
            {
                _targetTransform = Lander.Instance.transform;
            }
        }

        private void Activate()
        {
            if (_isActive) return;

            _isActive = true;
            _fireTimer = GetRandomFireInterval();
            StartContinuousAiming();
        }

        private void Deactivate()
        {
            if (!_isActive) return;

            _isActive = false;
            CleanupTween();
            ReturnToInitialPosition();
        }

        #endregion

        #region Private Methods Ч Targeting

        private bool IsTargetInFiringArc(Transform target)
        {
            if (target == null) return false;

            Vector2 directionToTarget = (Vector2)(target.position - transform.position);
            float angle = Vector2.SignedAngle(_initialForward, directionToTarget);

            return Mathf.Abs(angle) <= _firingArcAngle;
        }

        private void StartContinuousAiming()
        {
            if (!_isActive || _targetTransform == null) return;

            if (!IsTargetInFiringArc(_targetTransform))
            {
                Deactivate();
                return;
            }

            float targetAngle = CalculateTargetAngle();
            targetAngle = ClampAngleToFiringArc(targetAngle);

            _rotationTween = _rotatingPivot
                .DORotate(new Vector3(0, 0, targetAngle), _rotationSpeed, RotateMode.Fast)
                .SetEase(_rotationEase)
                .OnComplete(OnAimingComplete);
        }

        private void OnAimingComplete()
        {
            if (_isActive)
            {
                StartContinuousAiming();
            }
        }

        private float CalculateTargetAngle()
        {
            Vector2 direction = _targetTransform.position - _rotatingPivot.position;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        }

        private float ClampAngleToFiringArc(float targetAngle)
        {
            targetAngle = NormalizeAngle(targetAngle);
            float normalizedInitial = NormalizeAngle(_initialRotation);

            float angleDifference = NormalizeAngle(targetAngle - normalizedInitial);
            angleDifference = Mathf.Clamp(angleDifference, -_firingArcAngle, _firingArcAngle);

            return NormalizeAngle(normalizedInitial + angleDifference);
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        private void ReturnToInitialPosition()
        {
            if (_rotatingPivot == null) return;

            _rotationTween = _rotatingPivot
                .DORotate(new Vector3(0, 0, _initialRotation), _rotationSpeed, RotateMode.Fast)
                .SetEase(_rotationEase);
        }

        #endregion

        #region Private Methods Ч Combat

        private float GetRandomFireInterval()
        {
            return Random.Range(_minFireInterval, _maxFireInterval);
        }

        private void UpdateActiveState()
        {
            if (_targetTransform == null || !IsTargetInFiringArc(_targetTransform))
            {
                Deactivate();
                return;
            }

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                Shoot();
                _fireTimer = GetRandomFireInterval();
            }
        }

        private void Shoot()
        {
            if (_projectilePrefab == null || _firePoint == null) return;

            GameObject projectile = Instantiate(_projectilePrefab, _firePoint.position, _firePoint.rotation);

            if (projectile.TryGetComponent(out TurretProjectile projectileScript))
            {
                Vector2 direction = _firePoint.up;
                projectileScript.LaunchTowards(direction);
            }

            OnShoot?.Invoke();
            EventManager.Instance?.Broadcast(GameEvents.TurretShoot);
        }

        #endregion

        #region Private Methods Ч Cleanup

        private void CleanupTween()
        {
            if (_rotationTween != null)
            {
                _rotationTween.Kill();
                _rotationTween = null;
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_triggerRadius <= 0f)
            {
                _triggerRadius = 25f;
            }

            // √арантируем корректный диапазон интервалов
            _minFireInterval = Mathf.Max(0.1f, _minFireInterval);
            _maxFireInterval = Mathf.Max(_minFireInterval, _maxFireInterval);

            _firingArcAngle = Mathf.Clamp(_firingArcAngle, 0f, 180f);

            if (_triggerCollider != null)
            {
                _triggerCollider.radius = _triggerRadius;
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawTriggerRadius();
            DrawFirePoint();
            DrawFiringArc();
        }

        private void DrawTriggerRadius()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _triggerRadius);
        }

        private void DrawFirePoint()
        {
            if (_firePoint == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(_firePoint.position, _firePoint.position + _firePoint.up * 2f);
        }

        private void DrawFiringArc()
        {
            if (_rotatingPivot == null) return;

            Vector2 forward = Application.isPlaying ? _initialForward : (Vector2)_rotatingPivot.up;

            Gizmos.color = Color.cyan;

            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(forward * _triggerRadius));

            Vector2 leftBound = Quaternion.Euler(0, 0, _firingArcAngle) * forward;
            Vector2 rightBound = Quaternion.Euler(0, 0, -_firingArcAngle) * forward;

            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(leftBound * _triggerRadius));
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(rightBound * _triggerRadius));

            DrawArc(transform.position, forward, _triggerRadius, _firingArcAngle * 2f);
        }

        private void DrawArc(Vector2 center, Vector2 forward, float radius, float totalAngle)
        {
            float halfAngle = totalAngle / 2f;
            Vector2 startDir = Quaternion.Euler(0, 0, halfAngle) * forward;
            Vector2 previousPoint = center + startDir * radius;

            for (int i = 1; i <= GIZMO_ARC_SEGMENTS; i++)
            {
                float t = i / (float)GIZMO_ARC_SEGMENTS;
                float currentAngle = Mathf.Lerp(halfAngle, -halfAngle, t);
                Vector2 currentDir = Quaternion.Euler(0, 0, currentAngle) * forward;
                Vector2 currentPoint = center + currentDir * radius;

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }
#endif

        #endregion
    }
}
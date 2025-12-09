using UnityEngine;
using DG.Tweening;

namespace My.Scripts.Environment.Hazards
{
    public class TurretVisuals : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Transform _gunTransform;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private Transform _gunPivot;
        [SerializeField] private ParticleSystem _smokePrefab;

        [Header("Recoil Animation")]
        [SerializeField] private float _recoilDistance = 1.5f;
        [SerializeField] private float _recoilDuration = 0.05f;
        [SerializeField] private float _returnDuration = 0.4f;

        [Header("Shake Effect")]
        [SerializeField] private float _shakeIntensity = 0.5f;
        [SerializeField] private int _shakeVibrato = 15;

        #endregion

        #region Private Fields

        private Turret _turret;
        private Vector3 _originalGunPosition;
        private Sequence _animationSequence;
        private bool _isAnimating;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            CacheComponents();
            CacheOriginalPosition();
        }

        private void OnEnable()
        {
            SubscribeToTurret();
        }

        private void OnDisable()
        {
            UnsubscribeFromTurret();
        }

        private void OnDestroy()
        {
            CleanupAnimation();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void ValidateReferences()
        {
            if (_gunTransform == null)
            {
                Debug.LogError($"[{nameof(TurretVisuals)}] Gun Transform is not assigned!", this);
            }
        }

        private void CacheComponents()
        {
            _turret = GetComponent<Turret>();

            if (_turret == null)
            {
                Debug.LogError($"[{nameof(TurretVisuals)}] Turret component not found!", this);
            }
        }

        private void CacheOriginalPosition()
        {
            if (_gunTransform != null)
            {
                _originalGunPosition = _gunTransform.localPosition;
            }
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToTurret()
        {
            if (_turret != null)
            {
                _turret.OnShoot += HandleShoot;
            }
        }

        private void UnsubscribeFromTurret()
        {
            if (_turret != null)
            {
                _turret.OnShoot -= HandleShoot;
            }
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void HandleShoot()
        {
            PlayRecoilAnimation();
            PlaySmokeEffect();
        }

        #endregion

        #region Private Methods Ч Recoil Animation

        private void PlayRecoilAnimation()
        {
            if (_gunTransform == null) return;
            if (_isAnimating) return;

            _isAnimating = true;

            // ”биваем предыдущую анимацию
            CleanupAnimation();

            // ¬ычисл€ем позицию отдачи
            Vector3 recoilDirection = -_gunTransform.up;
            Vector3 recoilPosition = _originalGunPosition + recoilDirection * _recoilDistance;

            // —оздаЄм последовательность анимации
            _animationSequence = DOTween.Sequence();

            // ќтдача назад
            _animationSequence.Append(
                _gunTransform.DOLocalMove(recoilPosition, _recoilDuration)
                    .SetEase(Ease.OutCubic)
            );

            // “р€ска
            _animationSequence.Join(
                _gunTransform.DOShakePosition(
                    _recoilDuration,
                    _shakeIntensity,
                    _shakeVibrato,
                    fadeOut: false
                )
            );

            // ¬озврат в исходное положение
            _animationSequence.Append(
                _gunTransform.DOLocalMove(_originalGunPosition, _returnDuration)
                    .SetEase(Ease.Linear)
            );

            // ќбработка завершени€
            _animationSequence.OnComplete(OnAnimationComplete);
            _animationSequence.OnKill(OnAnimationComplete);

            _animationSequence.SetLink(gameObject);
        }

        private void OnAnimationComplete()
        {
            _isAnimating = false;
            ResetGunPosition();
        }

        private void ResetGunPosition()
        {
            if (_gunTransform != null)
            {
                _gunTransform.localPosition = _originalGunPosition;
            }
        }

        #endregion

        #region Private Methods Ч Smoke Effect

        private void PlaySmokeEffect()
        {
            if (_smokePrefab == null || _firePoint == null) return;

            try
            {
                // —оздаЄм экземпл€р дыма
                ParticleSystem smokeInstance = Instantiate(
                    _smokePrefab,
                    _firePoint.position,
                    CalculateSmokeRotation()
                );

                // Ќастраиваем направление
                ConfigureSmokeDirection(smokeInstance);

                // «апускаем и уничтожаем
                smokeInstance.Play();
                ScheduleSmokeDestruction(smokeInstance);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{nameof(TurretVisuals)}] Error playing smoke effect: {ex.Message}", this);
            }
        }

        private Quaternion CalculateSmokeRotation()
        {
            return Quaternion.Euler(-90f, 0f, 0f);
        }

        private void ConfigureSmokeDirection(ParticleSystem smoke)
        {
            if (_gunPivot == null) return;

            var shape = smoke.shape;
            float combinedAngle = (360f - _gunPivot.localEulerAngles.z) + (360f - transform.eulerAngles.z);
            shape.rotation = new Vector3(0f, combinedAngle, 0f);
        }

        private void ScheduleSmokeDestruction(ParticleSystem smoke)
        {
            var main = smoke.main;
            float lifetime = main.duration + main.startLifetime.constantMax;
            Destroy(smoke.gameObject, lifetime);
        }

        #endregion

        #region Private Methods Ч Cleanup

        private void CleanupAnimation()
        {
            if (_animationSequence != null)
            {
                _animationSequence.Kill();
                _animationSequence = null;
            }

            if (_gunTransform != null)
            {
                _gunTransform.DOKill();
            }

            _isAnimating = false;
        }

        #endregion
    }
}
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
        private Vector3 _originalGunLocalPosition;
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

        #region Private Methods — Initialization

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
                // Запоминаем локальную позицию — она не зависит от мирового поворота
                _originalGunLocalPosition = _gunTransform.localPosition;
            }
        }

        #endregion

        #region Private Methods — Event Subscription

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

        #region Private Methods — Event Handlers

        private void HandleShoot()
        {
            PlayRecoilAnimation();
            PlaySmokeEffect();
        }

        #endregion

        #region Private Methods — Recoil Animation

        private void PlayRecoilAnimation()
        {
            if (_gunTransform == null) return;
            if (_isAnimating) return;

            _isAnimating = true;

            CleanupAnimation();

            // ✅ Смещаем только по локальной оси Y вниз — не зависит от мирового поворота турели
            Vector3 recoilLocalPosition = _originalGunLocalPosition + Vector3.down * _recoilDistance;

            _animationSequence = DOTween.Sequence();

            // Отдача вниз в локальном пространстве
            _animationSequence.Append(
                _gunTransform.DOLocalMove(recoilLocalPosition, _recoilDuration)
                    .SetEase(Ease.OutCubic)
            );

            // Тряска в локальном пространстве
            _animationSequence.Join(
                _gunTransform.DOShakePosition(
                    _recoilDuration,
                    // ✅ Шейкаем только по локальной Y, чтобы не уходить в стороны
                    new Vector3(0f, _shakeIntensity, 0f),
                    _shakeVibrato,
                    fadeOut: false
                )
            );

            // Возврат в исходную локальную позицию
            _animationSequence.Append(
                _gunTransform.DOLocalMove(_originalGunLocalPosition, _returnDuration)
                    .SetEase(Ease.Linear)
            );

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
                // ✅ Сбрасываем локальную позицию
                _gunTransform.localPosition = _originalGunLocalPosition;
            }
        }

        #endregion

        #region Private Methods — Smoke Effect

        private void PlaySmokeEffect()
        {
            if (_smokePrefab == null || _firePoint == null) return;

            try
            {
                ParticleSystem smokeInstance = Instantiate(
                    _smokePrefab,
                    _firePoint.position,
                    CalculateSmokeRotation()
                );

                ConfigureSmokeDirection(smokeInstance);

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

        #region Private Methods — Cleanup

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
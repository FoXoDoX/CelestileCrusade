using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace My.Scripts.Gameplay.Pickups
{
    /// <summary>
    /// јнимаци€ pickup'ов: круговое движение, раст€гивание и мерцание.
    /// </summary>
    public class PickupAnimation : MonoBehaviour
    {
        #region Constants

        private const float STRETCH_SCALE_Y = 1.3f;
        private const float STRETCH_SCALE_X = 0.7f;
        private const float BLINK_INTERVAL = 4f;
        private const float FLASH_INTENSITY = 1.5f;

        #endregion

        #region Serialized Fields

        [Header("Stretch Animation")]
        [SerializeField] private float _stretchDuration = 0.5f;
        [SerializeField] private Vector2 _stretchIntervalRange = new(3f, 6f);

        [Header("Circle Animation")]
        [SerializeField] private float _circleRadius = 0.1f;
        [SerializeField] private float _circleDuration = 2f;

        [Header("Blink Animation")]
        [SerializeField] private float _blinkDuration = 0.3f;

        #endregion

        #region Private Fields

        private Transform _cachedTransform;
        private SpriteRenderer _spriteRenderer;

        private Vector3 _originalScale;
        private Vector3 _localCircleCenter;
        private Color _originalColor;

        private Coroutine _circleCoroutine;
        private Coroutine _stretchCoroutine;
        private Coroutine _blinkCoroutine;

        private bool _isAlive = true;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            SaveOriginalValues();
            StartAnimations();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void CacheComponents()
        {
            _cachedTransform = transform;
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_spriteRenderer == null)
            {
                Debug.LogWarning($"[{nameof(PickupAnimation)}] SpriteRenderer not found!", this);
            }
        }

        private void SaveOriginalValues()
        {
            _originalScale = _cachedTransform.localScale;
            _localCircleCenter = _cachedTransform.localPosition;

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        private void StartAnimations()
        {
            _circleCoroutine = StartCoroutine(CircleRoutine());
            _stretchCoroutine = StartCoroutine(StretchRoutine());
            _blinkCoroutine = StartCoroutine(BlinkRoutine());
        }

        #endregion

        #region Private Methods Ч Circle Animation

        private IEnumerator CircleRoutine()
        {
            float angle = 0f;

            while (_isAlive && _cachedTransform != null)
            {
                angle += Time.deltaTime * (2f * Mathf.PI / _circleDuration);

                float x = Mathf.Cos(angle) * _circleRadius;
                float y = Mathf.Sin(angle) * _circleRadius;

                _cachedTransform.localPosition = _localCircleCenter + new Vector3(x, y, 0f);

                if (angle >= 2f * Mathf.PI)
                {
                    angle = 0f;
                }

                yield return null;
            }
        }

        #endregion

        #region Private Methods Ч Stretch Animation

        private IEnumerator StretchRoutine()
        {
            while (_isAlive)
            {
                float delay = Random.Range(_stretchIntervalRange.x, _stretchIntervalRange.y);
                yield return new WaitForSeconds(delay);

                if (!_isAlive) yield break;

                PlayStretchAnimation();
            }
        }

        private void PlayStretchAnimation()
        {
            float halfDuration = _stretchDuration / 2f;

            Sequence sequence = DOTween.Sequence();

            // –аст€гиваем по Y, сжимаем по X
            sequence.Append(_cachedTransform.DOScaleY(_originalScale.y * STRETCH_SCALE_Y, halfDuration));
            sequence.Join(_cachedTransform.DOScaleX(_originalScale.x * STRETCH_SCALE_X, halfDuration));

            // ¬озвращаем к исходному размеру
            sequence.Append(_cachedTransform.DOScaleY(_originalScale.y, halfDuration));
            sequence.Join(_cachedTransform.DOScaleX(_originalScale.x, halfDuration));

            sequence.SetEase(Ease.OutQuad);
            sequence.SetLink(gameObject);
        }

        #endregion

        #region Private Methods Ч Blink Animation

        private IEnumerator BlinkRoutine()
        {
            while (_isAlive)
            {
                yield return new WaitForSeconds(BLINK_INTERVAL);

                if (!_isAlive || _spriteRenderer == null) yield break;

                PlayBlinkAnimation();
            }
        }

        private void PlayBlinkAnimation()
        {
            if (_spriteRenderer == null) return;

            Color flashColor = new Color(FLASH_INTENSITY, FLASH_INTENSITY, FLASH_INTENSITY, _originalColor.a);
            float thirdDuration = _blinkDuration / 3f;

            Sequence sequence = DOTween.Sequence();

            sequence.Append(_spriteRenderer.DOColor(flashColor, thirdDuration));
            sequence.Append(_spriteRenderer.DOColor(_originalColor, thirdDuration));

            sequence.SetEase(Ease.Flash);
            sequence.SetLink(gameObject);
        }

        #endregion

        #region Private Methods Ч Cleanup

        private void Cleanup()
        {
            _isAlive = false;

            StopAllAnimationCoroutines();
            KillAllTweens();
        }

        private void StopAllAnimationCoroutines()
        {
            if (_circleCoroutine != null)
            {
                StopCoroutine(_circleCoroutine);
                _circleCoroutine = null;
            }

            if (_stretchCoroutine != null)
            {
                StopCoroutine(_stretchCoroutine);
                _stretchCoroutine = null;
            }

            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
            }
        }

        private void KillAllTweens()
        {
            if (_cachedTransform != null)
            {
                DOTween.Kill(_cachedTransform);
            }

            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _stretchDuration = Mathf.Max(0.1f, _stretchDuration);
            _circleRadius = Mathf.Max(0f, _circleRadius);
            _circleDuration = Mathf.Max(0.1f, _circleDuration);
            _blinkDuration = Mathf.Max(0.1f, _blinkDuration);

            _stretchIntervalRange.x = Mathf.Max(0.1f, _stretchIntervalRange.x);
            _stretchIntervalRange.y = Mathf.Max(_stretchIntervalRange.x, _stretchIntervalRange.y);
        }
#endif

        #endregion
    }
}
using UnityEngine;
using DG.Tweening;

namespace My.Scripts.Environment.Hazards
{
    public class AcidBubble : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Visuals")]
        [SerializeField] private Transform _spriteTransform;
        [SerializeField] private SpriteRenderer _whiteOverlay;

        [Header("Growth")]
        [SerializeField] private float _growDuration = 1.5f;
        [SerializeField] private float _maxScale = 0.5f;
        [SerializeField] private AnimationCurve _growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Lifetime")]
        [SerializeField] private float _lifetimeMin = 2f;
        [SerializeField] private float _lifetimeMax = 3f;

        [Header("Wobble")]
        [SerializeField] private float _wobbleSpeed = 3f;
        [SerializeField] private float _wobbleAmount = 0.05f;

        [Header("Warning")]
        [SerializeField] private float _warningDuration = 0.5f;
        [SerializeField] private Ease _warningEase = Ease.InQuad;

        [Header("Splash")]
        [SerializeField] private GameObject _splashPrefab;

        #endregion

        #region Private Fields

        private float _timer;
        private float _lifetime;
        private bool _popped;
        private bool _warningStarted;
        private float _spriteHeight;
        private SpriteRenderer _spriteRenderer;
        private Tween _warningTween;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _lifetime = Random.Range(_lifetimeMin, _lifetimeMax);

            _spriteRenderer = _spriteTransform.GetComponent<SpriteRenderer>();

            if (_spriteRenderer != null && _spriteRenderer.sprite != null)
            {
                _spriteHeight = _spriteRenderer.sprite.bounds.size.y;
            }

            // ќверлей полностью прозрачный в начале
            if (_whiteOverlay != null)
            {
                Color c = _whiteOverlay.color;
                c.a = 0f;
                _whiteOverlay.color = c;
            }

            _spriteTransform.localScale = Vector3.zero;
        }

        private void Update()
        {
            if (_popped) return;

            _timer += Time.deltaTime;

            UpdateScale();
            CheckWarning();

            if (_timer >= _lifetime)
            {
                Pop();
            }
        }

        private void OnDestroy()
        {
            _warningTween?.Kill();
        }

        #endregion

        #region Private Methods

        private void UpdateScale()
        {
            float growProgress = Mathf.Clamp01(_timer / _growDuration);
            float curveValue = _growCurve.Evaluate(growProgress);
            float baseScale = curveValue * _maxScale;

            float wobbleX = 1f + Mathf.Sin(Time.time * _wobbleSpeed) * _wobbleAmount;
            float wobbleY = 1f + Mathf.Sin(Time.time * _wobbleSpeed * 1.3f + 0.5f) * _wobbleAmount;

            float scaleX = baseScale * wobbleX;
            float scaleY = baseScale * wobbleY;

            _spriteTransform.localScale = new Vector3(scaleX, scaleY, 1f);

            float offsetY = (scaleY * _spriteHeight) / 2f;
            _spriteTransform.localPosition = new Vector3(0f, offsetY, 0f);
        }

        private void CheckWarning()
        {
            if (_warningStarted) return;
            if (_whiteOverlay == null) return;

            float timeUntilPop = _lifetime - _timer;

            if (timeUntilPop <= _warningDuration)
            {
                _warningStarted = true;

                _warningTween = _whiteOverlay
                    .DOFade(0.25f, timeUntilPop)
                    .SetEase(_warningEase)
                    .SetLink(gameObject);
            }
        }

        private void Pop()
        {
            _popped = true;

            _warningTween?.Kill();

            if (_splashPrefab != null)
            {
                Instantiate(_splashPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        #endregion
    }
}
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace My.Scripts.UI.Popups
{
    /// <summary>
    /// јнимаци€ по€влени€ и исчезновени€ ScorePopup.
    /// </summary>
    public class ScorePopupAnimation : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Background References")]
        [SerializeField] private GameObject _whiteBackground;
        [SerializeField] private GameObject _background;

        [Header("Animation Settings")]
        [SerializeField] private float _squeezeDuration = 0.5f;
        [SerializeField] private float _backgroundSwitchTime = 0.05f;
        [SerializeField] private float _totalLifetime = 1.5f;
        [SerializeField] private float _disappearDuration = 0.5f;
        [SerializeField] private Vector3 _squeezeScale = new(1.5f, 0.5f, 1f);

        #endregion

        #region Private Fields

        private Vector3 _originalScale;
        private TextMeshPro _textMesh;
        private SpriteRenderer _whiteBackgroundRenderer;
        private SpriteRenderer _backgroundRenderer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            SaveOriginalValues();
            InitializeAnimationState();
            StartAnimationSequence();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ”станавливает текст popup'а.
        /// </summary>
        public void SetText(string text)
        {
            if (_textMesh != null)
            {
                _textMesh.text = text;
            }
        }

        #endregion

        #region Private Methods Ч Initialization

        private void CacheComponents()
        {
            _textMesh = GetComponentInChildren<TextMeshPro>();

            if (_whiteBackground != null)
            {
                _whiteBackgroundRenderer = _whiteBackground.GetComponent<SpriteRenderer>();
            }

            if (_background != null)
            {
                _backgroundRenderer = _background.GetComponent<SpriteRenderer>();
            }
        }

        private void SaveOriginalValues()
        {
            _originalScale = transform.localScale;
        }

        private void InitializeAnimationState()
        {
            // Ќачальное состо€ние: белый фон активен, обычный фон неактивен
            if (_whiteBackground != null)
            {
                _whiteBackground.SetActive(true);
            }

            if (_background != null)
            {
                _background.SetActive(false);
            }

            // ”станавливаем сжатый масштаб
            transform.localScale = _squeezeScale;

            // ƒелаем текст полностью прозрачным в начале
            if (_textMesh != null)
            {
                Color color = _textMesh.color;
                _textMesh.color = new Color(color.r, color.g, color.b, 0f);
            }
        }

        #endregion

        #region Private Methods Ч Animation

        private void StartAnimationSequence()
        {
            Sequence mainSequence = DOTween.Sequence();

            // 1. јнимаци€ "распр€млени€" (эластичный эффект)
            mainSequence.Append(
                transform.DOScale(_originalScale, _squeezeDuration)
                    .SetEase(Ease.OutElastic, 0.5f, 1f)
            );

            // ќдновременно Ч по€вление текста
            if (_textMesh != null)
            {
                mainSequence.Join(
                    _textMesh.DOFade(1f, _squeezeDuration * 0.7f)
                );
            }

            // 2. ѕереключение фонов
            mainSequence.InsertCallback(_backgroundSwitchTime, SwitchBackgrounds);

            // 3. ѕауза перед исчезновением
            float pauseDuration = _totalLifetime - _squeezeDuration - _disappearDuration;
            mainSequence.AppendInterval(pauseDuration);

            // 4. »счезновение
            mainSequence.Append(CreateDisappearSequence());

            // ”ничтожаем объект после завершени€
            mainSequence.OnComplete(() => Destroy(gameObject));
            mainSequence.SetLink(gameObject);
        }

        private Sequence CreateDisappearSequence()
        {
            Sequence disappearSequence = DOTween.Sequence();

            disappearSequence.Append(
                transform.DOScale(Vector3.zero, _disappearDuration)
                    .SetEase(Ease.InBack)
            );

            if (_textMesh != null)
            {
                disappearSequence.Join(_textMesh.DOFade(0f, _disappearDuration));
            }

            if (_whiteBackgroundRenderer != null)
            {
                disappearSequence.Join(_whiteBackgroundRenderer.DOFade(0f, _disappearDuration));
            }

            if (_backgroundRenderer != null)
            {
                disappearSequence.Join(_backgroundRenderer.DOFade(0f, _disappearDuration));
            }

            return disappearSequence;
        }

        private void SwitchBackgrounds()
        {
            if (_whiteBackground != null)
            {
                _whiteBackground.SetActive(false);
            }

            if (_background != null)
            {
                _background.SetActive(true);
            }
        }

        #endregion

        #region Private Methods Ч Cleanup

        private void KillAllTweens()
        {
            DOTween.Kill(transform);

            if (_textMesh != null)
            {
                DOTween.Kill(_textMesh);
            }

            if (_whiteBackgroundRenderer != null)
            {
                DOTween.Kill(_whiteBackgroundRenderer);
            }

            if (_backgroundRenderer != null)
            {
                DOTween.Kill(_backgroundRenderer);
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _squeezeDuration = Mathf.Max(0.01f, _squeezeDuration);
            _backgroundSwitchTime = Mathf.Max(0f, _backgroundSwitchTime);
            _totalLifetime = Mathf.Max(_squeezeDuration + _disappearDuration + 0.1f, _totalLifetime);
            _disappearDuration = Mathf.Max(0.01f, _disappearDuration);
        }

        private void Reset()
        {
            _whiteBackground = transform.Find("WhiteBackground")?.gameObject;
            _background = transform.Find("Background")?.gameObject;
        }
#endif

        #endregion
    }
}
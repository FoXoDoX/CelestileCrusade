using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.UI.Menus
{
    /// <summary>
    /// јнимаци€ по€влени€ меню после посадки: раст€гивание, возврат и по€вление звЄзд.
    /// </summary>
    public class LandedMenuUIAnimation : MonoBehaviour
    {
        #region Constants

        private const float STAR_ANIMATION_DURATION = 0.3f;
        private const float STAR_STAGGER_DELAY = 0.5f;

        #endregion

        #region Serialized Fields

        [Header("Animation Settings")]
        [SerializeField] private float _stretchOutDuration = 0.1f;
        [SerializeField] private float _bounceBackDuration = 0.4f;
        [SerializeField] private float _initialVerticalStretch = 1.5f;

        [Header("UI References")]
        [SerializeField] private RectTransform _mainPanel;
        [SerializeField] private List<Transform> _stars;

        #endregion

        #region Private Fields

        private Vector3 _originalScale;
        private List<Graphic> _colorElements;
        private List<Color> _originalColors;

        #endregion

        #region Properties

        private float TotalAnimationDuration => _stretchOutDuration + _bounceBackDuration;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheOriginalValues();
            ResetToInitialState();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        #endregion

        #region Public Methods

        public void PlayEnterAnimation()
        {
            ResetToInitialState();

            Sequence mainSequence = DOTween.Sequence();

            AnimatePanel(mainSequence);
            AnimateColors();
            AnimateStars(mainSequence);

            mainSequence.SetLink(gameObject);
        }

        #endregion

        #region Private Methods Ч Initialization

        private void CacheOriginalValues()
        {
            // —охран€ем оригинальный масштаб
            if (_mainPanel != null)
            {
                _originalScale = _mainPanel.localScale;
            }

            // Ќаходим все Graphic компоненты
            _colorElements = new List<Graphic>(GetComponentsInChildren<Graphic>(true));
            _originalColors = new List<Color>();

            foreach (var element in _colorElements)
            {
                if (element != null)
                {
                    _originalColors.Add(element.color);
                }
            }
        }

        private void ResetToInitialState()
        {
            ResetPanelScale();
            ResetColors();
            ResetStars();
        }

        private void ResetPanelScale()
        {
            if (_mainPanel != null)
            {
                _mainPanel.localScale = _originalScale;
            }
        }

        private void ResetColors()
        {
            if (_colorElements == null) return;

            foreach (var element in _colorElements)
            {
                if (element != null)
                {
                    element.color = Color.white;
                }
            }
        }

        private void ResetStars()
        {
            if (_stars == null) return;

            foreach (var star in _stars)
            {
                if (star != null)
                {
                    star.localScale = Vector3.zero;
                }
            }
        }

        #endregion

        #region Private Methods Ч Animation

        private void AnimatePanel(Sequence sequence)
        {
            if (_mainPanel == null) return;

            _mainPanel.localScale = _originalScale;

            // –аст€гивание по вертикали
            sequence.Append(
                _mainPanel.DOScaleY(_originalScale.y * _initialVerticalStretch, _stretchOutDuration)
                    .SetEase(Ease.OutQuad)
            );

            // ¬озврат к исходному масштабу с отскоком
            sequence.Append(
                _mainPanel.DOScaleY(_originalScale.y, _bounceBackDuration)
                    .SetEase(Ease.OutBack)
            );
        }

        private void AnimateColors()
        {
            if (_colorElements == null || _originalColors == null) return;

            Sequence colorSequence = DOTween.Sequence();

            for (int i = 0; i < _colorElements.Count; i++)
            {
                if (_colorElements[i] == null) continue;
                if (i >= _originalColors.Count) break;

                _colorElements[i].color = Color.white;

                colorSequence.Join(
                    _colorElements[i].DOColor(_originalColors[i], TotalAnimationDuration)
                        .SetEase(Ease.OutQuad)
                );
            }

            colorSequence.SetLink(gameObject);
        }

        private void AnimateStars(Sequence sequence)
        {
            if (_stars == null || _stars.Count == 0) return;

            for (int i = 0; i < _stars.Count; i++)
            {
                if (_stars[i] == null) continue;

                _stars[i].localScale = Vector3.zero;

                float delay = TotalAnimationDuration + i * STAR_STAGGER_DELAY;

                sequence.Insert(
                    delay,
                    _stars[i].DOScale(Vector3.one, STAR_ANIMATION_DURATION)
                        .SetEase(Ease.OutBack)
                );
            }
        }

        #endregion

        #region Private Methods Ч Cleanup

        private void KillAllTweens()
        {
            if (_mainPanel != null)
            {
                _mainPanel.DOKill();
            }

            if (_colorElements == null) return;

            foreach (var element in _colorElements)
            {
                if (element != null)
                {
                    element.DOKill();
                }
            }

            if (_stars == null) return;

            foreach (var star in _stars)
            {
                if (star != null)
                {
                    star.DOKill();
                }
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _stretchOutDuration = Mathf.Max(0.01f, _stretchOutDuration);
            _bounceBackDuration = Mathf.Max(0.01f, _bounceBackDuration);
            _initialVerticalStretch = Mathf.Max(1f, _initialVerticalStretch);
        }
#endif

        #endregion
    }
}
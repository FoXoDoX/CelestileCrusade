using System;
using DG.Tweening;
using UnityEngine;

namespace My.Scripts.UI.Menus
{
    /// <summary>
    /// Анимация появления и закрытия меню настроек.
    /// Появление: 0 → overshoot → нормальный размер.
    /// Закрытие: текущий размер → 0.
    /// </summary>
    public class SettingsMenuUIAnimation : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Show Animation")]
        [SerializeField] private float _showOvershootScale = 1.08f;
        [SerializeField] private float _showDuration = 0.25f;
        [SerializeField] private float _showBounceDuration = 0.1f;

        [Header("Hide Animation")]
        [SerializeField] private float _hideDuration = 0.15f;

        [Header("Target")]
        [SerializeField] private RectTransform _animatedRoot;

        #endregion

        #region Private Fields

        private Vector3 _originalScale;
        private Tween _tween;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_animatedRoot == null)
            {
                _animatedRoot = GetComponent<RectTransform>();
            }

            _originalScale = _animatedRoot.localScale;
        }

        private void OnDestroy()
        {
            KillTween();
        }

        #endregion

        #region Public Methods

        public void PlayShow(Action onComplete = null)
        {
            KillTween();

            _animatedRoot.localScale = Vector3.zero;

            _tween = _animatedRoot
                .DOScale(_originalScale * _showOvershootScale, _showDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _tween = _animatedRoot
                        .DOScale(_originalScale, _showBounceDuration)
                        .SetEase(Ease.InOutQuad)
                        .SetUpdate(true)
                        .OnComplete(() => onComplete?.Invoke());
                });
        }

        public void PlayHide(Action onComplete = null)
        {
            KillTween();

            _tween = _animatedRoot
                .DOScale(Vector3.zero, _hideDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    _animatedRoot.localScale = _originalScale;
                    onComplete?.Invoke();
                });
        }

        #endregion

        #region Private Methods

        private void KillTween()
        {
            _tween?.Kill();
            _tween = null;
        }

        #endregion
    }
}
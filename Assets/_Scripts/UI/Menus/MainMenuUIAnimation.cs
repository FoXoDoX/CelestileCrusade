using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace My.Scripts.UI.Menus
{
    public class MainMenuUIAnimation : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Elements")]
        [SerializeField] private RectTransform _logo;
        [SerializeField] private RectTransform[] _buttons;

        [Header("Logo Animation")]
        [SerializeField] private float _logoOffscreenOffset = 800f;
        [SerializeField] private float _logoDuration = 0.6f;
        [SerializeField] private float _logoDelay = 0f;

        [Header("Logo Floating")]
        [SerializeField] private float _floatAmplitude = 10f;
        [SerializeField] private float _floatDuration = 2f;
        [SerializeField] private Ease _floatEase = Ease.InOutSine;

        [Header("Buttons Animation")]
        [SerializeField] private float _buttonsOffscreenOffset = 800f;
        [SerializeField] private float _firstButtonDuration = 0.4f;
        [SerializeField] private float _lastButtonDuration = 0.7f;
        [SerializeField] private float _buttonsStartDelay = 0.1f;
        [SerializeField] private float _delayBetweenButtons = 0.08f;

        [Header("Ease")]
        [SerializeField] private Ease _ease = Ease.OutCubic;

        [Header("Start Delay")]
        [SerializeField] private int _waitFrames = 2;

        #endregion

        #region Events

        public event Action OnAnimationCompleted;

        #endregion

        #region Private Fields

        private Vector2 _logoOriginalPosition;
        private Vector2[] _buttonsOriginalPositions;
        private Sequence _sequence;
        private Tween _floatTween;
        private CanvasGroup _canvasGroup;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup.alpha = 0f;

            CacheOriginalPositions();
            SetStartPositions();
        }

        private void Start()
        {
            StartCoroutine(WaitAndPlay());
        }

        private void OnDestroy()
        {
            KillSequence();
            KillFloatTween();
            OnAnimationCompleted = null;
        }

        #endregion

        #region Private Methods — Wait

        private IEnumerator WaitAndPlay()
        {
            for (int i = 0; i < _waitFrames; i++)
            {
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            PlayEnterAnimation();
        }

        #endregion

        #region Private Methods — Cache

        private void CacheOriginalPositions()
        {
            if (_logo != null)
            {
                _logoOriginalPosition = _logo.anchoredPosition;
            }

            if (_buttons != null && _buttons.Length > 0)
            {
                _buttonsOriginalPositions = new Vector2[_buttons.Length];

                for (int i = 0; i < _buttons.Length; i++)
                {
                    if (_buttons[i] != null)
                    {
                        _buttonsOriginalPositions[i] = _buttons[i].anchoredPosition;
                    }
                }
            }
        }

        #endregion

        #region Private Methods — Animation

        private void PlayEnterAnimation()
        {
            KillSequence();
            KillFloatTween();
            SetStartPositions();

            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .OnComplete(() => OnAnimationCompleted?.Invoke());

            AnimateLogo();
            AnimateButtons();
        }

        private void SetStartPositions()
        {
            if (_logo != null)
            {
                _logo.anchoredPosition = new Vector2(
                    _logoOriginalPosition.x,
                    _logoOriginalPosition.y + _logoOffscreenOffset
                );
            }

            if (_buttons == null) return;

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] != null)
                {
                    _buttons[i].anchoredPosition = new Vector2(
                        _buttonsOriginalPositions[i].x,
                        _buttonsOriginalPositions[i].y - _buttonsOffscreenOffset
                    );
                }
            }
        }

        private void AnimateLogo()
        {
            if (_logo == null) return;

            _sequence.Insert(
                _logoDelay,
                _logo
                    .DOAnchorPos(_logoOriginalPosition, _logoDuration)
                    .SetEase(_ease)
                    .OnComplete(StartLogoFloat)
            );
        }

        private void AnimateButtons()
        {
            if (_buttons == null || _buttons.Length == 0) return;

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] == null) continue;

                float duration = Mathf.Lerp(
                    _firstButtonDuration,
                    _lastButtonDuration,
                    _buttons.Length > 1 ? (float)i / (_buttons.Length - 1) : 0f
                );

                float delay = _buttonsStartDelay + i * _delayBetweenButtons;

                _sequence.Insert(
                    delay,
                    _buttons[i]
                        .DOAnchorPos(_buttonsOriginalPositions[i], duration)
                        .SetEase(_ease)
                );
            }
        }

        #endregion

        #region Private Methods — Logo Float

        private void StartLogoFloat()
        {
            if (_logo == null) return;

            KillFloatTween();

            Vector2 floatTarget = new Vector2(
                _logoOriginalPosition.x,
                _logoOriginalPosition.y + _floatAmplitude
            );

            _floatTween = _logo
                .DOAnchorPosY(floatTarget.y, _floatDuration)
                .SetEase(_floatEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        #endregion

        #region Private Methods — Cleanup

        private void KillSequence()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        private void KillFloatTween()
        {
            _floatTween?.Kill();
            _floatTween = null;
        }

        #endregion
    }
}
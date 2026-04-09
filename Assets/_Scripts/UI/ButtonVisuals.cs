using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.Scripts.UI
{
    [RequireComponent(typeof(Selectable))]
    public class ButtonVisuals : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        #region Serialized Fields

        [Header("Visual Root")]
        [SerializeField] private Transform _visualsRoot;

        [Header("Selection Indicators")]
        [SerializeField] private GameObject _miniCrest1;
        [SerializeField] private GameObject _miniCrest2;

        [Header("Crest Rotation")]
        [SerializeField] private float _crestRotationDuration = 2f;
        [SerializeField] private Ease _crestRotationEase = Ease.Linear;

        [Header("Select Animation")]
        [SerializeField] private float _shrinkScale = 0.9f;
        [SerializeField] private float _bounceScale = 0.95f;
        [SerializeField] private float _shrinkDuration = 0.1f;
        [SerializeField] private float _bounceDuration = 0.08f;

        [Header("Deselect Animation")]
        [SerializeField] private float _deselectDuration = 0.3f;

        [Header("Click Animation")]
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private float _textClickPunchStrength = 0.2f;
        [SerializeField] private float _crestClickPunchStrength = 0.45f;
        [SerializeField] private float _clickAnimationDuration = 0.15f;

        #endregion

        #region Private Fields

        private Vector3 _originalScale;
        private Vector3 _originalTextScale;
        private Vector3 _originalCrest1Scale;
        private Vector3 _originalCrest2Scale;
        private Tween _scaleTween;
        private Tween _textTween;
        private Tween _crest1ClickTween;
        private Tween _crest2ClickTween;
        private Tween _crest1RotationTween;
        private Tween _crest2RotationTween;
        private Button _button;

        private event Action _onDelayedClick;
        private bool _isProcessingClick;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_visualsRoot == null)
            {
                _visualsRoot = transform;
            }

            _originalScale = _visualsRoot.localScale;
            _button = GetComponent<Button>();

            if (_buttonText == null)
            {
                _buttonText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (_buttonText != null)
            {
                _originalTextScale = _buttonText.transform.localScale;
            }

            if (_miniCrest1 != null)
            {
                _originalCrest1Scale = _miniCrest1.transform.localScale;
            }

            if (_miniCrest2 != null)
            {
                _originalCrest2Scale = _miniCrest2.transform.localScale;
            }

            SetCrestsActive(false);
        }

        private void OnEnable()
        {
            _button?.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button?.onClick.RemoveListener(HandleClick);
            _isProcessingClick = false;
            SetCrestsActive(false);
        }

        private void OnDestroy()
        {
            KillScaleTween();
            KillTextTween();
            KillCrestClickTweens();
            StopCrestRotations();
            _onDelayedClick = null;
        }

        #endregion

        #region Public Methods

        public void AddDelayedListener(Action callback)
        {
            _onDelayedClick += callback;
        }

        public void RemoveDelayedListener(Action callback)
        {
            _onDelayedClick -= callback;
        }

        #endregion

        #region Event Handlers

        public void OnSelect(BaseEventData eventData)
        {
            SetCrestsActive(true);
            PlaySelectAnimation();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetCrestsActive(false);
            PlayDeselectAnimation();
        }

        #endregion

        #region Private Methods — Click Handling

        private void HandleClick()
        {
            if (_isProcessingClick) return;

            if (_onDelayedClick != null)
            {
                _isProcessingClick = true;

                PlayClickAnimation(() =>
                {
                    _isProcessingClick = false;
                    _onDelayedClick?.Invoke();
                });
            }
            else
            {
                PlayClickAnimation(null);
            }
        }

        #endregion

        #region Private Methods — Animations

        private void PlaySelectAnimation()
        {
            KillScaleTween();

            _scaleTween = _visualsRoot
                .DOScale(_originalScale * _shrinkScale, _shrinkDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _scaleTween = _visualsRoot
                        .DOScale(_originalScale * _bounceScale, _bounceDuration)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                });
        }

        private void PlayDeselectAnimation()
        {
            KillScaleTween();

            _scaleTween = _visualsRoot
                .DOScale(_originalScale, _deselectDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void PlayClickAnimation(Action onComplete)
        {
            if (_buttonText == null)
            {
                onComplete?.Invoke();
                return;
            }

            // Текст
            KillTextTween();
            _buttonText.transform.localScale = _originalTextScale;

            _textTween = _buttonText.transform
                .DOPunchScale(Vector3.one * -_textClickPunchStrength, _clickAnimationDuration, 1, 0.5f)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());

            // Кресты
            var crestPunchVector = Vector3.one * -_crestClickPunchStrength;

            KillCrestClickTweens();

            if (_miniCrest1 != null && _miniCrest1.activeInHierarchy)
            {
                _miniCrest1.transform.localScale = _originalCrest1Scale;
                _crest1ClickTween = _miniCrest1.transform
                    .DOPunchScale(crestPunchVector, _clickAnimationDuration, 1, 0.5f)
                    .SetUpdate(true);
            }

            if (_miniCrest2 != null && _miniCrest2.activeInHierarchy)
            {
                _miniCrest2.transform.localScale = _originalCrest2Scale;
                _crest2ClickTween = _miniCrest2.transform
                    .DOPunchScale(crestPunchVector, _clickAnimationDuration, 1, 0.5f)
                    .SetUpdate(true);
            }
        }

        #endregion

        #region Private Methods — Crests

        private void SetCrestsActive(bool active)
        {
            if (_miniCrest1 != null) _miniCrest1.SetActive(active);
            if (_miniCrest2 != null) _miniCrest2.SetActive(active);

            if (active)
                StartCrestRotations();
            else
                StopCrestRotations();
        }

        private void StartCrestRotations()
        {
            StopCrestRotations();

            if (_miniCrest1 != null)
            {
                _miniCrest1.transform.localRotation = Quaternion.identity;
                _crest1RotationTween = _miniCrest1.transform
                    .DOLocalRotate(new Vector3(0f, 360f, 0f), _crestRotationDuration, RotateMode.FastBeyond360)
                    .SetEase(_crestRotationEase)
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true);
            }

            if (_miniCrest2 != null)
            {
                _miniCrest2.transform.localRotation = Quaternion.identity;
                _crest2RotationTween = _miniCrest2.transform
                    .DOLocalRotate(new Vector3(0f, 360f, 0f), _crestRotationDuration, RotateMode.FastBeyond360)
                    .SetEase(_crestRotationEase)
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true);
            }
        }

        private void StopCrestRotations()
        {
            if (_crest1RotationTween != null)
            {
                _crest1RotationTween.Kill();
                _crest1RotationTween = null;
            }

            if (_crest2RotationTween != null)
            {
                _crest2RotationTween.Kill();
                _crest2RotationTween = null;
            }

            if (_miniCrest1 != null) _miniCrest1.transform.localRotation = Quaternion.identity;
            if (_miniCrest2 != null) _miniCrest2.transform.localRotation = Quaternion.identity;
        }

        #endregion

        #region Private Methods — Cleanup

        private void KillScaleTween()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        private void KillTextTween()
        {
            _textTween?.Kill();
            _textTween = null;
        }

        private void KillCrestClickTweens()
        {
            if (_crest1ClickTween != null)
            {
                _crest1ClickTween.Kill();
                _crest1ClickTween = null;
            }

            if (_crest2ClickTween != null)
            {
                _crest2ClickTween.Kill();
                _crest2ClickTween = null;
            }
        }

        #endregion
    }
}
using System.Collections.Generic;
using DG.Tweening;
using My.Scripts.EventBus;          // ← добавлено
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.UI.Menus
{
    /// <summary>
    /// Анимация появления меню после посадки: растягивание, возврат, белая вспышка,
    /// набор счёта с прогресс-баром, появление гербов и смена ранга.
    /// </summary>
    public class LandedUIAnimation : MonoBehaviour
    {
        #region Constants

        private const float CREST_ANIMATION_DURATION = 0.4f;
        private const float CREST_INITIAL_SCALE = 5f;
        private const float CREST_INITIAL_ALPHA = 0f;

        private const float BURST_FINAL_SCALE = 2.5f;

        private const float SCORE_DELAY_AFTER_FLASH = 0.1f;
        private const float SCORE_COUNT_DURATION = 2.5f;

        private const float SHAKE_DURATION = 0.3f;
        private const float SHAKE_STRENGTH = 10f;
        private const int SHAKE_VIBRATO = 15;
        private const float SHAKE_RANDOMNESS = 90f;

        private const float RANK_DELAY_AFTER_SCORE = 0.3f;
        private const float RANK_FLASH_FADE_IN = 0.3f;
        private const float RANK_FLASH_FADE_OUT = 0.4f;

        #endregion

        #region Serialized Fields

        [Header("Animation Settings")]
        [SerializeField] private float _stretchOutDuration = 0.1f;
        [SerializeField] private float _bounceBackDuration = 0.4f;
        [SerializeField] private float _initialVerticalStretch = 1.5f;

        [Header("UI References")]
        [SerializeField] private RectTransform _mainPanel;
        [SerializeField] private TextMeshProUGUI _totalScoreText;

        [Header("Progress Bar")]
        [SerializeField] private Image _progressBarImage;
        [SerializeField, Range(0f, 0.2f)] private float _crestRightPadding = 0.05f;

        [Header("Crests")]
        [Tooltip("Контейнеры гербов (Crest1, Crest2, Crest3) — для позиционирования на прогресс-баре")]
        [SerializeField] private List<RectTransform> _crestContainers;

        [Tooltip("Graphic-компоненты гербов (EarnedCrestImage) — для анимации появления")]
        [SerializeField] private List<Graphic> _crestGraphics;

        [Header("Flash Overlays")]
        [Tooltip("Белые копии UI-элементов для эффекта вспышки при успехе (включая WhiteBanner от SuccessBanner)")]
        [SerializeField] private List<Image> _successFlashOverlays;

        [Tooltip("Белые копии UI-элементов для эффекта вспышки при краше (WhiteBanner от CrashBanner)")]
        [SerializeField] private List<Image> _crashFlashOverlays;

        [SerializeField] private float _flashDuration = 0.3f;
        [SerializeField] private Ease _flashEase = Ease.OutQuad;

        [Header("Crest Burst Effect")]
        [SerializeField] private Sprite _burstSprite;
        [SerializeField] private Color _burstColor = new Color(1f, 0.9f, 0.3f, 0.8f);
        [SerializeField] private float _burstDuration = 0.45f;
        [SerializeField] private float _burstInitialScale = 0.5f;

        [Header("Rank Display")]
        [SerializeField] private Image _rankImage;
        [SerializeField] private Image _rankFlashImage;
        [Tooltip("Спрайты ранга: 0 гербов, 1 герб, 2 герба, 3 герба")]
        [SerializeField] private List<Sprite> _rankSprites;

        #endregion

        #region Private Fields

        private Vector3 _originalScale;
        private Vector2 _originalAnchoredPosition;
        private Sprite _originalRankSprite;
        private readonly List<Image> _burstImages = new();
        private int _earnedCrestsCount;
        private int _totalScore;
        private int _maxScore;
        private int[] _crestThresholds;
        private bool[] _crestAnimationTriggered;
        private bool _isSuccess;
        private bool _rankAnimationTriggered;

        #endregion

        #region Properties

        private float TotalAnimationDuration => _stretchOutDuration + _bounceBackDuration;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheOriginalValues();
            CreateBurstImages();
            ResetToInitialState();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        #endregion

        #region Public Methods

        public void PlayEnterAnimation(int earnedCrestsCount, int totalScore, int[] crestThresholds, bool isSuccess)
        {
            _earnedCrestsCount = earnedCrestsCount;
            _totalScore = totalScore;
            _crestThresholds = crestThresholds;
            _maxScore = (crestThresholds != null && crestThresholds.Length > 0)
                ? crestThresholds[crestThresholds.Length - 1]
                : 0;
            _isSuccess = isSuccess;

            _crestAnimationTriggered = new bool[_crestGraphics?.Count ?? 0];
            _rankAnimationTriggered = false;

            ResetToInitialState();
            PositionCrestsOnProgressBar();

            Sequence mainSequence = DOTween.Sequence();

            AnimatePanel(mainSequence);
            AnimateFlashOverlays(mainSequence);
            AnimateScoreCount(mainSequence);

            mainSequence.SetLink(gameObject);
        }

        #endregion

        #region Private Methods — Initialization

        private void CacheOriginalValues()
        {
            if (_mainPanel != null)
            {
                _originalScale = _mainPanel.localScale;
                _originalAnchoredPosition = _mainPanel.anchoredPosition;
            }

            if (_rankImage != null)
            {
                _originalRankSprite = _rankImage.sprite;
            }
        }

        private void CreateBurstImages()
        {
            if (_crestGraphics == null || _burstSprite == null) return;

            foreach (var crest in _crestGraphics)
            {
                if (crest == null) continue;

                var burstObj = new GameObject("CrestBurst");
                burstObj.transform.SetParent(crest.transform, false);

                var rectTransform = burstObj.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = (crest.transform as RectTransform)?.sizeDelta ?? new Vector2(100f, 100f);

                var image = burstObj.AddComponent<Image>();
                image.sprite = _burstSprite;
                image.color = _burstColor;
                image.raycastTarget = false;

                burstObj.transform.SetAsFirstSibling();

                burstObj.SetActive(false);
                _burstImages.Add(image);
            }
        }

        private void ResetToInitialState()
        {
            ResetPanelScale();
            ResetPanelPosition();
            ResetFlashOverlays();
            ResetScoreText();
            ResetProgressBar();
            ResetCrests();
            ResetBursts();
            ResetRank();
        }

        private void ResetPanelScale()
        {
            if (_mainPanel != null)
            {
                _mainPanel.localScale = _originalScale;
            }
        }

        private void ResetPanelPosition()
        {
            if (_mainPanel != null)
            {
                _mainPanel.anchoredPosition = _originalAnchoredPosition;
            }
        }

        private void ResetFlashOverlays()
        {
            ResetOverlayList(_successFlashOverlays);
            ResetOverlayList(_crashFlashOverlays);
        }

        private void ResetOverlayList(List<Image> overlays)
        {
            if (overlays == null) return;

            foreach (var overlay in overlays)
            {
                if (overlay == null) continue;

                overlay.DOKill();

                var color = overlay.color;
                color.a = 1f;
                overlay.color = color;

                overlay.gameObject.SetActive(true);
            }
        }

        private void ResetScoreText()
        {
            if (_totalScoreText != null)
            {
                _totalScoreText.DOKill();
                _totalScoreText.text = "0";
            }
        }

        private void ResetProgressBar()
        {
            if (_progressBarImage != null)
            {
                _progressBarImage.DOKill();
                _progressBarImage.fillAmount = 0f;
            }
        }

        private void PositionCrestsOnProgressBar()
        {
            if (_crestContainers == null || _crestThresholds == null) return;
            if (_maxScore <= 0) return;

            float usableWidth = 1f - _crestRightPadding;

            for (int i = 0; i < _crestContainers.Count && i < _crestThresholds.Length; i++)
            {
                if (_crestContainers[i] == null) continue;

                float normalizedX = Mathf.Clamp01((float)_crestThresholds[i] / _maxScore) * usableWidth;

                float anchorY = 0.5f;
                float posY = _crestContainers[i].anchoredPosition.y;
                Vector2 sizeDelta = _crestContainers[i].sizeDelta;

                _crestContainers[i].anchorMin = new Vector2(normalizedX, anchorY);
                _crestContainers[i].anchorMax = new Vector2(normalizedX, anchorY);
                _crestContainers[i].anchoredPosition = new Vector2(0f, posY);
                _crestContainers[i].sizeDelta = sizeDelta;
            }
        }

        private void ResetCrests()
        {
            if (_crestGraphics == null) return;

            foreach (var crest in _crestGraphics)
            {
                if (crest == null) continue;

                crest.transform.DOKill();
                crest.DOKill();

                crest.transform.localScale = Vector3.one * CREST_INITIAL_SCALE;

                var color = crest.color;
                color.a = CREST_INITIAL_ALPHA;
                crest.color = color;
            }
        }

        private void ResetBursts()
        {
            foreach (var burst in _burstImages)
            {
                if (burst == null) continue;

                burst.DOKill();
                burst.transform.DOKill();

                burst.transform.localScale = Vector3.one * _burstInitialScale;
                burst.color = _burstColor;

                burst.gameObject.SetActive(false);
            }
        }

        private void ResetRank()
        {
            if (_rankFlashImage != null)
            {
                _rankFlashImage.DOKill();
                var color = _rankFlashImage.color;
                color.a = 0f;
                _rankFlashImage.color = color;
            }

            if (_rankImage != null)
            {
                _rankImage.sprite = _originalRankSprite;
                _rankImage.gameObject.SetActive(_isSuccess);
            }

            if (_rankFlashImage != null)
            {
                _rankFlashImage.gameObject.SetActive(_isSuccess);
            }
        }

        #endregion

        #region Private Methods — Animation

        private void AnimatePanel(Sequence sequence)
        {
            if (_mainPanel == null) return;

            _mainPanel.localScale = _originalScale;

            sequence.Append(
                _mainPanel.DOScaleY(_originalScale.y * _initialVerticalStretch, _stretchOutDuration)
                    .SetEase(Ease.OutQuad)
            );

            sequence.Append(
                _mainPanel.DOScaleY(_originalScale.y, _bounceBackDuration)
                    .SetEase(Ease.OutBack)
            );
        }

        private void AnimateFlashOverlays(Sequence sequence)
        {
            var overlays = _isSuccess ? _successFlashOverlays : _crashFlashOverlays;

            if (overlays == null || overlays.Count == 0) return;

            foreach (var overlay in overlays)
            {
                if (overlay == null) continue;

                sequence.Join(
                    overlay.DOFade(0f, _flashDuration)
                        .SetEase(_flashEase)
                        .OnComplete(() => overlay.gameObject.SetActive(false))
                );
            }
        }

        private void AnimateScoreCount(Sequence sequence)
        {
            if (_totalScoreText == null) return;

            float scoreDelay = _flashDuration + SCORE_DELAY_AFTER_FLASH;

            sequence.Insert(
                scoreDelay,
                DOVirtual.Int(0, _totalScore, SCORE_COUNT_DURATION, value =>
                {
                    _totalScoreText.text = value.ToString();

                    if (_progressBarImage != null && _maxScore > 0)
                    {
                        _progressBarImage.fillAmount = Mathf.Clamp01((float)value / _maxScore);
                    }

                    TryTriggerCrestAnimations(value);
                    TryTriggerRankAnimation(value);
                })
                .SetEase(Ease.OutCubic)
                .OnComplete(() => TryTriggerRankAnimation(_totalScore))
            );
        }

        private void TryTriggerRankAnimation(int currentScore)
        {
            if (_rankAnimationTriggered) return;
            if (!_isSuccess) return;

            int triggerScore = (_maxScore > 0)
                ? Mathf.Min(_totalScore, _maxScore)
                : _totalScore;

            if (currentScore < triggerScore) return;

            _rankAnimationTriggered = true;
            PlayRankReveal();
        }

        private void PlayRankReveal()
        {
            if (_rankImage == null || _rankFlashImage == null) return;
            if (_rankSprites == null || _rankSprites.Count < 4) return;

            int spriteIndex = Mathf.Clamp(_earnedCrestsCount, 0, _rankSprites.Count - 1);

            DOVirtual.DelayedCall(RANK_DELAY_AFTER_SCORE, () =>
            {
                if (_rankFlashImage == null) return;

                _rankFlashImage
                    .DOFade(1f, RANK_FLASH_FADE_IN)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        if (_rankImage != null)
                        {
                            _rankImage.sprite = _rankSprites[spriteIndex];
                        }

                        if (_rankFlashImage != null)
                        {
                            _rankFlashImage
                                .DOFade(0f, RANK_FLASH_FADE_OUT)
                                .SetEase(Ease.OutQuad)
                                .SetLink(gameObject);
                        }
                    })
                    .SetLink(gameObject);
            }).SetLink(gameObject);
        }

        private void TryTriggerCrestAnimations(int currentScore)
        {
            if (_crestThresholds == null || _crestGraphics == null) return;

            for (int i = 0; i < _crestThresholds.Length && i < _crestGraphics.Count; i++)
            {
                if (_crestAnimationTriggered[i]) continue;
                if (i >= _earnedCrestsCount) continue;
                if (currentScore < _crestThresholds[i]) continue;

                _crestAnimationTriggered[i] = true;
                PlayCrestAnimation(i);
            }
        }

        private void PlayCrestAnimation(int index)
        {
            if (_crestGraphics[index] == null) return;

            bool isLastCrest = (index == _earnedCrestsCount - 1);    // ← добавлено

            _crestGraphics[index].transform
                .DOScale(Vector3.one, CREST_ANIMATION_DURATION)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    ShakePanel();

                    var crestEvent = isLastCrest                      // ← изменено
                        ? GameEvents.LastCrestRevealed
                        : GameEvents.CrestRevealed;
                    EventManager.Instance?.Broadcast(crestEvent);
                })
                .SetLink(gameObject);

            _crestGraphics[index]
                .DOFade(1f, CREST_ANIMATION_DURATION)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject);

            if (index < _burstImages.Count && _burstImages[index] != null)
            {
                PlayBurstAnimation(index);
            }
        }

        private void ShakePanel()
        {
            if (_mainPanel == null) return;

            _mainPanel.DOKill(complete: true);
            _mainPanel.anchoredPosition = _originalAnchoredPosition;

            _mainPanel
                .DOShakeAnchorPos(SHAKE_DURATION, SHAKE_STRENGTH, SHAKE_VIBRATO, SHAKE_RANDOMNESS)
                .OnComplete(() => _mainPanel.anchoredPosition = _originalAnchoredPosition)
                .SetLink(gameObject);
        }

        private void PlayBurstAnimation(int index)
        {
            var burst = _burstImages[index];

            DOVirtual.DelayedCall(CREST_ANIMATION_DURATION, () =>
            {
                if (burst == null) return;

                burst.gameObject.SetActive(true);
                burst.transform.localScale = Vector3.one * _burstInitialScale;
                burst.color = _burstColor;

                burst.transform
                    .DOScale(Vector3.one * BURST_FINAL_SCALE, _burstDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);

                burst
                    .DOFade(0f, _burstDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => burst.gameObject.SetActive(false))
                    .SetLink(gameObject);
            }).SetLink(gameObject);
        }

        #endregion

        #region Private Methods — Cleanup

        private void KillAllTweens()
        {
            if (_mainPanel != null)
            {
                _mainPanel.DOKill();
            }

            KillOverlayList(_successFlashOverlays);
            KillOverlayList(_crashFlashOverlays);

            if (_totalScoreText != null)
            {
                _totalScoreText.DOKill();
            }

            if (_progressBarImage != null)
            {
                _progressBarImage.DOKill();
            }

            if (_crestGraphics != null)
            {
                foreach (var crest in _crestGraphics)
                {
                    if (crest == null) continue;

                    crest.transform.DOKill();
                    crest.DOKill();
                }
            }

            foreach (var burst in _burstImages)
            {
                if (burst == null) continue;

                burst.transform.DOKill();
                burst.DOKill();
            }

            if (_rankFlashImage != null)
            {
                _rankFlashImage.DOKill();
            }
        }

        private void KillOverlayList(List<Image> overlays)
        {
            if (overlays == null) return;

            foreach (var overlay in overlays)
            {
                if (overlay != null)
                {
                    overlay.DOKill();
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
            _flashDuration = Mathf.Max(0.01f, _flashDuration);
            _burstDuration = Mathf.Max(0.01f, _burstDuration);
            _burstInitialScale = Mathf.Max(0.01f, _burstInitialScale);
        }
#endif

        #endregion
    }
}
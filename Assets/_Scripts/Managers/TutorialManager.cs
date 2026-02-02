using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using TMPEffects.Components;
using DG.Tweening;
using My.Scripts.Core.Data;
using My.Scripts.Core.Utility;
using My.Scripts.EventBus;
using My.Scripts.Input;
using My.Scripts.UI;
using My.Data.Tutorials;

namespace My.Scripts.Managers
{
    /// <summary>
    /// Управляет отображением обучающих сообщений.
    /// Синглтон, размещается на игровой сцене.
    /// </summary>
    public class TutorialManager : Singleton<TutorialManager>
    {
        #region Serialized Fields

        [Header("Tutorial Data")]
        [Tooltip("Данные туториалов для разных уровней")]
        [SerializeField] private List<TutorialData> _tutorialDataList = new();

        [Header("UI Container")]
        [SerializeField] private TutorialUI _uiContainer;

        [Header("TMPWriter (Optional)")]
        [SerializeField] private TMPWriter _tmpWriter;

        [Header("Skip Settings")]
        [SerializeField] private float _skipHoldDuration = 3f;

        #endregion

        #region Private Fields

        private TutorialData _currentTutorialData;
        private int _currentBlockIndex;
        private bool _isTutorialActive;

        private InputAction _continueAction;
        private InputAction _skipAction;

        private float _skipHoldTimer;
        private bool _isHoldingSkip;

        private Image _backgroundImage;
        private RectTransform _backgroundRect;

        private Dictionary<GameObject, Vector2> _originalPositions = new();
        private Dictionary<GameObject, float> _originalAlphas = new();
        private List<Tween> _activeTweens = new();
        private List<GameObject> _activeImages = new();

        private static readonly Color32 BackgroundColor = new Color32(56, 56, 56, 210);

        private bool _isGamePaused;

        #endregion

        #region Properties

        public bool IsTutorialActive => _isTutorialActive;
        public int CurrentBlockIndex => _currentBlockIndex;
        public int TotalBlocks => _currentTutorialData?.Blocks?.Count ?? 0;

        #endregion

        #region Events

        public event Action OnTutorialStarted;
        public event Action OnTutorialCompleted;
        public event Action<int, int> OnBlockChanged;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            ValidateSetup();
            SetupInputActions();
            CreateBackgroundImage();
            SubscribeToPauseEvents();
            HideUI();
        }

        private void Update()
        {
            if (!_isTutorialActive) return;

            HandleSkipHold();
        }

        private void LateUpdate()
        {
            if (!_isTutorialActive) return;

            UpdateBackgroundSize();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupInputActions();
            StopAllAnimations();
            UnsubscribeFromPauseEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Пытается запустить туториал для указанного уровня.
        /// </summary>
        public void TryStartTutorialForLevel(int levelNumber)
        {
            if (_isTutorialActive) return;

            if (GameData.IsTutorialCompletedForLevel(levelNumber)) return;

            TutorialData data = GetTutorialDataForLevel(levelNumber);
            if (data == null || data.Blocks == null || data.Blocks.Count == 0) return;

            StartTutorial(data);
        }

        /// <summary>
        /// Проверяет, есть ли туториал для указанного уровня.
        /// </summary>
        public bool HasTutorialForLevel(int levelNumber)
        {
            return GetTutorialDataForLevel(levelNumber) != null;
        }

        /// <summary>
        /// Проверяет, пройден ли туториал для указанного уровня.
        /// </summary>
        public bool IsTutorialCompletedForLevel(int levelNumber)
        {
            return GameData.IsTutorialCompletedForLevel(levelNumber);
        }

        public void NextBlock()
        {
            if (!_isTutorialActive) return;

            HideCurrentBlockImages();

            _currentBlockIndex++;

            if (_currentBlockIndex >= _currentTutorialData.Blocks.Count)
            {
                CompleteTutorial();
            }
            else
            {
                DisplayCurrentBlock();
                OnBlockChanged?.Invoke(_currentBlockIndex, _currentTutorialData.Blocks.Count);
            }
        }

        public void SkipTutorial()
        {
            if (!_isTutorialActive) return;

            _isHoldingSkip = false;
            ResetSkipProgressUI();
            CompleteTutorial();
        }

        #endregion

        #region Initialization

        private void ValidateSetup()
        {
            if (_uiContainer == null)
            {
                Debug.LogError("[TutorialManager] UI Container is not assigned!");
            }

            if (_tutorialDataList == null || _tutorialDataList.Count == 0)
            {
                Debug.LogWarning("[TutorialManager] Tutorial Data List is empty");
            }
        }

        private void CreateBackgroundImage()
        {
            if (_uiContainer == null || _uiContainer.TutorialText == null) return;

            RectTransform textRect = _uiContainer.TutorialTextRect;
            Transform textParent = textRect.parent;

            GameObject backgroundObj = new GameObject("TutorialBackground");
            backgroundObj.transform.SetParent(textParent, false);

            int textIndex = textRect.GetSiblingIndex();
            backgroundObj.transform.SetSiblingIndex(textIndex);

            _backgroundImage = backgroundObj.AddComponent<Image>();
            _backgroundImage.sprite = _uiContainer.BackgroundSprite;
            _backgroundImage.type = Image.Type.Sliced;
            _backgroundImage.color = BackgroundColor;
            _backgroundImage.raycastTarget = false;

            _backgroundRect = backgroundObj.GetComponent<RectTransform>();
            _backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            _backgroundRect.anchorMin = textRect.anchorMin;
            _backgroundRect.anchorMax = textRect.anchorMax;

            backgroundObj.SetActive(false);
        }

        private TutorialData GetTutorialDataForLevel(int levelNumber)
        {
            foreach (var data in _tutorialDataList)
            {
                if (data != null && data.LevelNumber == levelNumber)
                {
                    return data;
                }
            }
            return null;
        }

        #endregion

        #region Input Setup

        private void SetupInputActions()
        {
            _continueAction = new InputAction("TutorialContinue", InputActionType.Button);
            _continueAction.AddBinding("<Keyboard>/enter");
            _continueAction.AddBinding("<Keyboard>/numpadEnter");
            _continueAction.AddBinding("<Keyboard>/space");
            _continueAction.performed += OnContinuePerformed;

            _skipAction = new InputAction("TutorialSkip", InputActionType.Button);
            _skipAction.AddBinding("<Keyboard>/backspace");
            _skipAction.started += OnSkipStarted;
            _skipAction.canceled += OnSkipCanceled;
        }

        private void EnableInputActions()
        {
            _continueAction?.Enable();
            _skipAction?.Enable();
        }

        private void DisableInputActions()
        {
            _continueAction?.Disable();
            _skipAction?.Disable();
        }

        private void CleanupInputActions()
        {
            if (_continueAction != null)
            {
                _continueAction.performed -= OnContinuePerformed;
                _continueAction.Disable();
                _continueAction.Dispose();
                _continueAction = null;
            }

            if (_skipAction != null)
            {
                _skipAction.started -= OnSkipStarted;
                _skipAction.canceled -= OnSkipCanceled;
                _skipAction.Disable();
                _skipAction.Dispose();
                _skipAction = null;
            }
        }

        #endregion

        #region Input Handlers

        private void OnContinuePerformed(InputAction.CallbackContext context)
        {
            if (!_isTutorialActive) return;
            if (_isHoldingSkip) return;
            if (_isGamePaused) return;

            NextBlock();
        }

        private void OnSkipStarted(InputAction.CallbackContext context)
        {
            if (!_isTutorialActive) return;
            if (_isGamePaused) return;

            _isHoldingSkip = true;
            _skipHoldTimer = 0f;
            UpdateSkipProgressUI();
        }

        private void OnSkipCanceled(InputAction.CallbackContext context)
        {
            _isHoldingSkip = false;
            _skipHoldTimer = 0f;
            ResetSkipProgressUI();
        }

        private void HandleSkipHold()
        {
            if (!_isHoldingSkip) return;

            _skipHoldTimer += Time.deltaTime;
            UpdateSkipProgressUI();

            if (_skipHoldTimer >= _skipHoldDuration)
            {
                SkipTutorial();
            }
        }

        #endregion

        #region Skip Progress UI

        private void InitializeSkipUI()
        {
            if (_uiContainer?.SkipProgressCircle != null)
            {
                _uiContainer.SkipProgressCircle.type = Image.Type.Filled;
                _uiContainer.SkipProgressCircle.fillMethod = Image.FillMethod.Radial360;
                _uiContainer.SkipProgressCircle.fillOrigin = (int)Image.Origin360.Top;
                _uiContainer.SkipProgressCircle.fillClockwise = true;
                _uiContainer.SkipProgressCircle.fillAmount = 0f;
            }
        }

        private void UpdateSkipProgressUI()
        {
            if (_uiContainer?.SkipProgressCircle != null)
            {
                float progress = Mathf.Clamp01(_skipHoldTimer / _skipHoldDuration);
                _uiContainer.SkipProgressCircle.fillAmount = progress;
            }
        }

        private void ResetSkipProgressUI()
        {
            if (_uiContainer?.SkipProgressCircle != null)
            {
                _uiContainer.SkipProgressCircle.fillAmount = 0f;
            }
        }

        #endregion

        #region Tutorial Flow

        private void StartTutorial(TutorialData data)
        {
            if (_isTutorialActive) return;
            if (data == null || data.Blocks == null || data.Blocks.Count == 0) return;

            _currentTutorialData = data;
            _isTutorialActive = true;
            _currentBlockIndex = 0;

            CacheOriginalStates();
            DisableGameInput();
            ShowUI();
            InitializeSkipUI();
            DisplayCurrentBlock();

            BroadcastTutorialStarted();
            OnTutorialStarted?.Invoke();
        }

        private void CompleteTutorial()
        {
            int levelNumber = _currentTutorialData.LevelNumber;

            GameData.MarkTutorialCompletedForLevel(levelNumber);

            _isTutorialActive = false;

            EnableGameInput();
            HideUI();

            BroadcastTutorialCompleted();
            OnTutorialCompleted?.Invoke();

            _currentTutorialData = null;
        }

        #endregion

        #region Event Broadcasting

        private void BroadcastTutorialStarted()
        {
            if (EventManager.HasInstance)
            {
                EventManager.Instance.Broadcast(GameEvents.TutorialStarted);
            }
        }

        private void BroadcastTutorialCompleted()
        {
            if (EventManager.HasInstance)
            {
                EventManager.Instance.Broadcast(GameEvents.TutorialCompleted);
            }
        }

        #endregion

        #region Pause Events

        private void SubscribeToPauseEvents()
        {
            if (EventManager.HasInstance)
            {
                EventManager.Instance.AddHandler(GameEvents.GamePaused, OnGamePaused);
                EventManager.Instance.AddHandler(GameEvents.GameUnpaused, OnGameUnpaused);
            }
        }

        private void UnsubscribeFromPauseEvents()
        {
            if (EventManager.HasInstance)
            {
                EventManager.Instance.RemoveHandler(GameEvents.GamePaused, OnGamePaused);
                EventManager.Instance.RemoveHandler(GameEvents.GameUnpaused, OnGameUnpaused);
            }
        }

        private void OnGamePaused()
        {
            _isGamePaused = true;

            // Сбрасываем skip если был в процессе
            if (_isHoldingSkip)
            {
                _isHoldingSkip = false;
                _skipHoldTimer = 0f;
                ResetSkipProgressUI();
            }
        }

        private void OnGameUnpaused()
        {
            _isGamePaused = false;
        }

        #endregion

        #region Display

        private void CacheOriginalStates()
        {
            _originalPositions.Clear();
            _originalAlphas.Clear();

            if (_currentTutorialData == null || _uiContainer == null) return;

            foreach (var block in _currentTutorialData.Blocks)
            {
                if (block.Images == null) continue;

                foreach (var imageData in block.Images)
                {
                    GameObject obj = _uiContainer.GetImageByName(imageData.ImageObjectName);
                    if (obj == null) continue;

                    RectTransform rect = obj.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        _originalPositions[obj] = rect.anchoredPosition;
                    }

                    CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        _originalAlphas[obj] = canvasGroup.alpha;
                    }
                    else
                    {
                        Image img = obj.GetComponent<Image>();
                        if (img != null)
                        {
                            _originalAlphas[obj] = img.color.a;
                        }
                    }
                }
            }
        }

        private void DisplayCurrentBlock()
        {
            if (_currentTutorialData == null) return;
            if (_currentBlockIndex < 0 || _currentBlockIndex >= _currentTutorialData.Blocks.Count) return;

            var block = _currentTutorialData.Blocks[_currentBlockIndex];

            ApplyBlockPosition(block);
            ShowBlockImages(block);

            if (_uiContainer?.TutorialText != null)
            {
                _uiContainer.TutorialText.text = block.Text;

                if (_tmpWriter != null)
                {
                    _tmpWriter.ResetWriter();
                    _tmpWriter.StartWriter();
                }
            }

            Canvas.ForceUpdateCanvases();
            UpdateBackgroundSize();
        }

        private void ApplyBlockPosition(TutorialData.TutorialBlock block)
        {
            if (_uiContainer == null) return;

            RectTransform textRect = _uiContainer.TutorialTextRect;
            if (textRect == null) return;

            Vector2 anchor = new Vector2(block.NormalizedX, block.NormalizedY);

            textRect.anchorMin = anchor;
            textRect.anchorMax = anchor;
            textRect.pivot = block.Pivot;
            textRect.anchoredPosition = block.PixelOffset;
        }

        private void ShowBlockImages(TutorialData.TutorialBlock block)
        {
            if (block.Images == null || _uiContainer == null) return;

            StopAllAnimations();
            _activeImages.Clear();

            foreach (var imageData in block.Images)
            {
                GameObject obj = _uiContainer.GetImageByName(imageData.ImageObjectName);
                if (obj == null) continue;

                ResetImageState(obj);
                obj.SetActive(true);
                _activeImages.Add(obj);

                if (imageData.AddFadeInAnimation)
                {
                    StartFadeInAnimation(obj, imageData);
                }

                if (imageData.AddArrowAnimation)
                {
                    float arrowDelay = imageData.AddFadeInAnimation
                        ? imageData.FadeInDelay + imageData.FadeInDuration
                        : 0f;

                    StartArrowAnimation(obj, imageData, arrowDelay);
                }
            }
        }

        private void HideCurrentBlockImages()
        {
            StopAllAnimations();

            foreach (var obj in _activeImages)
            {
                if (obj != null)
                {
                    ResetImageState(obj);
                    obj.SetActive(false);
                }
            }

            _activeImages.Clear();
        }

        private void ResetImageState(GameObject obj)
        {
            if (_originalPositions.TryGetValue(obj, out Vector2 originalPos))
            {
                RectTransform rect = obj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = originalPos;
                }
            }

            if (_originalAlphas.TryGetValue(obj, out float originalAlpha))
            {
                SetImageAlpha(obj, originalAlpha);
            }
        }

        private void UpdateBackgroundSize()
        {
            if (_backgroundRect == null) return;
            if (_uiContainer?.TutorialTextRect == null || _uiContainer.TutorialText == null) return;

            RectTransform textRect = _uiContainer.TutorialTextRect;
            TMP_Text text = _uiContainer.TutorialText;

            text.ForceMeshUpdate();

            Bounds textBounds = text.textBounds;
            Vector2 textSize = textBounds.size;

            if (textSize.x < 1f || textSize.y < 1f)
            {
                textSize = text.GetPreferredValues();

                if (text.textWrappingMode != TextWrappingModes.NoWrap && textRect.rect.width > 0)
                {
                    textSize.x = Mathf.Min(textSize.x, textRect.rect.width);
                }
            }

            Vector2 backgroundSize = textSize + _uiContainer.BackgroundPadding * 2f;
            _backgroundRect.sizeDelta = backgroundSize;

            _backgroundRect.anchorMin = textRect.anchorMin;
            _backgroundRect.anchorMax = textRect.anchorMax;

            Vector2 boundsCenter = new Vector2(textBounds.center.x, textBounds.center.y);
            _backgroundRect.anchoredPosition = textRect.anchoredPosition + boundsCenter;
        }

        private void ShowUI()
        {
            _uiContainer?.SetActive(true);

            if (_uiContainer?.TutorialText != null)
            {
                _uiContainer.TutorialText.gameObject.SetActive(true);
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.gameObject.SetActive(true);
            }

            if (_uiContainer?.SkipHintContainer != null)
            {
                _uiContainer.SkipHintContainer.SetActive(true);
            }

            ResetSkipProgressUI();
        }

        private void HideUI()
        {
            StopAllAnimations();

            if (_uiContainer != null)
            {
                _uiContainer.HideAllImages();
            }

            if (_uiContainer?.TutorialText != null)
            {
                _uiContainer.TutorialText.gameObject.SetActive(false);
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.gameObject.SetActive(false);
            }

            if (_uiContainer?.SkipHintContainer != null)
            {
                _uiContainer.SkipHintContainer.SetActive(false);
            }

            _activeImages.Clear();
        }

        #endregion

        #region Fade In Animation

        private void StartFadeInAnimation(GameObject obj, TutorialData.TutorialImage imageData)
        {
            SetImageAlpha(obj, 0f);

            float targetAlpha = _originalAlphas.TryGetValue(obj, out float original) ? original : 1f;

            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Tween tween = canvasGroup
                    .DOFade(targetAlpha, imageData.FadeInDuration)
                    .SetDelay(imageData.FadeInDelay)
                    .SetEase(Ease.OutQuad)
                    .SetLink(obj);

                _activeTweens.Add(tween);
                return;
            }

            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                Tween tween = img
                    .DOFade(targetAlpha, imageData.FadeInDuration)
                    .SetDelay(imageData.FadeInDelay)
                    .SetEase(Ease.OutQuad)
                    .SetLink(obj);

                _activeTweens.Add(tween);
            }
        }

        private void SetImageAlpha(GameObject obj, float alpha)
        {
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                return;
            }

            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                Color color = img.color;
                color.a = alpha;
                img.color = color;
            }
        }

        #endregion

        #region Arrow Animation

        private void StartArrowAnimation(GameObject obj, TutorialData.TutorialImage imageData, float delay = 0f)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;

            Vector2 originalPos = rect.anchoredPosition;
            Vector2 moveOffset = GetDirectionVector(imageData.Direction) * imageData.MoveDistance;
            float halfDuration = imageData.AnimationDuration / 2f;

            Sequence sequence = DOTween.Sequence();

            if (delay > 0f)
            {
                sequence.AppendInterval(delay);
            }

            sequence.Append(
                rect.DOAnchorPos(originalPos - moveOffset, halfDuration)
                    .SetEase(Ease.OutQuad)
            );

            sequence.Append(
                rect.DOAnchorPos(originalPos, halfDuration)
                    .SetEase(Ease.InQuad)
            );

            sequence.SetLoops(-1, LoopType.Restart);
            sequence.SetLink(obj);

            _activeTweens.Add(sequence);
        }

        private Vector2 GetDirectionVector(TutorialData.ArrowDirection direction)
        {
            return direction switch
            {
                TutorialData.ArrowDirection.Up => Vector2.up,
                TutorialData.ArrowDirection.Down => Vector2.down,
                TutorialData.ArrowDirection.Left => Vector2.left,
                TutorialData.ArrowDirection.Right => Vector2.right,
                _ => Vector2.right
            };
        }

        #endregion

        #region Animation Control

        private void StopAllAnimations()
        {
            foreach (var tween in _activeTweens)
            {
                if (tween != null && tween.IsActive())
                {
                    tween.Kill();
                }
            }
            _activeTweens.Clear();
        }

        #endregion

        #region Game Input Control

        private void DisableGameInput()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.DisableGameplayInput();
            }
        }

        private void EnableGameInput()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.EnableGameplayInput();
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [ContextMenu("Reset All Tutorial Progress")]
        private void ResetAllTutorialProgress()
        {
            GameData.ResetAllTutorials();
        }

        [ContextMenu("Reset Tutorial For Level 1")]
        private void ResetTutorialLevel1()
        {
            GameData.ResetTutorialForLevel(1);
        }

        [ContextMenu("Test Tutorial Level 1")]
        private void TestTutorialLevel1()
        {
            GameData.ResetTutorialForLevel(1);
            TryStartTutorialForLevel(1);
        }

        [ContextMenu("Show Completed Tutorials")]
        private void ShowCompletedTutorials()
        {
            int[] completed = GameData.GetCompletedTutorials();
            Debug.Log(completed.Length == 0
                ? "[TutorialManager] No tutorials completed yet"
                : $"[TutorialManager] Completed tutorials: {string.Join(", ", completed)}");
        }
#endif

        #endregion
    }
}
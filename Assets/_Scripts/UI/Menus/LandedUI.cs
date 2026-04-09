using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using My.Scripts.Managers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.UI.Menus
{
    public class LandedUI : MonoBehaviour
    {
        #region Constants

        private const string SUCCESS_TITLE = "<wave amplitude=5>SUCCESSFUL LANDING!</wave>";

        private const string CRASH_TITLE = "<color=#ff0000><shake minx=2 miny=2 maxx=2 maxy=2>CRASH!</shake></color>";

        private const string TOO_FAST_TITLE = "<color=#ff0000><shake minx=2 miny=2 maxx=2 maxy=2>LANDED TOO FAST!</shake></color>";

        private const string TOO_STEEP_TITLE = "<color=#ff0000><shake minx=2 miny=2 maxx=2 maxy=2>TOO STEEP ANGLE!</shake></color>";

        private const string CONTINUE_BUTTON_TEXT = "CONTINUE";
        private const string RESTART_BUTTON_TEXT = "RESTART";

        #endregion

        #region Serialized Fields

        [Header("Banners")]
        [SerializeField] private GameObject _successBanner;
        [SerializeField] private GameObject _crashBanner;

        [Header("Title Texts")]
        [SerializeField] private TextMeshProUGUI _successTitleText;
        [SerializeField] private TextMeshProUGUI _crashTitleText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private TextMeshProUGUI _nextButtonText;

        [Header("Crests")]
        [SerializeField] private List<CrestUI> _crests;

        [Header("Button")]
        [SerializeField] private Button _nextButton;

        [Header("Layout")]
        [Tooltip("Ёлементы, которые остаютс€ видимыми при краше (кроме CrashBanner и NextButton, которые управл€ютс€ отдельно)")]
        [SerializeField] private List<GameObject> _successOnlyElements;

        [Header("Canvas Group")]
        [SerializeField] private CanvasGroup _canvasGroup;

        #endregion

        #region Private Fields

        private Action _nextButtonAction;
        private LandedUIAnimation _animation;
        private bool _isSubscribed;
        private int _currentEarnedCrests;
        private bool _isSuccess;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animation = GetComponent<LandedUIAnimation>();
            SetupButton();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            SubscribeToUIEvents();
            Hide();
        }

        private void OnDestroy()
        {
            UnsubscribeFromUIEvents();
            CleanupButton();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void SetupButton()
        {
            if (_nextButton != null)
            {
                BindButton(_nextButton, OnNextButtonClicked);
            }
        }

        private void CleanupButton()
        {
            if (_nextButton != null)
            {
                UnbindButton(_nextButton, OnNextButtonClicked);
            }
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToUIEvents()
        {
            if (_isSubscribed) return;

            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler<LevelCompletedData>(
                GameEvents.LevelCompleted,
                OnLevelCompleted
            );

            em.AddHandler(GameEvents.GamePaused, OnGamePaused);
            em.AddHandler(GameEvents.GameUnpaused, OnGameUnpaused);

            _isSubscribed = true;
        }

        private void UnsubscribeFromUIEvents()
        {
            if (!_isSubscribed) return;
            if (!EventManager.HasInstance) return;

            EventManager.Instance.RemoveHandler<LevelCompletedData>(
                GameEvents.LevelCompleted,
                OnLevelCompleted
            );

            EventManager.Instance.RemoveHandler(GameEvents.GamePaused, OnGamePaused);
            EventManager.Instance.RemoveHandler(GameEvents.GameUnpaused, OnGameUnpaused);

            _isSubscribed = false;
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnLevelCompleted(LevelCompletedData data)
        {
            if (data.IsSuccess)
            {
                SetupSuccessState(data);
            }
            else
            {
                SetupCrashState(data);
            }

            UpdateStats(data);
            UpdateLayout();
            Show();
        }

        private void OnNextButtonClicked()
        {
            _nextButtonAction?.Invoke();
        }

        private void OnGamePaused()
        {
            if (!gameObject.activeSelf) return;
            SetInteractable(false);
        }
        private void OnGameUnpaused()
        {
            if (!gameObject.activeSelf) return;
            SetInteractable(true);
            _nextButton?.Select();
        }

        #endregion

        #region Private Methods Ч UI State

        private void SetupSuccessState(LevelCompletedData data)
        {
            if (_successTitleText != null)
            {
                _successTitleText.text = SUCCESS_TITLE;
            }

            _nextButtonText.text = CONTINUE_BUTTON_TEXT;
            _nextButtonAction = () => GameManager.Instance?.GoToNextLevel();

            Debug.Log($"[LandedUI] Total score: {data.TotalScore}, Crests: {data.CrestsEarned}");

            _currentEarnedCrests = data.CrestsEarned;
            _isSuccess = true;
            ShowCrests(_currentEarnedCrests);
        }

        private void SetupCrashState(LevelCompletedData data)
        {
            if (_crashTitleText != null)
            {
                _crashTitleText.text = GetCrashTitle(data.LandingType);
            }

            _nextButtonText.text = RESTART_BUTTON_TEXT;
            _nextButtonAction = () => GameManager.Instance?.RetryLevel();

            _currentEarnedCrests = 0;
            _isSuccess = false;
            HideAllCrests();
        }

        private string GetCrashTitle(Lander.LandingType landingType)
        {
            return landingType switch
            {
                Lander.LandingType.TooFastLanding => TOO_FAST_TITLE,
                Lander.LandingType.TooSteepAngle => TOO_STEEP_TITLE,
                _ => CRASH_TITLE
            };
        }

        private void UpdateStats(LevelCompletedData data)
        {
            float speed = Mathf.Round(data.LandingSpeed * 2f);
            float angle = Mathf.Round(data.DotVector * 100f);
            float multiplier = data.ScoreMultiplier;
            int scoreForLanding = data.LandingScore;

            _statsText.text = $"{speed}\n{angle}\nx{multiplier}\n{scoreForLanding}";
        }

        private void UpdateLayout()
        {
            if (_successBanner != null)
            {
                _successBanner.SetActive(_isSuccess);
            }

            if (_crashBanner != null)
            {
                _crashBanner.SetActive(!_isSuccess);
            }

            if (_successOnlyElements != null)
            {
                foreach (var element in _successOnlyElements)
                {
                    if (element != null)
                    {
                        element.SetActive(_isSuccess);
                    }
                }
            }
        }

        #endregion

        #region Private Methods Ч Crests

        private void ShowCrests(int earnedCount)
        {
            for (int i = 0; i < _crests.Count; i++)
            {
                bool isEarned = i < earnedCount;
                _crests[i].SetState(earned: isEarned, visible: true);
            }
        }

        private void HideAllCrests()
        {
            foreach (var crest in _crests)
            {
                crest.SetState(earned: false, visible: false);
            }
        }

        #endregion

        #region Private Methods Ч Level Data

        private int[] GetCrestThresholds()
        {
            if (!GameManager.HasInstance) return null;

            var currentLevel = GameManager.Instance.GetCurrentLevelObject();
            if (currentLevel == null) return null;

            return currentLevel.GetCrestThresholds();
        }

        #endregion

        #region Private Methods Ч Button Binding

        // >>> ƒќЅј¬Ћ≈Ќќ: хелперы дл€ прив€зки через ButtonVisuals <<<
        private void BindButton(Button button, System.Action action)
        {
            if (button == null) return;

            var visuals = button.GetComponent<ButtonVisuals>();
            if (visuals != null)
            {
                visuals.AddDelayedListener(action);
            }
        }

        private void UnbindButton(Button button, System.Action action)
        {
            if (button == null) return;

            var visuals = button.GetComponent<ButtonVisuals>();
            if (visuals != null)
            {
                visuals.RemoveDelayedListener(action);
            }
        }

        #endregion

        #region Private Methods Ч Visibility

        private void Show()
        {
            gameObject.SetActive(true);
            SetInteractable(true);

            int totalScore = GameManager.HasInstance ? GameManager.Instance.Score : 0;
            int[] crestThresholds = GetCrestThresholds();

            _animation?.PlayEnterAnimation(_currentEarnedCrests, totalScore, crestThresholds, _isSuccess);

            SoundManager.Instance?.SuppressHoverSound();
            _nextButton?.Select();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        #endregion
    }
}
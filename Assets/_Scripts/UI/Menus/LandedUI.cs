using My.Scripts.Core.Data;
using My.Scripts.EventBus;
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
                _nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        private void CleanupButton()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(OnNextButtonClicked);
            }
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToUIEvents()
        {
            if (_isSubscribed) return;

            EventManager.Instance?.AddHandler<LevelCompletedData>(
                GameEvents.LevelCompleted,
                OnLevelCompleted
            );

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
                _crashTitleText.text = CRASH_TITLE;
            }

            _nextButtonText.text = RESTART_BUTTON_TEXT;
            _nextButtonAction = () => GameManager.Instance?.RetryLevel();

            _currentEarnedCrests = 0;
            _isSuccess = false;
            HideAllCrests();
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
            // Ѕаннеры
            if (_successBanner != null)
            {
                _successBanner.SetActive(_isSuccess);
            }

            if (_crashBanner != null)
            {
                _crashBanner.SetActive(!_isSuccess);
            }

            // Ёлементы, видимые только при успехе
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

        #region Private Methods Ч Visibility

        private void Show()
        {
            gameObject.SetActive(true);

            int totalScore = GameManager.HasInstance ? GameManager.Instance.Score : 0;
            int[] crestThresholds = GetCrestThresholds();

            _animation?.PlayEnterAnimation(_currentEarnedCrests, totalScore, crestThresholds, _isSuccess);

            _nextButton?.Select();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}
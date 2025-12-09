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
    public class LandedMenuUI : MonoBehaviour
    {
        #region Constants

        private const string SUCCESS_TITLE = "<wave amplitude=5>SUCCESSFUL LANDING!</wave>";
        private const string CRASH_TITLE = "<color=#ff0000><shake>CRASH!</shake></color>";
        private const string CONTINUE_BUTTON_TEXT = "CONTINUE";
        private const string RESTART_BUTTON_TEXT = "RESTART";

        #endregion

        #region Serialized Fields

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private TextMeshProUGUI _nextButtonText;

        [Header("Stars")]
        [SerializeField] private List<StarUI> _stars;

        [Header("Button")]
        [SerializeField] private Button _nextButton;

        #endregion

        #region Private Fields

        private Action _nextButtonAction;
        private LandedMenuUIAnimation _animation;
        private bool _isSubscribed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animation = GetComponent<LandedMenuUIAnimation>();
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

            // ѕодписываемс€ на LevelCompleted вместо LanderLanded
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
            _titleText.text = SUCCESS_TITLE;
            _nextButtonText.text = CONTINUE_BUTTON_TEXT;
            _nextButtonAction = () => GameManager.Instance?.GoToNextLevel();

            // ƒанные уже посчитаны в GameManager!
            Debug.Log($"[LandedUI] Total score: {data.TotalScore}, Stars: {data.StarsEarned}");

            ShowStars(data.StarsEarned);
        }

        private void SetupCrashState(LevelCompletedData data)
        {
            _titleText.text = CRASH_TITLE;
            _nextButtonText.text = RESTART_BUTTON_TEXT;
            _nextButtonAction = () => GameManager.Instance?.RetryLevel();

            HideAllStars();
        }

        private void UpdateStats(LevelCompletedData data)
        {
            float speed = Mathf.Round(data.LandingSpeed * 2f);
            float angle = Mathf.Round(data.DotVector * 100f);
            float multiplier = data.ScoreMultiplier;
            int score = data.LandingScore;

            _statsText.text = $"{speed}\n{angle}\nx{multiplier}\n{score}";
        }

        /// <summary>
        /// ¬ычисл€ет общий счЄт уровн€ (очки до посадки + очки за посадку).
        /// </summary>
        /// <param name="landingScore">ќчки за текущую посадку</param>
        private int GetTotalLevelScore(int landingScore)
        {
            if (!GameManager.HasInstance)
            {
                return landingScore;
            }

            // GameManager.Score содержит очки, собранные до посадки (монеты, бонусы и т.д.)
            // landingScore Ч очки за саму посадку
            int scoreBeforeLanding = GameManager.Instance.Score;
            int totalScore = scoreBeforeLanding + landingScore;

            Debug.Log($"[LandedUI] Score before landing: {scoreBeforeLanding}, Landing: {landingScore}, Total: {totalScore}");

            return totalScore;
        }

        /// <summary>
        /// ¬ычисл€ет количество заработанных звЄзд на основе общего счЄта уровн€.
        /// </summary>
        /// <param name="totalLevelScore">ќбщий счЄт за уровень</param>
        private int GetEarnedStarsCount(int totalLevelScore)
        {
            if (!GameManager.HasInstance)
            {
                Debug.LogWarning("[LandedUI] GameManager not found!");
                return 0;
            }

            var currentLevel = GameManager.Instance.GetCurrentLevelObject();
            if (currentLevel == null)
            {
                Debug.LogWarning("[LandedUI] Current level is null!");
                return 0;
            }

            int earnedStars = currentLevel.GetEarnedStarsCount(totalLevelScore);

            return earnedStars;
        }

        #endregion

        #region Private Methods Ч Stars

        private void ShowStars(int earnedCount)
        {
            for (int i = 0; i < _stars.Count; i++)
            {
                bool isEarned = i < earnedCount;
                _stars[i].SetState(earned: isEarned, visible: true);
            }
        }

        private void HideAllStars()
        {
            foreach (var star in _stars)
            {
                star.SetState(earned: false, visible: false);
            }
        }

        #endregion

        #region Private Methods Ч Visibility

        private void Show()
        {
            gameObject.SetActive(true);
            _animation?.PlayEnterAnimation();
            _nextButton?.Select();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}
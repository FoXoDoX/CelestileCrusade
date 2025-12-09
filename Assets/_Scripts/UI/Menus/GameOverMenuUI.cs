using My.Scripts.Core.Data;
using My.Scripts.Core.Persistence;
using My.Scripts.Core.Scene;
using My.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.UI.Menus
{
    public class GameOverMenuUI : MonoBehaviour
    {
        #region Constants

        private const string FINAL_SCORE_FORMAT = "FINAL SCORE: {0}";

        #endregion

        #region Serialized Fields

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Header("Buttons")]
        [SerializeField] private Button _mainMenuButton;

        [Header("Optional")]
        [SerializeField] private TextMeshProUGUI _completionText;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupButtons();
            ResetTimeScale();
        }

        private void Start()
        {
            DisplayResults();
            SelectDefaultButton();
        }

        private void OnDestroy()
        {
            CleanupButtons();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }

        private void SetupButtons()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void CleanupButtons()
        {
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        private void SelectDefaultButton()
        {
            _mainMenuButton?.Select();
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnMainMenuClicked()
        {
            // —охран€ем прогресс перед выходом
            SaveSystem.Save();

            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        }

        #endregion

        #region Private Methods Ч Display

        private void DisplayResults()
        {
            DisplayFinalScore();
            DisplayCompletionStats();
        }

        private void DisplayFinalScore()
        {
            if (_scoreText == null) return;

            int finalScore = GameManager.HasInstance
                ? GameManager.Instance.GetTotalScore()
                : GameData.TotalScore;

            _scoreText.text = string.Format(FINAL_SCORE_FORMAT, finalScore);
        }

        private void DisplayCompletionStats()
        {
            if (_completionText == null) return;

            int highestLevel = GameData.HighestCompletedLevel;
            int totalStars = CalculateTotalStars();

            _completionText.text = $"LEVELS COMPLETED: {highestLevel}\nTOTAL STARS: {totalStars}";
        }

        private int CalculateTotalStars()
        {
            int totalStars = 0;
            int highestLevel = GameData.HighestCompletedLevel;

            for (int level = 1; level <= highestLevel; level++)
            {
                totalStars += GameData.GetStarsForLevel(level);
            }

            return totalStars;
        }

        #endregion
    }
}
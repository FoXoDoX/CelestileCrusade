using My.Scripts.Core.Data;
using My.Scripts.Core.Scene;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.UI.Menus
{
    public class LevelsMenuUI : MonoBehaviour
    {
        #region Constants

        private const int DEFAULT_STARS_PER_LEVEL = 3;

        #endregion

        #region Serialized Fields

        [Header("Level Buttons")]
        [Tooltip("Список кнопок уровней. Звёзды находятся автоматически.")]
        [SerializeField] private List<Button> _levelButtons;

        [Header("Settings")]
        [Tooltip("Количество звёзд на каждом уровне")]
        [SerializeField] private int _starsPerLevel = DEFAULT_STARS_PER_LEVEL;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        #endregion

        #region Private Fields

        private List<LevelButtonData> _levelButtonsData = new();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResetTimeScale();
            InitializeLevelButtons();
            InitializeBackButton();
        }

        private void Start()
        {
            SelectDefaultButton();
        }

        private void OnDestroy()
        {
            CleanupButtons();
        }

        #endregion

        #region Private Methods — Initialization

        private void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }

        private void InitializeLevelButtons()
        {
            Debug.Log($"[LevelsMenuUI] Initializing {_levelButtons.Count} buttons");
            Debug.Log($"[LevelsMenuUI] HighestCompletedLevel: {GameData.HighestCompletedLevel}");

            _levelButtonsData.Clear();

            for (int i = 0; i < _levelButtons.Count; i++)
            {
                int levelNumber = i + 1;
                var button = _levelButtons[i];

                if (button == null)
                {
                    Debug.LogWarning($"[LevelsMenuUI] Button at index {i} is null!");
                    continue;
                }

                // Создаём данные для кнопки и автоматически находим звёзды
                var buttonData = new LevelButtonData(button, levelNumber, _starsPerLevel);
                _levelButtonsData.Add(buttonData);

                bool isAvailable = GameData.IsLevelAvailable(levelNumber);
                int starsEarned = isAvailable ? GameData.GetStarsForLevel(levelNumber) : 0;

                Debug.Log($"[LevelsMenuUI] Level {levelNumber}: available={isAvailable}, stars={starsEarned}");

                buttonData.Initialize(isAvailable, starsEarned);

                if (isAvailable)
                {
                    buttonData.OnLevelSelected += HandleLevelSelected;
                }
            }
        }

        private void InitializeBackButton()
        {
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        private void CleanupButtons()
        {
            foreach (var buttonData in _levelButtonsData)
            {
                buttonData.OnLevelSelected -= HandleLevelSelected;
                buttonData.Cleanup();
            }

            _levelButtonsData.Clear();

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackButtonClicked);
            }
        }

        private void SelectDefaultButton()
        {
            _backButton?.Select();
        }

        #endregion

        #region Private Methods — Event Handlers

        private void HandleLevelSelected(int levelNumber)
        {
            GameData.CurrentLevel = levelNumber;
            GameData.TotalScore = 0;
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        }

        private void OnBackButtonClicked()
        {
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _starsPerLevel = Mathf.Max(1, _starsPerLevel);
        }
#endif

        #endregion
    }

    /// <summary>
    /// Внутренние данные кнопки уровня с автопоиском звёзд.
    /// Структура иерархии кнопки:
    /// Button
    ///   ├── Text (или любой другой объект)
    ///   ├── Star1
    ///   │     ├── UnearnedImage (index 0)
    ///   │     └── EarnedImage (index 1)
    ///   ├── Star2
    ///   │     ├── UnearnedImage
    ///   │     └── EarnedImage
    ///   └── Star3
    ///         ├── UnearnedImage
    ///         └── EarnedImage
    /// </summary>
    public class LevelButtonData
    {
        #region Events

        public event Action<int> OnLevelSelected;

        #endregion

        #region Private Fields

        private readonly Button _button;
        private readonly int _levelNumber;
        private readonly List<StarImages> _stars = new();

        #endregion

        #region Constructor

        public LevelButtonData(Button button, int levelNumber, int expectedStarsCount)
        {
            _button = button;
            _levelNumber = levelNumber;

            FindStars(expectedStarsCount);
        }

        #endregion

        #region Public Methods

        public void Initialize(bool isAvailable, int starsEarned)
        {
            if (_button == null) return;

            _button.interactable = isAvailable;

            if (isAvailable)
            {
                _button.onClick.AddListener(OnButtonClicked);
                DisplayStars(starsEarned);
            }
            else
            {
                HideAllStars();
            }
        }

        public void Cleanup()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        #endregion

        #region Private Methods — Star Discovery

        private void FindStars(int expectedCount)
        {
            _stars.Clear();

            Transform buttonTransform = _button.transform;
            int foundStars = 0;

            // Проходим по всем дочерним объектам кнопки
            for (int i = 0; i < buttonTransform.childCount && foundStars < expectedCount; i++)
            {
                Transform child = buttonTransform.GetChild(i);

                // Проверяем, является ли это звездой (имеет минимум 2 дочерних Image)
                if (TryParseAsStar(child, out StarImages starImages))
                {
                    _stars.Add(starImages);
                    foundStars++;

                    Debug.Log($"[LevelButtonData] Level {_levelNumber}: Found star {foundStars} in '{child.name}'");
                }
            }

            if (foundStars < expectedCount)
            {
                Debug.LogWarning($"[LevelButtonData] Level {_levelNumber}: Expected {expectedCount} stars, found {foundStars}");
            }
        }

        private bool TryParseAsStar(Transform starTransform, out StarImages starImages)
        {
            starImages = default;

            if (starTransform.childCount < 2)
                return false;

            // Первый дочерний объект — UnearnedImage (index 0)
            // Второй дочерний объект — EarnedImage (index 1)
            var unearnedImage = starTransform.GetChild(0).GetComponent<Image>();
            var earnedImage = starTransform.GetChild(1).GetComponent<Image>();

            if (unearnedImage == null || earnedImage == null)
                return false;

            starImages = new StarImages(unearnedImage, earnedImage);
            return true;
        }

        #endregion

        #region Private Methods — Display

        private void DisplayStars(int starsEarned)
        {
            Debug.Log($"[LevelButtonData] Level {_levelNumber}: Displaying {starsEarned} earned stars out of {_stars.Count}");

            for (int i = 0; i < _stars.Count; i++)
            {
                bool isEarned = i < starsEarned;
                _stars[i].SetState(isEarned, visible: true);

                Debug.Log($"[LevelButtonData] Level {_levelNumber}, Star {i + 1}: earned={isEarned}");
            }
        }

        private void HideAllStars()
        {
            foreach (var star in _stars)
            {
                star.SetState(earned: false, visible: false);
            }
        }

        private void OnButtonClicked()
        {
            OnLevelSelected?.Invoke(_levelNumber);
        }

        #endregion
    }

    /// <summary>
    /// Структура для хранения ссылок на Image звезды.
    /// </summary>
    public readonly struct StarImages
    {
        private readonly Image _unearnedImage;
        private readonly Image _earnedImage;

        public StarImages(Image unearnedImage, Image earnedImage)
        {
            _unearnedImage = unearnedImage;
            _earnedImage = earnedImage;
        }

        /// <summary>
        /// Устанавливает состояние звезды.
        /// </summary>
        /// <param name="earned">Звезда заработана</param>
        /// <param name="visible">Звезда видима</param>
        public void SetState(bool earned, bool visible)
        {
            // Фон (unearned) виден всегда, когда звезда отображается
            if (_unearnedImage != null)
            {
                _unearnedImage.enabled = visible;
            }

            // Заработанная звезда накладывается поверх
            if (_earnedImage != null)
            {
                _earnedImage.enabled = earned && visible;
            }
        }
    }
}
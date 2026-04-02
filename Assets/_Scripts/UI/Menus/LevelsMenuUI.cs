using My.Scripts.Core.Data;
using My.Scripts.Core.Scene;
using My.Scripts.Managers;
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

                var buttonData = new LevelButtonData(button, levelNumber, _starsPerLevel);
                _levelButtonsData.Add(buttonData);

                bool isAvailable = GameData.IsLevelAvailable(levelNumber);
                int starsEarned = isAvailable ? GameData.GetCrestsForLevel(levelNumber) : 0;

                Debug.Log($"[LevelsMenuUI] Level {levelNumber}: available={isAvailable}, stars={starsEarned}");

                buttonData.Initialize(isAvailable, starsEarned);

                if (isAvailable)
                {
                    BindButton(button, () => HandleLevelSelected(levelNumber));
                }
            }
        }

        private void InitializeBackButton()
        {
            BindButton(_backButton, OnBackButtonClicked);
        }

        private void CleanupButtons()
        {
            foreach (var buttonData in _levelButtonsData)
            {
                buttonData.Cleanup();
            }

            _levelButtonsData.Clear();

            UnbindButton(_backButton, OnBackButtonClicked);
        }

        private void SelectDefaultButton()
        {
            SoundManager.Instance?.SuppressHoverSound();
            _backButton?.Select();
        }

        #endregion

        #region Private Methods — Button Binding

        private void BindButton(Button button, Action action)
        {
            if (button == null) return;

            var visuals = button.GetComponent<ButtonVisuals>();
            if (visuals != null)
            {
                visuals.AddDelayedListener(action);
            }
            else
            {
                button.onClick.AddListener(() => action());
            }
        }

        private void UnbindButton(Button button, Action action)
        {
            if (button == null) return;

            var visuals = button.GetComponent<ButtonVisuals>();
            if (visuals != null)
            {
                visuals.RemoveDelayedListener(action);
            }
            else
            {
                button.onClick.RemoveAllListeners();
            }
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
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene, TransitionDirection.Left);
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

    public class LevelButtonData
    {
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

            if (!isAvailable)
            {
                _button.gameObject.SetActive(false);
                return;
            }

            DisplayStars(starsEarned);
        }

        public void Cleanup()
        {
            // Привязка через ButtonVisuals управляется LevelsMenuUI
        }

        #endregion

        #region Private Methods — Star Discovery

        private void FindStars(int expectedCount)
        {
            _stars.Clear();

            Transform buttonTransform = _button.transform;
            int foundStars = 0;

            for (int i = 0; i < buttonTransform.childCount && foundStars < expectedCount; i++)
            {
                Transform child = buttonTransform.GetChild(i);

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

        #endregion
    }

    public readonly struct StarImages
    {
        private readonly Image _unearnedImage;
        private readonly Image _earnedImage;

        public StarImages(Image unearnedImage, Image earnedImage)
        {
            _unearnedImage = unearnedImage;
            _earnedImage = earnedImage;
        }

        public void SetState(bool earned, bool visible)
        {
            if (_unearnedImage != null)
            {
                _unearnedImage.enabled = visible;
            }

            if (_earnedImage != null)
            {
                _earnedImage.enabled = earned && visible;
            }
        }
    }
}
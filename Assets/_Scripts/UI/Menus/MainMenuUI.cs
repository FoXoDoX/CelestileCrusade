using My.Scripts.Core.Data;
using My.Scripts.Core.Persistence;
using My.Scripts.Core.Scene;
using My.Scripts.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.UI.Menus
{
    public class MainMenuUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Navigation Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _levelsButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResetTimeScale();
            EnsureSaveFileExists();
            SetupButtons();
        }

        private void Start()
        {
            SelectDefaultButton();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
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

        private void EnsureSaveFileExists()
        {
            if (!SaveSystem.SaveFileExists)
            {
                SaveSystem.Save();
            }
        }

        private void SetupButtons()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }

            if (_levelsButton != null)
            {
                _levelsButton.onClick.AddListener(OnLevelsClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void CleanupButtons()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
            }

            if (_levelsButton != null)
            {
                _levelsButton.onClick.RemoveListener(OnLevelsClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }

        private void SelectDefaultButton()
        {
            _playButton?.Select();
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.SettingsBackButtonPressed, OnSettingsBack);
        }

        private void UnsubscribeFromEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler(GameEvents.SettingsBackButtonPressed, OnSettingsBack);
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnPlayClicked()
        {
            GameData.ResetSessionData();
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        }

        private void OnLevelsClicked()
        {
            GameData.ResetSessionData();
            SceneLoader.LoadScene(SceneLoader.Scene.LevelsMenuScene);
        }

        private void OnSettingsClicked()
        {
            EventManager.Instance?.Broadcast(GameEvents.SettingsButtonPressed);
        }

        private void OnQuitClicked()
        {
            QuitGame();
        }

        private void OnSettingsBack()
        {
            SelectDefaultButton();
        }

        #endregion

        #region Private Methods Ч Game Control

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE || UNITY_WEBGL
            Application.Quit();
#elif UNITY_ANDROID || UNITY_IOS
            // Ќа мобильных обычно не используют кнопку Quit
            Debug.Log($"[{nameof(MainMenuUI)}] Quit not supported on mobile platforms");
#endif
        }

        #endregion
    }
}
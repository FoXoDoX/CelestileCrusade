using My.Scripts.Core.Data;
using My.Scripts.Core.Persistence;
using My.Scripts.Core.Scene;
using My.Scripts.EventBus;
using My.Scripts.Managers;
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

        [Header("Sub-Menus")]
        [SerializeField] private SettingsMenuUI _settingsMenu;

        [Header("Canvas Group")]
        [SerializeField] private CanvasGroup _canvasGroup;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResetTimeScale();
            EnsureSaveFileExists();
            ConfigurePlatformSpecificUI();
            SetupButtons();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
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

        private void ConfigurePlatformSpecificUI()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_quitButton != null)
            {
                _quitButton.gameObject.SetActive(false);
            }
#endif
        }

        // >>> »«ћ≈Ќ≈Ќќ: используем BindButton вместо onClick.AddListener <<<
        private void SetupButtons()
        {
            BindButton(_playButton, OnPlayClicked);
            BindButton(_levelsButton, OnLevelsClicked);
            BindButton(_settingsButton, OnSettingsClicked);
            BindButton(_quitButton, OnQuitClicked);
        }

        // >>> »«ћ≈Ќ≈Ќќ: используем UnbindButton вместо onClick.RemoveListener <<<
        private void CleanupButtons()
        {
            UnbindButton(_playButton, OnPlayClicked);
            UnbindButton(_levelsButton, OnLevelsClicked);
            UnbindButton(_settingsButton, OnSettingsClicked);
            UnbindButton(_quitButton, OnQuitClicked);
        }

        private void SelectDefaultButton()
        {
            SoundManager.Instance?.SuppressHoverSound();
            _playButton?.Select();
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
            SetInteractable(false);
            _settingsMenu?.Show();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_WEBGL
            Application.Quit();
#endif
        }

        private void OnSettingsBack()
        {
            SetInteractable(true);
            _settingsButton?.Select();
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
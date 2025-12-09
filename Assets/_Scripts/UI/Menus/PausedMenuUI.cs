using My.Scripts.Core.Scene;
using My.Scripts.EventBus;
using My.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.UI.Menus
{
    public class PausedMenuUI : MonoBehaviour
    {
        #region Constants

        private const string SOUND_VOLUME_FORMAT = "SOUND {0}";
        private const string MUSIC_VOLUME_FORMAT = "MUSIC {0}";

        #endregion

        #region Serialized Fields

        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _soundVolumeButton;
        [SerializeField] private Button _musicVolumeButton;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _soundVolumeText;
        [SerializeField] private TextMeshProUGUI _musicVolumeText;

        #endregion

        #region Private Fields

        private bool _isSubscribed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupButtons();
        }

        private void Start()
        {
            SubscribeToUIEvents();  // ѕодписка один раз
            SubscribeToVolumeChanges();
            UpdateVolumeDisplays();
            Hide();
        }

        private void OnDestroy()
        {
            UnsubscribeFromUIEvents();  // ќтписка только при уничтожении
            CleanupButtons();
            UnsubscribeFromVolumeChanges();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void SetupButtons()
        {
            _resumeButton?.onClick.AddListener(OnResumeClicked);
            _mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
            _soundVolumeButton?.onClick.AddListener(OnSoundVolumeClicked);
            _musicVolumeButton?.onClick.AddListener(OnMusicVolumeClicked);
        }

        private void CleanupButtons()
        {
            _resumeButton?.onClick.RemoveListener(OnResumeClicked);
            _mainMenuButton?.onClick.RemoveListener(OnMainMenuClicked);
            _soundVolumeButton?.onClick.RemoveListener(OnSoundVolumeClicked);
            _musicVolumeButton?.onClick.RemoveListener(OnMusicVolumeClicked);
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToUIEvents()
        {
            if (_isSubscribed) return;

            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.GamePaused, OnGamePaused);
            em.AddHandler(GameEvents.GameUnpaused, OnGameUnpaused);

            _isSubscribed = true;
        }

        private void UnsubscribeFromUIEvents()
        {
            if (!_isSubscribed) return;
            if (!EventManager.HasInstance) return;

            var em = EventManager.Instance;
            em.RemoveHandler(GameEvents.GamePaused, OnGamePaused);
            em.RemoveHandler(GameEvents.GameUnpaused, OnGameUnpaused);

            _isSubscribed = false;
        }

        private void SubscribeToVolumeChanges()
        {
            if (SoundManager.HasInstance)
                SoundManager.Instance.OnSoundVolumeChanged += UpdateSoundVolumeDisplay;

            if (MusicManager.HasInstance)
                MusicManager.Instance.OnMusicVolumeChanged += UpdateMusicVolumeDisplay;
        }

        private void UnsubscribeFromVolumeChanges()
        {
            if (SoundManager.HasInstance)
                SoundManager.Instance.OnSoundVolumeChanged -= UpdateSoundVolumeDisplay;

            if (MusicManager.HasInstance)
                MusicManager.Instance.OnMusicVolumeChanged -= UpdateMusicVolumeDisplay;
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnGamePaused() => Show();
        private void OnGameUnpaused() => Hide();

        private void OnResumeClicked()
        {
            GameManager.Instance?.UnpauseGame();
        }

        private void OnMainMenuClicked()
        {
            GameManager.Instance?.UnpauseGame();
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        }

        private void OnSoundVolumeClicked()
        {
            SoundManager.Instance?.ChangeSoundVolume();
        }

        private void OnMusicVolumeClicked()
        {
            MusicManager.Instance?.ChangeMusicVolume();
        }

        #endregion

        #region Private Methods Ч UI Updates

        private void UpdateVolumeDisplays()
        {
            UpdateSoundVolumeDisplay();
            UpdateMusicVolumeDisplay();
        }

        private void UpdateSoundVolumeDisplay()
        {
            if (_soundVolumeText == null) return;
            int volume = SoundManager.HasInstance ? SoundManager.Instance.GetSoundVolume() : 0;
            _soundVolumeText.text = string.Format(SOUND_VOLUME_FORMAT, volume);
        }

        private void UpdateMusicVolumeDisplay()
        {
            if (_musicVolumeText == null) return;
            int volume = MusicManager.HasInstance ? MusicManager.Instance.GetMusicVolume() : 0;
            _musicVolumeText.text = string.Format(MUSIC_VOLUME_FORMAT, volume);
        }

        #endregion

        #region Private Methods Ч Visibility

        private void Show()
        {
            gameObject.SetActive(true);
            UpdateVolumeDisplays();
            _resumeButton?.Select();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}
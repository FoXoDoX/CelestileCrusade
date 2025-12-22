using My.Scripts.Core.Persistence;
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

        private const string SOUND_VOLUME_FORMAT = "{0}%";
        private const string MUSIC_VOLUME_FORMAT = "{0}%";

        #endregion

        #region Serialized Fields

        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Volume Sliders")]
        [SerializeField] private Slider _soundVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;

        [Header("Volume Value Text")]
        [SerializeField] private TextMeshProUGUI _soundVolumeText;
        [SerializeField] private TextMeshProUGUI _musicVolumeText;

        #endregion

        #region Private Fields

        private bool _isSubscribed;
        private bool _isUpdatingSliders;
        private bool _isInitialized;
        private bool _volumeChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupButtons();
            ConfigureSliderRanges();
        }

        private void Start()
        {
            SubscribeToUIEvents();
            SubscribeToVolumeChanges();
            SyncSlidersWithoutNotify();
            SubscribeToSliderEvents();

            _isInitialized = true;
            Hide();
        }

        private void OnEnable()
        {
            SubscribeToVolumeChanges();

            if (_isInitialized)
            {
                SyncSlidersWithoutNotify();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromVolumeChanges();

            // —охран€ем при закрытии меню, если громкость мен€лась
            SaveIfNeeded();
        }

        private void OnDestroy()
        {
            CleanupButtons();
            UnsubscribeFromSliderEvents();
            UnsubscribeFromVolumeChanges();
            UnsubscribeFromUIEvents();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void SetupButtons()
        {
            _resumeButton?.onClick.AddListener(OnResumeClicked);
            _mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        }

        private void ConfigureSliderRanges()
        {
            if (_soundVolumeSlider != null)
            {
                _soundVolumeSlider.minValue = 0f;
                _soundVolumeSlider.maxValue = 1f;
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.minValue = 0f;
                _musicVolumeSlider.maxValue = 1f;
            }
        }

        private void CleanupButtons()
        {
            _resumeButton?.onClick.RemoveListener(OnResumeClicked);
            _mainMenuButton?.onClick.RemoveListener(OnMainMenuClicked);
        }

        #endregion

        #region Private Methods Ч Slider Events

        private void SubscribeToSliderEvents()
        {
            _soundVolumeSlider?.onValueChanged.AddListener(OnSoundSliderChanged);
            _musicVolumeSlider?.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        private void UnsubscribeFromSliderEvents()
        {
            _soundVolumeSlider?.onValueChanged.RemoveListener(OnSoundSliderChanged);
            _musicVolumeSlider?.onValueChanged.RemoveListener(OnMusicSliderChanged);
        }

        private void OnSoundSliderChanged(float value)
        {
            if (!_isInitialized || _isUpdatingSliders) return;

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.SetSoundVolume(value);
                _volumeChanged = true;
            }

            UpdateSoundVolumeText(value);
        }

        private void OnMusicSliderChanged(float value)
        {
            if (!_isInitialized || _isUpdatingSliders) return;

            if (MusicManager.HasInstance)
            {
                MusicManager.Instance.SetMusicVolume(value);
                _volumeChanged = true;
            }

            UpdateMusicVolumeText(value);
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
            {
                SoundManager.Instance.OnSoundVolumeChanged += OnSoundVolumeChangedExternally;
            }

            if (MusicManager.HasInstance)
            {
                MusicManager.Instance.OnMusicVolumeChanged += OnMusicVolumeChangedExternally;
            }
        }

        private void UnsubscribeFromVolumeChanges()
        {
            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.OnSoundVolumeChanged -= OnSoundVolumeChangedExternally;
            }

            if (MusicManager.HasInstance)
            {
                MusicManager.Instance.OnMusicVolumeChanged -= OnMusicVolumeChangedExternally;
            }
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnGamePaused() => Show();
        private void OnGameUnpaused() => Hide();

        private void OnResumeClicked()
        {
            SaveIfNeeded();
            GameManager.Instance?.UnpauseGame();
        }

        private void OnMainMenuClicked()
        {
            SaveIfNeeded();
            GameManager.Instance?.UnpauseGame();
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        }

        private void OnSoundVolumeChangedExternally()
        {
            SyncSoundSliderWithoutNotify();
        }

        private void OnMusicVolumeChangedExternally()
        {
            SyncMusicSliderWithoutNotify();
        }

        #endregion

        #region Private Methods Ч Save

        private void SaveIfNeeded()
        {
            if (_volumeChanged)
            {
                SaveSystem.Save();
                _volumeChanged = false;
                Debug.Log("[PausedMenuUI] Volume settings saved");
            }
        }

        #endregion

        #region Private Methods Ч UI Sync

        private void SyncSlidersWithoutNotify()
        {
            SyncSoundSliderWithoutNotify();
            SyncMusicSliderWithoutNotify();
        }

        private void SyncSoundSliderWithoutNotify()
        {
            if (_soundVolumeSlider == null) return;

            _isUpdatingSliders = true;

            float volume = SoundManager.HasInstance
                ? SoundManager.Instance.GetSoundVolumeNormalized()
                : 0.7f;

            _soundVolumeSlider.SetValueWithoutNotify(volume);
            UpdateSoundVolumeText(volume);

            _isUpdatingSliders = false;
        }

        private void SyncMusicSliderWithoutNotify()
        {
            if (_musicVolumeSlider == null) return;

            _isUpdatingSliders = true;

            float volume = MusicManager.HasInstance
                ? MusicManager.Instance.GetMusicVolumeNormalized()
                : 0.5f;

            _musicVolumeSlider.SetValueWithoutNotify(volume);
            UpdateMusicVolumeText(volume);

            _isUpdatingSliders = false;
        }

        private void UpdateSoundVolumeText(float normalizedVolume)
        {
            if (_soundVolumeText == null) return;

            int percentage = Mathf.RoundToInt(normalizedVolume * 100f);
            _soundVolumeText.text = string.Format(SOUND_VOLUME_FORMAT, percentage);
        }

        private void UpdateMusicVolumeText(float normalizedVolume)
        {
            if (_musicVolumeText == null) return;

            int percentage = Mathf.RoundToInt(normalizedVolume * 100f);
            _musicVolumeText.text = string.Format(MUSIC_VOLUME_FORMAT, percentage);
        }

        #endregion

        #region Private Methods Ч Visibility

        private void Show()
        {
            _volumeChanged = false;
            gameObject.SetActive(true);
            SyncSlidersWithoutNotify();
            _resumeButton?.Select();
        }

        private void Hide()
        {
            SaveIfNeeded();
            gameObject.SetActive(false);
        }

        #endregion
    }
}
using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Managers;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace My.Scripts.UI.Menus
{
    public class SettingsMenuUI : MonoBehaviour
    {
        #region Constants

        private const string SOUND_VOLUME_FORMAT = "{0}%";
        private const string MUSIC_VOLUME_FORMAT = "{0}%";

        #endregion

        #region Serialized Fields

        [Header("Buttons")]
        [SerializeField] private Button _backButton;

        [Header("Volume Sliders")]
        [SerializeField] private Slider _soundVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;

        [Header("Volume Value Text")]
        [SerializeField] private TextMeshProUGUI _soundVolumeText;
        [SerializeField] private TextMeshProUGUI _musicVolumeText;

        #endregion

        #region Private Fields

        private bool _isSubscribedToEvents;
        private bool _isInitialized;

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

            // ВАЖНО: Сначала синхронизируем слайдеры БЕЗ подписки на события
            SyncSlidersWithoutNotify();

            // Только ПОСЛЕ синхронизации подписываемся на изменения слайдеров
            SubscribeToSliderEvents();

            _isInitialized = true;

            Debug.Log($"[SettingsMenuUI] Initialized. Music={GameData.MusicVolume:F3}, Sound={GameData.SoundVolume:F3}");

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
        }

        private void OnDestroy()
        {
            CleanupButtons();
            UnsubscribeFromSliderEvents();
            UnsubscribeFromVolumeChanges();
            UnsubscribeFromUIEvents();
        }

        #endregion

        #region Private Methods — Initialization

        private void SetupButtons()
        {
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
            }
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
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        #endregion

        #region Private Methods — Slider Events

        private void SubscribeToSliderEvents()
        {
            if (_soundVolumeSlider != null)
            {
                _soundVolumeSlider.onValueChanged.AddListener(OnSoundSliderChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }
        }

        private void UnsubscribeFromSliderEvents()
        {
            if (_soundVolumeSlider != null)
            {
                _soundVolumeSlider.onValueChanged.RemoveListener(OnSoundSliderChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            }
        }

        private void OnSoundSliderChanged(float value)
        {
            if (!_isInitialized) return;

            Debug.Log($"[SettingsMenuUI] Sound slider changed: {value:F3}");

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.SetSoundVolume(value);
            }

            UpdateSoundVolumeText(value);
        }

        private void OnMusicSliderChanged(float value)
        {
            if (!_isInitialized) return;

            Debug.Log($"[SettingsMenuUI] Music slider changed: {value:F3}");

            if (MusicManager.HasInstance)
            {
                MusicManager.Instance.SetMusicVolume(value);
            }

            UpdateMusicVolumeText(value);
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToUIEvents()
        {
            if (_isSubscribedToEvents) return;

            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.SettingsButtonPressed, OnSettingsButtonPressed);
            _isSubscribedToEvents = true;
        }

        private void UnsubscribeFromUIEvents()
        {
            if (!_isSubscribedToEvents) return;

            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler(GameEvents.SettingsButtonPressed, OnSettingsButtonPressed);
            _isSubscribedToEvents = false;
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

        #region Private Methods — Event Handlers

        private void OnSettingsButtonPressed()
        {
            Show();
        }

        private void OnBackClicked()
        {
            Hide();
            EventManager.Instance?.Broadcast(GameEvents.SettingsBackButtonPressed);
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

        #region Private Methods — UI Sync

        private void SyncSlidersWithoutNotify()
        {
            SyncSoundSliderWithoutNotify();
            SyncMusicSliderWithoutNotify();
        }

        private void SyncSoundSliderWithoutNotify()
        {
            if (_soundVolumeSlider == null) return;

            float volume = GameData.SoundVolume;

            _soundVolumeSlider.SetValueWithoutNotify(volume);
            UpdateSoundVolumeText(volume);

            Debug.Log($"[SettingsMenuUI] Synced sound slider: {volume:F3}");
        }

        private void SyncMusicSliderWithoutNotify()
        {
            if (_musicVolumeSlider == null) return;

            float volume = GameData.MusicVolume;

            _musicVolumeSlider.SetValueWithoutNotify(volume);
            UpdateMusicVolumeText(volume);

            Debug.Log($"[SettingsMenuUI] Synced music slider: {volume:F3}");
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

        #region Private Methods — Visibility

        private void Show()
        {
            gameObject.SetActive(true);
            SyncSlidersWithoutNotify();
            _soundVolumeSlider?.Select();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}
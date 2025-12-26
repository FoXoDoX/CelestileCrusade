using My.Scripts.Core.Data;
using My.Scripts.Core.Persistence;
using My.Scripts.EventBus;
using My.Scripts.Managers;
using System.Collections.Generic;
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
        private const string RESOLUTION_FORMAT = "{0} x {1}";

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

        [Header("Graphic Settings")]
        [SerializeField] private TMP_Dropdown _graphicsDropdown;

        [Header("Display Settings")]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _fullscreenToggle;

        [Header("Display Settings Containers (для скрытия в WebGL)")]
        [SerializeField] private GameObject _resolutionContainer;

        #endregion

        #region Private Fields

        private bool _isSubscribedToEvents;
        private bool _isInitialized;
        private bool _settingsChanged;

        private List<Resolution> _availableResolutions = new();
        private int _currentResolutionIndex;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupButtons();
            ConfigureSliderRanges();
            ConfigurePlatformSpecificUI();
        }

        private void Start()
        {
            SubscribeToUIEvents();
            SubscribeToVolumeChanges();

            InitializeGraphicsSettings();
            InitializeDisplaySettings();
            SyncSlidersWithoutNotify();

            SubscribeToSliderEvents();
            SubscribeToGraphicsEvents();
            SubscribeToDisplayEvents();

            _isInitialized = true;

            Debug.Log($"[SettingsMenuUI] Initialized. Graphics={GameData.GraphicsQuality}, Music={GameData.MusicVolume:F3}, Sound={GameData.SoundVolume:F3}");

            Hide();
        }

        private void OnEnable()
        {
            SubscribeToVolumeChanges();

            if (_isInitialized)
            {
                SyncSlidersWithoutNotify();
                SyncGraphicsSettings();
                SyncDisplaySettings();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromVolumeChanges();
            SaveIfNeeded();
        }

        private void OnDestroy()
        {
            CleanupButtons();
            UnsubscribeFromSliderEvents();
            UnsubscribeFromGraphicsEvents();
            UnsubscribeFromDisplayEvents();
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

        private void ConfigurePlatformSpecificUI()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Скрываем настройку разрешения в WebGL
            if (_resolutionContainer != null)
            {
                _resolutionContainer.SetActive(false);
            }
            else if (_resolutionDropdown != null)
            {
                _resolutionDropdown.gameObject.SetActive(false);
            }
#endif
        }

        private void CleanupButtons()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        #endregion

        #region Private Methods — Graphics Initialization

        private void InitializeGraphicsSettings()
        {
            if (_graphicsDropdown == null) return;

            int qualityLevel = GameData.GraphicsQuality;
            _graphicsDropdown.SetValueWithoutNotify(qualityLevel);

            QualitySettings.SetQualityLevel(qualityLevel);

            Debug.Log($"[SettingsMenuUI] Graphics initialized: {qualityLevel} ({QualitySettings.names[qualityLevel]})");
        }

        #endregion

        #region Private Methods — Display Initialization

        private void InitializeDisplaySettings()
        {
            InitializeResolutionDropdown();
            InitializeFullscreenToggle();
        }

        private void InitializeResolutionDropdown()
        {
            // Пропускаем для WebGL
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#endif

            if (_resolutionDropdown == null) return;

            _resolutionDropdown.ClearOptions();
            _availableResolutions.Clear();

            PopulateResolutionList();

            _currentResolutionIndex = FindCurrentResolutionIndex();
            _resolutionDropdown.SetValueWithoutNotify(_currentResolutionIndex);

            Debug.Log($"[SettingsMenuUI] Resolutions: {_availableResolutions.Count}, current index: {_currentResolutionIndex}");
        }

        private void PopulateResolutionList()
        {
            Resolution[] allResolutions = Screen.resolutions;
            HashSet<string> addedResolutions = new();
            List<string> options = new();

            for (int i = allResolutions.Length - 1; i >= 0; i--)
            {
                Resolution res = allResolutions[i];
                string key = $"{res.width}x{res.height}";

                if (addedResolutions.Contains(key)) continue;

                addedResolutions.Add(key);
                _availableResolutions.Add(res);
                options.Add(string.Format(RESOLUTION_FORMAT, res.width, res.height));
            }

            _resolutionDropdown.AddOptions(options);
        }

        private int FindCurrentResolutionIndex()
        {
            int savedWidth = GameData.ScreenWidth;
            int savedHeight = GameData.ScreenHeight;

            if (savedWidth > 0 && savedHeight > 0)
            {
                int savedIndex = FindResolutionIndex(savedWidth, savedHeight);
                if (savedIndex >= 0)
                {
                    return savedIndex;
                }
            }

            int currentIndex = FindResolutionIndex(Screen.width, Screen.height);
            if (currentIndex >= 0)
            {
                return currentIndex;
            }

            return 0;
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _availableResolutions.Count; i++)
            {
                if (_availableResolutions[i].width == width &&
                    _availableResolutions[i].height == height)
                {
                    return i;
                }
            }
            return -1;
        }

        private void InitializeFullscreenToggle()
        {
            if (_fullscreenToggle == null) return;

            bool isFullscreen = GameData.IsFullscreen;
            _fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);

            Debug.Log($"[SettingsMenuUI] Fullscreen initialized: {isFullscreen}");
        }

        #endregion

        #region Private Methods — Slider Events

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
            if (!_isInitialized) return;

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.SetSoundVolume(value);
                _settingsChanged = true;
            }

            UpdateSoundVolumeText(value);
        }

        private void OnMusicSliderChanged(float value)
        {
            if (!_isInitialized) return;

            if (MusicManager.HasInstance)
            {
                MusicManager.Instance.SetMusicVolume(value);
                _settingsChanged = true;
            }

            UpdateMusicVolumeText(value);
        }

        #endregion

        #region Private Methods — Graphics Events

        private void SubscribeToGraphicsEvents()
        {
            _graphicsDropdown?.onValueChanged.AddListener(OnGraphicsChanged);
        }

        private void UnsubscribeFromGraphicsEvents()
        {
            _graphicsDropdown?.onValueChanged.RemoveListener(OnGraphicsChanged);
        }

        private void OnGraphicsChanged(int index)
        {
            if (!_isInitialized) return;

            QualitySettings.SetQualityLevel(index);

            GameData.SetGraphicsQuality(index);
            _settingsChanged = true;

            Debug.Log($"[SettingsMenuUI] Graphics changed: {index} ({QualitySettings.names[index]})");
        }

        #endregion

        #region Private Methods — Display Events

        private void SubscribeToDisplayEvents()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _resolutionDropdown?.onValueChanged.AddListener(OnResolutionChanged);
#endif
            _fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
        }

        private void UnsubscribeFromDisplayEvents()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _resolutionDropdown?.onValueChanged.RemoveListener(OnResolutionChanged);
#endif
            _fullscreenToggle?.onValueChanged.RemoveListener(OnFullscreenChanged);
        }

        private void OnResolutionChanged(int index)
        {
            if (!_isInitialized) return;
            if (index < 0 || index >= _availableResolutions.Count) return;

            _currentResolutionIndex = index;
            Resolution selected = _availableResolutions[index];
            bool isFullscreen = _fullscreenToggle != null && _fullscreenToggle.isOn;

            ApplyResolution(selected.width, selected.height, isFullscreen);
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            if (!_isInitialized) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            // В WebGL используем Screen.fullScreen напрямую
            Screen.fullScreen = isFullscreen;
            GameData.SetFullscreen(isFullscreen);
            _settingsChanged = true;
            
            Debug.Log($"[SettingsMenuUI] WebGL Fullscreen: {isFullscreen}");
#else
            Resolution current = _availableResolutions[_currentResolutionIndex];
            ApplyResolution(current.width, current.height, isFullscreen);
#endif
        }

        private void ApplyResolution(int width, int height, bool isFullscreen)
        {
            Screen.SetResolution(width, height, isFullscreen);

            GameData.SetResolution(width, height);
            GameData.SetFullscreen(isFullscreen);
            _settingsChanged = true;

            Debug.Log($"[SettingsMenuUI] Applied: {width}x{height}, Fullscreen={isFullscreen}");
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
            SaveIfNeeded();
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

        #region Private Methods — Save

        private void SaveIfNeeded()
        {
            if (!_settingsChanged) return;

            SaveSystem.Save();
            _settingsChanged = false;

            Debug.Log("[SettingsMenuUI] Settings saved");
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
        }

        private void SyncMusicSliderWithoutNotify()
        {
            if (_musicVolumeSlider == null) return;

            float volume = GameData.MusicVolume;
            _musicVolumeSlider.SetValueWithoutNotify(volume);
            UpdateMusicVolumeText(volume);
        }

        private void SyncGraphicsSettings()
        {
            if (_graphicsDropdown == null) return;

            int qualityLevel = GameData.GraphicsQuality;
            _graphicsDropdown.SetValueWithoutNotify(qualityLevel);
        }

        private void SyncDisplaySettings()
        {
            SyncResolutionDropdown();
            SyncFullscreenToggle();
        }

        private void SyncResolutionDropdown()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#endif

            if (_resolutionDropdown == null) return;

            _currentResolutionIndex = FindCurrentResolutionIndex();
            _resolutionDropdown.SetValueWithoutNotify(_currentResolutionIndex);
        }

        private void SyncFullscreenToggle()
        {
            if (_fullscreenToggle == null) return;

            _fullscreenToggle.SetIsOnWithoutNotify(GameData.IsFullscreen);
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
            _settingsChanged = false;
            gameObject.SetActive(true);
            SyncSlidersWithoutNotify();
            SyncGraphicsSettings();
            SyncDisplaySettings();
            _soundVolumeSlider?.Select();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}
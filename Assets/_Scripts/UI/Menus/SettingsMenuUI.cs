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
        private const string FPS_UNLIMITED_TEXT = "∞";

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

        [Header("Graphics Settings")]
        [SerializeField] private Button _graphicsLeftButton;
        [SerializeField] private Button _graphicsRightButton;
        [SerializeField] private TextMeshProUGUI _graphicsValueText;

        [Header("FPS Settings")]
        [SerializeField] private Button _fpsLeftButton;
        [SerializeField] private Button _fpsRightButton;
        [SerializeField] private TextMeshProUGUI _fpsValueText;
        [SerializeField] private int[] _predefinedFpsValues = { 30, 60, 90, 120, 144, 165, 240 };
        [SerializeField] private bool _includeMonitorRefreshRate = true;

        [Header("VSync Settings")]
        [SerializeField] private Toggle _vsyncToggle;

        [Header("Resolution Settings")]
        [SerializeField] private Button _resolutionLeftButton;
        [SerializeField] private Button _resolutionRightButton;
        [SerializeField] private TextMeshProUGUI _resolutionValueText;

        [Header("Fullscreen Settings")]
        [SerializeField] private Toggle _fullscreenToggle;

        [Header("Animation")]
        [SerializeField] private SettingsMenuUIAnimation _animation;

        [Header("Containers (для скрытия в WebGL)")]
        [SerializeField] private GameObject _resolutionContainer;

        #endregion

        #region Private Fields

        private bool _isSubscribedToEvents;
        private bool _isInitialized;
        private bool _settingsChanged;

        private List<Resolution> _availableResolutions = new();
        private int _currentResolutionIndex;

        private List<int> _fpsOptions = new();
        private int _currentFpsIndex;

        private int _currentGraphicsIndex;

        private Color _fpsTextOriginalColor;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupButtons();
            ConfigureSliderRanges();
            ConfigurePlatformSpecificUI();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                SubscribeToVolumeChanges();
                SyncSlidersWithoutNotify();
                SyncGraphicsSettings();
                SyncFpsSettings();
                SyncVSyncSettings();
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
            UnsubscribeFromFpsEvents();
            UnsubscribeFromVSyncEvents();
            UnsubscribeFromDisplayEvents();
            UnsubscribeFromVolumeChanges();
        }

        #endregion

        #region Private Methods — Initialization

        private void EnsureInitialized()
        {
            if (_isInitialized) return;

            SubscribeToVolumeChanges();

            InitializeGraphicsSettings();
            InitializeFpsSettings();
            InitializeVSyncSettings();
            InitializeDisplaySettings();
            SyncSlidersWithoutNotify();

            SubscribeToSliderEvents();
            SubscribeToGraphicsEvents();
            SubscribeToFpsEvents();
            SubscribeToVSyncEvents();
            SubscribeToDisplayEvents();

            _isInitialized = true;

            Debug.Log($"[SettingsMenuUI] Initialized. Graphics={GameData.GraphicsQuality}, " +
                      $"Music={GameData.MusicVolume:F3}, Sound={GameData.SoundVolume:F3}, " +
                      $"FPS={Application.targetFrameRate}, VSync={QualitySettings.vSyncCount}");
        }

        private void SetupButtons()
        {
            if (_backButton != null)
            {
                BindButton(_backButton, OnBackClicked);
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
            if (_resolutionContainer != null)
            {
                _resolutionContainer.SetActive(false);
            }
            else
            {
                if (_resolutionLeftButton != null) _resolutionLeftButton.gameObject.SetActive(false);
                if (_resolutionRightButton != null) _resolutionRightButton.gameObject.SetActive(false);
                if (_resolutionValueText != null) _resolutionValueText.gameObject.SetActive(false);
            }
#endif
        }

        private void CleanupButtons()
        {
            if (_backButton != null)
            {
                UnbindButton(_backButton, OnBackClicked);
            }
        }

        #endregion

        #region Private Methods — Graphics Initialization

        private void InitializeGraphicsSettings()
        {
            _currentGraphicsIndex = GameData.GraphicsQuality;
            QualitySettings.SetQualityLevel(_currentGraphicsIndex);
            UpdateGraphicsText();

            Debug.Log($"[SettingsMenuUI] Graphics initialized: {_currentGraphicsIndex} ({QualitySettings.names[_currentGraphicsIndex]})");
        }

        #endregion

        #region Private Methods — FPS Initialization

        private void InitializeFpsSettings()
        {
            BuildFpsOptions();

            if (_fpsValueText != null)
            {
                _fpsTextOriginalColor = _fpsValueText.color;
            }

            int savedFps = GetSavedOrDefaultFps();
            _currentFpsIndex = FindFpsIndex(savedFps);

            int actualFps = _fpsOptions[_currentFpsIndex];
            Application.targetFrameRate = actualFps;

            UpdateFpsInteractability();

            Debug.Log($"[SettingsMenuUI] FPS initialized: {FormatFpsValue(actualFps)}");
        }

        private void BuildFpsOptions()
        {
            _fpsOptions.Clear();

            HashSet<int> added = new();

            if (_predefinedFpsValues != null)
            {
                foreach (int fps in _predefinedFpsValues)
                {
                    if (fps > 0 && added.Add(fps))
                    {
                        _fpsOptions.Add(fps);
                    }
                }
            }

            if (_includeMonitorRefreshRate)
            {
                int monitorHz = GetMonitorRefreshRate();
                if (monitorHz > 0 && added.Add(monitorHz))
                {
                    _fpsOptions.Add(monitorHz);
                }
            }

            _fpsOptions.Sort();
            _fpsOptions.Add(-1);
        }

        private int GetSavedOrDefaultFps()
        {
            int saved = GameData.TargetFPS;
            if (saved != 0) return saved;

            return GetMonitorRefreshRate();
        }

        private int FindFpsIndex(int fps)
        {
            for (int i = 0; i < _fpsOptions.Count; i++)
            {
                if (_fpsOptions[i] == fps) return i;
            }

            if (fps <= 0) return _fpsOptions.Count - 1;

            int closestIndex = 0;
            int closestDiff = int.MaxValue;

            for (int i = 0; i < _fpsOptions.Count; i++)
            {
                if (_fpsOptions[i] == -1) continue;

                int diff = Mathf.Abs(_fpsOptions[i] - fps);
                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private int GetMonitorRefreshRate()
        {
            int hz = (int)System.Math.Round(Screen.currentResolution.refreshRateRatio.value);
            return hz > 0 ? hz : 60;
        }

        #endregion

        #region Private Methods — VSync Initialization

        private void InitializeVSyncSettings()
        {
            if (_vsyncToggle == null) return;

            bool isVSync = GameData.IsVSyncEnabled;
            _vsyncToggle.SetIsOnWithoutNotify(isVSync);
            ApplyVSyncSetting(isVSync);

            Debug.Log($"[SettingsMenuUI] VSync initialized: {isVSync}");
        }

        #endregion

        #region Private Methods — Display Initialization

        private void InitializeDisplaySettings()
        {
            InitializeResolutionSelector();
            InitializeFullscreenToggle();
        }

        private void InitializeResolutionSelector()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#endif

            _availableResolutions.Clear();
            PopulateResolutionList();

            _currentResolutionIndex = FindCurrentResolutionIndex();
            UpdateResolutionText();

            Debug.Log($"[SettingsMenuUI] Resolutions: {_availableResolutions.Count}, current index: {_currentResolutionIndex}");
        }

        private void PopulateResolutionList()
        {
            Resolution[] allResolutions = Screen.resolutions;
            HashSet<string> addedResolutions = new();

            for (int i = allResolutions.Length - 1; i >= 0; i--)
            {
                Resolution res = allResolutions[i];
                string key = $"{res.width}x{res.height}";

                if (addedResolutions.Contains(key)) continue;

                addedResolutions.Add(key);
                _availableResolutions.Add(res);
            }
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
            _graphicsLeftButton?.onClick.AddListener(OnGraphicsLeftClicked);
            _graphicsRightButton?.onClick.AddListener(OnGraphicsRightClicked);
        }

        private void UnsubscribeFromGraphicsEvents()
        {
            _graphicsLeftButton?.onClick.RemoveListener(OnGraphicsLeftClicked);
            _graphicsRightButton?.onClick.RemoveListener(OnGraphicsRightClicked);
        }

        private void OnGraphicsLeftClicked()
        {
            if (!_isInitialized) return;

            int count = QualitySettings.names.Length;
            if (count == 0) return;

            _currentGraphicsIndex--;
            if (_currentGraphicsIndex < 0)
                _currentGraphicsIndex = count - 1;

            ApplyCurrentGraphics();
        }

        private void OnGraphicsRightClicked()
        {
            if (!_isInitialized) return;

            int count = QualitySettings.names.Length;
            if (count == 0) return;

            _currentGraphicsIndex++;
            if (_currentGraphicsIndex >= count)
                _currentGraphicsIndex = 0;

            ApplyCurrentGraphics();
        }

        private void ApplyCurrentGraphics()
        {
            QualitySettings.SetQualityLevel(_currentGraphicsIndex);

            // SetQualityLevel сбрасывает vSyncCount — восстанавливаем
            ApplyVSyncSetting(GameData.IsVSyncEnabled);

            GameData.SetGraphicsQuality(_currentGraphicsIndex);
            _settingsChanged = true;
            UpdateGraphicsText();

            Debug.Log($"[SettingsMenuUI] Graphics changed: {_currentGraphicsIndex} ({QualitySettings.names[_currentGraphicsIndex]})");
        }

        #endregion

        #region Private Methods — FPS Events

        private void SubscribeToFpsEvents()
        {
            _fpsLeftButton?.onClick.AddListener(OnFpsLeftClicked);
            _fpsRightButton?.onClick.AddListener(OnFpsRightClicked);
        }

        private void UnsubscribeFromFpsEvents()
        {
            _fpsLeftButton?.onClick.RemoveListener(OnFpsLeftClicked);
            _fpsRightButton?.onClick.RemoveListener(OnFpsRightClicked);
        }

        private void OnFpsLeftClicked()
        {
            if (!_isInitialized || _fpsOptions.Count == 0) return;

            _currentFpsIndex--;
            if (_currentFpsIndex < 0)
                _currentFpsIndex = _fpsOptions.Count - 1;

            ApplyCurrentFps();
        }

        private void OnFpsRightClicked()
        {
            if (!_isInitialized || _fpsOptions.Count == 0) return;

            _currentFpsIndex++;
            if (_currentFpsIndex >= _fpsOptions.Count)
                _currentFpsIndex = 0;

            ApplyCurrentFps();
        }

        private void ApplyCurrentFps()
        {
            int fps = _fpsOptions[_currentFpsIndex];
            Application.targetFrameRate = fps;
            GameData.SetTargetFPS(fps);
            _settingsChanged = true;
            UpdateFpsText();

            Debug.Log($"[SettingsMenuUI] FPS limit changed: {FormatFpsValue(fps)}");
        }

        #endregion

        #region Private Methods — VSync Events

        private void SubscribeToVSyncEvents()
        {
            _vsyncToggle?.onValueChanged.AddListener(OnVSyncChanged);
        }

        private void UnsubscribeFromVSyncEvents()
        {
            _vsyncToggle?.onValueChanged.RemoveListener(OnVSyncChanged);
        }

        private void OnVSyncChanged(bool enabled)
        {
            if (!_isInitialized) return;

            ApplyVSyncSetting(enabled);
            GameData.SetVSync(enabled);
            _settingsChanged = true;

            UpdateFpsInteractability();

            Debug.Log($"[SettingsMenuUI] VSync changed: {enabled}");
        }

        private void ApplyVSyncSetting(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
        }

        #endregion

        #region Private Methods — Resolution Events

        private void SubscribeToDisplayEvents()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _resolutionLeftButton?.onClick.AddListener(OnResolutionLeftClicked);
            _resolutionRightButton?.onClick.AddListener(OnResolutionRightClicked);
#endif
            _fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
        }

        private void UnsubscribeFromDisplayEvents()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _resolutionLeftButton?.onClick.RemoveListener(OnResolutionLeftClicked);
            _resolutionRightButton?.onClick.RemoveListener(OnResolutionRightClicked);
#endif
            _fullscreenToggle?.onValueChanged.RemoveListener(OnFullscreenChanged);
        }

        private void OnResolutionLeftClicked()
        {
            if (!_isInitialized || _availableResolutions.Count == 0) return;

            _currentResolutionIndex--;
            if (_currentResolutionIndex < 0)
                _currentResolutionIndex = _availableResolutions.Count - 1;

            ApplyCurrentResolution();
        }

        private void OnResolutionRightClicked()
        {
            if (!_isInitialized || _availableResolutions.Count == 0) return;

            _currentResolutionIndex++;
            if (_currentResolutionIndex >= _availableResolutions.Count)
                _currentResolutionIndex = 0;

            ApplyCurrentResolution();
        }

        private void ApplyCurrentResolution()
        {
            Resolution selected = _availableResolutions[_currentResolutionIndex];
            bool isFullscreen = _fullscreenToggle != null && _fullscreenToggle.isOn;

            ApplyResolution(selected.width, selected.height, isFullscreen);
            UpdateResolutionText();
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            if (!_isInitialized) return;

#if UNITY_WEBGL && !UNITY_EDITOR
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
            FullScreenMode mode = isFullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

            Screen.SetResolution(width, height, mode);

            GameData.SetResolution(width, height);
            GameData.SetFullscreen(isFullscreen);
            _settingsChanged = true;
        }

        #endregion

        #region Private Methods — Button Binding

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

        #region Private Methods — Event Subscription

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
            _currentGraphicsIndex = GameData.GraphicsQuality;
            UpdateGraphicsText();
        }

        private void SyncFpsSettings()
        {
            if (_fpsOptions.Count == 0) return;

            int savedFps = GetSavedOrDefaultFps();
            _currentFpsIndex = FindFpsIndex(savedFps);
            UpdateFpsInteractability();
        }

        private void SyncVSyncSettings()
        {
            if (_vsyncToggle == null) return;

            _vsyncToggle.SetIsOnWithoutNotify(GameData.IsVSyncEnabled);
            UpdateFpsInteractability();
        }

        private void SyncDisplaySettings()
        {
            SyncResolutionSelector();
            SyncFullscreenToggle();
        }

        private void SyncResolutionSelector()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#endif

            _currentResolutionIndex = FindCurrentResolutionIndex();
            UpdateResolutionText();
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

        private void UpdateGraphicsText()
        {
            if (_graphicsValueText == null) return;

            string[] names = QualitySettings.names;
            if (_currentGraphicsIndex >= 0 && _currentGraphicsIndex < names.Length)
            {
                _graphicsValueText.text = names[_currentGraphicsIndex];
            }
        }

        private void UpdateFpsInteractability()
        {
            bool vsyncOn = _vsyncToggle != null && _vsyncToggle.isOn;

            if (_fpsLeftButton != null) _fpsLeftButton.interactable = !vsyncOn;
            if (_fpsRightButton != null) _fpsRightButton.interactable = !vsyncOn;

            if (_fpsValueText != null)
            {
                _fpsValueText.color = vsyncOn
                    ? new Color32(0xC8, 0xC8, 0xC8, 0xFF)
                    : _fpsTextOriginalColor;
            }

            if (vsyncOn)
            {
                int monitorHz = GetMonitorRefreshRate();

                if (_fpsValueText != null)
                {
                    _fpsValueText.text = monitorHz.ToString();
                }
            }
            else
            {
                UpdateFpsText();
            }
        }

        private void UpdateFpsText()
        {
            if (_fpsValueText == null || _fpsOptions.Count == 0) return;

            int fps = _fpsOptions[_currentFpsIndex];
            _fpsValueText.text = FormatFpsValue(fps);
        }

        private void UpdateResolutionText()
        {
            if (_resolutionValueText == null) return;

            if (_currentResolutionIndex >= 0 && _currentResolutionIndex < _availableResolutions.Count)
            {
                Resolution res = _availableResolutions[_currentResolutionIndex];
                _resolutionValueText.text = string.Format(RESOLUTION_FORMAT, res.width, res.height);
            }
        }

        private string FormatFpsValue(int fps)
        {
            return fps == -1 ? FPS_UNLIMITED_TEXT : fps.ToString();
        }

        #endregion

        #region Private Methods — Visibility

        public void Show()
        {
            _settingsChanged = false;
            gameObject.SetActive(true);
            EnsureInitialized();

            SyncSlidersWithoutNotify();
            SyncGraphicsSettings();
            SyncFpsSettings();
            SyncVSyncSettings();
            SyncDisplaySettings();

            _backButton?.Select();
            _animation?.PlayShow();
        }

        private void Hide()
        {
            if (_animation != null)
            {
                _animation.PlayHide();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
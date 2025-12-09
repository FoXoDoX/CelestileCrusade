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

        private const string SOUND_VOLUME_FORMAT = "SOUND {0}";
        private const string MUSIC_VOLUME_FORMAT = "MUSIC {0}";

        #endregion

        #region Serialized Fields

        [Header("Buttons")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _soundVolumeButton;
        [SerializeField] private Button _musicVolumeButton;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _soundVolumeText;
        [SerializeField] private TextMeshProUGUI _musicVolumeText;

        #endregion

        #region Private Fields

        private bool _isSubscribedToEvents;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupButtons();
        }

        private void Start()
        {
            // ѕодписываемс€ на событи€ один раз
            SubscribeToUIEvents();
            SubscribeToVolumeChanges();
            UpdateVolumeDisplays();
            Hide();
        }

        private void OnEnable()
        {
            // ѕри включении обновл€ем отображение и подписки на звук
            SubscribeToVolumeChanges();
            UpdateVolumeDisplays();
        }

        private void OnDisable()
        {
            // ќтписываемс€ только от volume events Ч UI событи€ нужны всегда
            UnsubscribeFromVolumeChanges();
        }

        private void OnDestroy()
        {
            CleanupButtons();
            UnsubscribeFromVolumeChanges();
            UnsubscribeFromUIEvents();  // ѕолна€ отписка только при уничтожении
        }

        #endregion

        #region Private Methods Ч Initialization

        private void SetupButtons()
        {
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
            }

            if (_soundVolumeButton != null)
            {
                _soundVolumeButton.onClick.AddListener(OnSoundVolumeClicked);
            }

            if (_musicVolumeButton != null)
            {
                _musicVolumeButton.onClick.AddListener(OnMusicVolumeClicked);
            }
        }

        private void CleanupButtons()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
            }

            if (_soundVolumeButton != null)
            {
                _soundVolumeButton.onClick.RemoveListener(OnSoundVolumeClicked);
            }

            if (_musicVolumeButton != null)
            {
                _musicVolumeButton.onClick.RemoveListener(OnMusicVolumeClicked);
            }
        }

        #endregion

        #region Private Methods Ч Event Subscription

        /// <summary>
        /// ѕодписка на UI событи€ Ч делаетс€ один раз и держитс€ до уничтожени€
        /// </summary>
        private void SubscribeToUIEvents()
        {
            if (_isSubscribedToEvents) return;

            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.SettingsButtonPressed, OnSettingsButtonPressed);
            _isSubscribedToEvents = true;
        }

        /// <summary>
        /// ќтписка от UI событий Ч только при уничтожении объекта
        /// </summary>
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
                SoundManager.Instance.OnSoundVolumeChanged += UpdateSoundVolumeDisplay;
            }

            if (MusicManager.HasInstance)
            {
                MusicManager.Instance.OnMusicVolumeChanged += UpdateMusicVolumeDisplay;
            }
        }

        private void UnsubscribeFromVolumeChanges()
        {
            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.OnSoundVolumeChanged -= UpdateSoundVolumeDisplay;
            }

            if (MusicManager.HasInstance)
            {
                MusicManager.Instance.OnMusicVolumeChanged -= UpdateMusicVolumeDisplay;
            }
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnSettingsButtonPressed()
        {
            Show();
        }

        private void OnBackClicked()
        {
            Hide();
            EventManager.Instance?.Broadcast(GameEvents.SettingsBackButtonPressed);
        }

        private void OnSoundVolumeClicked()
        {
            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.ChangeSoundVolume();
            }
        }

        private void OnMusicVolumeClicked()
        {
            if (MusicManager.HasInstance)
            {
                MusicManager.Instance.ChangeMusicVolume();
            }
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

            int volume = SoundManager.HasInstance
                ? SoundManager.Instance.GetSoundVolume()
                : 0;

            _soundVolumeText.text = string.Format(SOUND_VOLUME_FORMAT, volume);
        }

        private void UpdateMusicVolumeDisplay()
        {
            if (_musicVolumeText == null) return;

            int volume = MusicManager.HasInstance
                ? MusicManager.Instance.GetMusicVolume()
                : 0;

            _musicVolumeText.text = string.Format(MUSIC_VOLUME_FORMAT, volume);
        }

        #endregion

        #region Private Methods Ч Visibility

        private void Show()
        {
            gameObject.SetActive(true);
            UpdateVolumeDisplays();
            _backButton?.Select();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}
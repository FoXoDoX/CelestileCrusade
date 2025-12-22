using My.Scripts.Core.Data;
using My.Scripts.Core.Utility;
using My.Scripts.EventBus;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace My.Scripts.Managers
{
    public class MusicManager : PersistentSingleton<MusicManager>
    {
        #region Constants

        private const string MIXER_MUSIC_PARAM = "MusicVolume";
        private const float MIN_DECIBELS = -80f;
        private const float TRACK_END_THRESHOLD = 0.1f;
        private const float MIXER_INIT_DELAY = 0.05f;

        #endregion

        #region Serialized Fields

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _musicMixerGroup;

        [Header("Music Tracks")]
        [SerializeField] private AudioClip[] _musicTracks;

        [Header("Settings")]
        [SerializeField] private bool _shuffleTracks = true;
        [SerializeField] private bool _loopPlaylist = true;

        #endregion

        #region Private Fields

        private AudioSource _audioSource;
        private List<int> _availableTrackIndices = new();
        private int _currentTrackIndex = -1;
        private bool _isApplicationFocused = true;
        private bool _isPaused;
        private bool _isMixerReady;
        private bool _isFullyInitialized;

        #endregion

        #region Events

        public event Action OnMusicVolumeChanged;
        public event Action<AudioClip> OnTrackChanged;

        #endregion

        #region Properties

        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;
        public bool IsPaused => _isPaused;
        public AudioClip CurrentTrack => _audioSource?.clip;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            Debug.Log($"[MusicManager] OnSingletonAwake - GameData.MusicVolume = {GameData.MusicVolume:F3}");

            InitializeAudioSource();
            InitializeTrackList();

            // Запускаем инициализацию микшера
            StartCoroutine(InitializeMixerAndStartPlayback());
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Проверяем окончание трека только после полной инициализации
            if (_isFullyInitialized)
            {
                CheckTrackEnd();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Игнорируем события фокуса до полной инициализации
            if (!_isFullyInitialized) return;

            HandleApplicationFocus(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Игнорируем события паузы до полной инициализации
            if (!_isFullyInitialized) return;

            HandleApplicationFocus(!pauseStatus);
        }

        #endregion

        #region Private Methods — Initialization

        private IEnumerator InitializeMixerAndStartPlayback()
        {
            // Ждём один кадр для инициализации Unity систем
            yield return null;

            // Небольшая задержка для надёжности AudioMixer
            yield return new WaitForSecondsRealtime(MIXER_INIT_DELAY);

            // Применяем громкость ДО начала воспроизведения
            _isMixerReady = true;
            ApplyVolumeToMixer();

            Debug.Log($"[MusicManager] Mixer ready, starting playback with volume: {GameData.MusicVolume:F3}");

            // Теперь запускаем воспроизведение
            StartPlayback();

            // Помечаем полную готовность
            _isFullyInitialized = true;
        }

        private void InitializeAudioSource()
        {
            _audioSource = GetComponent<AudioSource>();

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
            _audioSource.volume = 1f;
            _audioSource.outputAudioMixerGroup = _musicMixerGroup;
        }

        private void InitializeTrackList()
        {
            _availableTrackIndices.Clear();

            if (_musicTracks == null) return;

            for (int i = 0; i < _musicTracks.Length; i++)
            {
                if (_musicTracks[i] != null)
                {
                    _availableTrackIndices.Add(i);
                }
            }
        }

        private void StartPlayback()
        {
            if (CanPlayMusic())
            {
                PlayNextTrack();
            }
        }

        #endregion

        #region Public Methods — Volume Control

        public void SetMusicVolume(float normalizedVolume)
        {
            float clampedVolume = Mathf.Clamp01(normalizedVolume);

            GameData.SetMusicVolume(clampedVolume);

            if (_isMixerReady)
            {
                ApplyVolumeToMixer();
            }

            OnMusicVolumeChanged?.Invoke();
            EventManager.Instance?.Broadcast(GameEvents.MusicVolumeChanged);
        }

        public float GetMusicVolumeNormalized() => GameData.MusicVolume;

        public int GetMusicVolumePercent() => Mathf.RoundToInt(GameData.MusicVolume * 100f);

        #endregion

        #region Public Methods — Playback Control

        public void Play()
        {
            if (_audioSource == null || !_isFullyInitialized) return;

            if (_audioSource.clip == null)
            {
                PlayNextTrack();
            }
            else
            {
                _audioSource.Play();
                _isPaused = false;
            }
        }

        public void Pause()
        {
            if (_audioSource == null || !_audioSource.isPlaying) return;

            _audioSource.Pause();
            _isPaused = true;
        }

        public void Resume()
        {
            if (_audioSource == null || !_isPaused) return;

            _audioSource.UnPause();
            _isPaused = false;
        }

        public void Stop()
        {
            if (_audioSource == null) return;

            _audioSource.Stop();
            _isPaused = false;
        }

        public void PlayNextTrack()
        {
            if (!CanPlayMusic() || !_isMixerReady) return;

            SelectNextTrack();
            PlayCurrentTrack();
        }

        public void RestartPlaylist()
        {
            InitializeTrackList();
            PlayNextTrack();
        }

        public void TogglePause()
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }

        #endregion

        #region Private Methods — Volume

        private void ApplyVolumeToMixer()
        {
            if (_audioMixer == null)
            {
                Debug.LogWarning($"[{nameof(MusicManager)}] AudioMixer is not assigned!");
                return;
            }

            float volume = GameData.MusicVolume;
            float decibels = NormalizedToDecibels(volume);

            bool success = _audioMixer.SetFloat(MIXER_MUSIC_PARAM, decibels);

            if (!success)
            {
                Debug.LogError($"[MusicManager] Failed to set '{MIXER_MUSIC_PARAM}'. Check if parameter is exposed in AudioMixer!");
                return;
            }

            // Верификация
            _audioMixer.GetFloat(MIXER_MUSIC_PARAM, out float actualValue);

            if (!Mathf.Approximately(actualValue, decibels))
            {
                Debug.LogError($"[MusicManager] Mixer value mismatch! Set={decibels:F1}, Got={actualValue:F1}");
            }
            else
            {
                Debug.Log($"[MusicManager] Volume applied: {volume:P0} → {decibels:F1} dB ✓");
            }
        }

        private float NormalizedToDecibels(float normalizedVolume)
        {
            if (normalizedVolume <= 0.0001f)
            {
                return MIN_DECIBELS;
            }

            return Mathf.Log10(normalizedVolume) * 20f;
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToEvents()
        {
            EventManager.Instance?.AddHandler(GameEvents.GamePaused, OnGamePaused);
            EventManager.Instance?.AddHandler(GameEvents.GameUnpaused, OnGameUnpaused);
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Instance?.RemoveHandler(GameEvents.GamePaused, OnGamePaused);
            EventManager.Instance?.RemoveHandler(GameEvents.GameUnpaused, OnGameUnpaused);
        }

        private void OnGamePaused() { }
        private void OnGameUnpaused() { }

        #endregion

        #region Private Methods — Playback Logic

        private bool CanPlayMusic()
        {
            return _audioSource != null &&
                   _musicTracks != null &&
                   _musicTracks.Length > 0;
        }

        private void SelectNextTrack()
        {
            if (_availableTrackIndices.Count == 0)
            {
                if (!_loopPlaylist) return;

                InitializeTrackList();

                if (_currentTrackIndex >= 0 && _availableTrackIndices.Count > 1)
                {
                    _availableTrackIndices.Remove(_currentTrackIndex);
                }
            }

            if (_availableTrackIndices.Count == 0) return;

            int listIndex = _shuffleTracks
                ? UnityEngine.Random.Range(0, _availableTrackIndices.Count)
                : 0;

            _currentTrackIndex = _availableTrackIndices[listIndex];
            _availableTrackIndices.RemoveAt(listIndex);
        }

        private void PlayCurrentTrack()
        {
            if (_currentTrackIndex < 0 || _currentTrackIndex >= _musicTracks.Length) return;

            var track = _musicTracks[_currentTrackIndex];
            if (track == null) return;

            _audioSource.Stop();
            _audioSource.clip = track;
            _audioSource.Play();

            _isPaused = false;

            OnTrackChanged?.Invoke(track);
            Debug.Log($"[{nameof(MusicManager)}] Playing: {track.name}");
        }

        private void CheckTrackEnd()
        {
            if (!_isApplicationFocused || _isPaused || _audioSource == null) return;
            if (_audioSource.clip == null) return;

            bool isNearEnd = _audioSource.time >= _audioSource.clip.length - TRACK_END_THRESHOLD;

            if (isNearEnd && _audioSource.isPlaying)
            {
                PlayNextTrack();
            }
            else if (!_audioSource.isPlaying && !_isPaused)
            {
                TryResumeOrPlayNext();
            }
        }

        private void TryResumeOrPlayNext()
        {
            if (_audioSource.clip == null)
            {
                PlayNextTrack();
                return;
            }

            bool hasTimeRemaining = _audioSource.time < _audioSource.clip.length - TRACK_END_THRESHOLD;

            if (hasTimeRemaining)
            {
                _audioSource.Play();
            }
            else
            {
                PlayNextTrack();
            }
        }

        private void TryResumePlayback()
        {
            if (!_isApplicationFocused || _isPaused || _audioSource == null) return;

            if (_audioSource.clip != null && !_audioSource.isPlaying)
            {
                TryResumeOrPlayNext();
            }
            else if (_audioSource.clip == null && CanPlayMusic())
            {
                PlayNextTrack();
            }
        }

        #endregion

        #region Private Methods — Application Focus

        private void HandleApplicationFocus(bool hasFocus)
        {
            _isApplicationFocused = hasFocus;

            if (_audioSource == null) return;

            if (hasFocus)
            {
                if (!_isPaused)
                {
                    if (_isMixerReady)
                    {
                        ApplyVolumeToMixer();
                    }
                    TryResumePlayback();
                }
            }
            else
            {
                if (_audioSource.isPlaying)
                {
                    _audioSource.Pause();
                }
            }
        }

        #endregion
    }
}
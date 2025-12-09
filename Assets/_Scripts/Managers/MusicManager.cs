using My.Scripts.Core.Utility;
using My.Scripts.EventBus;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Scripts.Managers
{
    public class MusicManager : PersistentSingleton<MusicManager>
    {
        #region Constants

        private const int MUSIC_VOLUME_MAX = 10;
        private const int MUSIC_VOLUME_DEFAULT = 4;
        private const float TRACK_END_THRESHOLD = 0.1f;

        #endregion

        #region Static Fields

        private static int _musicVolume = MUSIC_VOLUME_DEFAULT;

        #endregion

        #region Serialized Fields

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

        #endregion

        #region Events

        public event Action OnMusicVolumeChanged;
        public event Action<AudioClip> OnTrackChanged;

        #endregion

        #region Properties

        public int MusicVolume => _musicVolume;
        public float MusicVolumeNormalized => (float)_musicVolume / MUSIC_VOLUME_MAX;
        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;
        public bool IsPaused => _isPaused;
        public AudioClip CurrentTrack => _audioSource?.clip;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            InitializeAudioSource();
            InitializeTrackList();
            StartPlayback();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            TryResumePlayback();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            CheckTrackEnd();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            HandleApplicationFocus(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // ƒл€ мобильных платформ
            HandleApplicationFocus(!pauseStatus);
        }

        #endregion

        #region Public Methods Ч Volume Control

        public void ChangeMusicVolume()
        {
            _musicVolume = (_musicVolume + 1) % (MUSIC_VOLUME_MAX + 1);
            ApplyVolume();

            OnMusicVolumeChanged?.Invoke();
            EventManager.Instance?.Broadcast(GameEvents.MusicVolumeChanged);

            Debug.Log($"[{nameof(MusicManager)}] Volume: {_musicVolume}");
        }

        public void SetMusicVolume(int volume)
        {
            _musicVolume = Mathf.Clamp(volume, 0, MUSIC_VOLUME_MAX);
            ApplyVolume();

            OnMusicVolumeChanged?.Invoke();
        }

        public int GetMusicVolume() => _musicVolume;

        public float GetMusicVolumeNormalized() => MusicVolumeNormalized;

        #endregion

        #region Public Methods Ч Playback Control

        public void Play()
        {
            if (_audioSource == null) return;

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
            if (!CanPlayMusic()) return;

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

        #region Private Methods Ч Initialization

        private void InitializeAudioSource()
        {
            _audioSource = GetComponent<AudioSource>();

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.loop = false; // ћы сами управл€ем переключением треков
            _audioSource.playOnAwake = false;
            _audioSource.volume = MusicVolumeNormalized;
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

        #region Private Methods Ч Event Subscription

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

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnGamePaused()
        {
            // ќпционально: приглушить музыку при паузе
            // _audioSource.volume = MusicVolumeNormalized * 0.3f;
        }

        private void OnGameUnpaused()
        {
            // ќпционально: вернуть громкость
            // _audioSource.volume = MusicVolumeNormalized;
        }

        #endregion

        #region Private Methods Ч Playback Logic

        private bool CanPlayMusic()
        {
            return _audioSource != null &&
                   _musicTracks != null &&
                   _musicTracks.Length > 0;
        }

        private void SelectNextTrack()
        {
            // ≈сли список пуст, переинициализируем
            if (_availableTrackIndices.Count == 0)
            {
                if (!_loopPlaylist) return;

                InitializeTrackList();

                // »сключаем текущий трек чтобы не повтор€лс€ сразу
                if (_currentTrackIndex >= 0 && _availableTrackIndices.Count > 1)
                {
                    _availableTrackIndices.Remove(_currentTrackIndex);
                }
            }

            if (_availableTrackIndices.Count == 0) return;

            // ¬ыбираем следующий трек
            int listIndex;
            if (_shuffleTracks)
            {
                listIndex = UnityEngine.Random.Range(0, _availableTrackIndices.Count);
            }
            else
            {
                listIndex = 0;
            }

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
            _audioSource.volume = MusicVolumeNormalized;
            _audioSource.Play();

            _isPaused = false;

            OnTrackChanged?.Invoke(track);
            Debug.Log($"[{nameof(MusicManager)}] Playing: {track.name}");
        }

        private void CheckTrackEnd()
        {
            if (!_isApplicationFocused || _isPaused || _audioSource == null) return;
            if (_audioSource.clip == null) return;

            // “рек почти закончилс€
            bool isNearEnd = _audioSource.time >= _audioSource.clip.length - TRACK_END_THRESHOLD;

            if (isNearEnd && _audioSource.isPlaying)
            {
                PlayNextTrack();
            }
            // ≈сли музыка остановилась неожиданно
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

        #region Private Methods Ч Application Focus

        private void HandleApplicationFocus(bool hasFocus)
        {
            _isApplicationFocused = hasFocus;

            if (_audioSource == null) return;

            if (hasFocus)
            {
                // ¬озвращаем фокус Ч возобновл€ем если нужно
                if (!_isPaused)
                {
                    TryResumePlayback();
                }
            }
            else
            {
                // “ер€ем фокус Ч ставим на паузу
                if (_audioSource.isPlaying)
                {
                    _audioSource.Pause();
                    // Ќе мен€ем _isPaused Ч это системна€ пауза
                }
            }
        }

        #endregion

        #region Private Methods Ч Helpers

        private void ApplyVolume()
        {
            if (_audioSource != null)
            {
                _audioSource.volume = MusicVolumeNormalized;
            }
        }

        #endregion
    }
}
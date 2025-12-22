using My.Scripts.Core.Data;
using My.Scripts.Core.Utility;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace My.Scripts.Managers
{
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        #region Constants

        private const string MIXER_SOUND_PARAM = "SoundVolume";
        private const float MIN_DECIBELS = -80f;
        private const float WIND_FADE_TIME = 0.5f;
        private const float MIXER_INIT_DELAY = 0.05f;

        #endregion

        #region Serialized Fields

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _soundMixerGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _mainAudioSource;
        [SerializeField] private AudioSource _windAudioSource;
        [SerializeField] private AudioSource _progressBarAudioSource;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip _fuelPickupClip;
        [SerializeField] private AudioClip _coinPickupClip;
        [SerializeField] private AudioClip _crashClip;
        [SerializeField] private AudioClip _landingSuccessClip;
        [SerializeField] private AudioClip _crateCrackedClip;
        [SerializeField] private AudioClip _crateDestroyedClip;
        [SerializeField] private AudioClip _crateDeliveredClip;
        [SerializeField] private AudioClip _keyPickupClip;
        [SerializeField] private AudioClip _keyDeliveredClip;
        [SerializeField] private AudioClip _progressBarClip;
        [SerializeField] private AudioClip _windClip;
        [SerializeField] private AudioClip _turretShootClip;
        [SerializeField] private AudioClip _thrusterClip;

        #endregion

        #region Private Fields

        private Coroutine _windFadeCoroutine;
        private bool _isWindPlaying;
        private bool _isPaused;
        private bool _isMixerReady;

        // Thruster audio
        private AudioSource _thrusterAudioSource;
        private bool _isThrusterPlaying;

        private readonly List<AudioSourcePauseState> _pausedAudioStates = new();

        #endregion

        #region Nested Types

        private struct AudioSourcePauseState
        {
            public AudioSource Source;
            public bool WasPlaying;
            public float Volume;
        }

        #endregion

        #region Events

        public event Action OnSoundVolumeChanged;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            ValidateAudioSources();
            ConfigureAudioSources();
        }

        private void Start()
        {
            StartCoroutine(InitializeMixerDelayed());
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
            StopAllCoroutines();
            CleanupThrusterAudio();
        }

        #endregion

        #region Private Methods Ч Initialization

        private IEnumerator InitializeMixerDelayed()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(MIXER_INIT_DELAY);

            _isMixerReady = true;
            ApplyVolumeToMixer();

            Debug.Log($"[SoundManager] Mixer initialized, volume applied: {GameData.SoundVolume:F3}");
        }

        private void ValidateAudioSources()
        {
            if (_audioMixer == null)
            {
                Debug.LogError($"[{nameof(SoundManager)}] AudioMixer is not assigned!");
            }

            if (_mainAudioSource == null)
            {
                Debug.LogError($"[{nameof(SoundManager)}] Main AudioSource is not assigned!");
            }
        }

        private void ConfigureAudioSources()
        {
            if (_mainAudioSource != null)
            {
                _mainAudioSource.playOnAwake = false;
                _mainAudioSource.loop = false;
                _mainAudioSource.outputAudioMixerGroup = _soundMixerGroup;
            }

            if (_windAudioSource != null)
            {
                _windAudioSource.playOnAwake = false;
                _windAudioSource.loop = true;
                _windAudioSource.clip = _windClip;
                _windAudioSource.volume = 0f;
                _windAudioSource.outputAudioMixerGroup = _soundMixerGroup;
            }

            if (_progressBarAudioSource != null)
            {
                _progressBarAudioSource.playOnAwake = false;
                _progressBarAudioSource.loop = false;
                _progressBarAudioSource.outputAudioMixerGroup = _soundMixerGroup;
            }
        }

        #endregion

        #region Public Methods Ч Volume Control

        public void SetSoundVolume(float normalizedVolume)
        {
            float clampedVolume = Mathf.Clamp01(normalizedVolume);

            GameData.SetSoundVolume(clampedVolume);

            if (_isMixerReady)
            {
                ApplyVolumeToMixer();
            }

            OnSoundVolumeChanged?.Invoke();

            Debug.Log($"[SoundManager] Volume: {GetSoundVolumePercent()}%");
        }

        public float GetSoundVolumeNormalized() => GameData.SoundVolume;

        public int GetSoundVolumePercent() => Mathf.RoundToInt(GameData.SoundVolume * 100f);

        #endregion

        #region Public Methods Ч Sound Playback

        public void PlaySound(AudioClip clip)
        {
            if (clip == null || _mainAudioSource == null || _isPaused) return;

            _mainAudioSource.PlayOneShot(clip, 1f);
        }

        public void PlayProgressBarSound()
        {
            if (_progressBarClip == null || _progressBarAudioSource == null || _isPaused) return;
            if (_progressBarAudioSource.isPlaying) return;

            _progressBarAudioSource.clip = _progressBarClip;
            _progressBarAudioSource.Play();
        }

        public void StopProgressBarSound()
        {
            if (_progressBarAudioSource != null && _progressBarAudioSource.isPlaying)
            {
                _progressBarAudioSource.Stop();
            }
        }

        public void PlayWindSound()
        {
            if (_windClip == null || _windAudioSource == null) return;

            if (_isPaused)
            {
                _isWindPlaying = true;
                return;
            }

            StopWindFadeCoroutine();

            if (!_isWindPlaying)
            {
                _windAudioSource.volume = 0f;
                _windAudioSource.Play();
                _isWindPlaying = true;
            }

            _windFadeCoroutine = StartCoroutine(FadeWindVolume(1f));
        }

        public void StopWindSound()
        {
            if (_windAudioSource == null) return;

            if (_isPaused)
            {
                _isWindPlaying = false;
                return;
            }

            StopWindFadeCoroutine();
            _windFadeCoroutine = StartCoroutine(FadeWindVolume(0f));
        }

        public void RefreshSubscriptions()
        {
            UnsubscribeFromEvents();
            SubscribeToEvents();
        }

        #endregion

        #region Public Methods Ч Thruster Audio

        /// <summary>
        /// –егистрирует AudioSource двигател€ Lander.
        /// ¬ызываетс€ при спавне Lander.
        /// </summary>
        public void RegisterThrusterAudioSource(AudioSource thrusterSource)
        {
            if (thrusterSource == null)
            {
                Debug.LogWarning($"[{nameof(SoundManager)}] Thruster AudioSource is null!");
                return;
            }

            _thrusterAudioSource = thrusterSource;

            // Ќастраиваем AudioSource
            _thrusterAudioSource.clip = _thrusterClip;
            _thrusterAudioSource.loop = true;
            _thrusterAudioSource.playOnAwake = false;
            _thrusterAudioSource.volume = 0f;
            _thrusterAudioSource.outputAudioMixerGroup = _soundMixerGroup;
            _thrusterAudioSource.Play();

            _isThrusterPlaying = false;

            Debug.Log($"[{nameof(SoundManager)}] Thruster AudioSource registered");
        }

        /// <summary>
        /// ќтмен€ет регистрацию AudioSource двигател€.
        /// ¬ызываетс€ при уничтожении Lander.
        /// </summary>
        public void UnregisterThrusterAudioSource()
        {
            StopThrusterSound();
            _thrusterAudioSource = null;

            Debug.Log($"[{nameof(SoundManager)}] Thruster AudioSource unregistered");
        }

        public void StartThrusterSound()
        {
            if (_isThrusterPlaying) return;
            if (_thrusterAudioSource == null) return;
            if (_isPaused) return;

            _isThrusterPlaying = true;
            _thrusterAudioSource.volume = 1f; // Mixer контролирует громкость

            Debug.Log($"[{nameof(SoundManager)}] Thruster started");
        }

        public void StopThrusterSound()
        {
            if (!_isThrusterPlaying) return;
            if (_thrusterAudioSource == null) return;

            _isThrusterPlaying = false;
            _thrusterAudioSource.volume = 0f;

            Debug.Log($"[{nameof(SoundManager)}] Thruster stopped");
        }

        #endregion

        #region Public Methods Ч Pause Control

        public void PauseAllSounds()
        {
            if (_isPaused) return;

            _isPaused = true;
            _pausedAudioStates.Clear();

            StopWindFadeCoroutine();

            PauseAudioSource(_mainAudioSource);
            PauseAudioSource(_windAudioSource);
            PauseAudioSource(_progressBarAudioSource);
            PauseAudioSource(_thrusterAudioSource);
        }

        public void ResumeAllSounds()
        {
            if (!_isPaused) return;

            _isPaused = false;

            foreach (var state in _pausedAudioStates)
            {
                if (state.Source != null && state.WasPlaying)
                {
                    state.Source.volume = state.Volume;
                    state.Source.UnPause();
                }
            }

            _pausedAudioStates.Clear();
        }

        #endregion

        #region Private Methods Ч Pause Helpers

        private void PauseAudioSource(AudioSource source)
        {
            if (source == null) return;

            var state = new AudioSourcePauseState
            {
                Source = source,
                WasPlaying = source.isPlaying,
                Volume = source.volume
            };

            _pausedAudioStates.Add(state);

            if (source.isPlaying)
            {
                source.Pause();
            }
        }

        #endregion

        #region Private Methods Ч Volume

        private void ApplyVolumeToMixer()
        {
            if (_audioMixer == null) return;

            float decibels = NormalizedToDecibels(GameData.SoundVolume);
            _audioMixer.SetFloat(MIXER_SOUND_PARAM, decibels);
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

        #region Private Methods Ч Event Subscriptions

        private void SubscribeToEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.GamePaused, OnGamePaused);
            em.AddHandler(GameEvents.GameUnpaused, OnGameUnpaused);
            em.AddHandler(GameEvents.TurretShoot, OnTurretShoot);
            em.AddHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.AddHandler(GameEvents.CrateCracked, OnCrateCracked);
            em.AddHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);
            em.AddHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.AddHandler(GameEvents.KeyPickup, OnKeyPickup);

            // Thruster events
            em.AddHandler(GameEvents.LanderBeforeForce, OnLanderBeforeForce);
            em.AddHandler(GameEvents.LanderUpForce, OnLanderUpForce);
            em.AddHandler(GameEvents.LanderLeftForce, OnLanderLeftForce);
            em.AddHandler(GameEvents.LanderRightForce, OnLanderRightForce);

            em.AddHandler<PickupEventData>(GameEvents.FuelPickup, OnFuelPickup);
            em.AddHandler<PickupEventData>(GameEvents.CoinPickup, OnCoinPickup);
            em.AddHandler<KeyDeliveredData>(GameEvents.KeyDelivered, OnKeyDelivered);
            em.AddHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
        }

        private void UnsubscribeFromEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler(GameEvents.GamePaused, OnGamePaused);
            em.RemoveHandler(GameEvents.GameUnpaused, OnGameUnpaused);
            em.RemoveHandler(GameEvents.TurretShoot, OnTurretShoot);
            em.RemoveHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.RemoveHandler(GameEvents.CrateCracked, OnCrateCracked);
            em.RemoveHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);
            em.RemoveHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.RemoveHandler(GameEvents.KeyPickup, OnKeyPickup);

            // Thruster events
            em.RemoveHandler(GameEvents.LanderBeforeForce, OnLanderBeforeForce);
            em.RemoveHandler(GameEvents.LanderUpForce, OnLanderUpForce);
            em.RemoveHandler(GameEvents.LanderLeftForce, OnLanderLeftForce);
            em.RemoveHandler(GameEvents.LanderRightForce, OnLanderRightForce);

            em.RemoveHandler<PickupEventData>(GameEvents.FuelPickup, OnFuelPickup);
            em.RemoveHandler<PickupEventData>(GameEvents.CoinPickup, OnCoinPickup);
            em.RemoveHandler<KeyDeliveredData>(GameEvents.KeyDelivered, OnKeyDelivered);
            em.RemoveHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnGamePaused() => PauseAllSounds();
        private void OnGameUnpaused() => ResumeAllSounds();

        private void OnFuelPickup(PickupEventData data) => PlaySound(_fuelPickupClip);
        private void OnCoinPickup(PickupEventData data) => PlaySound(_coinPickupClip);
        private void OnTurretShoot() => PlaySound(_turretShootClip);
        private void OnCrateDrop() => PlaySound(_crateDeliveredClip);
        private void OnCrateCracked() => PlaySound(_crateCrackedClip);
        private void OnCrateDestroyed() => PlaySound(_crateDestroyedClip);
        private void OnRopeWithCrateSpawned() => RefreshSubscriptions();
        private void OnKeyPickup() => PlaySound(_keyPickupClip);
        private void OnKeyDelivered(KeyDeliveredData data) => PlaySound(_keyDeliveredClip);

        private void OnLanderLanded(LanderLandedData data)
        {
            // ќстанавливаем двигатель при посадке
            StopThrusterSound();

            var clip = data.LandingType == Lander.LandingType.Success
                ? _landingSuccessClip
                : _crashClip;

            PlaySound(clip);
        }

        // Thruster event handlers
        private void OnLanderBeforeForce() => StopThrusterSound();
        private void OnLanderUpForce() => StartThrusterSound();
        private void OnLanderLeftForce() => StartThrusterSound();
        private void OnLanderRightForce() => StartThrusterSound();

        #endregion

        #region Private Methods Ч Audio Helpers

        private void StopWindFadeCoroutine()
        {
            if (_windFadeCoroutine != null)
            {
                StopCoroutine(_windFadeCoroutine);
                _windFadeCoroutine = null;
            }
        }

        private IEnumerator FadeWindVolume(float targetVolume)
        {
            if (_windAudioSource == null) yield break;

            float startVolume = _windAudioSource.volume;
            float elapsedTime = 0f;

            while (elapsedTime < WIND_FADE_TIME)
            {
                if (_windAudioSource == null) yield break;
                if (_isPaused) yield break;

                elapsedTime += Time.deltaTime;
                float t = elapsedTime / WIND_FADE_TIME;
                _windAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }

            if (_windAudioSource != null)
            {
                _windAudioSource.volume = targetVolume;

                if (targetVolume <= 0f && _isWindPlaying)
                {
                    _windAudioSource.Stop();
                    _isWindPlaying = false;
                }
            }

            _windFadeCoroutine = null;
        }

        private void CleanupThrusterAudio()
        {
            StopThrusterSound();
            _thrusterAudioSource = null;
        }

        #endregion
    }
}
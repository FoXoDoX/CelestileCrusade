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
        private const float VOLUME_FADE_TIME = 0.15f;
        private const float THRUSTER_FADE_TIME = 0.1f;

        // Настройки стерео-панорамирования
        private const float SIDE_THRUSTER_PAN = 0.7f;      // Насколько сильно смещается звук в сторону (0-1)
        private const float SIDE_THRUSTER_VOLUME = 0.8f;   // Громкость бокового двигателя
        private const float UP_THRUSTER_VOLUME = 0.6f;     // Громкость верхнего двигателя
        private const float MAX_COMBINED_VOLUME = 1f;      // Максимальная комбинированная громкость

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
        [SerializeField] private AudioClip _energyBookPickupClip;
        [SerializeField] private AudioClip _energyBookParticleClip;
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

        [Header("Thruster Stereo Settings")]
        [SerializeField, Range(0f, 1f)]
        private float _sideThrusterPan = SIDE_THRUSTER_PAN;

        [SerializeField, Range(0f, 1f)]
        private float _sideThrusterVolume = SIDE_THRUSTER_VOLUME;

        [SerializeField, Range(0f, 1f)]
        private float _upThrusterVolume = UP_THRUSTER_VOLUME;

        #endregion

        #region Private Fields

        private Coroutine _windFadeCoroutine;
        private Coroutine _volumeFadeCoroutine;
        private Coroutine _thrusterFadeCoroutine;
        private Coroutine _thrusterUpdateCoroutine;

        private bool _isWindPlaying;
        private bool _isPaused;
        private bool _isMixerReady;

        private AudioSource _thrusterAudioSource;
        private bool _isThrusterPlaying;

        private float _targetMixerVolume;

        // Состояние активных двигателей
        private bool _isLeftThrusterActive;
        private bool _isRightThrusterActive;
        private bool _isUpThrusterActive;

        // Целевые значения для плавного перехода
        private float _targetThrusterVolume;
        private float _targetThrusterPan;

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

        #region Private Methods — Initialization

        private IEnumerator InitializeMixerDelayed()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(MIXER_INIT_DELAY);

            _isMixerReady = true;
            _targetMixerVolume = GameData.SoundVolume;

            ApplyVolumeToMixerImmediate(_targetMixerVolume);

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

        #region Public Methods — Volume Control

        public void SetSoundVolume(float normalizedVolume)
        {
            float clampedVolume = Mathf.Clamp01(normalizedVolume);

            GameData.SetSoundVolume(clampedVolume);

            if (_isMixerReady)
            {
                ApplyVolumeToMixerSmooth(clampedVolume);
            }

            OnSoundVolumeChanged?.Invoke();

            Debug.Log($"[SoundManager] Volume: {GetSoundVolumePercent()}%");
        }

        public float GetSoundVolumeNormalized() => GameData.SoundVolume;

        public int GetSoundVolumePercent() => Mathf.RoundToInt(GameData.SoundVolume * 100f);

        #endregion

        #region Public Methods — Sound Playback

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

        #region Public Methods — Thruster Audio

        public void RegisterThrusterAudioSource(AudioSource thrusterSource)
        {
            if (thrusterSource == null)
            {
                Debug.LogWarning($"[{nameof(SoundManager)}] Thruster AudioSource is null!");
                return;
            }

            _thrusterAudioSource = thrusterSource;

            _thrusterAudioSource.clip = _thrusterClip;
            _thrusterAudioSource.loop = true;
            _thrusterAudioSource.playOnAwake = false;
            _thrusterAudioSource.volume = 0f;
            _thrusterAudioSource.panStereo = 0f;
            _thrusterAudioSource.outputAudioMixerGroup = _soundMixerGroup;
            _thrusterAudioSource.Play();

            _isThrusterPlaying = false;
            ResetThrusterState();

            Debug.Log($"[{nameof(SoundManager)}] Thruster AudioSource registered");
        }

        public void UnregisterThrusterAudioSource()
        {
            StopThrusterFadeCoroutine();

            if (_thrusterAudioSource != null)
            {
                _thrusterAudioSource.volume = 0f;
                _thrusterAudioSource.Stop();
            }

            _thrusterAudioSource = null;
            _isThrusterPlaying = false;
            ResetThrusterState();

            Debug.Log($"[{nameof(SoundManager)}] Thruster AudioSource unregistered");
        }

        #endregion

        #region Private Methods — Thruster Stereo Control

        private void ResetThrusterState()
        {
            _isLeftThrusterActive = false;
            _isRightThrusterActive = false;
            _isUpThrusterActive = false;
            _targetThrusterVolume = 0f;
            _targetThrusterPan = 0f;
        }

        private void UpdateThrusterAudio()
        {
            if (_thrusterAudioSource == null) return;
            if (_isPaused) return;

            bool anyThrusterActive = _isLeftThrusterActive || _isRightThrusterActive || _isUpThrusterActive;

            if (!anyThrusterActive)
            {
                // Все двигатели выключены
                if (_isThrusterPlaying)
                {
                    _isThrusterPlaying = false;
                    StopThrusterFadeCoroutine();
                    _thrusterFadeCoroutine = StartCoroutine(FadeThrusterVolumeAndPan(0f, 0f));
                }
                return;
            }

            // Вычисляем целевую громкость и панораму
            CalculateThrusterParameters(out float targetVolume, out float targetPan);

            _targetThrusterVolume = targetVolume;
            _targetThrusterPan = targetPan;

            if (!_isThrusterPlaying)
            {
                _isThrusterPlaying = true;
            }

            // Плавно переходим к новым значениям
            StopThrusterFadeCoroutine();
            _thrusterFadeCoroutine = StartCoroutine(FadeThrusterVolumeAndPan(targetVolume, targetPan));
        }

        private void CalculateThrusterParameters(out float volume, out float pan)
        {
            float totalVolume = 0f;
            float panContribution = 0f;
            float panWeight = 0f;

            // Левый двигатель: звук смещается ВПРАВО (положительный pan)
            if (_isLeftThrusterActive)
            {
                totalVolume += _sideThrusterVolume;
                panContribution += _sideThrusterPan * _sideThrusterVolume;
                panWeight += _sideThrusterVolume;
            }

            // Правый двигатель: звук смещается ВЛЕВО (отрицательный pan)
            if (_isRightThrusterActive)
            {
                totalVolume += _sideThrusterVolume;
                panContribution += -_sideThrusterPan * _sideThrusterVolume;
                panWeight += _sideThrusterVolume;
            }

            // Верхний двигатель: звук по центру
            if (_isUpThrusterActive)
            {
                totalVolume += _upThrusterVolume;
                // Pan contribution = 0 для центра
                panWeight += _upThrusterVolume;
            }

            // Нормализуем громкость
            volume = Mathf.Min(totalVolume, MAX_COMBINED_VOLUME);

            // Вычисляем взвешенную панораму
            if (panWeight > 0f)
            {
                pan = Mathf.Clamp(panContribution / panWeight, -1f, 1f);
            }
            else
            {
                pan = 0f;
            }
        }

        private IEnumerator UpdateThrusterAtEndOfFrame()
        {
            // Ждём конца кадра, чтобы все события Force успели сработать
            yield return new WaitForEndOfFrame();

            UpdateThrusterAudio();
            _thrusterUpdateCoroutine = null;
        }

        #endregion

        #region Public Methods — Pause Control

        public void PauseAllSounds()
        {
            if (_isPaused) return;

            _isPaused = true;
            _pausedAudioStates.Clear();

            StopWindFadeCoroutine();
            StopThrusterFadeCoroutine();

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

        #region Private Methods — Pause Helpers

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

        #region Private Methods — Volume

        private void ApplyVolumeToMixerImmediate(float normalizedVolume)
        {
            if (_audioMixer == null) return;

            float decibels = NormalizedToDecibels(normalizedVolume);
            _audioMixer.SetFloat(MIXER_SOUND_PARAM, decibels);
        }

        private void ApplyVolumeToMixerSmooth(float targetNormalizedVolume)
        {
            if (_audioMixer == null) return;

            _targetMixerVolume = targetNormalizedVolume;

            StopVolumeFadeCoroutine();
            _volumeFadeCoroutine = StartCoroutine(FadeMixerVolume(targetNormalizedVolume));
        }

        private float NormalizedToDecibels(float normalizedVolume)
        {
            if (normalizedVolume <= 0.0001f)
            {
                return MIN_DECIBELS;
            }

            return Mathf.Log10(normalizedVolume) * 20f;
        }

        private float DecibelsToNormalized(float decibels)
        {
            if (decibels <= MIN_DECIBELS + 0.1f)
            {
                return 0f;
            }

            return Mathf.Pow(10f, decibels / 20f);
        }

        #endregion

        #region Private Methods — Event Subscriptions

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

            em.AddHandler(GameEvents.LanderBeforeForce, OnLanderBeforeForce);
            em.AddHandler(GameEvents.LanderUpForce, OnLanderUpForce);
            em.AddHandler(GameEvents.LanderLeftForce, OnLanderLeftForce);
            em.AddHandler(GameEvents.LanderRightForce, OnLanderRightForce);

            em.AddHandler<PickupEventData>(GameEvents.EnergyBookPickup, OnEnergyBookPickup);
            em.AddHandler<PickupEventData>(GameEvents.EnergyBookParticle, OnEnergyBookParticle);
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

            em.RemoveHandler(GameEvents.LanderBeforeForce, OnLanderBeforeForce);
            em.RemoveHandler(GameEvents.LanderUpForce, OnLanderUpForce);
            em.RemoveHandler(GameEvents.LanderLeftForce, OnLanderLeftForce);
            em.RemoveHandler(GameEvents.LanderRightForce, OnLanderRightForce);

            em.RemoveHandler<PickupEventData>(GameEvents.EnergyBookPickup, OnEnergyBookPickup);
            em.RemoveHandler<PickupEventData>(GameEvents.EnergyBookParticle, OnEnergyBookParticle);
            em.RemoveHandler<PickupEventData>(GameEvents.CoinPickup, OnCoinPickup);
            em.RemoveHandler<KeyDeliveredData>(GameEvents.KeyDelivered, OnKeyDelivered);
            em.RemoveHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
        }

        #endregion

        #region Private Methods — Event Handlers

        private void OnGamePaused() => PauseAllSounds();
        private void OnGameUnpaused() => ResumeAllSounds();

        private void OnEnergyBookPickup(PickupEventData data) => PlaySound(_energyBookPickupClip);
        private void OnEnergyBookParticle(PickupEventData data) => PlaySound(_energyBookParticleClip);
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
            // Сбрасываем все двигатели
            ResetThrusterState();
            UpdateThrusterAudio();

            var clip = data.LandingType == Lander.LandingType.Success
                ? _landingSuccessClip
                : _crashClip;

            PlaySound(clip);
        }

        private void OnLanderBeforeForce()
        {
            // Сбрасываем состояние перед новым кадром
            _isLeftThrusterActive = false;
            _isRightThrusterActive = false;
            _isUpThrusterActive = false;

            // Запускаем отложенную проверку в конце кадра
            if (_thrusterUpdateCoroutine != null)
            {
                StopCoroutine(_thrusterUpdateCoroutine);
            }
            _thrusterUpdateCoroutine = StartCoroutine(UpdateThrusterAtEndOfFrame());
        }

        private void OnLanderUpForce()
        {
            _isUpThrusterActive = true;
        }

        private void OnLanderLeftForce()
        {
            _isLeftThrusterActive = true;
        }

        private void OnLanderRightForce()
        {
            _isRightThrusterActive = true;
        }

        #endregion

        #region Private Methods — Audio Helpers

        private void StopWindFadeCoroutine()
        {
            if (_windFadeCoroutine != null)
            {
                StopCoroutine(_windFadeCoroutine);
                _windFadeCoroutine = null;
            }
        }

        private void StopVolumeFadeCoroutine()
        {
            if (_volumeFadeCoroutine != null)
            {
                StopCoroutine(_volumeFadeCoroutine);
                _volumeFadeCoroutine = null;
            }
        }

        private void StopThrusterFadeCoroutine()
        {
            if (_thrusterFadeCoroutine != null)
            {
                StopCoroutine(_thrusterFadeCoroutine);
                _thrusterFadeCoroutine = null;
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

        private IEnumerator FadeMixerVolume(float targetNormalizedVolume)
        {
            if (_audioMixer == null) yield break;

            float currentDecibels;
            if (!_audioMixer.GetFloat(MIXER_SOUND_PARAM, out currentDecibels))
            {
                currentDecibels = MIN_DECIBELS;
            }

            float startNormalized = DecibelsToNormalized(currentDecibels);
            float elapsedTime = 0f;

            while (elapsedTime < VOLUME_FADE_TIME)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / VOLUME_FADE_TIME);

                float currentNormalized = Mathf.Lerp(startNormalized, targetNormalizedVolume, t);
                float decibels = NormalizedToDecibels(currentNormalized);

                _audioMixer.SetFloat(MIXER_SOUND_PARAM, decibels);

                yield return null;
            }

            float finalDecibels = NormalizedToDecibels(targetNormalizedVolume);
            _audioMixer.SetFloat(MIXER_SOUND_PARAM, finalDecibels);

            _volumeFadeCoroutine = null;
        }

        private IEnumerator FadeThrusterVolumeAndPan(float targetVolume, float targetPan)
        {
            if (_thrusterAudioSource == null) yield break;

            float startVolume = _thrusterAudioSource.volume;
            float startPan = _thrusterAudioSource.panStereo;
            float elapsedTime = 0f;

            while (elapsedTime < THRUSTER_FADE_TIME)
            {
                if (_thrusterAudioSource == null) yield break;
                if (_isPaused) yield break;

                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / THRUSTER_FADE_TIME);

                _thrusterAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
                _thrusterAudioSource.panStereo = Mathf.Lerp(startPan, targetPan, t);

                yield return null;
            }

            if (_thrusterAudioSource != null)
            {
                _thrusterAudioSource.volume = targetVolume;
                _thrusterAudioSource.panStereo = targetPan;
            }

            _thrusterFadeCoroutine = null;
        }

        private void CleanupThrusterAudio()
        {
            StopThrusterFadeCoroutine();

            if (_thrusterUpdateCoroutine != null)
            {
                StopCoroutine(_thrusterUpdateCoroutine);
                _thrusterUpdateCoroutine = null;
            }

            if (_thrusterAudioSource != null)
            {
                _thrusterAudioSource.volume = 0f;
                _thrusterAudioSource.Stop();
            }

            _thrusterAudioSource = null;
            _isThrusterPlaying = false;
            ResetThrusterState();
        }

        #endregion
    }
}
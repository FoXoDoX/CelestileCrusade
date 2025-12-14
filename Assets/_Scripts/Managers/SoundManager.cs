using My.Scripts.Core.Data;
using My.Scripts.Core.Utility;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using System.Collections;
using UnityEngine;

namespace My.Scripts.Managers
{
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        #region Constants

        private const int SOUND_VOLUME_MAX = 10;
        private const int SOUND_VOLUME_DEFAULT = 6;
        private const float WIND_FADE_TIME = 0.5f;

        #endregion

        #region Static Fields

        private static int _soundVolume = SOUND_VOLUME_DEFAULT;

        #endregion

        #region Serialized Fields

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _soundEffectsSource;

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

        #endregion

        #region Private Fields

        private AudioSource _progressBarSource;
        private AudioSource _windSource;
        private Coroutine _windFadeCoroutine;
        private bool _isWindPlaying;
        private float _windTargetVolume;

        #endregion

        #region Events

        public event System.Action OnSoundVolumeChanged;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            InitializeAudioSources();
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
        }

        #endregion

        #region Public Methods Ч Volume Control

        public void ChangeSoundVolume()
        {
            _soundVolume = (_soundVolume + 1) % (SOUND_VOLUME_MAX + 1);

            UpdateActiveSourcesVolume();
            PlaySound(_coinPickupClip);

            OnSoundVolumeChanged?.Invoke();
        }

        public int GetSoundVolume() => _soundVolume;

        public float GetSoundVolumeNormalized() => (float)_soundVolume / SOUND_VOLUME_MAX;

        #endregion

        #region Public Methods Ч Sound Playback

        public void PlaySound(AudioClip clip)
        {
            if (clip == null || _soundEffectsSource == null) return;

            // PlayOneShot позвол€ет воспроизводить несколько звуков одновременно
            _soundEffectsSource.PlayOneShot(clip, GetSoundVolumeNormalized());
        }

        public void PlayProgressBarSound()
        {
            if (_progressBarClip == null || _progressBarSource == null) return;

            // Ќе останавливаем, если уже играет тот же звук
            if (_progressBarSource.isPlaying && _progressBarSource.clip == _progressBarClip)
            {
                return;
            }

            _progressBarSource.clip = _progressBarClip;
            _progressBarSource.volume = GetSoundVolumeNormalized();
            _progressBarSource.Play();
        }

        public void StopProgressBarSound()
        {
            if (_progressBarSource != null && _progressBarSource.isPlaying)
            {
                _progressBarSource.Stop();
            }
        }

        public void PlayWindSound()
        {
            if (_windClip == null || _windSource == null) return;

            _windTargetVolume = GetSoundVolumeNormalized();

            StopWindFadeCoroutine();

            if (!_isWindPlaying)
            {
                _windSource.volume = 0f;
                _windSource.Play();
                _isWindPlaying = true;
            }

            _windFadeCoroutine = StartCoroutine(FadeWindVolume(_windTargetVolume));
        }

        public void StopWindSound()
        {
            if (_windSource == null) return;

            _windTargetVolume = 0f;
            StopWindFadeCoroutine();
            _windFadeCoroutine = StartCoroutine(FadeWindVolume(0f));
        }

        public void RefreshSubscriptions()
        {
            UnsubscribeFromEvents();
            SubscribeToEvents();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void InitializeAudioSources()
        {
            _progressBarSource = gameObject.AddComponent<AudioSource>();
            _progressBarSource.playOnAwake = false;
            _progressBarSource.loop = false;

            _windSource = gameObject.AddComponent<AudioSource>();
            _windSource.playOnAwake = false;
            _windSource.loop = true;
            _windSource.clip = _windClip;
            _windSource.volume = 0f;
        }

        #endregion

        #region Private Methods Ч Event Subscriptions

        private void SubscribeToEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler<PickupEventData>(GameEvents.FuelPickup, OnFuelPickup);
            em.AddHandler<PickupEventData>(GameEvents.CoinPickup, OnCoinPickup);

            em.AddHandler(GameEvents.TurretShoot, OnTurretShoot);

            em.AddHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.AddHandler(GameEvents.CrateCracked, OnCrateCracked);
            em.AddHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);
            em.AddHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.AddHandler(GameEvents.KeyPickup, OnKeyPickup);
            em.AddHandler(GameEvents.KeyDelivered, OnKeyDelivered);

            em.AddHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
        }

        private void UnsubscribeFromEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler<PickupEventData>(GameEvents.FuelPickup, OnFuelPickup);
            em.RemoveHandler<PickupEventData>(GameEvents.CoinPickup, OnCoinPickup);

            em.RemoveHandler(GameEvents.TurretShoot, OnTurretShoot);

            em.RemoveHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.RemoveHandler(GameEvents.CrateCracked, OnCrateCracked);
            em.RemoveHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);
            em.RemoveHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.RemoveHandler(GameEvents.KeyPickup, OnKeyPickup);
            em.RemoveHandler(GameEvents.KeyDelivered, OnKeyDelivered);

            em.RemoveHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnFuelPickup(PickupEventData data) => PlaySound(_fuelPickupClip);
        private void OnCoinPickup(PickupEventData data) => PlaySound(_coinPickupClip);

        private void OnTurretShoot() => PlaySound(_turretShootClip);

        private void OnCrateDrop() => PlaySound(_crateDeliveredClip);
        private void OnCrateCracked() => PlaySound(_crateCrackedClip);
        private void OnCrateDestroyed() => PlaySound(_crateDestroyedClip);
        private void OnRopeWithCrateSpawned() => RefreshSubscriptions();
        private void OnKeyPickup() => PlaySound(_keyPickupClip);
        private void OnKeyDelivered() => PlaySound(_keyDeliveredClip);

        private void OnLanderLanded(LanderLandedData data)
        {
            var clip = data.LandingType == Lander.LandingType.Success
                ? _landingSuccessClip
                : _crashClip;

            PlaySound(clip);
        }

        #endregion

        #region Private Methods Ч Audio Helpers

        private void UpdateActiveSourcesVolume()
        {
            float volume = GetSoundVolumeNormalized();

            if (_progressBarSource != null)
            {
                _progressBarSource.volume = volume;
            }

            if (_windSource != null && _isWindPlaying)
            {
                _windTargetVolume = volume;
                StopWindFadeCoroutine();
                _windFadeCoroutine = StartCoroutine(FadeWindVolume(_windTargetVolume));
            }
        }

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
            if (_windSource == null) yield break;

            float startVolume = _windSource.volume;
            float elapsedTime = 0f;

            while (elapsedTime < WIND_FADE_TIME)
            {
                if (_windSource == null) yield break;

                elapsedTime += Time.deltaTime;
                float t = elapsedTime / WIND_FADE_TIME;
                _windSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }

            if (_windSource != null)
            {
                _windSource.volume = targetVolume;

                if (targetVolume <= 0f && _isWindPlaying)
                {
                    _windSource.Stop();
                    _isWindPlaying = false;
                }
            }

            _windFadeCoroutine = null;
        }

        #endregion
    }
}
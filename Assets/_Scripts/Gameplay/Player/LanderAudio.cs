using My.Scripts.EventBus;
using My.Scripts.Managers;
using UnityEngine;

namespace My.Scripts.Gameplay.Player
{
    public class LanderAudio : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioSource _thrusterAudioSource;

        #endregion

        #region Private Fields

        private bool _isThrusting;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeAudioSource();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            StopThrusting();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void InitializeAudioSource()
        {
            if (_thrusterAudioSource == null)
            {
                Debug.LogError($"[{nameof(LanderAudio)}] Thruster AudioSource is missing!", this);
                return;
            }

            _thrusterAudioSource.volume = 0f;
            _thrusterAudioSource.loop = true;
            _thrusterAudioSource.Play();
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.LanderBeforeForce, OnBeforeForce);
            em.AddHandler(GameEvents.LanderUpForce, OnUpForce);
            em.AddHandler(GameEvents.LanderLeftForce, OnLeftForce);
            em.AddHandler(GameEvents.LanderRightForce, OnRightForce);

            // ѕодписка на изменение громкости
            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.OnSoundVolumeChanged += OnSoundVolumeChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var em = EventManager.Instance;
            if (em != null)
            {
                em.RemoveHandler(GameEvents.LanderBeforeForce, OnBeforeForce);
                em.RemoveHandler(GameEvents.LanderUpForce, OnUpForce);
                em.RemoveHandler(GameEvents.LanderLeftForce, OnLeftForce);
                em.RemoveHandler(GameEvents.LanderRightForce, OnRightForce);
            }

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.OnSoundVolumeChanged -= OnSoundVolumeChanged;
            }
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnBeforeForce()
        {
            StopThrusting();
        }

        private void OnUpForce()
        {
            StartThrusting();
        }

        private void OnLeftForce()
        {
            StartThrusting();
        }

        private void OnRightForce()
        {
            StartThrusting();
        }

        private void OnSoundVolumeChanged()
        {
            if (_isThrusting)
            {
                UpdateThrusterVolume();
            }
        }

        #endregion

        #region Private Methods Ч Audio Control

        private void StartThrusting()
        {
            if (_isThrusting) return;
            if (_thrusterAudioSource == null) return;

            _isThrusting = true;
            UpdateThrusterVolume();
        }

        private void StopThrusting()
        {
            if (!_isThrusting) return;
            if (_thrusterAudioSource == null) return;

            _isThrusting = false;
            _thrusterAudioSource.volume = 0f;
        }

        private void UpdateThrusterVolume()
        {
            if (_thrusterAudioSource == null) return;

            float volume = SoundManager.HasInstance
                ? SoundManager.Instance.GetSoundVolumeNormalized()
                : 1f;

            _thrusterAudioSource.volume = volume;
        }

        #endregion
    }
}
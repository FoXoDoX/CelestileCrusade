using My.Scripts.EventBus;
using UnityEngine;

namespace My.Scripts.Environment.Hazards
{
    [RequireComponent(typeof(AudioSource))]
    public class AsteroidAudio : MonoBehaviour
    {
        private AudioSource _audioSource;
        private bool _isSubscribed;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed) return;
            if (!EventManager.HasInstance) return;

            EventManager.Instance.AddHandler(GameEvents.GamePaused, OnGamePaused);
            EventManager.Instance.AddHandler(GameEvents.GameUnpaused, OnGameUnpaused);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;
            if (!EventManager.HasInstance) return;

            EventManager.Instance.RemoveHandler(GameEvents.GamePaused, OnGamePaused);
            EventManager.Instance.RemoveHandler(GameEvents.GameUnpaused, OnGameUnpaused);
            _isSubscribed = false;
        }

        private void OnGamePaused()
        {
            if (_audioSource != null && _audioSource.isPlaying)
                _audioSource.Pause();
        }

        private void OnGameUnpaused()
        {
            if (_audioSource != null && !_audioSource.isPlaying)
                _audioSource.UnPause();
        }
    }
}
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

        #region Unity Lifecycle

        private void Start()
        {
            RegisterWithSoundManager();
        }

        private void OnDestroy()
        {
            UnregisterFromSoundManager();
        }

        #endregion

        #region Private Methods

        private void RegisterWithSoundManager()
        {
            if (_thrusterAudioSource == null)
            {
                Debug.LogError($"[{nameof(LanderAudio)}] Thruster AudioSource is missing!", this);
                return;
            }

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.RegisterThrusterAudioSource(_thrusterAudioSource);
            }
        }

        private void UnregisterFromSoundManager()
        {
            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.UnregisterThrusterAudioSource();
            }
        }

        #endregion
    }
}
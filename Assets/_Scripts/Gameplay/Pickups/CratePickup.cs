using My.Scripts.Gameplay.Player;
using My.Scripts.Managers;
using System.Collections;
using UnityEngine;

namespace My.Scripts.Gameplay.Pickups
{
    public class CratePickup : MonoBehaviour
    {
        #region Constants

        private const float PICKUP_TIME = 3f;

        #endregion

        #region Serialized Fields

        [Header("Settings")]
        [SerializeField] private float _pickupDuration = PICKUP_TIME;

        #endregion

        #region Private Fields

        private float _pickupProgress;
        private bool _isLanderInside;
        private bool _isSoundPlaying;
        private Coroutine _pickupCoroutine;

        #endregion

        #region Properties

        public float PickupProgress => _pickupProgress;
        public bool IsPickupInProgress => _isLanderInside && _pickupProgress > 0f;

        #endregion

        #region Unity Lifecycle

        private void OnDisable()
        {
            CancelPickup();
        }

        private void OnDestroy()
        {
            StopProgressBarSound();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryStartPickup(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            TryExitPickup(other);
        }

        #endregion

        #region Public Methods

        public float GetPickupProgress() => _pickupProgress;

        public void DestroySelf()
        {
            CancelPickup();
            Destroy(gameObject);
        }

        #endregion

        #region Private Methods Ч Pickup Logic

        private void TryStartPickup(Collider2D other)
        {
            if (!other.TryGetComponent(out Lander lander)) return;
            if (lander.HasCrate) return;

            _isLanderInside = true;
            StartPickupProcess();
        }

        private void TryExitPickup(Collider2D other)
        {
            if (!other.TryGetComponent(out Lander _)) return;

            CancelPickup();
        }

        private void StartPickupProcess()
        {
            // ќстанавливаем предыдущий процесс если был
            StopPickupCoroutine();

            _pickupCoroutine = StartCoroutine(PickupRoutine());
        }

        private void CancelPickup()
        {
            _isLanderInside = false;
            _pickupProgress = 0f;

            StopPickupCoroutine();
            StopProgressBarSound();
        }

        private void StopPickupCoroutine()
        {
            if (_pickupCoroutine != null)
            {
                StopCoroutine(_pickupCoroutine);
                _pickupCoroutine = null;
            }
        }

        private IEnumerator PickupRoutine()
        {
            float timer = 0f;

            StartProgressBarSound();

            while (timer < _pickupDuration)
            {
                // ѕровер€ем услови€ продолжени€
                if (!CanContinuePickup())
                {
                    CancelPickup();
                    yield break;
                }

                timer += Time.deltaTime;
                _pickupProgress = timer / _pickupDuration;

                yield return null;
            }

            // ‘инальна€ проверка перед завершением
            if (CanCompletePickup())
            {
                CompletePickup();
            }
            else
            {
                CancelPickup();
            }
        }

        private bool CanContinuePickup()
        {
            if (!_isLanderInside) return false;
            if (!Lander.HasInstance) return false;
            if (Lander.Instance.HasCrate) return false;

            return true;
        }

        private bool CanCompletePickup()
        {
            if (_pickupProgress < 1f) return false;
            if (!Lander.HasInstance) return false;
            if (Lander.Instance.HasCrate) return false;

            return true;
        }

        private void CompletePickup()
        {
            StopProgressBarSound();

            if (Lander.HasInstance)
            {
                Lander.Instance.HandleCratePickup();
            }

            DestroySelf();
        }

        #endregion

        #region Private Methods Ч Sound

        private void StartProgressBarSound()
        {
            if (_isSoundPlaying) return;

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.PlayProgressBarSound();
                _isSoundPlaying = true;
            }
        }

        private void StopProgressBarSound()
        {
            if (!_isSoundPlaying) return;

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.StopProgressBarSound();
                _isSoundPlaying = false;
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_pickupDuration <= 0f)
            {
                _pickupDuration = PICKUP_TIME;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // ¬изуализаци€ радиуса подбора если есть CircleCollider2D
            if (TryGetComponent(out CircleCollider2D circle))
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(
                    transform.position + (Vector3)circle.offset,
                    circle.radius
                );
            }
        }
#endif

        #endregion
    }
}
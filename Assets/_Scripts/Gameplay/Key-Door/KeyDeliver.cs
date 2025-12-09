using My.Scripts.Gameplay.Player;
using My.Scripts.Managers;
using System.Collections;
using UnityEngine;

namespace My.Scripts.Gameplay.KeyDoor
{
    public class KeyDeliver : MonoBehaviour
    {
        #region Constants

        private const float DELIVER_TIME = 3f;

        #endregion

        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField] private Key.KeyType _requiredKeyType;

        [Header("Settings")]
        [SerializeField] private float _deliverDuration = DELIVER_TIME;

        #endregion

        #region Private Fields

        private float _deliverProgress;
        private bool _isLanderInside;
        private bool _isSoundPlaying;
        private Coroutine _deliverCoroutine;
        private KeyHolder _cachedKeyHolder;

        #endregion

        #region Properties

        public float DeliverProgress => _deliverProgress;
        public Key.KeyType RequiredKeyType => _requiredKeyType;
        public bool IsDeliveryInProgress => _isLanderInside && _deliverProgress > 0f;

        #endregion

        #region Unity Lifecycle

        private void OnDisable()
        {
            CancelDelivery();
        }

        private void OnDestroy()
        {
            StopProgressBarSound();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryStartDelivery(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            TryExitDelivery(other);
        }

        #endregion

        #region Public Methods

        public Key.KeyType GetKeyType() => _requiredKeyType;

        public float GetDeliverProgress() => _deliverProgress;

        public void DestroySelf()
        {
            CancelDelivery();
            Destroy(gameObject);
        }

        #endregion

        #region Private Methods Ч Delivery Logic

        private void TryStartDelivery(Collider2D other)
        {
            if (!other.TryGetComponent(out Lander lander)) return;
            if (!lander.TryGetComponent(out KeyHolder keyHolder)) return;
            if (!keyHolder.ContainsKey(_requiredKeyType)) return;

            _cachedKeyHolder = keyHolder;
            _isLanderInside = true;
            StartDeliveryProcess();
        }

        private void TryExitDelivery(Collider2D other)
        {
            if (!other.TryGetComponent(out Lander _)) return;

            CancelDelivery();
        }

        private void StartDeliveryProcess()
        {
            StopDeliveryCoroutine();
            _deliverCoroutine = StartCoroutine(DeliveryRoutine());
        }

        private void CancelDelivery()
        {
            _isLanderInside = false;
            _deliverProgress = 0f;
            _cachedKeyHolder = null;

            StopDeliveryCoroutine();
            StopProgressBarSound();
        }

        private void StopDeliveryCoroutine()
        {
            if (_deliverCoroutine != null)
            {
                StopCoroutine(_deliverCoroutine);
                _deliverCoroutine = null;
            }
        }

        private IEnumerator DeliveryRoutine()
        {
            float timer = 0f;

            StartProgressBarSound();

            while (timer < _deliverDuration)
            {
                if (!CanContinueDelivery())
                {
                    CancelDelivery();
                    yield break;
                }

                timer += Time.deltaTime;
                _deliverProgress = timer / _deliverDuration;

                yield return null;
            }

            if (CanCompleteDelivery())
            {
                CompleteDelivery();
            }
            else
            {
                CancelDelivery();
            }
        }

        private bool CanContinueDelivery()
        {
            if (!_isLanderInside) return false;
            if (_cachedKeyHolder == null) return false;
            if (!_cachedKeyHolder.ContainsKey(_requiredKeyType)) return false;

            return true;
        }

        private bool CanCompleteDelivery()
        {
            if (_deliverProgress < 1f) return false;
            if (!Lander.HasInstance) return false;

            return true;
        }

        private void CompleteDelivery()
        {
            StopProgressBarSound();

            if (Lander.HasInstance)
            {
                Lander.Instance.HandleKeyDeliver(_requiredKeyType);
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
            if (_deliverDuration <= 0f)
            {
                _deliverDuration = DELIVER_TIME;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // ¬изуализаци€ зоны доставки
            Gizmos.color = GetGizmoColor();

            if (TryGetComponent(out CircleCollider2D circle))
            {
                Gizmos.DrawWireSphere(
                    transform.position + (Vector3)circle.offset,
                    circle.radius
                );
            }
            else if (TryGetComponent(out BoxCollider2D box))
            {
                Gizmos.DrawWireCube(
                    transform.position + (Vector3)box.offset,
                    box.size
                );
            }
        }

        private Color GetGizmoColor()
        {
            return _requiredKeyType switch
            {
                Key.KeyType.Red => new Color(1f, 0f, 0f, 0.5f),
                Key.KeyType.Green => new Color(0f, 1f, 0f, 0.5f),
                Key.KeyType.Blue => new Color(0f, 0f, 1f, 0.5f),
                _ => new Color(1f, 1f, 0f, 0.5f)
            };
        }
#endif

        #endregion
    }
}
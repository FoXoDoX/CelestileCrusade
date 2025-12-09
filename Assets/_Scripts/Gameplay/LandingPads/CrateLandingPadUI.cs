using My.Scripts.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.Gameplay.LandingPads
{
    public class CrateLandingPadUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI Elements")]
        [SerializeField] private Image _progressFillImage;

        [Header("References")]
        [SerializeField] private CrateLandingPad _crateLandingPad;

        #endregion

        #region Private Fields

        private bool _isRopeSpawned;
        private bool _hasValidReferences;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            ResetProgress();
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
            UpdateProgressBar();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void ValidateReferences()
        {
            _hasValidReferences = _progressFillImage != null && _crateLandingPad != null;

            if (!_hasValidReferences)
            {
                Debug.LogWarning($"[{nameof(CrateLandingPadUI)}] Missing references!", this);
            }
        }

        private void ResetProgress()
        {
            if (_progressFillImage != null)
            {
                _progressFillImage.fillAmount = 0f;
            }
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.CratePickup, OnCratePickup);
            em.AddHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.AddHandler(GameEvents.RopeWithCrateDestroyed, OnRopeWithCrateDestroyed);
            em.AddHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.AddHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);
        }

        private void UnsubscribeFromEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler(GameEvents.CratePickup, OnCratePickup);
            em.RemoveHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.RemoveHandler(GameEvents.RopeWithCrateDestroyed, OnRopeWithCrateDestroyed);
            em.RemoveHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.RemoveHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnCratePickup()
        {
            _isRopeSpawned = true;
        }

        private void OnRopeWithCrateSpawned()
        {
            _isRopeSpawned = true;
        }

        private void OnRopeWithCrateDestroyed()
        {
            _isRopeSpawned = false;
            ResetProgress();
        }

        private void OnCrateDrop()
        {
            _isRopeSpawned = false;
            ResetProgress();
        }

        private void OnCrateDestroyed()
        {
            _isRopeSpawned = false;
            ResetProgress();
        }

        #endregion

        #region Private Methods Ч UI Updates

        private void UpdateProgressBar()
        {
            if (!_hasValidReferences) return;

            if (_isRopeSpawned)
            {
                _progressFillImage.fillAmount = _crateLandingPad.CurrentProgress;
            }
            else
            {
                _progressFillImage.fillAmount = 0f;
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            // јвтоматически находим CrateLandingPad на родителе
            if (_crateLandingPad == null)
            {
                _crateLandingPad = GetComponentInParent<CrateLandingPad>();
            }
        }
#endif

        #endregion
    }
}
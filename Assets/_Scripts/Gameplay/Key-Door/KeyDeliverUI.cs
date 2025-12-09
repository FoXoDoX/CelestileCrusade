using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.Gameplay.KeyDoor
{
    public class KeyDeliverUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI Elements")]
        [SerializeField] private Image _progressFillImage;

        [Header("Visual Indicators")]
        [SerializeField] private SpriteRenderer _keySpriteRenderer;
        [SerializeField] private SpriteRenderer _crossSpriteRenderer;

        [Header("Configuration")]
        [SerializeField] private Key.KeyType _requiredKeyType;

        [Header("References")]
        [SerializeField] private KeyDeliver _keyDeliver;

        #endregion

        #region Private Fields

        private KeyHolder _keyHolder;
        private bool _hasRequiredKey;
        private bool _isDelivered;
        private bool _isSubscribed;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            CacheKeyHolder();
            UpdateVisuals();
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

        #region Private Methods — Initialization

        private void CacheKeyHolder()
        {
            if (_keyHolder != null) return;

            if (Lander.HasInstance)
            {
                _keyHolder = Lander.Instance.GetComponent<KeyHolder>();
            }
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToEvents()
        {
            if (_isSubscribed) return;

            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler<KeyDeliveredData>(GameEvents.KeyDelivered, OnKeyDelivered);
            em.AddHandler(GameEvents.KeyPickup, OnKeyPickup);

            _isSubscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribed) return;

            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler<KeyDeliveredData>(GameEvents.KeyDelivered, OnKeyDelivered);
            em.RemoveHandler(GameEvents.KeyPickup, OnKeyPickup);

            _isSubscribed = false;
        }

        #endregion

        #region Private Methods — Event Handlers

        private void OnKeyDelivered(KeyDeliveredData data)
        {
            if (data.KeyType == _requiredKeyType)
            {
                _isDelivered = true;
            }

            UpdateVisuals();
        }

        private void OnKeyPickup()
        {
            CacheKeyHolder();
            UpdateVisuals();
        }

        #endregion

        #region Private Methods — UI Updates

        private void UpdateProgressBar()
        {
            if (_progressFillImage == null || _keyDeliver == null) return;

            _progressFillImage.fillAmount = _keyDeliver.GetDeliverProgress();
        }

        private void UpdateVisuals()
        {
            CacheKeyHolder();

            _hasRequiredKey = _keyHolder != null && _keyHolder.ContainsKey(_requiredKeyType);

            bool showKey = _hasRequiredKey && !_isDelivered;
            bool showCross = !_hasRequiredKey && !_isDelivered;

            if (_keySpriteRenderer != null)
            {
                _keySpriteRenderer.gameObject.SetActive(showKey);
            }

            if (_crossSpriteRenderer != null)
            {
                _crossSpriteRenderer.gameObject.SetActive(showCross);
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_keyDeliver == null)
            {
                _keyDeliver = GetComponentInParent<KeyDeliver>();
            }
        }
#endif

        #endregion
    }
}
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
        [SerializeField] private SpriteRenderer _crossSpriteRenderer;

        [Header("References")]
        // KeyDeliver подхватываетс€ автоматически Ч мен€ть не нужно
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

        #region Private Methods Ч Initialization

        private void CacheKeyHolder()
        {
            if (_keyHolder != null) return;

            if (Lander.HasInstance)
            {
                _keyHolder = Lander.Instance.GetComponent<KeyHolder>();
            }
        }

        #endregion

        #region Private Methods Ч Event Subscription

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

        #region Private Methods Ч Event Handlers

        private void OnKeyDelivered(KeyDeliveredData data)
        {
            // “ип берЄм напр€мую из KeyDeliver Ч единый источник истины
            if (_keyDeliver != null && data.KeyType == _keyDeliver.RequiredKeyType)
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

        #region Private Methods Ч UI Updates

        private void UpdateProgressBar()
        {
            if (_progressFillImage == null || _keyDeliver == null) return;

            _progressFillImage.fillAmount = _keyDeliver.GetDeliverProgress();
        }

        private void UpdateVisuals()
        {
            if (_keyDeliver == null) return;

            CacheKeyHolder();

            _hasRequiredKey = _keyHolder != null
                && _keyHolder.ContainsKey(_keyDeliver.RequiredKeyType);

            // —прайт ключа управл€етс€ через KeyDeliver Ч здесь только крестик
            bool showCross = !_hasRequiredKey && !_isDelivered;

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
            // јвтоматически подхватываем KeyDeliver с того же объекта
            if (_keyDeliver == null)
            {
                _keyDeliver = GetComponent<KeyDeliver>();
            }
        }
#endif

        #endregion
    }
}
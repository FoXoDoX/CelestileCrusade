using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace My.Scripts.Gameplay.KeyDoor
{
    public class KeyHolder : MonoBehaviour
    {
        #region Constants

        private const float SCALE_UP_DURATION = 0.1f;
        private const float SCALE_DOWN_DURATION = 0.2f;
        private const float FLIGHT_DURATION = 1f;
        private const float FADE_OUT_DURATION = 0.3f;
        private const float SCALE_UP_VALUE = 1.5f;

        #endregion

        #region Serialized Fields

        [Header("UI Destinations")]
        [SerializeField] private RectTransform _redKeyDestination;
        [SerializeField] private RectTransform _greenKeyDestination;
        [SerializeField] private RectTransform _blueKeyDestination;

        [Header("UI Setup")]
        [SerializeField] private Canvas _targetCanvas;
        [SerializeField] private Image _keyUIPrefab;

        #endregion

        #region Private Fields

        private readonly List<Key.KeyType> _collectedKeys = new();
        private readonly Dictionary<Key.KeyType, RectTransform> _keyDestinations = new();
        private readonly Dictionary<Key.KeyType, Image> _keyUIElements = new();
        private readonly Dictionary<Key.KeyType, Sequence> _activeAnimations = new();

        private Camera _mainCamera;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeDestinations();
            CacheCamera();
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
            KillAllAnimations();
            UnsubscribeFromEvents();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryPickupKey(other);
        }

        #endregion

        #region Public Methods

        public bool ContainsKey(Key.KeyType keyType)
        {
            return _collectedKeys.Contains(keyType);
        }

        public int GetCollectedKeysCount()
        {
            return _collectedKeys.Count;
        }

        #endregion

        #region Private Methods — Initialization

        private void InitializeDestinations()
        {
            _keyDestinations[Key.KeyType.Red] = _redKeyDestination;
            _keyDestinations[Key.KeyType.Green] = _greenKeyDestination;
            _keyDestinations[Key.KeyType.Blue] = _blueKeyDestination;
        }

        private void CacheCamera()
        {
            _mainCamera = Camera.main;

            if (_mainCamera == null)
            {
                Debug.LogWarning($"[{nameof(KeyHolder)}] Main camera not found!");
            }
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToEvents()
        {
            EventManager.Instance?.AddHandler<KeyDeliveredData>(
                GameEvents.KeyDelivered,
                OnKeyDelivered
            );
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Instance?.RemoveHandler<KeyDeliveredData>(
                GameEvents.KeyDelivered,
                OnKeyDelivered
            );
        }

        #endregion

        #region Private Methods — Event Handlers

        private void OnKeyDelivered(KeyDeliveredData data)
        {
            RemoveKey(data.KeyType);
            RemoveKeyUI(data.KeyType);
        }

        #endregion

        #region Private Methods — Key Management

        private void TryPickupKey(Collider2D other)
        {
            if (!other.TryGetComponent(out Key key)) return;

            // Сохраняем данные до уничтожения
            KeyAnimationData animData = new()
            {
                KeyType = key.Type,
                Sprite = GetKeySprite(key),
                WorldPosition = key.transform.position
            };

            // Добавляем ключ в коллекцию
            AddKey(animData.KeyType);

            // Уничтожаем физический ключ
            Destroy(key.gameObject);

            // Запускаем анимацию
            AnimateKeyToUI(animData);
        }

        private void AddKey(Key.KeyType keyType)
        {
            _collectedKeys.Add(keyType);

            EventManager.Instance?.Broadcast(GameEvents.KeyPickup);
        }

        private void RemoveKey(Key.KeyType keyType)
        {
            _collectedKeys.Remove(keyType);
        }

        private Sprite GetKeySprite(Key key)
        {
            var spriteRenderer = key.GetComponentInChildren<SpriteRenderer>();
            return spriteRenderer != null ? spriteRenderer.sprite : null;
        }

        #endregion

        #region Private Methods — UI Animation

        private void AnimateKeyToUI(KeyAnimationData animData)
        {
            if (!ValidateAnimationRequirements()) return;

            // Очищаем предыдущий UI элемент этого типа
            CleanupExistingKeyUI(animData.KeyType);

            // Создаём новый UI элемент
            Image keyImage = CreateKeyUIElement(animData);
            if (keyImage == null) return;

            RectTransform keyRect = keyImage.rectTransform;
            RectTransform destination = GetDestinationForKeyType(animData.KeyType);

            // Настраиваем начальную позицию
            SetupInitialPosition(keyRect, animData.WorldPosition);

            // Запускаем анимацию
            PlayFlightAnimation(keyImage, keyRect, destination, animData.KeyType);
        }

        private bool ValidateAnimationRequirements()
        {
            if (_targetCanvas == null)
            {
                Debug.LogError($"[{nameof(KeyHolder)}] Target canvas is missing!");
                return false;
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                {
                    Debug.LogError($"[{nameof(KeyHolder)}] Main camera not found!");
                    return false;
                }
            }

            return true;
        }

        private void CleanupExistingKeyUI(Key.KeyType keyType)
        {
            // Останавливаем активную анимацию
            if (_activeAnimations.TryGetValue(keyType, out var existingSequence))
            {
                existingSequence?.Kill();
                _activeAnimations.Remove(keyType);
            }

            // Уничтожаем UI элемент
            if (_keyUIElements.TryGetValue(keyType, out var existingImage))
            {
                if (existingImage != null)
                {
                    Destroy(existingImage.gameObject);
                }
                _keyUIElements.Remove(keyType);
            }
        }

        private Image CreateKeyUIElement(KeyAnimationData animData)
        {
            if (animData.Sprite == null)
            {
                Debug.LogWarning($"[{nameof(KeyHolder)}] Key sprite is null for {animData.KeyType}");
                return null;
            }

            Image keyImage = Instantiate(_keyUIPrefab, _targetCanvas.transform);
            keyImage.sprite = animData.Sprite;

            return keyImage;
        }

        private void SetupInitialPosition(RectTransform keyRect, Vector3 worldPosition)
        {
            Vector3 screenPoint = _mainCamera.WorldToScreenPoint(worldPosition);

            keyRect.position = screenPoint;
            keyRect.localRotation = Quaternion.identity;
            keyRect.localScale = Vector3.one;
        }

        private void PlayFlightAnimation(Image keyImage, RectTransform keyRect, RectTransform destination, Key.KeyType keyType)
        {
            Sequence sequence = DOTween.Sequence();

            // Эффект "рывка" — быстрое увеличение
            sequence.Append(
                keyRect.DOScale(SCALE_UP_VALUE, SCALE_UP_DURATION)
                    .SetEase(Ease.OutBack)
            );

            // Возврат к нормальному размеру
            sequence.Append(
                keyRect.DOScale(1f, SCALE_DOWN_DURATION)
            );

            // Перемещение к цели
            sequence.Join(
                keyRect.DOMove(destination.position, FLIGHT_DURATION)
                    .SetEase(Ease.OutQuad)
            );

            // Обработка завершения
            sequence.OnComplete(() => OnFlightAnimationComplete(keyImage, keyRect, destination, keyType));

            // Безопасная очистка при уничтожении
            sequence.OnKill(() => OnAnimationKilled(keyType));

            // Сохраняем ссылку на анимацию
            _activeAnimations[keyType] = sequence;

            sequence.Play();
        }

        private void OnFlightAnimationComplete(Image keyImage, RectTransform keyRect, RectTransform destination, Key.KeyType keyType)
        {
            if (keyImage == null) return;

            // Фиксируем финальную позицию
            keyRect.position = destination.position;

            // Сохраняем UI элемент
            _keyUIElements[keyType] = keyImage;

            // Удаляем из активных анимаций
            _activeAnimations.Remove(keyType);
        }

        private void OnAnimationKilled(Key.KeyType keyType)
        {
            _activeAnimations.Remove(keyType);
        }

        private void RemoveKeyUI(Key.KeyType keyType)
        {
            // Останавливаем активную анимацию
            if (_activeAnimations.TryGetValue(keyType, out var sequence))
            {
                sequence?.Kill();
                _activeAnimations.Remove(keyType);
            }

            // Анимируем исчезновение
            if (_keyUIElements.TryGetValue(keyType, out var keyImage) && keyImage != null)
            {
                keyImage.DOFade(0f, FADE_OUT_DURATION)
                    .OnComplete(() =>
                    {
                        if (keyImage != null)
                        {
                            Destroy(keyImage.gameObject);
                        }
                    });

                _keyUIElements.Remove(keyType);
            }
        }

        private RectTransform GetDestinationForKeyType(Key.KeyType keyType)
        {
            if (_keyDestinations.TryGetValue(keyType, out var destination) && destination != null)
            {
                return destination;
            }

            // Fallback
            return _redKeyDestination;
        }

        private void KillAllAnimations()
        {
            foreach (var sequence in _activeAnimations.Values)
            {
                sequence?.Kill();
            }
            _activeAnimations.Clear();
        }

        #endregion

        #region Nested Types

        private struct KeyAnimationData
        {
            public Key.KeyType KeyType;
            public Sprite Sprite;
            public Vector3 WorldPosition;
        }

        #endregion
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static Lander;
using System;

public class KeyHolder : MonoBehaviour
{
    [SerializeField] private RectTransform _redKeyDestination;
    [SerializeField] private RectTransform _greenKeyDestination;
    [SerializeField] private RectTransform _blueKeyDestination;
    [SerializeField] private Canvas _targetCanvas;
    [SerializeField] private Image _keyUIPrefab;

    public static event EventHandler OnKeyPickup;

    private List<Key.KeyType> keyList;
    private Dictionary<Key.KeyType, RectTransform> _keyDestinations;
    private Dictionary<Key.KeyType, Image> _keyUIElements;
    private Camera _mainCamera;

    private void Awake()
    {
        DOTween.Init();

        keyList = new List<Key.KeyType>();

        _keyDestinations = new Dictionary<Key.KeyType, RectTransform>
        {
            { Key.KeyType.Red, _redKeyDestination },
            { Key.KeyType.Green, _greenKeyDestination },
            { Key.KeyType.Blue, _blueKeyDestination }
        };

        _keyUIElements = new Dictionary<Key.KeyType, Image>();

        // Кэшируем камеру
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        // Используем Instance с проверкой на null
        if (Lander.Instance != null)
        {
            Lander.Instance.OnKeyDeliver += Lander_OnKeyDeliver;
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от событий при уничтожении
        if (Lander.Instance != null)
        {
            Lander.Instance.OnKeyDeliver -= Lander_OnKeyDeliver;
        }
    }

    private void Lander_OnKeyDeliver(object sender, OnKeyDeliverEventArgs e)
    {
        RemoveKey(e.DeliveredKeyType);
        RemoveKeyUI(e.DeliveredKeyType);
    }

    public void AddKey(Key.KeyType keyType)
    {
        keyList.Add(keyType);
        OnKeyPickup?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveKey(Key.KeyType keyType)
    {
        keyList.Remove(keyType);
    }

    public bool ContainsKey(Key.KeyType keyType)
    {
        return keyList.Contains(keyType);
    }

    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        Key key = collider2D.GetComponent<Key>();
        if (key != null)
        {
            AddKey(key.GetKeyType());

            // Получаем необходимые данные ДО уничтожения ключа
            KeyData keyData = new KeyData
            {
                KeyType = key.GetKeyType(),
                Sprite = GetKeySprite(key),
                WorldPosition = key.transform.position
            };

            // Сразу уничтожаем физический ключ
            Destroy(key.gameObject);

            // Анимируем с использованием сохраненных данных
            AnimateKeyToUI(keyData);
        }
    }

    private Sprite GetKeySprite(Key key)
    {
        SpriteRenderer spriteRenderer = key.GetComponentInChildren<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private void AnimateKeyToUI(KeyData keyData)
    {
        if (_targetCanvas == null || _mainCamera == null)
        {
            Debug.LogError("Key animation failed: missing required components");
            return;
        }

        // Удаляем старый UI элемент, если он есть
        if (_keyUIElements.ContainsKey(keyData.KeyType))
        {
            Destroy(_keyUIElements[keyData.KeyType].gameObject);
            _keyUIElements.Remove(keyData.KeyType);
        }

        // Создаем новый UI элемент
        Image keyImage = Instantiate(_keyUIPrefab, _targetCanvas.transform);

        // Устанавливаем спрайт
        if (keyData.Sprite != null)
        {
            keyImage.sprite = keyData.Sprite;
        }
        else
        {
            Destroy(keyImage.gameObject);
            return;
        }

        // Получаем RectTransform
        RectTransform keyRect = keyImage.GetComponent<RectTransform>();
        if (keyRect == null)
        {
            Destroy(keyImage.gameObject);
            return;
        }

        // Устанавливаем начальную позицию
        Vector3 screenPoint = _mainCamera.WorldToScreenPoint(keyData.WorldPosition);

        // Сброс позиции и вращения
        keyRect.position = screenPoint;
        keyRect.localRotation = Quaternion.identity;
        keyRect.localScale = Vector3.one;

        // Получаем целевую позицию
        RectTransform targetDestination = GetDestinationForKeyType(keyData.KeyType);
        if (targetDestination == null)
        {
            targetDestination = _redKeyDestination;
        }

        // Создаем анимацию
        Sequence flightSequence = DOTween.Sequence();

        // 1. Быстрое увеличение (эффект "рывка")
        flightSequence.Append(keyRect.DOScale(1.5f, 0.1f).SetEase(Ease.OutBack));

        // 2. Возврат к нормальному размеру с одновременным перемещением
        flightSequence.Append(keyRect.DOScale(1f, 0.2f));
        flightSequence.Join(keyRect.DOMove(targetDestination.position, 1f)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() => {
                // Если UI элемент был уничтожен, прерываем анимацию
                if (!keyImage)
                {
                    flightSequence.Kill();
                }
            })
            .OnComplete(() => {
                // Сохраняем UI элемент в словаре
                if (keyImage != null)
                {
                    _keyUIElements[keyData.KeyType] = keyImage;
                }

                // Фиксируем позицию UI элемента на случай изменения разрешения
                if (keyRect != null && targetDestination != null)
                {
                    keyRect.position = targetDestination.position;
                }
            }));

        // Запускаем анимацию
        flightSequence.Play();
    }

    private void RemoveKeyUI(Key.KeyType keyType)
    {
        if (_keyUIElements.ContainsKey(keyType) && _keyUIElements[keyType] != null)
        {
            // Плавно исчезаем перед удалением
            Image keyImage = _keyUIElements[keyType];
            keyImage.DOFade(0f, 0.3f)
                .OnComplete(() => {
                    if (keyImage != null && keyImage.gameObject != null)
                    {
                        Destroy(keyImage.gameObject);
                    }
                });

            _keyUIElements.Remove(keyType);
        }
    }

    private RectTransform GetDestinationForKeyType(Key.KeyType keyType)
    {
        if (_keyDestinations.TryGetValue(keyType, out RectTransform destination))
        {
            return destination;
        }

        return _redKeyDestination;
    }

    // Структура для хранения данных ключа перед анимацией
    private struct KeyData
    {
        public Key.KeyType KeyType;
        public Sprite Sprite;
        public Vector3 WorldPosition;
    }
}
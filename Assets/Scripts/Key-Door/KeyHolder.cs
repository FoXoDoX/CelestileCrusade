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

    private void Awake()
    {
        keyList = new List<Key.KeyType>();

        _keyDestinations = new Dictionary<Key.KeyType, RectTransform>
        {
            { Key.KeyType.Red, _redKeyDestination },
            { Key.KeyType.Green, _greenKeyDestination },
            { Key.KeyType.Blue, _blueKeyDestination }
        };

        _keyUIElements = new Dictionary<Key.KeyType, Image>();
    }

    private void Start()
    {
        Instance.OnKeyDeliver += Lander_OnKeyDeliver;
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
            AnimateKeyToUI(key);
            Destroy(key.gameObject);
        }
    }

    private void AnimateKeyToUI(Key worldKey)
    {
        Key.KeyType keyType = worldKey.GetKeyType();
        if (_keyUIElements.ContainsKey(keyType))
        {
            Destroy(_keyUIElements[keyType].gameObject);
            _keyUIElements.Remove(keyType);
        }

        Image keyImage = Instantiate(_keyUIPrefab, _targetCanvas.transform);

        SpriteRenderer spriteRenderer = worldKey.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            keyImage.sprite = spriteRenderer.sprite;
        }
        else
        {
            return;
        }

        RectTransform keyRect = keyImage.GetComponent<RectTransform>();
        Vector3 worldPosition = worldKey.transform.position;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPosition);
        keyRect.position = screenPoint;

        RectTransform targetDestination = GetDestinationForKeyType(keyType);

        // Создаем эффект "скорости" - быстрое мерцание и изменение размера
        Sequence flightSequence = DOTween.Sequence();

        // 1. Быстрое увеличение (эффект "рывка")
        flightSequence.Append(keyRect.DOScale(1.5f, 0.1f));

        // 2. Возврат к нормальному размеру с одновременным перемещением
        flightSequence.Append(keyRect.DOScale(1f, 0.2f));
        flightSequence.Join(keyRect.DOMove(targetDestination.position, 1f).SetEase(Ease.OutQuad));

        _keyUIElements[keyType] = keyImage;

        // Уничтожаем физический ключ
        Destroy(worldKey.gameObject);
    }

    private void RemoveKeyUI(Key.KeyType keyType)
    {
        if (_keyUIElements.ContainsKey(keyType))
        {
            Destroy(_keyUIElements[keyType].gameObject);
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
}
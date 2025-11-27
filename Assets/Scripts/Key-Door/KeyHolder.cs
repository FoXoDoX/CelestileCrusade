using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static Lander;

public class KeyHolder : MonoBehaviour
{
    [SerializeField] private RectTransform _redKeyDestination;
    [SerializeField] private RectTransform _greenKeyDestination;
    [SerializeField] private RectTransform _blueKeyDestination;
    [SerializeField] private Canvas _targetCanvas;
    [SerializeField] private Image _keyUIPrefab;

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
        Debug.Log("Added key: " + keyType);
        keyList.Add(keyType);
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
            Debug.LogError("SpriteRenderer not found in children of Key object");
            return;
        }

        RectTransform keyRect = keyImage.GetComponent<RectTransform>();

        Vector3 worldPosition = worldKey.transform.position;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPosition);

        keyRect.position = screenPoint;

        RectTransform targetDestination = GetDestinationForKeyType(keyType);

        keyRect.DOMove(targetDestination.position, 1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    Debug.Log($"Ключ {keyType} занял своё место в UI!");
                });

        _keyUIElements[keyType] = keyImage;
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

        Debug.LogWarning($"Destination for key type {keyType} not found, using red key destination");
        return _redKeyDestination;
    }
}
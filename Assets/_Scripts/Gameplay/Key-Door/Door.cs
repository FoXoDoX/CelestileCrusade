using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using UnityEngine;
using DG.Tweening;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace My.Scripts.Gameplay.KeyDoor
{
    public class Door : MonoBehaviour
    {
        #region Constants

        private const float DOOR_ROTATION_ANGLE = 90f;
        private const float DOOR_OPEN_DURATION = 2f;

        #endregion

        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField] private Key.KeyType _requiredKeyType;

        [Header("Door Parts")]
        [SerializeField] private Transform _leftDoor;
        [SerializeField] private Transform _rightDoor;

        [Header("Sprites")]
        [SerializeField] private Sprite _goldenDoorSprite;
        [SerializeField] private Sprite _silverDoorSprite;
        [SerializeField] private Sprite _bronzeDoorSprite;

        [Header("Animation Settings")]
        [SerializeField] private float _openDuration = DOOR_OPEN_DURATION;
        [SerializeField] private Ease _openEase = Ease.OutQuad;

        #endregion

        #region Private Fields

        private bool _isOpen;
        private Tween _leftDoorTween;
        private Tween _rightDoorTween;

        #endregion

        #region Properties

        public bool IsOpen => _isOpen;
        public Key.KeyType RequiredKeyType => _requiredKeyType;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ApplySprites();
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
            KillTweens();
            UnsubscribeFromEvents();
        }

        #endregion

        #region Private Methods — Sprites

        private void ApplySprites()
        {
            Sprite sprite = _requiredKeyType switch
            {
                Key.KeyType.Golden => _goldenDoorSprite,
                Key.KeyType.Silver => _silverDoorSprite,
                Key.KeyType.Bronze => _bronzeDoorSprite,
                _ => null
            };

            if (sprite == null) return;

            ApplySpriteToChild(_leftDoor, sprite);
            ApplySpriteToChild(_rightDoor, sprite);
        }

        private void ApplySpriteToChild(Transform doorPart, Sprite sprite)
        {
            if (doorPart == null) return;

            var spriteRenderer = doorPart.GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer == null) return;

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToEvents()
        {
            EventManager.Instance?.AddHandler<KeyDeliveredData>(GameEvents.KeyDelivered, OnKeyDelivered);
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Instance?.RemoveHandler<KeyDeliveredData>(GameEvents.KeyDelivered, OnKeyDelivered);
        }

        #endregion

        #region Private Methods — Event Handlers

        private void OnKeyDelivered(KeyDeliveredData data)
        {
            if (data.KeyType == _requiredKeyType)
            {
                OpenDoor();
            }
        }

        #endregion

        #region Private Methods — Door Animation

        private void OpenDoor()
        {
            if (_isOpen) return;
            _isOpen = true;

            KillTweens();

            if (_leftDoor != null)
            {
                _leftDoorTween = _leftDoor
                    .DORotate(new Vector3(0, 0, -DOOR_ROTATION_ANGLE), _openDuration, RotateMode.LocalAxisAdd)
                    .SetEase(_openEase);
            }

            if (_rightDoor != null)
            {
                _rightDoorTween = _rightDoor
                    .DORotate(new Vector3(0, 0, DOOR_ROTATION_ANGLE), _openDuration, RotateMode.LocalAxisAdd)
                    .SetEase(_openEase);
            }
        }

        private void KillTweens()
        {
            _leftDoorTween?.Kill();
            _rightDoorTween?.Kill();

            _leftDoorTween = null;
            _rightDoorTween = null;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_openDuration <= 0)
            {
                _openDuration = DOOR_OPEN_DURATION;
            }

            EditorApplication.delayCall += () =>
            {
                if (this == null) return;

                ApplySprites();
            };
        }
#endif

        #endregion
    }
}
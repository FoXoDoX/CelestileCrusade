using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace My.Scripts.Gameplay.KeyDoor
{
    public class Key : MonoBehaviour
    {
        #region Enums

        public enum KeyType
        {
            Golden,
            Silver,
            Bronze
        }

        #endregion

        #region Serialized Fields

        [Header("Settings")]
        [SerializeField] private KeyType _keyType;

        [Header("Sprites")]
        [SerializeField] private Sprite _goldenKeySprite;
        [SerializeField] private Sprite _silverKeySprite;
        [SerializeField] private Sprite _bronzeKeySprite;

        [Header("References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        #endregion

        #region Properties

        public KeyType Type => _keyType;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ApplySprite();
        }

        #endregion

        #region Private Methods

        private void ApplySprite()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_spriteRenderer == null) return;

            _spriteRenderer.sprite = _keyType switch
            {
                KeyType.Golden => _goldenKeySprite,
                KeyType.Silver => _silverKeySprite,
                KeyType.Bronze => _bronzeKeySprite,
                _ => _spriteRenderer.sprite
            };

            _spriteRenderer.color = Color.white;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Откладываем вызов до безопасного момента
            EditorApplication.delayCall += () =>
            {
                // Объект мог быть уничтожен к этому моменту
                if (this == null) return;

                ApplySprite();
            };
        }
#endif

        #endregion
    }
}
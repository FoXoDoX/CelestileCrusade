using UnityEngine;

namespace My.Scripts.Gameplay.KeyDoor
{
    /// <summary>
    /// Компонент ключа, который можно подобрать и доставить.
    /// </summary>
    public class Key : MonoBehaviour
    {
        #region Enums

        public enum KeyType
        {
            Red,
            Green,
            Blue
        }

        #endregion

        #region Serialized Fields

        [Header("Settings")]
        [SerializeField] private KeyType _keyType;

        #endregion

        #region Properties

        public KeyType Type => _keyType;

        #endregion
    }
}
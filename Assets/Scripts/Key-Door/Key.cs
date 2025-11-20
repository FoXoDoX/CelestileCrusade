using UnityEngine;

public class Key : MonoBehaviour
{
    [SerializeField] private KeyType keyType;

    public enum KeyType
    {
        Red,
        Green,
        Blue
    }

    public KeyType GetKeyType()
    {
        return keyType;
    }
}

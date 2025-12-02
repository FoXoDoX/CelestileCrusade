using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSoundInitializer : MonoBehaviour
{
    private void Start()
    {
        // Переподписываем SoundManager на события при старте уровня
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.RefreshSubscriptions();
        }
    }
}
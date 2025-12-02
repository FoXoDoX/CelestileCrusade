using UnityEngine;
using System.Collections;

public class KeyDeliver : MonoBehaviour
{
    [SerializeField] private Key.KeyType keyType;

    public Key.KeyType GetKeyType()
    {
        return keyType;
    }

    private float deliverProgress = 0f;
    private bool isPlayerInside = false;
    private bool isSoundPlaying = false;
    private Coroutine deliverCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Lander lander = other.GetComponent<Lander>();
        if (lander != null)
        {
            KeyHolder keyHolder = lander.GetComponent<KeyHolder>();
            if (keyHolder != null && keyHolder.ContainsKey(keyType))
            {
                isPlayerInside = true;
                deliverCoroutine = StartCoroutine(UpdateDeliverProgress());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Lander lander = other.GetComponent<Lander>();
        if (lander != null)
        {
            isPlayerInside = false;
            deliverProgress = 0f;

            StopProgressBarSound();

            if (deliverCoroutine != null)
            {
                StopCoroutine(deliverCoroutine);
                deliverCoroutine = null;
            }
        }
    }

    private IEnumerator UpdateDeliverProgress()
    {
        float deliverTime = 3f;
        float timer = 0f;

        StartProgressBarSound();

        while (timer < deliverTime && isPlayerInside)
        {
            timer += Time.deltaTime;
            deliverProgress = timer / deliverTime;
            yield return null;
        }

        StopProgressBarSound();

        if (deliverProgress >= 1f)
        {
            Lander.Instance.HandleKeyDeliver(keyType);
            DestroySelf();
        }
        else
        {
            deliverProgress = 0f;
        }
    }

    private void StartProgressBarSound()
    {
        if (!isSoundPlaying && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayProgressBarSound();
            isSoundPlaying = true;
        }
    }

    private void StopProgressBarSound()
    {
        if (isSoundPlaying && SoundManager.Instance != null)
        {
            SoundManager.Instance.StopProgressBarSound();
            isSoundPlaying = false;
        }
    }

    public void DestroySelf()
    {
        StopProgressBarSound();
        Destroy(gameObject);
    }

    public float GetDeliverProgress()
    {
        return deliverProgress;
    }
}
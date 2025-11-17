using UnityEngine;
using System.Collections;

public class CratePickup : MonoBehaviour
{
    private float pickupProgress = 0f;
    private bool isPlayerInside = false;
    private Coroutine pickupCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Lander lander = other.GetComponent<Lander>();
        if (lander != null && !lander.HasCrate)
        {
            isPlayerInside = true;
            pickupCoroutine = StartCoroutine(UpdatePickupProgress());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Lander lander = other.GetComponent<Lander>();
        if (lander != null)
        {
            isPlayerInside = false;
            pickupProgress = 0f;
            if (pickupCoroutine != null)
            {
                StopCoroutine(pickupCoroutine);
                pickupCoroutine = null;
            }
        }
    }

    private IEnumerator UpdatePickupProgress()
    {
        float pickupTime = 3f;
        float timer = 0f;

        while (timer < pickupTime && isPlayerInside && !Lander.Instance.HasCrate)
        {
            timer += Time.deltaTime;
            pickupProgress = timer / pickupTime;
            yield return null;
        }

        if (pickupProgress >= 1f && !Lander.Instance.HasCrate)
        {
            Lander.Instance.HandleCratePickup();
            DestroySelf();
        }
        else
        {
            pickupProgress = 0f;
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    public float GetPickupProgress()
    {
        return pickupProgress;
    }
}
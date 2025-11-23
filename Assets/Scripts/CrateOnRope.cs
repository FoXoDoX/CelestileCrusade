using System;
using System.Collections;
using UnityEngine;

public class CrateOnRope : MonoBehaviour
{
    public System.Action<Collider2D> OnCrateCollider;

    public static CrateOnRope Instance { get; private set; }

    public event EventHandler OnCoinPickup;
    public event EventHandler OnFuelPickup;
    public event EventHandler OnCrateDrop;

    private float timerForCrateDrop = 0f;
    private float delayForCrateDrop = 3f;
    private int CrateHealth = 5;
    private bool isInCrateLandingAreaCollider = false;

    private CrateLandingArea currentLandingArea;

    private Coroutine crateDropCoroutine;
    
    private void Awake()
    {
        Instance = this;
    }

    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.gameObject.TryGetComponent(out FuelPickup fuelPickup))
        {
            float addFuelAmountAfterFuelPickup = 15f;
            Lander.Instance.SetFuel(addFuelAmountAfterFuelPickup);
            OnFuelPickup?.Invoke(this, EventArgs.Empty);
            fuelPickup.DestroySelf();
            return;
        }

        if (collider2D.gameObject.TryGetComponent(out CoinPickup coinPickup))
        {
            OnCoinPickup?.Invoke(this, EventArgs.Empty);
            coinPickup.DestroySelf();
            return;
        }

        if (collider2D.gameObject.TryGetComponent(out CrateLandingArea crateLandingArea))
        {
            CrateLandingPad landingPad = crateLandingArea.GetLandingPad();
            if (landingPad == null || !landingPad.CanAcceptCrates)
                return;

            currentLandingArea = crateLandingArea;
            isInCrateLandingAreaCollider = true;
            if (crateDropCoroutine != null)
            {
                StopCoroutine(crateDropCoroutine);
            }
            crateDropCoroutine = StartCoroutine(DropCrateAfterDelay());
            return;
        }
    }

    private void OnTriggerExit2D(Collider2D collider2D)
    {
        if (collider2D.gameObject.TryGetComponent(out CrateLandingArea crateLandingArea))
        {
            if (currentLandingArea == crateLandingArea)
            {
                Debug.Log("Drop canceled");
                isInCrateLandingAreaCollider = false;
                CrateLandingPad landingPad = currentLandingArea?.GetLandingPad();
                if (landingPad != null)
                {
                    landingPad.ResetDeliveryProgress();
                }
                currentLandingArea = null;
                timerForCrateDrop = 0f;
                if (crateDropCoroutine != null)
                {
                    StopCoroutine(crateDropCoroutine);
                    crateDropCoroutine = null;
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (!collision2D.collider.gameObject.TryGetComponent(out CrateLandingPad crateLandingPad))
        {
            Debug.Log(CrateHealth);
            CrateHealth--;
        }
        if (CrateHealth <= 0)
        {
            OnCrateCollider?.Invoke(collision2D.collider);
        }
    }

    private IEnumerator DropCrateAfterDelay()
    {
        Debug.Log("Drop started");

        CrateLandingArea landingArea = currentLandingArea;
        CrateLandingPad landingPad = landingArea?.GetLandingPad();

        if (landingPad == null) yield break;

        while (timerForCrateDrop < delayForCrateDrop)
        {
            if (!isInCrateLandingAreaCollider || landingArea == null || !landingPad.CanAcceptCrates)
            {
                yield break;
            }

            timerForCrateDrop += Time.deltaTime;
            float progress = timerForCrateDrop / delayForCrateDrop;
            landingPad.UpdateDeliveryProgress(progress);
            yield return null;
        }

        if (landingArea != null && landingPad != null && landingPad.CanAcceptCrates)
        {
            landingArea.RegisterCrateDelivery();
            landingPad.ResetDeliveryProgress();
        }

        OnCrateDrop?.Invoke(this, EventArgs.Empty);

        crateDropCoroutine = null;
        currentLandingArea = null;
    }

    public float GetTimerForCrateDropNormalized()
    {
        return timerForCrateDrop / delayForCrateDrop;
    }
}
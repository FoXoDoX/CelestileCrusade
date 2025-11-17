using UnityEngine;

public class CrateLandingArea : MonoBehaviour
{
    private CrateLandingPad parentLandingPad;

    private void Start()
    {
        parentLandingPad = GetComponentInParent<CrateLandingPad>();
    }

    public void RegisterCrateDelivery()
    {
        if (parentLandingPad != null)
        {
            parentLandingPad.RegisterCrateDelivery();
        }
    }

    public CrateLandingPad GetLandingPad()
    {
        return parentLandingPad;
    }
}
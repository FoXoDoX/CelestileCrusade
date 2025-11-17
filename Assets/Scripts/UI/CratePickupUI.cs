using UnityEngine;
using UnityEngine.UI;

public class CratePickupUI : MonoBehaviour
{
    [SerializeField] private Image crateFilledImage;
    [SerializeField] private CratePickup cratePickup;

    private void Update()
    {
        if (cratePickup != null)
        {
            crateFilledImage.fillAmount = cratePickup.GetPickupProgress();
        }
    }
}
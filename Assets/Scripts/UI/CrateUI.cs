using UnityEngine;
using UnityEngine.UI;

public class CrateUI : MonoBehaviour
{
    [SerializeField] private Image crateFilledImage;

    private void Update()
    {
        crateFilledImage.fillAmount = Lander.Instance.GetTimerForCratePickupNormalized();
    }
}

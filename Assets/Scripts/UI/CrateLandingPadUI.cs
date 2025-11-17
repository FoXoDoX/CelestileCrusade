using UnityEngine;
using UnityEngine.UI;

public class CrateLandingPadUI : MonoBehaviour
{
    [SerializeField] private Image crateLandingPadFilledImage;
    [SerializeField] private CrateLandingPad crateLandingPad;

    private bool isRopeSpawned = false;

    private void Start()
    {
        crateLandingPadFilledImage.fillAmount = 0f;

        Lander.Instance.OnCratePickup += Lander_OnCratePickup;
    }

    private void Lander_OnCratePickup(object sender, System.EventArgs e)
    {
        isRopeSpawned = true;
    }

    private void Update()
    {
        if (isRopeSpawned && crateLandingPad != null)
        {
            crateLandingPadFilledImage.fillAmount = crateLandingPad.CurrentProgress;
        }
        else
        {
            crateLandingPadFilledImage.fillAmount = 0f;
        }
    }
}
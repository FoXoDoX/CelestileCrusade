using UnityEngine;

public class CrateLandingPad : MonoBehaviour
{
    [SerializeField] private Transform landedCrates;
    [SerializeField] private CrateLandingArea crateLandingArea;
    [SerializeField] private GameObject background;

    private int deliveredCratesCount = 0;
    private const int MAX_CRATES = 5;
    private float currentProgress = 0f;

    public float CurrentProgress => currentProgress;
    public bool CanAcceptCrates => deliveredCratesCount < MAX_CRATES;

    private void Start()
    {
        HideAllCrates();
    }

    private void HideAllCrates()
    {
        if (landedCrates != null)
        {
            foreach (Transform child in landedCrates)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    public void RegisterCrateDelivery()
    {
        if (deliveredCratesCount >= MAX_CRATES) return;

        string crateName = "Crate" + (deliveredCratesCount + 1);
        Transform crate = landedCrates.Find(crateName);

        if (crate != null)
        {
            crate.gameObject.SetActive(true);
            deliveredCratesCount++;

            Debug.Log($"Crate delivered! Total: {deliveredCratesCount}/{MAX_CRATES}");

            if (deliveredCratesCount >= MAX_CRATES)
            {
                DisableLandingArea();
                HideBackground();
            }
        }
        else
        {
            Debug.LogWarning($"Crate with name {crateName} not found!");
        }
    }

    private void DisableLandingArea()
    {
        if (crateLandingArea != null)
        {
            Collider2D collider = crateLandingArea.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            Debug.Log("All crates delivered! Landing area disabled.");
        }
    }

    private void HideBackground()
    {
        if (background != null)
        {
            background.SetActive(false);
        }
    }

    public void UpdateDeliveryProgress(float progress)
    {
        currentProgress = progress;
    }

    public void ResetDeliveryProgress()
    {
        currentProgress = 0f;
    }
}
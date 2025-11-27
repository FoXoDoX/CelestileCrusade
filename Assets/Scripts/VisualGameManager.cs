using UnityEngine;

public class VisualGameManager : MonoBehaviour
{
    public static VisualGameManager Instance { get; private set; }

    [SerializeField] private ScorePopup scorePopupPrefab;
    [SerializeField] private Transform pickupVfxPrefab;

    private Vector3 spawnPositionForPopup;
    private Quaternion rotationForPopup = Quaternion.Euler(0, 0, -20);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Lander.Instance.OnCoinPickup += Lander_OnCoinPickup;
        Lander.Instance.OnFuelPickup += Lander_OnFuelPickup;
    }

    public void RopeWithCrateSpawned()
    {
        CrateOnRope.Instance.OnCoinPickup += CrateOnRope_OnCoinPickup;
        CrateOnRope.Instance.OnFuelPickup += CrateOnRope_OnFuelPickup;
        CrateOnRope.Instance.OnCrateDrop += CrateOnRope_OnCrateDrop;
    }

    private void CrateOnRope_OnCrateDrop(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = Lander.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup).SetText("+" + GameManager.SCORE_PER_CRATE);
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, CrateOnRope.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }

    private void CrateOnRope_OnFuelPickup(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = CrateOnRope.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup).SetText("+FUEL");
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, CrateOnRope.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }

    private void CrateOnRope_OnCoinPickup(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = CrateOnRope.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup).SetText("+" + GameManager.SCORE_PER_COIN);
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, CrateOnRope.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }

    private void Lander_OnFuelPickup(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = Lander.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup).SetText("+FUEL");
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, Lander.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }

    private void Lander_OnCoinPickup(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = Lander.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup).SetText("+" + GameManager.SCORE_PER_COIN);
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, Lander.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }
}

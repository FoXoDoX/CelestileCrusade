using Unity.Cinemachine;
using UnityEngine;

public class VisualGameManager : MonoBehaviour
{
    public static VisualGameManager Instance { get; private set; }

    [SerializeField] private ScorePopup scorePopupPrefab;
    [SerializeField] private Transform pickupVfxPrefab;
    [SerializeField] private Transform confettiVfxPrefab;
    [SerializeField] private CinemachineImpulseSource cinemachineImpulseSourceForPickup;
    [SerializeField] private CinemachineImpulseSource cinemachineImpulseSourceForLanderCrash;

    private Vector3 spawnPositionForPopup;
    private Quaternion rotationForPopup = Quaternion.Euler(0, 0, -20);
    private float pickupImpulsePower = 0.5f;
    private float landerCrashImpulsePower = 50f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Lander.Instance.OnCoinPickup += Lander_OnCoinPickup;
        Lander.Instance.OnFuelPickup += Lander_OnFuelPickup;
        Lander.Instance.OnLanded += Lander_OnLanded;
        KeyHolder.OnKeyPickup += KeyHolder_OnKeyPickup;
    }

    private void Lander_OnLanded(object sender, Lander.OnLandedEventArgs e)
    {
        if (e.landingType != Lander.LandingType.Success)
        {
            cinemachineImpulseSourceForLanderCrash.GenerateImpulse(landerCrashImpulsePower);
        }
        else
        {
            Transform confettiVfxTransform = 
                Instantiate(confettiVfxPrefab, Lander.Instance.transform.position, Quaternion.identity, Lander.Instance.transform);
        }
    }

    public void RopeWithCrateSpawned()
    {
        CrateOnRope.Instance.OnCoinPickup += CrateOnRope_OnCoinPickup;
        CrateOnRope.Instance.OnFuelPickup += CrateOnRope_OnFuelPickup;
        CrateOnRope.Instance.OnCrateDrop += CrateOnRope_OnCrateDrop;
    }

    private void KeyHolder_OnKeyPickup(object sender, System.EventArgs e)
    {
        cinemachineImpulseSourceForPickup.GenerateImpulse(pickupImpulsePower);
    }

    private void CrateOnRope_OnCrateDrop(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = Lander.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup)
            .Setup("+" + GameManager.SCORE_PER_CRATE, Color.gold, Color.black, true);
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, CrateOnRope.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }

    private void CrateOnRope_OnFuelPickup(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = CrateOnRope.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup).Setup("+FUEL");
        cinemachineImpulseSourceForPickup.GenerateImpulse(pickupImpulsePower);
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, CrateOnRope.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }

    private void CrateOnRope_OnCoinPickup(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = CrateOnRope.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup).Setup("+" + GameManager.SCORE_PER_COIN);
        cinemachineImpulseSourceForPickup.GenerateImpulse(pickupImpulsePower);
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, CrateOnRope.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }

    private void Lander_OnFuelPickup(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = Lander.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup).Setup("+FUEL");
        cinemachineImpulseSourceForPickup.GenerateImpulse(pickupImpulsePower);
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, Lander.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }

    private void Lander_OnCoinPickup(object sender, System.EventArgs e)
    {
        spawnPositionForPopup = Lander.Instance.transform.position + new Vector3(1.5f, 2f, 0f);
        Instantiate(scorePopupPrefab, spawnPositionForPopup, rotationForPopup).Setup("+" + GameManager.SCORE_PER_COIN);
        cinemachineImpulseSourceForPickup.GenerateImpulse(pickupImpulsePower);
        Transform pickupVfxTransform = Instantiate(pickupVfxPrefab, Lander.Instance.transform.position, Quaternion.identity);
        Destroy(pickupVfxTransform.gameObject, 1.5f);
    }

    private void OnDestroy()
    {
        if (Lander.Instance != null)
        {
            Lander.Instance.OnCoinPickup -= Lander_OnCoinPickup;
            Lander.Instance.OnFuelPickup -= Lander_OnFuelPickup;
        }

        KeyHolder.OnKeyPickup -= KeyHolder_OnKeyPickup;

        if (CrateOnRope.Instance != null)
        {
            CrateOnRope.Instance.OnCoinPickup -= CrateOnRope_OnCoinPickup;
            CrateOnRope.Instance.OnFuelPickup -= CrateOnRope_OnFuelPickup;
            CrateOnRope.Instance.OnCrateDrop -= CrateOnRope_OnCrateDrop;
        }
    }
}

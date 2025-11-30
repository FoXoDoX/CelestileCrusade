using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VisualGameManager : MonoBehaviour
{
    public static VisualGameManager Instance { get; private set; }

    [SerializeField] private ScorePopup scorePopupPrefab;
    [SerializeField] private Transform pickupVfxPrefab;
    [SerializeField] private Transform confettiVfxPrefab;
    [SerializeField] private CinemachineImpulseSource cinemachineImpulseSourceForPickup;
    [SerializeField] private CinemachineImpulseSource cinemachineImpulseSourceForLanderCrash;
    [SerializeField] private Volume globalVolume;

    private Vector3 spawnPositionForPopup;
    private Quaternion rotationForPopup = Quaternion.Euler(0, 0, -20);
    private float pickupImpulsePower = 0.5f;
    private float landerCrashImpulsePower = 50f;

    private ChromaticAberration chromaticAberration;
    private bool isGameOver = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Lander.Instance.OnCoinPickup += Lander_OnCoinPickup;
        Lander.Instance.OnFuelPickup += Lander_OnFuelPickup;
        Lander.Instance.OnLanded += Lander_OnLanded;
        Lander.Instance.OnStateChanged += Lander_OnStateChanged;
        KeyHolder.OnKeyPickup += KeyHolder_OnKeyPickup;

        globalVolume.profile.TryGet(out chromaticAberration);
    }

    private void Update()
    {
        UpdateChromaticAberration();
    }

    private void Lander_OnStateChanged(object sender, Lander.OnStateChangedEventArgs e)
    {
        if (e.state == Lander.State.GameOver && chromaticAberration != null)
        {
            isGameOver = true;
        }
    }

    private void UpdateChromaticAberration()
    {
        if (isGameOver) 
        {
            chromaticAberration.intensity.value = 0f;
            return; 
        }

        bool isLowFuel = Lander.Instance.GetFuelAmountNormalized() < 0.25f;

        if (isLowFuel)
        {
            chromaticAberration.intensity.value = Mathf.PingPong(Time.time, 0.8f);
        }
        else if (!isLowFuel)
        {
            chromaticAberration.intensity.value = 0f;
        }
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

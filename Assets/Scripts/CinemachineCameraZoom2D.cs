using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraZoom2D : MonoBehaviour
{
    public static CinemachineCameraZoom2D Instance { get; private set; }

    [SerializeField] private CinemachineCamera cinemachineCamera;

    private float targetOrthographicSize = 12f;
    private Lander.State currentLanderState;

    private void Awake()
    {
        Instance = this; 
    }

    private void Start()
    {
        Lander.Instance.OnStateChanged += Lander_OnStateChanged;
    }

    private void Lander_OnStateChanged(object sender, Lander.OnStateChangedEventArgs e)
    {
        currentLanderState = e.state;
    }

    private void Update()
    {
        float zoomSpeed = 2f;
        cinemachineCamera.Lens.OrthographicSize = 
            Mathf.Lerp(cinemachineCamera.Lens.OrthographicSize, targetOrthographicSize, Time.deltaTime * zoomSpeed);
    }

    public void SetTargetOrthographicSize(float targetOrthographicSize)
    {
        this.targetOrthographicSize = targetOrthographicSize;
    }

    public void SetNormalOrthographicSize(float normalOrthographicSize)
    {
        SetTargetOrthographicSize(normalOrthographicSize);
    }
}

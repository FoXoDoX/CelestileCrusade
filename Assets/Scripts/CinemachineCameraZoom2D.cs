using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraZoom2D : MonoBehaviour
{
    private const float NORMAL_ORTHOGRAPHIC_SIZE = 12f;

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

        if (currentLanderState == Lander.State.Normal)
        {
            if (CrateOnRope.Instance != null)
            {
                SetTargetOrthographicSize(18f);
            }
            else
            {
                SetNormalOrthographicSize();
            }
        }
    }

    public void SetTargetOrthographicSize(float targetOrthographicSize)
    {
        this.targetOrthographicSize = targetOrthographicSize;
    }

    public void SetNormalOrthographicSize()
    {
        SetTargetOrthographicSize(NORMAL_ORTHOGRAPHIC_SIZE);
    }
}

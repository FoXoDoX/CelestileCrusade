using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float parallaxFactor = 0.03f;

    private Material backgroundMaterial;
    private Vector3 startTargetPosition;
    private Vector3 startBackgroundPosition;
    private static readonly int ParallaxOffset = Shader.PropertyToID("_ParallaxOffset");

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        backgroundMaterial = renderer.material;

        startTargetPosition = target.position;
        startBackgroundPosition = transform.position;
    }

    void Update()
    {
        if (target == null) return;

        Vector3 baseMovement = target.position + offset;
        transform.position = new Vector3(baseMovement.x, baseMovement.y, transform.position.z);

        Vector3 targetDelta = target.position - startTargetPosition;
        Vector2 parallaxOffset = new Vector2(
            targetDelta.x * parallaxFactor,
            targetDelta.y * parallaxFactor
        );

        if (backgroundMaterial.HasProperty(ParallaxOffset))
        {
            backgroundMaterial.SetVector(ParallaxOffset, parallaxOffset);
        }
    }

    public void ResetPositions()
    {
        startTargetPosition = target.position;
        startBackgroundPosition = transform.position;

        if (backgroundMaterial.HasProperty(ParallaxOffset))
        {
            backgroundMaterial.SetVector(ParallaxOffset, Vector2.zero);
        }
    }
}
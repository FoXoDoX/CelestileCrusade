using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float parallaxFactor = 0.025f;

    private Material backgroundMaterial;
    private Material particleMaterial;
    private Vector3 startTargetPosition;
    private Vector3 startBackgroundPosition;
    private static readonly int ParallaxOffset = Shader.PropertyToID("_ParallaxOffset");

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        backgroundMaterial = renderer.material;

        // Получаем материал системы частиц
        ParticleSystemRenderer particleRenderer = GetComponentInChildren<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleMaterial = particleRenderer.material;
        }

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

        // Применяем к фону
        if (backgroundMaterial.HasProperty(ParallaxOffset))
        {
            backgroundMaterial.SetVector(ParallaxOffset, parallaxOffset);
        }

        // Применяем к частицам
        if (particleMaterial != null && particleMaterial.HasProperty(ParallaxOffset))
        {
            particleMaterial.SetVector(ParallaxOffset, parallaxOffset);
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

        if (particleMaterial != null && particleMaterial.HasProperty(ParallaxOffset))
        {
            particleMaterial.SetVector(ParallaxOffset, Vector2.zero);
        }
    }
}
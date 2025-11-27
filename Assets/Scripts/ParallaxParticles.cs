using UnityEngine;

public class ParallaxParticles : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float parallaxFactor = 0.03f;

    private ParticleSystem particles;
    private Vector3 lastTargetPosition;
    private Vector3 particleStartPosition;

    void Start()
    {
        particles = GetComponent<ParticleSystem>();
        lastTargetPosition = target.position;
        particleStartPosition = transform.position;

        // Настройка Particle System
        var main = particles.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Custom;
        main.customSimulationSpace = transform.parent; // или создайте отдельный anchor
    }

    void Update()
    {
        if (target == null) return;

        // Вычисляем движение для параллакса
        Vector3 targetDelta = target.position - lastTargetPosition;
        Vector3 parallaxMovement = new Vector3(
            targetDelta.x * parallaxFactor,
            targetDelta.y * parallaxFactor,
            0
        );

        // Применяем параллакс к системе частиц
        transform.position += parallaxMovement;

        lastTargetPosition = target.position;
    }
}
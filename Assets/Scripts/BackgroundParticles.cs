using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundParticles : MonoBehaviour
{
    [SerializeField] private List<GameObject> particlePrefabs = new List<GameObject>();
    [SerializeField] private Vector2 spawnRateRange = new Vector2(0.05f, 0.15f);
    [SerializeField] private Vector2 particleSpeedRange = new Vector2(30f, 40f);
    [SerializeField] private bool debugMode = false;
    [SerializeField] private float spawnDistance = 1f;
    [SerializeField] private Camera targetCamera;

    private List<GameObject> activeParticles = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private Transform particlesContainer;

    void Start()
    {
        // Если камера не назначена в инспекторе, используем основную камеру
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("Target camera not found!");
            return;
        }

        // Проверяем, что есть хотя бы один префаб частицы
        if (particlePrefabs.Count == 0)
        {
            Debug.LogError("No particle prefabs assigned!");
            return;
        }

        particlesContainer = new GameObject("BackgroundParticlesContainer").transform;

        spawnCoroutine = StartCoroutine(SpawnParticles());
    }

    void Update()
    {
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            GameObject particle = activeParticles[i];
            if (particle == null)
            {
                activeParticles.RemoveAt(i);
                continue;
            }

            MoveParticle(particle);

            if (IsParticleOutOfBounds(particle))
            {
                activeParticles.RemoveAt(i);
                Destroy(particle);
            }
        }
    }

    IEnumerator SpawnParticles()
    {
        while (true)
        {
            // Случайное значение задержки из диапазона spawnRateRange
            float randomSpawnRate = Random.Range(spawnRateRange.x, spawnRateRange.y);
            yield return new WaitForSeconds(1f / randomSpawnRate);
            SpawnParticle();
        }
    }

    void SpawnParticle()
    {
        // Проверяем, что есть префабы частиц
        if (particlePrefabs.Count == 0)
        {
            Debug.LogError("No particle prefabs assigned!");
            return;
        }

        // Случайным образом выбираем префаб из списка
        int randomIndex = Random.Range(0, particlePrefabs.Count);
        GameObject selectedPrefab = particlePrefabs[randomIndex];

        if (selectedPrefab == null)
        {
            Debug.LogError($"Particle prefab at index {randomIndex} is null!");
            return;
        }

        Vector3 spawnPosition = CalculateSpawnPosition();

        GameObject particle = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity, particlesContainer);

        if (particle == null)
        {
            Debug.LogError("Failed to instantiate particle!");
            return;
        }

        SetParticleScaleRecursive(particle.transform, 1f / 5f);

        float randomXRotation = Random.Range(-60f, -30f);
        particle.transform.rotation = Quaternion.Euler(randomXRotation, 90f, 0f);

        activeParticles.Add(particle);

        // Теперь всегда используется случайная скорость из диапазона
        float speed = Random.Range(particleSpeedRange.x, particleSpeedRange.y);

        ParticleData particleData = particle.GetComponent<ParticleData>();
        if (particleData == null)
        {
            particleData = particle.AddComponent<ParticleData>();
        }

        particleData.speed = speed;
        particleData.direction = -particle.transform.forward;

        if (debugMode)
        {
            Debug.Log($"Spawned particle of type {selectedPrefab.name} at {spawnPosition} with speed {speed}");
            Debug.Log($"Particle rotation: {particle.transform.rotation.eulerAngles}");
            Debug.Log($"Particle direction: {particleData.direction}");
        }
    }

    // Этот метод рекурсивно устанавливает scale для всех детских объектов
    void SetParticleScaleRecursive(Transform target, float scaleFactor)
    {
        target.localScale = target.localScale * scaleFactor;

        for (int i = 0; i < target.childCount; i++)
        {
            SetParticleScaleRecursive(target.GetChild(i), scaleFactor);
        }
    }

    Vector3 CalculateSpawnPosition()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
                return Vector3.zero;
        }

        // Получаем границы камеры в мировых координатах
        float cameraHeight = 2f * targetCamera.orthographicSize;
        float cameraWidth = cameraHeight * targetCamera.aspect;

        Vector3 cameraPosition = targetCamera.transform.position;

        // Середина верхней границы камеры
        Vector3 cameraTopCenter = new Vector3(
            cameraPosition.x,
            cameraPosition.y + cameraHeight / 2f,
            0f
        );

        // Область спавна - точная копия области камеры по размерам
        // Нижний левый угол области спавна совпадает с серединой верхней границы камеры
        // Это значит, что область спавна будет справа от камеры на том же уровне по высоте
        Vector3 spawnPosition = new Vector3(
            cameraTopCenter.x + Random.Range(0f, cameraWidth),
            cameraTopCenter.y + Random.Range(0f, cameraHeight),
            0f
        );

        return spawnPosition;
    }

    void MoveParticle(GameObject particle)
    {
        ParticleData particleData = particle.GetComponent<ParticleData>();
        if (particleData != null)
        {
            // Сохраняем Z-координату при движении
            Vector3 newPosition = particle.transform.position + particleData.direction * particleData.speed * Time.deltaTime;
            newPosition.z = 0f; // Всегда сохраняем Z = 0
            particle.transform.position = newPosition;
        }
    }

    bool IsParticleOutOfBounds(GameObject particle)
    {
        if (targetCamera == null)
            return false;

        // Получаем границы камеры в мировых координатах
        float cameraHeight = 2f * targetCamera.orthographicSize;
        float cameraWidth = cameraHeight * targetCamera.aspect;
        Vector3 cameraPosition = targetCamera.transform.position;

        // Проверяем, вышла ли частица за левую или нижнюю границу камеры
        // Умножаем на 1.5 чтобы дать частицам немного времени перед уничтожением
        return particle.transform.position.x < cameraPosition.x - cameraWidth * 1.5f ||
               particle.transform.position.y < cameraPosition.y - cameraHeight * 1.5f;
    }

    void OnDrawGizmos()
    {
        if (!debugMode) return;

        if (targetCamera != null)
        {
            // Получаем границы камеры в мировых координатах
            float cameraHeight = 2f * targetCamera.orthographicSize;
            float cameraWidth = cameraHeight * targetCamera.aspect;
            Vector3 cameraPosition = targetCamera.transform.position;

            // Визуализация камеры
            Vector3 cameraCenter = new Vector3(cameraPosition.x, cameraPosition.y, cameraPosition.z);
            Vector3 cameraSize = new Vector3(cameraWidth, cameraHeight, 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(cameraCenter, cameraSize);

            // Середина верхней границы камеры
            Vector3 cameraTopCenter = new Vector3(
                cameraPosition.x,
                cameraPosition.y + cameraHeight / 2f,
                0f
            );

            // Визуализация области спавна
            Vector3 spawnAreaSize = new Vector3(cameraWidth, cameraHeight, 0.1f);
            Vector3 spawnAreaCenter = new Vector3(
                cameraTopCenter.x + cameraWidth / 2f,
                cameraTopCenter.y + cameraHeight / 2f,
                0f
            );

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);

            // Визуализация активных частиц
            Gizmos.color = Color.cyan;
            foreach (GameObject particle in activeParticles)
            {
                if (particle != null)
                {
                    Gizmos.DrawSphere(particle.transform.position, 0.1f);
                    ParticleData particleData = particle.GetComponent<ParticleData>();
                    if (particleData != null)
                    {
                        Gizmos.DrawRay(particle.transform.position, particleData.direction * 2f);
                    }
                }
            }

            // Визуализация точки спавна (середина верхней границы камеры)
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(cameraTopCenter, 0.2f);

            // Визуализация границ области спавна
            Gizmos.color = Color.magenta;
            Vector3 spawnBottomLeft = cameraTopCenter;
            Vector3 spawnBottomRight = spawnBottomLeft + new Vector3(cameraWidth, 0, 0);
            Vector3 spawnTopLeft = spawnBottomLeft + new Vector3(0, cameraHeight, 0);
            Vector3 spawnTopRight = spawnBottomLeft + new Vector3(cameraWidth, cameraHeight, 0);

            Gizmos.DrawLine(spawnBottomLeft, spawnBottomRight);
            Gizmos.DrawLine(spawnBottomRight, spawnTopRight);
            Gizmos.DrawLine(spawnTopRight, spawnTopLeft);
            Gizmos.DrawLine(spawnTopLeft, spawnBottomLeft);
        }
    }

    public void SetSpawnRateRange(Vector2 newRateRange)
    {
        spawnRateRange = new Vector2(
            Mathf.Max(0.01f, newRateRange.x),
            Mathf.Max(0.01f, newRateRange.y)
        );

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnParticles());
        }
    }

    public void SetSpawnDistance(float newDistance)
    {
        spawnDistance = newDistance;
    }

    // Этот метод позволяет изменять scale активных партиклов
    public void SetParticleScale(float scaleFactor)
    {
        foreach (GameObject particle in activeParticles)
        {
            if (particle != null)
            {
                SetParticleScaleRecursive(particle.transform, scaleFactor);
            }
        }
    }

    public void ClearAllParticles()
    {
        foreach (GameObject particle in activeParticles)
        {
            if (particle != null)
                Destroy(particle);
        }
        activeParticles.Clear();
    }

    void OnDestroy()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        ClearAllParticles();

        if (particlesContainer != null)
        {
            Destroy(particlesContainer.gameObject);
        }
    }
}

public class ParticleData : MonoBehaviour
{
    public float speed;
    public Vector3 direction;
}
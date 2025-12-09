using UnityEngine;

namespace My.Scripts.Environment.Hazards
{
    public class WindParticleSpawnSystem : MonoBehaviour
    {
        [Header("Particle Settings")]
        [SerializeField] private GameObject[] windParticlePrefabs; // Assign your 4 prefabs here
        [SerializeField] private float particleLifetime = 5f; // 5 seconds as requested

        private Collider2D spawnCollider;
        private float spawnTimer;
        private float spawnInterval;
        private float baseArea = 450f; // Base area for 1 particle per second

        private void Start()
        {
            // ѕолучаем коллайдер зоны спавна частиц
            spawnCollider = GetComponent<Collider2D>();
            if (spawnCollider != null)
            {
                spawnCollider.isTrigger = true;
                CalculateSpawnInterval();
            }
            else
            {
                Debug.LogWarning("WindParticleSpawner: No Collider2D found on this GameObject!");
            }

            // ѕровер€ем наличие префабов частиц
            if (windParticlePrefabs == null || windParticlePrefabs.Length == 0)
            {
                Debug.LogWarning("WindParticleSpawner: No wind particle prefabs assigned!");
            }
        }

        private void Update()
        {
            // ќбработка спавна частиц
            UpdateParticleSpawning();
        }

        private void UpdateParticleSpawning()
        {
            if (spawnCollider == null || windParticlePrefabs == null || windParticlePrefabs.Length == 0) return;

            spawnTimer += Time.deltaTime;

            if (spawnTimer >= spawnInterval)
            {
                SpawnParticle();
                spawnTimer = 0f;
            }
        }

        private void SpawnParticle()
        {
            // ¬ыбираем случайный префаб частицы
            int randomIndex = Random.Range(0, windParticlePrefabs.Length);
            GameObject particlePrefab = windParticlePrefabs[randomIndex];

            // ѕолучаем случайную позицию внутри коллайдера
            Vector2 spawnPosition = GetRandomPointInCollider(spawnCollider);

            // —оздаем частицу
            GameObject particle = Instantiate(particlePrefab, spawnPosition, Quaternion.identity);

            particle.transform.SetParent(transform, true);
            particle.transform.localRotation = Quaternion.identity;

            // ”ничтожаем частицу через заданное врем€
            Destroy(particle, particleLifetime);
        }

        private Vector2 GetRandomPointInCollider(Collider2D collider)
        {
            if (collider is BoxCollider2D boxCollider)
            {
                Vector2 center = (Vector2)transform.position + boxCollider.offset;
                Vector2 size = boxCollider.size;
                Vector2 scale = transform.lossyScale;

                float randomX = Random.Range(-size.x * scale.x / 2, size.x * scale.x / 2);
                float randomY = Random.Range(-size.y * scale.y / 2, size.y * scale.y / 2);

                return center + new Vector2(randomX, randomY);
            }
            else if (collider is CircleCollider2D circleCollider)
            {
                Vector2 center = (Vector2)transform.position + circleCollider.offset;
                float radius = circleCollider.radius;
                Vector2 scale = transform.lossyScale;
                float maxScale = Mathf.Max(scale.x, scale.y);

                // —лучайна€ точка в круге
                Vector2 randomPoint = Random.insideUnitCircle * radius * maxScale;
                return center + randomPoint;
            }
            else
            {
                // ƒл€ других типов коллайдеров используем bounds
                Bounds bounds = collider.bounds;
                float randomX = Random.Range(bounds.min.x, bounds.max.x);
                float randomY = Random.Range(bounds.min.y, bounds.max.y);
                return new Vector2(randomX, randomY);
            }
        }

        private void CalculateSpawnInterval()
        {
            if (spawnCollider == null) return;

            float area = GetColliderArea(spawnCollider);

            // Ѕазовое значение: 1 частица в секунду на 450 единиц площади
            spawnInterval = baseArea / area;

            // ќграничим минимальный интервал, чтобы не спавнить слишком много частиц
            if (spawnInterval < 0.8f) spawnInterval = 0.8f;

            Debug.Log($"WindParticleSpawner: Area = {area}, Spawn Interval = {spawnInterval}s");
        }

        private float GetColliderArea(Collider2D collider)
        {
            if (collider is BoxCollider2D boxCollider)
            {
                Vector2 size = boxCollider.size;
                Vector2 scale = transform.lossyScale;
                return size.x * scale.x * size.y * scale.y;
            }
            else if (collider is CircleCollider2D circleCollider)
            {
                float radius = circleCollider.radius;
                Vector2 scale = transform.lossyScale;
                float maxScale = Mathf.Max(scale.x, scale.y);
                return Mathf.PI * radius * radius * maxScale * maxScale;
            }
            else
            {
                // ƒл€ других коллайдеров используем площадь bounding box
                Bounds bounds = collider.bounds;
                return bounds.size.x * bounds.size.y;
            }
        }

        // ћетод дл€ обновлени€ интервала спавна при изменении коллайдера в редакторе
        private void OnValidate()
        {
            if (Application.isPlaying) return;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                float area = GetColliderArea(col);
                float interval = baseArea / area;
                if (interval < 0.8f)
                {
                    interval = 0.8f;
                }
            }
        }

        // ћетод дл€ ручного обновлени€ интервала (например, если коллайдер мен€етс€ во врем€ выполнени€)
        public void UpdateSpawnInterval()
        {
            CalculateSpawnInterval();
        }
    }
}
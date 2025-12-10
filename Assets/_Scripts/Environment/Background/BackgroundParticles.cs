using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Scripts.Environment.Background
{
    /// <summary>
    /// Управляет спавном и движением фоновых частиц (падающие звёзды, метеоры и т.д.).
    /// </summary>
    public class BackgroundParticles : MonoBehaviour
    {
        #region Constants

        private const float MIN_SPAWN_RATE = 0.01f;
        private const float PARTICLE_SCALE_FACTOR = 0.2f;
        private const float BOUNDS_MULTIPLIER = 1.5f;
        private const string CONTAINER_NAME = "BackgroundParticlesContainer";

        #endregion

        #region Serialized Fields

        [Header("Prefabs")]
        [SerializeField] private List<GameObject> _particlePrefabs = new();

        [Header("Spawn Settings")]
        [SerializeField] private Vector2 _spawnRateRange = new(0.07f, 0.25f);
        [SerializeField] private Vector2 _particleSpeedRange = new(30f, 40f);
        [SerializeField] private Vector2 _rotationXRange = new(-60f, -30f);

        [Header("References")]
        [SerializeField] private Camera _targetCamera;

        [Header("Debug")]
        [SerializeField] private bool _debugMode;

        #endregion

        #region Private Fields

        private readonly List<ParticleInstance> _activeParticles = new();
        private Transform _particlesContainer;
        private Coroutine _spawnCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            UpdateParticles();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Public Methods

        public void SetSpawnRateRange(Vector2 newRateRange)
        {
            _spawnRateRange = new Vector2(
                Mathf.Max(MIN_SPAWN_RATE, newRateRange.x),
                Mathf.Max(MIN_SPAWN_RATE, newRateRange.y)
            );

            RestartSpawning();
        }

        public void SetParticleScale(float scaleFactor)
        {
            foreach (var particle in _activeParticles)
            {
                if (particle.GameObject != null)
                {
                    SetScaleRecursive(particle.GameObject.transform, scaleFactor);
                }
            }
        }

        public void ClearAllParticles()
        {
            foreach (var particle in _activeParticles)
            {
                if (particle.GameObject != null)
                {
                    Destroy(particle.GameObject);
                }
            }

            _activeParticles.Clear();
        }

        #endregion

        #region Private Methods — Initialization

        private void Initialize()
        {
            if (!ValidateSetup()) return;

            CreateContainer();
            StartSpawning();
        }

        private bool ValidateSetup()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera == null)
            {
                Debug.LogError($"[{nameof(BackgroundParticles)}] Target camera not found!", this);
                return false;
            }

            if (_particlePrefabs.Count == 0)
            {
                Debug.LogError($"[{nameof(BackgroundParticles)}] No particle prefabs assigned!", this);
                return false;
            }

            return true;
        }

        private void CreateContainer()
        {
            var containerObject = new GameObject(CONTAINER_NAME);
            _particlesContainer = containerObject.transform;
        }

        #endregion

        #region Private Methods — Spawning

        private void StartSpawning()
        {
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private void RestartSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
            }

            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                float spawnRate = Random.Range(_spawnRateRange.x, _spawnRateRange.y);
                yield return new WaitForSeconds(1f / spawnRate);

                SpawnParticle();
            }
        }

        private void SpawnParticle()
        {
            GameObject prefab = GetRandomPrefab();
            if (prefab == null) return;

            Vector3 spawnPosition = CalculateSpawnPosition();
            GameObject particleObject = Instantiate(prefab, spawnPosition, Quaternion.identity, _particlesContainer);

            if (particleObject == null)
            {
                Debug.LogError($"[{nameof(BackgroundParticles)}] Failed to instantiate particle!", this);
                return;
            }

            SetupParticle(particleObject);

            if (_debugMode)
            {
                LogParticleSpawn(particleObject, prefab.name);
            }
        }

        private GameObject GetRandomPrefab()
        {
            if (_particlePrefabs.Count == 0) return null;

            int randomIndex = Random.Range(0, _particlePrefabs.Count);
            GameObject prefab = _particlePrefabs[randomIndex];

            if (prefab == null)
            {
                Debug.LogError($"[{nameof(BackgroundParticles)}] Particle prefab at index {randomIndex} is null!", this);
            }

            return prefab;
        }

        private void SetupParticle(GameObject particleObject)
        {
            // Масштаб
            SetScaleRecursive(particleObject.transform, PARTICLE_SCALE_FACTOR);

            // Поворот
            float rotationX = Random.Range(_rotationXRange.x, _rotationXRange.y);
            particleObject.transform.rotation = Quaternion.Euler(rotationX, 90f, 0f);

            // Скорость и направление
            float speed = Random.Range(_particleSpeedRange.x, _particleSpeedRange.y);
            Vector3 direction = -particleObject.transform.forward;

            var particleInstance = new ParticleInstance
            {
                GameObject = particleObject,
                Speed = speed,
                Direction = direction
            };

            _activeParticles.Add(particleInstance);
        }

        private Vector3 CalculateSpawnPosition()
        {
            if (_targetCamera == null) return Vector3.zero;

            float cameraHeight = _targetCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * _targetCamera.aspect;
            Vector3 cameraPosition = _targetCamera.transform.position;

            // Область спавна — над и правее камеры
            Vector3 spawnOrigin = new Vector3(
                cameraPosition.x,
                cameraPosition.y + cameraHeight / 2f,
                0f
            );

            return new Vector3(
                spawnOrigin.x + Random.Range(0f, cameraWidth),
                spawnOrigin.y + Random.Range(0f, cameraHeight),
                0f
            );
        }

        #endregion

        #region Private Methods — Update

        private void UpdateParticles()
        {
            for (int i = _activeParticles.Count - 1; i >= 0; i--)
            {
                var particle = _activeParticles[i];

                if (particle.GameObject == null)
                {
                    _activeParticles.RemoveAt(i);
                    continue;
                }

                MoveParticle(particle);

                if (IsOutOfBounds(particle.GameObject))
                {
                    Destroy(particle.GameObject);
                    _activeParticles.RemoveAt(i);
                }
            }
        }

        private void MoveParticle(ParticleInstance particle)
        {
            Vector3 movement = particle.Direction * particle.Speed * Time.deltaTime;
            Vector3 newPosition = particle.GameObject.transform.position + movement;
            newPosition.z = 0f;

            particle.GameObject.transform.position = newPosition;
        }

        private bool IsOutOfBounds(GameObject particleObject)
        {
            if (_targetCamera == null) return false;

            float cameraHeight = _targetCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * _targetCamera.aspect;
            Vector3 cameraPosition = _targetCamera.transform.position;
            Vector3 particlePosition = particleObject.transform.position;

            float leftBound = cameraPosition.x - cameraWidth * BOUNDS_MULTIPLIER;
            float bottomBound = cameraPosition.y - cameraHeight * BOUNDS_MULTIPLIER;

            return particlePosition.x < leftBound || particlePosition.y < bottomBound;
        }

        #endregion

        #region Private Methods — Helpers

        private void SetScaleRecursive(Transform target, float scaleFactor)
        {
            target.localScale *= scaleFactor;

            foreach (Transform child in target)
            {
                SetScaleRecursive(child, scaleFactor);
            }
        }

        private void Cleanup()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            ClearAllParticles();

            if (_particlesContainer != null)
            {
                Destroy(_particlesContainer.gameObject);
            }
        }

        private void LogParticleSpawn(GameObject particleObject, string prefabName)
        {
            var particle = _activeParticles[^1]; // Последний добавленный

            Debug.Log($"[{nameof(BackgroundParticles)}] Spawned '{prefabName}' at {particleObject.transform.position} " +
                      $"with speed {particle.Speed:F1}");
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_debugMode || _targetCamera == null) return;

            DrawCameraBounds();
            DrawSpawnArea();
            DrawActiveParticles();
        }

        private void DrawCameraBounds()
        {
            float cameraHeight = _targetCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * _targetCamera.aspect;
            Vector3 cameraPosition = _targetCamera.transform.position;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                new Vector3(cameraPosition.x, cameraPosition.y, 0f),
                new Vector3(cameraWidth, cameraHeight, 0.1f)
            );
        }

        private void DrawSpawnArea()
        {
            float cameraHeight = _targetCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * _targetCamera.aspect;
            Vector3 cameraPosition = _targetCamera.transform.position;

            Vector3 spawnOrigin = new Vector3(
                cameraPosition.x,
                cameraPosition.y + cameraHeight / 2f,
                0f
            );

            Vector3 spawnCenter = new Vector3(
                spawnOrigin.x + cameraWidth / 2f,
                spawnOrigin.y + cameraHeight / 2f,
                0f
            );

            // Область спавна
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(spawnCenter, new Vector3(cameraWidth, cameraHeight, 0.1f));

            // Точка начала спавна
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(spawnOrigin, 0.2f);

            // Границы области спавна
            Gizmos.color = Color.magenta;
            Vector3 bottomLeft = spawnOrigin;
            Vector3 bottomRight = spawnOrigin + Vector3.right * cameraWidth;
            Vector3 topLeft = spawnOrigin + Vector3.up * cameraHeight;
            Vector3 topRight = spawnOrigin + new Vector3(cameraWidth, cameraHeight, 0f);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }

        private void DrawActiveParticles()
        {
            Gizmos.color = Color.cyan;

            foreach (var particle in _activeParticles)
            {
                if (particle.GameObject == null) continue;

                Vector3 position = particle.GameObject.transform.position;
                Gizmos.DrawSphere(position, 0.1f);
                Gizmos.DrawRay(position, particle.Direction * 2f);
            }
        }
#endif

        #endregion

        #region Nested Types

        /// <summary>
        /// Данные активной частицы.
        /// </summary>
        private class ParticleInstance
        {
            public GameObject GameObject;
            public float Speed;
            public Vector3 Direction;
        }

        #endregion
    }
}
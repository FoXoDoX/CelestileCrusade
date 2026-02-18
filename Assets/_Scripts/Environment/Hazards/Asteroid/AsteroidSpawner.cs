using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace My.Scripts.Environment.Hazards
{
    public class AsteroidSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public GameObject asteroidPrefab;
        public float respawnTime = 3f;
        public int poolSize = 5;

        [Header("Path Settings")]
        public float movementSpeed = 8f;
        public Vector3 rotationSpeed = new Vector3(0, 0, 60);
        public PathType pathType = PathType.CatmullRom;

        private Vector3[] pathPoints;
        private List<Transform> pathPointTransforms = new List<Transform>();
        private Queue<GameObject> asteroidPool = new Queue<GameObject>();
        private List<GameObject> allAsteroids = new List<GameObject>();
        private bool _isDestroyed;

        void Start()
        {
            CollectPathPoints();
            InitializePool();
            StartCoroutine(SpawnCycle());
        }

        void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject asteroid = Instantiate(asteroidPrefab, transform);
                asteroid.SetActive(false);
                asteroidPool.Enqueue(asteroid);
                allAsteroids.Add(asteroid);
            }
        }

        GameObject GetFromPool()
        {
            if (asteroidPool.Count > 0)
            {
                var asteroid = asteroidPool.Dequeue();
                asteroid.SetActive(true);
                return asteroid;
            }

            // Если пул пуст — создаём новый
            var newAsteroid = Instantiate(asteroidPrefab, transform);
            allAsteroids.Add(newAsteroid);
            return newAsteroid;
        }

        void ReturnToPool(GameObject asteroid)
        {
            if (asteroid == null) return;

            // Убиваем ВСЕ твины на этом конкретном объекте
            DOTween.Kill(asteroid.transform);

            asteroid.transform.localPosition = Vector3.zero;
            asteroid.transform.localRotation = Quaternion.identity;
            asteroid.SetActive(false);

            asteroidPool.Enqueue(asteroid);
        }

        void CollectPathPoints()
        {
            pathPointTransforms.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.gameObject != asteroidPrefab)
                {
                    pathPointTransforms.Add(child);
                }
            }

            pathPoints = new Vector3[pathPointTransforms.Count];
            for (int i = 0; i < pathPointTransforms.Count; i++)
            {
                pathPoints[i] = pathPointTransforms[i].localPosition;
            }
        }

        IEnumerator SpawnCycle()
        {
            while (!_isDestroyed)
            {
                if (pathPoints == null || pathPoints.Length == 0)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                GameObject asteroid = GetFromPool();
                asteroid.transform.localPosition = Vector3.zero;

                yield return StartCoroutine(MoveAndRotateAsteroid(asteroid));

                if (_isDestroyed) yield break;

                yield return new WaitForSeconds(respawnTime);
            }
        }

        IEnumerator MoveAndRotateAsteroid(GameObject asteroid)
        {
            if (asteroid == null || !asteroid.activeInHierarchy) yield break;

            float pathLength = CalculatePathLength(pathPoints);
            float moveDuration = pathLength / movementSpeed;

            Tween moveTween = asteroid.transform
                .DOLocalPath(pathPoints, moveDuration, pathType)
                .SetEase(Ease.Linear)
                .SetLink(asteroid);     // ← автоматический Kill при деактивации/уничтожении

            Tween rotateTween = asteroid.transform
                .DORotate(rotationSpeed * moveDuration, moveDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetLink(asteroid);     // ← автоматический Kill при деактивации/уничтожении

            yield return moveTween.WaitForCompletion();

            // На случай если SetLink не убил (объект ещё жив)
            if (rotateTween != null && rotateTween.IsActive())
            {
                rotateTween.Kill();
            }

            if (!_isDestroyed)
            {
                ReturnToPool(asteroid);
            }
        }

        private float CalculatePathLength(Vector3[] points)
        {
            if (points.Length < 2) return 0f;

            float length = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                length += Vector3.Distance(points[i - 1], points[i]);
            }
            return length;
        }

        void OnDestroy()
        {
            _isDestroyed = true;

            // Убиваем твины ВСЕХ астероидов, не только спавнера
            foreach (var asteroid in allAsteroids)
            {
                if (asteroid != null)
                {
                    DOTween.Kill(asteroid.transform);
                }
            }

            DOTween.Kill(transform);

            allAsteroids.Clear();
            asteroidPool.Clear();
        }
    }
}
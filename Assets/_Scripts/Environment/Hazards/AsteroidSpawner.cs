using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
            return Instantiate(asteroidPrefab, transform);
        }

        void ReturnToPool(GameObject asteroid)
        {
            if (asteroid == null) return;

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
            while (true)
            {
                if (pathPoints == null || pathPoints.Length == 0)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                GameObject asteroid = GetFromPool();
                asteroid.transform.localPosition = Vector3.zero;

                yield return StartCoroutine(MoveAndRotateAsteroid(asteroid));

                yield return new WaitForSeconds(respawnTime);
            }
        }

        IEnumerator MoveAndRotateAsteroid(GameObject asteroid)
        {
            float pathLength = CalculatePathLength(pathPoints);
            float moveDuration = pathLength / movementSpeed;

            Tween moveTween = asteroid.transform
                .DOLocalPath(pathPoints, moveDuration, pathType)
                .SetEase(Ease.Linear);

            Tween rotateTween = asteroid.transform
                .DORotate(rotationSpeed * moveDuration, moveDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);

            yield return moveTween.WaitForCompletion();

            rotateTween.Kill();

            // Возвращаем в пул вместо уничтожения
            ReturnToPool(asteroid);
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
            DOTween.Kill(transform);
        }
    }
}
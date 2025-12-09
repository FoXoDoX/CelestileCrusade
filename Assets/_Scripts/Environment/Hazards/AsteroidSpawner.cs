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

        [Header("Path Settings")]
        public float movementSpeed = 8f;
        public Vector3 rotationSpeed = new Vector3(0, 0, 60);
        public PathType pathType = PathType.CatmullRom;

        private Vector3[] pathPoints;
        private List<Transform> pathPointTransforms = new List<Transform>();

        void Start()
        {
            CollectPathPoints();
            StartCoroutine(SpawnCycle());
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

            if (pathPoints.Length == 0)
            {
                Debug.LogWarning("Не найдено дочерних объектов для использования в качестве точек пути!");
            }
        }

        IEnumerator SpawnCycle()
        {
            while (true)
            {
                if (pathPoints == null || pathPoints.Length == 0)
                {
                    Debug.LogWarning("Нет точек пути! Ждем 1 секунду и проверяем снова.");
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                GameObject asteroid = Instantiate(asteroidPrefab, transform.position, Quaternion.identity);
                asteroid.transform.SetParent(transform);
                asteroid.transform.localPosition = Vector3.zero;

                yield return StartCoroutine(MoveAndRotateAsteroid(asteroid));

                yield return new WaitForSeconds(respawnTime);
            }
        }

        IEnumerator MoveAndRotateAsteroid(GameObject asteroid)
        {
            float pathLength = CalculatePathLength(pathPoints);
            float moveDuration = pathLength / movementSpeed;

            Tween moveTween = asteroid.transform.DOLocalPath(pathPoints, moveDuration, pathType).SetEase(Ease.Linear);

            Tween rotateTween = asteroid.transform.DORotate(rotationSpeed * moveDuration, moveDuration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);

            yield return moveTween.WaitForCompletion();

            rotateTween.Kill();
            Destroy(asteroid);
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

        void OnDrawGizmos()
        {
            if (pathPoints == null || pathPoints.Length == 0)
            {
                CollectPathPointsForGizmos();
            }

            if (pathPoints == null || pathPoints.Length == 0)
                return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                Vector3 worldPoint = transform.TransformPoint(pathPoints[i]);

                Gizmos.DrawSphere(worldPoint, 0.2f);

                if (i > 0)
                {
                    Vector3 prevWorldPoint = transform.TransformPoint(pathPoints[i - 1]);
                    Gizmos.DrawLine(prevWorldPoint, worldPoint);
                }
            }

            if (pathPoints.Length > 0)
            {
                Gizmos.color = Color.green;
                Vector3 firstWorldPoint = transform.TransformPoint(pathPoints[0]);
                Gizmos.DrawSphere(firstWorldPoint, 0.25f);
            }

            if (pathPoints.Length > 1)
            {
                Gizmos.color = Color.red;
                Vector3 lastWorldPoint = transform.TransformPoint(pathPoints[pathPoints.Length - 1]);
                Gizmos.DrawSphere(lastWorldPoint, 0.25f);
            }
        }

        void CollectPathPointsForGizmos()
        {
            pathPointTransforms.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                if (asteroidPrefab == null || child.gameObject != asteroidPrefab)
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

        void OnDestroy()
        {
            DOTween.Kill(this.gameObject);
        }
    }
}
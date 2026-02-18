using UnityEngine;
using UnityEngine.U2D;

namespace My.Scripts.Environment.Hazards
{
    public class AcidBubbleSpawner : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Spawn Settings")]
        [SerializeField] private GameObject _bubblePrefab;
        [SerializeField] private float _spawnIntervalMin = 3f;
        [SerializeField] private float _spawnIntervalMax = 6f;

        [Header("Surface")]
        [SerializeField] private SpriteShapeController _spriteShape;
        [SerializeField] private int _surfaceStartIndex = 2;
        [SerializeField] private int _surfaceEndIndex = 13;

        [Header("Edge Margin")]
        [Tooltip("Отступ от краёв поверхности (0-0.5)")]
        [SerializeField, Range(0f, 0.5f)] private float _edgeMargin = 0.1f;

        [Header("Spawn Offset")]
        [Tooltip("Смещение точки спавна вниз от поверхности")]
        [SerializeField] private float _spawnOffsetY = 0.2f;

        #endregion

        #region Private Fields

        private float _timer;
        private float _nextSpawnTime;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            SetNextSpawnTime();
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= _nextSpawnTime)
            {
                SpawnBubble();
                _timer = 0f;
                SetNextSpawnTime();
            }
        }

        #endregion

        #region Private Methods

        private void SetNextSpawnTime()
        {
            _nextSpawnTime = Random.Range(_spawnIntervalMin, _spawnIntervalMax);
        }

        private void SpawnBubble()
        {
            if (_bubblePrefab == null || _spriteShape == null) return;

            Vector3 spawnPos = GetRandomSurfacePosition();
            spawnPos.y -= _spawnOffsetY;

            Instantiate(_bubblePrefab, spawnPos, Quaternion.identity);
        }

        private Vector3 GetRandomSurfacePosition()
        {
            var spline = _spriteShape.spline;

            float t = Random.Range(_edgeMargin, 1f - _edgeMargin);

            Vector3 startPos = spline.GetPosition(_surfaceStartIndex);
            Vector3 endPos = spline.GetPosition(_surfaceEndIndex);

            Vector3 localPos = Vector3.Lerp(startPos, endPos, t);

            Vector3 worldPos = _spriteShape.transform.TransformPoint(localPos);

            return worldPos;
        }

        #endregion
    }
}
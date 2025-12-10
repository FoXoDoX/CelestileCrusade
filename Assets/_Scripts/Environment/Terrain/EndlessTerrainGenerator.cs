using My.Scripts.Gameplay.Player;
using System.Collections.Generic;
using UnityEngine;

namespace My.Scripts.Environment.Terrain
{
    public class EndlessTerrainGenerator : MonoBehaviour
    {
        #region Constants

        private const float DEFAULT_SPAWN_DISTANCE = 100f;
        private const int DEFAULT_INITIAL_PARTS = 3;

        #endregion

        #region Serialized Fields

        [Header("Spawn Points")]
        [SerializeField] private Transform _rightSpawnPoint;
        [SerializeField] private Transform _leftSpawnPoint;

        [Header("Terrain Parts")]
        [SerializeField] private List<Transform> _terrainPartPrefabs;

        [Header("Settings")]
        [SerializeField] private float _spawnDistance = DEFAULT_SPAWN_DISTANCE;
        [SerializeField] private int _initialPartsPerSide = DEFAULT_INITIAL_PARTS;
        [SerializeField] private Vector3 _rightSpawnOffset = new(3f, 6f, 0f);
        [SerializeField] private Vector3 _leftSpawnOffset = new(3f, 6f, 0f);

        #endregion

        #region Private Fields

        private Vector3 _lastRightEndPosition;
        private Vector3 _lastLeftEndPosition;
        private Transform _trackedTransform;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            InitializeSpawnPositions();
        }

        private void Start()
        {
            CacheTrackedTransform();
            SpawnInitialTerrain();
        }

        private void Update()
        {
            if (_trackedTransform == null)
            {
                TryRecacheTrackedTransform();
                return;
            }

            CheckAndSpawnTerrain(Side.Right);
            CheckAndSpawnTerrain(Side.Left);
        }

        #endregion

        #region Private Methods Ч Initialization

        private void ValidateReferences()
        {
            if (_rightSpawnPoint == null || _leftSpawnPoint == null)
            {
                Debug.LogError($"[{nameof(EndlessTerrainGenerator)}] Spawn points not assigned!", this);
            }

            if (_terrainPartPrefabs == null || _terrainPartPrefabs.Count == 0)
            {
                Debug.LogError($"[{nameof(EndlessTerrainGenerator)}] No terrain part prefabs assigned!", this);
            }
        }

        private void InitializeSpawnPositions()
        {
            if (_rightSpawnPoint != null)
            {
                _lastRightEndPosition = _rightSpawnPoint.position - _rightSpawnOffset;
            }

            if (_leftSpawnPoint != null)
            {
                _lastLeftEndPosition = _leftSpawnPoint.position + new Vector3(_leftSpawnOffset.x, -_leftSpawnOffset.y, _leftSpawnOffset.z);
            }
        }

        private void CacheTrackedTransform()
        {
            if (Lander.HasInstance)
            {
                _trackedTransform = Lander.Instance.transform;
            }
        }

        private void TryRecacheTrackedTransform()
        {
            if (Lander.HasInstance)
            {
                _trackedTransform = Lander.Instance.transform;
            }
        }

        private void SpawnInitialTerrain()
        {
            for (int i = 0; i < _initialPartsPerSide; i++)
            {
                SpawnTerrainPart(Side.Right);
                SpawnTerrainPart(Side.Left);
            }
        }

        #endregion

        #region Private Methods Ч Terrain Generation

        private void CheckAndSpawnTerrain(Side side)
        {
            Vector3 endPosition = GetEndPosition(side);
            float distance = Vector3.Distance(_trackedTransform.position, endPosition);

            if (distance < _spawnDistance)
            {
                SpawnTerrainPart(side);
            }
        }

        private void SpawnTerrainPart(Side side)
        {
            if (_terrainPartPrefabs.Count == 0) return;

            // ¬ыбираем случайный prefab
            Transform prefab = GetRandomTerrainPrefab();

            // ќпредел€ем позицию и поворот
            Vector3 spawnPosition = GetEndPosition(side);
            Quaternion rotation = GetRotationForSide(side);

            // —павним
            Transform spawnedPart = Instantiate(prefab, spawnPosition, rotation, transform);

            // ќбновл€ем конечную позицию
            UpdateEndPosition(side, spawnedPart);
        }

        private Transform GetRandomTerrainPrefab()
        {
            int randomIndex = Random.Range(0, _terrainPartPrefabs.Count);
            return _terrainPartPrefabs[randomIndex];
        }

        private Vector3 GetEndPosition(Side side)
        {
            return side == Side.Right ? _lastRightEndPosition : _lastLeftEndPosition;
        }

        private Quaternion GetRotationForSide(Side side)
        {
            return side == Side.Right
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);
        }

        private void UpdateEndPosition(Side side, Transform spawnedPart)
        {
            // ѕредполагаем, что первый дочерний объект Ч это точка конца terrain part
            if (spawnedPart.childCount == 0)
            {
                Debug.LogWarning(
                    $"[{nameof(EndlessTerrainGenerator)}] Spawned terrain part has no children for end position!",
                    spawnedPart
                );
                return;
            }

            Vector3 newEndPosition = spawnedPart.GetChild(0).position;

            if (side == Side.Right)
            {
                _lastRightEndPosition = newEndPosition;
            }
            else
            {
                _lastLeftEndPosition = newEndPosition;
            }
        }

        #endregion

        #region Nested Types

        private enum Side
        {
            Right,
            Left
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_spawnDistance <= 0)
            {
                _spawnDistance = DEFAULT_SPAWN_DISTANCE;
            }

            if (_initialPartsPerSide < 0)
            {
                _initialPartsPerSide = DEFAULT_INITIAL_PARTS;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // ¬изуализаци€ зон спавна
            Gizmos.color = Color.green;

            if (_rightSpawnPoint != null)
            {
                Gizmos.DrawWireSphere(_rightSpawnPoint.position, 2f);

                if (Application.isPlaying)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(_lastRightEndPosition, 1f);
                }
            }

            Gizmos.color = Color.red;

            if (_leftSpawnPoint != null)
            {
                Gizmos.DrawWireSphere(_leftSpawnPoint.position, 2f);

                if (Application.isPlaying)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(_lastLeftEndPosition, 1f);
                }
            }
        }
#endif

        #endregion
    }
}
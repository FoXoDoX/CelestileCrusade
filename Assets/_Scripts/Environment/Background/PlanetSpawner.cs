using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Scripts.Environment.Background
{
    /// <summary>
    /// —павнер редких планет на фоне с эффектом параллакса.
    /// ѕланеты по€вл€ютс€ в €чейках сетки с заданной веро€тностью,
    /// при этом одинаковые варианты не по€вл€ютс€ р€дом друг с другом.
    /// </summary>
    public class PlanetSpawner : MonoBehaviour
    {
        #region Nested Types

        [Serializable]
        public class PlanetVariant
        {
            public Sprite Sprite;
            [Range(0.1f, 3f)]
            public float MinScale = 0.8f;
            [Range(0.1f, 3f)]
            public float MaxScale = 1.2f;
        }

        private class CellData
        {
            public bool HasPlanet;
            public int VariantIndex;
            public SpriteRenderer Renderer;
            public Vector3 BaseWorldPosition;
            public float Scale;
        }

        #endregion

        #region Serialized Fields

        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private Camera _camera;

        [Header("Planet Variants")]
        [SerializeField] private List<PlanetVariant> _variants = new();

        [Header("Spawn Settings")]
        [Tooltip("–азмер одной €чейки сетки")]
        [SerializeField] private float _cellSize = 20f;

        [Tooltip("¬еро€тность по€влени€ планеты в €чейке (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _spawnChance = 0.15f;

        [Tooltip("ƒополнительные €чейки за пределами камеры")]
        [SerializeField] private int _bufferCells = 2;

        [Header("Parallax")]
        [Tooltip("0 = статичный фон, 1 = движетс€ с камерой")]
        [Range(0f, 1f)]
        [SerializeField] private float _parallaxFactor = 0.05f;

        [Header("Rendering")]
        [SerializeField] private int _sortingOrder = -90;
        [SerializeField] private string _sortingLayerName = "Background";

        [Header("Randomization")]
        [SerializeField] private int _seed = 12345;

        #endregion

        #region Private Fields

        private Dictionary<Vector2Int, CellData> _cells = new();
        private Queue<SpriteRenderer> _pool = new();
        private Vector3 _startTargetPosition;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_target == null)
            {
                Debug.LogError($"[{nameof(PlanetSpawner)}] Target is not assigned!");
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _startTargetPosition = _target.position;
            UpdateVisibleCells();
        }

        private void Update()
        {
            if (_target == null) return;

            UpdateVisibleCells();
            UpdatePlanetPositions();
        }

        private void OnDestroy()
        {
            foreach (var cell in _cells.Values)
            {
                if (cell.Renderer != null)
                {
                    Destroy(cell.Renderer.gameObject);
                }
            }

            while (_pool.Count > 0)
            {
                var renderer = _pool.Dequeue();
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
            }
        }

        #endregion

        #region Cell Management

        private void UpdateVisibleCells()
        {
            float camHeight = _camera.orthographicSize * 2f;
            float camWidth = camHeight * _camera.aspect;

            Vector3 camPos = _camera.transform.position;

            float totalWidth = camWidth + _cellSize * _bufferCells * 2;
            float totalHeight = camHeight + _cellSize * _bufferCells * 2;

            int minCellX = Mathf.FloorToInt((camPos.x - totalWidth / 2f) / _cellSize);
            int maxCellX = Mathf.CeilToInt((camPos.x + totalWidth / 2f) / _cellSize);
            int minCellY = Mathf.FloorToInt((camPos.y - totalHeight / 2f) / _cellSize);
            int maxCellY = Mathf.CeilToInt((camPos.y + totalHeight / 2f) / _cellSize);

            RemoveOutOfBoundsCells(minCellX, maxCellX, minCellY, maxCellY);
            CreateNewCells(minCellX, maxCellX, minCellY, maxCellY);
        }

        private void RemoveOutOfBoundsCells(int minX, int maxX, int minY, int maxY)
        {
            int removeBuffer = _bufferCells + 3;
            List<Vector2Int> cellsToRemove = new();

            foreach (var kvp in _cells)
            {
                Vector2Int pos = kvp.Key;
                if (pos.x < minX - removeBuffer || pos.x > maxX + removeBuffer ||
                    pos.y < minY - removeBuffer || pos.y > maxY + removeBuffer)
                {
                    cellsToRemove.Add(pos);
                }
            }

            foreach (var pos in cellsToRemove)
            {
                CellData cell = _cells[pos];
                if (cell.Renderer != null)
                {
                    cell.Renderer.gameObject.SetActive(false);
                    _pool.Enqueue(cell.Renderer);
                }
                _cells.Remove(pos);
            }
        }

        private void CreateNewCells(int minX, int maxX, int minY, int maxY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector2Int cellPos = new Vector2Int(x, y);
                    if (!_cells.ContainsKey(cellPos))
                    {
                        CreateCell(cellPos);
                    }
                }
            }
        }

        private void CreateCell(Vector2Int cellPos)
        {
            CellData cell = new CellData();

            int baseHash = HashCell(cellPos);
            System.Random spawnRng = new System.Random(baseHash);
            System.Random variantRng = new System.Random(baseHash ^ 123456789);
            System.Random positionRng = new System.Random(baseHash ^ 987654321);

            float roll = (float)spawnRng.NextDouble();
            cell.HasPlanet = roll < _spawnChance;

            if (cell.HasPlanet && _variants.Count > 0)
            {
                TrySpawnPlanet(cell, cellPos, variantRng, positionRng);
            }
            else
            {
                cell.VariantIndex = -1;
            }

            _cells[cellPos] = cell;
        }

        private void TrySpawnPlanet(CellData cell, Vector2Int cellPos,
            System.Random variantRng, System.Random positionRng)
        {
            List<int> availableVariants = GetAvailableVariants(cellPos);

            if (availableVariants.Count > 0)
            {
                int variantRoll = variantRng.Next(availableVariants.Count);
                cell.VariantIndex = availableVariants[variantRoll];

                float offsetX = (float)(positionRng.NextDouble() - 0.5) * _cellSize * 0.6f;
                float offsetY = (float)(positionRng.NextDouble() - 0.5) * _cellSize * 0.6f;

                cell.BaseWorldPosition = new Vector3(
                    (cellPos.x + 0.5f) * _cellSize + offsetX,
                    (cellPos.y + 0.5f) * _cellSize + offsetY,
                    0f
                );

                PlanetVariant variant = _variants[cell.VariantIndex];
                cell.Scale = Mathf.Lerp(
                    variant.MinScale,
                    variant.MaxScale,
                    (float)positionRng.NextDouble()
                );

                if (variant.Sprite != null)
                {
                    cell.Renderer = GetOrCreateRenderer();
                    cell.Renderer.sprite = variant.Sprite;
                    cell.Renderer.transform.localScale = Vector3.one * cell.Scale;
                    cell.Renderer.gameObject.SetActive(true);
                    cell.Renderer.transform.position = cell.BaseWorldPosition;
                }
                else
                {
                    cell.HasPlanet = false;
                    cell.VariantIndex = -1;
                }
            }
            else
            {
                cell.HasPlanet = false;
                cell.VariantIndex = -1;
            }
        }

        #endregion

        #region Planet Positioning

        private void UpdatePlanetPositions()
        {
            Vector3 targetDelta = _target.position - _startTargetPosition;

            foreach (var cell in _cells.Values)
            {
                if (cell.Renderer != null && cell.Renderer.gameObject.activeSelf)
                {
                    cell.Renderer.transform.position = new Vector3(
                        cell.BaseWorldPosition.x + targetDelta.x * _parallaxFactor,
                        cell.BaseWorldPosition.y + targetDelta.y * _parallaxFactor,
                        transform.position.z
                    );
                }
            }
        }

        #endregion

        #region Variant Selection

        private List<int> GetAvailableVariants(Vector2Int cellPos)
        {
            HashSet<int> usedVariants = new HashSet<int>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    Vector2Int neighborPos = new Vector2Int(cellPos.x + dx, cellPos.y + dy);

                    if (_cells.TryGetValue(neighborPos, out CellData neighbor))
                    {
                        if (neighbor.HasPlanet && neighbor.VariantIndex >= 0)
                        {
                            usedVariants.Add(neighbor.VariantIndex);
                        }
                    }
                }
            }

            List<int> available = new();
            for (int i = 0; i < _variants.Count; i++)
            {
                if (!usedVariants.Contains(i))
                {
                    available.Add(i);
                }
            }

            // ≈сли все варианты зан€ты сосед€ми Ч разрешаем любой
            if (available.Count == 0)
            {
                for (int i = 0; i < _variants.Count; i++)
                {
                    available.Add(i);
                }
            }

            return available;
        }

        #endregion

        #region Object Pooling

        private SpriteRenderer GetOrCreateRenderer()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }

            GameObject obj = new GameObject("Planet");
            obj.transform.SetParent(transform);

            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = _sortingOrder;

            return renderer;
        }

        #endregion

        #region Utility

        private int HashCell(Vector2Int cellPos)
        {
            unchecked
            {
                int hash = _seed;
                hash = hash * 31 + cellPos.x;
                hash = hash * 31 + cellPos.y;
                hash ^= hash >> 16;
                hash *= unchecked((int)0x85ebca6b);
                hash ^= hash >> 13;
                return hash;
            }
        }

        #endregion
    }
}
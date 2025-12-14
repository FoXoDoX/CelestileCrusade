using System.Collections.Generic;
using UnityEngine;

namespace My.Scripts.Gameplay.Crate
{
    /// <summary>
    /// Визуальная симуляция верёвки методом Verlet Integration.
    /// Не влияет на физику — только отрисовка.
    /// </summary>
    public class RopeVerlet : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Rope Settings")]
        [SerializeField] private int _segmentCount = 30;
        [SerializeField] private float _ropeLength = 5f;

        [Header("Physics Simulation")]
        [SerializeField] private Vector2 _gravity = new(0f, -9.8f);
        [SerializeField] private float _damping = 0.98f;
        [SerializeField] private int _constraintIterations = 50;

        #endregion

        #region Private Fields

        private LineRenderer _lineRenderer;
        private List<RopeSegment> _segments = new();

        private Transform _startPoint;
        private Transform _endPoint;
        private float _segmentLength;
        private bool _isInitialized;

        #endregion

        #region Properties

        public float TotalLength => _ropeLength;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void Update()
        {
            if (!_isInitialized) return;
            DrawRope();
        }

        private void FixedUpdate()
        {
            if (!_isInitialized) return;

            Simulate();
            ApplyConstraints();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализирует верёвку между двумя точками.
        /// </summary>
        public void Initialize(Transform startPoint, Transform endPoint)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _segmentLength = _ropeLength / _segmentCount;

            CreateSegments();
            _isInitialized = true;
        }

        /// <summary>
        /// Отвязывает конечную точку.
        /// </summary>
        public void DetachEndPoint()
        {
            _endPoint = null;
            _isInitialized = false;
        }

        #endregion

        #region Private Methods

        private void CreateSegments()
        {
            _segments.Clear();

            Vector2 start = _startPoint.position;
            Vector2 end = _endPoint.position;

            for (int i = 0; i < _segmentCount; i++)
            {
                float t = (float)i / (_segmentCount - 1);
                Vector2 position = Vector2.Lerp(start, end, t);
                _segments.Add(new RopeSegment(position));
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = _segmentCount;
            }
        }

        private void Simulate()
        {
            // Первый и последний сегменты не симулируем — они привязаны
            for (int i = 1; i < _segments.Count - 1; i++)
            {
                RopeSegment segment = _segments[i];

                Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * _damping;

                segment.OldPosition = segment.CurrentPosition;
                segment.CurrentPosition += velocity;
                segment.CurrentPosition += _gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;

                _segments[i] = segment;
            }
        }

        private void ApplyConstraints()
        {
            for (int iteration = 0; iteration < _constraintIterations; iteration++)
            {
                // Привязка к начальной точке (игрок)
                if (_startPoint != null && _segments.Count > 0)
                {
                    RopeSegment first = _segments[0];
                    first.CurrentPosition = _startPoint.position;
                    _segments[0] = first;
                }

                // Привязка к конечной точке (ящик)
                if (_endPoint != null && _segments.Count > 0)
                {
                    RopeSegment last = _segments[^1];
                    last.CurrentPosition = _endPoint.position;
                    _segments[^1] = last;
                }

                // Ограничения расстояния между сегментами
                for (int i = 0; i < _segments.Count - 1; i++)
                {
                    RopeSegment current = _segments[i];
                    RopeSegment next = _segments[i + 1];

                    float distance = (current.CurrentPosition - next.CurrentPosition).magnitude;
                    float difference = distance - _segmentLength;

                    if (Mathf.Abs(difference) < 0.0001f) continue;

                    Vector2 direction = (current.CurrentPosition - next.CurrentPosition).normalized;
                    Vector2 correction = direction * difference;

                    // Первый и последний сегменты фиксированы
                    if (i == 0)
                    {
                        next.CurrentPosition += correction;
                    }
                    else if (i == _segments.Count - 2)
                    {
                        current.CurrentPosition -= correction;
                    }
                    else
                    {
                        current.CurrentPosition -= correction * 0.5f;
                        next.CurrentPosition += correction * 0.5f;
                    }

                    _segments[i] = current;
                    _segments[i + 1] = next;
                }
            }
        }

        private void DrawRope()
        {
            if (_lineRenderer == null || _segments.Count == 0) return;

            for (int i = 0; i < _segments.Count; i++)
            {
                _lineRenderer.SetPosition(i, _segments[i].CurrentPosition);
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _segmentCount = Mathf.Max(2, _segmentCount);
            _ropeLength = Mathf.Max(0.5f, _ropeLength);
            _damping = Mathf.Clamp01(_damping);
            _constraintIterations = Mathf.Max(1, _constraintIterations);
        }
#endif

        #endregion

        #region Nested Types

        private struct RopeSegment
        {
            public Vector2 CurrentPosition;
            public Vector2 OldPosition;

            public RopeSegment(Vector2 position)
            {
                CurrentPosition = position;
                OldPosition = position;
            }
        }

        #endregion
    }
}
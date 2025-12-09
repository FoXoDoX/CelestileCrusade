using My.Scripts.Gameplay.Crate;
using My.Scripts.Gameplay.Player;
using My.Scripts.Managers;
using UnityEngine;

namespace My.Scripts.Environment.Hazards
{
    public class WindZone2D : MonoBehaviour
    {
        #region Constants

        private const float GIZMO_ZONE_ALPHA = 0.3f;
        private const float GIZMO_ARROW_LENGTH_FACTOR = 0.8f;
        private const float GIZMO_ARROWHEAD_SIZE = 0.3f;
        private const float GIZMO_ARROWHEAD_ANGLE = 30f;

        #endregion

        #region Serialized Fields

        [Header("Wind Settings")]
        [SerializeField] private float _windForce = 10f;
        [SerializeField][Range(0f, 360f)] private float _windDirection = 0f;
        [SerializeField] private ForceMode2D _forceMode = ForceMode2D.Force;

        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _gizmoZoneColor = new(0.5f, 0.8f, 1f, GIZMO_ZONE_ALPHA);
        [SerializeField] private Color _gizmoArrowColor = Color.blue;

        #endregion

        #region Private Fields

        private Vector2 _windVector;
        private Collider2D _collider;
        private int _affectedObjectsCount;

        #endregion

        #region Properties

        public float WindForce => _windForce;
        public float WindDirection => _windDirection;
        public Vector2 WindVector => _windVector;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            UpdateWindVector();
        }

        private void OnValidate()
        {
            UpdateWindVector();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!TryGetAffectedRigidbody(other, out _)) return;

            _affectedObjectsCount++;

            // Включаем звук ветра при первом объекте в зоне
            if (_affectedObjectsCount == 1)
            {
                PlayWindSound();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (TryGetAffectedRigidbody(other, out var rb))
            {
                ApplyWindForce(rb);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryGetAffectedRigidbody(other, out _)) return;

            _affectedObjectsCount = Mathf.Max(0, _affectedObjectsCount - 1);

            // Выключаем звук когда никого нет в зоне
            if (_affectedObjectsCount == 0)
            {
                StopWindSound();
            }
        }

        private void OnDisable()
        {
            // Останавливаем звук при деактивации
            if (_affectedObjectsCount > 0)
            {
                _affectedObjectsCount = 0;
                StopWindSound();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Устанавливает направление ветра в градусах (0 = вправо, 90 = вверх).
        /// </summary>
        public void SetWindDirection(float degrees)
        {
            _windDirection = degrees % 360f;
            UpdateWindVector();
        }

        /// <summary>
        /// Устанавливает силу ветра.
        /// </summary>
        public void SetWindForce(float force)
        {
            _windForce = Mathf.Max(0f, force);
            UpdateWindVector();
        }

        /// <summary>
        /// Устанавливает направление и силу ветра.
        /// </summary>
        public void SetWind(float degrees, float force)
        {
            _windDirection = degrees % 360f;
            _windForce = Mathf.Max(0f, force);
            UpdateWindVector();
        }

        #endregion

        #region Private Methods — Initialization

        private void CacheComponents()
        {
            _collider = GetComponent<Collider2D>();

            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
            else
            {
                Debug.LogWarning($"[{nameof(WindZone2D)}] No Collider2D found!", this);
            }
        }

        private void UpdateWindVector()
        {
            float radians = _windDirection * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Cos(radians), Mathf.Sin(radians));
            _windVector = direction * _windForce;
        }

        #endregion

        #region Private Methods — Physics

        private bool TryGetAffectedRigidbody(Collider2D other, out Rigidbody2D rb)
        {
            rb = null;

            // Проверяем, это Lander или CrateOnRope
            if (other.TryGetComponent(out Lander _) ||
                other.TryGetComponent(out CrateOnRope _))
            {
                rb = other.attachedRigidbody;
                return rb != null;
            }

            return false;
        }

        private void ApplyWindForce(Rigidbody2D rb)
        {
            rb.AddForce(_windVector, _forceMode);
        }

        #endregion

        #region Private Methods — Audio

        private void PlayWindSound()
        {
            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.PlayWindSound();
            }
        }

        private void StopWindSound()
        {
            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.StopWindSound();
            }
        }

        #endregion

        #region Editor — Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;

            var col = _collider != null ? _collider : GetComponent<Collider2D>();
            if (col == null) return;

            DrawZoneGizmo(col);
            DrawWindArrow(col);
        }

        private void DrawZoneGizmo(Collider2D col)
        {
            Gizmos.color = _gizmoZoneColor;

            switch (col)
            {
                case BoxCollider2D box:
                    DrawBoxGizmo(box);
                    break;

                case CircleCollider2D circle:
                    DrawCircleGizmo(circle);
                    break;

                default:
                    DrawBoundsGizmo(col.bounds);
                    break;
            }
        }

        private void DrawBoxGizmo(BoxCollider2D box)
        {
            Vector2 center = transform.TransformPoint(box.offset);
            Vector2 size = Vector2.Scale(box.size, transform.lossyScale);
            Gizmos.DrawCube(center, size);
        }

        private void DrawCircleGizmo(CircleCollider2D circle)
        {
            Vector2 center = transform.TransformPoint(circle.offset);
            float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            Gizmos.DrawSphere(center, circle.radius * maxScale);
        }

        private void DrawBoundsGizmo(Bounds bounds)
        {
            Gizmos.DrawCube(bounds.center, bounds.size);
        }

        private void DrawWindArrow(Collider2D col)
        {
            Gizmos.color = _gizmoArrowColor;

            Vector2 center = GetColliderCenter(col);
            float size = GetColliderSize(col);
            float arrowLength = size * GIZMO_ARROW_LENGTH_FACTOR;

            // Направление стрелки
            Vector3 direction = ((Vector3)_windVector).normalized;
            if (direction == Vector3.zero)
            {
                direction = Vector3.right;
            }

            // Основная линия
            Vector3 start = center;
            Vector3 end = start + direction * arrowLength;
            Gizmos.DrawLine(start, end);

            // Наконечник стрелки
            float headSize = arrowLength * GIZMO_ARROWHEAD_SIZE;
            Vector3 right = Quaternion.Euler(0, 0, GIZMO_ARROWHEAD_ANGLE) * -direction * headSize;
            Vector3 left = Quaternion.Euler(0, 0, -GIZMO_ARROWHEAD_ANGLE) * -direction * headSize;
            Gizmos.DrawLine(end, end + right);
            Gizmos.DrawLine(end, end + left);
        }

        private Vector2 GetColliderCenter(Collider2D col)
        {
            return col switch
            {
                BoxCollider2D box => transform.TransformPoint(box.offset),
                CircleCollider2D circle => transform.TransformPoint(circle.offset),
                _ => col.bounds.center
            };
        }

        private float GetColliderSize(Collider2D col)
        {
            return col switch
            {
                BoxCollider2D box => Mathf.Max(
                    box.size.x * transform.lossyScale.x,
                    box.size.y * transform.lossyScale.y
                ),
                CircleCollider2D circle => circle.radius *
                    Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) * 2f,
                _ => Mathf.Max(col.bounds.size.x, col.bounds.size.y)
            };
        }
#endif

        #endregion
    }
}
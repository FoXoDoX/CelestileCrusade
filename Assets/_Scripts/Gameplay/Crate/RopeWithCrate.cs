using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using UnityEngine;

namespace My.Scripts.Gameplay.Crate
{
    /// <summary>
    ///  онтейнер дл€ верЄвки и €щика. 
    /// ‘изика через Distance Joint, визуал через Verlet.
    /// </summary>
    public class RopeWithCrate : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private RopeVerlet _rope;
        [SerializeField] private CrateOnRope _crate;

        [Header("Settings")]
        [SerializeField] private float _crateSpawnOffset = 2f;

        #endregion

        #region Private Fields

        private bool _isDestroyed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            PositionCrate();
            InitializeRope();
            AttachCrateToPlayer();
            SetupCollisionIgnore();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            BroadcastSpawned();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            DestroySelf();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void CacheComponents()
        {
            if (_rope == null)
            {
                _rope = GetComponentInChildren<RopeVerlet>();
            }

            if (_crate == null)
            {
                _crate = GetComponentInChildren<CrateOnRope>();
            }
        }

        private void PositionCrate()
        {
            if (_crate == null) return;
            if (!Lander.HasInstance) return;

            Vector3 landerPosition = Lander.Instance.transform.position;
            float offset = _rope != null ? _rope.TotalLength : _crateSpawnOffset;

            _crate.transform.position = landerPosition + Vector3.down * offset;
        }

        private void InitializeRope()
        {
            if (_rope == null)
            {
                Debug.LogError($"[{nameof(RopeWithCrate)}] RopeVerlet not found!", this);
                return;
            }

            if (!Lander.HasInstance)
            {
                Debug.LogError($"[{nameof(RopeWithCrate)}] Lander not found!", this);
                return;
            }

            if (_crate == null)
            {
                Debug.LogError($"[{nameof(RopeWithCrate)}] Crate not found!", this);
                return;
            }

            // ¬изуальна€ верЄвка: начало Ч точка прив€зки, конец Ч €щик
            RopeAttachPoint attachPoint = Lander.Instance.RopeAttachPoint;
            Transform startPoint = attachPoint != null
                ? attachPoint.transform
                : Lander.Instance.transform;

            _rope.Initialize(startPoint, _crate.transform);
        }

        private void AttachCrateToPlayer()
        {
            if (_crate == null) return;
            if (!Lander.HasInstance) return;

            // »спользуем точку прив€зки вместо основного Rigidbody
            RopeAttachPoint attachPoint = Lander.Instance.RopeAttachPoint;

            if (attachPoint == null)
            {
                Debug.LogError($"[{nameof(RopeWithCrate)}] Lander has no RopeAttachPoint!", this);
                return;
            }

            float ropeLength = _rope != null ? _rope.TotalLength : _crateSpawnOffset;

            // ‘изическа€ св€зь через Distance Joint к точке прив€зки
            _crate.AttachToPlayer(attachPoint.Rigidbody, ropeLength);
        }

        private void SetupCollisionIgnore()
        {
            if (!Lander.HasInstance) return;

            Collider2D landerCollider = Lander.Instance.GetComponent<Collider2D>();
            if (landerCollider == null) return;

            if (_crate != null)
            {
                var crateColliders = _crate.GetComponents<Collider2D>();
                foreach (var col in crateColliders)
                {
                    Physics2D.IgnoreCollision(landerCollider, col);
                }
            }
        }

        private void BroadcastSpawned()
        {
            EventManager.Instance?.Broadcast(GameEvents.RopeWithCrateSpawned);
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.AddHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);
            em.AddHandler<LanderStateData>(GameEvents.LanderStateChanged, OnLanderStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.RemoveHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);
            em.RemoveHandler<LanderStateData>(GameEvents.LanderStateChanged, OnLanderStateChanged);
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnCrateDrop()
        {
            DestroySelf();
        }

        private void OnCrateDestroyed()
        {
            DestroySelf();
        }

        private void OnLanderStateChanged(LanderStateData data)
        {
            if (data.State == Lander.State.GameOver)
            {
                DestroySelf();
            }
        }

        #endregion

        #region Private Methods Ч Destruction

        private void DestroySelf()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            if (Lander.HasInstance)
            {
                Lander.Instance.ReleaseCrate();
            }

            if (_rope != null)
            {
                _rope.DetachEndPoint();
            }

            if (_crate != null)
            {
                _crate.DetachFromPlayer();
            }

            DisableAllColliders();
            UnsubscribeFromEvents();

            EventManager.Instance?.Broadcast(GameEvents.RopeWithCrateDestroyed);

            Destroy(gameObject);
        }

        private void DisableAllColliders()
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

            foreach (Collider2D collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _crateSpawnOffset = Mathf.Max(0.5f, _crateSpawnOffset);

            if (_rope == null)
            {
                _rope = GetComponentInChildren<RopeVerlet>();
            }

            if (_crate == null)
            {
                _crate = GetComponentInChildren<CrateOnRope>();
            }
        }
#endif

        #endregion
    }
}
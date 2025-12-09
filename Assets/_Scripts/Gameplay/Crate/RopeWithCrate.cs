using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Player;
using UnityEngine;

namespace My.Scripts.Gameplay.Crate
{
    public class RopeWithCrate : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private GameObject _anchor;
        [SerializeField] private GameObject _crate;

        [Header("Settings")]
        [SerializeField] private Vector3 _anchorOffset = Vector3.down;

        #endregion

        #region Private Fields

        private HingeJoint2D _anchorJoint;
        private Collider2D _crateCollider;
        private bool _isDestroyed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            SetupCollisionIgnore();
            BroadcastSpawned();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            DestroySelf();
        }

        private void FixedUpdate()
        {
            if (_isDestroyed) return;

            UpdateAnchorPosition();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void CacheComponents()
        {
            if (_anchor != null)
            {
                _anchorJoint = _anchor.GetComponent<HingeJoint2D>();
            }

            if (_crate != null)
            {
                _crateCollider = _crate.GetComponent<Collider2D>();
            }
        }

        private void SetupCollisionIgnore()
        {
            if (!Lander.HasInstance) return;

            Collider2D landerCollider = Lander.Instance.GetComponent<Collider2D>();
            if (landerCollider == null) return;

            Collider2D[] ropeColliders = GetComponentsInChildren<Collider2D>();

            foreach (Collider2D ropeCollider in ropeColliders)
            {
                // ѕропускаем коллайдер €щика
                if (ropeCollider == _crateCollider) continue;

                // ¬ерЄвка не должна сталкиватьс€ с Lander и €щиком
                Physics2D.IgnoreCollision(landerCollider, ropeCollider);

                if (_crateCollider != null)
                {
                    Physics2D.IgnoreCollision(_crateCollider, ropeCollider);
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

        #region Private Methods Ч Update

        private void UpdateAnchorPosition()
        {
            if (!IsValidAnchor()) return;
            if (!Lander.HasInstance) return;

            _anchor.transform.position = Lander.Instance.transform.position + _anchorOffset;
        }

        private bool IsValidAnchor()
        {
            return _anchor != null &&
                   _anchorJoint != null &&
                   _anchorJoint.enabled;
        }

        #endregion

        #region Private Methods Ч Destruction

        private void DestroySelf()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            // ќсвобождаем €щик у Lander
            if (Lander.HasInstance)
            {
                Lander.Instance.ReleaseCrate();
            }

            // ќтключаем joint
            DisableJoint();

            // ќтключаем все коллайдеры
            DisableAllColliders();

            // ќтписываемс€ от событий
            UnsubscribeFromEvents();

            // —ообщаем об уничтожении
            EventManager.Instance?.Broadcast(GameEvents.RopeWithCrateDestroyed);

            // ”ничтожаем объект
            Destroy(gameObject);
        }

        private void DisableJoint()
        {
            if (_anchorJoint != null)
            {
                _anchorJoint.enabled = false;
            }
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
    }
}
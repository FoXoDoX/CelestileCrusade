using My.Scripts.EventBus;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace My.Scripts.Gameplay.LandingPads
{
    public class CrateLandingPadVisuals : MonoBehaviour
    {
        #region Constants

        private const float SCALE_ANIMATION_DURATION = 1f;

        #endregion

        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Transform _landedCratesContainer;
        [SerializeField] private ParticleSystem _deliveryParticlePrefab;

        [Header("Animation Settings")]
        [SerializeField] private float _scaleAnimationDuration = SCALE_ANIMATION_DURATION;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;

        #endregion

        #region Private Fields

        private readonly HashSet<GameObject> _processedCrates = new();
        private bool _hasValidReferences;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            RegisterExistingActiveCrates();
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
            KillAllTweens();
        }

        #endregion

        #region Private Methods Ч Initialization

        private void ValidateReferences()
        {
            _hasValidReferences = _landedCratesContainer != null;

            if (!_hasValidReferences)
            {
                Debug.LogWarning($"[{nameof(CrateLandingPadVisuals)}] Missing landed crates container!", this);
            }
        }

        private void RegisterExistingActiveCrates()
        {
            if (!_hasValidReferences) return;

            foreach (Transform child in _landedCratesContainer)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    _processedCrates.Add(child.gameObject);
                }
            }
        }

        #endregion

        #region Private Methods Ч Event Subscription

        private void SubscribeToEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.CrateDrop, OnCrateDelivered);
        }

        private void UnsubscribeFromEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler(GameEvents.CrateDrop, OnCrateDelivered);
        }

        #endregion

        #region Private Methods Ч Event Handlers

        private void OnCrateDelivered()
        {
            CheckForNewCrates();
        }

        #endregion

        #region Private Methods Ч Crate Detection

        private void CheckForNewCrates()
        {
            if (!_hasValidReferences) return;

            foreach (Transform child in _landedCratesContainer)
            {
                GameObject crate = child.gameObject;

                if (crate.activeInHierarchy && !_processedCrates.Contains(crate))
                {
                    ProcessNewCrate(crate);
                    _processedCrates.Add(crate);
                }
            }
        }

        #endregion

        #region Private Methods Ч Animation

        private void ProcessNewCrate(GameObject crate)
        {
            if (crate == null) return;

            Transform crateTransform = crate.transform;
            crateTransform.localScale = Vector3.zero;

            crateTransform
                .DOScale(Vector3.one, _scaleAnimationDuration)
                .SetEase(_scaleEase)
                .OnStart(() => SpawnDeliveryParticle(crateTransform.position))
                .SetLink(crate);
        }

        private void SpawnDeliveryParticle(Vector3 position)
        {
            if (_deliveryParticlePrefab == null) return;

            ParticleSystem particle = Instantiate(
                _deliveryParticlePrefab,
                position,
                Quaternion.identity
            );

            particle.Play();

            float particleDuration = particle.main.duration + particle.main.startLifetime.constantMax;
            Destroy(particle.gameObject, particleDuration);
        }

        private void KillAllTweens()
        {
            if (!_hasValidReferences) return;

            foreach (Transform child in _landedCratesContainer)
            {
                child.DOKill();
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            _scaleAnimationDuration = Mathf.Max(0.1f, _scaleAnimationDuration);

            // јвтоматически находим контейнер в родителе
            if (_landedCratesContainer == null)
            {
                var landingPad = GetComponentInParent<CrateLandingPad>();
                if (landingPad != null)
                {
                    // ѕытаемс€ найти через сериализованное поле или дочерний объект
                    _landedCratesContainer = transform.parent?.Find("LandedCrates");
                }
            }
        }
#endif

        #endregion
    }
}
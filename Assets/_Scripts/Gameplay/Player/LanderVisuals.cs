using My.Scripts.Gameplay;
using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using UnityEngine;

namespace My.Scripts.Gameplay.Player
{
    public class LanderVisuals : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Thruster Particles")]
        [SerializeField] private ParticleSystem _leftThrusterParticles;
        [SerializeField] private ParticleSystem _middleThrusterParticles;
        [SerializeField] private ParticleSystem _rightThrusterParticles;

        [Header("Effects")]
        [SerializeField] private GameObject _explosionVfxPrefab;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            DisableAllThrusters();
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
            UnsubscribeFromEvents();
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.LanderBeforeForce, OnBeforeForce);
            em.AddHandler(GameEvents.LanderUpForce, OnUpForce);
            em.AddHandler(GameEvents.LanderLeftForce, OnLeftForce);
            em.AddHandler(GameEvents.LanderRightForce, OnRightForce);
            em.AddHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
        }

        private void UnsubscribeFromEvents()
        {
            var em = EventManager.Instance;
            if (em == null) return;

            em.RemoveHandler(GameEvents.LanderBeforeForce, OnBeforeForce);
            em.RemoveHandler(GameEvents.LanderUpForce, OnUpForce);
            em.RemoveHandler(GameEvents.LanderLeftForce, OnLeftForce);
            em.RemoveHandler(GameEvents.LanderRightForce, OnRightForce);
            em.RemoveHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
        }

        #endregion

        #region Private Methods — Event Handlers

        private void OnBeforeForce()
        {
            DisableAllThrusters();
        }

        private void OnUpForce()
        {
            SetThrusterEnabled(_middleThrusterParticles, true);
        }

        private void OnLeftForce()
        {
            // При повороте влево включается ПРАВЫЙ двигатель
            SetThrusterEnabled(_rightThrusterParticles, true);
        }

        private void OnRightForce()
        {
            // При повороте вправо включается ЛЕВЫЙ двигатель
            SetThrusterEnabled(_leftThrusterParticles, true);
        }

        private void OnLanderLanded(LanderLandedData data)
        {
            if (data.LandingType == Lander.LandingType.Success)
            {
                HandleSuccessfulLanding();
            }
            else
            {
                HandleCrashLanding();
            }
        }

        #endregion

        #region Private Methods — Visual Effects

        private void HandleSuccessfulLanding()
        {
            DisableAllThrusters();
        }

        private void HandleCrashLanding()
        {
            DisableAllThrusters();
            SpawnExplosion();
            HideLander();
        }

        private void SpawnExplosion()
        {
            if (_explosionVfxPrefab == null) return;

            Instantiate(_explosionVfxPrefab, transform.position, Quaternion.identity);
        }

        private void HideLander()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods — Thruster Control

        private void DisableAllThrusters()
        {
            SetThrusterEnabled(_leftThrusterParticles, false);
            SetThrusterEnabled(_middleThrusterParticles, false);
            SetThrusterEnabled(_rightThrusterParticles, false);
        }

        private void SetThrusterEnabled(ParticleSystem thruster, bool enabled)
        {
            if (thruster == null) return;

            var emission = thruster.emission;
            emission.enabled = enabled;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateParticleSystem(_leftThrusterParticles, "Left Thruster");
            ValidateParticleSystem(_middleThrusterParticles, "Middle Thruster");
            ValidateParticleSystem(_rightThrusterParticles, "Right Thruster");
        }

        private void ValidateParticleSystem(ParticleSystem ps, string name)
        {
            if (ps == null)
            {
                Debug.LogWarning($"[{nameof(LanderVisuals)}] {name} particle system is not assigned!", this);
            }
        }
#endif

        #endregion
    }
}
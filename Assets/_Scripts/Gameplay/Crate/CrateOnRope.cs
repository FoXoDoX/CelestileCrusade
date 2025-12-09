using My.Scripts.Core.Data;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.LandingPads;
using My.Scripts.Gameplay.Pickups;
using My.Scripts.Managers;
using System;
using System.Collections;
using UnityEngine;

namespace My.Scripts.Gameplay.Crate
{
    public class CrateOnRope : MonoBehaviour
    {
        #region Constants

        private const float DELAY_FOR_CRATE_DROP = 3f;
        private const float FUEL_PICKUP_AMOUNT = 15f;
        private const int INITIAL_CRATE_HEALTH = 3;

        #endregion

        #region Serialized Fields

        [SerializeField] private Sprite _crackedCrateSprite;
        [SerializeField] private Sprite _veryCrackedCrateSprite;

        #endregion

        #region Private Fields

        private SpriteRenderer _spriteRenderer;
        private CrateLandingPadArea _currentLandingArea;
        private Coroutine _crateDropCoroutine;

        private float _dropTimer;
        private int _crateHealth = INITIAL_CRATE_HEALTH;
        private bool _isInLandingArea;
        private bool _isProgressSoundPlaying;

        #endregion

        #region Events

        // Для внешних подписчиков, которым нужен collider
        public Action<Collider2D> OnCrateCollision;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            EventManager.Instance?.Broadcast(GameEvents.RopeWithCrateSpawned);
        }

        private void OnDisable()
        {
            StopProgressBarSound();
            EventManager.Instance?.Broadcast(GameEvents.RopeWithCrateDestroyed);
        }

        #endregion

        #region Collision Handling

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (TryHandleFuelPickup(other)) return;
            if (TryHandleCoinPickup(other)) return;
            if (TryHandleLandingAreaEnter(other)) return;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            TryHandleLandingAreaExit(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Посадка на площадку не наносит урон
            if (collision.collider.TryGetComponent(out CrateLandingPad _))
                return;

            ApplyDamage();
        }

        #endregion

        #region Public Methods

        public float GetDropProgressNormalized()
        {
            return _dropTimer / DELAY_FOR_CRATE_DROP;
        }

        #endregion

        #region Private Methods — Pickup Handling

        private bool TryHandleCoinPickup(Collider2D other)
        {
            if (!other.TryGetComponent(out CoinPickup coinPickup))
                return false;

            EventManager.Instance?.Broadcast(GameEvents.CoinPickup, new PickupEventData(transform.position));

            coinPickup.DestroySelf();
            return true;
        }

        private bool TryHandleFuelPickup(Collider2D other)
        {
            if (!other.TryGetComponent(out FuelPickup fuelPickup))
                return false;

            EventManager.Instance?.Broadcast(GameEvents.FuelPickup, new PickupEventData(transform.position));

            fuelPickup.DestroySelf();
            return true;
        }

        #endregion

        #region Private Methods — Landing Area

        private bool TryHandleLandingAreaEnter(Collider2D other)
        {
            if (!other.TryGetComponent(out CrateLandingPadArea landingArea))
                return false;

            // Используем свойство LandingPad вместо метода GetLandingPad()
            var landingPad = landingArea.LandingPad;
            if (landingPad == null || !landingPad.CanAcceptCrates)
                return false;

            _currentLandingArea = landingArea;
            _isInLandingArea = true;

            RestartDropCoroutine();
            return true;
        }

        private void TryHandleLandingAreaExit(Collider2D other)
        {
            if (!other.TryGetComponent(out CrateLandingPadArea landingArea))
                return;

            if (_currentLandingArea != landingArea)
                return;

            Debug.Log("Drop canceled");

            CancelDrop();
        }

        private void RestartDropCoroutine()
        {
            if (_crateDropCoroutine != null)
            {
                StopCoroutine(_crateDropCoroutine);
                StopProgressBarSound();
            }

            _crateDropCoroutine = StartCoroutine(DropCrateAfterDelay());
        }

        private void CancelDrop()
        {
            _isInLandingArea = false;

            // Используем свойство LandingPad вместо метода GetLandingPad()
            var landingPad = _currentLandingArea?.LandingPad;
            landingPad?.ResetDeliveryProgress();

            _currentLandingArea = null;
            _dropTimer = 0f;

            if (_crateDropCoroutine != null)
            {
                StopCoroutine(_crateDropCoroutine);
                _crateDropCoroutine = null;
                StopProgressBarSound();
            }
        }

        private IEnumerator DropCrateAfterDelay()
        {
            Debug.Log("Drop started");

            var landingArea = _currentLandingArea;
            // Используем свойство LandingPad вместо метода GetLandingPad()
            var landingPad = landingArea?.LandingPad;

            if (landingPad == null)
                yield break;

            StartProgressBarSound();
            _dropTimer = 0f;

            while (_dropTimer < DELAY_FOR_CRATE_DROP)
            {
                // Проверяем условия отмены
                if (!_isInLandingArea || landingArea == null || !landingPad.CanAcceptCrates)
                {
                    StopProgressBarSound();
                    yield break;
                }

                _dropTimer += Time.deltaTime;
                float progress = _dropTimer / DELAY_FOR_CRATE_DROP;
                landingPad.UpdateDeliveryProgress(progress);

                yield return null;
            }

            // Успешная доставка
            if (landingArea != null && landingPad != null && landingPad.CanAcceptCrates)
            {
                landingArea.RegisterCrateDelivery();
                landingPad.ResetDeliveryProgress();

                DeliverCrate();
            }

            StopProgressBarSound();
            _crateDropCoroutine = null;
            _currentLandingArea = null;
        }

        #endregion

        #region Private Methods — Damage

        private void ApplyDamage()
        {
            _crateHealth--;
            EventManager.Instance?.Broadcast(GameEvents.CrateCracked);

            UpdateCrateVisual();

            if (_crateHealth <= 0)
            {
                DestroyCrate();
            }
        }

        private void UpdateCrateVisual()
        {
            _spriteRenderer.sprite = _crateHealth switch
            {
                2 => _crackedCrateSprite,
                1 => _veryCrackedCrateSprite,
                _ => _spriteRenderer.sprite
            };
        }

        private void DestroyCrate()
        {
            EventManager.Instance?.Broadcast(GameEvents.CrateDestroyed);
            // OnCrateCollision?.Invoke(...); // Если нужно передать collider
        }

        private void DeliverCrate()
        {
            EventManager.Instance?.Broadcast(GameEvents.CrateDrop);
        }

        #endregion

        #region Private Methods — Sound

        private void StartProgressBarSound()
        {
            if (_isProgressSoundPlaying) return;

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.PlayProgressBarSound();
                _isProgressSoundPlaying = true;
            }
        }

        private void StopProgressBarSound()
        {
            if (!_isProgressSoundPlaying) return;

            if (SoundManager.HasInstance)
            {
                SoundManager.Instance.StopProgressBarSound();
                _isProgressSoundPlaying = false;
            }
        }

        #endregion
    }
}
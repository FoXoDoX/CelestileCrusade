using My.Scripts.Core.Data;
using My.Scripts.Core.Utility;
using My.Scripts.Environment.Light;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Crate;
using My.Scripts.Gameplay.KeyDoor;
using My.Scripts.Gameplay.LandingPads;
using My.Scripts.Gameplay.Pickups;
using My.Scripts.Environment.Hazards;
using My.Scripts.Input;
using System.Collections.Generic;
using UnityEngine;

namespace My.Scripts.Gameplay.Player
{
    public class Lander : Singleton<Lander>
    {
        #region Constants

        private const float GRAVITY_NORMAL = 0.8f;
        private const float FORCE_UP = 20f;
        private const float TURN_SPEED = 5f;
        private const float GAMEPAD_DEAD_ZONE = 0.4f;
        private const float SOFT_LANDING_VELOCITY = 5f;
        private const float MIN_DOT_VECTOR = 0.90f;
        private const float MAX_SCORE_PER_CATEGORY = 100f;
        private const float ENERGY_CONSUMPTION_RATE = 1f;

        #endregion

        #region Serialized Fields

        [Header("Prefabs")]
        [SerializeField] private RopeWithCrate _ropeWithCratePrefab;

        [Header("Settings")]
        [SerializeField] private float _energyAmountMax = 30f;

        [Header("Rope")]
        [SerializeField] private RopeAttachPoint _ropeAttachPoint;

        #endregion

        #region Private Fields

        private Rigidbody2D _rigidbody;
        private RopeWithCrate _currentRopeWithCrate;

        private State _currentState;
        private float _energyAmount;

        #endregion

        #region Properties

        public bool HasCrate => _currentRopeWithCrate != null;
        public State CurrentState => _currentState;

        public RopeAttachPoint RopeAttachPoint => _ropeAttachPoint;

        #endregion

        #region Enums

        public enum LandingType
        {
            Success,
            WrongLandingArea,
            TooSteepAngle,
            TooFastLanding,
        }

        public enum State
        {
            WaitingToStart,
            Normal,
            GameOver,
        }

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;

            _energyAmount = _energyAmountMax;
            _currentState = State.WaitingToStart;
        }

        private void FixedUpdate()
        {
            BroadcastEvent(GameEvents.LanderBeforeForce);

            switch (_currentState)
            {
                case State.WaitingToStart:
                    HandleWaitingToStart();
                    break;
                case State.Normal:
                    HandleNormalState();
                    break;
                case State.GameOver:
                    break;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTrigger(other);
        }

        #endregion

        #region Public Methods — Energy

        public float GetEnergy() => _energyAmount;

        public float GetEnergyNormalized() => _energyAmount / _energyAmountMax;

        public void AddEnergy(float amount)
        {
            _energyAmount = Mathf.Clamp(_energyAmount + amount, 0f, _energyAmountMax);
        }

        #endregion

        #region Public Methods — Velocity

        public float GetSpeedX() => _rigidbody != null ? _rigidbody.linearVelocity.x : 0f;

        public float GetSpeedY() => _rigidbody != null ? _rigidbody.linearVelocity.y : 0f;

        public Vector2 GetVelocity() => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;

        #endregion

        #region Public Methods — Crate

        public void HandleCratePickup()
        {
            if (HasCrate) return;

            BroadcastEvent(GameEvents.CratePickup);

            _currentRopeWithCrate = Instantiate(
                _ropeWithCratePrefab,
                transform.position,
                Quaternion.identity
            );

            Debug.Log($"[Lander] Crate picked up, rope spawned at {transform.position}");
        }

        public void ReleaseCrate()
        {
            if (_currentRopeWithCrate == null) return;

            Debug.Log($"[Lander] Crate released");
            _currentRopeWithCrate = null;
        }

        #endregion

        #region Public Methods — Key

        public void HandleKeyDeliver(Key.KeyType keyType)
        {
            BroadcastEvent(GameEvents.KeyDelivered, new KeyDeliveredData(keyType));
        }

        #endregion

        #region Private Methods — State Handling

        private void HandleWaitingToStart()
        {
            if (!IsAnyMovementInput()) return;

            _rigidbody.gravityScale = GRAVITY_NORMAL;
            SetState(State.Normal);
        }

        private void HandleNormalState()
        {
            if (_energyAmount <= 0f) return;
            if (!IsAnyMovementInput()) return;

            ConsumeEnergy();
            ApplyForces();
        }

        private void SetState(State newState)
        {
            _currentState = newState;
            BroadcastEvent(GameEvents.LanderStateChanged, new LanderStateData(newState));
        }

        #endregion

        #region Private Methods — Input & Movement

        private bool IsAnyMovementInput()
        {
            if (!GameInput.HasInstance) return false;

            var input = GameInput.Instance;
            return input.IsUpActionPressed() ||
                   input.IsLeftActionPressed() ||
                   input.IsRightActionPressed() ||
                   input.GetMovementInputVector2() != Vector2.zero;
        }

        private void ApplyForces()
        {
            if (!GameInput.HasInstance) return;

            var input = GameInput.Instance;
            Vector2 movementInput = input.GetMovementInputVector2();

            if (input.IsUpActionPressed() || movementInput.y > GAMEPAD_DEAD_ZONE)
            {
                _rigidbody.AddForce(FORCE_UP * transform.up);
                BroadcastEvent(GameEvents.LanderUpForce);
            }

            if (input.IsLeftActionPressed() || movementInput.x < -GAMEPAD_DEAD_ZONE)
            {
                _rigidbody.AddTorque(TURN_SPEED);
                BroadcastEvent(GameEvents.LanderLeftForce);
            }

            if (input.IsRightActionPressed() || movementInput.x > GAMEPAD_DEAD_ZONE)
            {
                _rigidbody.AddTorque(-TURN_SPEED);
                BroadcastEvent(GameEvents.LanderRightForce);
            }
        }

        private void ConsumeEnergy()
        {
            _energyAmount -= ENERGY_CONSUMPTION_RATE * Time.deltaTime;
            _energyAmount = Mathf.Max(_energyAmount, 0f);
        }

        #endregion

        #region Private Methods — Collision

        private void HandleCollision(Collision2D collision)
        {
            if (!collision.gameObject.TryGetComponent(out LandingPad landingPad))
            {
                CrashLanding(LandingType.WrongLandingArea);
                return;
            }

            float relativeVelocity = collision.relativeVelocity.magnitude;

            if (relativeVelocity > SOFT_LANDING_VELOCITY)
            {
                BroadcastLanded(LanderLandedData.CrashedTooFast(relativeVelocity));
                SetState(State.GameOver);
                return;
            }

            float dotVector = Vector2.Dot(Vector2.up, transform.up);
            if (dotVector < MIN_DOT_VECTOR)
            {
                BroadcastLanded(LanderLandedData.CrashedBadAngle(dotVector, relativeVelocity));
                SetState(State.GameOver);
                return;
            }

            SuccessfulLanding(landingPad, dotVector, relativeVelocity);
        }

        private void CrashLanding(LandingType reason)
        {
            Debug.Log($"Crashed: {reason}");
            BroadcastLanded(LanderLandedData.Crashed(reason));
            SetState(State.GameOver);
        }

        private void SuccessfulLanding(LandingPad landingPad, float dotVector, float relativeVelocity)
        {
            Destroy(_rigidbody);

            int score = CalculateLandingScore(dotVector, relativeVelocity, landingPad.ScoreMultiplier);

            Debug.Log($"Landed! Score: {score}");

            BroadcastLanded(LanderLandedData.Success(
                score,
                dotVector,
                relativeVelocity,
                landingPad.ScoreMultiplier
            ));

            SetState(State.GameOver);
        }

        private int CalculateLandingScore(float dotVector, float relativeVelocity, float multiplier)
        {
            float anglePercentage = (dotVector - MIN_DOT_VECTOR) / (1f - MIN_DOT_VECTOR);
            anglePercentage = Mathf.Clamp01(anglePercentage);
            float angleScore = anglePercentage * MAX_SCORE_PER_CATEGORY;

            float speedPercentage = 1f - (relativeVelocity / SOFT_LANDING_VELOCITY);
            speedPercentage = Mathf.Clamp01(speedPercentage);
            float speedScore = speedPercentage * MAX_SCORE_PER_CATEGORY;

            float totalScore = (angleScore + speedScore) * multiplier;

            return Mathf.RoundToInt(totalScore);
        }

        #endregion

        #region Private Methods — Triggers

        private void HandleTrigger(Collider2D other)
        {
            if (TryHandleEnergyBookPickup(other)) return;
            if (TryHandleCoinPickup(other)) return;
        }

        private bool TryHandleCoinPickup(Collider2D other)
        {
            if (!other.TryGetComponent(out CoinPickup coinPickup))
                return false;

            EventManager.Instance?.Broadcast(GameEvents.CoinPickup, new PickupEventData(transform.position));

            coinPickup.DestroySelf();
            return true;
        }

        private bool TryHandleEnergyBookPickup(Collider2D other)
        {
            if (!other.TryGetComponent(out EnergyBookPickup energyBookPickup))
                return false;

            energyBookPickup.Pickup();
            return true;
        }

        #endregion

        #region Private Methods — Event Broadcasting

        private void BroadcastEvent(GameEvents gameEvent)
        {
            EventManager.Instance?.Broadcast(gameEvent);
        }

        private void BroadcastEvent<T>(GameEvents gameEvent, T data)
        {
            EventManager.Instance?.Broadcast(gameEvent, data);
        }

        private void BroadcastLanded(LanderLandedData data)
        {
            BroadcastEvent(GameEvents.LanderLanded, data);
        }

        #endregion
    }
}
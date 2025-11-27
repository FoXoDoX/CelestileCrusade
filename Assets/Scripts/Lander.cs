using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Lander : MonoBehaviour
{
    private const float GRAVITY_NORMAL = 0.8f;

    [SerializeField] private RopeWithCrate ropeWithCrate;

    public static Lander Instance { get; private set; }

    public event EventHandler OnUpForce;
    public event EventHandler OnRightForce;
    public event EventHandler OnLeftForce;
    public event EventHandler OnBeforeForce;
    public event EventHandler OnCoinPickup;
    public event EventHandler OnFuelPickup;
    public event EventHandler OnCratePickup;
    public event EventHandler<OnKeyDeliverEventArgs> OnKeyDeliver;
    public class OnKeyDeliverEventArgs : EventArgs
    {
        public Key.KeyType DeliveredKeyType { get; set; }
    }
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }
    public event EventHandler<OnLandedEventArgs> OnLanded;
    public class OnLandedEventArgs : EventArgs
    {
        public LandingType landingType;
        public int score;
        public float dotVector;
        public float landingSpeed;
        public float scoreMultiplier;
    }

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

    private Rigidbody2D landerRigidBody2D;
    private float fuelAmount;
    private float fuelAmountMax = 30f;
    private State state;
    private RopeWithCrate currentRopeWithCrate;

    public bool HasCrate => currentRopeWithCrate != null;

    private void Awake()
    {
        Instance = this;

        fuelAmount = fuelAmountMax;
        state = State.WaitingToStart;

        landerRigidBody2D = GetComponent<Rigidbody2D>();
        landerRigidBody2D.gravityScale = 0f;
    }

    private void FixedUpdate()
    {
        OnBeforeForce?.Invoke(this, EventArgs.Empty);

        switch (state)
        {
            case State.WaitingToStart:
                if (GameInput.Instance.IsUpActionPressed() ||
                    GameInput.Instance.IsLeftActionPressed() ||
                    GameInput.Instance.IsRightActionPressed() ||
                    GameInput.Instance.GetMovementInputVector2() != Vector2.zero)
                {
                    landerRigidBody2D.gravityScale = GRAVITY_NORMAL;
                    SetState(State.Normal);
                }
                break;
            case State.Normal:
                if (fuelAmount <= 0f)
                {
                    return;
                }

                if (GameInput.Instance.IsUpActionPressed() ||
                    GameInput.Instance.IsLeftActionPressed() ||
                    GameInput.Instance.IsRightActionPressed() ||
                    GameInput.Instance.GetMovementInputVector2() != Vector2.zero)
                {
                    ConsumeFuel();
                }

                float gamepadDeadZone = .4f;
                if (GameInput.Instance.IsUpActionPressed() || GameInput.Instance.GetMovementInputVector2().y > gamepadDeadZone)
                {
                    float force = 20f;
                    landerRigidBody2D.AddForce(force * transform.up);
                    OnUpForce?.Invoke(this, EventArgs.Empty);
                }
                if (GameInput.Instance.IsLeftActionPressed() || GameInput.Instance.GetMovementInputVector2().x < -gamepadDeadZone)
                {
                    float turnSpeed = +3f;
                    landerRigidBody2D.AddTorque(turnSpeed);
                    OnLeftForce?.Invoke(this, EventArgs.Empty);
                }
                if (GameInput.Instance.IsRightActionPressed() || GameInput.Instance.GetMovementInputVector2().x > gamepadDeadZone)
                {
                    float turnSpeed = -3f;
                    landerRigidBody2D.AddTorque(turnSpeed);
                    OnRightForce?.Invoke(this, EventArgs.Empty);
                }
                break;
            case State.GameOver:
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (!collision2D.gameObject.TryGetComponent(out LandingPad landingPad))
        {
            Debug.Log("Crashed on the Terrain!");
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.WrongLandingArea,
                dotVector = 0f,
                landingSpeed = 0f,
                scoreMultiplier = 0,
                score = 0
            });
            SetState(State.GameOver);
            return;
        }

        float softLandingVelocityMagnitude = 5f;
        float relativeVelocityMagnitude = collision2D.relativeVelocity.magnitude;

        if (relativeVelocityMagnitude > softLandingVelocityMagnitude)
        {
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.TooFastLanding,
                dotVector = 0f,
                landingSpeed = relativeVelocityMagnitude,
                scoreMultiplier = 0,
                score = 0
            });
            SetState(State.GameOver);
            return;
        }

        float dotVector = Vector2.Dot(Vector2.up, transform.up);
        float minDDotVector = .90f;

        if (dotVector < minDDotVector)
        {
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.TooSteepAngle,
                dotVector = dotVector,
                landingSpeed = relativeVelocityMagnitude,
                scoreMultiplier = 0,
                score = 0
            });
            SetState(State.GameOver);
            return;
        }

        Destroy(landerRigidBody2D);

        float maxScoreAmountLandingAngle = 100f;
        float scoreDotLandingMultiplier = 10f;
        float landingAngleScore = maxScoreAmountLandingAngle - Mathf.Abs(dotVector - 1f) * scoreDotLandingMultiplier * maxScoreAmountLandingAngle;

        float maxScoreAmountLandingSpeed = 100f;
        float landingSpeedScore = (softLandingVelocityMagnitude - relativeVelocityMagnitude) * maxScoreAmountLandingSpeed;

        int score = Mathf.RoundToInt((landingAngleScore + landingSpeedScore) * landingPad.ScoreMultiplier);

        Debug.Log("Score:" + score);

        OnLanded?.Invoke(this, new OnLandedEventArgs
        {
            landingType = LandingType.Success,
            dotVector = dotVector,
            landingSpeed = relativeVelocityMagnitude,
            scoreMultiplier = landingPad.ScoreMultiplier,
            score = score
        });
        SetState(State.GameOver);
    }

    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.gameObject.TryGetComponent(out FuelPickup fuelPickup))
        {
            float addFuelAmountAfterFuelPickup = 15f;
            SetFuel(addFuelAmountAfterFuelPickup);
            OnFuelPickup?.Invoke(this, EventArgs.Empty);
            fuelPickup.DestroySelf();
        }

        if (collider2D.gameObject.TryGetComponent(out CoinPickup coinPickup))
        {
            OnCoinPickup?.Invoke(this, EventArgs.Empty);
            coinPickup.DestroySelf();
        }
    }

    public void HandleCratePickup()
    {
        if (HasCrate) return;

        OnCratePickup?.Invoke(this, EventArgs.Empty);

        currentRopeWithCrate = Instantiate(ropeWithCrate, transform.position, Quaternion.identity);
    }

    public void HandleKeyDeliver(Key.KeyType keyType)
    {
        OnKeyDeliver?.Invoke(this, new OnKeyDeliverEventArgs { DeliveredKeyType = keyType });
    }

    public void ReleaseCrate()
    {
        currentRopeWithCrate = null;
    }

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs { state = state });
    }

    private void ConsumeFuel()
    {
        float fuelConsumtionAmount = 1f;
        fuelAmount -= fuelConsumtionAmount * Time.deltaTime;
    }

    public void SetFuel(float addFuelAmount)
    {
        fuelAmount += addFuelAmount;
        if (fuelAmount > fuelAmountMax)
        {
            fuelAmount = fuelAmountMax;
        }
    }

    public float GetFuel()
    {
        return fuelAmount;
    }

    public float GetFuelAmountNormalized()
    {
        return fuelAmount / fuelAmountMax;
    }

    public float GetSpeedX()
    {
        return landerRigidBody2D.linearVelocityX;
    }

    public float GetSpeedY()
    {
        return landerRigidBody2D.linearVelocityY;
    }
}
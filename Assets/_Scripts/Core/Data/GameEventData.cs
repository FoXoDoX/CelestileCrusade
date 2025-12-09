using My.Scripts.Gameplay.Player;
using My.Scripts.Gameplay.KeyDoor;
using UnityEngine;

namespace My.Scripts.Core.Data
{
    /// <summary>
    /// Данные события приземления Lander
    /// </summary>
    public readonly struct LanderLandedData
    {
        public Lander.LandingType LandingType { get; }
        public int Score { get; }
        public float DotVector { get; }
        public float LandingSpeed { get; }
        public float ScoreMultiplier { get; }

        public LanderLandedData(
            Lander.LandingType landingType,
            int score = 0,
            float dotVector = 0f,
            float landingSpeed = 0f,
            float scoreMultiplier = 0f)
        {
            LandingType = landingType;
            Score = score;
            DotVector = dotVector;
            LandingSpeed = landingSpeed;
            ScoreMultiplier = scoreMultiplier;
        }

        // Фабричные методы для удобства
        public static LanderLandedData Crashed(Lander.LandingType reason)
            => new(reason);

        public static LanderLandedData CrashedTooFast(float speed)
            => new(Lander.LandingType.TooFastLanding, landingSpeed: speed);

        public static LanderLandedData CrashedBadAngle(float dotVector, float speed)
            => new(Lander.LandingType.TooSteepAngle, dotVector: dotVector, landingSpeed: speed);

        public static LanderLandedData Success(int score, float dotVector, float speed, float multiplier)
            => new(Lander.LandingType.Success, score, dotVector, speed, multiplier);
    }

    /// <summary>
    /// Данные события смены состояния Lander
    /// </summary>
    public readonly struct LanderStateData
    {
        public Lander.State State { get; }

        public LanderStateData(Lander.State state)
        {
            State = state;
        }
    }

    /// <summary>
    /// Данные события доставки ключа
    /// </summary>
    public readonly struct KeyDeliveredData
    {
        public Key.KeyType KeyType { get; }

        public KeyDeliveredData(Key.KeyType keyType)
        {
            KeyType = keyType;
        }
    }

    public readonly struct PickupEventData
    {
        public Vector3 Position { get; }

        public PickupEventData(Vector3 position)
        {
            Position = position;
        }
    }

    public readonly struct LevelCompletedData
    {
        public readonly bool IsSuccess;
        public readonly int TotalScore;
        public readonly int StarsEarned;
        public readonly int LandingScore;
        public readonly float LandingSpeed;
        public readonly float DotVector;
        public readonly float ScoreMultiplier;

        public LevelCompletedData(
            bool isSuccess,
            int totalScore,
            int starsEarned,
            int landingScore,
            float landingSpeed,
            float dotVector,
            float scoreMultiplier)
        {
            IsSuccess = isSuccess;
            TotalScore = totalScore;
            StarsEarned = starsEarned;
            LandingScore = landingScore;
            LandingSpeed = landingSpeed;
            DotVector = dotVector;
            ScoreMultiplier = scoreMultiplier;
        }
    }
}
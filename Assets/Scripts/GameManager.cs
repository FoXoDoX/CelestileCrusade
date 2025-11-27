using DG.Tweening;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public const int SCORE_PER_COIN = 100;
    public const int SCORE_PER_CRATE = 500;

    public static GameManager Instance { get; private set; }

    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;

    [SerializeField] private List<GameLevel> gameLevelList;
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private int score;
    private float time;
    private bool isTimerActive;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Lander.Instance.OnCoinPickup += Lander_OnCoinPickup;
        Lander.Instance.OnLanded += Lander_OnLanded;
        Lander.Instance.OnStateChanged += Lander_OnStateChanged;

        GameInput.Instance.OnMenuButtonPressed += GameInput_OnMenuButtonPressed;

        LoadCurrentLevel();
    }

    private void GameInput_OnMenuButtonPressed(object sender, System.EventArgs e)
    {
        PauseUnpauseGame();
    }

    private void Lander_OnStateChanged(object sender, Lander.OnStateChangedEventArgs e)
    {
        isTimerActive = e.state == Lander.State.Normal;

        if (e.state == Lander.State.Normal)
        {
            cinemachineCamera.Target.TrackingTarget = Lander.Instance.transform;
            CinemachineCameraZoom2D.Instance.SetNormalOrthographicSize();
        }
    }

    private void Update()
    {
        if (GameInput.Instance.IsRestartActionPressed())
        {
            RetryLevel();
        }
        if (isTimerActive)
        {
            time += Time.deltaTime;
        }
    }

    private void LoadCurrentLevel()
    {
        GameLevel gameLevel = GetGameLevel();
        GameLevel spawnedGameLevel = Instantiate(gameLevel, Vector3.zero, Quaternion.identity);
        Lander.Instance.transform.position = spawnedGameLevel.GetLanderStartPosition();
        cinemachineCamera.Target.TrackingTarget = spawnedGameLevel.GetCameraStartTargetTransform();
        CinemachineCameraZoom2D.Instance.SetTargetOrthographicSize(spawnedGameLevel.GetZoomedOutOrthographicSize());
    }

    private GameLevel GetGameLevel()
    {
        foreach (GameLevel gameLevel in gameLevelList)
        {
            if (gameLevel.GetLevelNumber() == GameData.CurrentLevel)
            {
                return gameLevel;                
            }
        }
        return null;
    }

    public GameLevel GetCurrentLevelObject()
    {
        return GetGameLevel();
    }

    private void Lander_OnLanded(object sender, Lander.OnLandedEventArgs e)
    {
        AddScore(e.score);

        if (e.landingType == Lander.LandingType.Success)
        {
            GameLevel currentLevel = GetCurrentLevelObject();
            int starsEarned = currentLevel.GetEarnedStarsCount(score);

            GameData.MarkLevelCompleted(GameData.CurrentLevel, starsEarned);
        }
    }

    private void Lander_OnCoinPickup(object sender, System.EventArgs e)
    {
        AddScore(SCORE_PER_COIN);
    }

    private void CrateOnRope_OnCoinPickup(object sender, System.EventArgs e)
    {
        AddScore(SCORE_PER_COIN);
    }

    private void CrateOnRope_OnCrateDrop(object sender, System.EventArgs e)
    {
        AddScore(SCORE_PER_CRATE);
    }

    public void AddScore(int addScoreAmount)
    {
        score += addScoreAmount;
        Debug.Log(score);
    }

    public void RopeWithCrateSpawned()
    {
        CrateOnRope.Instance.OnCoinPickup += CrateOnRope_OnCoinPickup;
        CrateOnRope.Instance.OnCrateDrop += CrateOnRope_OnCrateDrop;
    }

    public int GetScore()
    {
        return score;
    }
    
    public float GetTime()
    {
        return time;
    }

    public int GetTotalScore()
    {
        return GameData.TotalScore;
    }

    public void GoToNextLevel()
    {
        GameData.CurrentLevel++;
        GameData.TotalScore += score;

        DOTween.KillAll();

        if (GetGameLevel() == null)
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GameOverScene);
        }
        else
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        }            
    }

    public void RetryLevel()
    {
        DOTween.KillAll();

        SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
    }

    public void PauseUnpauseGame()
    {
        if (Time.timeScale == 1f)
        {
            PauseGame();
        }
        else
        {
            UnpauseGame();
        }
    }


    public void PauseGame()
    {
        Time.timeScale = 0f;
        OnGamePaused?.Invoke(this, EventArgs.Empty);
    }

    public void UnpauseGame()
    {
        Time.timeScale = 1f;
        OnGameUnpaused?.Invoke(this, EventArgs.Empty);
    }
}

public static class GameData
{
    private static int currentLevel = 1;
    private static int totalScore = 0;
    private static int highestCompletedLevel = 0;
    private static Dictionary<int, int> levelStars = new Dictionary<int, int>();

    public static int CurrentLevel
    {
        get => currentLevel;
        set
        {
            if (value > 0)
                currentLevel = value;
        }
    }

    public static int TotalScore
    {
        get => totalScore;
        set
        {
            if (value >= 0)
                totalScore = value;
        }
    }

    public static void MarkLevelCompleted(int completedLevelNumber, int starsEarned)
    {
        Debug.Log($"MarkLevelCompleted called: level={completedLevelNumber}, stars={starsEarned}");

        if (completedLevelNumber > highestCompletedLevel)
        {
            highestCompletedLevel = completedLevelNumber;
            Debug.Log($"New highest completed level: {highestCompletedLevel}");
        }

        // Сохраняем максимальное количество звёзд для уровня
        if (!levelStars.ContainsKey(completedLevelNumber) || starsEarned > levelStars[completedLevelNumber])
        {
            levelStars[completedLevelNumber] = starsEarned;
            Debug.Log($"Updated stars for level {completedLevelNumber}: {starsEarned}");
        }
        else
        {
            Debug.Log($"Stars for level {completedLevelNumber} already at maximum: {levelStars[completedLevelNumber]}");
        }

        SaveSystem.Save();
    }

    public static bool IsLevelAvailable(int levelNumber)
    {
        return levelNumber == 1 || levelNumber <= highestCompletedLevel + 1;
    }

    public static int GetStarsForLevel(int levelNumber)
    {
        int stars = levelStars.ContainsKey(levelNumber) ? levelStars[levelNumber] : 0;
        Debug.Log($"GetStarsForLevel: level={levelNumber}, stars={stars}");
        return stars;
    }

    public static void ResetStaticData()
    {
        currentLevel = 1;
        totalScore = 0;
        // Не сбрасываем levelStars и highestCompletedLevel, так как это постоянный прогресс
    }

    public static int GetHighestCompletedLevel()
    {
        return highestCompletedLevel;
    }

    public static void SetHighestCompletedLevel(int _highestCompletedLevel)
    {
        highestCompletedLevel = _highestCompletedLevel;
    }

    public static void Save(ref GameSaveData data)
    {
        data.highestCompletedLevel = highestCompletedLevel;

        // Преобразуем Dictionary в List для сериализации
        data.levelStarsData = new List<LevelStarData>();
        foreach (var kvp in levelStars)
        {
            data.levelStarsData.Add(new LevelStarData
            {
                levelNumber = kvp.Key,
                starsCount = kvp.Value
            });
        }

        Debug.Log($"Saved {levelStars.Count} level stars entries");
        Debug.Log($"Saved highest completed level: {highestCompletedLevel}");
    }

    public static void Load(GameSaveData data)
    {
        highestCompletedLevel = data.highestCompletedLevel;

        // Преобразуем List обратно в Dictionary
        levelStars = new Dictionary<int, int>();
        if (data.levelStarsData != null)
        {
            foreach (var starData in data.levelStarsData)
            {
                levelStars[starData.levelNumber] = starData.starsCount;
            }
            Debug.Log($"Loaded {levelStars.Count} level stars entries");
        }
        else
        {
            Debug.Log("No level stars data found in save file");
        }

        Debug.Log($"Loaded highest completed level: {highestCompletedLevel}");
    }
}

[System.Serializable]
public struct GameSaveData
{
    public int highestCompletedLevel;
    public List<LevelStarData> levelStarsData;
}

[System.Serializable]
public struct LevelStarData
{
    public int levelNumber;
    public int starsCount;
}
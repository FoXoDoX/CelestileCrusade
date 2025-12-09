using My.Scripts.Core.Data;
using My.Scripts.Core.Scene;
using My.Scripts.Core.Utility;
using My.Scripts.EventBus;
using My.Scripts.Gameplay.Levels;
using My.Scripts.Gameplay.Player;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace My.Scripts.Managers
{
    public class GameManager : PersistentSingleton<GameManager>
    {
        #region Constants

        public const int SCORE_PER_COIN = 100;
        public const int SCORE_PER_CRATE = 500;

        #endregion

        #region Serialized Fields

        [Header("Level Configuration")]
        [SerializeField] private List<GameLevel> _gameLevelList;

        [Header("Camera")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;

        #endregion

        #region Private Fields

        private GameLevel _currentGameLevel;
        private GameLevel _spawnedLevelInstance;

        private int _score;
        private float _time;
        private bool _isTimerActive;
        private bool _isPaused;
        private bool _isSubscribedToEvents;

        #endregion

        #region Properties

        public int Score => _score;
        public float Time => _time;
        public bool IsPaused => _isPaused;

        #endregion

        #region Unity Lifecycle

        protected override void OnSingletonAwake()
        {
            // Подписываемся на событие загрузки сцены
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            HandleTimerUpdate();
        }

        #endregion

        #region Private Methods — Scene Loading

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameManager] Scene loaded: {scene.name}");

            // Проверяем, что это игровая сцена
            if (scene.name == SceneLoader.Scene.GameScene.ToString())
            {
                InitializeLevel();
            }
            else
            {
                // На других сценах отписываемся от игровых событий
                UnsubscribeFromEvents();
            }
        }

        private void InitializeLevel()
        {
            Debug.Log($"[GameManager] Initializing level {GameData.CurrentLevel}");

            // Сбрасываем состояние для нового уровня
            ResetLevelState();

            // Находим уровень по номеру
            _currentGameLevel = FindGameLevelByNumber(GameData.CurrentLevel);

            if (_currentGameLevel == null)
            {
                Debug.LogError($"[GameManager] Level {GameData.CurrentLevel} not found!");
                return;
            }

            // Ищем камеру на новой сцене
            FindCamera();

            // Загружаем уровень
            LoadCurrentLevel();

            // Подписываемся на события
            SubscribeToEvents();
        }

        private void ResetLevelState()
        {
            _score = 0;
            _time = 0f;
            _isTimerActive = false;
            _isPaused = false;

            // Убеждаемся что timeScale нормальный
            UnityEngine.Time.timeScale = 1f;
        }

        private void FindCamera()
        {
            // Если камера не назначена или была уничтожена, ищем на сцене
            if (_cinemachineCamera == null)
            {
                _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

                if (_cinemachineCamera == null)
                {
                    Debug.LogError("[GameManager] CinemachineCamera not found on scene!");
                }
            }
        }

        #endregion

        #region Public Methods — Score

        public void AddScore(int amount)
        {
            if (amount <= 0) return;

            _score += amount;
            Debug.Log($"Score: {_score}");

            EventManager.Instance?.Broadcast(GameEvents.ScoreChanged, _score);
        }

        public int GetTotalScore() => GameData.TotalScore;

        #endregion

        #region Public Methods — Level Management

        public GameLevel GetCurrentLevelObject() => _currentGameLevel;

        public void GoToNextLevel()
        {
            Debug.Log($"[GameManager] GoToNextLevel: {GameData.CurrentLevel} → {GameData.CurrentLevel + 1}");

            GameData.CurrentLevel++;
            GameData.TotalScore += _score;

            DOTween.KillAll();

            var nextLevel = FindGameLevelByNumber(GameData.CurrentLevel);
            var targetScene = nextLevel != null
                ? SceneLoader.Scene.GameScene
                : SceneLoader.Scene.GameOverScene;

            Debug.Log($"[GameManager] Loading scene: {targetScene}");
            SceneLoader.LoadScene(targetScene);
        }

        public void RetryLevel()
        {
            DOTween.KillAll();
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        }

        #endregion

        #region Public Methods — Pause

        public void TogglePause()
        {
            if (_isPaused)
                UnpauseGame();
            else
                PauseGame();
        }

        public void PauseGame()
        {
            if (_isPaused) return;

            _isPaused = true;
            UnityEngine.Time.timeScale = 0f;

            EventManager.Instance?.Broadcast(GameEvents.GamePaused);
        }

        public void UnpauseGame()
        {
            if (!_isPaused) return;

            _isPaused = false;
            UnityEngine.Time.timeScale = 1f;

            EventManager.Instance?.Broadcast(GameEvents.GameUnpaused);
        }

        #endregion

        #region Private Methods — Event Subscription

        private void SubscribeToEvents()
        {
            if (_isSubscribedToEvents) return;

            var em = EventManager.Instance;
            if (em == null) return;

            em.AddHandler(GameEvents.MenuButtonPressed, OnMenuButtonPressed);
            em.AddHandler(GameEvents.RestartButtonPressed, OnRestartButtonPressed);
            em.AddHandler<PickupEventData>(GameEvents.CoinPickup, OnCoinPickup);
            em.AddHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
            em.AddHandler<LanderStateData>(GameEvents.LanderStateChanged, OnLanderStateChanged);
            em.AddHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.AddHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.AddHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);

            _isSubscribedToEvents = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribedToEvents) return;
            if (!EventManager.HasInstance) return;

            var em = EventManager.Instance;

            em.RemoveHandler(GameEvents.MenuButtonPressed, OnMenuButtonPressed);
            em.RemoveHandler(GameEvents.RestartButtonPressed, OnRestartButtonPressed);
            em.RemoveHandler<PickupEventData>(GameEvents.CoinPickup, OnCoinPickup);
            em.RemoveHandler<LanderLandedData>(GameEvents.LanderLanded, OnLanderLanded);
            em.RemoveHandler<LanderStateData>(GameEvents.LanderStateChanged, OnLanderStateChanged);
            em.RemoveHandler(GameEvents.RopeWithCrateSpawned, OnRopeWithCrateSpawned);
            em.RemoveHandler(GameEvents.CrateDrop, OnCrateDrop);
            em.RemoveHandler(GameEvents.CrateDestroyed, OnCrateDestroyed);

            _isSubscribedToEvents = false;
        }

        #endregion

        #region Private Methods — Event Handlers

        private void OnMenuButtonPressed()
        {
            TogglePause();
        }

        private void OnRestartButtonPressed()
        {
            RetryLevel();
        }

        private void OnCoinPickup(PickupEventData data)
        {
            AddScore(SCORE_PER_COIN);
        }

        private void OnLanderLanded(LanderLandedData data)
        {
            // Сначала добавляем очки
            AddScore(data.Score);

            // Теперь _score содержит полный счёт уровня
            int starsEarned = 0;

            if (data.LandingType == Lander.LandingType.Success && _currentGameLevel != null)
            {
                starsEarned = _currentGameLevel.GetEarnedStarsCount(_score);
                GameData.MarkLevelCompleted(GameData.CurrentLevel, starsEarned);
            }

            // Отправляем событие с полными данными
            var completedData = new LevelCompletedData(
                isSuccess: data.LandingType == Lander.LandingType.Success,
                totalScore: _score,
                starsEarned: starsEarned,
                landingScore: data.Score,
                landingSpeed: data.LandingSpeed,
                dotVector: data.DotVector,
                scoreMultiplier: data.ScoreMultiplier
            );

            EventManager.Instance?.Broadcast(GameEvents.LevelCompleted, completedData);
        }

        private void OnLanderStateChanged(LanderStateData data)
        {
            _isTimerActive = data.State == Lander.State.Normal;

            if (data.State == Lander.State.Normal && Lander.HasInstance && _cinemachineCamera != null)
            {
                _cinemachineCamera.Target.TrackingTarget = Lander.Instance.transform;

                if (CinemachineCameraZoom2D.HasInstance && _currentGameLevel != null)
                {
                    CinemachineCameraZoom2D.Instance.SetNormalOrthographicSize(
                        _currentGameLevel.GetNormalOrthographicSize()
                    );
                }
            }
        }

        private void OnRopeWithCrateSpawned()
        {
            if (CinemachineCameraZoom2D.HasInstance && _currentGameLevel != null)
            {
                float zoomOutSize = _currentGameLevel.GetNormalOrthographicSize() + 6f;
                CinemachineCameraZoom2D.Instance.SetNormalOrthographicSize(zoomOutSize);
            }
        }

        private void OnCrateDrop()
        {
            AddScore(SCORE_PER_CRATE);
            ResetCameraZoom();
        }

        private void OnCrateDestroyed()
        {
            ResetCameraZoom();
        }

        #endregion

        #region Private Methods — Game Logic

        private void HandleTimerUpdate()
        {
            if (_isTimerActive)
            {
                _time += UnityEngine.Time.deltaTime;
            }
        }

        private void LoadCurrentLevel()
        {
            if (_currentGameLevel == null)
            {
                Debug.LogError($"[GameManager] Level {GameData.CurrentLevel} not found!");
                return;
            }

            Debug.Log($"[GameManager] Loading level {_currentGameLevel.GetLevelNumber()}");

            // Уничтожаем предыдущий инстанс если есть
            if (_spawnedLevelInstance != null)
            {
                Destroy(_spawnedLevelInstance.gameObject);
            }

            _spawnedLevelInstance = Instantiate(_currentGameLevel, Vector3.zero, Quaternion.identity);

            // Позиционируем Lander
            if (Lander.HasInstance)
            {
                Lander.Instance.transform.position = _spawnedLevelInstance.GetLanderStartPosition();
                Debug.Log($"[GameManager] Lander positioned at {_spawnedLevelInstance.GetLanderStartPosition()}");
            }
            else
            {
                Debug.LogWarning("[GameManager] Lander not found!");
            }

            // Настраиваем камеру
            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.Target.TrackingTarget = _spawnedLevelInstance.GetCameraStartTargetTransform();
            }

            if (CinemachineCameraZoom2D.HasInstance)
            {
                CinemachineCameraZoom2D.Instance.SetTargetOrthographicSize(
                    _spawnedLevelInstance.GetZoomedOutOrthographicSize()
                );
            }

            Debug.Log($"[GameManager] Level {_currentGameLevel.GetLevelNumber()} loaded successfully");
        }

        private GameLevel FindGameLevelByNumber(int levelNumber)
        {
            if (_gameLevelList == null || _gameLevelList.Count == 0)
            {
                Debug.LogError("[GameManager] Level list is empty!");
                return null;
            }

            foreach (var level in _gameLevelList)
            {
                if (level != null && level.GetLevelNumber() == levelNumber)
                {
                    return level;
                }
            }

            Debug.LogWarning($"[GameManager] Level {levelNumber} not found in list");
            return null;
        }

        private void ResetCameraZoom()
        {
            if (CinemachineCameraZoom2D.HasInstance && _currentGameLevel != null)
            {
                CinemachineCameraZoom2D.Instance.SetNormalOrthographicSize(
                    _currentGameLevel.GetNormalOrthographicSize()
                );
            }
        }

        #endregion
    }
}
using My.Scripts.Core.Persistence;
using System.Collections.Generic;
using UnityEngine;

namespace My.Scripts.Core.Data
{
    public static class GameData
    {
        #region Constants

        private const float DEFAULT_MUSIC_VOLUME = 0.5f;
        private const float DEFAULT_SOUND_VOLUME = 0.7f;
        private const bool DEFAULT_FULLSCREEN = true;
        private const int DEFAULT_GRAPHICS_QUALITY = 2;

        #endregion

        #region Private Fields

        private static int _currentLevel = 1;
        private static int _totalScore;
        private static int _highestCompletedLevel;
        private static Dictionary<int, int> _levelStars;
        private static HashSet<int> _completedTutorials;
        private static bool _isInitialized;

        #endregion

        #region Properties

        public static int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                if (value > 0)
                    _currentLevel = value;
            }
        }

        public static int TotalScore
        {
            get => _totalScore;
            set
            {
                if (value >= 0)
                    _totalScore = value;
            }
        }

        public static int HighestCompletedLevel => _highestCompletedLevel;

        /// <summary>
        /// Это первый запуск игры (туториал первого уровня ещё не был показан).
        /// </summary>
        public static bool IsFirstLaunch => !IsTutorialCompletedForLevel(1);

        #endregion

        #region Audio Settings

        public static float MusicVolume { get; private set; } = DEFAULT_MUSIC_VOLUME;
        public static float SoundVolume { get; private set; } = DEFAULT_SOUND_VOLUME;

        public static void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
        }

        public static void SetSoundVolume(float volume)
        {
            SoundVolume = Mathf.Clamp01(volume);
        }

        #endregion

        #region Graphics Settings

        public static int GraphicsQuality { get; private set; } = DEFAULT_GRAPHICS_QUALITY;

        public static void SetGraphicsQuality(int qualityLevel)
        {
            int maxQuality = QualitySettings.names.Length - 1;
            GraphicsQuality = Mathf.Clamp(qualityLevel, 0, maxQuality);

            Debug.Log($"[GameData] Graphics quality set: {GraphicsQuality}");
        }

        #endregion

        #region Display Settings

        public static int ScreenWidth { get; private set; }
        public static int ScreenHeight { get; private set; }
        public static bool IsFullscreen { get; private set; } = DEFAULT_FULLSCREEN;

        public static void SetResolution(int width, int height)
        {
            ScreenWidth = width;
            ScreenHeight = height;
        }

        public static void SetFullscreen(bool isFullscreen)
        {
            IsFullscreen = isFullscreen;
        }

        #endregion

        #region Tutorial Methods

        /// <summary>
        /// Проверяет, пройден ли туториал для указанного уровня.
        /// </summary>
        public static bool IsTutorialCompletedForLevel(int levelNumber)
        {
            EnsureInitialized();
            return _completedTutorials.Contains(levelNumber);
        }

        /// <summary>
        /// Отмечает туториал указанного уровня как пройденный.
        /// Автоматически сохраняет прогресс.
        /// </summary>
        public static void MarkTutorialCompletedForLevel(int levelNumber)
        {
            EnsureInitialized();

            if (_completedTutorials.Contains(levelNumber)) return;

            _completedTutorials.Add(levelNumber);
            SaveSystem.Save();

            Debug.Log($"[GameData] Tutorial for level {levelNumber} marked as completed");
        }

        /// <summary>
        /// Сбрасывает состояние туториала для указанного уровня.
        /// </summary>
        public static void ResetTutorialForLevel(int levelNumber)
        {
            EnsureInitialized();

            if (_completedTutorials.Remove(levelNumber))
            {
                SaveSystem.Save();
                Debug.Log($"[GameData] Tutorial for level {levelNumber} reset");
            }
        }

        /// <summary>
        /// Сбрасывает все туториалы.
        /// </summary>
        public static void ResetAllTutorials()
        {
            EnsureInitialized();
            _completedTutorials.Clear();
            SaveSystem.Save();

            Debug.Log("[GameData] All tutorials reset");
        }

        /// <summary>
        /// Возвращает список пройденных туториалов (для отладки).
        /// </summary>
        public static int[] GetCompletedTutorials()
        {
            EnsureInitialized();
            int[] result = new int[_completedTutorials.Count];
            _completedTutorials.CopyTo(result);
            return result;
        }

        #endregion

        #region Public Methods

        public static void MarkLevelCompleted(int levelNumber, int starsEarned)
        {
            EnsureInitialized();

            Debug.Log($"[GameData] Level {levelNumber} completed with {starsEarned} stars");

            if (levelNumber > _highestCompletedLevel)
            {
                _highestCompletedLevel = levelNumber;
            }

            if (!_levelStars.TryGetValue(levelNumber, out int currentStars) || starsEarned > currentStars)
            {
                _levelStars[levelNumber] = starsEarned;
            }

            SaveSystem.Save();
        }

        public static bool IsLevelAvailable(int levelNumber)
        {
            EnsureInitialized();
            return levelNumber == 1 || levelNumber <= _highestCompletedLevel + 1;
        }

        public static int GetStarsForLevel(int levelNumber)
        {
            EnsureInitialized();
            return _levelStars.TryGetValue(levelNumber, out int stars) ? stars : 0;
        }

        public static void SetHighestCompletedLevel(int level)
        {
            EnsureInitialized();
            _highestCompletedLevel = level;
        }

        public static void ResetSessionData()
        {
            _currentLevel = 1;
            _totalScore = 0;
        }

        public static void ResetAllProgress()
        {
            _currentLevel = 1;
            _totalScore = 0;
            _highestCompletedLevel = 0;

            MusicVolume = DEFAULT_MUSIC_VOLUME;
            SoundVolume = DEFAULT_SOUND_VOLUME;
            GraphicsQuality = DEFAULT_GRAPHICS_QUALITY;

            ScreenWidth = Screen.width;
            ScreenHeight = Screen.height;
            IsFullscreen = DEFAULT_FULLSCREEN;

            _levelStars = new Dictionary<int, int>();
            _completedTutorials = new HashSet<int>();
            _isInitialized = true;

            Debug.Log("[GameData] All progress reset (including all tutorials)");
        }

        #endregion

        #region Save/Load

        public static void Save(ref GameSaveData data)
        {
            EnsureInitialized();

            data.hasBeenSavedBefore = true;

            // Audio
            data.musicVolume = MusicVolume;
            data.soundVolume = SoundVolume;

            // Graphics
            data.graphicsQuality = GraphicsQuality;

            // Display
            data.screenWidth = ScreenWidth;
            data.screenHeight = ScreenHeight;
            data.isFullscreen = IsFullscreen;

            // Progress
            data.highestCompletedLevel = _highestCompletedLevel;
            data.levelStarsData = new List<LevelStarData>();

            foreach (var kvp in _levelStars)
            {
                data.levelStarsData.Add(new LevelStarData
                {
                    levelNumber = kvp.Key,
                    starsCount = kvp.Value
                });
            }

            // Tutorials
            data.completedTutorials = new List<int>(_completedTutorials);

            Debug.Log($"[GameData] Saved: Tutorials={_completedTutorials.Count}, Graphics={GraphicsQuality}, Resolution={ScreenWidth}x{ScreenHeight}");
        }

        public static void Load(GameSaveData data)
        {
            Debug.Log($"[GameData] Loading... hasBeenSavedBefore={data.hasBeenSavedBefore}");

            if (!data.hasBeenSavedBefore)
            {
                LoadDefaults();
            }
            else
            {
                LoadFromSaveData(data);
            }

            LoadProgressData(data);

            _isInitialized = true;

            Debug.Log($"[GameData] Loaded: Tutorials={_completedTutorials.Count}, Graphics={GraphicsQuality}, Fullscreen={IsFullscreen}");
        }

        private static void LoadDefaults()
        {
            Debug.Log("[GameData] First launch — using defaults");

            MusicVolume = DEFAULT_MUSIC_VOLUME;
            SoundVolume = DEFAULT_SOUND_VOLUME;
            GraphicsQuality = DEFAULT_GRAPHICS_QUALITY;

            Resolution nativeResolution = Screen.currentResolution;
            ScreenWidth = nativeResolution.width;
            ScreenHeight = nativeResolution.height;
            IsFullscreen = DEFAULT_FULLSCREEN;

            _completedTutorials = new HashSet<int>();

            QualitySettings.SetQualityLevel(GraphicsQuality);
            Screen.SetResolution(ScreenWidth, ScreenHeight, IsFullscreen);

            Debug.Log($"[GameData] Applied defaults: Graphics={GraphicsQuality}");
        }

        private static void LoadFromSaveData(GameSaveData data)
        {
            // Audio
            MusicVolume = Mathf.Clamp01(data.musicVolume);
            SoundVolume = Mathf.Clamp01(data.soundVolume);

            // Graphics
            int maxQuality = QualitySettings.names.Length - 1;
            GraphicsQuality = Mathf.Clamp(data.graphicsQuality, 0, maxQuality);

            // Display
            ScreenWidth = data.screenWidth;
            ScreenHeight = data.screenHeight;
            IsFullscreen = data.isFullscreen;

            // Tutorials
            _completedTutorials = new HashSet<int>();
            if (data.completedTutorials != null)
            {
                foreach (int levelNumber in data.completedTutorials)
                {
                    _completedTutorials.Add(levelNumber);
                }
            }

            // Применяем настройки графики
            QualitySettings.SetQualityLevel(GraphicsQuality);
            Debug.Log($"[GameData] Applied graphics quality: {GraphicsQuality} ({QualitySettings.names[GraphicsQuality]})");

            // Применяем разрешение
            if (ScreenWidth > 0 && ScreenHeight > 0)
            {
                Screen.SetResolution(ScreenWidth, ScreenHeight, IsFullscreen);
                Debug.Log($"[GameData] Applied resolution: {ScreenWidth}x{ScreenHeight}, Fullscreen={IsFullscreen}");
            }
            else
            {
                Resolution nativeResolution = Screen.currentResolution;
                ScreenWidth = nativeResolution.width;
                ScreenHeight = nativeResolution.height;
                Screen.SetResolution(ScreenWidth, ScreenHeight, IsFullscreen);
            }
        }

        private static void LoadProgressData(GameSaveData data)
        {
            _highestCompletedLevel = data.highestCompletedLevel;
            _levelStars = new Dictionary<int, int>();

            if (data.levelStarsData != null)
            {
                foreach (var starData in data.levelStarsData)
                {
                    if (starData.starsCount > 0)
                    {
                        _levelStars[starData.levelNumber] = starData.starsCount;
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private static void EnsureInitialized()
        {
            if (_isInitialized) return;

            _levelStars ??= new Dictionary<int, int>();
            _completedTutorials ??= new HashSet<int>();
            _isInitialized = true;

            Debug.LogWarning("[GameData] Force initialized with empty data");
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticFields()
        {
            MusicVolume = DEFAULT_MUSIC_VOLUME;
            SoundVolume = DEFAULT_SOUND_VOLUME;
            GraphicsQuality = DEFAULT_GRAPHICS_QUALITY;
            ScreenWidth = 0;
            ScreenHeight = 0;
            IsFullscreen = DEFAULT_FULLSCREEN;
            _currentLevel = 1;
            _totalScore = 0;
            _highestCompletedLevel = 0;
            _completedTutorials = null;
            _levelStars = null;
            _isInitialized = false;

            Debug.Log("[GameData] Static fields reset (Domain Reload)");
        }
#endif

        #endregion
    }

    #region Save Data Structures

    [System.Serializable]
    public struct GameSaveData
    {
        public bool hasBeenSavedBefore;

        // Audio
        public float musicVolume;
        public float soundVolume;

        // Graphics
        public int graphicsQuality;

        // Display
        public int screenWidth;
        public int screenHeight;
        public bool isFullscreen;

        // Progress
        public int highestCompletedLevel;
        public List<LevelStarData> levelStarsData;

        // Tutorials
        public List<int> completedTutorials;
    }

    [System.Serializable]
    public struct LevelStarData
    {
        public int levelNumber;
        public int starsCount;
    }

    #endregion
}
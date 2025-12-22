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

        #endregion

        #region Private Fields

        private static int _currentLevel = 1;
        private static int _totalScore;
        private static int _highestCompletedLevel;
        private static Dictionary<int, int> _levelStars;
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

            _levelStars = new Dictionary<int, int>();
            _isInitialized = true;

            Debug.Log("[GameData] All progress reset");
        }

        #endregion

        #region Save/Load

        public static void Save(ref GameSaveData data)
        {
            EnsureInitialized();

            // ВАЖНО: Устанавливаем флаг что игра была сохранена
            data.hasBeenSavedBefore = true;

            data.musicVolume = MusicVolume;
            data.soundVolume = SoundVolume;
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

            Debug.Log($"[GameData] Saved: Music={MusicVolume:F3}, Sound={SoundVolume:F3}, hasBeenSaved=true");
        }

        public static void Load(GameSaveData data)
        {
            Debug.Log($"[GameData] Loading... hasBeenSavedBefore={data.hasBeenSavedBefore}, music={data.musicVolume:F3}, sound={data.soundVolume:F3}");

            // Если игра НИКОГДА не сохранялась — используем дефолтные значения
            if (!data.hasBeenSavedBefore)
            {
                Debug.Log("[GameData] First launch detected — using default volumes");
                MusicVolume = DEFAULT_MUSIC_VOLUME;
                SoundVolume = DEFAULT_SOUND_VOLUME;
            }
            else
            {
                // Игра сохранялась — используем сохранённые значения (даже если они 0!)
                MusicVolume = Mathf.Clamp01(data.musicVolume);
                SoundVolume = Mathf.Clamp01(data.soundVolume);
                Debug.Log($"[GameData] Loaded saved volumes: Music={MusicVolume:F3}, Sound={SoundVolume:F3}");
            }

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

            _isInitialized = true;

            Debug.Log($"[GameData] Load complete: Music={MusicVolume:F3}, Sound={SoundVolume:F3}");
        }

        #endregion

        #region Private Methods

        private static void EnsureInitialized()
        {
            if (_isInitialized) return;

            _levelStars ??= new Dictionary<int, int>();
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
            _currentLevel = 1;
            _totalScore = 0;
            _highestCompletedLevel = 0;
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
        public float musicVolume;
        public float soundVolume;
        public int highestCompletedLevel;
        public List<LevelStarData> levelStarsData;
    }

    [System.Serializable]
    public struct LevelStarData
    {
        public int levelNumber;
        public int starsCount;
    }

    #endregion
}
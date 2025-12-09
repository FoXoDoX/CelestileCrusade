using My.Scripts.Core.Persistence;
using System.Collections.Generic;
using UnityEngine;

namespace My.Scripts.Core.Data
{
    public static class GameData
    {
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

        #region Public Methods

        public static void MarkLevelCompleted(int levelNumber, int starsEarned)
        {
            EnsureInitialized();

            Debug.Log($"[GameData] Level {levelNumber} completed with {starsEarned} stars");

            // Обновляем максимальный пройденный уровень
            if (levelNumber > _highestCompletedLevel)
            {
                _highestCompletedLevel = levelNumber;
            }

            // Сохраняем лучший результат по звёздам
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

        /// <summary>
        /// Сброс данных текущей сессии (не прогресса!)
        /// </summary>
        public static void ResetSessionData()
        {
            _currentLevel = 1;
            _totalScore = 0;
        }

        /// <summary>
        /// Полный сброс всего прогресса
        /// </summary>
        public static void ResetAllProgress()
        {
            _currentLevel = 1;
            _totalScore = 0;
            _highestCompletedLevel = 0;

            // Гарантируем создание нового словаря
            _levelStars = new Dictionary<int, int>();
            _isInitialized = true;

            Debug.Log("[GameData] All progress reset");
        }

        #endregion

        #region Save/Load

        public static void Save(ref GameSaveData data)
        {
            EnsureInitialized();

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

            Debug.Log($"[GameData] Saved: {_levelStars.Count} levels, highest = {_highestCompletedLevel}");
        }

        public static void Load(GameSaveData data)
        {
            _highestCompletedLevel = data.highestCompletedLevel;

            // Всегда создаём новый словарь при загрузке
            _levelStars = new Dictionary<int, int>();

            if (data.levelStarsData != null)
            {
                foreach (var starData in data.levelStarsData)
                {
                    if (starData.starsCount > 0) // Только валидные данные
                    {
                        _levelStars[starData.levelNumber] = starData.starsCount;
                    }
                }
            }

            _isInitialized = true;

            Debug.Log($"[GameData] Loaded: {_levelStars.Count} levels, highest = {_highestCompletedLevel}");

            // Выводим все загруженные звёзды для отладки
            foreach (var kvp in _levelStars)
            {
                Debug.Log($"[GameData]   Level {kvp.Key}: {kvp.Value} stars");
            }
        }

        #endregion

        #region Private Methods

        private static void EnsureInitialized()
        {
            if (_isInitialized) return;

            // Если не инициализированы, создаём пустой словарь
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
            // Сброс статических полей при Domain Reload (важно для Enter Play Mode Options)
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
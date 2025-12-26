using My.Scripts.Core.Data;
using System;
using System.IO;
using UnityEngine;

namespace My.Scripts.Core.Persistence
{
    public static class SaveSystem
    {
        #region Constants

        private const string SAVE_FILE_NAME = "game.save";
        private const string PLAYERPREFS_SAVE_KEY = "GameSaveData";
        private const bool PRETTY_PRINT_JSON = true;

        #endregion

        #region Private Fields

        private static SaveData _saveData;
        private static bool _isInitialized;

        #endregion

        #region Properties

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        public static bool IsInitialized => _isInitialized;

        public static bool SaveFileExists
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return PlayerPrefs.HasKey(PLAYERPREFS_SAVE_KEY);
#else
                return File.Exists(SaveFilePath);
#endif
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// —охран€ет текущее состо€ние игры.
        /// </summary>
        public static void Save()
        {
            try
            {
                _saveData = new SaveData();
                PrepareSaveData();

                string json = JsonUtility.ToJson(_saveData, PRETTY_PRINT_JSON);

#if UNITY_WEBGL && !UNITY_EDITOR
                SaveToPlayerPrefs(json);
#else
                SaveToFile(json);
#endif

                LogSaveSuccess(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveSystem)}] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// «агружает состо€ние игры.
        /// </summary>
        public static void Load()
        {
            try
            {
                if (!SaveFileExists)
                {
                    Debug.Log($"[{nameof(SaveSystem)}] No save data found");
                    InitializeWithDefaults();
                    return;
                }

#if UNITY_WEBGL && !UNITY_EDITOR
                string json = LoadFromPlayerPrefs();
#else
                string json = LoadFromFile();
#endif

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning($"[{nameof(SaveSystem)}] Save data is empty");
                    InitializeWithDefaults();
                    return;
                }

                _saveData = JsonUtility.FromJson<SaveData>(json);
                ApplyLoadedData();
                LogLoadSuccess(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveSystem)}] Failed to load: {e.Message}");
                InitializeWithDefaults();
            }
        }

        /// <summary>
        /// ”дал€ет сохранение.
        /// </summary>
        public static void DeleteSave()
        {
            try
            {
                if (!SaveFileExists)
                {
                    Debug.Log($"[{nameof(SaveSystem)}] No save data to delete");
                    return;
                }

#if UNITY_WEBGL && !UNITY_EDITOR
                PlayerPrefs.DeleteKey(PLAYERPREFS_SAVE_KEY);
                PlayerPrefs.Save();
#else
                File.Delete(SaveFilePath);
#endif

                InitializeWithDefaults();
                Debug.Log($"[{nameof(SaveSystem)}] Save data deleted");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveSystem)}] Failed to delete save: {e.Message}");
            }
        }

        /// <summary>
        /// ѕринудительна€ инициализаци€.
        /// </summary>
        public static void ForceInitialize()
        {
            if (_isInitialized) return;
            Initialize();
        }

        #endregion

        #region Private Methods Ч Save/Load Implementation

        private static void SaveToFile(string json)
        {
            File.WriteAllText(SaveFilePath, json);
        }

        private static void SaveToPlayerPrefs(string json)
        {
            PlayerPrefs.SetString(PLAYERPREFS_SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private static string LoadFromFile()
        {
            return File.ReadAllText(SaveFilePath);
        }

        private static string LoadFromPlayerPrefs()
        {
            return PlayerPrefs.GetString(PLAYERPREFS_SAVE_KEY, string.Empty);
        }

        #endregion

        #region Private Methods Ч Data Handling

        private static void PrepareSaveData()
        {
            GameData.Save(ref _saveData.GameSaveData);
        }

        private static void ApplyLoadedData()
        {
            GameData.Load(_saveData.GameSaveData);
        }

        private static void InitializeWithDefaults()
        {
            Debug.Log($"[{nameof(SaveSystem)}] Initializing with default values");
            _saveData = new SaveData();
            GameData.ResetAllProgress();
        }

        #endregion

        #region Private Methods Ч Logging

        private static void LogSaveSuccess(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log($"[{nameof(SaveSystem)}] Saved to PlayerPrefs");
#else
            Debug.Log($"[{nameof(SaveSystem)}] Saved to: {SaveFilePath}");
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[{nameof(SaveSystem)}] Content:\n{json}");
#endif
        }

        private static void LogLoadSuccess(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log($"[{nameof(SaveSystem)}] Loaded from PlayerPrefs");
#else
            Debug.Log($"[{nameof(SaveSystem)}] Loaded from: {SaveFilePath}");
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[{nameof(SaveSystem)}] Content:\n{json}");
#endif
        }

        #endregion

        #region Initialization

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Debug.Log($"[{nameof(SaveSystem)}] Initializing...");

            _isInitialized = false;
            _saveData = new SaveData();

            Load();
            _isInitialized = true;

            Debug.Log($"[{nameof(SaveSystem)}] === Initialize END === MusicVolume={GameData.MusicVolume:F3}");

            Application.quitting += OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            Debug.Log($"[{nameof(SaveSystem)}] Auto-saving on quit...");
            Save();
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticFields()
        {
            _saveData = new SaveData();
            _isInitialized = false;
            Application.quitting -= OnApplicationQuitting;

            Debug.Log($"[{nameof(SaveSystem)}] Static fields reset (Domain Reload)");
        }
#endif

        #endregion

        #region Nested Types

        [Serializable]
        public struct SaveData
        {
            public GameSaveData GameSaveData;
        }

        #endregion
    }
}
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
        private const bool PRETTY_PRINT_JSON = true;

        #endregion

        #region Private Fields

        private static SaveData _saveData;
        private static bool _isInitialized;

        #endregion

        #region Properties

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        public static bool SaveFileExists => File.Exists(SaveFilePath);
        public static bool IsInitialized => _isInitialized;

        #endregion

        #region Public Methods

        /// <summary>
        /// Сохраняет текущее состояние игры в файл.
        /// </summary>
        public static void Save()
        {
            try
            {
                _saveData = new SaveData();
                PrepareSaveData();

                string json = JsonUtility.ToJson(_saveData, PRETTY_PRINT_JSON);
                File.WriteAllText(SaveFilePath, json);

                LogSaveSuccess(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveSystem)}] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Загружает состояние игры из файла.
        /// </summary>
        public static void Load()
        {
            try
            {
                if (!SaveFileExists)
                {
                    Debug.Log($"[{nameof(SaveSystem)}] No save file found at: {SaveFilePath}");
                    InitializeWithDefaults();
                    return;
                }

                string json = File.ReadAllText(SaveFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning($"[{nameof(SaveSystem)}] Save file is empty");
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
        /// Удаляет файл сохранения.
        /// </summary>
        public static void DeleteSave()
        {
            try
            {
                if (!SaveFileExists)
                {
                    Debug.Log($"[{nameof(SaveSystem)}] No save file to delete");
                    return;
                }

                File.Delete(SaveFilePath);
                InitializeWithDefaults();

                Debug.Log($"[{nameof(SaveSystem)}] Save file deleted");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveSystem)}] Failed to delete save: {e.Message}");
            }
        }

        /// <summary>
        /// Принудительная инициализация (если нужно до автоматической).
        /// </summary>
        public static void ForceInitialize()
        {
            if (_isInitialized) return;
            Initialize();
        }

        #endregion

        #region Private Methods

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

        private static void LogSaveSuccess(string json)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[{nameof(SaveSystem)}] Saved to: {SaveFilePath}");
            Debug.Log($"[{nameof(SaveSystem)}] Content:\n{json}");
#else
            Debug.Log($"[{nameof(SaveSystem)}] Game saved successfully");
#endif
        }

        private static void LogLoadSuccess(string json)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[{nameof(SaveSystem)}] Loaded from: {SaveFilePath}");
            Debug.Log($"[{nameof(SaveSystem)}] Content:\n{json}");
#else
            Debug.Log($"[{nameof(SaveSystem)}] Game loaded successfully");
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

            // Автосохранение при выходе из приложения
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
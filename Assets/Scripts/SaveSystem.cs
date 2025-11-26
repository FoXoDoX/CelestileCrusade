using UnityEngine;
using System.IO;
using System;

public static class SaveSystem
{
    private static SaveData _saveData = new SaveData();

    [System.Serializable]
    public struct SaveData
    {
        public GameSaveData GameSaveData;
    }

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, "game.save");

    public static void Save()
    {
        try
        {
            PrepareSaveData();
            string json = JsonUtility.ToJson(_saveData, true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"Game data saved to: {SaveFilePath}");
            Debug.Log($"Save file content: {json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game data: {e.Message}");
        }
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                Debug.Log($"Loading save file from: {SaveFilePath}");
                Debug.Log($"Save file content: {json}");

                _saveData = JsonUtility.FromJson<SaveData>(json);
                ApplyLoadedData();
                Debug.Log("Game data loaded successfully");
            }
            else
            {
                Debug.Log("No save file found, using default data");
                _saveData = new SaveData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game data: {e.Message}");
            _saveData = new SaveData();
        }
    }

    private static void PrepareSaveData()
    {
        GameData.Save(ref _saveData.GameSaveData);
    }

    private static void ApplyLoadedData()
    {
        GameData.Load(_saveData.GameSaveData);
    }

    public static bool SaveFileExists => File.Exists(SaveFilePath);

    public static void DeleteSave()
    {
        try
        {
            if (SaveFileExists)
            {
                File.Delete(SaveFilePath);
                Debug.Log("Save file deleted");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save file: {e.Message}");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        Debug.Log("SaveSystem initializing...");
        Load();
    }
}
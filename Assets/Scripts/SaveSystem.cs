using UnityEngine;
using System.IO;

public class SaveSystem
{
    public static SaveData _saveData = new SaveData();

    [System.Serializable]
    public struct SaveData
    {
        public GameSaveData GameSaveData;
    }

    public static string SaveFileName()
    {
        string saveFile = Application.persistentDataPath + "/save" + ".save";
        return saveFile;
    }

    public static void Save()
    {
        HandleSaveData();

        File.WriteAllText(SaveFileName(), JsonUtility.ToJson(_saveData, true));

        Debug.Log("Data saved");
    }

    public static void HandleSaveData()
    {
        GameData.Save(ref _saveData.GameSaveData);
    }

    public static bool IsSaveFileExists()
    {
        string saveFile = SaveFileName();
        return File.Exists(saveFile);
    }

    public static void Load()
    {
        string saveContent = File.ReadAllText(SaveFileName());

        _saveData = JsonUtility.FromJson<SaveData>(saveContent);

        HandleLoadData();

        Debug.Log("Data loaded");
    }

    public static void HandleLoadData()
    {
        GameData.Load(_saveData.GameSaveData);
    }
}

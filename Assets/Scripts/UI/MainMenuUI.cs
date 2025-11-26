using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (!SaveSystem.SaveFileExists)
        {
            Debug.Log("No save file exists, creating initial save");
            SaveSystem.Save();
        }

        playButton.onClick.AddListener(() =>
        {
            GameData.ResetStaticData();
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        });

        levelsButton.onClick.AddListener(() =>
        {
            GameData.ResetStaticData();
            SceneLoader.LoadScene(SceneLoader.Scene.LevelsMenuScene);
        });

        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }

    private void Start()
    {
        playButton.Select();
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public static event EventHandler OnSettingsButtonClick;

    [SerializeField] private Button playButton;
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (!SaveSystem.SaveFileExists)
        {
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

        settingsButton.onClick.AddListener(() =>
        {
            OnSettingsButtonClick?.Invoke(this, EventArgs.Empty);
        });

        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }

    private void Start()
    {
        playButton.Select();

        SettingsUI.OnBackButtonClick += SettingsUI_OnBackButtonClick;
    }

    private void SettingsUI_OnBackButtonClick(object sender, EventArgs e)
    {
        playButton.Select();
    }

    private void OnDestroy()
    {
        SettingsUI.OnBackButtonClick -= SettingsUI_OnBackButtonClick;
    }
}

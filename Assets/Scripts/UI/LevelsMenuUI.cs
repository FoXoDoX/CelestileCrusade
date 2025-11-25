using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelsMenuUI : MonoBehaviour
{
    [SerializeField] private List<Button> listOfLevelsButtons;
    [SerializeField] private Button backToMainMenuButton;

    private void Awake()
    {
        Time.timeScale = 1f;

        for (int i = 0; i < listOfLevelsButtons.Count; i++)
        {
            int levelNumber = i + 1;
            Button levelButton = listOfLevelsButtons[i];

            if (GameData.IsLevelAvailable(levelNumber))
            {
                levelButton.interactable = true;
                levelButton.onClick.AddListener(() =>
                {
                    GameData.CurrentLevel = levelNumber;
                    GameData.TotalScore = 0;
                    SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
                });
            }
            else
            {
                levelButton.interactable = false;
            }
        }

        backToMainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }

    private void Start()
    {
        backToMainMenuButton.Select();
    }
}
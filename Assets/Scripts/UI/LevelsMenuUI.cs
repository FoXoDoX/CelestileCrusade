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

                // Отображаем звёзды для доступного уровня
                DisplayStarsForLevel(levelButton, levelNumber);
            }
            else
            {
                levelButton.interactable = false;
                // Для недоступных уровней скрываем звёзды или показываем заблокированными
                HideStarsForLevel(levelButton);
            }
        }

        backToMainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }

    private void DisplayStarsForLevel(Button levelButton, int levelNumber)
    {
        // Получаем количество заработанных звёзд для уровня
        int starsCount = GameData.GetStarsForLevel(levelNumber);

        // Находим объекты звёзд для этой кнопки
        for (int starIndex = 1; starIndex <= 3; starIndex++)
        {
            Transform star = levelButton.transform.Find($"Star{starIndex}");
            if (star != null)
            {
                // Находим компоненты Image для заработанной и не заработанной звезды
                Transform earnedStarImage = star.Find("EarnedStarImage");
                Transform unearnedStarImage = star.Find("UnearnedStarImage");

                if (earnedStarImage != null && unearnedStarImage != null)
                {
                    Image earnedImage = earnedStarImage.GetComponent<Image>();
                    Image unearnedImage = unearnedStarImage.GetComponent<Image>();

                    if (earnedImage != null && unearnedImage != null)
                    {
                        // Активируем соответствующий спрайт в зависимости от того, заработана ли звезда
                        bool isStarEarned = starIndex <= starsCount;
                        earnedImage.enabled = isStarEarned;
                        unearnedImage.enabled = !isStarEarned;
                    }
                }
            }
        }
    }

    private void HideStarsForLevel(Button levelButton)
    {
        // Для недоступных уровней скрываем все звёзды
        for (int starIndex = 1; starIndex <= 3; starIndex++)
        {
            Transform star = levelButton.transform.Find($"Star{starIndex}");
            if (star != null)
            {
                Transform earnedStarImage = star.Find("EarnedStarImage");
                Transform unearnedStarImage = star.Find("UnearnedStarImage");

                if (earnedStarImage != null)
                {
                    Image earnedImage = earnedStarImage.GetComponent<Image>();
                    if (earnedImage != null) earnedImage.enabled = false;
                }

                if (unearnedStarImage != null)
                {
                    Image unearnedImage = unearnedStarImage.GetComponent<Image>();
                    if (unearnedImage != null) unearnedImage.enabled = false;
                }
            }
        }
    }

    private void Start()
    {
        backToMainMenuButton.Select();
    }
}
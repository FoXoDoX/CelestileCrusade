using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class LandedUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleTextMesh;
    [SerializeField] private TextMeshProUGUI statsTextMesh;
    [SerializeField] private TextMeshProUGUI nextButtonTextMesh;
    [SerializeField] private List<Transform> starsList;
    [SerializeField] private Button nextButton;

    private Action nextButtonClickAction;

    private void Awake()
    {
        nextButton.onClick.AddListener(() => {
            nextButtonClickAction();
        });
    }

    private void Start()
    {
        Lander.Instance.OnLanded += Lander_OnLanded;
        Hide();
    }

    private void Lander_OnLanded(object sender, Lander.OnLandedEventArgs e)
    {
        if (e.landingType == Lander.LandingType.Success)
        {
            titleTextMesh.text = "SUCCESSFUL LANDING!";
            nextButtonTextMesh.text = "CONTINUE";
            nextButtonClickAction = GameManager.Instance.GoToNextLevel;

            ShowStarsForSuccess();
        }
        else
        {
            titleTextMesh.text = "<color=#ff0000>CRASH!</color>";
            nextButtonTextMesh.text = "RESTART";
            nextButtonClickAction = GameManager.Instance.RetryLevel;

            HideAllStars();
        }

        statsTextMesh.text =
            Mathf.Round(e.landingSpeed * 2f) + "\n" +
            Mathf.Round(e.dotVector * 100f) + "\n" +
            "x" + e.scoreMultiplier + "\n" +
            e.score;

        Show();
    }

    private void ShowStarsForSuccess()
    {
        GameLevel currentLevel = GameManager.Instance.GetCurrentLevelObject();
        int currentScore = GameManager.Instance.GetScore();

        if (currentLevel != null)
        {
            int earnedStars = currentLevel.GetEarnedStarsCount(currentScore);

            for (int i = 0; i < starsList.Count; i++)
            {
                Transform star = starsList[i];

                Transform earnedStarImage = star.Find("EarnedStarImage");
                Transform unearnedStarImage = star.Find("UnearnedStarImage");

                if (earnedStarImage != null && unearnedStarImage != null)
                {
                    Image earnedImage = earnedStarImage.GetComponent<Image>();
                    Image unearnedImage = unearnedStarImage.GetComponent<Image>();

                    bool isStarEarned = i < earnedStars;

                    earnedImage.enabled = isStarEarned;
                    unearnedImage.enabled = true;
                }
            }
        }
    }

    private void HideAllStars()
    {
        foreach (Transform star in starsList)
        {
            Transform earnedStarImage = star.Find("EarnedStarImage");
            Transform unearnedStarImage = star.Find("UnearnedStarImage");

            if (earnedStarImage != null)
            {
                Image earnedImage = earnedStarImage.GetComponent<Image>();
                if (earnedImage != null)
                {
                    earnedImage.enabled = false;
                }
            }

            if (unearnedStarImage != null)
            {
                Image unearnedImage = unearnedStarImage.GetComponent<Image>();
                if (unearnedImage != null)
                {
                    unearnedImage.enabled = false;
                }
            }
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);

        LandedUIAnimation anim = GetComponent<LandedUIAnimation>();
    if (anim != null)
    {
        anim.PlayEnterAnimation();
    }

        nextButton.Select();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
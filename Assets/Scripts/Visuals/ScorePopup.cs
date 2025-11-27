using TMPro;
using UnityEngine;

public class ScorePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMeshPro;
    private ScorePopupAnimation animationComponent;

    private void Awake()
    {
        animationComponent = GetComponent<ScorePopupAnimation>();
    }

    public void SetText(string text)
    {
        textMeshPro.text = text;

        if (animationComponent != null)
            animationComponent.SetText(text);
    }
}
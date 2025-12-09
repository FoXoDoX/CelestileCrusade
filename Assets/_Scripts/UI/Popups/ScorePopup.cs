using TMPro;
using UnityEngine;

namespace My.Scripts.UI.Popups
{
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMeshPro;
        [SerializeField] private GameObject background;
        private ScorePopupAnimation animationComponent;

        private void Awake()
        {
            animationComponent = GetComponent<ScorePopupAnimation>();
        }

        public void Setup(string text)
        {
            textMeshPro.text = text;

            if (animationComponent != null)
                animationComponent.SetText(text);
        }

        public void Setup(string text, Color backgroundColor, Color textColor, bool isBoldText)
        {
            textMeshPro.text = text;
            textMeshPro.color = textColor;
            textMeshPro.fontStyle = isBoldText ? FontStyles.Bold : FontStyles.Normal;

            if (animationComponent != null)
                animationComponent.SetText(text);

            background.GetComponent<SpriteRenderer>().color = backgroundColor;
        }
    }
}
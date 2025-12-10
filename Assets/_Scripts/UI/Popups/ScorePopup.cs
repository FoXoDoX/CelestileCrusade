using TMPro;
using UnityEngine;

namespace My.Scripts.UI.Popups
{
    /// <summary>
    /// Popup с отображением очков или текста.
    /// </summary>
    public class ScorePopup : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private TextMeshPro _textMesh;
        [SerializeField] private GameObject _background;

        #endregion

        #region Private Fields

        private ScorePopupAnimation _animation;
        private SpriteRenderer _backgroundRenderer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ќастраивает popup с текстом (стандартный стиль).
        /// </summary>
        public void Setup(string text)
        {
            _textMesh.text = text;

            if (_animation != null)
            {
                _animation.SetText(text);
            }
        }

        /// <summary>
        /// Ќастраивает popup с текстом и кастомными цветами.
        /// </summary>
        public void Setup(string text, Color backgroundColor, Color textColor, bool isBoldText)
        {
            _textMesh.text = text;
            _textMesh.color = textColor;
            _textMesh.fontStyle = isBoldText ? FontStyles.Bold : FontStyles.Normal;

            if (_animation != null)
            {
                _animation.SetText(text);
            }

            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.color = backgroundColor;
            }
        }

        #endregion

        #region Private Methods

        private void CacheComponents()
        {
            _animation = GetComponent<ScorePopupAnimation>();

            if (_background != null)
            {
                _backgroundRenderer = _background.GetComponent<SpriteRenderer>();
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_textMesh == null)
            {
                _textMesh = GetComponentInChildren<TextMeshPro>();
            }
        }

        private void Reset()
        {
            _textMesh = GetComponentInChildren<TextMeshPro>();
        }
#endif

        #endregion
    }
}
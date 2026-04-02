using My.Scripts.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.Scripts.UI
{
    public enum UIElementType
    {
        Button,
        Toggle
    }

    public enum UIElementSize
    {
        Large,
        Small
    }

    public enum ClickSoundVariant
    {
        Open,
        Close
    }

    public class UIElementSounds : MonoBehaviour, ISelectHandler
    {
        #region Serialized Fields

        [Header("Element Type")]
        [SerializeField] private UIElementType _elementType = UIElementType.Button;

        [Header("Button Settings")]
        [SerializeField] private UIElementSize _size = UIElementSize.Large;
        [SerializeField] private ClickSoundVariant _clickVariant = ClickSoundVariant.Open;

        [Header("Toggle Settings")]
        [SerializeField] private ClickSoundVariant _toggleEnableVariant = ClickSoundVariant.Open;
        [SerializeField] private ClickSoundVariant _toggleDisableVariant = ClickSoundVariant.Close;

        #endregion

        #region Private Fields

        private Selectable _selectable;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        private void OnEnable()
        {
            switch (_elementType)
            {
                case UIElementType.Button:
                    if (_selectable is Button button)
                        button.onClick.AddListener(OnButtonClick);
                    break;

                case UIElementType.Toggle:
                    if (_selectable is Toggle toggle)
                        toggle.onValueChanged.AddListener(OnToggleChanged);
                    break;
            }
        }

        private void OnDisable()
        {
            switch (_elementType)
            {
                case UIElementType.Button:
                    if (_selectable is Button button)
                        button.onClick.RemoveListener(OnButtonClick);
                    break;

                case UIElementType.Toggle:
                    if (_selectable is Toggle toggle)
                        toggle.onValueChanged.RemoveListener(OnToggleChanged);
                    break;
            }
        }

        #endregion

        #region Interface Implementations

        public void OnSelect(BaseEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;

            var hoverSize = _elementType == UIElementType.Button
                ? _size
                : UIElementSize.Small;

            SoundManager.Instance?.PlayUIHoverSound(hoverSize);
        }

        #endregion

        #region Private Methods

        private void OnButtonClick()
        {
            SoundManager.Instance?.PlayUIClickSound(_size, _clickVariant);
        }

        private void OnToggleChanged(bool isOn)
        {
            var variant = isOn
                ? _toggleEnableVariant
                : _toggleDisableVariant;

            SoundManager.Instance?.PlayUIClickSound(UIElementSize.Small, variant);
        }

        #endregion
    }
}
using My.Scripts.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.Scripts.UI
{
    /// <summary>
    /// Управляет звуками и анимациями Handle для всей области слайдера.
    /// Handle Image должен иметь Raycast Target = false.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class SliderInteractionTracker : MonoBehaviour,
        ISelectHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        #region Serialized Fields

        [Header("Sound Settings")]
        [SerializeField] private ClickSoundVariant _pressVariant = ClickSoundVariant.Open;
        [SerializeField] private ClickSoundVariant _releaseVariant = ClickSoundVariant.Close;

        [Header("Handle")]
        [SerializeField] private UIElementAnimator _handleAnimator;

        #endregion

        #region Private Fields

        private Slider _slider;
        private bool _isPressed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        private void OnDisable()
        {
            if (_isPressed)
            {
                _isPressed = false;
                _handleAnimator?.TriggerRelease();
            }
        }

        #endregion

        #region Interface Implementations

        public void OnSelect(BaseEventData eventData)
        {
            if (_slider != null && !_slider.interactable) return;
            if (_isPressed) return;

            SoundManager.Instance?.PlayUIHoverSound(UIElementSize.Small);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_slider != null && !_slider.interactable) return;

            _isPressed = true;
            SoundManager.Instance?.PlayUIClickSound(UIElementSize.Small, _pressVariant);
            _handleAnimator?.TriggerPress();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed) return;

            _isPressed = false;
            SoundManager.Instance?.PlayUIClickSound(UIElementSize.Small, _releaseVariant);
            _handleAnimator?.TriggerRelease();
        }

        #endregion
    }
}
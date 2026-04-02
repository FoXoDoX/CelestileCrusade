using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.Scripts.UI
{
    /// <summary>
    /// ”ниверсальна€ анимаци€ наведени€ и нажати€ дл€ любого UI-элемента.
    /// ¬ешаетс€ на Button, Toggle или Handle слайдера.
    /// ѕоддерживает как мышь, так и клавиатурную навигацию.
    /// јвтоматически находит Selectable в родител€х дл€ проверки interactable.
    /// </summary>
    public class UIElementAnimator : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        ISubmitHandler, ISelectHandler, IDeselectHandler
    {
        #region Serialized Fields

        [Header("Visual Root")]
        [Tooltip("ƒочерний объект дл€ анимации. ≈сли пустой Ч анимируетс€ сам объект.")]
        [SerializeField] private Transform _visualsRoot;

        [Header("Hover Animation")]
        [SerializeField] private float _hoverScale = 1.15f;
        [SerializeField] private float _hoverBounceScale = 1.08f;
        [SerializeField] private float _hoverScaleDuration = 0.1f;
        [SerializeField] private float _hoverBounceDuration = 0.08f;

        [Header("Unhover Animation")]
        [SerializeField] private float _unhoverDuration = 0.2f;

        [Header("Click Animation")]
        [SerializeField] private float _clickScale = 0.9f;
        [SerializeField] private float _clickShrinkDuration = 0.06f;
        [SerializeField] private float _clickReleaseDuration = 0.1f;

        #endregion

        #region Private Fields

        private Vector3 _originalScale;
        private Tween _tween;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isDragging;
        private bool _isSelected;
        private bool _isChildOfSelectable;
        private Selectable _selectable;
        private Slider _parentSlider;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_visualsRoot == null)
            {
                _visualsRoot = transform;
            }

            _originalScale = _visualsRoot.localScale;
            _selectable = GetComponentInParent<Selectable>();
            _parentSlider = GetComponentInParent<Slider>();
            _isChildOfSelectable = _selectable != null && _selectable.gameObject != gameObject;
        }

        private void OnEnable()
        {
            if (_isChildOfSelectable)
            {
                var forwarder = _selectable.gameObject.GetComponent<SelectionEventForwarder>();
                if (forwarder == null)
                {
                    forwarder = _selectable.gameObject.AddComponent<SelectionEventForwarder>();
                }
                forwarder.Register(this);
            }
        }

        private void OnDisable()
        {
            KillTween();
            _visualsRoot.localScale = _originalScale;
            _isHovered = false;
            _isPressed = false;
            _isDragging = false;
            _isSelected = false;

            if (_isChildOfSelectable)
            {
                var forwarder = _selectable.gameObject.GetComponent<SelectionEventForwarder>();
                if (forwarder != null)
                {
                    forwarder.Unregister(this);
                }
            }
        }

        private void OnDestroy()
        {
            KillTween();
        }

        #endregion

        #region Pointer Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            _isHovered = true;

            if (_isPressed || _isDragging) return;

            PlayHoverAnimation();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;

            if (!IsInteractable()) return;
            if (_isPressed || _isDragging) return;
            if (_isSelected) return;

            PlayUnhoverAnimation();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            _isPressed = true;
            PlayClickDownAnimation();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            _isPressed = false;

            if (_isDragging) return;

            PlayClickUpAnimation();
        }

        #endregion

        #region Drag Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsInteractable()) return;

            _isDragging = true;

            if (_parentSlider != null)
            {
                ExecuteEvents.Execute(_parentSlider.gameObject, eventData,
                    ExecuteEvents.beginDragHandler);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_parentSlider != null)
            {
                ExecuteEvents.Execute(_parentSlider.gameObject, eventData,
                    ExecuteEvents.dragHandler);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _isPressed = false;

            if (!IsInteractable()) return;

            if (_parentSlider != null)
            {
                ExecuteEvents.Execute(_parentSlider.gameObject, eventData,
                    ExecuteEvents.endDragHandler);
            }

            PlayClickUpAnimation();
        }

        #endregion

        #region Submit Handler

        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsInteractable()) return;

            PlaySubmitAnimation();
        }

        #endregion

        #region Selection Handlers

        public void OnSelect(BaseEventData eventData)
        {
            if (_isChildOfSelectable) return;
            if (!IsInteractable()) return;

            _isSelected = true;

            if (_isHovered || _isPressed || _isDragging) return;

            PlayHoverAnimation();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (_isChildOfSelectable) return;

            _isSelected = false;

            if (!IsInteractable()) return;
            if (_isHovered || _isPressed || _isDragging) return;

            PlayUnhoverAnimation();
        }

        #endregion

        #region Public Methods Ч Called by SelectionEventForwarder

        public void HandleParentSelected()
        {
            if (!IsInteractable()) return;

            _isSelected = true;

            if (_isHovered || _isPressed || _isDragging) return;

            PlayHoverAnimation();
        }

        public void HandleParentDeselected()
        {
            _isSelected = false;

            if (!IsInteractable()) return;
            if (_isHovered || _isPressed || _isDragging) return;

            PlayUnhoverAnimation();
        }

        #endregion

        #region Public Methods Ч Called by SliderInteractionTracker

        public void TriggerPress()
        {
            if (!IsInteractable()) return;

            _isPressed = true;
            PlayClickDownAnimation();
        }

        public void TriggerRelease()
        {
            if (!IsInteractable()) return;

            _isPressed = false;

            if (_isDragging) return;

            PlayClickUpAnimation();
        }

        #endregion

        #region Private Methods Ч Animations

        private void PlayHoverAnimation()
        {
            KillTween();

            _tween = _visualsRoot
                .DOScale(_originalScale * _hoverScale, _hoverScaleDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _tween = _visualsRoot
                        .DOScale(_originalScale * _hoverBounceScale, _hoverBounceDuration)
                        .SetEase(Ease.InOutQuad)
                        .SetUpdate(true);
                });
        }

        private void PlayUnhoverAnimation()
        {
            KillTween();

            _tween = _visualsRoot
                .DOScale(_originalScale, _unhoverDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void PlayClickDownAnimation()
        {
            KillTween();

            _tween = _visualsRoot
                .DOScale(_originalScale * _clickScale, _clickShrinkDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true);
        }

        private void PlayClickUpAnimation()
        {
            KillTween();

            Vector3 targetScale = (_isHovered || _isSelected)
                ? _originalScale * _hoverBounceScale
                : _originalScale;

            _tween = _visualsRoot
                .DOScale(targetScale, _clickReleaseDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void PlaySubmitAnimation()
        {
            KillTween();

            _tween = _visualsRoot
                .DOScale(_originalScale * _clickScale, _clickShrinkDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    Vector3 targetScale = (_isHovered || _isSelected)
                        ? _originalScale * _hoverBounceScale
                        : _originalScale;

                    _tween = _visualsRoot
                        .DOScale(targetScale, _clickReleaseDuration)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true);
                });
        }

        #endregion

        #region Private Methods Ч Utility

        private bool IsInteractable()
        {
            return _selectable == null || _selectable.interactable;
        }

        private void KillTween()
        {
            _tween?.Kill();
            _tween = null;
        }

        #endregion
    }
}
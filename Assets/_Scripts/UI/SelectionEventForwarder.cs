using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace My.Scripts.UI
{
    /// <summary>
    /// Пробрасывает события Select/Deselect от Selectable к дочерним UIElementAnimator'ам.
    /// Автоматически добавляется на родительский Selectable при необходимости.
    /// </summary>
    [DisallowMultipleComponent]
    public class SelectionEventForwarder : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private readonly List<UIElementAnimator> _listeners = new();

        public void Register(UIElementAnimator animator)
        {
            if (!_listeners.Contains(animator))
            {
                _listeners.Add(animator);
            }
        }

        public void Unregister(UIElementAnimator animator)
        {
            _listeners.Remove(animator);
        }

        public void OnSelect(BaseEventData eventData)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (_listeners[i] != null && _listeners[i].isActiveAndEnabled)
                {
                    _listeners[i].HandleParentSelected();
                }
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (_listeners[i] != null && _listeners[i].isActiveAndEnabled)
                {
                    _listeners[i].HandleParentDeselected();
                }
            }
        }
    }
}
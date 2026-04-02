using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.Scripts.UI
{
    [RequireComponent(typeof(Selectable))]
    public class SelectOnHover : MonoBehaviour, IPointerEnterHandler
    {
        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectable != null && _selectable.interactable)
            {
                _selectable.Select();
            }
        }
    }
}
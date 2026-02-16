using My.Scripts.EventBus;
using My.Scripts.Core.Data;
using UnityEngine;
using UnityEngine.UI;

namespace My.Scripts.UI.Indicators
{
    public class EnergyBarFireEffect : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Image _energyBarImage;
        [SerializeField] private RectTransform _fireContainer;
        [SerializeField] private RectTransform _flameRect;

        [Header("Offset")]
        [SerializeField] private Vector2 _offset = Vector2.zero;

        [Header("Smooth")]
        [SerializeField] private float _fadeSpeed = 5f;

        #endregion

        #region Private Fields

        private CanvasGroup _fireCanvasGroup;
        private float _targetAlpha;
        private readonly Vector3[] _corners = new Vector3[4];

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_fireContainer == null || _energyBarImage == null)
            {
                Debug.LogError("[FireEffect] Missing references!");
                return;
            }

            _fireCanvasGroup = _fireContainer.GetComponent<CanvasGroup>();
            if (_fireCanvasGroup == null)
                _fireCanvasGroup = _fireContainer.gameObject.AddComponent<CanvasGroup>();

            _fireCanvasGroup.alpha = 0f;
            _targetAlpha = 0f;
            _fireContainer.gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            EventManager.Instance?.AddHandler<HotZoneStateData>(
                GameEvents.HotZoneStateChanged,
                OnHotZoneStateChanged
            );
        }

        private void OnDisable()
        {
            EventManager.Instance?.RemoveHandler<HotZoneStateData>(
                GameEvents.HotZoneStateChanged,
                OnHotZoneStateChanged
            );
        }

        private void LateUpdate()
        {
            UpdateAlpha();
            UpdatePosition();
            ResetFlameLocalPosition();
        }

        #endregion

        #region Private Methods

        private void OnHotZoneStateChanged(HotZoneStateData data)
        {
            _targetAlpha = data.IsPlayerInside ? 1f : 0f;
        }

        private void UpdateAlpha()
        {
            if (_fireCanvasGroup == null) return;
            _fireCanvasGroup.alpha = Mathf.MoveTowards(
                _fireCanvasGroup.alpha,
                _targetAlpha,
                _fadeSpeed * Time.deltaTime
            );
        }

        private void UpdatePosition()
        {
            if (_energyBarImage == null || _fireContainer == null) return;
            if (_fireCanvasGroup != null && _fireCanvasGroup.alpha <= 0.01f) return;

            _energyBarImage.rectTransform.GetWorldCorners(_corners);

            float leftX = _corners[0].x;
            float rightX = _corners[2].x;
            float centerY = (_corners[0].y + _corners[1].y) * 0.5f;

            float fill = _energyBarImage.fillAmount;

            float fireX = leftX + fill * (rightX - leftX) + _offset.x;
            float fireY = centerY + _offset.y;

            _fireContainer.position = new Vector3(fireX, fireY, _fireContainer.position.z);
        }

        private void ResetFlameLocalPosition()
        {
            if (_flameRect == null) return;
            if (_fireCanvasGroup != null && _fireCanvasGroup.alpha <= 0.01f) return;

            _flameRect.anchoredPosition = Vector2.zero;
        }

        #endregion
    }
}
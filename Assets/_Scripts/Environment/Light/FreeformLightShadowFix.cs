using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;

namespace My.Scripts.Environment.Light
{
    /// <summary>
    /// Исправляет баг Unity 2022.3+ с неправильным расчётом bounds для Freeform Light.
    /// Расширяет m_LocalBounds для корректного рендеринга теней на расстоянии.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    [ExecuteAlways]
    public class FreeformLightShadowFix : MonoBehaviour
    {
        [SerializeField] private float _shadowRange = 100f;

        private Light2D _light;
        private FieldInfo _localBoundsField;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            StartCoroutine(ApplyFixNextFrame());
        }

        private void Initialize()
        {
            _light = GetComponent<Light2D>();

            _localBoundsField = typeof(Light2D).GetField("m_LocalBounds",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (_localBoundsField == null)
            {
                Debug.LogError("[FreeformLightShadowFix] Could not find m_LocalBounds field!");
                return;
            }

            _isInitialized = true;
        }

        private IEnumerator ApplyFixNextFrame()
        {
            yield return null;
            ApplyBoundsFix();
        }

        private void LateUpdate()
        {
            ApplyBoundsFix();
        }

        private void ApplyBoundsFix()
        {
            if (!_isInitialized || _light == null || _localBoundsField == null) return;

            // Получаем текущие bounds
            Bounds currentBounds = (Bounds)_localBoundsField.GetValue(_light);

            // Проверяем, достаточно ли большие bounds
            float currentMaxExtent = Mathf.Max(currentBounds.extents.x, currentBounds.extents.y);

            // Если bounds уже достаточно большие — не трогаем
            if (currentMaxExtent >= _shadowRange) return;

            // Создаём фиксированные большие bounds (не множитель!)
            Bounds extendedBounds = new Bounds(
                currentBounds.center,
                new Vector3(_shadowRange * 2, _shadowRange * 2, currentBounds.size.z)
            );

            _localBoundsField.SetValue(_light, extendedBounds);
        }

        [ContextMenu("Log Current Bounds")]
        public void LogCurrentBounds()
        {
            if (!_isInitialized) Initialize();
            if (_localBoundsField == null) return;

            Bounds bounds = (Bounds)_localBoundsField.GetValue(_light);
            Debug.Log($"[FreeformLightShadowFix] Current Bounds:\n" +
                      $"  Center: {bounds.center}\n" +
                      $"  Size: {bounds.size}\n" +
                      $"  Extents: {bounds.extents}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _shadowRange);
        }
    }

}
using DG.Tweening;
using UnityEngine;
using System.Collections;

public class PickupAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float stretchDuration = 0.5f;
    [SerializeField] private float minTimeBetweenAnimations = 3f;
    [SerializeField] private float maxTimeBetweenAnimations = 6f;
    [SerializeField] private float circleRadius = 0.1f;
    [SerializeField] private float circleDuration = 2f;
    [SerializeField] private float blinkDuration = 0.3f;

    private Vector3 originalScale;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;
    private Coroutine stretchCoroutine;
    private Coroutine blinkCoroutine;
    private Coroutine circleCoroutine;
    private Vector3 localCircleCenter;
    private bool isAlive = true;
    private Transform cachedTransform;

    private void Start()
    {
        // Кешируем Transform для производительности
        cachedTransform = transform;

        // Получаем компоненты
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Сохраняем оригинальные значения в ЛОКАЛЬНОМ пространстве
        originalScale = cachedTransform.localScale;
        originalColor = spriteRenderer.color;
        localCircleCenter = cachedTransform.localPosition; // Используем локальную позицию

        // Запускаем постоянное круговое движение в ЛОКАЛЬНОМ пространстве
        circleCoroutine = StartCoroutine(SmoothCircleAnimation());

        // Запускаем корутины для случайных анимаций
        stretchCoroutine = StartCoroutine(StretchAnimationRoutine());
        blinkCoroutine = StartCoroutine(BlinkAnimationRoutine());
    }

    IEnumerator SmoothCircleAnimation()
    {
        float angle = 0f;
        Vector3 startLocalPosition = localCircleCenter;

        while (isAlive)
        {
            // Проверяем, не уничтожен ли объект
            if (!isAlive || cachedTransform == null) yield break;

            // Увеличиваем угол с учетом времени и радиуса
            angle += Time.deltaTime * (2 * Mathf.PI / circleDuration);

            // Вычисляем позицию на окружности в ЛОКАЛЬНОМ пространстве
            float x = Mathf.Cos(angle) * circleRadius;
            float y = Mathf.Sin(angle) * circleRadius;

            // Плавное перемещение в ЛОКАЛЬНОМ пространстве
            cachedTransform.localPosition = startLocalPosition + new Vector3(x, y, 0);

            // Сброс угла при полном обороте
            if (angle >= 2 * Mathf.PI) angle = 0f;

            yield return null;
        }
    }

    private IEnumerator StretchAnimationRoutine()
    {
        while (isAlive)
        {
            // Случайная пауза между анимациями
            yield return new WaitForSeconds(Random.Range(minTimeBetweenAnimations, maxTimeBetweenAnimations));

            if (!isAlive) yield break;

            // Запускаем анимацию растягивания
            StretchAnimation();
        }
    }

    private void StretchAnimation()
    {
        Sequence stretchSequence = DOTween.Sequence();

        // Растягиваем по Y и сжимаем по X в ЛОКАЛЬНОМ пространстве
        stretchSequence.Append(cachedTransform.DOScaleY(originalScale.y * 1.3f, stretchDuration / 2f));
        stretchSequence.Join(cachedTransform.DOScaleX(originalScale.x * 0.7f, stretchDuration / 2f));

        // Возвращаем к исходному размеру в ЛОКАЛЬНОМ пространстве
        stretchSequence.Append(cachedTransform.DOScaleY(originalScale.y, stretchDuration / 2f));
        stretchSequence.Join(cachedTransform.DOScaleX(originalScale.x, stretchDuration / 2f));

        stretchSequence.SetEase(Ease.OutQuad);
        stretchSequence.SetLink(gameObject);
    }

    private IEnumerator BlinkAnimationRoutine()
    {
        while (isAlive)
        {
            // Случайная пауза между морганиями
            yield return new WaitForSeconds(4);

            if (!isAlive) yield break;

            // Запускаем анимацию моргания
            BlinkAnimation();
        }
    }

    void BlinkAnimation()
    {
        Sequence blinkSequence = DOTween.Sequence();

        Color flashColor = new Color(1.5f, 1.5f, 1.5f, originalColor.a);

        blinkSequence.Append(spriteRenderer.DOColor(flashColor, blinkDuration / 3f));
        blinkSequence.Append(spriteRenderer.DOColor(originalColor, blinkDuration / 3f));

        blinkSequence.SetEase(Ease.Flash);
        blinkSequence.SetLink(gameObject);
    }

    private void OnDestroy()
    {
        isAlive = false;

        // Правильно останавливаем все корутины
        if (circleCoroutine != null)
            StopCoroutine(circleCoroutine);

        if (stretchCoroutine != null)
            StopCoroutine(stretchCoroutine);

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        // Отменяем все DOTween анимации для этого объекта
        DOTween.Kill(cachedTransform);
        DOTween.Kill(spriteRenderer);
    }
}
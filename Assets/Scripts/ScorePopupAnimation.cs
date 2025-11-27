using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScorePopupAnimation : MonoBehaviour
{
    [Header("Background References")]
    [SerializeField] private GameObject whiteBackground;
    [SerializeField] private GameObject background;

    [Header("Animation Settings")]
    [SerializeField] private float squeezeDuration = 0.5f;
    [SerializeField] private float backgroundSwitchTime = 0.05f;
    [SerializeField] private float totalLifetime = 1.5f;
    [SerializeField] private float disappearDuration = 0.5f;
    [SerializeField] private Vector3 squeezeScale = new Vector3(1.5f, 0.5f, 1f);

    private Vector3 originalScale;
    private TextMeshPro textMesh;
    private SpriteRenderer whiteBgRenderer;
    private SpriteRenderer bgRenderer;

    private void Awake()
    {
        // Получаем компоненты
        textMesh = GetComponentInChildren<TextMeshPro>();
        if (whiteBackground != null)
            whiteBgRenderer = whiteBackground.GetComponent<SpriteRenderer>();
        if (background != null)
            bgRenderer = background.GetComponent<SpriteRenderer>();

        // Сохраняем оригинальный масштаб
        originalScale = transform.localScale;

        // Настраиваем начальное состояние
        InitializeAnimationState();

        // Запускаем анимацию
        StartAnimationSequence();
    }

    private void InitializeAnimationState()
    {
        // Начальное состояние: белый фон активен, обычный фон неактивен
        if (whiteBackground != null)
            whiteBackground.SetActive(true);
        if (background != null)
            background.SetActive(false);

        // Устанавливаем сжатый масштаб
        transform.localScale = squeezeScale;

        // Делаем текст полностью прозрачным в начале
        if (textMesh != null)
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, 0f);
    }

    private void StartAnimationSequence()
    {
        Sequence mainSequence = DOTween.Sequence();

        // 1. Анимация "распрямления" слайма (0.5 секунд)
        mainSequence.Append(transform.DOScale(originalScale, squeezeDuration)
            .SetEase(Ease.OutElastic, 0.5f, 1f));

        // Одновременно с распрямлением - появление текста
        mainSequence.Join(textMesh.DOFade(1f, squeezeDuration * 0.7f));

        // 2. Переключение фонов на 0.25 секунде
        mainSequence.InsertCallback(backgroundSwitchTime, SwitchBackgrounds);

        // 3. Пауза перед исчезновением
        float pauseDuration = totalLifetime - squeezeDuration - disappearDuration;
        mainSequence.AppendInterval(pauseDuration);

        // 4. Исчезновение (0.5 секунд)
        Sequence disappearSequence = DOTween.Sequence();
        disappearSequence.Append(transform.DOScale(Vector3.zero, disappearDuration)
            .SetEase(Ease.InBack));
        disappearSequence.Join(textMesh.DOFade(0f, disappearDuration));

        // Если фоны активны, тоже анимируем их исчезновение
        if (whiteBgRenderer != null)
            disappearSequence.Join(whiteBgRenderer.DOFade(0f, disappearDuration));
        if (bgRenderer != null)
            disappearSequence.Join(bgRenderer.DOFade(0f, disappearDuration));

        mainSequence.Append(disappearSequence);

        // Уничтожаем объект после завершения анимации
        mainSequence.OnComplete(() => Destroy(gameObject));

        mainSequence.SetLink(gameObject);
    }

    private void SwitchBackgrounds()
    {
        // Переключаем фоны: выключаем белый, включаем обычный
        if (whiteBackground != null)
            whiteBackground.SetActive(false);
        if (background != null)
            background.SetActive(true);
    }

    // Метод для установки текста (совместимость с существующим кодом)
    public void SetText(string text)
    {
        if (textMesh != null)
            textMesh.text = text;
    }

    private void OnDestroy()
    {
        // Останавливаем все твины при уничтожении объекта
        DOTween.Kill(transform);
        if (textMesh != null)
            DOTween.Kill(textMesh);
        if (whiteBgRenderer != null)
            DOTween.Kill(whiteBgRenderer);
        if (bgRenderer != null)
            DOTween.Kill(bgRenderer);
    }
}
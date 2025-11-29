using UnityEngine;
using DG.Tweening;

public class TurretProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 20f;
    [SerializeField] private Ease movementEase = Ease.Linear;

    private Tween movementTween;
    private Tween scaleTween;
    private float lifeTimer;
    private bool isDestroying = false;

    public void LaunchTowards(Vector2 direction)
    {
        Vector2 normalizedDirection = direction.normalized;

        float distance = speed * lifetime;
        Vector2 targetPosition = (Vector2)transform.position + normalizedDirection * distance;

        float moveDuration = distance / speed;

        movementTween = transform.DOMove(targetPosition, moveDuration)
            .SetEase(movementEase)
            .OnComplete(() => {
                DestroyProjectile();
            })
            .SetLink(gameObject); // Автоматическая отмена при уничтожении объекта

        lifeTimer = lifetime;
    }

    private void Update()
    {
        if (isDestroying) return;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            DestroyProjectile();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision2D)
    {
        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        if (isDestroying) return;
        isDestroying = true;

        // Останавливаем движение
        movementTween?.Kill();

        // Анимация исчезновения
        scaleTween = transform.DOScale(Vector3.zero, 0.2f)
            .OnComplete(() => {
                if (gameObject != null)
                    Destroy(gameObject);
            })
            .SetLink(gameObject);
    }
}
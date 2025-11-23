using UnityEngine;
using DG.Tweening;

public class Turret : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rotatingPivot;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 0.3f;
    [SerializeField] private float fireRate = 6f;
    [SerializeField] private float triggerRadius = 25f;
    [SerializeField] private Ease rotationEase = Ease.OutBack;

    private Transform playerTarget;
    private bool isActive = false;
    private float fireTimer;
    private Tween rotationTween;
    private CircleCollider2D triggerCollider;
    private Vector2 initialForward; // Фиксированное начальное направление
    private float initialRotation; // Начальный угол поворота

    // Добавляем свойство для получения текущего направления
    private Vector2 CurrentForward => rotatingPivot.up;

    private void Start()
    {
        // Настройка триггерного коллайдера
        triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = triggerRadius;

        // Сохраняем начальное направление и угол поворота турели
        initialForward = rotatingPivot.up;
        initialRotation = rotatingPivot.eulerAngles.z;

        // Получаем игрока через синглтон
        playerTarget = Lander.Instance?.transform;

        if (playerTarget == null)
        {
            Debug.LogError("Player (Lander) not found!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Lander>(out _) && IsPlayerInFiringArc(other.transform))
        {
            ActivateTurret();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Постоянно проверяем, находится ли игрок в зоне обстрела
        if (isActive && other.TryGetComponent<Lander>(out _) && !IsPlayerInFiringArc(other.transform))
        {
            DeactivateTurret();
        }
        else if (!isActive && other.TryGetComponent<Lander>(out _) && IsPlayerInFiringArc(other.transform))
        {
            ActivateTurret();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<Lander>(out _))
        {
            DeactivateTurret();
        }
    }

    private bool IsPlayerInFiringArc(Transform playerTransform)
    {
        if (playerTransform == null) return false;

        // Получаем направление к игроку относительно турели
        Vector2 directionToPlayer = playerTransform.position - transform.position;

        // Используем ТЕКУЩЕЕ направление турели для определения зоны обстрела
        Vector2 turretForward = CurrentForward;

        // Вычисляем угол между направлением турели и направлением к игроку
        float angle = Vector2.SignedAngle(turretForward, directionToPlayer);

        // Игрок находится в зоне обстрела, если угол между -90 и 90 градусами
        return Mathf.Abs(angle) <= 90f;
    }

    private bool IsAngleInFiringArc(float targetAngle)
    {
        // Преобразуем угол в диапазон -180 до 180
        targetAngle = NormalizeAngle(targetAngle);

        // Получаем текущий угол турели
        float currentTurretAngle = NormalizeAngle(rotatingPivot.eulerAngles.z);

        // Вычисляем разницу между целевым углом и текущим углом турели
        float angleDifference = NormalizeAngle(targetAngle - currentTurretAngle);

        // Проверяем, находится ли угол в пределах -90 до 90 градусов ОТНОСИТЕЛЬНО ТЕКУЩЕГО НАПРАВЛЕНИЯ
        return Mathf.Abs(angleDifference) <= 90f;
    }

    private float NormalizeAngle(float angle)
    {
        // Приводим угол к диапазону -180 до 180
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private void ActivateTurret()
    {
        isActive = true;
        fireTimer = fireRate;
        StartContinuousAiming();
    }

    private void DeactivateTurret()
    {
        isActive = false;
        rotationTween?.Kill();

        // Возвращаем турель в исходное положение
        ReturnToInitialPosition();
    }

    private void ReturnToInitialPosition()
    {
        rotationTween = rotatingPivot.DORotate(
            new Vector3(0, 0, initialRotation),
            rotationSpeed,
            RotateMode.Fast
        ).SetEase(rotationEase);
    }

    private void StartContinuousAiming()
    {
        if (!isActive || playerTarget == null || !IsPlayerInFiringArc(playerTarget))
        {
            DeactivateTurret();
            return;
        }

        Vector2 direction = playerTarget.position - rotatingPivot.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // Ограничиваем угол зоной обстрела ОТНОСИТЕЛЬНО ТЕКУЩЕГО ПОЛОЖЕНИЯ
        targetAngle = ClampAngleToFiringArc(targetAngle);

        rotationTween = rotatingPivot.DORotate(
            new Vector3(0, 0, targetAngle),
            rotationSpeed,
            RotateMode.Fast
        ).SetEase(rotationEase)
         .OnComplete(() => {
             if (isActive)
             {
                 StartContinuousAiming();
             }
         });
    }

    private float ClampAngleToFiringArc(float targetAngle)
    {
        // Нормализуем угол
        targetAngle = NormalizeAngle(targetAngle);

        // Получаем текущий угол турели
        float currentAngle = NormalizeAngle(rotatingPivot.eulerAngles.z);

        // Вычисляем разницу между целевым углом и текущим углом
        float angleDifference = NormalizeAngle(targetAngle - currentAngle);

        // Ограничиваем разницу углов зоной обстрела (-90 до 90 градусов)
        angleDifference = Mathf.Clamp(angleDifference, -90f, 90f);

        // Возвращаем ограниченный угол относительно текущего положения
        return NormalizeAngle(currentAngle + angleDifference);
    }

    private void Update()
    {
        if (!isActive || playerTarget == null || !IsPlayerInFiringArc(playerTarget)) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0)
        {
            Shoot();
            fireTimer = fireRate;
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        // Проверяем, что турель смотрит в допустимом направлении
        float currentAngle = NormalizeAngle(rotatingPivot.eulerAngles.z);
        if (!IsAngleInFiringArc(currentAngle)) return;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        TurretProjectile projectileScript = projectile.GetComponent<TurretProjectile>();

        if (projectileScript != null)
        {
            Vector2 direction = firePoint.up;
            projectileScript.LaunchTowards(direction);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // Визуализация направления пушки
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.up * 2f);
        }

        // Визуализация зоны обстрела - используем ТЕКУЩЕЕ направление
        Vector2 forward = Application.isPlaying ? CurrentForward : (Vector2)rotatingPivot.up;

        Gizmos.color = Color.cyan;
        Vector2 leftBound = Quaternion.Euler(0, 0, 90) * forward;
        Vector2 rightBound = Quaternion.Euler(0, 0, -90) * forward;

        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(forward * triggerRadius));
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(leftBound * triggerRadius));
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(rightBound * triggerRadius));

        // Рисуем дугу для визуализации 180 градусов
        DrawArc(transform.position, forward, triggerRadius, 180f, 20);
    }

    private void DrawArc(Vector2 center, Vector2 forward, float radius, float angle, int segments)
    {
        Vector2 startDir = Quaternion.Euler(0, 0, angle / 2) * forward;
        Vector2 endDir = Quaternion.Euler(0, 0, -angle / 2) * forward;

        Vector2 previousPoint = center + startDir * radius;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float currentAngle = Mathf.Lerp(angle / 2, -angle / 2, t);
            Vector2 currentDir = Quaternion.Euler(0, 0, currentAngle) * forward;
            Vector2 currentPoint = center + currentDir * radius;

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
}
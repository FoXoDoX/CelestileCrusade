using UnityEngine;
using DG.Tweening;
using System;

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

    public static event EventHandler OnTurretShoot;

    private Transform playerTarget;
    private bool isActive = false;
    private float fireTimer;
    private Tween rotationTween;
    private CircleCollider2D triggerCollider;
    private float initialRotation;
    private Vector2 initialForward; // Фиксированное начальное направление

    private void Start()
    {
        triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = triggerRadius;

        initialRotation = rotatingPivot.eulerAngles.z;
        initialForward = rotatingPivot.up; // Сохраняем начальное направление

        playerTarget = Lander.Instance?.transform;

        if (playerTarget == null)
        {
            Debug.LogError("Player (Lander) not found!");
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Lander>(out _))
        {
            CheckPlayerPosition();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent<Lander>(out _))
        {
            CheckPlayerPosition();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<Lander>(out _))
        {
            DeactivateTurret();
        }
    }

    private void CheckPlayerPosition()
    {
        if (playerTarget == null) return;

        bool inFiringArc = IsPlayerInFiringArc(playerTarget);

        if (inFiringArc && !isActive)
        {
            ActivateTurret();
        }
        else if (!inFiringArc && isActive)
        {
            DeactivateTurret();
        }
    }

    private bool IsPlayerInFiringArc(Transform playerTransform)
    {
        if (playerTransform == null) return false;

        Vector2 directionToPlayer = (Vector2)(playerTransform.position - transform.position);

        // Используем ФИКСИРОВАННОЕ начальное направление, а не текущее
        Vector2 turretForward = initialForward;

        float angle = Vector2.SignedAngle(turretForward, directionToPlayer);
        bool inArc = Mathf.Abs(angle) <= 90f;

        return inArc;
    }

    private void ActivateTurret()
    {
        if (isActive) return;

        isActive = true;
        fireTimer = fireRate;
        StartContinuousAiming();
    }

    private void DeactivateTurret()
    {
        if (!isActive) return;

        isActive = false;
        rotationTween?.Kill();
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
        if (!isActive || playerTarget == null)
        {
            return;
        }

        // Проверяем, находится ли игрок в зоне обстрела перед началом прицеливания
        if (!IsPlayerInFiringArc(playerTarget))
        {
            DeactivateTurret();
            return;
        }

        Vector2 direction = playerTarget.position - rotatingPivot.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // Ограничиваем угол поворота фиксированной зоной обстрела
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
        // Нормализуем углы
        targetAngle = NormalizeAngle(targetAngle);
        float currentInitialRotation = NormalizeAngle(initialRotation);

        // Вычисляем разницу между целевым углом и начальным направлением
        float angleDifference = NormalizeAngle(targetAngle - currentInitialRotation);

        // Ограничиваем разницу углов зоной обстрела (-90 до 90 градусов)
        angleDifference = Mathf.Clamp(angleDifference, -90f, 90f);

        // Возвращаем ограниченный угол относительно начального положения
        return NormalizeAngle(currentInitialRotation + angleDifference);
    }

    private float NormalizeAngle(float angle)
    {
        // Приводим угол к диапазону -180 до 180
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private void Update()
    {
        if (!isActive) return;

        // Постоянно проверяем, находится ли игрок в зоне обстрела
        if (playerTarget == null || !IsPlayerInFiringArc(playerTarget))
        {
            DeactivateTurret();
            return;
        }

        // Стрельба
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

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        TurretProjectile projectileScript = projectile.GetComponent<TurretProjectile>();

        if (projectileScript != null)
        {
            Vector2 direction = firePoint.up;
            projectileScript.LaunchTowards(direction);
        }

        OnTurretShoot?.Invoke(this, EventArgs.Empty);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.up * 2f);
        }

        Vector2 forward = Application.isPlaying ? initialForward : (Vector2)rotatingPivot.up;

        Gizmos.color = Color.cyan;
        Vector2 leftBound = Quaternion.Euler(0, 0, 90) * forward;
        Vector2 rightBound = Quaternion.Euler(0, 0, -90) * forward;

        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(forward * triggerRadius));
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(leftBound * triggerRadius));
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(rightBound * triggerRadius));

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
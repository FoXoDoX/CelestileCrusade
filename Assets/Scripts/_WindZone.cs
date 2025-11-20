using UnityEngine;

public class WindZone2D : MonoBehaviour
{
    [Header("Wind Settings")]
    [SerializeField] private float windForce = 10f;
    [SerializeField][Range(0, 360)] private float windDirection = 0f;
    [SerializeField] private ForceMode2D forceMode = ForceMode2D.Force;
    [SerializeField] private bool showGizmos = true;

    private Vector2 windVector;
    private Lander playerLander;
    private Collider2D windCollider;

    private void OnValidate()
    {
        UpdateWindDirection();
    }

    private void Awake()
    {
        UpdateWindDirection();
        windCollider = GetComponent<Collider2D>();

        // Настраиваем коллайдер если он есть
        if (windCollider != null)
        {
            windCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        // Получаем ссылку на игрока через синглтон
        playerLander = Lander.Instance;
    }

    private void Update()
    {
        if (playerLander == null) return;

        // Проверяем находится ли игрок в зоне ветра
        if (windCollider != null && windCollider.OverlapPoint(playerLander.transform.position))
        {
            ApplyWindForce();
        }
    }

    private void ApplyWindForce()
    {
        Rigidbody2D playerRb = playerLander.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.AddForce(windVector, forceMode);
        }
    }

    private void UpdateWindDirection()
    {
        // Конвертируем градусы в вектор направления
        float rad = windDirection * Mathf.Deg2Rad;
        windVector = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * windForce;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Получаем коллайдер (в редакторе может быть не инициализирован)
        Collider2D col = windCollider != null ? windCollider : GetComponent<Collider2D>();
        if (col == null) return;

        // Вычисляем центр коллайдера в мировых координатах
        Vector2 colliderCenter = GetColliderCenter(col);

        // Рисуем область коллайдера
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);

        // Обрабатываем разные типы коллайдеров
        if (col is BoxCollider2D boxCollider)
        {
            Vector2 size = boxCollider.size;
            Vector2 scale = transform.lossyScale;
            Vector2 scaledSize = new Vector2(size.x * scale.x, size.y * scale.y);
            Gizmos.DrawCube(colliderCenter, scaledSize);
        }
        else if (col is CircleCollider2D circleCollider)
        {
            float radius = circleCollider.radius;
            Vector2 scale = transform.lossyScale;
            float maxScale = Mathf.Max(scale.x, scale.y);
            Gizmos.DrawSphere(colliderCenter, radius * maxScale);
        }
        else if (col is PolygonCollider2D || col is EdgeCollider2D)
        {
            // Для сложных коллайдеров рисуем bounding box
            Bounds bounds = col.bounds;
            Gizmos.DrawCube(bounds.center, bounds.size);
        }

        // Рисуем направление ветра из центра коллайдера
        Gizmos.color = Color.blue;
        Vector3 direction = new Vector3(windVector.x, windVector.y, 0).normalized;

        // Определяем длину стрелки в зависимости от размера коллайдера
        float arrowLength = GetColliderSize(col) * 0.8f;

        Vector3 start = colliderCenter;
        Vector3 end = start + direction * arrowLength;
        Gizmos.DrawLine(start, end);

        // Стрелка направления
        Vector3 right = Quaternion.Euler(0, 0, 30) * -direction * arrowLength * 0.3f;
        Vector3 left = Quaternion.Euler(0, 0, -30) * -direction * arrowLength * 0.3f;
        Gizmos.DrawLine(end, end + right);
        Gizmos.DrawLine(end, end + left);
    }

    private Vector2 GetColliderCenter(Collider2D collider)
    {
        if (collider is BoxCollider2D boxCollider)
        {
            return transform.TransformPoint(boxCollider.offset);
        }
        else if (collider is CircleCollider2D circleCollider)
        {
            return transform.TransformPoint(circleCollider.offset);
        }
        else
        {
            return collider.bounds.center;
        }
    }

    private float GetColliderSize(Collider2D collider)
    {
        if (collider is BoxCollider2D boxCollider)
        {
            Vector2 size = boxCollider.size;
            Vector2 scale = transform.lossyScale;
            return Mathf.Max(size.x * scale.x, size.y * scale.y);
        }
        else if (collider is CircleCollider2D circleCollider)
        {
            float radius = circleCollider.radius;
            Vector2 scale = transform.lossyScale;
            float maxScale = Mathf.Max(scale.x, scale.y);
            return radius * maxScale * 2f; // Диаметр
        }
        else
        {
            Bounds bounds = collider.bounds;
            return Mathf.Max(bounds.size.x, bounds.size.y);
        }
    }

    public void SetWindDirection(float degrees)
    {
        windDirection = degrees;
        UpdateWindDirection();
    }

    public void SetWindForce(float force)
    {
        windForce = force;
        UpdateWindDirection();
    }
}
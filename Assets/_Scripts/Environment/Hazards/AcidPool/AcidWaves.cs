using UnityEngine;
using UnityEngine.U2D;

public class AcidWaves : MonoBehaviour
{
    [Header("References")]
    private SpriteShapeController shapeController;

    [Header("Wave Settings")]
    public int surfacePointStartIndex = 0;  // индекс первой точки поверхности
    public int surfacePointEndIndex = 10;    // индекс последней точки поверхности
    public float waveAmplitude = 0.1f;       // высота волн
    public float waveFrequency = 2f;         // частота волн
    public float waveSpeed = 2f;             // скорость движения

    private Vector3[] originalPositions;

    void Awake()
    {
        shapeController = GetComponent<SpriteShapeController>();
    }

    void Start()
    {
        // Сохраняем оригинальные позиции точек поверхности
        SaveOriginalPositions();
    }

    void Update()
    {
        AnimateWaves();
    }

    void SaveOriginalPositions()
    {
        var spline = shapeController.spline;
        int count = surfacePointEndIndex - surfacePointStartIndex + 1;
        originalPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            originalPositions[i] = spline.GetPosition(surfacePointStartIndex + i);
        }
    }

    void AnimateWaves()
    {
        var spline = shapeController.spline;

        for (int i = surfacePointStartIndex; i <= surfacePointEndIndex; i++)
        {
            int localIndex = i - surfacePointStartIndex;
            Vector3 pos = originalPositions[localIndex];

            // Синусоида для волны
            float wave = Mathf.Sin(
                pos.x * waveFrequency + Time.time * waveSpeed
            ) * waveAmplitude;

            // Второй слой для естественности
            float wave2 = Mathf.Sin(
                pos.x * waveFrequency * 2.3f + Time.time * waveSpeed * 1.5f
            ) * waveAmplitude * 0.3f;

            spline.SetPosition(i, new Vector3(
                pos.x,
                pos.y + wave + wave2,
                pos.z
            ));
        }

        // Обязательно — обновить меш
        shapeController.BakeMesh();
    }
}
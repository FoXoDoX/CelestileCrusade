using UnityEngine;

public class GameLevel : MonoBehaviour
{
    [SerializeField] private int levelNumber;
    [SerializeField] private Transform landerStartPositionTransform;
    [SerializeField] private Transform cameraStartTargetTransform;
    [SerializeField] private float zoomedOutOrthographicSize;
    [SerializeField] private int[] starThresholds = new int[3];

    public int GetLevelNumber()
    {
        return levelNumber;
    }

    public Vector3 GetLanderStartPosition()
    {
        return landerStartPositionTransform.position;
    }

    public Transform GetCameraStartTargetTransform()
    {
        return cameraStartTargetTransform;
    }

    public float GetZoomedOutOrthographicSize()
    {
        return zoomedOutOrthographicSize;
    }

    public int[] GetStarThresholds()
    {
        return starThresholds;
    }

    public int GetEarnedStarsCount(int score)
    {
        int stars = 0;
        for (int i = 0; i < starThresholds.Length; i++)
        {
            if (score >= starThresholds[i])
            {
                stars++;
            }
        }
        return stars;
    }
}

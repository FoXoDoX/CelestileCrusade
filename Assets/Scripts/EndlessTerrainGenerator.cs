using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrainGenerator : MonoBehaviour
{
    private const float LANDER_DISTANCE_SPAWN_TERRAIN_PART = 100f;

    [SerializeField] private Transform terrainEndPositionRight;
    [SerializeField] private Transform terrainEndPositionLeft;
    [SerializeField] private List<Transform> terrainPartsList;

    private Vector3 lastEndPositionRight;
    private Vector3 lastEndPositionLeft;

    private void Awake()
    {
        lastEndPositionRight = terrainEndPositionRight.position - new Vector3(3f, 6f, 0);
        lastEndPositionLeft = terrainEndPositionLeft.position - new Vector3(-3f, 6f, 0);

        int startingSpawnTerrainParts = 3;

        for (int i = 0; i < startingSpawnTerrainParts; i++)
        {
            SpawnTerrainPart(Side.Right);
            SpawnTerrainPart(Side.Left);
        }
    }

    private void Update()
    {
        CheckAndSpawnTerrain(Side.Right);
        CheckAndSpawnTerrain(Side.Left);
    }

    private void CheckAndSpawnTerrain(Side side)
    {
        Vector3 landerPosition = Lander.Instance.transform.position;
        Vector3 targetPosition = side == Side.Right ? lastEndPositionRight : lastEndPositionLeft;

        if (Vector3.Distance(landerPosition, targetPosition) < LANDER_DISTANCE_SPAWN_TERRAIN_PART)
        {
            SpawnTerrainPart(side);
        }
    }

    private void SpawnTerrainPart(Side side)
    {
        Transform chosenTerrainPart = terrainPartsList[Random.Range(0, terrainPartsList.Count)];
        Vector3 spawnPosition = side == Side.Right ? lastEndPositionRight : lastEndPositionLeft;
        Quaternion rotation = side == Side.Right ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

        Transform terrainPartTransform = Instantiate(chosenTerrainPart, spawnPosition, rotation);
        Vector3 newEndPosition = terrainPartTransform.GetChild(0).position;

        if (side == Side.Right)
            lastEndPositionRight = newEndPosition;
        else
            lastEndPositionLeft = newEndPosition;
    }

    private enum Side
    {
        Right,
        Left
    }
}
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ChessPieceSpawnPointManager : MonoBehaviour
{
    public static ChessPieceSpawnPointManager Instance { get; private set; }

    [Header("棋子生成点")]
    [SerializeField] private List<Transform> chessPieceSpawnPoints = new List<Transform>();
    [SerializeField, Min(0f)] private float occupiedDistance = 0.25f;

    public IReadOnlyList<Transform> ChessPieceSpawnPoints => chessPieceSpawnPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool TryGetSpawnPose(int index, out Vector3 position, out Quaternion rotation)
    {
        Transform spawnPoint = GetSpawnPoint(index);
        if (spawnPoint == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = spawnPoint.position;
        rotation = spawnPoint.rotation;
        return true;
    }

    public bool TryGetNextAvailableSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        if (chessPieceSpawnPoints == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        for (int i = 0; i < chessPieceSpawnPoints.Count; i++)
        {
            Transform spawnPoint = chessPieceSpawnPoints[i];
            if (spawnPoint == null || IsSpawnPointOccupied(spawnPoint.position))
            {
                continue;
            }

            position = spawnPoint.position;
            rotation = spawnPoint.rotation;
            return true;
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    private Transform GetSpawnPoint(int index)
    {
        if (chessPieceSpawnPoints == null || index < 0 || index >= chessPieceSpawnPoints.Count)
        {
            return null;
        }

        return chessPieceSpawnPoints[index];
    }

    private bool IsSpawnPointOccupied(Vector3 position)
    {
        ChessBossEnemy[] chessEnemies = FindObjectsByType<ChessBossEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < chessEnemies.Length; i++)
        {
            ChessBossEnemy chessEnemy = chessEnemies[i];
            if (chessEnemy == null || chessEnemy.IsDead)
            {
                continue;
            }

            if (Vector3.Distance(chessEnemy.transform.position, position) <= occupiedDistance)
            {
                return true;
            }
        }

        return false;
    }
}
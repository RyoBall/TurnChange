using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋局Boss战棋子站位管理器（单例）
/// 管理四个兵卒生成点与皇后生成点
/// </summary>
[DisallowMultipleComponent]
public class ChessStandPositionManager : MonoBehaviour
{
    public static ChessStandPositionManager Instance { get; private set; }

    [Header("兵卒生成点（按顺序对应 standPosition 3~6）")]
    [SerializeField] private Transform[] pawnSpawnPoints = new Transform[4];

    [Header("皇后生成点")]
    [SerializeField] private Transform queenSpawnPoint;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of ChessStandPositionManager detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public Transform GetPawnStandPosition(int standPosition)
    {
        if (standPosition < 3 || standPosition > 6)
        {
            Debug.LogError($"Invalid pawn stand position: {standPosition}");
            return null;
        }
        int index = standPosition - 3;
        if (index >= 0 && index < pawnSpawnPoints.Length)
        {
            return pawnSpawnPoints[index];
        }
        Debug.LogError($"Pawn spawn point not found for stand position: {standPosition}");
        return null;
    }
    public Transform GetQueenStandPosition()
    {
        return queenSpawnPoint;
    }
}

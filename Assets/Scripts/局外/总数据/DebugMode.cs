using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug 模式单例，用于控制是否启用调试功能
/// </summary>
public class DebugMode : MonoBehaviour
{
    public static DebugMode Instance { get; private set; }

    [SerializeField] private bool m_isDebugMode;

    /// <summary>
    /// Debug 模式下自动注入到 Datas 的角色列表
    /// </summary>
    [SerializeField] private List<CharacterType> m_debugCharacterTypes = new List<CharacterType>();

    public bool IsDebugMode => m_isDebugMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InjectDebugCharacters();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    /// <summary>
    /// 将 Debug 角色列表中的角色注入到 Datas（Datas.AddCharacterData 内部已确保不会重复添加）
    /// </summary>
    private void InjectDebugCharacters()
    {
        if (!m_isDebugMode)
        {
            return;
        }

        if (Datas.Instance == null)
        {
            Debug.LogWarning("[DebugMode] Datas.Instance 为空，无法注入 Debug 角色。");
            return;
        }

        for (int i = 0; i < m_debugCharacterTypes.Count; i++)
        {
            Datas.Instance.AddCharacterData(m_debugCharacterTypes[i]);
        }
    }
}

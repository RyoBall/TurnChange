using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public enum CharacterType
{
    DotMain,
    DotSub,
    DotSupport,
    DirectMain,
    DirectSub,
    DirectSupport
}

[DisallowMultipleComponent]
public class StarterBranchRuntimeController : MonoBehaviour
{
    [SerializeField] private StarterBranchConfig starterBranchConfig;
    private Datas m_datas;
    private bool m_sceneLoadedSubscribed;
    private void Start()
    {
        Initialize(Datas.Instance);
        ApplyInitialRosterState();
    }

    private void OnDestroy()
    {
        UnsubscribeDatasEvents();
    }

    private void Initialize(Datas datas)
    {
        if (datas == null)
        {
            return;
        }

        if (m_datas == datas && m_sceneLoadedSubscribed)
        {
            return;
        }

        m_datas = datas;
        UnsubscribeDatasEvents();
        m_datas.LevelCompleted += HandleLevelCompleted;
        m_sceneLoadedSubscribed = true;
    }
    // 根据当前已选流派和关卡完成情况，确保角色列表正确。通常在游戏开始时调用一次，之后每次关卡完成后调用以同步角色解锁。
    private void ApplyInitialRosterState()
    {
        if (m_datas == null)
        {
            return;
        }

        if (!m_datas.HasSelectedStarterBranch)
        {
            bool hadCharacters = m_datas.GetUnlockedCharacterRosters().Count > 0;
            if (hadCharacters)
            {
                m_datas.NotifyCharacterRosterChanged();
            }

            return;
        }

        SynchronizeStarterBranchProgression(false);
    }
    // 关卡完成后同步角色解锁状态，确保玩家获得已选流派对应的角色。
    private void HandleLevelCompleted(string levelId)
    {
        SynchronizeStarterBranchProgression();
    }
    //加载场景时调用
    private void UnsubscribeDatasEvents()
    {
        if (m_datas == null)
        {
            return;
        }

        m_datas.LevelCompleted -= HandleLevelCompleted;
    }

    // 根据当前已选流派和关卡完成情况，确保角色列表正确。通常在游戏开始时调用一次，之后每次关卡完成后调用以同步角色解锁。
    private void SynchronizeStarterBranchProgression(bool notify = true)
    {
        if (m_datas == null)
        {
            return;
        }

        if (!m_datas.HasSelectedStarterBranch)
        {
            if (notify)
            {
                m_datas.NotifyCharacterRosterChanged();
            }

            return;
        }

        bool rosterChanged = false;
        StarterBranchDefinition selectedBranch = GetStarterBranch(m_datas.SelectedStarterBranchId);

        rosterChanged |= AddBranchCoreCharacters(selectedBranch);

        rosterChanged |= AddConfiguredFollowupUnlocks(selectedBranch);

        if (notify && (rosterChanged || m_datas.GetUnlockedCharacterRosters().Count > 0))
        {
            m_datas.NotifyCharacterRosterChanged();
        }
    }
    // 添加开局流派的核心角色（主C和副C）到角色列表。返回是否有新增角色被添加。通常在选择流派后调用以确保玩家获得开局角色。
    private bool AddBranchCoreCharacters(StarterBranchDefinition branch)
    {
        if (branch == null)
        {
            return false;
        }

        bool changed = false;
        changed |= m_datas.AddCharacterData(branch.primaryCharacterType);
        changed |= m_datas.AddCharacterData(branch.secondaryCharacterType);
        return changed;
    }
    // 根据已选流派的后续解锁配置和当前关卡完成情况，添加对应角色到角色列表。通常在关卡完成后调用以同步角色解锁。
    private bool AddConfiguredFollowupUnlocks(StarterBranchDefinition branch)
    {
        if (branch == null || branch.followupUnlocks == null)
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < branch.followupUnlocks.Count; i++)
        {
            StarterBranchUnlockEntry unlockEntry = branch.followupUnlocks[i];
            if (unlockEntry == null
                || string.IsNullOrWhiteSpace(unlockEntry.levelId)
                || !m_datas.IsLevelCompleted(unlockEntry.levelId))
            {
                continue;
            }

            changed |= m_datas.AddCharacterData(unlockEntry.characterType);
        }

        return changed;
    }
    //根据ID获取流派定义
    private StarterBranchDefinition GetStarterBranch(string branchId)
    {
        if (string.IsNullOrWhiteSpace(branchId))
        {
            return null;
        }

        IReadOnlyList<StarterBranchDefinition> starterBranches = GetStarterBranches();
        for (int i = 0; i < starterBranches.Count; i++)
        {
            StarterBranchDefinition branch = starterBranches[i];
            if (branch != null && string.Equals(branch.branchId, branchId, StringComparison.Ordinal))
            {
                return branch;
            }
        }

        return null;
    }
    private IReadOnlyList<StarterBranchDefinition> GetStarterBranches()
    {
        return starterBranchConfig != null ? starterBranchConfig.StarterBranches : Array.Empty<StarterBranchDefinition>();
    }

}
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StarterBranchUnlockEntry
{
    public string levelId;
    public CharacterType characterType;
}

[Serializable]
public class StarterBranchDefinition
{
    public string branchId;
    public string displayName;
    [TextArea(2, 4)] public string description;
    public CharacterType primaryCharacterType;
    public CharacterType secondaryCharacterType;
    public CharacterType supportCharacterType;
    public List<StarterBranchUnlockEntry> followupUnlocks = new List<StarterBranchUnlockEntry>();
    public Color accentColor = new Color(0.2f, 0.47f, 0.85f, 1f);
}

[CreateAssetMenu(fileName = "StarterBranchConfig", menuName = "Config/Starter Branch Config")]
public class StarterBranchConfig : ScriptableObject
{
    [SerializeField] private List<StarterBranchDefinition> starterBranches = new List<StarterBranchDefinition>();

    public IReadOnlyList<StarterBranchDefinition> StarterBranches => starterBranches;
}
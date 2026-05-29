using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

[Serializable]
public class AnimationClipOverrideEntry
{
    public string layerName;
    public string stateName;
    public AnimationClip overrideClip;

    [SerializeField, HideInInspector] private AnimationClip originalClip;

    public AnimationClip OriginalClip => originalClip;

#if UNITY_EDITOR
    public void SetOriginalClip(AnimationClip clip)
    {
        originalClip = clip;
    }
#endif
}

[Serializable]
public class CharacterAnimationOverrideEntry
{
    public CharacterType characterType;
    public List<AnimationClipOverrideEntry> clipOverrides = new List<AnimationClipOverrideEntry>();
}

[CreateAssetMenu(fileName = "CharacterAnimationOverrideDatabase", menuName = "Character/Animation Override Database")]
public class CharacterAnimationOverrideDatabase : ScriptableObject
{
    [SerializeField] private RuntimeAnimatorController baseController;
    [SerializeField] private List<CharacterAnimationOverrideEntry> characterOverrides = new List<CharacterAnimationOverrideEntry>();

    public bool TryGetCharacterOverrides(CharacterType targetCharacterType, out List<AnimationClipOverrideEntry> clipOverrides)
    {
        clipOverrides = null;

        for (int i = 0; i < characterOverrides.Count; i++)
        {
            CharacterAnimationOverrideEntry entry = characterOverrides[i];
            if (entry == null || entry.characterType != targetCharacterType)
            {
                continue;
            }

            clipOverrides = entry.clipOverrides;
            return clipOverrides != null && clipOverrides.Count > 0;
        }

        return false;
    }

    public bool TryGetCharacterOverrides(string targetCharacterID, out List<AnimationClipOverrideEntry> clipOverrides)
    {
        clipOverrides = null;
        if (string.IsNullOrWhiteSpace(targetCharacterID) || Datas.Instance == null)
        {
            return false;
        }

        for (int i = 0; i < characterOverrides.Count; i++)
        {
            CharacterAnimationOverrideEntry entry = characterOverrides[i];
            if (entry == null)
            {
                continue;
            }

            if (!Datas.Instance.TryGetCharacterId(entry.characterType, out string resolvedCharacterID))
            {
                continue;
            }

            if (!string.Equals(resolvedCharacterID, targetCharacterID, StringComparison.Ordinal))
            {
                continue;
            }

            clipOverrides = entry.clipOverrides;
            return clipOverrides != null && clipOverrides.Count > 0;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildOriginalClipBindings();
    }

    private void RebuildOriginalClipBindings()
    {
        Dictionary<string, AnimationClip> stateClipMap = BuildStateClipMap();

        for (int i = 0; i < characterOverrides.Count; i++)
        {
            CharacterAnimationOverrideEntry characterEntry = characterOverrides[i];
            if (characterEntry == null || characterEntry.clipOverrides == null)
            {
                continue;
            }

            for (int j = 0; j < characterEntry.clipOverrides.Count; j++)
            {
                AnimationClipOverrideEntry clipEntry = characterEntry.clipOverrides[j];
                if (clipEntry == null)
                {
                    continue;
                }

                string lookupKey = BuildStateKey(clipEntry.layerName, clipEntry.stateName);
                if (string.IsNullOrEmpty(lookupKey))
                {
                    clipEntry.SetOriginalClip(null);
                    continue;
                }

                stateClipMap.TryGetValue(lookupKey, out AnimationClip originalClip);
                clipEntry.SetOriginalClip(originalClip);
            }
        }
    }

    private Dictionary<string, AnimationClip> BuildStateClipMap()
    {
        Dictionary<string, AnimationClip> stateClipMap = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
        AnimatorController animatorController = baseController as AnimatorController;
        if (animatorController == null)
        {
            return stateClipMap;
        }

        for (int i = 0; i < animatorController.layers.Length; i++)
        {
            AnimatorControllerLayer layer = animatorController.layers[i];
            CollectStateClips(layer.name, layer.stateMachine, stateClipMap);
        }

        return stateClipMap;
    }

    private void CollectStateClips(string layerName, AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> stateClipMap)
    {
        if (stateMachine == null)
        {
            return;
        }

        for (int i = 0; i < stateMachine.states.Length; i++)
        {
            ChildAnimatorState childState = stateMachine.states[i];
            if (childState.state == null)
            {
                continue;
            }

            AnimationClip clip = childState.state.motion as AnimationClip;
            if (clip == null)
            {
                continue;
            }

            string stateKey = BuildStateKey(layerName, childState.state.name);
            if (!string.IsNullOrEmpty(stateKey))
            {
                stateClipMap[stateKey] = clip;
            }
        }

        for (int i = 0; i < stateMachine.stateMachines.Length; i++)
        {
            CollectStateClips(layerName, stateMachine.stateMachines[i].stateMachine, stateClipMap);
        }
    }

    private static string BuildStateKey(string layerName, string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return string.Empty;
        }

        string normalizedLayerName = string.IsNullOrWhiteSpace(layerName) ? "Base Layer" : layerName.Trim();
        return normalizedLayerName + "/" + stateName.Trim();
    }

    [ContextMenu("Refresh State Bindings")]
    private void RefreshStateBindings()
    {
        RebuildOriginalClipBindings();
        EditorUtility.SetDirty(this);
    }
#endif
}
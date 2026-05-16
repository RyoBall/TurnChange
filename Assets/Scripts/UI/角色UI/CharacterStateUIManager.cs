using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStateUIManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private List<CharacterStateUIItem> characterUIs;
    [SerializeField] private float switchFadeDuration = 0.2f;


    private void Start()
    {
        var characterManager = CharacterManager.Instance;
        if (characterManager == null)
        {
            Debug.LogWarning("[CharacterStateUIManager] CharacterManager.Instance 为空，无法初始化角色UI");
            return;
        }

        characterManager.OnFieldCharacterSwapped += OnFieldCharacterSwapped;

        for (int i = 0; i < characterUIs.Count; i++)
        {
            characterUIs[i].Initialize(characterManager.fieldCharacters.Count > i ? characterManager.fieldCharacters[i] : null);
        }
    }

    private void OnDestroy()
    {
        var characterManager = CharacterManager.Instance;
        if (characterManager != null)
        {
            characterManager.OnFieldCharacterSwapped -= OnFieldCharacterSwapped;
        }
    }

    private void OnFieldCharacterSwapped(Character oldCharacter, Character newCharacter)
    {
        if (characterUIs == null || characterUIs.Count == 0)
        {
            return;
        }

        for (int i = 0; i < characterUIs.Count; i++)
        {
            if (characterUIs[i] != null && characterUIs[i].CurrentCharacter == oldCharacter)
            {
                StartCoroutine(characterUIs[i].PlaySwitch(newCharacter, switchFadeDuration));
                break;
            }
        }
    }
}

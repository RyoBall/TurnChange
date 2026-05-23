using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterData
{
    [SerializeField] private CharacterRosterData characterRosterData;
    [SerializeField] private int characterLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float expToNextLevel = 100f;
    public float GetCurrentExp() => currentExp;
    public float GetExpToNextLevel() => expToNextLevel;

    public CharacterRosterData GetRosterData()
    {
        return characterRosterData;
    }

    public string GetCharacterName()
    {
        if (characterRosterData == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(characterRosterData.characterName)
            ? characterRosterData.characterID
            : characterRosterData.characterName;
    }

    public string GetCharacterID()
    {
        return characterRosterData != null ? characterRosterData.characterID : string.Empty;
    }

    public Sprite GetPortraitSprite()
    {
        return characterRosterData != null ? characterRosterData.portraitSprite : null;
    }

    public CharacterRosterData GetRosterDataOrNull()
    {
        return characterRosterData;
    }

    public int GetLevel()
    {
        return Mathf.Clamp(characterLevel, 1, 99);
    }
}

public class Datas : MonoBehaviour
{
    public static Datas Instance;
    public List<CharacterData> characterDatas = new List<CharacterData>();

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IReadOnlyList<CharacterData> GetCharacterDatas()
    {
        return characterDatas;
    }
}

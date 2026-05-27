using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterRosterData", menuName = "Level/Character Roster Data")]
public class CharacterRosterData : ScriptableObject
{
    public CharacterType characterType;
    public string characterName;
    public string characterID;
    public Sprite portraitSprite;

    public List<CharacterSkillType> skills = new List<CharacterSkillType>();
    public CharacterSkillType enterSkill;

    public string GetCharacterId()
    {
        return string.IsNullOrWhiteSpace(characterID) ? string.Empty : characterID;
    }

    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(characterName) ? GetCharacterId() : characterName;
    }

    public Sprite GetPortraitSprite()
    {
        return portraitSprite;
    }
}

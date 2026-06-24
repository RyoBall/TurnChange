using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterRosterData", menuName = "Level/Character Roster Data")]
public class CharacterRosterData : ScriptableObject
{
    public CharacterType characterType;
    public string characterName;
    public string characterID;
    public Sprite portraitSprite;
    public Sprite illustrationSprite;
    public Vector2 illustrationSize;
    public Sprite preparationIllustrationSprite;
    public Vector2 preparationIllustrationSize;

    public List<CharacterSkillType> skills = new List<CharacterSkillType>();
    public CharacterSkillType enterSkill;
    public CharacterSkillType additionalSkill;

    [Header("角色职责介绍（技能页第三面板）")]
    [TextArea(1, 2)]
    public string characterRoleTitle;

    [TextArea(5, 15)]
    public string characterRoleDescription;

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

    public Sprite GetIllustrationSprite()
    {
        return illustrationSprite;
    }

    public Vector2 GetIllustrationSize()
    {
        return illustrationSize;
    }

    public Sprite GetPreparationIllustrationSprite()
    {
        return preparationIllustrationSprite != null ? preparationIllustrationSprite : illustrationSprite;
    }

    public Vector2 GetPreparationIllustrationSize()
    {
        if (preparationIllustrationSprite != null
            && preparationIllustrationSize.x > 0f
            && preparationIllustrationSize.y > 0f)
        {
            return preparationIllustrationSize;
        }

        return illustrationSize;
    }
}

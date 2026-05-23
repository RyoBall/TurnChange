using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterRosterData", menuName = "Level/Character Roster Data")]
public class CharacterRosterData : ScriptableObject
{
    public string characterName;
    public string characterID;
    public Sprite portraitSprite;

    public List<CharacterSkillType> skills = new List<CharacterSkillType>();
    public CharacterSkillType enterSkill;
    public CharacterSkillType exitSkill;
}

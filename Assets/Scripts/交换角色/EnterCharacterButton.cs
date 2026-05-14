using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MoreMountains.Feedbacks;

public class EnterCharacterButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Character character;
    [SerializeField] private MMF_Player enterFeedback;
    [SerializeField] private MMF_Player exitFeedback;
    public void Initialize(Character character)
    {
        this.character = character;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CharacterManager.Instance.IsSelectingReserveCharacter)
        {
            var targetCharacter = character != null ? character : GetComponentInParent<Character>();
            if (targetCharacter != null)
            {
                SkillDescription.Instance.ChangeDescription(SkillDictionaryManager.GetSkill(targetCharacter.enterSkill));
            }
            enterFeedback?.PlayFeedbacks();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CharacterManager.Instance.IsSelectingReserveCharacter)
        {
            SkillDescription.Instance.ChangeDescription(null);
        }
        exitFeedback?.PlayFeedbacks();
    }
}

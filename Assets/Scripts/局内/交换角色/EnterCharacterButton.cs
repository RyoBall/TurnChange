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
    private Button m_button;

    private void Awake()
    {
        m_button = GetComponent<Button>();
    }

    public void Initialize(Character character)
    {
        this.character = character;
        if (m_button == null)
        {
            m_button = GetComponent<Button>();
        }

        if (m_button != null)
        {
            m_button.interactable = character != null && !character.IsSwapOnCooldown;
        }
    }

    private bool CanRespondToPointer()
    {
        return CharacterManager.Instance != null
            && CharacterManager.Instance.IsSelectingReserveCharacter
            && m_button != null
            && m_button.interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CanRespondToPointer())
        {
            var targetCharacter = character != null ? character : GetComponentInParent<Character>();
            if (targetCharacter != null)
            {
                SkillDescription.Instance.ChangeDescription(targetCharacter.GetEnterSkillInstance());
            }
            enterFeedback?.PlayFeedbacks();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CanRespondToPointer())
        {
            SkillDescription.Instance.HideDescription();
            exitFeedback?.PlayFeedbacks();
        }
    }
}

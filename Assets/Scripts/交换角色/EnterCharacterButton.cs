using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnterCharacterButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Character character;

    private Vector3 baseScale;
    private Tween hoverTween;

    public void Initialize(Character character)
    {
        this.character = character;
    }

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var targetCharacter = character != null ? character : GetComponentInParent<Character>();
        if (targetCharacter != null)
        {
            SkillDescription.Instance.ChangeDescription(SkillDictionaryManager.GetSkill(targetCharacter.enterSkill));
        }

        PlayEnterAnimation();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SkillDescription.Instance.ChangeDescription(null);
        PlayExitAnimation();
    }

    private void PlayEnterAnimation()
    {
        if (hoverTween != null && hoverTween.IsActive())
        {
            hoverTween.Kill();
        }

        hoverTween = transform.DOScale(baseScale * 1.2f, 0.12f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                hoverTween = transform.DOScale(baseScale, 0.3f)
                    .SetEase(Ease.OutElastic, 1.2f, 0.4f);
            });
    }

    private void PlayExitAnimation()
    {
        if (hoverTween != null && hoverTween.IsActive())
        {
            hoverTween.Kill();
        }

        hoverTween = transform.DOScale(baseScale, 0.25f)
            .SetEase(Ease.OutElastic, 1.2f, 0.4f);
    }
}

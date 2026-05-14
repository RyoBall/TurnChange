using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
public class SkillDescription : MonoBehaviour
{
    public static SkillDescription Instance { get; private set; }
    private Sequence currentSequence;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public TMP_Text skillDesText;
    public CanvasGroup canvasGroup;
    void Start()
    {
        if (skillDesText == null)
        {
            skillDesText = GetComponentInChildren<TMP_Text>();
        }
        skillDesText.text = "";
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0;
    }
    public void ChangeDescription(SkillBase skill = null)
    {
        if(currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
        currentSequence = DOTween.Sequence();
        if (skill != null)
        {
            skillDesText.text = skill.description;
            currentSequence.Join(canvasGroup.DOFade(1, 0.3f).SetEase(Ease.InOutQuad));
            currentSequence.Join(BackgroundManager.Instance.ChangeBackground(true));
        }
        else
        {
            currentSequence.Join(canvasGroup.DOFade(0, 0.3f).SetEase(Ease.InOutQuad));
            currentSequence.Join(BackgroundManager.Instance.ChangeBackground(false));
            currentSequence.AppendCallback(() => skillDesText.text = "");
        }
    }
}

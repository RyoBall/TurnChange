using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class CharacterStateUIItem : MonoBehaviour
{
    [Header("UI绑定")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UnitHealthUIController healthBar;
    [SerializeField] private ChaosPointSlotUI chaosPointSlotUI;
    [SerializeField] private ShieldSlider shieldSlider;

    private Character currentCharacter;

    public Character CurrentCharacter => currentCharacter;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void Initialize(Character character)
    {
        currentCharacter = character;
        IntializeChildUI();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = currentCharacter == null ? 0f : 1f;
            canvasGroup.interactable = currentCharacter != null;
            canvasGroup.blocksRaycasts = currentCharacter != null;
        }
    }

    public IEnumerator PlaySwitch(Character newCharacter, float fadeDuration)
    {
        if (newCharacter == currentCharacter)
        {
            yield break;
        }

        if (canvasGroup == null)
        {
            Initialize(newCharacter);
            yield break;
        }

        yield return canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();

        currentCharacter = newCharacter;
        IntializeChildUI();

        canvasGroup.interactable = currentCharacter != null;
        canvasGroup.blocksRaycasts = currentCharacter != null;
        yield return canvasGroup.DOFade(currentCharacter == null ? 0f : 1f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();
    }

    private void IntializeChildUI()
    {
        if (healthBar != null)
        {
            healthBar.SetTarget(currentCharacter);
        }
        if(chaosPointSlotUI != null)
        {
            chaosPointSlotUI.InitializeTarget(currentCharacter);
        }
        if(shieldSlider != null)
        {
            shieldSlider.SetTarget(currentCharacter);
        }
    }

}

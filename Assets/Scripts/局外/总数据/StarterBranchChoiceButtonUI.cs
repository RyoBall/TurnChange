using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class StarterBranchChoiceButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    private string m_branchId;
    private Action<string> m_onSelected;

    public void Bind(StarterBranchDefinition branch, string description, Action<string> onSelected)
    {
        if (branch == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        m_branchId = branch.branchId;
        m_onSelected = onSelected;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(branch.displayName) ? branch.branchId : branch.displayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = branch.accentColor;
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);

            ColorBlock colors = button.colors;
            colors.normalColor = branch.accentColor;
            colors.highlightedColor = branch.accentColor * 1.08f;
            colors.pressedColor = branch.accentColor * 0.92f;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            button.colors = colors;
        }
    }

    private void HandleClick()
    {
        m_onSelected?.Invoke(m_branchId);
    }
}
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterSelectButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image selectionHighlight;

    private CharacterRosterData m_data;
    private Action<CharacterRosterData> m_onClick;

    public RectTransform RectTransform => transform as RectTransform;
    public CharacterRosterData BoundData => m_data;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (nameText == null)
        {
            nameText = GetComponentInChildren<TMP_Text>(true);
        }

        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>(true);
        }
    }

    public void Bind(CharacterRosterData data, Action<CharacterRosterData> onClick, bool selected)
    {
        m_data = data;
        m_onClick = onClick;

        if (nameText != null)
        {
            nameText.text = data != null ? data.GetDisplayName() : string.Empty;
        }

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.GetPortraitSprite() : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = selected;
        }
    }

    private void HandleClick()
    {
        m_onClick?.Invoke(m_data);
    }
}
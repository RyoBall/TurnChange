using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 鼠标悬停时更换 Image 的 Sprite 与长宽尺寸，退出时恢复。
/// </summary>
[DisallowMultipleComponent]
public class HoverImageSwapper : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Vector2 hoverSize = new Vector2(100f, 100f);

    private Sprite m_originalSprite;
    private Vector2 m_originalSize;

    private void Awake()
    {
        CacheComponents();
        CacheOriginalValues();
    }

    private void CacheComponents()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    private void CacheOriginalValues()
    {
        if (targetImage != null)
        {
            m_originalSprite = targetImage.sprite;
            m_originalSize = targetImage.rectTransform.sizeDelta;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ApplyHoverAppearance();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreOriginalAppearance();
    }

    private void ApplyHoverAppearance()
    {
        if (targetImage == null) return;

        targetImage.sprite = hoverSprite;
        targetImage.rectTransform.sizeDelta = hoverSize;
    }

    private void RestoreOriginalAppearance()
    {
        if (targetImage == null) return;

        targetImage.sprite = m_originalSprite;
        targetImage.rectTransform.sizeDelta = m_originalSize;
    }
}

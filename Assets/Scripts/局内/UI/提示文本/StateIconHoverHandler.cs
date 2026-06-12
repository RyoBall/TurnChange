using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// 附加到状态图标上，鼠标悬停时显示 State 描述，移开时隐藏
/// </summary>
[RequireComponent(typeof(Image))]
public class StateIconHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private State m_state;

    public void Initialize(State state)
    {
        m_state = state;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_state == null)
        {
            return;
        }

        SkillDescription.Instance?.ChangeDescription(m_state);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SkillDescription.Instance?.HideDescription();
    }
}

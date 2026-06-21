using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public interface ICharacterSwitchPopupView
{
    bool IsPlaying { get; }
    float PlaybackSpeed { get; set; }
    IEnumerator PlayPopupAnimation(CharacterType characterType);
}

/// <summary>
/// Fight 场景 Canvas 下切人弹窗动画：按 CharacterType 播放对应 Animator 状态。
/// </summary>
public class CharacterSwitchPopupView : MonoBehaviour, ICharacterSwitchPopupView
{
    [SerializeField] private Image m_popupImage;
    [SerializeField] private Animator m_animator;
    [SerializeField] [Range(0.1f, 5f)] private float m_playbackSpeed = 1f;

    private bool m_isPlaying;

    public bool IsPlaying => m_isPlaying;

    /// <summary>
    /// 动画播放速度倍率，1 为原始速度。可在 Inspector 中调整，修改后下次播放生效。
    /// </summary>
    public float PlaybackSpeed
    {
        get => m_playbackSpeed;
        set => m_playbackSpeed = Mathf.Max(0.01f, value);
    }

    private void Awake()
    {
        HideImmediate();
    }

    public IEnumerator PlayPopupAnimation(CharacterType characterType)
    {
        if (m_animator == null || m_popupImage == null)
        {
            yield break;
        }

        m_isPlaying = true;
        gameObject.SetActive(true);
        m_popupImage.enabled = true;

        GameAudioEvents.Raise(GameAudioEventType.CharacterSwitch);

        string stateName = characterType.ToString();
        m_animator.speed = m_playbackSpeed;
        m_animator.Play(stateName, 0, 0f);

        yield return WaitForStateComplete(stateName);

        HideImmediate();
        m_isPlaying = false;
    }

    private IEnumerator WaitForStateComplete(string stateName)
    {
        // 等待一帧确保 Animator 已切换到目标状态
        yield return null;

        const int maxWaitFrames = 720;
        for (int i = 0; i < maxWaitFrames; i++)
        {
            AnimatorStateInfo stateInfo = m_animator.GetCurrentAnimatorStateInfo(0);

            // 动画播放完毕（normalizedTime >= 1f 且未处于过渡中）
            if (stateInfo.IsName(stateName) && stateInfo.normalizedTime >= 1f && !m_animator.IsInTransition(0))
            {
                yield break;
            }

            // 兜底：如果已经切回 Empty 状态，说明动画已结束
            if (stateInfo.IsName("Empty"))
            {
                yield break;
            }

            yield return null;
        }
    }

    private void HideImmediate()
    {
        if (m_popupImage != null)
        {
            m_popupImage.enabled = false;
            m_popupImage.sprite = null;
        }

        if (m_animator != null)
        {
            m_animator.speed = 1f;
        }
    }
}

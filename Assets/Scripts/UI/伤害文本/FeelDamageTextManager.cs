using System.Reflection;
using UnityEngine;
using MoreMountains.Feedbacks;

public class FeelDamageTextManager : MonoBehaviour
{
    public static FeelDamageTextManager Instance { get; private set; }

    private MMF_Player _feedbackPlayer;
    private MMF_FloatingText _floatingTextFeedback;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheFeedbackReferences();
    }

    void Start()
    {
        CacheFeedbackReferences();
    }

    public static void ShowText(string customMessage, Vector3 position, Color color)
    {
        if (Instance == null)
        {
            Debug.LogWarning("FeelDamageTextManager 未初始化，无法播放漂浮文本。");
            return;
        }

        Instance.ShowCustomText(customMessage, position, color);
    }

    public void ShowCustomText(string customMessage, Vector3 position)
    {
        ShowCustomText(customMessage, position, Color.white);
    }

    public void ShowCustomText(string customMessage, Vector3 position, Color color)
    {
        if (!CacheFeedbackReferences())
        {
            Debug.LogWarning("FeelDamageTextManager 无法播放漂浮文本，因为缺少必要的反馈组件。");
            return;
        }
        TrySetFloatingTextColor(color);
        _floatingTextFeedback.Value = customMessage;
        _feedbackPlayer.PlayFeedbacks(position);
    }
    private void TrySetFloatingTextColor(Color color)
    {
        if (_floatingTextFeedback == null) return;

        // 创建单色渐变
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(color, 0f),
            new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(0f, 1f)
            }
        );

        _floatingTextFeedback.ForceColor = true;
        _floatingTextFeedback.AnimateColorGradient = gradient;
    }
    private bool CacheFeedbackReferences()
    {
        if (_feedbackPlayer == null)
        {
            _feedbackPlayer = GetComponent<MMF_Player>();
        }

        if (_feedbackPlayer == null)
        {
            Debug.LogWarning("FeelDamageTextManager 缺少 MMF_Player 组件。");
            return false;
        }

        if (_floatingTextFeedback == null)
        {
            _floatingTextFeedback = _feedbackPlayer.GetFeedbackOfType<MMF_FloatingText>();
        }

        if (_floatingTextFeedback == null)
        {
            Debug.LogWarning("MMF_Player 上未找到 MMF_FloatingText 反馈。", this);
            return false;
        }

        return true;
    }
}

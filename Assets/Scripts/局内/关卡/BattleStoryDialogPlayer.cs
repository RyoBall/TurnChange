using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗剧情对话播放器 — 管理战前/战后多段对话的逐段展示
/// 挂载到 Fight 场景的 Canvas 下
/// </summary>
public class BattleStoryDialogPlayer : MonoBehaviour
{
    public static BattleStoryDialogPlayer Instance { get; private set; }

    [Header("对话显示参数")]
    [Tooltip("每段对话的显示时长（秒）")]
    [SerializeField] private float m_dialogDisplayDuration = 3f;
    [Tooltip("两段对话之间的间隔（秒）")]
    [SerializeField] private float m_dialogInterval = 0.5f;
    [Tooltip("对话淡入时长（秒）")]
    [SerializeField] private float m_fadeInDuration = 0.4f;
    [Tooltip("对话淡出时长（秒）")]
    [SerializeField] private float m_fadeOutDuration = 0.3f;

    [Header("对话框外观")]
    [Tooltip("对话框在屏幕中的Y位置比例（0-1）")]
    [SerializeField, Range(0f, 1f)] private float m_dialogYRatio = 0.25f;

    private bool m_isPlaying;
    public bool IsPlaying => m_isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 播放一系列剧情对话，逐段显示
    /// </summary>
    /// <param name="dialogs">对话列表</param>
    public IEnumerator PlayDialogs(IReadOnlyList<BattleStoryDialogData> dialogs)
    {
        if (dialogs == null || dialogs.Count == 0)
        {
            yield break;
        }

        if (m_isPlaying)
        {
            Debug.LogWarning("[BattleStoryDialogPlayer] 已有对话正在播放，跳过新请求");
            yield break;
        }

        m_isPlaying = true;

        for (int i = 0; i < dialogs.Count; i++)
        {
            BattleStoryDialogData dialog = dialogs[i];
            if (dialog == null || string.IsNullOrEmpty(dialog.content))
            {
                continue;
            }

            string dialogId = $"story_{i}";
            string displayText = BuildDisplayText(dialog);

            // 显示当前对话
            ShowDialog(dialogId, displayText);

            // 等待显示时长
            yield return new WaitForSeconds(m_dialogDisplayDuration);

            // 关闭当前对话
            HideDialog(dialogId);

            // 段间间隔
            if (i < dialogs.Count - 1)
            {
                yield return new WaitForSeconds(m_dialogInterval);
            }
        }

        // 等待最后一段对话完全淡出
        yield return new WaitForSeconds(m_fadeOutDuration);
        m_isPlaying = false;
    }

    /// <summary>
    /// 跳过当前正在播放的对话序列
    /// </summary>
    public void SkipCurrentDialogs()
    {
        if (!m_isPlaying) return;

        StopAllCoroutines();
        ClearAllDialogs();
        m_isPlaying = false;
    }

    private string BuildDisplayText(BattleStoryDialogData dialog)
    {
        if (string.IsNullOrEmpty(dialog.speakerName))
        {
            return dialog.content;
        }

        return $"【{dialog.speakerName}】\n{dialog.content}";
    }

    private void ShowDialog(string dialogId, string message)
    {
        if (FloatingTipGenerator.Instance == null) return;

        // 使用 FloatingTipGenerator 的持续对话框功能，不自动消失
        FloatingTipGenerator.Instance.StartPersistentDialog(dialogId, message, 0f);
    }

    private void HideDialog(string dialogId)
    {
        if (FloatingTipGenerator.Instance == null) return;

        FloatingTipGenerator.Instance.StopPersistentDialog(dialogId);
    }

    private void ClearAllDialogs()
    {
        if (FloatingTipGenerator.Instance == null) return;

        FloatingTipGenerator.Instance.ClearAllPersistentDialogs();
    }
}

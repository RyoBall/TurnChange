using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡剧情对话数据 — 定义在关卡 ScriptableObject 中
/// </summary>
[Serializable]
public class BattleStoryDialogData
{
    [Tooltip("说话角色名（可为空，表示旁白）")]
    public string speakerName;

    [Tooltip("对话内容")]
    [TextArea(2, 5)]
    public string content;
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教程类型枚举，决定使用哪个行为子类
/// </summary>
public enum TutorialType
{
    教程一,     // 角色页面引导教程（游戏开始触发）
    教程二,     // 角色页面详情教程（进入角色页面触发）
    教程三,     // 战斗引导教程（关闭角色页面触发）
    教程四,     // 备战页面教程（进入关卡页面触发）
    教程五,     // 角色选择教程（备战教程结束后触发）
    教程六,     // 战斗界面教程（进入战斗后触发）
    教程七,     // 技能演示教程（战斗界面教程结束后触发）
    教程八,     // 商店引导教程（战斗结束回到主场景后触发）
    教程九,     // 商店详情教程（进入商店后触发）
    教程十,     // 序体引导教程（离开商店后触发）
    教程十一,   // 序体搭载教程（进入序体页面后触发）
    教程十二,   // 第二关引导教程（离开序体页面后触发）
    教程十三,   // 第二关提示教程（教程十二完成后触发）
    教程十四,   // 强敌提示教程（敌人行动且教程十三完成后触发）
    教程十五,   // 援军到达教程（教程十四完成后触发）
    教程十六,   // 新角色引导教程（战斗结束回主界面后触发）
    教程十七,   // 最终测验教程（教程十六结束后触发）
}

/// <summary>
/// 教程数据 ScriptableObject，在 Inspector 中配置教程内容和类型
/// </summary>
[CreateAssetMenu(fileName = "TutorialData", menuName = "教程/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [Header("教程类型")]
    [SerializeField] private TutorialType m_type;

    [Header("对话文本列表")]
    [SerializeField] private List<string> m_textList = new List<string>();

    /// <summary>教程类型，决定使用哪个行为子类</summary>
    public TutorialType Type => m_type;

    /// <summary>该教程的对话文本列表</summary>
    public IReadOnlyList<string> TextList => m_textList;
}

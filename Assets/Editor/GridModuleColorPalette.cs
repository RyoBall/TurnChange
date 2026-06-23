using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 序体名称 → 渐变起止色映射。按名称意象分配，供导入器与批量上色工具共用。
/// </summary>
public static class GridModuleColorPalette
{
    private struct ModuleColorPair
    {
        public Color Start;
        public Color End;

        public ModuleColorPair(Color start, Color end)
        {
            Start = start;
            End = end;
        }
    }

    private static readonly Dictionary<string, ModuleColorPair> s_colorsByName = new Dictionary<string, ModuleColorPair>
    {
        { "有备而来", Pair(0.92f, 0.72f, 0.28f, 0.18f, 0.32f, 0.58f) },
        { "先发制人", Pair(1.00f, 0.94f, 0.45f, 0.22f, 0.58f, 1.00f) },
        { "额外指挥", Pair(0.58f, 0.38f, 0.92f, 0.28f, 0.12f, 0.58f) },
        { "增伤切入", Pair(0.95f, 0.32f, 0.28f, 0.52f, 0.08f, 0.12f) },
        { "迅速切入", Pair(0.32f, 0.88f, 0.78f, 0.12f, 0.48f, 0.58f) },
        { "呼吸回血", Pair(0.52f, 0.92f, 0.68f, 0.18f, 0.58f, 0.38f) },
        { "治愈增强", Pair(0.82f, 1.00f, 0.82f, 0.22f, 0.72f, 0.48f) },
        { "混沌疗化", Pair(0.72f, 0.52f, 0.95f, 0.42f, 0.22f, 0.68f) },
        { "法术强化", Pair(0.48f, 0.22f, 0.88f, 0.18f, 0.62f, 0.38f) },
        { "Dot强化", Pair(0.55f, 0.25f, 0.82f, 0.12f, 0.72f, 0.35f) },
        { "直伤强化", Pair(0.72f, 0.75f, 0.80f, 0.58f, 0.38f, 0.22f) },
        { "紧急回避", Pair(1.00f, 0.82f, 0.22f, 0.32f, 0.30f, 0.28f) },
        { "起死回生", Pair(1.00f, 0.90f, 0.48f, 0.92f, 0.96f, 1.00f) },
        { "生命强化", Pair(0.88f, 0.38f, 0.42f, 0.48f, 0.12f, 0.18f) },
        { "防御强化", Pair(0.42f, 0.58f, 0.82f, 0.28f, 0.32f, 0.42f) },
        { "暴伤强化", Pair(1.00f, 0.48f, 0.18f, 0.78f, 0.12f, 0.10f) },
        { "暴率强化", Pair(1.00f, 0.82f, 0.25f, 0.88f, 0.52f, 0.08f) },
        { "沉重毒素", Pair(0.38f, 0.18f, 0.58f, 0.22f, 0.68f, 0.28f) },
        { "重型炮台", Pair(0.48f, 0.52f, 0.38f, 0.32f, 0.34f, 0.28f) },
        { "赌徒步伐", Pair(0.18f, 0.68f, 0.38f, 0.88f, 0.18f, 0.22f) },
        { "燃血逆转", Pair(0.82f, 0.18f, 0.22f, 0.28f, 0.04f, 0.08f) },
        { "域场共鸣", Pair(0.58f, 0.38f, 0.95f, 0.12f, 0.18f, 0.58f) },
        { "蓄势逆击·共鸣", Pair(0.28f, 0.68f, 1.00f, 0.42f, 0.18f, 0.78f) },
        { "物法双修", Pair(0.32f, 0.72f, 1.00f, 0.72f, 0.28f, 0.85f) },
        { "辅切瞬发", Pair(0.55f, 0.82f, 1.00f, 0.62f, 0.48f, 0.92f) },
        { "混沌豁免", Pair(0.62f, 0.52f, 0.72f, 0.22f, 0.18f, 0.38f) },
        { "切人蓄爆", Pair(1.00f, 0.88f, 0.38f, 0.22f, 0.52f, 1.00f) },
        { "紧急切入", Pair(1.00f, 0.52f, 0.22f, 0.68f, 0.18f, 0.12f) },
        { "暴噬蔓延", Pair(0.38f, 0.78f, 0.32f, 0.28f, 0.18f, 0.48f) },
    };

    public static bool TryGetColors(string moduleName, out Color startColor, out Color endColor)
    {
        if (!string.IsNullOrWhiteSpace(moduleName) && s_colorsByName.TryGetValue(moduleName, out ModuleColorPair pair))
        {
            startColor = pair.Start;
            endColor = pair.End;
            return true;
        }

        startColor = new Color(0.28f, 0.78f, 1f, 0.9f);
        endColor = new Color(0.1f, 0.3f, 0.85f, 0.9f);
        return false;
    }

    public static void ApplyColors(GridModuleDefinition moduleAsset)
    {
        if (moduleAsset == null)
        {
            return;
        }

        if (!TryGetColors(moduleAsset.moduleName, out Color startColor, out Color endColor))
        {
            return;
        }

        moduleAsset.color = startColor;
        moduleAsset.gradientColorB = endColor;
    }

    public static IReadOnlyCollection<string> GetConfiguredModuleNames()
    {
        return s_colorsByName.Keys;
    }

    private static ModuleColorPair Pair(
        float startR, float startG, float startB,
        float endR, float endG, float endB)
    {
        return new ModuleColorPair(
            new Color(startR, startG, startB, 0.9f),
            new Color(endR, endG, endB, 0.9f));
    }
}

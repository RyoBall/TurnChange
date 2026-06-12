using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class LevelEventOptionData
{
    [TextArea(2, 4)] public string optionDescription;//选项描述
    public LevelEventOptionType optionType;
    [Min(0)] public int battleCount = 1;//持续战斗场次N
    [Min(0)] public int value = 1;//主数值，如X
    [Min(0)] public int extraValue;//额外数值，如M
    public bool HasContent => optionType != LevelEventOptionType.None || !string.IsNullOrWhiteSpace(optionDescription);
}

[CreateAssetMenu(fileName = "LevelEventData", menuName = "事件/LevelEventData", order = 1)]
public class LevelEventData:ScriptableObject
{
    public string eventName;
    [TextArea(3, 6)] public string eventDescription;
    public List<LevelEventOptionData> options = new List<LevelEventOptionData>(3);

    public IReadOnlyList<LevelEventOptionData> GetOptions()
    {
        return options != null ? options : Array.Empty<LevelEventOptionData>();
    }
}

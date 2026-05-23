using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandSkillManager : MonoBehaviour
{
    public static CommandSkillManager Instance { get; private set; }
    public List<SkillBase> commandSkills;
    public List<CommandButton> commandButtons;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        //初始时将技能绑定到按钮上，后续可以根据需要重新绑定
        for (int i = 0; i < commandButtons.Count && i < commandSkills.Count; i++)
        {
            var button = commandButtons[i];
            var skill = commandSkills[i];
            button.BindSkill(null, skill);
        }
    }
}

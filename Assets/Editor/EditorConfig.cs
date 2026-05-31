
using UnityEngine;
// 创建资产文件，在Project窗口中双击编辑
[CreateAssetMenu(fileName = "AppConfig", menuName = "Config/AppConfig")]
public class AppConfig : ScriptableObject
{
    [Header("数据路径配置")]
    public string CharacterDataCSVPath = "Assets/Data/CharacterData.csv";
    public string EnemyDataCSVPath = "Assets/Data/EnemyData.csv";
    public string CharacterSkillCSVPath = "Assets/Data/CharacterSkillData.csv";
    public string GridModuleCSVPath = "Assets/Data/GridModuleData.csv";
    public string StateDataCSVPath = "Assets/Data/StateData.csv";
    public string CharacterSkillAssetOutputPath = "Assets/Resources/配置可编程物体/角色技能";
    public string GridModuleAssetOutputPath = "Assets/Resources/配置可编程物体/模块";
    public string StateAssetOutputPath = "Assets/Resources/配置可编程物体/状态";
}

// 全局访问
public static class Config
{
    private static AppConfig _config;
    public static AppConfig Instance
    {
        get
        {
            if (_config == null)
            {
                _config = Resources.Load<AppConfig>("AppConfig");
            }
            return _config;
        }
    }
}
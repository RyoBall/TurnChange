
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
    public string TutorialDataCSVPath = "Assets/Data/TutorialData.csv";
    public string KeyWordConfigCSVPath = "Assets/Data/KeyWordConfig.csv";
    public string TagColorCSVPath = "Assets/Data/TagColor.csv";
    public string BattleLevelDataCSVPath = "Assets/Data/BattleLevelData.csv";
    public string CharacterSkillAssetOutputPath = "Assets/Resources/配置可编程物体/角色技能";
    public string LevelSelectionDataOutputPath = "Assets/Resources/配置可编程物体/关卡数据";
    public string GridModuleAssetOutputPath = "Assets/Resources/配置可编程物体/模块";
    public string StateAssetOutputPath = "Assets/Resources/配置可编程物体/状态";
    public string TutorialAssetOutputPath = "Assets/Resources/配置可编程物体/教程";
    public string KeyWordConfigAssetOutputPath = "Assets/Resources/配置可编程物体/技能/关键词配置";
    public string TagColorAssetOutputPath = "Assets/Resources/配置可编程物体/技能/关键词配置";
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
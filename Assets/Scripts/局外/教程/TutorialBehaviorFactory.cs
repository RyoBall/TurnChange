/// <summary>
/// 教程行为工厂，根据 TutorialType 枚举创建对应的行为子类实例
/// </summary>
public static class TutorialBehaviorFactory
{
    /// <summary>
    /// 根据教程类型创建对应的行为实例
    /// </summary>
    /// <param name="type">教程类型枚举</param>
    /// <returns>对应的 TutorialBehavior 子类实例</returns>
    public static TutorialBehavior Create(TutorialType type)
    {
        switch (type)
        {
            case TutorialType.教程一:
                return new CharacterPanelTutorial();
            case TutorialType.教程二:
                return new CharacterPanelDetailTutorial();
            case TutorialType.教程三:
                return new BattleIntroTutorial();
            case TutorialType.教程四:
                return new BattlePreparationTutorial();
            case TutorialType.教程五:
                return new CharacterSelectionTutorial();
            case TutorialType.教程六:
                return new BattleUITutorial();
            case TutorialType.教程七:
                return new SkillDemoTutorial();
            case TutorialType.教程八:
                return new ShopIntroTutorial();
            case TutorialType.教程九:
                return new ShopDetailTutorial();
            case TutorialType.教程十:
                return new BackpackIntroTutorial();
            case TutorialType.教程十一:
                return new BackpackPlacementTutorial();
            case TutorialType.教程十二:
                return new SecondLevelIntroTutorial();
            case TutorialType.教程十三:
                return new SecondLevelTipTutorial();
            case TutorialType.教程十四:
                return new EnemyStrongTutorial();
            case TutorialType.教程十五:
                return new ReinforcementArriveTutorial();
            case TutorialType.教程十六:
                return new NewCharacterIntroTutorial();
            case TutorialType.教程十七:
                return new FinalTestTutorial();
            default:
                return null;
        }
    }
}

using UnityEngine;

/// <summary>
/// 教程行为抽象基类，子类通过覆盖 CanProgress 实现不同的推进条件
/// </summary>
public abstract class TutorialBehavior
{
    /// <summary>教程结束时的静态委托字段（可由子类在 OnTutorialEnd 中触发）</summary>
    public static System.Action<TutorialType> TutorialEnded;

    protected TutorialData m_data;
    protected int m_currentIndex = 0;
    protected TutorialController m_controller;

    /// <summary>关联的教程数据</summary>
    public TutorialData Data => m_data;

    /// <summary>当前文本索引</summary>
    public int CurrentIndex => m_currentIndex;

    /// <summary>是否已读完所有文本</summary>
    public bool IsCompleted => m_currentIndex >= m_data.TextList.Count;

    /// <summary>
    /// 初始化行为实例
    /// </summary>
    public virtual void Initialize(TutorialData data, TutorialController controller)
    {
        m_data = data;
        m_controller = controller;
        m_currentIndex = 0;
    }

    /// <summary>
    /// 检测是否可以推进到下一条文本
    /// 默认实现：检测鼠标左键按下
    /// 子类可覆盖以针对特定索引实现不同的推进条件
    /// </summary>
    public virtual bool CanProgress()
    {
        return Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 推进教程：显示下一条文本，文本全部显示完毕后结束教程
    /// 推进后会调用 OnProgress() 以执行当前索引对应的系统处理
    /// </summary>
    public virtual void Progress()
    {
        if (m_currentIndex < m_data.TextList.Count)
        {
            m_controller.UpdateDialogText(m_data.TextList[m_currentIndex]);
            m_currentIndex++;
            OnProgress(m_currentIndex - 1);
        }
        else
        {
            m_controller.EndTutorial();
        }
    }

    /// <summary>
    /// 推进文本后的回调，子类可覆写以执行当前索引对应的系统处理（如高亮聚焦）
    /// </summary>
    /// <param name="index">刚推进完成的文本索引</param>
    public virtual void OnProgress(int index) { }

    /// <summary>
    /// 教程开始时的回调，子类可覆盖以监听特殊事件
    /// </summary>
    public virtual void OnTutorialStart() { }

    /// <summary>
    /// 教程结束时的回调，子类可覆盖以清理资源
    /// </summary>
    public virtual void OnTutorialEnd() { }

    /// <summary>
    /// 在控制器 Start 中调用，子类可覆写以注册监听事件，
    /// 事件发生后调用 m_controller.StartTutorial(m_data.Type) 启动教程
    /// </summary>
    public virtual void StartListening() { }

    /// <summary>
    /// 在控制器 OnDestroy 中调用，子类可覆写以取消事件监听
    /// </summary>
    public virtual void StopListening() { }
}

/// <summary>
/// 等待点击推进的教程行为
/// 特殊索引处可等待外部事件而非鼠标点击
/// </summary>
public class WaitClickTutorial : TutorialBehavior
{
    private bool m_specialFlag = false;

    /// <summary>
    /// 检查是否可以推进
    /// 索引 2 处等待外部事件设置 specialFlag，其余索引等待鼠标点击
    /// </summary>
    public override bool CanProgress()
    {
        if (m_currentIndex == 2)
            return m_specialFlag;
        else
            return Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 由外部事件调用，设置特殊标志以允许教程推进
    /// </summary>
    public void OnSpecialEvent()
    {
        m_specialFlag = true;
    }

    public override void OnTutorialEnd()
    {
        m_specialFlag = false;
    }
}

/// <summary>
/// 角色页面引导教程行为
/// 文本1结束后触发聚焦高亮角色按钮，文本2等待玩家进入角色页面后推进
/// StartListening 直接启动教程（游戏开始时触发），文本2通过静态事件监听角色页面打开
/// </summary>
public class CharacterPanelTutorial : TutorialBehavior
{
    /// <summary>是否已进入角色页面</summary>
    private bool m_enteredCharacterPanel = false;

    /// <summary>
    /// 控制器 Start 时调用：直接启动本教程（游戏开始时立即触发）
    /// </summary>
    public override void StartListening()
    {
        m_controller.StartTutorial(m_data.Type);
    }

    /// <summary>
    /// 检查是否可以推进
    /// 序号 2（文本2）等待进入角色页面事件，其余序号等待鼠标点击
    /// </summary>
    public override bool CanProgress()
    {
        if (m_currentIndex == 2)
            return m_enteredCharacterPanel;
        else
            return Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 文本推进后的系统处理
    /// 索引0 → 高亮角色栏按钮；索引1 → 取消高亮
    /// </summary>
    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0:
                m_controller.ShowGuideHighlight(GuideHighlightType.角色栏);
                break;
            case 1:
                m_controller.HideGuideHighlight();
                break;
        }
    }

    /// <summary>
    /// 教程开始时注册页面切换监听（用于文本2推进）
    /// </summary>
    public override void OnTutorialStart()
    {
        ChangePanelButton.PanelSwitched += OnPanelSwitched;
    }

    /// <summary>
    /// 页面切换事件回调，检测到角色页面打开时设置标志
    /// </summary>
    private void OnPanelSwitched(PanelType panelType)
    {
        if (panelType == PanelType.角色页面)
        {
            m_enteredCharacterPanel = true;
        }
    }

    public override void OnTutorialEnd()
    {
        m_enteredCharacterPanel = false;
        ChangePanelButton.PanelSwitched -= OnPanelSwitched;
    }
}

/// <summary>
/// 角色页面详情教程行为
/// 进入角色页面时触发，4条纯点击推进的文本，无特殊事件
/// </summary>
public class CharacterPanelDetailTutorial : TutorialBehavior
{
    /// <summary>
    /// 监听角色页面打开事件，触发后启动本教程
    /// </summary>
    public override void StartListening()
    {
        ChangePanelButton.PanelSwitched += OnPanelSwitched;
    }

    public override void StopListening()
    {
        ChangePanelButton.PanelSwitched -= OnPanelSwitched;
    }

    private void OnPanelSwitched(PanelType panelType)
    {
        if (panelType == PanelType.角色页面)
        {
            m_controller.StartTutorial(m_data.Type);
        }
    }
}

/// <summary>
/// 战斗引导教程行为
/// 关闭角色页面时触发，高亮开始战斗按钮，文本2等待进入战斗事件
/// </summary>
public class BattleIntroTutorial : TutorialBehavior
{
    private bool m_battleStarted = false;

    /// <summary>
    /// 监听角色页面关闭事件，触发后启动本教程
    /// </summary>
    public override void StartListening()
    {
        CharacterPanelView.PanelClosed += OnCharacterPanelClosed;
    }

    public override void StopListening()
    {
        CharacterPanelView.PanelClosed -= OnCharacterPanelClosed;
    }

    private void OnCharacterPanelClosed()
    {
        m_controller.StartTutorial(m_data.Type);
    }

    /// <summary>
    /// 检查是否可以推进
    /// 序号 2（文本2）等待开始战斗事件，其余序号等待鼠标点击
    /// </summary>
    public override bool CanProgress()
    {
        if (m_currentIndex == 2)
            return m_battleStarted;
        else
            return Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 文本推进后的系统处理
    /// 索引0 → 高亮开始战斗按钮；索引1 → 取消高亮
    /// </summary>
    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0:
                m_controller.ShowGuideHighlight(GuideHighlightType.开始战斗按钮);
                break;
            case 1:
                m_controller.HideGuideHighlight();
                break;
        }
    }

    /// <summary>
    /// 教程开始时注册开始战斗监听
    /// </summary>
    public override void OnTutorialStart()
    {
        LevelSelectionItemUI.BattleLevelSelected += OnBattleStarted;
    }

    private void OnBattleStarted()
    {
        m_battleStarted = true;
    }

    public override void OnTutorialEnd()
    {
        m_battleStarted = false;
        LevelSelectionItemUI.BattleLevelSelected -= OnBattleStarted;
    }
}

/// <summary>
/// 备战页面教程行为
/// 进入关卡页面时触发，高亮关卡信息区域，3条纯点击推进的文本
/// </summary>
public class BattlePreparationTutorial : TutorialBehavior
{
    /// <summary>
    /// 监听关卡页面打开事件，触发后启动本教程
    /// </summary>
    public override void StartListening()
    {
        LevelSelectionItemUI.BattleLevelSelected += OnBattleLevelSelected;
    }

    public override void StopListening()
    {
        LevelSelectionItemUI.BattleLevelSelected -= OnBattleLevelSelected;
    }

    private void OnBattleLevelSelected()
    {
        m_controller.StartTutorial(m_data.Type);
    }

    /// <summary>
    /// 文本推进后的系统处理
    /// 索引0 → 高亮关卡信息；索引2 → 取消高亮
    /// </summary>
    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0:
                m_controller.ShowGuideHighlight(GuideHighlightType.关卡信息);
                break;
            case 2:
                m_controller.HideGuideHighlight();
                break;
        }
    }

    /// <summary>
    /// 教程结束时触发静态事件，通知后续教程可以启动
    /// </summary>
    public override void OnTutorialEnd()
    {
        TutorialEnded?.Invoke(m_data.Type);
    }
}

/// <summary>
/// 角色选择教程行为
/// 备战教程结束后触发，高亮角色头像区域，文本3等待点击角色头像推进
/// </summary>
public class CharacterSelectionTutorial : TutorialBehavior
{
    private bool m_characterClicked = false;

    /// <summary>
    /// 监听备战教程结束事件，触发后启动本教程
    /// </summary>
    public override void StartListening()
    {
        BattlePreparationTutorial.TutorialEnded += OnBattlePreparationEnded;
    }

    public override void StopListening()
    {
        BattlePreparationTutorial.TutorialEnded -= OnBattlePreparationEnded;
    }

    private void OnBattlePreparationEnded(TutorialType type)
    {
        if (type == TutorialType.教程四)
        {
            m_controller.StartTutorial(m_data.Type);
        }
    }

    /// <summary>
    /// 检查是否可以推进
    /// 序号 3（文本3）等待点击角色头像，其余序号等待鼠标点击
    /// </summary>
    public override bool CanProgress()
    {
        if (m_currentIndex == 3)
            return m_characterClicked;
        else
            return Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 文本推进后的系统处理
    /// 索引0 → 高亮角色选择区域；索引2 → 取消高亮
    /// </summary>
    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0:
                m_controller.ShowGuideHighlight(GuideHighlightType.角色头像选择);
                break;
            case 2:
                m_controller.HideGuideHighlight();
                break;
        }
    }

    /// <summary>
    /// 教程开始时注册角色头像点击监听
    /// </summary>
    public override void OnTutorialStart()
    {
        CharacterSelectButtonUI.CharacterClicked += OnCharacterClicked;
    }

    private void OnCharacterClicked()
    {
        m_characterClicked = true;
    }

    public override void OnTutorialEnd()
    {
        m_characterClicked = false;
        CharacterSelectButtonUI.CharacterClicked -= OnCharacterClicked;
    }
}

/// <summary>
/// 战斗界面教程行为
/// 进入战斗后触发，高亮角色状态栏，文本6后切换高亮到行动序列，结束时隐藏聚焦
/// </summary>
public class BattleUITutorial : TutorialBehavior
{
    /// <summary>
    /// 监听 TurnManager 战斗开始事件，触发后启动本教程
    /// </summary>
    public override void StartListening()
    {
        TurnManager.BattleStarted += OnBattleStarted;
    }

    public override void StopListening()
    {
        TurnManager.BattleStarted -= OnBattleStarted;
    }

    private void OnBattleStarted()
    {
        m_controller.StartTutorial(m_data.Type);
    }

    /// <summary>
    /// 文本推进后的系统处理
    /// 索引0 → 高亮角色状态栏；索引5 → 切换高亮到行动序列；索引9 → 取消高亮
    /// </summary>
    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0:
                m_controller.ShowGuideHighlight(GuideHighlightType.角色状态栏);
                break;
            case 5:
                m_controller.ShowGuideHighlight(GuideHighlightType.行动序列);
                break;
            case 9:
                m_controller.HideGuideHighlight();
                break;
        }
    }
}

/// <summary>
/// 技能演示教程行为
/// 战斗界面教程结束后触发，演示追惩和厄运播撒技能，文本5/6等待对应技能执行完毕
/// </summary>
public class SkillDemoTutorial : TutorialBehavior
{
    private bool m_pursuitPunishExecuted = false;
    private bool m_debuffSpreadExecuted = false;

    /// <summary>
    /// 监听战斗界面教程结束事件，触发后启动本教程
    /// </summary>
    public override void StartListening()
    {
        BattleUITutorial.TutorialEnded += OnBattleUIEnded;
    }

    public override void StopListening()
    {
        BattleUITutorial.TutorialEnded -= OnBattleUIEnded;
    }

    private void OnBattleUIEnded(TutorialType type)
    {
        if (type == TutorialType.教程六)
        {
            m_controller.StartTutorial(m_data.Type);
        }
    }

    /// <summary>
    /// 检查是否可以推进
    /// 序号 5（文本5）等待追惩技能执行完毕
    /// 序号 6（文本6）等待厄运播撒技能执行完毕
    /// 其余序号等待鼠标点击
    /// </summary>
    public override bool CanProgress()
    {
        if (m_currentIndex == 5)
            return m_pursuitPunishExecuted;
        if (m_currentIndex == 6)
            return m_debuffSpreadExecuted;
        return Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 文本推进后的系统处理
    /// 索引3 → 高亮追惩技能；索引5 → 切换高亮到敌人词条；索引7 → 取消高亮
    /// </summary>
    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 3:
                m_controller.ShowGuideHighlight(GuideHighlightType.追惩技能);
                break;
            case 5:
                m_controller.ShowGuideHighlight(GuideHighlightType.敌人词条);
                break;
            case 7:
                m_controller.HideGuideHighlight();
                break;
        }
    }

    /// <summary>
    /// 教程开始时注册技能执行监听
    /// </summary>
    public override void OnTutorialStart()
    {
        SkillExecuteManager.OnSkillExecuted += OnSkillExecuted;
    }

    private void OnSkillExecuted(UnitCombatant unit, SkillBase skill)
    {
        if (skill is CharacterSkillBase characterSkill)
        {
            if (characterSkill.skillType == CharacterSkillType.PursuitPunish ||
                characterSkill.skillType == CharacterSkillType.PursuitPunishAdditional)
            {
                m_pursuitPunishExecuted = true;
            }
            else if (characterSkill.skillType == CharacterSkillType.DebuffSpreadAttack)
            {
                m_debuffSpreadExecuted = true;
            }
        }
    }

    public override void OnTutorialEnd()
    {
        m_pursuitPunishExecuted = false;
        m_debuffSpreadExecuted = false;
        SkillExecuteManager.OnSkillExecuted -= OnSkillExecuted;
    }
}

/// <summary>
/// 商店引导教程行为（教程八）
/// 战斗结束回到主场景后触发，高亮商店按钮，文本2等待进入商店页面
/// </summary>
public class ShopIntroTutorial : TutorialBehavior
{
    private bool m_enteredShop = false;

    public override void StartListening()
    {
        SkillDemoTutorial.TutorialEnded += OnSkillDemoEnded;
    }

    public override void StopListening()
    {
        SkillDemoTutorial.TutorialEnded -= OnSkillDemoEnded;
    }

    private void OnSkillDemoEnded(TutorialType type)
    {
        if (type == TutorialType.教程七)
            m_controller.StartTutorial(m_data.Type);
    }

    public override bool CanProgress()
    {
        if (m_currentIndex == 2) return m_enteredShop;
        return Input.GetMouseButtonDown(0);
    }

    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0: m_controller.ShowGuideHighlight(GuideHighlightType.商店按钮); break;
            case 1: m_controller.HideGuideHighlight(); break;
        }
    }

    public override void OnTutorialStart()
    {
        ChangePanelButton.PanelSwitched += OnPanelSwitched;
    }

    private void OnPanelSwitched(PanelType panelType)
    {
        if (panelType == PanelType.商店页面) m_enteredShop = true;
    }

    public override void OnTutorialEnd()
    {
        m_enteredShop = false;
        ChangePanelButton.PanelSwitched -= OnPanelSwitched;
    }
}

/// <summary>
/// 商店详情教程行为（教程九）
/// 进入商店页面后触发，高亮商品/刷新/扩容，文本3等待购买序体
/// </summary>
public class ShopDetailTutorial : TutorialBehavior
{
    private bool m_itemPurchased = false;

    public override void StartListening()
    {
        ChangePanelButton.PanelSwitched += OnPanelSwitched;
    }

    public override void StopListening()
    {
        ChangePanelButton.PanelSwitched -= OnPanelSwitched;
    }

    private void OnPanelSwitched(PanelType panelType)
    {
        if (panelType == PanelType.商店页面)
            m_controller.StartTutorial(m_data.Type);
    }

    public override bool CanProgress()
    {
        if (m_currentIndex == 3) return m_itemPurchased;
        return Input.GetMouseButtonDown(0);
    }

    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0: m_controller.ShowGuideHighlight(GuideHighlightType.序体商品); break;
            case 2: m_controller.ShowGuideHighlight(GuideHighlightType.刷新按钮); break;
            case 3: m_controller.ShowGuideHighlight(GuideHighlightType.扩容按钮); break;
            case 4: m_controller.HideGuideHighlight(); break;
        }
    }

    public override void OnTutorialStart()
    {
        ShopModuleManager.ItemPurchasedStatic += OnItemPurchased;
    }

    private void OnItemPurchased(GridModuleDefinition module, int price, int slotIndex)
    {
        m_itemPurchased = true;
    }

    public override void OnTutorialEnd()
    {
        m_itemPurchased = false;
        ShopModuleManager.ItemPurchasedStatic -= OnItemPurchased;
    }
}

/// <summary>
/// 序体引导教程行为（教程十）
/// 离开商店页面后触发，高亮序体按钮，文本1等待进入序体页面
/// </summary>
public class BackpackIntroTutorial : TutorialBehavior
{
    private bool m_enteredBackpack = false;

    public override void StartListening()
    {
        CharacterPanelView.PanelClosed += OnShopClosed;
    }

    public override void StopListening()
    {
        CharacterPanelView.PanelClosed -= OnShopClosed;
    }

    private void OnShopClosed()
    {
        m_controller.StartTutorial(m_data.Type);
    }

    public override bool CanProgress()
    {
        if (m_currentIndex == 1) return m_enteredBackpack;
        return Input.GetMouseButtonDown(0);
    }

    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0: m_controller.ShowGuideHighlight(GuideHighlightType.序体按钮); break;
            case 1: m_controller.HideGuideHighlight(); break;
        }
    }

    public override void OnTutorialStart()
    {
        ChangePanelButton.PanelSwitched += OnPanelSwitched;
    }

    private void OnPanelSwitched(PanelType panelType)
    {
        if (panelType == PanelType.背包页面) m_enteredBackpack = true;
    }

    public override void OnTutorialEnd()
    {
        m_enteredBackpack = false;
        ChangePanelButton.PanelSwitched -= OnPanelSwitched;
    }
}

/// <summary>
/// 序体搭载教程行为（教程十一）
/// 进入序体页面后触发，文本2等待序体装载
/// </summary>
public class BackpackPlacementTutorial : TutorialBehavior
{
    private bool m_modulePlaced = false;

    public override void StartListening()
    {
        BackpackIntroTutorial.TutorialEnded += OnBackpackIntroEnded;
    }

    public override void StopListening()
    {
        BackpackIntroTutorial.TutorialEnded -= OnBackpackIntroEnded;
    }

    private void OnBackpackIntroEnded(TutorialType type)
    {
        if (type == TutorialType.教程十)
            m_controller.StartTutorial(m_data.Type);
    }

    public override bool CanProgress()
    {
        if (m_currentIndex == 2) return m_modulePlaced;
        return Input.GetMouseButtonDown(0);
    }

    public override void OnTutorialStart()
    {
        Datas.ModulePlacedStatic += OnModulePlaced;
    }

    private void OnModulePlaced()
    {
        m_modulePlaced = true;
    }

    public override void OnTutorialEnd()
    {
        m_modulePlaced = false;
        Datas.ModulePlacedStatic -= OnModulePlaced;
    }
}

/// <summary>
/// 第二关引导教程行为（教程十二）
/// 离开序体页面后触发，纯点击
/// </summary>
public class SecondLevelIntroTutorial : TutorialBehavior
{
    public override void StartListening()
    {
        CharacterPanelView.PanelClosed += OnBackpackClosed;
    }

    public override void StopListening()
    {
        CharacterPanelView.PanelClosed -= OnBackpackClosed;
    }

    private void OnBackpackClosed()
    {
        m_controller.StartTutorial(m_data.Type);
    }
}

/// <summary>
/// 第二关提示教程行为（教程十三）
/// 教程十二完成后触发，纯点击
/// </summary>
public class SecondLevelTipTutorial : TutorialBehavior
{
    public override void StartListening()
    {
        SecondLevelIntroTutorial.TutorialEnded += OnSecondLevelEnded;
    }

    public override void StopListening()
    {
        SecondLevelIntroTutorial.TutorialEnded -= OnSecondLevelEnded;
    }

    private void OnSecondLevelEnded(TutorialType type)
    {
        if (type == TutorialType.教程十二)
            m_controller.StartTutorial(m_data.Type);
    }
}

/// <summary>
/// 强敌提示教程行为（教程十四）
/// 敌人行动且教程十三完成后触发，纯点击
/// </summary>
public class EnemyStrongTutorial : TutorialBehavior
{
    private bool m_enemyActed = false;

    public override void StartListening()
    {
        SecondLevelTipTutorial.TutorialEnded += OnTipEnded;
    }

    public override void StopListening()
    {
        SecondLevelTipTutorial.TutorialEnded -= OnTipEnded;
    }

    private void OnTipEnded(TutorialType type)
    {
        if (type == TutorialType.教程十三)
            m_controller.StartTutorial(m_data.Type);
    }
}

/// <summary>
/// 援军到达教程行为（教程十五）
/// 教程十四完成后触发，高亮切人按键和指挥点，文本6等待换人回合结束
/// </summary>
public class ReinforcementArriveTutorial : TutorialBehavior
{
    private bool m_swapCompleted = false;

    public override void StartListening()
    {
        EnemyStrongTutorial.TutorialEnded += OnEnemyStrongEnded;
    }

    public override void StopListening()
    {
        EnemyStrongTutorial.TutorialEnded -= OnEnemyStrongEnded;
    }

    private void OnEnemyStrongEnded(TutorialType type)
    {
        if (type == TutorialType.教程十四)
            m_controller.StartTutorial(m_data.Type);
    }

    public override bool CanProgress()
    {
        if (m_currentIndex == 6) return m_swapCompleted;
        return Input.GetMouseButtonDown(0);
    }

    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0: m_controller.ShowGuideHighlight(GuideHighlightType.切人按键); break;
            case 4: m_controller.HideGuideHighlight(); break;
        }
    }

    public override void OnTutorialStart()
    {
        CharacterManager.SwapCompleted += OnSwapCompleted;
    }

    private void OnSwapCompleted()
    {
        m_swapCompleted = true;
    }

    public override void OnTutorialEnd()
    {
        m_swapCompleted = false;
        CharacterManager.SwapCompleted -= OnSwapCompleted;
    }
}

/// <summary>
/// 新角色引导教程行为（教程十六）
/// 战斗结束回主界面后触发，高亮角色按钮，文本2等待进入角色页面
/// </summary>
public class NewCharacterIntroTutorial : TutorialBehavior
{
    private bool m_enteredCharacterPanel = false;

    public override void StartListening()
    {
        ReinforcementArriveTutorial.TutorialEnded += OnReinforcementEnded;
    }

    public override void StopListening()
    {
        ReinforcementArriveTutorial.TutorialEnded -= OnReinforcementEnded;
    }

    private void OnReinforcementEnded(TutorialType type)
    {
        if (type == TutorialType.教程十五)
            m_controller.StartTutorial(m_data.Type);
    }

    public override bool CanProgress()
    {
        if (m_currentIndex == 2) return m_enteredCharacterPanel;
        return Input.GetMouseButtonDown(0);
    }

    public override void OnProgress(int index)
    {
        switch (index)
        {
            case 0: m_controller.ShowGuideHighlight(GuideHighlightType.角色栏); break;
            case 1: m_controller.HideGuideHighlight(); break;
        }
    }

    public override void OnTutorialStart()
    {
        ChangePanelButton.PanelSwitched += OnPanelSwitched;
    }

    private void OnPanelSwitched(PanelType panelType)
    {
        if (panelType == PanelType.角色页面) m_enteredCharacterPanel = true;
    }

    public override void OnTutorialEnd()
    {
        m_enteredCharacterPanel = false;
        ChangePanelButton.PanelSwitched -= OnPanelSwitched;
    }
}

/// <summary>
/// 最终测验教程行为（教程十七）
/// 教程十六结束且退出角色界面后触发，纯点击
/// </summary>
public class FinalTestTutorial : TutorialBehavior
{
    public override void StartListening()
    {
        NewCharacterIntroTutorial.TutorialEnded += OnNewCharacterEnded;
    }

    public override void StopListening()
    {
        NewCharacterIntroTutorial.TutorialEnded -= OnNewCharacterEnded;
    }

    private void OnNewCharacterEnded(TutorialType type)
    {
        if (type == TutorialType.教程十六)
            m_controller.StartTutorial(m_data.Type);
    }
}

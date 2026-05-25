using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StartBattleButton : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private PreparationPanelView preparationPanel;
    [SerializeField] private string battleSceneName;

    private void Awake()
    {
        BindButton();
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartBattleClicked);
        }
    }

    public void OnStartBattleClicked()
    {
        if (preparationPanel == null)
        {
            preparationPanel = PreparationPanelView.Instance;
        }

        if (preparationPanel == null || preparationPanel.CurrentLevelData == null)
        {
            Debug.LogWarning("[StartBattleButton] 当前没有可用的关卡数据，无法开始战斗。", this);
            return;
        }

        if (!preparationPanel.HasEnoughSelectedCharacters)
        {
            Debug.LogWarning("[StartBattleButton] 需要先选择两名优先出场角色，才能进入战斗。", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogWarning("[StartBattleButton] 未配置战斗场景名，无法载入战斗场景。", this);
            return;
        }

        Datas.Instance?.MarkLevelCompleted(preparationPanel.CurrentLevelData.levelId);

        BattleLaunchContext.SetPendingLevelData(preparationPanel.CurrentLevelData, preparationPanel.SelectedFieldCharacters);
        SceneManager.LoadScene(battleSceneName);
    }

    private void BindButton()
    {
        if (startButton == null)
        {
            startButton = GetComponent<Button>();
        }

        if (preparationPanel == null)
        {
            preparationPanel = PreparationPanelView.Instance;
        }

        if (startButton == null)
        {
            return;
        }

        startButton.onClick.RemoveListener(OnStartBattleClicked);
        startButton.onClick.AddListener(OnStartBattleClicked);
    }
}
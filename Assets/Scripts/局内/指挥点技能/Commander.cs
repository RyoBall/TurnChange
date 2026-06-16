using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Commander : MonoBehaviour
{
    private const int DefaultCommandPoints = 1;
    private const int DefaultMaxCommandPoints = 5;
    private const int KillRecoveryAmount = 1;
    private const int GuaranteeRecoveryAmount = 1;
    private const float GuaranteeActionValueThreshold = 180f;

    [Header("指挥点飞行动画")]
    [SerializeField] private Camera effectCamera;
    [SerializeField] private GameObject flyIconPrefab;
    [SerializeField] private float flyDuration = 0.8f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private Ease flyEase = Ease.InOutQuad;
    [SerializeField] private Canvas canvas; // 可以在Inspector中指定Canvas，如果不指定会在运行时查找
    public CommandSkillBase changeSkill;
    private static Commander Instance ;
    public static Commander GetInstance()
    {
        return Instance;
    }
    private int commandPoints = DefaultCommandPoints;
    public int CommandPoints
    {
        get { return commandPoints; }
        private set{;}
    }
    private int maxCommandPoints = DefaultMaxCommandPoints;
    private float actionValueSinceLastRecovery;
    public int MaxCommandPoints => maxCommandPoints;

    /// <summary>
    /// 设置初始指挥点（用于教程关等特殊关卡）
    /// </summary>
    public void SetInitialCommandPoints(int points)
    {
        commandPoints = Mathf.Clamp(points, 0, maxCommandPoints);
        actionValueSinceLastRecovery = 0f;
    }

    // 棋局Boss 王车易位机会系统
    private int m_castlingOpportunities;
    private const int MaxCastlingOpportunities = 2;

    public int CastlingOpportunities => m_castlingOpportunities;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            RecoverCommandPointsInternal(1,"");
            commandPoints -= 1;
        }
    }
    public void AddCastlingOpportunity(int amount, string tipText = null)
    {
        if (amount <= 0)
        {
            return;
        }

        int before = m_castlingOpportunities;
        m_castlingOpportunities = Mathf.Clamp(m_castlingOpportunities + amount, 0, MaxCastlingOpportunities);
        if (m_castlingOpportunities > before && !string.IsNullOrEmpty(tipText))
        {
            FloatingTipGenerator.Instance?.ShowDefaultTip(tipText);
        }
    }

    public bool TryConsumeCastlingOpportunity()
    {
        if (m_castlingOpportunities <= 0)
        {
            return false;
        }

        m_castlingOpportunities--;
        return true;
    }

    public void ResetCastlingOpportunities()
    {
        m_castlingOpportunities = 0;
    }

    public bool UseCommandPoints(int amount)
    {
        if (amount > 0 && amount <= commandPoints)
        {
            commandPoints -= amount;
            TemporaryBattleModifierRuntimeManager.NotifyCommandPointsSpent(amount);
            return true;
            //这里可以添加一些使用指示点后的逻辑，比如UI更新等
        }
        else
        {
            return false;
        }
    }

    public bool RecoverCommandPoints(int amount, string tipText = null)
    {
        return RecoverCommandPointsInternal(amount, tipText);
    }

    public void NotifyEnemyKilled()
    {
        RecoverCommandPoints(KillRecoveryAmount, $"击杀回点+{KillRecoveryAmount}");
    }

    public void NotifyActionValueAdvanced(float actionValue)
    {
        if (actionValue <= 0f)
        {
            return;
        }

        actionValueSinceLastRecovery += actionValue;
        while (actionValueSinceLastRecovery >= GuaranteeActionValueThreshold)
        {
            actionValueSinceLastRecovery -= GuaranteeActionValueThreshold;
            RecoverCommandPointsInternal(GuaranteeRecoveryAmount, $"指挥点+{GuaranteeRecoveryAmount}");
        }
    }

    private bool RecoverCommandPointsInternal(int amount, string tipText)
    {
        if (amount <= 0)
        {
            return false;
        }

        int before = commandPoints;
        commandPoints = Mathf.Clamp(commandPoints + amount, 0, maxCommandPoints);
        int actualRecovered = commandPoints - before;
        if (actualRecovered <= 0)
        {
            return false;
        }
        FloatingTipGenerator.Instance?.ShowDefaultTip(string.IsNullOrEmpty(tipText)
            ? $"指挥点+{actualRecovered}"
            : tipText);

        // 播放指挥点飞入动画
        PlayFlyInAnimation(actualRecovered);

        return true;
    }


    /// <summary>
    /// 播放指挥点飞入槽位的动画：在屏幕中央生成图标 → DOTween移动到目标槽位 → 淡化并销毁
    /// </summary>
    private void PlayFlyInAnimation(int amount)
    {
        StartCoroutine(PlayFlyInAnimationCoroutine(amount));
    }

    private IEnumerator PlayFlyInAnimationCoroutine(int amount)
    {
        // 查找场景中的Canvas（仅用于获取UI相机和坐标转换）
        Canvas canvas = this.canvas ?? FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            yield break;
        }

        // 获取目的地坐标（指挥点槽位的世界坐标）
        CommandPointSlotUI slotUI = CommandPointSlotUI.Instance;
        Vector3 targetWorldPos;
        if (slotUI != null)
        {
            var screenTargetPos = slotUI.GetLastFilledSlotScreenPosition();
            targetWorldPos=effectCamera.ScreenToWorldPoint(screenTargetPos)+Vector3.forward*10f; // 加一个向前的偏移，确保在UI前面显示
        }
        else
        {
            targetWorldPos = canvas.transform.position;
        }

        // 计算屏幕中心对应的世界坐标
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (canvas.worldCamera ?? Camera.main);
        Vector3 startWorldPos;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // ScreenSpaceOverlay 模式下，用 Canvas 的坐标空间推算世界坐标
            // 将屏幕中心转为 Canvas 局部坐标，再转回世界坐标
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            startWorldPos = effectCamera.ScreenToWorldPoint(screenCenter)+Vector3.forward*10f; // 加一个向前的偏移，确保在UI前面显示
        }  
        else
        {
            startWorldPos = uiCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, canvas.transform.position.z));
        }

        for (int i = 0; i < amount; i++)
        {
            // 有小延迟让多个图标依次飞出
            if (i > 0)
            {
                yield return new WaitForSeconds(0.12f);
            }

            // 生成飞行图标（放在世界坐标，不挂载到Canvas下）
            GameObject flyIcon = CreateFlyIcon(null);
            if (flyIcon == null)
            {
                continue;
            }

            // 设置初始位置为屏幕中心的世界坐标
            flyIcon.transform.position = startWorldPos;

            // DOTween动画：使用DOMove在世界坐标中移动
            SpriteRenderer spriteRenderer = flyIcon.GetComponent<SpriteRenderer>();
            Sequence seq = DOTween.Sequence();
            seq.Join(flyIcon.transform.DOMove(targetWorldPos, flyDuration).SetEase(flyEase));
            // 到达后淡出并摧毁
            seq.OnComplete(() =>
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.DOFade(0f, fadeDuration).OnComplete(() =>
                    {
                        if (flyIcon != null) Destroy(flyIcon);
                    });
                }
                else
                {
                    Destroy(flyIcon);
                }
            });
            seq.Play();
        }
    }

    /// <summary>
    /// 创建飞行图标GameObject，包含Image和CanvasGroup组件
    /// </summary>
    private GameObject CreateFlyIcon(Transform parent)
    {
        GameObject iconObj;

        if (flyIconPrefab != null)
        {
            iconObj = parent != null ? Instantiate(flyIconPrefab, parent) : Instantiate(flyIconPrefab);
        }
        else
        {
            // 无预制体时，创建一个带SpriteRenderer的简单GameObject
            iconObj = new GameObject("CommandPointFlyIcon", typeof(SpriteRenderer));
            if (parent != null)
            {
                iconObj.transform.SetParent(parent, false);
            }

            SpriteRenderer spriteRenderer = iconObj.GetComponent<SpriteRenderer>();
            // 使用默认的黄色方形（可在Inspector中替换）
            spriteRenderer.color = Color.yellow;
            spriteRenderer.sortingOrder = 999;

            iconObj.transform.localScale = Vector3.one * 0.5f;
        }

        return iconObj;
    }
}

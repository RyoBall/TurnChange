using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 伤害文本对象池
/// </summary>
public class DamageTextPool : MonoBehaviour
{
    public static DamageTextPool Instance { get; private set; }

    [Header("对象池设置")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Transform poolParent;

    [Header("状态提示设置")]
    [SerializeField] private GameObject stateTipPrefab;
    [SerializeField] private float stateTipFloatDistance = 50f;
    [SerializeField] private float stateTipDuration = 1.5f;

    private Queue<DamageText> pool = new Queue<DamageText>();
    private Canvas rootCanvas;
    private RectTransform poolParentRectTransform;
    private Camera uiCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (poolParent == null)
        {
            poolParent = transform;
        }

        poolParentRectTransform = poolParent as RectTransform;
        rootCanvas = poolParent != null ? poolParent.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();

        uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        if (poolParentRectTransform == null)
        {
            Debug.LogError("DamageTextPool 的 poolParent 必须是 UI RectTransform。", this);
            return;
        }

        if (rootCanvas == null)
        {
            Debug.LogError("DamageTextPool 未找到所属 Canvas，无法初始化伤害跳字。", this);
            return;
        }

        // ��ʼ�������
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewObject();
        }
    }

    private DamageText CreateNewObject()
    {
        GameObject obj = Instantiate(damageTextPrefab, poolParent);
        obj.SetActive(false);
        DamageText damageText = obj.GetComponent<DamageText>();
        damageText.Initialize(rootCanvas, poolParentRectTransform);
        pool.Enqueue(damageText);
        return damageText;
    }

    /// <summary>
    /// ��ȡ�˺����ֶ���
    /// </summary>
    public DamageText Get()
    {
        if (pool.Count == 0)
        {
            CreateNewObject();
        }

        DamageText damageText = pool.Dequeue();
        damageText.gameObject.SetActive(true);
        return damageText;
    }

    /// <summary>
    /// �����˺����ֶ���
    /// </summary>
    public void ReturnToPool(DamageText damageText)
    {
        damageText.gameObject.SetActive(false);
        pool.Enqueue(damageText);
    }
    public void ShowDamage(int damage, Vector3 worldPosition, bool isDotDamage = false, bool isCriticalHit = false)
    {
        DamageText damageText = Get();
        damageText.ShowDamage(damage, worldPosition, isDotDamage, isCriticalHit);
    }
    public void ShowHeal(int healAmount, Vector3 worldPosition)
    {
        DamageText damageText = Get();
        damageText.ShowHeal(healAmount, worldPosition);
    }
    public void ShowCustomText(string customMessage, Vector3 position, Color color=default)
    {
        if(color == default)
        {
            color = Color.white;
        }
        DamageText damageText = Get();
        damageText.ShowCustomText(customMessage, position, color);
    }

    /// <summary>
    /// 在指定 Transform 位置生成状态漂浮提示（图标 + 名称）
    /// </summary>
    /// <param name="targetTransform">目标 Transform</param>
    /// <param name="state">状态 ScriptableObject</param>
    public void ShowStateTipAtObject(Transform targetTransform, State state)
    {
        if (targetTransform == null || state == null)
        {
            Debug.LogError("[DamageTextPool] ShowStateTipAtObject: Transform 或 State 为空");
            return;
        }

        if (rootCanvas == null || poolParentRectTransform == null)
        {
            Debug.LogError("[DamageTextPool] ShowStateTipAtObject: Canvas 未初始化");
            return;
        }

        // 世界坐标转屏幕坐标
        Camera worldCamera = Camera.main;
        if (worldCamera == null) return;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(targetTransform.position);
        if (screenPos.z < 0f) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                poolParentRectTransform, screenPos, uiCamera, out Vector2 localPoint))
        {
            return;
        }

        // 实例化预制体或动态创建
        GameObject tipObj;
        if (stateTipPrefab != null)
        {
            tipObj = Instantiate(stateTipPrefab, poolParentRectTransform);
        }
        else
        {
            Debug.LogWarning("NoTipOBJ");
            return;
        }
        tipObj.name = $"StateTip_{state.stateType}";

        RectTransform rect = tipObj.GetComponent<RectTransform>();
        if (rect == null) rect = tipObj.AddComponent<RectTransform>();
        rect.anchoredPosition = localPoint + new Vector2(0, 40f);

        // 设置图标和名称
        StateTipUI stateTipUI = tipObj.GetComponent<StateTipUI>();
        if (stateTipUI == null) stateTipUI = tipObj.AddComponent<StateTipUI>();

        string stateName = StateDictionaryManager.GetStateName(state.stateType);
        stateTipUI.SetState(state.icon, stateName);

        // 启动漂浮动画，结束后销毁
        StartCoroutine(AnimateAndDestroyStateTip(tipObj));
    }

    private System.Collections.IEnumerator AnimateAndDestroyStateTip(GameObject tipObj)
    {
        if (tipObj == null) yield break;

        RectTransform rect = tipObj.GetComponent<RectTransform>();
        CanvasGroup cg = tipObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = tipObj.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        float fadeInDuration = 0.2f;
        float totalDuration = stateTipDuration;
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * stateTipFloatDistance;

        // 淡入
        while (elapsed < fadeInDuration)
        {
            if (tipObj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t * 0.3f);
            yield return null;
        }

        if (tipObj == null) yield break;
        cg.alpha = 1f;

        // 悬浮 + 淡出
        float remainDuration = totalDuration - fadeInDuration;
        float remainElapsed = 0f;
        Vector2 hoverStartPos = rect.anchoredPosition;

        while (remainElapsed < remainDuration)
        {
            if (tipObj == null) yield break;
            remainElapsed += Time.deltaTime;
            float t = remainElapsed / remainDuration;
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            rect.anchoredPosition = Vector2.Lerp(hoverStartPos, endPos, t);
            yield return null;
        }

        if (tipObj != null)
        {
            Destroy(tipObj);
        }
    }
    private static void SetStateTipUIRefs(StateTipUI stateTipUI, Image icon, TMP_Text nameText)
    {
        if (stateTipUI != null)
        {
            stateTipUI.BindComponents(icon, nameText);
        }
    }
}
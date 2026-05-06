using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �˺����ֶ����
/// </summary>
public class DamageTextPool : MonoBehaviour
{
    public static DamageTextPool Instance { get; private set; }

    [Header("���������")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Transform poolParent;

    private Queue<DamageText> pool = new Queue<DamageText>();
    private Canvas rootCanvas;
    private RectTransform poolParentRectTransform;

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
}
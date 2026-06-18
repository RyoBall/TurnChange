using UnityEngine;
using UnityEngine.UI;

public class CustomAnimatedCursor : MonoBehaviour
{
    public static CustomAnimatedCursor Instance { get; private set; }

    [Header("光标图片")]
    public Sprite cursorSprite;

    [Header("点击动效")]
    public float pressScale = 0.7f;
    public float overshootScale = 1.1f;
    public float animDuration = 0.15f;

    [Header("热点偏移 (相对图片左上角)")]
    public Vector2 hotspot = Vector2.zero;

    [Header("整体缩放 (1 = 原始大小)")]
    [Range(0.1f, 3f)]
    public float cursorScale = 1f;  // ★ 新增：可调缩放

    private Image cursorImage;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // --- 1. 创建专属Canvas并强制最高层级 ---
        GameObject canvasGO = new GameObject("CursorCanvas");
        parentCanvas = canvasGO.AddComponent<Canvas>();
        parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        parentCanvas.overrideSorting = true;
        parentCanvas.sortingOrder =3000;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler != null) DestroyImmediate(scaler);
        GraphicRaycaster raycaster = canvasGO.GetComponent<GraphicRaycaster>();
        if (raycaster != null) DestroyImmediate(raycaster);

        DontDestroyOnLoad(canvasGO);

        // --- 2. 设置光标物体 ---
        cursorImage = GetComponent<Image>();
        if (cursorImage == null) cursorImage = gameObject.AddComponent<Image>();

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) rectTransform = gameObject.AddComponent<RectTransform>();

        transform.SetParent(parentCanvas.transform);

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0, 0);
        rectTransform.anchoredPosition = Vector2.zero;

        // 加载图片并应用缩放
        if (cursorSprite != null)
        {
            float width = cursorSprite.rect.width * cursorScale;   // ★ 应用缩放
            float height = cursorSprite.rect.height * cursorScale; // ★ 应用缩放
            rectTransform.sizeDelta = new Vector2(width, height);
            cursorImage.sprite = cursorSprite;
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(32, 32);
            // ... 省略临时纹理 ...
        }

        cursorImage.color = Color.white;
        Cursor.visible = false;
        rectTransform.localScale = Vector3.one;  // 不再用 localScale 缩放，直接用 sizeDelta

        transform.SetAsLastSibling();

        Debug.Log($"✅ 光标初始化成功 (排序: {parentCanvas.sortingOrder}, 缩放: {cursorScale})");
    }

    private void Update()
    {
        Cursor.visible = false;
        // 坐标逻辑不变
        rectTransform.anchoredPosition = (Vector2)Input.mousePosition - hotspot;

        // 点击动效
        if (Input.GetMouseButtonDown(0))
        {
            StopAllCoroutines();
            StartCoroutine(PlayClickAnimation());
        }
    }

    // ... 动效协程保持不变 (省略，与之前一样) ...

    private System.Collections.IEnumerator PlayClickAnimation()
    {
        // 动效中使用 localScale，但我们用 sizeDelta 控制基础大小，动效缩放 relative 到 localScale 也没问题
        // 但为了不影响基础大小，动效最好基于 localScale 来做，所以保留 localScale 动画
        // 不过我们设置 localScale 为 1，动效缩放后恢复，不会改变 sizeDelta
        // 这样 sizeDelta 控制静态大小，localScale 控制动效。
        // 保持不变即可。
        Vector3 startScale = Vector3.one;
        Vector3 targetPress = new Vector3(pressScale, pressScale, 1f);
        Vector3 targetOvershoot = new Vector3(overshootScale, overshootScale, 1f);

        float halfDur = animDuration / 2f;
        float elapsed = 0f;

        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDur;
            float smooth = t * t * (3f - 2f * t);
            rectTransform.localScale = Vector3.Lerp(startScale, targetPress, smooth);
            yield return null;
        }

        elapsed = 0f;
        float overshootDur = halfDur * 0.6f;
        while (elapsed < overshootDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / overshootDur;
            float smooth = t * t * (3f - 2f * t);
            rectTransform.localScale = Vector3.Lerp(targetPress, targetOvershoot, smooth);
            yield return null;
        }

        elapsed = 0f;
        float returnDur = halfDur * 0.4f;
        Vector3 startOvershoot = targetOvershoot;
        while (elapsed < returnDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDur;
            float smooth = t * t * (3f - 2f * t);
            rectTransform.localScale = Vector3.Lerp(startOvershoot, Vector3.one, smooth);
            yield return null;
        }
        rectTransform.localScale = Vector3.one;
    }

    public void ChangeCursorSprite(Sprite newSprite)
    {
        if (newSprite == null) return;
        cursorImage.sprite = newSprite;
        float width = newSprite.rect.width * cursorScale;
        float height = newSprite.rect.height * cursorScale;
        rectTransform.sizeDelta = new Vector2(width, height);
    }

    private void OnDestroy() { Cursor.visible = true; }
}
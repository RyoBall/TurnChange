using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitHealthUIController : MonoBehaviour
{
    [Header("绑定")]
    [SerializeField] private UnitCombatant targetUnit;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text hpText;

    [Header("差值显示（回血/掉血）")]
    [SerializeField] private Image diffImage;
    [SerializeField] private Color damageDiffColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color healColor = new Color(0.4f, 1f, 0.4f, 1f);
    [SerializeField] private float decreaseSpeed = 0.5f;
    [SerializeField] private float increaseSpeed = 1.0f;
    [SerializeField] private float diffFadeSpeed = 2f;
    [SerializeField] private float diffAlphaScale = 5f;
    [SerializeField] private bool autoCreateDiffImage = true;

    private const float DiffEpsilon = 0.0001f;

    private RectTransform fillRect;
    private RectTransform diffRect;
    private float targetValue = 0f;
    private float displayedValue = 0f;
    private float diffValue = 0f;
    private float diffAlpha = 0f;
    private Color currentDiffColor;
    private bool visualsInitialized = false;

    private void Reset()
    {
        ResolveReferences();
        SetupVisuals();
        SyncImmediate();
    }

    private void Awake()
    {
        ResolveReferences();
        SetupVisuals();
        SyncImmediate();
    }

    public void Initialize(UnitCombatant unit)
    {
        targetUnit = unit;
        SyncImmediate();
    }

    private void OnEnable()
    {
        SyncImmediate();
    }

    private void Update()
    {
        if (!EnsureVisuals())
        {
            UpdateText(0);
            return;
        }

        float nextTargetValue = GetHealthPercent();
        if (Mathf.Abs(nextTargetValue - targetValue) > DiffEpsilon)
        {
            HandleTargetValueChanged(nextTargetValue);
        }

        AnimateFillAmounts();

        fillImage.fillAmount = displayedValue;
        UpdateDiffOverlay();
        UpdateText(targetUnit == null ? 0 : targetUnit.currentHP);
    }

    public void SetTarget(UnitCombatant unit)
    {
        targetUnit = unit;
        SyncImmediate();
    }

    private bool EnsureVisuals()
    {
        if (visualsInitialized && fillImage != null && fillRect != null)
        {
            return true;
        }

        ResolveReferences();
        SetupVisuals();
        return fillImage != null && fillRect != null;
    }

    private void ResolveReferences()
    {
        if (fillImage == null)
        {
            fillImage = FindPrimaryFillImage();
        }

        if (fillImage != null)
        {
            fillRect = fillImage.rectTransform;
        }

        if (diffImage == fillImage)
        {
            diffImage = null;
        }

        if (diffImage != null)
        {
            diffRect = diffImage.rectTransform;
        }
    }

    private Image FindPrimaryFillImage()
    {
        Image selfImage = GetComponent<Image>();
        if (selfImage != null && selfImage.type == Image.Type.Filled)
        {
            return selfImage;
        }

        Image[] childImages = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < childImages.Length; i++)
        {
            if (childImages[i] != null && childImages[i] != diffImage && childImages[i].type == Image.Type.Filled)
            {
                return childImages[i];
            }
        }

        return null;
    }

    private void SetupVisuals()
    {
        visualsInitialized = false;

        if (fillImage == null)
        {
            return;
        }

        if (fillImage.type != Image.Type.Filled)
        {
            fillImage.type = Image.Type.Filled;
        }

        if (fillImage.fillMethod != Image.FillMethod.Horizontal && fillImage.fillMethod != Image.FillMethod.Vertical)
        {
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        fillRect = fillImage.rectTransform;

        if (diffImage == null && autoCreateDiffImage)
        {
            CreateDiffImage();
        }

        if (diffImage != null)
        {
            diffRect = diffImage.rectTransform;
            CopyImageStyle(fillImage, diffImage);
            MatchRectTransform(fillRect, diffRect);
            diffImage.raycastTarget = false;
            diffImage.fillAmount = fillImage.fillAmount;
            diffImage.gameObject.SetActive(false);

            int fillIndex = fillRect.GetSiblingIndex();
            diffRect.SetSiblingIndex(fillIndex);
            fillRect.SetSiblingIndex(fillIndex + 1);
        }

        visualsInitialized = fillRect != null;
    }

    private void CreateDiffImage()
    {
        if (fillRect == null || fillRect.parent == null)
        {
            return;
        }

        GameObject go = new GameObject("DiffFill", typeof(RectTransform), typeof(Image));
        diffRect = go.GetComponent<RectTransform>();
        diffRect.SetParent(fillRect.parent, false);
        diffImage = go.GetComponent<Image>();
    }

    private void CopyImageStyle(Image source, Image destination)
    {
        destination.sprite = source.sprite;
        destination.type = source.type;
        destination.fillMethod = source.fillMethod;
        destination.fillOrigin = source.fillOrigin;
        destination.fillClockwise = source.fillClockwise;
        destination.preserveAspect = source.preserveAspect;
        destination.useSpriteMesh = source.useSpriteMesh;
        destination.material = source.material;
        destination.maskable = source.maskable;
    }

    private void MatchRectTransform(RectTransform source, RectTransform destination)
    {
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.anchoredPosition3D = source.anchoredPosition3D;
        destination.sizeDelta = source.sizeDelta;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
        destination.offsetMin = source.offsetMin;
        destination.offsetMax = source.offsetMax;
    }

    private float GetHealthPercent()
    {
        if (targetUnit == null || targetUnit.maxHP <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)targetUnit.currentHP / targetUnit.maxHP);
    }

    private void SyncImmediate()
    {
        targetValue = GetHealthPercent();
        displayedValue = targetValue;
        diffValue = targetValue;
        diffAlpha = 0f;
        currentDiffColor = damageDiffColor;

        if (EnsureVisuals())
        {
            fillImage.fillAmount = displayedValue;
            HideDiffOverlay();
        }

        UpdateText(targetUnit == null ? 0 : targetUnit.currentHP);
    }

    private void HandleTargetValueChanged(float nextTargetValue)
    {
        if (nextTargetValue < displayedValue - DiffEpsilon)
        {
            diffValue = Mathf.Max(diffValue, displayedValue);
            displayedValue = nextTargetValue;
            currentDiffColor = damageDiffColor;
        }
        else if (nextTargetValue > displayedValue + DiffEpsilon)
        {
            diffValue = nextTargetValue;
            currentDiffColor = healColor;
        }
        else
        {
            displayedValue = nextTargetValue;
            diffValue = nextTargetValue;
        }

        targetValue = nextTargetValue;
    }

    private void AnimateFillAmounts()
    {
        if (displayedValue < targetValue - DiffEpsilon)
        {
            displayedValue = Mathf.MoveTowards(displayedValue, targetValue, increaseSpeed * Time.deltaTime);
        }
        else if (displayedValue > targetValue + DiffEpsilon)
        {
            displayedValue = targetValue;
        }

        if (diffValue > targetValue + DiffEpsilon)
        {
            diffValue = Mathf.MoveTowards(diffValue, targetValue, decreaseSpeed * Time.deltaTime);
        }
        else if (diffValue < targetValue - DiffEpsilon)
        {
            diffValue = targetValue;
        }
    }

    private void UpdateDiffOverlay()
    {
        if (diffImage == null)
        {
            return;
        }

        float difference = diffValue - displayedValue;

        if (difference <= DiffEpsilon)
        {
            HideDiffOverlay();
            return;
        }

        diffImage.fillAmount = diffValue;

        float desiredAlpha = Mathf.Clamp01(difference * diffAlphaScale);
        diffAlpha = Mathf.MoveTowards(diffAlpha, desiredAlpha, diffFadeSpeed * Time.deltaTime);

        Color diffColor = currentDiffColor;
        diffColor.a = diffAlpha;
        diffImage.color = diffColor;

        if (!diffImage.gameObject.activeSelf)
        {
            diffImage.gameObject.SetActive(true);
        }
    }

    private void HideDiffOverlay()
    {
        if (diffImage == null)
        {
            return;
        }

        diffValue = displayedValue;
        diffImage.fillAmount = displayedValue;
        diffImage.gameObject.SetActive(false);
        diffAlpha = 0f;
        Color hiddenColor = diffImage.color;
        hiddenColor.a = 0f;
        diffImage.color = hiddenColor;
    }

    private void UpdateText(int currentHp)
    {
        if (hpText != null)
        {
            hpText.text = currentHp.ToString();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
        SetupVisuals();

        if (!Application.isPlaying)
        {
            SyncImmediate();
        }
    }
#endif
}

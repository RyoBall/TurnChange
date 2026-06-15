using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIEmergencyPulseEffect : MonoBehaviour
{
    public enum EmergencyPulseRenderMode
    {
        Layered = 0,
        Soft = 1
    }

    private static readonly int PhaseId = Shader.PropertyToID("_Phase");
    private static readonly int ExpandPixelsId = Shader.PropertyToID("_ExpandPixels");
    private static readonly int WhiteRimPixelsId = Shader.PropertyToID("_WhiteRimPixels");
    private static readonly int RedHaloPixelsId = Shader.PropertyToID("_RedHaloPixels");
    private static readonly int OuterGlowPixelsId = Shader.PropertyToID("_OuterGlowPixels");
    private static readonly int OuterGlowStrengthId = Shader.PropertyToID("_OuterGlowStrength");
    private static readonly int OuterGlowSoftnessId = Shader.PropertyToID("_OuterGlowSoftness");
    private static readonly int GlowStepsId = Shader.PropertyToID("_GlowSteps");
    private static readonly int WhiteBoostId = Shader.PropertyToID("_WhiteBoost");
    private static readonly int RedBoostId = Shader.PropertyToID("_RedBoost");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int BreathBrightnessId = Shader.PropertyToID("_BreathBrightness");
    private static readonly int AlphaBlurId = Shader.PropertyToID("_AlphaBlur");
    private static readonly string SoftGlowKeyword = "_PULSE_SOFT";

    [Header("Shape")]
    [SerializeField] private Image shapeSource;
    [SerializeField] private Image pulseImage;
    [SerializeField] private Material pulseMaterialTemplate;
    [SerializeField] private bool renderOnTop = true;
    [SerializeField] private bool ignoreParentCanvasGroup = true;
    [SerializeField] private EmergencyPulseRenderMode renderMode = EmergencyPulseRenderMode.Layered;
    [SerializeField] private bool onlyForChangeButton = false;

    [Header("Animation")]
    [SerializeField] private float pulseSpeed = 1.35f;
    [SerializeField] private bool usePingPong = true;
    [SerializeField] private bool enableBrightnessBreath = true;
    [FormerlySerializedAs("brightnessBreathSpeed")]
    [SerializeField, Min(0.15f)] private float brightnessBreathPeriod = 1.2f;
    [SerializeField, Range(0f, 1.5f)] private float brightnessMin = 0.9f;
    [SerializeField, Range(0f, 1.5f)] private float brightnessMax = 1.15f;

    [Header("Glow")]
    [SerializeField] private float expandPixels = 10f;
    [SerializeField] private float whiteRimPixels = 3f;
    [SerializeField] private float redHaloPixels = 14f;
    [SerializeField] private float outerGlowPixels = 38f;
    [SerializeField] private float outerGlowStrength = 1.15f;
    [SerializeField] private float outerGlowSoftness = 4.2f;
    [SerializeField, Range(3, 8)] private int glowSteps = 6;
    [SerializeField] private float whiteBoost = 4f;
    [SerializeField] private float redBoost = 1.2f;
    [SerializeField] private float intensity = 1.35f;
    [SerializeField] private float alphaBlur = 5f;
    [SerializeField] private float rectOutset = 18f;

    private Material m_PulseMaterial;
    private CanvasGroup m_PulseCanvasGroup;
    private float m_Phase;
    private float m_BreathPhase;
    private bool m_IsActive;

    public bool IsEmergencyActive => m_IsActive;
    public bool HasValidShape => shapeSource != null && shapeSource.sprite != null;

    private void Awake()
    {
        RebuildIfNeeded();
        SetEmergencyActive(false);
    }

    private void Start()
    {
        if (!onlyForChangeButton)
        {
            return;
        }

        StartCoroutine(DisableIfNotChangeButton());
    }

    private IEnumerator DisableIfNotChangeButton()
    {
        yield return null;

        CommandButton button = GetComponent<CommandButton>();
        if (button != null && !button.IsChangeSkillButton)
        {
            SetEmergencyActive(false);
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (m_PulseMaterial != null)
        {
            Destroy(m_PulseMaterial);
            m_PulseMaterial = null;
        }
    }

    private void Update()
    {
        if (!m_IsActive || m_PulseMaterial == null)
        {
            return;
        }

        float delta = pulseSpeed * Time.deltaTime;
        m_Phase = usePingPong
            ? Mathf.PingPong(m_Phase + delta, 1f)
            : Mathf.Repeat(m_Phase + delta, 1f);

        m_BreathPhase += Time.deltaTime / GetBreathPeriodSeconds();
        m_PulseMaterial.SetFloat(PhaseId, m_Phase);
        UpdateBreathBrightness();
    }

    private float GetBreathPeriodSeconds()
    {
        return Mathf.Max(Mathf.Abs(brightnessBreathPeriod), 0.15f);
    }

    private float EvaluateBreathBrightness()
    {
        if (!enableBrightnessBreath)
        {
            return 1f;
        }

        float wave = 0.5f - 0.5f * Mathf.Cos(m_BreathPhase * Mathf.PI * 2f);
        return Mathf.Lerp(brightnessMin, brightnessMax, wave);
    }

    private void UpdateBreathBrightness()
    {
        if (m_PulseMaterial == null)
        {
            return;
        }

        m_PulseMaterial.SetFloat(BreathBrightnessId, EvaluateBreathBrightness());
    }

    public void ConfigureFromShape(Image source)
    {
        shapeSource = source;
        RebuildIfNeeded();
    }

    public void SetEmergencyActive(bool active)
    {
        if (m_IsActive == active)
        {
            return;
        }

        m_IsActive = active;

        if (active)
        {
            RebuildIfNeeded();
        }

        if (!HasValidShape || pulseImage == null)
        {
            return;
        }

        pulseImage.gameObject.SetActive(active);

        if (m_PulseCanvasGroup != null)
        {
            m_PulseCanvasGroup.alpha = active ? 1f : 0f;
        }

        if (active)
        {
            SyncFromShapeSource();
            AlignPulseRect();
            ApplyMaterialProperties();
            m_Phase = 0f;
            m_BreathPhase = 0f;
            if (m_PulseMaterial != null)
            {
                m_PulseMaterial.SetFloat(PhaseId, m_Phase);
                UpdateBreathBrightness();
            }
        }
    }

    public void RebuildIfNeeded()
    {
        ResolveShapeSource();
        EnsurePulseImage();
        EnsureMaterial();
        SyncFromShapeSource();
        AlignPulseRect();
        ApplyMaterialProperties();
    }

    private void ResolveShapeSource()
    {
        if (shapeSource != null && shapeSource.sprite != null)
        {
            return;
        }

        Transform baseTransform = transform.Find("基底");
        if (baseTransform != null)
        {
            shapeSource = baseTransform.GetComponent<Image>();
        }

        if (shapeSource == null || shapeSource.sprite == null)
        {
            shapeSource = GetComponent<Image>();
        }

        if (shapeSource == null || shapeSource.sprite == null)
        {
            shapeSource = FindBestShapeImage();
        }
    }

    private Image FindBestShapeImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        Image best = null;
        float bestArea = 0f;

        foreach (Image image in images)
        {
            if (image == null || image == pulseImage || image.sprite == null)
            {
                continue;
            }

            if (image.gameObject.name == "EmergencyPulse")
            {
                continue;
            }

            Rect rect = image.rectTransform.rect;
            float area = Mathf.Abs(rect.width * rect.height);
            if (area > bestArea)
            {
                bestArea = area;
                best = image;
            }
        }

        return best;
    }

    private void EnsurePulseImage()
    {
        if (shapeSource == null)
        {
            return;
        }

        if (pulseImage == null)
        {
            Transform existing = transform.Find("EmergencyPulse");
            if (existing == null && shapeSource.transform != transform)
            {
                existing = shapeSource.transform.Find("EmergencyPulse");
            }

            if (existing != null)
            {
                pulseImage = existing.GetComponent<Image>();
            }
        }

        if (pulseImage == null)
        {
            GameObject pulseObject = new GameObject(
                "EmergencyPulse",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));

            pulseObject.transform.SetParent(transform, false);
            pulseImage = pulseObject.GetComponent<Image>();
            pulseImage.raycastTarget = false;
            pulseImage.maskable = true;
            pulseImage.color = Color.white;

            m_PulseCanvasGroup = pulseObject.GetComponent<CanvasGroup>();
            m_PulseCanvasGroup.alpha = 0f;
            m_PulseCanvasGroup.interactable = false;
            m_PulseCanvasGroup.blocksRaycasts = false;
            m_PulseCanvasGroup.ignoreParentGroups = ignoreParentCanvasGroup;
        }
        else if (pulseImage.transform.parent != transform)
        {
            pulseImage.transform.SetParent(transform, false);
        }

        EnsurePulseCanvasGroup();
        PlacePulseInHierarchy();
        AlignPulseRect();
    }

    private void EnsurePulseCanvasGroup()
    {
        if (pulseImage == null)
        {
            return;
        }

        if (m_PulseCanvasGroup == null)
        {
            m_PulseCanvasGroup = pulseImage.GetComponent<CanvasGroup>();
            if (m_PulseCanvasGroup == null)
            {
                m_PulseCanvasGroup = pulseImage.gameObject.AddComponent<CanvasGroup>();
            }
        }

        m_PulseCanvasGroup.interactable = false;
        m_PulseCanvasGroup.blocksRaycasts = false;
        m_PulseCanvasGroup.ignoreParentGroups = ignoreParentCanvasGroup;
    }

    private void PlacePulseInHierarchy()
    {
        if (pulseImage == null)
        {
            return;
        }

        if (renderOnTop)
        {
            pulseImage.transform.SetAsLastSibling();
        }
        else
        {
            pulseImage.transform.SetAsFirstSibling();
        }
    }

    private void AlignPulseRect()
    {
        if (shapeSource == null || pulseImage == null)
        {
            return;
        }

        RectTransform shapeRect = shapeSource.rectTransform;
        RectTransform pulseRect = pulseImage.rectTransform;
        Vector2 outset = Vector2.one * rectOutset;

        if (shapeSource.transform == transform)
        {
            pulseRect.anchorMin = Vector2.zero;
            pulseRect.anchorMax = Vector2.one;
            pulseRect.pivot = new Vector2(0.5f, 0.5f);
            pulseRect.anchoredPosition = Vector2.zero;
            pulseRect.localScale = Vector3.one;
            pulseRect.localRotation = Quaternion.identity;
            pulseRect.offsetMin = -outset;
            pulseRect.offsetMax = outset;
            return;
        }

        pulseRect.anchorMin = shapeRect.anchorMin;
        pulseRect.anchorMax = shapeRect.anchorMax;
        pulseRect.pivot = shapeRect.pivot;
        pulseRect.anchoredPosition = shapeRect.anchoredPosition;
        pulseRect.sizeDelta = shapeRect.sizeDelta;
        pulseRect.localRotation = shapeRect.localRotation;
        pulseRect.localScale = shapeRect.localScale;
        pulseRect.offsetMin = shapeRect.offsetMin - outset;
        pulseRect.offsetMax = shapeRect.offsetMax + outset;
    }

    private void EnsureMaterial()
    {
        if (pulseImage == null)
        {
            return;
        }

        if (m_PulseMaterial == null)
        {
            Material template = pulseMaterialTemplate;
#if UNITY_EDITOR
            if (template == null)
            {
                template = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/VFX/UI/EmergencyPulse/UIEmergencyPulse.mat");
            }
#endif
            if (template == null)
            {
                Debug.LogWarning("[UIEmergencyPulseEffect] 缺少脉冲材质模板。", this);
                return;
            }

            m_PulseMaterial = new Material(template);
        }

        pulseImage.material = m_PulseMaterial;
    }

    private void SyncFromShapeSource()
    {
        if (shapeSource == null || pulseImage == null)
        {
            return;
        }

        pulseImage.sprite = shapeSource.sprite;
        pulseImage.type = shapeSource.type;
        pulseImage.preserveAspect = shapeSource.preserveAspect;
        pulseImage.fillCenter = shapeSource.fillCenter;
        pulseImage.fillMethod = shapeSource.fillMethod;
        pulseImage.fillOrigin = shapeSource.fillOrigin;
        pulseImage.fillAmount = shapeSource.fillAmount;
        pulseImage.fillClockwise = shapeSource.fillClockwise;
        pulseImage.pixelsPerUnitMultiplier = shapeSource.pixelsPerUnitMultiplier;
    }

    private void ApplyMaterialProperties()
    {
        if (m_PulseMaterial == null)
        {
            return;
        }

        m_PulseMaterial.SetFloat(ExpandPixelsId, expandPixels);
        m_PulseMaterial.SetFloat(WhiteRimPixelsId, whiteRimPixels);
        m_PulseMaterial.SetFloat(RedHaloPixelsId, redHaloPixels);
        m_PulseMaterial.SetFloat(OuterGlowPixelsId, outerGlowPixels);
        m_PulseMaterial.SetFloat(OuterGlowStrengthId, outerGlowStrength);
        m_PulseMaterial.SetFloat(OuterGlowSoftnessId, outerGlowSoftness);
        m_PulseMaterial.SetFloat(GlowStepsId, glowSteps);
        m_PulseMaterial.SetFloat(WhiteBoostId, whiteBoost);
        m_PulseMaterial.SetFloat(RedBoostId, redBoost);
        m_PulseMaterial.SetFloat(IntensityId, intensity);
        m_PulseMaterial.SetFloat(AlphaBlurId, alphaBlur);
        m_PulseMaterial.SetFloat(PhaseId, m_Phase);
        m_PulseMaterial.SetFloat(BreathBrightnessId, EvaluateBreathBrightness());

        if (renderMode == EmergencyPulseRenderMode.Soft)
        {
            m_PulseMaterial.EnableKeyword(SoftGlowKeyword);
        }
        else
        {
            m_PulseMaterial.DisableKeyword(SoftGlowKeyword);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        AlignPulseRect();
        ApplyMaterialProperties();
        if (m_IsActive)
        {
            UpdateBreathBrightness();
        }
    }
#endif
}

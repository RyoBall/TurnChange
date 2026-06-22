using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum FieldDomainVisualPhase
{
    Idle = 0,
    Expanding = 1,
    Active = 2,
    Contracting = 3
}

public class FieldDomainScreenEffectController : MonoBehaviour
{
    public static FieldDomainScreenEffectController Instance { get; private set; }
    public static bool IsRendering =>
        Instance != null && Instance.m_IsRendering && Instance.m_EffectIntensity > 0.001f;

    private static readonly int OriginId = Shader.PropertyToID("_Origin");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int MaxRadiusId = Shader.PropertyToID("_MaxRadius");
    private static readonly int WaveWidthId = Shader.PropertyToID("_WaveWidth");
    private static readonly int PhaseId = Shader.PropertyToID("_Phase");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
    private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
    private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
    private static readonly int DistortionStrengthId = Shader.PropertyToID("_DistortionStrength");
    private static readonly int VignetteColorId = Shader.PropertyToID("_VignetteColor");
    private static readonly int VignetteIntensityId = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int GridColorId = Shader.PropertyToID("_GridColor");
    private static readonly int GridLineWidthId = Shader.PropertyToID("_GridLineWidth");
    private static readonly int GridScaleId = Shader.PropertyToID("_GridScale");
    private static readonly int EdgeGridWidthId = Shader.PropertyToID("_EdgeGridWidth");
    private static readonly int BreathSpeedId = Shader.PropertyToID("_BreathSpeed");
    private static readonly int BreathAmplitudeId = Shader.PropertyToID("_BreathAmplitude");
    private static readonly int HeartbeatPhaseId = Shader.PropertyToID("_HeartbeatPhase");
    private static readonly int HeartbeatStrengthId = Shader.PropertyToID("_HeartbeatStrength");
    private static readonly int BloomStrengthId = Shader.PropertyToID("_BloomStrength");
    private static readonly int EffectTimeId = Shader.PropertyToID("_EffectTime");
    private static readonly int EdgeGridSoftnessId = Shader.PropertyToID("_EdgeGridSoftness");
    private static readonly int VisualStyleId = Shader.PropertyToID("_VisualStyle");
    private static readonly int GrainStrengthId = Shader.PropertyToID("_GrainStrength");
    private static readonly int ChromaticStrengthId = Shader.PropertyToID("_ChromaticStrength");
    private static readonly int RadialGlowStrengthId = Shader.PropertyToID("_RadialGlowStrength");
    private static readonly int HeatShimmerStrengthId = Shader.PropertyToID("_HeatShimmerStrength");
    private static readonly int SecondaryAccentColorId = Shader.PropertyToID("_SecondaryAccentColor");
    private static readonly int BorderVfxStrengthId = Shader.PropertyToID("_BorderVfxStrength");
    private static readonly int BorderVfxDepthId = Shader.PropertyToID("_BorderVfxDepth");
    private static readonly int BorderVfxEdgeSoftnessId = Shader.PropertyToID("_BorderVfxEdgeSoftness");
    private static readonly int RingBurnStrengthId = Shader.PropertyToID("_RingBurnStrength");
    private static readonly int BorderVfxSpeedId = Shader.PropertyToID("_BorderVfxSpeed");
    private static readonly int BorderVfxHotColorId = Shader.PropertyToID("_BorderVfxHotColor");
    private static readonly int BorderVfxCoreColorId = Shader.PropertyToID("_BorderVfxCoreColor");
    private static readonly int FlameNoiseTexId = Shader.PropertyToID("_FlameNoiseTex");
    private static readonly int FlameNoiseTilingId = Shader.PropertyToID("_FlameNoiseTiling");
    private static readonly int FlameNoiseInwardStretchId = Shader.PropertyToID("_FlameNoiseInwardStretch");
    private static readonly int FlameNoiseInwardScrollId = Shader.PropertyToID("_FlameNoiseInwardScroll");

    private static readonly Vector2 ScreenCenterOrigin = new Vector2(0.5f, 0.5f);

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private Shader effectShader;

    [Header("Profiles")]
    [SerializeField] private FieldDomainEffectProfile verdictProfile;
    [SerializeField] private FieldDomainEffectProfile desperationProfile;
    [SerializeField] private FieldDomainEffectProfile miracleProfile;

    [Header("Timing")]
    [SerializeField] private float expandDuration = 1f;
    [SerializeField] private float contractDuration = 1f;

    [Header("Optional Burst VFX")]
    [SerializeField] private GameObject verdictBurstPrefab;
    [SerializeField] private GameObject desperationBurstPrefab;
    [SerializeField] private GameObject miracleBurstPrefab;

    private bool m_IsRendering;
    private float m_EffectIntensity = 1f;
    private float m_Radius;
    private float m_MaxRadius = 1.5f;
    private Vector2 m_OriginUv = new Vector2(0.5f, 0.5f);
    private Vector2 m_ContractOriginUv = new Vector2(0.5f, 0.5f);
    private FieldDomainVisualPhase m_Phase = FieldDomainVisualPhase.Idle;
    private FieldDomainEffectProfile m_ActiveProfile;
    private EnvironmentType m_ActiveEnvironmentType = EnvironmentType.Gravity;
    private float m_EffectTime;
    private float m_HeartbeatPhase;
    private Coroutine m_SequenceCoroutine;
    private bool m_HasActiveFieldVisual;
    private bool m_VolumeBloomOverridden;
    private float m_DefaultBloomIntensity = 1f;

    private Bloom m_BloomOverride;
    private Material m_EffectMaterial;
    private Texture2D m_DefaultFlameNoiseTexture;

    public bool HasValidEffectMaterial => GetEffectMaterial() != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (globalVolume == null && GameManager.Instance != null)
        {
            globalVolume = GameManager.Instance.globalVolume;
        }

        CacheBloomReference();
        EnsureEffectMaterial();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (effectShader == null)
        {
            effectShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/VFX/FieldDomain/Shaders/FieldDomainEffect.shader");
        }
    }
#endif

    private void OnDestroy()
    {
        ForceStop();

        if (m_EffectMaterial != null)
        {
            Destroy(m_EffectMaterial);
            m_EffectMaterial = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public Material GetEffectMaterial()
    {
        EnsureEffectMaterial();
        return m_EffectMaterial;
    }

    private void EnsureEffectMaterial()
    {
        if (m_EffectMaterial != null)
        {
            return;
        }

        Shader shader = effectShader;
        if (shader == null)
        {
            shader = Shader.Find("TurnChange/FieldDomainEffect");
        }

        if (shader == null)
        {
            Debug.LogError("[FieldDomain] 找不到 FieldDomainEffect Shader。请在 FieldDomainScreenEffectController 的 Effect Shader 槽位拖入 Assets/VFX/FieldDomain/Shaders/FieldDomainEffect.shader，并确认该 Shader 无编译报错。");
            return;
        }

        if (!shader.isSupported)
        {
            Debug.LogError("[FieldDomain] FieldDomainEffect shader 在当前平台不受支持。");
            return;
        }

        m_EffectMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "FieldDomainEffect(Runtime)"
        };
    }

    private void Update()
    {
        if (!m_IsRendering || m_ActiveProfile == null)
        {
            return;
        }

        m_EffectTime += Time.deltaTime;

        if (m_ActiveProfile.heartbeatBpm > 0.01f)
        {
            m_HeartbeatPhase += Time.deltaTime * m_ActiveProfile.heartbeatBpm * Mathf.PI * 2f / 60f;
        }

        if (m_Phase == FieldDomainVisualPhase.Active && m_ActiveProfile.volumeBloomIntensity > 0.01f)
        {
            ApplyVolumeBloom(m_ActiveProfile.volumeBloomIntensity);
        }
    }

    public FieldDomainVisualPhase CurrentPhase => m_Phase;
    public bool HasActiveFieldVisual => m_HasActiveFieldVisual;
    public float CurrentRadius => m_Radius;
    public EnvironmentType ActiveEnvironmentType => m_ActiveEnvironmentType;

    public IEnumerator PlayExpand(EnvironmentType environmentType, Transform origin, float duration = -1f, bool skipOpeningIntroWait = false)
    {
        if (duration <= 0f)
        {
            duration = expandDuration;
        }

        if (!skipOpeningIntroWait)
        {
            yield return WaitForOpeningIntroIfNeeded();
        }

        if (m_HasActiveFieldVisual && m_Phase == FieldDomainVisualPhase.Active)
        {
            yield return PlayContractInternal(GetEffectOrigin(), contractDuration);
        }

        FieldDomainEffectProfile profile = ResolveProfile(environmentType);
        if (profile == null)
        {
            yield break;
        }

        m_ActiveEnvironmentType = environmentType;
        m_ActiveProfile = profile;
        Vector2 effectOrigin = GetEffectOrigin();
        m_OriginUv = effectOrigin;
        m_ContractOriginUv = effectOrigin;
        m_MaxRadius = ComputeMaxRadius(effectOrigin);
        m_EffectTime = 0f;
        m_HeartbeatPhase = 0f;
        m_EffectIntensity = 1f;
        m_IsRendering = true;
        m_Phase = FieldDomainVisualPhase.Expanding;
        m_HasActiveFieldVisual = true;

        SpawnBurstPrefab(environmentType, origin);
        SetExternalVignetteSuppressed(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.0001f));
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            m_Radius = Mathf.Lerp(0f, m_MaxRadius, eased);
            elapsed += Time.deltaTime;
            yield return null;
        }

        m_Radius = m_MaxRadius;
        m_Phase = FieldDomainVisualPhase.Active;
        m_ContractOriginUv = GetEffectOrigin();
    }

    /// <summary>快速预览：扩散 → 保持激活 → 收缩，不依赖 EnvironmentManager。</summary>
    public IEnumerator PlayPreviewCycle(EnvironmentType environmentType, Transform origin, float activeHoldDuration = 2f, bool skipOpeningIntroWait = true)
    {
        yield return PlayExpand(environmentType, origin, -1f, skipOpeningIntroWait);

        if (!m_HasActiveFieldVisual)
        {
            yield break;
        }

        if (activeHoldDuration > 0f)
        {
            yield return new WaitForSeconds(activeHoldDuration);
        }

        yield return PlayContract(origin);
    }

    /// <summary>持续预览：扩散后保持在 Active，不自动收缩；由调用方触发 PlayContract / ForceStop。</summary>
    public IEnumerator PlaySustainedPreview(EnvironmentType environmentType, Transform origin, bool skipOpeningIntroWait = true)
    {
        yield return PlayExpand(environmentType, origin, -1f, skipOpeningIntroWait);
    }

    public void NotifyEnvironmentRegistered(EnvironmentType environmentType, UnitCombatant applier)
    {
        if (!m_HasActiveFieldVisual)
        {
            return;
        }

        m_ActiveEnvironmentType = environmentType;
        m_ActiveProfile = ResolveProfile(environmentType);
        m_Phase = FieldDomainVisualPhase.Active;
        m_Radius = m_MaxRadius;
    }

    public void NotifyEnvironmentUnregistered(EnvironmentType environmentType, UnitCombatant applier)
    {
        if (!m_HasActiveFieldVisual || m_ActiveEnvironmentType != environmentType)
        {
            return;
        }

        if (m_SequenceCoroutine != null)
        {
            StopCoroutine(m_SequenceCoroutine);
        }

        m_SequenceCoroutine = StartCoroutine(PlayContractRoutine(GetEffectOrigin(), contractDuration));
    }

    public IEnumerator PlayContract(Transform origin, float duration = -1f)
    {
        yield return PlayContractInternal(GetEffectOrigin(), duration > 0f ? duration : contractDuration);
    }

    public void ForceStop()
    {
        if (m_SequenceCoroutine != null)
        {
            StopCoroutine(m_SequenceCoroutine);
            m_SequenceCoroutine = null;
        }

        m_IsRendering = false;
        m_EffectIntensity = 0f;
        m_Phase = FieldDomainVisualPhase.Idle;
        m_HasActiveFieldVisual = false;
        m_ActiveProfile = null;
        RestoreVolumeBloom();
        SetExternalVignetteSuppressed(false);
    }

    public void ApplyToMaterial(Material material)
    {
        if (material == null || m_ActiveProfile == null)
        {
            return;
        }

        ApplyShaderUniforms(
            (id, v) => material.SetVector(id, v),
            (id, f) => material.SetFloat(id, f),
            (id, c) => material.SetColor(id, c));
        ApplyFlameNoiseTexture(material.SetTexture);
    }

    public void ApplyToPropertyBlock(MaterialPropertyBlock block)
    {
        if (block == null || m_ActiveProfile == null)
        {
            return;
        }

        ApplyShaderUniforms(
            (id, v) => block.SetVector(id, v),
            (id, f) => block.SetFloat(id, f),
            (id, c) => block.SetColor(id, c));
        ApplyFlameNoiseTexture(block.SetTexture);
    }

    private void ApplyFlameNoiseTexture(System.Action<int, Texture> setTexture)
    {
        if (setTexture == null || m_ActiveProfile == null)
        {
            return;
        }

        Texture2D flameTexture = m_ActiveProfile.flameNoiseTexture;
        if (flameTexture == null && m_ActiveProfile.visualStyle == FieldDomainVisualStyle.VerdictFlame)
        {
            flameTexture = GetDefaultFlameNoiseTexture();
        }

        if (flameTexture != null)
        {
            setTexture(FlameNoiseTexId, flameTexture);
        }
    }

    private Texture2D GetDefaultFlameNoiseTexture()
    {
        if (m_DefaultFlameNoiseTexture != null)
        {
            return m_DefaultFlameNoiseTexture;
        }

        if (verdictProfile != null && verdictProfile.flameNoiseTexture != null)
        {
            m_DefaultFlameNoiseTexture = verdictProfile.flameNoiseTexture;
            return m_DefaultFlameNoiseTexture;
        }

        m_DefaultFlameNoiseTexture = FieldDomainRuntimeResources.GetFlameNoiseTexture();
        return m_DefaultFlameNoiseTexture;
    }

    private void ApplyShaderUniforms(
        System.Action<int, Vector4> setVector,
        System.Action<int, float> setFloat,
        System.Action<int, Color> setColor)
    {
        float shaderPhase = m_Phase switch
        {
            FieldDomainVisualPhase.Expanding => 0f,
            FieldDomainVisualPhase.Active => 1f,
            FieldDomainVisualPhase.Contracting => 2f,
            _ => 0f
        };

        setVector(OriginId, new Vector4(m_OriginUv.x, m_OriginUv.y, 0f, 0f));
        setFloat(RadiusId, m_Radius);
        setFloat(MaxRadiusId, m_MaxRadius);
        setFloat(WaveWidthId, m_ActiveProfile.waveWidth);
        setFloat(PhaseId, shaderPhase);
        setFloat(IntensityId, m_EffectIntensity);
        setColor(TintColorId, m_ActiveProfile.tint);
        setFloat(SaturationId, m_ActiveProfile.saturation);
        setFloat(ContrastId, m_ActiveProfile.contrast);
        setFloat(ExposureId, m_ActiveProfile.exposure);
        setFloat(DistortionStrengthId, m_ActiveProfile.distortionStrength);
        setColor(VignetteColorId, m_ActiveProfile.vignetteColor);
        setFloat(VignetteIntensityId, m_ActiveProfile.vignetteIntensity);
        setColor(GridColorId, m_ActiveProfile.gridColor);
        setFloat(GridLineWidthId, m_ActiveProfile.gridLineWidth);
        setFloat(GridScaleId, m_ActiveProfile.gridScale);
        setFloat(EdgeGridWidthId, m_ActiveProfile.edgeGridWidth);
        setFloat(EdgeGridSoftnessId, m_ActiveProfile.edgeGridSoftness);
        setFloat(BreathSpeedId, m_ActiveProfile.breathSpeed);
        setFloat(BreathAmplitudeId, m_ActiveProfile.breathAmplitude);
        setFloat(HeartbeatPhaseId, m_HeartbeatPhase);
        setFloat(HeartbeatStrengthId, m_ActiveProfile.heartbeatStrength);
        setFloat(BloomStrengthId, m_ActiveProfile.bloomStrength);
        setFloat(EffectTimeId, m_EffectTime);
        setFloat(VisualStyleId, (float)m_ActiveProfile.visualStyle);
        setFloat(GrainStrengthId, m_ActiveProfile.grainStrength);
        setFloat(ChromaticStrengthId, m_ActiveProfile.chromaticStrength);
        setFloat(RadialGlowStrengthId, m_ActiveProfile.radialGlowStrength);
        setFloat(HeatShimmerStrengthId, m_ActiveProfile.heatShimmerStrength);
        setColor(SecondaryAccentColorId, m_ActiveProfile.secondaryAccentColor);
        setFloat(BorderVfxStrengthId, m_ActiveProfile.borderVfxStrength);
        setFloat(BorderVfxDepthId, m_ActiveProfile.borderVfxDepth);
        setFloat(BorderVfxEdgeSoftnessId, m_ActiveProfile.borderVfxEdgeSoftness);
        setFloat(RingBurnStrengthId, m_ActiveProfile.ringBurnStrength);
        setFloat(BorderVfxSpeedId, m_ActiveProfile.borderVfxSpeed);
        setColor(BorderVfxHotColorId, m_ActiveProfile.borderVfxHotColor);
        setColor(BorderVfxCoreColorId, m_ActiveProfile.borderVfxCoreColor);
        setVector(FlameNoiseTilingId, new Vector4(
            m_ActiveProfile.flameNoiseTiling.x,
            m_ActiveProfile.flameNoiseTiling.y,
            0f,
            0f));
        setFloat(FlameNoiseInwardStretchId, m_ActiveProfile.flameNoiseInwardStretch);
        setFloat(FlameNoiseInwardScrollId, m_ActiveProfile.flameNoiseInwardScroll);
    }

    private static Vector2 GetEffectOrigin() => ScreenCenterOrigin;

    private IEnumerator PlayContractRoutine(Vector2 originUv, float duration)
    {
        yield return PlayContractInternal(originUv, duration);
        m_SequenceCoroutine = null;
    }

    private IEnumerator PlayContractInternal(Vector2 originUv, float duration)
    {
        if (!m_HasActiveFieldVisual)
        {
            yield break;
        }

        m_Phase = FieldDomainVisualPhase.Contracting;
        m_OriginUv = originUv;
        m_ContractOriginUv = originUv;
        m_IsRendering = true;
        m_Radius = m_MaxRadius;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.0001f));
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            m_Radius = Mathf.Lerp(m_MaxRadius, 0f, eased);
            m_EffectIntensity = 1f - eased * 0.15f;
            elapsed += Time.deltaTime;
            yield return null;
        }

        ForceStop();
    }

    private IEnumerator WaitForOpeningIntroIfNeeded()
    {
        if (CinemachineCameraManager.Instance == null)
        {
            yield break;
        }

        yield return new WaitUntil(() =>
            CinemachineCameraManager.Instance == null || !CinemachineCameraManager.Instance.isOP);
    }

    private FieldDomainEffectProfile ResolveProfile(EnvironmentType environmentType)
    {
        FieldDomainEffectProfile profile = environmentType switch
        {
            EnvironmentType.Gravity => verdictProfile,
            EnvironmentType.DesperationField => desperationProfile,
            EnvironmentType.MiracleField => miracleProfile,
            _ => null
        };

        return profile != null ? profile : FieldDomainEffectProfile.CreateRuntimePreset(environmentType);
    }

    private static float ComputeMaxRadius(Vector2 originUv)
    {
        Vector2[] corners =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        float maxDistance = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 delta = corners[i] - originUv;
            delta.x *= Screen.width / (float)Mathf.Max(Screen.height, 1);
            maxDistance = Mathf.Max(maxDistance, delta.magnitude);
        }

        return maxDistance + 0.08f;
    }

    private void SpawnBurstPrefab(EnvironmentType environmentType, Transform origin)
    {
        GameObject prefab = environmentType switch
        {
            EnvironmentType.Gravity => verdictBurstPrefab,
            EnvironmentType.DesperationField => desperationBurstPrefab,
            EnvironmentType.MiracleField => miracleBurstPrefab,
            _ => null
        };

        if (prefab == null || origin == null)
        {
            return;
        }

        Instantiate(prefab, origin.position, Quaternion.identity);
    }

    private void CacheBloomReference()
    {
        if (globalVolume == null || globalVolume.profile == null)
        {
            return;
        }

        if (globalVolume.profile.TryGet(out Bloom bloom))
        {
            m_BloomOverride = bloom;
            m_DefaultBloomIntensity = bloom.intensity.value;
        }
    }

    private void ApplyVolumeBloom(float targetIntensity)
    {
        if (m_BloomOverride == null)
        {
            CacheBloomReference();
        }

        if (m_BloomOverride == null)
        {
            return;
        }

        m_VolumeBloomOverridden = true;
        m_BloomOverride.intensity.Override(Mathf.Lerp(m_DefaultBloomIntensity, targetIntensity, 0.85f));
    }

    private void RestoreVolumeBloom()
    {
        if (!m_VolumeBloomOverridden || m_BloomOverride == null)
        {
            return;
        }

        m_BloomOverride.intensity.Override(m_DefaultBloomIntensity);
        m_VolumeBloomOverridden = false;
    }

    private static void SetExternalVignetteSuppressed(bool suppressed)
    {
        if (CinemachineCameraManager.Instance == null)
        {
            return;
        }

        CinemachineCameraManager.Instance.SetFieldDomainVignetteSuppressed(suppressed);
    }
}

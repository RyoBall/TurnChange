using UnityEngine;

public enum FieldDomainVisualStyle
{
    VerdictFlame = 0,
    DesperationPulse = 1,
    MiracleRadiance = 2
}

[CreateAssetMenu(fileName = "FieldDomainEffectProfile", menuName = "TurnChange/Field Domain Effect Profile")]
public class FieldDomainEffectProfile : ScriptableObject
{
    [Header("Style")]
    public FieldDomainVisualStyle visualStyle = FieldDomainVisualStyle.VerdictFlame;
    [Range(0f, 1f)] public float grainStrength;
    [Range(0f, 1f)] public float chromaticStrength;
    [Range(0f, 1f)] public float radialGlowStrength;
    [Range(0f, 2f)] public float heatShimmerStrength;
    public Color secondaryAccentColor = new Color(1f, 0.75f, 0.2f, 1f);

    [Header("Border VFX")]
    [Range(0f, 1.5f)] public float borderVfxStrength = 0.6f;
    [Range(0.02f, 0.35f)] public float borderVfxDepth = 0.12f;
    [Tooltip("重裁/绝境：越大火焰内缘越柔和。奇迹：玻璃内侧衰减幂次，越大过渡越长（建议 2.5~4）")]
    [Range(0.25f, 4f)] public float borderVfxEdgeSoftness = 0.72f;
    [Range(0f, 1.5f)] public float ringBurnStrength;
    [Range(0f, 4f)] public float borderVfxSpeed = 1.2f;
    public Color borderVfxHotColor = new Color(1f, 0.48f, 0.08f, 1f);
    public Color borderVfxCoreColor = new Color(0.75f, 0.1f, 0.02f, 1f);
    public Texture2D flameNoiseTexture;
    [Tooltip("重裁火焰：X/Y=噪声 Tiling。奇迹焦散：X=沿边密度，Y=向内密度")]
    public Vector2 flameNoiseTiling = new Vector2(5f, 11f);
    [Range(0.5f, 3f)] public float flameNoiseInwardStretch = 2f;
    [Tooltip("向屏内流动的滚动倍率（× borderVfxSpeed）")]
    [Range(0.1f, 2f)] public float flameNoiseInwardScroll = 0.4f;

    [Header("Color Grading")]
    public Color tint = new Color(0.23f, 0.04f, 0.04f, 0.65f);
    [Range(0f, 2f)] public float saturation = 0.75f;
    [Range(0f, 3f)] public float contrast = 1.1f;
    [Range(0.5f, 2f)] public float exposure = 1f;

    [Header("Distortion")]
    [Range(0f, 2f)] public float distortionStrength = 0f;

    [Header("Vignette")]
    public Color vignetteColor = new Color(0.15f, 0f, 0f, 1f);
    [Range(0f, 1f)] public float vignetteIntensity = 0f;

    [Header("Grid")]
    public Color gridColor = new Color(1f, 0.35f, 0.15f, 1f);
    [Range(0.1f, 10f)] public float gridLineWidth = 3f;
    [Range(0.1f, 5f)] public float gridScale = 1f;
    [Range(0.001f, 0.2f)] public float waveWidth = 0.035f;
    [Range(0.001f, 0.15f)] public float edgeGridWidth = 0.025f;
    [Range(0.001f, 0.08f)] public float edgeGridSoftness = 0.02f;

    [Header("Breathing")]
    [Range(0f, 5f)] public float breathSpeed = 1.2f;
    [Range(0f, 1f)] public float breathAmplitude = 0.25f;

    [Header("Heartbeat")]
    [Range(0f, 120f)] public float heartbeatBpm = 0f;
    [Range(0f, 1f)] public float heartbeatStrength = 0f;

    [Header("Bloom")]
    [Range(0f, 2f)] public float bloomStrength = 0f;
    [Range(0f, 5f)] public float volumeBloomIntensity = 0f;

    public static FieldDomainEffectProfile CreateRuntimePreset(EnvironmentType environmentType)
    {
        FieldDomainEffectProfile profile = CreateInstance<FieldDomainEffectProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        ApplyPreset(profile, environmentType);
        return profile;
    }

    public static void ApplyPreset(FieldDomainEffectProfile profile, EnvironmentType environmentType)
    {
        if (profile == null)
        {
            return;
        }

        switch (environmentType)
        {
            case EnvironmentType.Gravity:
                ConfigureVerdictPreset(profile);
                break;
            case EnvironmentType.DesperationField:
                ConfigureDesperationPreset(profile);
                break;
            case EnvironmentType.MiracleField:
                ConfigureMiraclePreset(profile);
                break;
        }
    }

    private static void ConfigureVerdictPreset(FieldDomainEffectProfile profile)
    {
        profile.visualStyle = FieldDomainVisualStyle.VerdictFlame;
        profile.grainStrength = 0f;
        profile.chromaticStrength = 0f;
        profile.radialGlowStrength = 0f;
        profile.heatShimmerStrength = 0.15f;
        profile.secondaryAccentColor = new Color(1f, 0.78f, 0.22f, 1f);
        profile.tint = new Color(0.28f, 0.08f, 0.02f, 0.38f);
        profile.saturation = 0.7f;
        profile.contrast = 1.08f;
        profile.exposure = 0.96f;
        profile.distortionStrength = 0f;
        profile.vignetteColor = new Color(0.22f, 0.05f, 0.01f, 1f);
        profile.vignetteIntensity = 0.14f;
        profile.gridColor = new Color(1f, 0.55f, 0.15f, 1f);
        profile.gridLineWidth = 2.6f;
        profile.gridScale = 0.95f;
        profile.waveWidth = 0.042f;
        profile.edgeGridWidth = 0.026f;
        profile.edgeGridSoftness = 0.018f;
        profile.breathSpeed = 1.2f;
        profile.breathAmplitude = 0.14f;
        profile.heartbeatBpm = 0f;
        profile.heartbeatStrength = 0f;
        profile.bloomStrength = 0.08f;
        profile.volumeBloomIntensity = 0f;
        profile.borderVfxStrength = 0.62f;
        profile.borderVfxDepth = 0.12f;
        profile.borderVfxEdgeSoftness = 0.85f;
        profile.ringBurnStrength = 0.42f;
        profile.borderVfxSpeed = 1.55f;
        profile.borderVfxHotColor = new Color(1f, 0.48f, 0.08f, 1f);
        profile.borderVfxCoreColor = new Color(0.75f, 0.1f, 0.02f, 1f);
        profile.flameNoiseTiling = new Vector2(4.5f, 10f);
        profile.flameNoiseInwardStretch = 1.85f;
        profile.flameNoiseInwardScroll = 0.28f;
        AssignDefaultFlameNoise(profile);
    }

    private static void ConfigureDesperationPreset(FieldDomainEffectProfile profile)
    {
        profile.visualStyle = FieldDomainVisualStyle.DesperationPulse;
        profile.grainStrength = 0.16f;
        profile.chromaticStrength = 0.22f;
        profile.radialGlowStrength = 0f;
        profile.heatShimmerStrength = 0f;
        profile.secondaryAccentColor = new Color(0.55f, 0.05f, 0.08f, 1f);
        profile.tint = new Color(0.42f, 0.12f, 0.1f, 0.14f);
        profile.saturation = 0.72f;
        profile.contrast = 1.05f;
        profile.exposure = 1.0f;
        profile.distortionStrength = 0f;
        profile.vignetteColor = new Color(0.32f, 0.06f, 0.08f, 1f);
        profile.vignetteIntensity = 0.22f;
        profile.gridColor = new Color(0.7f, 0.08f, 0.12f, 0.82f);
        profile.gridLineWidth = 2.1f;
        profile.gridScale = 1.05f;
        profile.waveWidth = 0.028f;
        profile.edgeGridWidth = 0.022f;
        profile.edgeGridSoftness = 0.015f;
        profile.breathSpeed = 0.9f;
        profile.breathAmplitude = 0.09f;
        profile.heartbeatBpm = 60f;
        profile.heartbeatStrength = 0.48f;
        profile.bloomStrength = 0f;
        profile.volumeBloomIntensity = 0f;
        profile.borderVfxStrength = 0.5f;
        profile.borderVfxDepth = 0.16f;
        profile.borderVfxEdgeSoftness = 1.55f;
        profile.ringBurnStrength = 0f;
        profile.borderVfxSpeed = 0.85f;
        profile.borderVfxHotColor = new Color(0.58f, 0.22f, 0.12f, 1f);
        profile.borderVfxCoreColor = new Color(0.28f, 0.08f, 0.04f, 1f);
        profile.flameNoiseTiling = new Vector2(5f, 11f);
        profile.flameNoiseInwardStretch = 1.9f;
        profile.flameNoiseInwardScroll = 0.5f;
        AssignDefaultFlameNoise(profile);
    }

    private static void ConfigureMiraclePreset(FieldDomainEffectProfile profile)
    {
        profile.visualStyle = FieldDomainVisualStyle.MiracleRadiance;
        profile.grainStrength = 0f;
        profile.chromaticStrength = 0.35f;
        profile.radialGlowStrength = 0.1f;
        profile.heatShimmerStrength = 0f;
        profile.secondaryAccentColor = new Color(0.92f, 1f, 0.88f, 1f);
        profile.tint = new Color(0.5f, 0.95f, 0.7f, 0.14f);
        profile.saturation = 1.05f;
        profile.contrast = 0.94f;
        profile.exposure = 1.1f;
        profile.distortionStrength = 0f;
        profile.vignetteColor = new Color(0.05f, 0.14f, 0.22f, 1f);
        profile.vignetteIntensity = 0.06f;
        profile.gridColor = new Color(0.82f, 0.98f, 0.92f, 0.75f);
        profile.gridLineWidth = 1.6f;
        profile.gridScale = 0.85f;
        profile.waveWidth = 0.055f;
        profile.edgeGridWidth = 0.035f;
        profile.edgeGridSoftness = 0.04f;
        profile.breathSpeed = 0.48f;
        profile.breathAmplitude = 0.32f;
        profile.heartbeatBpm = 0f;
        profile.heartbeatStrength = 0f;
        profile.bloomStrength = 0.5f;
        profile.volumeBloomIntensity = 0.85f;
        profile.borderVfxStrength = 0.8f;
        profile.borderVfxDepth = 0.22f;
        profile.borderVfxEdgeSoftness = 3f;
        profile.ringBurnStrength = 0f;
        profile.borderVfxSpeed = 1.1f;
        profile.borderVfxHotColor = new Color(1f, 0.52f, 0.88f, 1f);
        profile.borderVfxCoreColor = new Color(0.35f, 0.82f, 1f, 1f);
        profile.flameNoiseTiling = new Vector2(4.5f, 6f);
    }

    private static void AssignDefaultFlameNoise(FieldDomainEffectProfile profile)
    {
        if (profile == null || profile.flameNoiseTexture != null)
        {
            return;
        }

        profile.flameNoiseTexture = FieldDomainRuntimeResources.GetFlameNoiseTexture();
    }
}

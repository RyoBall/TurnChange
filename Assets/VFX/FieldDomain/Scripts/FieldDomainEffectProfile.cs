using UnityEngine;

[CreateAssetMenu(fileName = "FieldDomainEffectProfile", menuName = "TurnChange/Field Domain Effect Profile")]
public class FieldDomainEffectProfile : ScriptableObject
{
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

        switch (environmentType)
        {
            case EnvironmentType.Gravity:
                profile.tint = new Color(0.23f, 0.04f, 0.04f, 0.65f);
                profile.saturation = 0.72f;
                profile.contrast = 1.15f;
                profile.exposure = 0.95f;
                profile.distortionStrength = 0.2f;
                profile.vignetteColor = new Color(0.2f, 0.02f, 0.02f, 1f);
                profile.vignetteIntensity = 0.25f;
                profile.gridColor = new Color(1f, 0.35f, 0.12f, 1f);
                profile.gridLineWidth = 3.5f;
                profile.gridScale = 1.1f;
                profile.waveWidth = 0.04f;
                profile.edgeGridWidth = 0.028f;
                profile.breathSpeed = 1.4f;
                profile.breathAmplitude = 0.2f;
                profile.bloomStrength = 0.1f;
                break;

            case EnvironmentType.DesperationField:
                profile.tint = new Color(0.08f, 0.02f, 0.02f, 0.35f);
                profile.saturation = 0.48f;
                profile.contrast = 1.28f;
                profile.exposure = 1.05f;
                profile.distortionStrength = 0.15f;
                profile.vignetteColor = new Color(0.35f, 0.02f, 0.02f, 1f);
                profile.vignetteIntensity = 0.65f;
                profile.gridColor = new Color(0.85f, 0.1f, 0.1f, 0.9f);
                profile.gridLineWidth = 2.8f;
                profile.gridScale = 1.3f;
                profile.waveWidth = 0.035f;
                profile.edgeGridWidth = 0.03f;
                profile.breathSpeed = 1.1f;
                profile.breathAmplitude = 0.15f;
                profile.heartbeatBpm = 72f;
                profile.heartbeatStrength = 0.85f;
                profile.bloomStrength = 0f;
                break;

            case EnvironmentType.MiracleField:
                profile.tint = new Color(0.55f, 0.95f, 0.72f, 0.5f);
                profile.saturation = 1.05f;
                profile.contrast = 0.95f;
                profile.exposure = 1.12f;
                profile.distortionStrength = 0.05f;
                profile.vignetteColor = new Color(0.05f, 0.15f, 0.25f, 1f);
                profile.vignetteIntensity = 0.12f;
                profile.gridColor = new Color(0.85f, 0.95f, 1f, 0.95f);
                profile.gridLineWidth = 2.2f;
                profile.gridScale = 0.9f;
                profile.waveWidth = 0.045f;
                profile.edgeGridWidth = 0.022f;
                profile.breathSpeed = 0.55f;
                profile.breathAmplitude = 0.35f;
                profile.heartbeatBpm = 0f;
                profile.heartbeatStrength = 0f;
                profile.bloomStrength = 0.75f;
                profile.volumeBloomIntensity = 2.2f;
                break;
        }

        return profile;
    }
}

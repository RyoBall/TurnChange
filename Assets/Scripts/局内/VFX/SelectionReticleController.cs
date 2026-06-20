using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class SelectionReticleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rotator;
    [SerializeField] private SpriteRenderer innerRingRenderer;
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private SpriteRenderer glowRenderer;
    [SerializeField] private SpriteRenderer outerRingRenderer;
    [SerializeField] private SpriteRenderer[] markerRenderers;

    [Header("Layout")]
    [SerializeField] private float innerRingScale = 0.5f;
    [SerializeField] private float fillScale = 1.05f;
    [SerializeField] private float glowScale = 0.75f;
    [SerializeField] private float outerRingScale = 0.92f;
    [SerializeField] private float markerOrbitRadius = 0.92f;
    [SerializeField] private float markerScale = 0.15f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private Vector3 planeEuler = Vector3.zero;
    [SerializeField] private bool editorPreviewVisible = true;

    [Header("Animation")]
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private bool enableBrightnessBreath = true;
    [SerializeField, Min(0.15f)] private float brightnessBreathPeriod = 1.2f;
    [SerializeField, Range(0f, 1.5f)] private float brightnessMin = 0.85f;
    [SerializeField, Range(0f, 1.5f)] private float brightnessMax = 1.15f;

    [Header("Colors")]
    [SerializeField] private Color lineColor = new Color(1f, 0.95f, 0.9f, 1f);
    [SerializeField] private Color fillColor = new Color(1f, 0.15f, 0.05f, 0.35f);
    [SerializeField] private Color glowColor = new Color(1f, 0.45f, 0.1f, 1f);
    [SerializeField, Range(0f, 4f)] private float glowIntensity = 1.2f;
    [SerializeField, Range(0.25f, 4f)] private float fillPower = 1.4f;
    [SerializeField, Range(0f, 2f)] private float centerBoost = 0.35f;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private bool followTargetTransform;

    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
    private static readonly int InnerRadiusId = Shader.PropertyToID("_InnerRadius");
    private static readonly int OuterRadiusId = Shader.PropertyToID("_OuterRadius");
    private static readonly int FillPowerId = Shader.PropertyToID("_FillPower");
    private static readonly int CenterBoostId = Shader.PropertyToID("_CenterBoost");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_Intensity");

    private MaterialPropertyBlock m_PropertyBlock;
    private float m_BreathPhase;
    private bool m_IsVisible;
    private bool m_IsLockedVisible;

    public bool IsVisible => m_IsVisible;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (followTarget == null && transform.parent != null)
            {
                SetFollowTarget(transform.parent, true);
            }

            ApplyVisualSettings();
            SetVisibleInternal(false, true);
            return;
        }

        RefreshEditorPreview();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying && followTargetTransform && followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
        }

        if (!ShouldAnimate())
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;

        if (rotator != null && Mathf.Abs(rotationSpeed) > 0.01f)
        {
            rotator.Rotate(0f, 0f, rotationSpeed * deltaTime, Space.Self);
        }

        if (enableBrightnessBreath)
        {
            m_BreathPhase += deltaTime * (Mathf.PI * 2f / Mathf.Max(brightnessBreathPeriod, 0.15f));
            float breath = Mathf.Lerp(brightnessMin, brightnessMax, (Mathf.Sin(m_BreathPhase) + 1f) * 0.5f);
            ApplyLineBrightness(breath);
        }
    }

    private bool ShouldAnimate()
    {
        if (Application.isPlaying)
        {
            return m_IsVisible;
        }

        return editorPreviewVisible && HasAssignedSprites();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyVisualSettings();
        ApplyLayout();
        RefreshEditorPreview();
    }
#endif

    public void RefreshEditorPreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (!editorPreviewVisible || !HasAssignedSprites())
        {
            SetVisibleInternal(false, true);
            return;
        }

        SetVisibleInternal(true, true);
    }

    private bool HasAssignedSprites()
    {
        return innerRingRenderer != null && innerRingRenderer.sprite != null;
    }

    public void SetFollowTarget(Transform target, bool followTransform = true)
    {
        followTarget = target;
        followTargetTransform = followTransform;
        if (followTargetTransform && followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
        }
    }

    public void Show()
    {
        SetVisibleInternal(true, false);
    }

    public void Hide()
    {
        if (m_IsLockedVisible)
        {
            return;
        }

        SetVisibleInternal(false, false);
    }

    public void SetLockedVisible(bool locked)
    {
        m_IsLockedVisible = locked;
        if (locked)
        {
            Show();
            return;
        }

        Hide();
    }

    public void ConfigureColors(Color newLineColor, Color newFillColor, Color newGlowColor)
    {
        lineColor = newLineColor;
        fillColor = newFillColor;
        glowColor = newGlowColor;
        ApplyVisualSettings();
    }

    public void ApplyVisualSettings()
    {
        ApplyLineColor(lineColor);
        ApplyFillMaterialSettings();
        ApplyGlowMaterialSettings();
    }

    public void ApplyLayout()
    {
        transform.localRotation = Quaternion.Euler(planeEuler);

        if (innerRingRenderer != null)
        {
            innerRingRenderer.transform.localScale = Vector3.one * innerRingScale;
        }

        if (fillRenderer != null)
        {
            fillRenderer.transform.localScale = Vector3.one * fillScale;
        }

        if (glowRenderer != null)
        {
            glowRenderer.transform.localScale = Vector3.one * glowScale;
        }

        if (outerRingRenderer != null)
        {
            outerRingRenderer.transform.localScale = Vector3.one * outerRingScale;
        }

        if (markerRenderers == null)
        {
            return;
        }

        float[] markerAngles =
        {
            0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f
        };

        for (int i = 0; i < markerRenderers.Length; i++)
        {
            SpriteRenderer marker = markerRenderers[i];
            if (marker == null)
            {
                continue;
            }

            float angleRadians = markerAngles[i % markerAngles.Length] * Mathf.Deg2Rad;
            Vector3 localPosition = new Vector3(
                Mathf.Cos(angleRadians) * markerOrbitRadius,
                Mathf.Sin(angleRadians) * markerOrbitRadius,
                0f);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = Vector3.one * markerScale;
            float markerAngle = markerAngles[i % markerAngles.Length];
            bool isTriangleMarker = marker.name.Contains("Triangle");
            marker.transform.localRotation = isTriangleMarker
                ? Quaternion.Euler(0f, 0f, markerAngle + 90f)
                : Quaternion.identity;
        }
    }

    private void ApplyLineColor(Color color)
    {
        SetRendererColor(innerRingRenderer, color);
        SetRendererColor(outerRingRenderer, color);
        if (markerRenderers == null)
        {
            return;
        }

        for (int i = 0; i < markerRenderers.Length; i++)
        {
            SetRendererColor(markerRenderers[i], color);
        }
    }

    private void ApplyLineBrightness(float brightness)
    {
        Color color = lineColor * brightness;
        color.a = lineColor.a;
        ApplyLineColor(color);
    }

    private void ApplyFillMaterialSettings()
    {
        if (fillRenderer == null || fillRenderer.sharedMaterial == null)
        {
            return;
        }

        float innerRadiusNormalized = Mathf.Clamp01(innerRingScale / Mathf.Max(fillScale, 0.001f) * 0.5f);
        float outerRadiusNormalized = Mathf.Clamp01(outerRingScale / Mathf.Max(fillScale, 0.001f) * 0.5f);

        if (Application.isPlaying)
        {
            MaterialPropertyBlock block = GetPropertyBlock(fillRenderer);
            block.SetColor(FillColorId, fillColor);
            block.SetFloat(InnerRadiusId, innerRadiusNormalized);
            block.SetFloat(OuterRadiusId, outerRadiusNormalized);
            block.SetFloat(FillPowerId, fillPower);
            block.SetFloat(CenterBoostId, centerBoost);
            fillRenderer.SetPropertyBlock(block);
            return;
        }

        Material material = fillRenderer.sharedMaterial;
        material.SetColor(FillColorId, fillColor);
        material.SetFloat(InnerRadiusId, innerRadiusNormalized);
        material.SetFloat(OuterRadiusId, outerRadiusNormalized);
        material.SetFloat(FillPowerId, fillPower);
        material.SetFloat(CenterBoostId, centerBoost);
    }

    private void ApplyGlowMaterialSettings()
    {
        if (glowRenderer == null || glowRenderer.sharedMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            MaterialPropertyBlock block = GetPropertyBlock(glowRenderer);
            block.SetColor(GlowColorId, glowColor);
            block.SetFloat(GlowIntensityId, glowIntensity);
            glowRenderer.SetPropertyBlock(block);
            return;
        }

        Material material = glowRenderer.sharedMaterial;
        material.SetColor(GlowColorId, glowColor);
        material.SetFloat(GlowIntensityId, glowIntensity);
    }

    private MaterialPropertyBlock GetPropertyBlock(SpriteRenderer renderer)
    {
        m_PropertyBlock ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(m_PropertyBlock);
        return m_PropertyBlock;
    }

    private static void SetRendererColor(SpriteRenderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.color = color;
    }

    private void SetVisibleInternal(bool visible, bool force)
    {
        if (!force && m_IsVisible == visible)
        {
            return;
        }

        m_IsVisible = visible;
        SetRendererEnabled(innerRingRenderer, visible);
        SetRendererEnabled(fillRenderer, visible);
        SetRendererEnabled(glowRenderer, visible);
        SetRendererEnabled(outerRingRenderer, visible);
        if (markerRenderers != null)
        {
            for (int i = 0; i < markerRenderers.Length; i++)
            {
                SetRendererEnabled(markerRenderers[i], visible);
            }
        }
    }

    private static void SetRendererEnabled(SpriteRenderer renderer, bool enabled)
    {
        if (renderer != null)
        {
            renderer.enabled = enabled;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// UI 环境漂浮粒子：使用 UI Image 池实现，保证在 Canvas 中可见；Inspector 可配置漂移、密度与外观。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("UI/Ambient Floating Particles")]
[ExecuteAlways]
public class AmbientFloatingParticles : MonoBehaviour
{
    private sealed class Floater
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Velocity;
        public float Lifetime;
        public float Age;
        public float PeakAlpha;
        public bool Active;
    }

    [Header("发射密度")]
    [SerializeField, Min(0f), Tooltip("每秒发射粒子数")]
    private float m_emissionRate = 8f;

    [SerializeField, Min(1), Tooltip("同屏粒子数量上限")]
    private int m_maxParticles = 30;

    [Header("漂移方向")]
    [SerializeField, Tooltip("平面漂移方向，X=右，Y=上")]
    private Vector2 m_driftDirection = new Vector2(1f, 0.35f);

    [SerializeField, Min(0f), Tooltip("基础漂移速度（Canvas 本地像素/秒）")]
    private float m_driftSpeed = 18f;

    [SerializeField, Min(0f), Tooltip("速度随机扰动幅度")]
    private float m_driftRandomness = 6f;

    [Header("外观")]
    [SerializeField, Tooltip("粒子 Sprite，留空则使用内置柔光圆点")]
    private Sprite m_particleSprite;

    [SerializeField, Tooltip("粒子基础颜色（含 Alpha）")]
    private Color m_particleColor = new Color(1f, 1f, 1f, 0.4f);

    [SerializeField, Range(0f, 1f), Tooltip("Alpha 随机偏移量")]
    private float m_colorAlphaRandomness = 0.12f;

    [SerializeField, Tooltip("粒子尺寸范围 (min, max)，单位为 Canvas 本地像素")]
    private Vector2 m_sizeRange = new Vector2(4f, 12f);

    [SerializeField, Tooltip("生命周期范围 (min, max) 秒")]
    private Vector2 m_lifetimeRange = new Vector2(6f, 12f);

    [Header("发射区域")]
    [SerializeField, Tooltip("发射盒半尺寸（Canvas 本地像素）；为零时自动使用 RectTransform 尺寸")]
    private Vector2 m_emissionArea = Vector2.zero;

    [Header("行为")]
    [SerializeField, Tooltip("不受 Time.timeScale 影响")]
    private bool m_useUnscaledTime = true;

    [SerializeField, Min(0f), Tooltip("位置噪声扰动幅度")]
    private float m_noiseStrength = 12f;

    [SerializeField, Tooltip("启动时自动播放")]
    private bool m_playOnAwake = true;

    private readonly List<Floater> m_floaters = new List<Floater>();
    private RectTransform m_rectTransform;
    private RectTransform m_poolRoot;
    private float m_spawnAccumulator;
    private float m_noiseSeed;
    private bool m_isPlaying;
    private Sprite m_runtimeSprite;

    private void Reset()
    {
        TryAssignDefaultSprite();
    }

    private void Awake()
    {
        EnsureHierarchy();
        TryAssignDefaultSprite();
        SyncPoolSize();
        if (m_playOnAwake && Application.isPlaying && isActiveAndEnabled)
        {
            Play();
        }
    }

    private void OnEnable()
    {
        if (!ShouldBuildRuntimePool())
        {
            return;
        }

        EnsureHierarchy();
        TryAssignDefaultSprite();
        SyncPoolSize();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Stop();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return;
        }
#endif

        // OnValidate 内禁止创建子物体或添加组件（会触发 SendMessage 限制）。
        TryAssignDefaultSprite();
    }

    private void OnRectTransformDimensionsChange()
    {
        // 布局变化时无需额外处理，发射区域在运行时读取 rect。
    }

    private void Update()
    {
        if (!m_isPlaying)
        {
            return;
        }

        float deltaTime = m_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (deltaTime <= 0f)
        {
            return;
        }

        UpdateSpawning(deltaTime);
        UpdateFloaters(deltaTime);
    }

    public void Play()
    {
        m_isPlaying = true;
        m_noiseSeed = Random.value * 1000f;
    }

    public void Stop()
    {
        m_isPlaying = false;
        m_spawnAccumulator = 0f;
        for (int i = 0; i < m_floaters.Count; i++)
        {
            DeactivateFloater(m_floaters[i]);
        }
    }

    /// <summary>兼容旧版 Prefab：若存在 ParticleSystem 则移除。</summary>
    public void ApplySettings()
    {
        EnsureHierarchy();
        TryAssignDefaultSprite();
        SyncPoolSize();
        if (m_playOnAwake && Application.isPlaying && isActiveAndEnabled)
        {
            Play();
        }
    }

    private bool ShouldBuildRuntimePool()
    {
        if (!Application.isPlaying)
        {
            return false;
        }

#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return false;
        }
#endif

        return true;
    }

    private void EnsureHierarchy()
    {
        m_rectTransform = transform as RectTransform;
        if (m_rectTransform == null)
        {
            return;
        }

        CleanupLegacyParticleSystem();

        if (m_poolRoot == null)
        {
            Transform existing = transform.Find("FloaterPool");
            if (existing != null)
            {
                m_poolRoot = existing as RectTransform;
            }
        }

        if (m_poolRoot == null && ShouldBuildRuntimePool())
        {
            GameObject poolObject = new GameObject("FloaterPool", typeof(RectTransform));
            poolObject.layer = gameObject.layer;
            m_poolRoot = poolObject.GetComponent<RectTransform>();
            m_poolRoot.SetParent(m_rectTransform, false);
            StretchRect(m_poolRoot);
        }

        StretchRect(m_rectTransform);
        if (m_poolRoot != null)
        {
            StretchRect(m_poolRoot);
        }
    }

    private void CleanupLegacyParticleSystem()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        ParticleSystem legacyParticleSystem = GetComponent<ParticleSystem>();
        if (legacyParticleSystem != null)
        {
            if (Application.isPlaying)
            {
                Destroy(legacyParticleSystem);
            }
            else
            {
                DestroyImmediate(legacyParticleSystem);
            }
        }

        ParticleSystemRenderer legacyRenderer = GetComponent<ParticleSystemRenderer>();
        if (legacyRenderer != null)
        {
            if (Application.isPlaying)
            {
                Destroy(legacyRenderer);
            }
            else
            {
                DestroyImmediate(legacyRenderer);
            }
        }
    }

    private void SyncPoolSize()
    {
        if (!ShouldBuildRuntimePool())
        {
            return;
        }

        EnsureHierarchy();
        if (m_poolRoot == null)
        {
            return;
        }

        while (m_floaters.Count < m_maxParticles)
        {
            m_floaters.Add(CreateFloater(m_floaters.Count));
        }

        for (int i = 0; i < m_floaters.Count; i++)
        {
            if (i >= m_maxParticles)
            {
                Floater floater = m_floaters[i];
                if (floater.Rect != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(floater.Rect.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(floater.Rect.gameObject);
                    }
                }
            }
        }

        if (m_floaters.Count > m_maxParticles)
        {
            m_floaters.RemoveRange(m_maxParticles, m_floaters.Count - m_maxParticles);
        }
    }

    private Floater CreateFloater(int index)
    {
        GameObject floaterObject = new GameObject($"Floater_{index}", typeof(RectTransform), typeof(Image));
        floaterObject.layer = gameObject.layer;
        RectTransform rect = floaterObject.GetComponent<RectTransform>();
        rect.SetParent(m_poolRoot, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = floaterObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.maskable = false;
        image.sprite = GetRuntimeSprite();
        image.color = m_particleColor;

        Floater floater = new Floater
        {
            Rect = rect,
            Image = image
        };
        DeactivateFloater(floater);
        return floater;
    }

    private void UpdateSpawning(float deltaTime)
    {
        if (m_emissionRate <= 0f)
        {
            return;
        }

        int activeCount = GetActiveCount();
        if (activeCount >= m_maxParticles)
        {
            return;
        }

        m_spawnAccumulator += deltaTime;
        float spawnInterval = 1f / m_emissionRate;
        while (m_spawnAccumulator >= spawnInterval && GetActiveCount() < m_maxParticles)
        {
            m_spawnAccumulator -= spawnInterval;
            SpawnFloater();
        }
    }

    private void SpawnFloater()
    {
        Floater floater = GetInactiveFloater();
        if (floater == null)
        {
            return;
        }

        Vector2 halfExtents = GetEmissionHalfExtents();
        Vector2 spawnPosition = new Vector2(
            Random.Range(-halfExtents.x, halfExtents.x),
            Random.Range(-halfExtents.y, halfExtents.y));

        Vector2 direction = m_driftDirection.sqrMagnitude > 0.0001f
            ? m_driftDirection.normalized
            : Vector2.right;
        float speed = m_driftSpeed + Random.Range(-m_driftRandomness, m_driftRandomness);
        Vector2 velocity = direction * speed;

        float size = Random.Range(m_sizeRange.x, m_sizeRange.y);
        float lifetime = Random.Range(m_lifetimeRange.x, m_lifetimeRange.y);
        float alphaJitter = Random.Range(-m_colorAlphaRandomness, m_colorAlphaRandomness);
        float peakAlpha = Mathf.Clamp01(m_particleColor.a + alphaJitter);

        floater.Active = true;
        floater.Age = 0f;
        floater.Lifetime = Mathf.Max(0.1f, lifetime);
        floater.Velocity = velocity;
        floater.PeakAlpha = peakAlpha;
        floater.Rect.anchoredPosition = spawnPosition;
        floater.Rect.sizeDelta = new Vector2(size, size);
        floater.Rect.gameObject.SetActive(true);

        Color color = m_particleColor;
        color.a = 0f;
        floater.Image.sprite = GetRuntimeSprite();
        floater.Image.color = color;
    }

    private void UpdateFloaters(float deltaTime)
    {
        Vector2 halfExtents = GetEmissionHalfExtents();
        float paddedX = halfExtents.x + 40f;
        float paddedY = halfExtents.y + 40f;

        for (int i = 0; i < m_floaters.Count; i++)
        {
            Floater floater = m_floaters[i];
            if (!floater.Active)
            {
                continue;
            }

            floater.Age += deltaTime;
            if (floater.Age >= floater.Lifetime)
            {
                DeactivateFloater(floater);
                continue;
            }

            Vector2 noiseOffset = Vector2.zero;
            if (m_noiseStrength > 0f)
            {
                float noiseTime = Time.realtimeSinceStartup + m_noiseSeed + floater.Age;
                noiseOffset = new Vector2(
                    (Mathf.PerlinNoise(noiseTime * 0.25f, floater.Age) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(floater.Age, noiseTime * 0.25f) - 0.5f) * 2f) * m_noiseStrength * deltaTime;
            }

            Vector2 position = floater.Rect.anchoredPosition + floater.Velocity * deltaTime + noiseOffset;
            floater.Rect.anchoredPosition = position;

            if (Mathf.Abs(position.x) > paddedX || Mathf.Abs(position.y) > paddedY)
            {
                DeactivateFloater(floater);
                continue;
            }

            float normalizedAge = floater.Age / floater.Lifetime;
            float alpha = EvaluateAlpha(normalizedAge, floater.PeakAlpha);
            Color color = m_particleColor;
            color.a = alpha;
            floater.Image.color = color;
        }
    }

    private static float EvaluateAlpha(float normalizedAge, float peakAlpha)
    {
        if (normalizedAge <= 0.12f)
        {
            return Mathf.Lerp(0f, peakAlpha, normalizedAge / 0.12f);
        }

        if (normalizedAge >= 0.78f)
        {
            return Mathf.Lerp(peakAlpha, 0f, (normalizedAge - 0.78f) / 0.22f);
        }

        return peakAlpha;
    }

    private Floater GetInactiveFloater()
    {
        for (int i = 0; i < m_floaters.Count; i++)
        {
            if (!m_floaters[i].Active)
            {
                return m_floaters[i];
            }
        }

        return null;
    }

    private int GetActiveCount()
    {
        int count = 0;
        for (int i = 0; i < m_floaters.Count; i++)
        {
            if (m_floaters[i].Active)
            {
                count++;
            }
        }

        return count;
    }

    private void DeactivateFloater(Floater floater)
    {
        floater.Active = false;
        floater.Age = 0f;
        if (floater.Rect != null)
        {
            floater.Rect.gameObject.SetActive(false);
        }
    }

    private Vector2 GetEmissionHalfExtents()
    {
        if (m_emissionArea.sqrMagnitude > 0.0001f)
        {
            return m_emissionArea;
        }

        if (m_rectTransform != null)
        {
            Vector2 size = m_rectTransform.rect.size;
            if (size.sqrMagnitude > 1f)
            {
                return size * 0.5f;
            }
        }

        return new Vector2(960f, 540f);
    }

    private Sprite GetRuntimeSprite()
    {
        if (m_particleSprite != null)
        {
            return m_particleSprite;
        }

        if (m_runtimeSprite == null)
        {
            m_runtimeSprite = CreateFallbackSprite();
        }

        return m_runtimeSprite;
    }

    private void TryAssignDefaultSprite()
    {
        if (m_particleSprite != null)
        {
            return;
        }

#if UNITY_EDITOR
        Sprite builtinSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (builtinSprite != null)
        {
            m_particleSprite = builtinSprite;
        }
#endif
    }

    private static Sprite CreateFallbackSprite()
    {
        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[32 * 32];
        Vector2 center = new Vector2(15.5f, 15.5f);
        float radius = 14f;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - distance / radius);
                alpha *= alpha;
                pixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }
}

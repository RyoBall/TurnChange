using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 跨场景管理 UI 漂浮粒子：仅在非战斗场景挂载到场景 Canvas，进入 Fight 时强制隐藏。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class AmbientFloatingParticlesHost : MonoBehaviour
{
    public const string BattleSceneName = "Fight";

    private static readonly string[] s_anchorChildNames = { "Main", "主界面" };
    private static readonly string[] s_backgroundChildNames = { "背景", "Background" };
    private const string SceneCanvasName = "Canvas";
    private const int PersistentOverlayCanvasSortingOrder = 100;
    private const string ParticlesPrefabResourcePath = "Prefabs/通用物体/AmbientFloatingParticles";

    public static AmbientFloatingParticlesHost Instance { get; private set; }

    [SerializeField] private AmbientFloatingParticles m_particlesPrefab;
    [SerializeField] private List<string> m_excludedSceneNames = new List<string> { BattleSceneName };

    private AmbientFloatingParticles m_runtimeParticles;
    private Coroutine m_showCoroutine;
    private bool m_isSubscribed;
    private bool m_isUnloadedSubscribed;

    private void Awake()
    {
        if (!TryClaimSingleton())
        {
            return;
        }

        EnsureParticlesInstance();
        SubscribeSceneEvents();
        HandleScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            StopShowCoroutine();
            UnsubscribeSceneEvents();
            Instance = null;
        }
    }

    private bool TryClaimSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return false;
        }

        Instance = this;
        return true;
    }

    private void SubscribeSceneEvents()
    {
        if (!m_isSubscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            m_isSubscribed = true;
        }

        if (!m_isUnloadedSubscribed)
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            m_isUnloadedSubscribed = true;
        }
    }

    private void UnsubscribeSceneEvents()
    {
        if (m_isSubscribed)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            m_isSubscribed = false;
        }

        if (m_isUnloadedSubscribed)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            m_isUnloadedSubscribed = false;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (m_runtimeParticles == null)
        {
            return;
        }

        Transform particleParent = m_runtimeParticles.transform.parent;
        if (particleParent != null && particleParent.gameObject.scene == scene)
        {
            StopShowCoroutine();
            m_runtimeParticles.Stop();
            m_runtimeParticles.transform.SetParent(transform, false);
            StretchRect(m_runtimeParticles.transform as RectTransform);
            m_runtimeParticles.gameObject.SetActive(false);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene);
    }

    private void HandleScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (IsExcludedScene(scene.name))
        {
            StopShowCoroutine();
            HideParticles();
            DestroyScenePlacedParticles(scene);
            return;
        }

        if (!EnsureParticlesInstance())
        {
            Debug.LogError("[AmbientFloatingParticlesHost] 无法创建粒子实例。");
            return;
        }

        DestroyScenePlacedParticles(scene);

        Canvas targetCanvas = FindSceneCanvas(scene);
        if (targetCanvas == null)
        {
            StopShowCoroutine();
            HideParticles();
            Debug.LogWarning($"[AmbientFloatingParticlesHost] 场景 {scene.name} 未找到可用 Canvas，已隐藏粒子。");
            return;
        }

        Transform anchor = FindAnchor(targetCanvas.transform);
        AttachParticles(anchor);
        ScheduleShowParticles();
    }

    private bool IsExcludedScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        if (sceneName == BattleSceneName)
        {
            return true;
        }

        if (m_excludedSceneNames == null)
        {
            return false;
        }

        for (int i = 0; i < m_excludedSceneNames.Count; i++)
        {
            if (m_excludedSceneNames[i] == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    private bool EnsureParticlesInstance()
    {
        if (m_runtimeParticles != null)
        {
            return true;
        }

        AmbientFloatingParticles prefab = m_particlesPrefab;
        if (prefab == null)
        {
            GameObject prefabObject = Resources.Load<GameObject>(ParticlesPrefabResourcePath);
            if (prefabObject != null)
            {
                prefab = prefabObject.GetComponent<AmbientFloatingParticles>();
            }
        }

        if (prefab == null)
        {
            Debug.LogError("[AmbientFloatingParticlesHost] 未找到 AmbientFloatingParticles Prefab。");
            return false;
        }

        m_runtimeParticles = Instantiate(prefab, transform);
        m_runtimeParticles.name = prefab.name;
        StretchRect(m_runtimeParticles.transform as RectTransform);
        m_runtimeParticles.gameObject.SetActive(false);
        return true;
    }

    private static Canvas FindSceneCanvas(Scene scene)
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        Canvas namedCanvas = null;
        Canvas fallbackCanvas = null;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.gameObject.scene != scene)
            {
                continue;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay
                && canvas.sortingOrder >= PersistentOverlayCanvasSortingOrder)
            {
                continue;
            }

            if (canvas.gameObject.name == SceneCanvasName)
            {
                namedCanvas = canvas;
                break;
            }

            if (fallbackCanvas == null)
            {
                fallbackCanvas = canvas;
            }
        }

        return namedCanvas != null ? namedCanvas : fallbackCanvas;
    }

    private static Transform FindAnchor(Transform canvasTransform)
    {
        for (int i = 0; i < s_anchorChildNames.Length; i++)
        {
            Transform anchor = canvasTransform.Find(s_anchorChildNames[i]);
            if (anchor != null)
            {
                return anchor;
            }
        }

        return canvasTransform;
    }

    private void AttachParticles(Transform anchor)
    {
        if (m_runtimeParticles == null || anchor == null)
        {
            return;
        }

        RectTransform rectTransform = m_runtimeParticles.transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.SetParent(anchor, false);
        StretchRect(rectTransform);
        rectTransform.SetSiblingIndex(GetParticleSiblingIndex(anchor));
    }

    private static int GetParticleSiblingIndex(Transform anchor)
    {
        for (int nameIndex = 0; nameIndex < s_backgroundChildNames.Length; nameIndex++)
        {
            Transform background = anchor.Find(s_backgroundChildNames[nameIndex]);
            if (background != null)
            {
                return background.GetSiblingIndex() + 1;
            }
        }

        return 0;
    }

    private void ScheduleShowParticles()
    {
        StopShowCoroutine();
        m_showCoroutine = StartCoroutine(ShowParticlesAfterLayout());
    }

    private void StopShowCoroutine()
    {
        if (m_showCoroutine == null)
        {
            return;
        }

        StopCoroutine(m_showCoroutine);
        m_showCoroutine = null;
    }

    private IEnumerator ShowParticlesAfterLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;

        m_showCoroutine = null;
        ShowParticles();
    }

    private void ShowParticles()
    {
        if (m_runtimeParticles == null)
        {
            return;
        }

        m_runtimeParticles.gameObject.SetActive(true);
        m_runtimeParticles.ApplySettings();
        m_runtimeParticles.Play();
    }

    private void HideParticles()
    {
        if (m_runtimeParticles == null)
        {
            return;
        }

        m_runtimeParticles.Stop();
        if (m_runtimeParticles.gameObject.activeSelf)
        {
            m_runtimeParticles.gameObject.SetActive(false);
        }
    }

    private bool IsHostManagedParticle(AmbientFloatingParticles particle)
    {
        return particle != null && particle == m_runtimeParticles;
    }

    private void DestroyScenePlacedParticles(Scene scene)
    {
        AmbientFloatingParticles[] particles = Object.FindObjectsOfType<AmbientFloatingParticles>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            AmbientFloatingParticles particle = particles[i];
            if (particle == null || particle.gameObject.scene != scene)
            {
                continue;
            }

            if (IsHostManagedParticle(particle))
            {
                continue;
            }

            Object.Destroy(particle.gameObject);
        }
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
        rectTransform.localPosition = Vector3.zero;
    }
}

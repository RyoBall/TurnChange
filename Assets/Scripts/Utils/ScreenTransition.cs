using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Feedbacks;
using System;

/// <summary>
/// 屏幕转场（协程）：动画分两部分——先屏幕渐黑，然后颜色过渡到白色并淡出。
/// 可在 Inspector 配置颜色与时长；若未指定 overlay，会自动创建一个全屏 Canvas+Image。
/// 使用：在场景挂载该组件，调用 StartTransition() 或 StartTransition(toPhase1,hold,phase1To2,outDur)。
/// </summary>
public class ScreenTransition : MonoBehaviour
{
    public static ScreenTransition Instance { get; private set; }


    [Header("时长（秒）")]
    [SerializeField] private float fadeToPhase1Duration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("覆盖层设置")]
    [SerializeField, Tooltip("优先使用已指定的 Image，若为空则自动创建全屏 Image")]
    private Coroutine runningCoroutine;
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private MMF_Player fadeInPlayer;
    [SerializeField] private MMF_Player fadeOutPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
            Instance = this;
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(overlayCanvas.gameObject);

    }

    public Coroutine EnterTransition()
    {
        return FadeIn();
    }
    public Coroutine ExitTransition()
    {
        return FadeOut();
    }
    public Coroutine Transition(Action action,float duration=0)
    {
        return StartCoroutine(TransitionIE(action,duration));
    }
    private IEnumerator TransitionIE(Action action,float duration=0)
    {
        yield return FadeIn();
        action?.Invoke();
        yield return new WaitForSeconds(duration);
        yield return FadeOut();
    }
    private Coroutine FadeIn()
    {
        if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        runningCoroutine = StartCoroutine(FadeInFeedback());
        return runningCoroutine;
    }
    private Coroutine FadeOut()
    {
        if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        runningCoroutine = StartCoroutine(FadeOutFeedback());
        return runningCoroutine;
    }
    private IEnumerator FadeInFeedback()
    {
        fadeInPlayer?.PlayFeedbacks();
        yield return new WaitForSecondsRealtime(fadeInPlayer != null ? fadeInPlayer.TotalDuration : fadeToPhase1Duration);
    }

    private IEnumerator FadeOutFeedback()
    {
        fadeOutPlayer?.PlayFeedbacks();
        yield return new WaitForSecondsRealtime(fadeOutPlayer != null ? fadeOutPlayer.TotalDuration : fadeOutDuration);
    }
    #region 程序转场动画(废弃)
    /*    private void CreateOverlay()
    {
        if (overlayImage != null) return;

        var canvasGo = new GameObject("ScreenTransition_Canvas");
        overlayCanvas = canvasGo.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 1000;

        canvasGo.AddComponent<CanvasScaler>();
        var gr = canvasGo.AddComponent<GraphicRaycaster>();
        gr.enabled = false;

        var imageGo = new GameObject("Overlay");
        imageGo.transform.SetParent(canvasGo.transform, false);
        var img = imageGo.AddComponent<Image>();
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        img.color = new Color(0f, 0f, 0f, 0f);

        overlayImage = img;

        DontDestroyOnLoad(canvasGo);
    }

    private void SetAlpha(float a)
    {
        if (overlayImage == null) return;
        var c = overlayImage.color;
        c.a = a;
        overlayImage.color = c;
    }
    public Coroutine StartTransition()
    {
        return StartTransition(fadeToPhase1Duration, holdPhase1Duration, transitionPhase1To2Duration, fadeOutDuration);
    }

    public Coroutine StartTransition(float toPhase1Dur, float holdDur, float phase1To2Dur, float outDur)
    {
        if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        runningCoroutine = StartCoroutine(PlayTransitionRoutine(toPhase1Dur, holdDur, phase1To2Dur, outDur));
        return runningCoroutine;
    }
    public Coroutine EnterTransition(float duration)
    {
        if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        runningCoroutine = StartCoroutine(FadeToBlackRoutine(duration));
        return runningCoroutine;
    }

    private IEnumerator FadeToBlackRoutine(float duration)
    {
        fadeInPlayer?.PlayFeedbacks();
        yield return StartCoroutine(FadeColorAndAlpha(Color.clear, new Color(phase1Color.r, phase1Color.g, phase1Color.b, phase1Color.a), duration));
    }

    /// <summary>
    /// 将屏幕从 phase1Color（默认黑）过渡到 phase2Color（默认白），alpha 保持为 1。
    /// </summary>

    public Coroutine ExitTransition(float duration)
    {
        if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        runningCoroutine = StartCoroutine(FadeToWhiteRoutine(duration));
        return runningCoroutine;
    }

    private IEnumerator FadeToWhiteRoutine(float duration)
    {
        if (overlayImage == null)
        {
            if (createOverlayIfMissing) CreateOverlay();
            else yield break;
        }

        // 确保 overlay 为不透明并使用 phase1Color 作为起始颜色
        overlayImage.color = new Color(phase1Color.r, phase1Color.g, phase1Color.b, 1f);
        yield return StartCoroutine(FadeColor(new Color(phase1Color.r, phase1Color.g, phase1Color.b, phase1Color.a), new Color(phase2Color.r, phase2Color.g, phase2Color.b, phase2Color.a), duration));
    }

    private IEnumerator PlayTransitionRoutine(float toPhase1Dur, float holdDur, float phase1To2Dur, float outDur)
    {
        if (overlayImage == null)
        {
            if (createOverlayIfMissing) CreateOverlay();
            else yield break;
        }

        // 1) 从透明 => 渐变到 phase1Color（alpha 从 0 -> 1）
        yield return StartCoroutine(FadeColorAndAlpha(Color.clear, new Color(phase1Color.r, phase1Color.g, phase1Color.b, 1f), toPhase1Dur));

        // 保持（可为 0）
        if (holdDur > 0f) yield return new WaitForSecondsRealtime(holdDur);

        // 2) 颜色从 phase1Color 过渡到 phase2Color（alpha 保持 1）
        yield return StartCoroutine(FadeColor(new Color(phase1Color.r, phase1Color.g, phase1Color.b, 1f), new Color(phase2Color.r, phase2Color.g, phase2Color.b, 1f), phase1To2Dur));

        // 最后淡出 overlay（alpha 1 -> 0）以还原场景
        yield return StartCoroutine(FadeAlpha(1f, 0f, outDur));

        runningCoroutine = null;
    }

    private IEnumerator FadeColorAndAlpha(Color from, Color to, float duration)
    {
        float elapsed = 0f;
        if (duration <= 0f)
        {
            overlayImage.color = to;
            yield break;
        }
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            overlayImage.color = Color.Lerp(from, to, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        overlayImage.color = to;
    }

    private IEnumerator FadeColor(Color from, Color to, float duration)
    {
        float elapsed = 0f;
        if (duration <= 0f)
        {
            var final = to; final.a = 1f;
            overlayImage.color = final;
            yield break;
        }
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            var c = Color.Lerp(from, to, t);
            overlayImage.color = c;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        var end = to;
        overlayImage.color = end;
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        var color = overlayImage.color;
        if (duration <= 0f)
        {
            color.a = to;
            overlayImage.color = color;
            yield break;
        }
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            color.a = Mathf.Lerp(from, to, t);
            overlayImage.color = color;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        color.a = to;
        overlayImage.color = color;
    }*/
    #endregion

}

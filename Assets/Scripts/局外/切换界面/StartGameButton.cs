using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 开始游戏按钮：通过 ScreenTransition 转场加载主界面场景
/// </summary>
[DisallowMultipleComponent]
public class StartGameButton : MonoBehaviour
{
    /// <summary>点击开始游戏按钮时的静态事件</summary>
    public static event Action GameStarted;

    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private Button startButton;
    [SerializeField] private StartGameSceneIntro m_sceneIntro;

    private bool m_isStarting;

    private void Awake()
    {
        BindButton();
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartGameClicked);
        }
    }

    /// <summary>
    /// 公开函数：开始游戏，播放转场动画后加载主界面场景
    /// </summary>
    public void OnStartGameClicked()
    {
        if (m_isStarting)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogWarning("[StartGameButton] 未配置主界面场景名，无法加载场景。", this);
            return;
        }

        if (ScreenTransition.Instance == null)
        {
            Debug.LogWarning("[StartGameButton] 未找到 ScreenTransition，无法转场。", this);
            return;
        }

        m_isStarting = true;
        if (startButton != null)
        {
            startButton.interactable = false;
        }

        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        if (m_sceneIntro != null)
        {
            yield return m_sceneIntro.PlayIntroCoroutine();
        }

        yield return ScreenTransition.Instance.Transition(() =>
        {
            GameStarted?.Invoke();
            SceneManager.LoadScene(mainSceneName);
        });
    }

    private void BindButton()
    {
        if (startButton == null)
        {
            startButton = GetComponent<Button>();
        }

        if (startButton == null)
        {
            return;
        }

        startButton.onClick.RemoveListener(OnStartGameClicked);
        startButton.onClick.AddListener(OnStartGameClicked);
    }
}

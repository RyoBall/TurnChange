using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameAudioInputObserver : MonoBehaviour
{
    private static bool s_Bootstrapped;

    private readonly List<RaycastResult> m_RaycastResults = new List<RaycastResult>();
    private Button m_CurrentHoveredButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (s_Bootstrapped)
        {
            return;
        }

        var observerObject = new GameObject(nameof(GameAudioInputObserver));
        observerObject.AddComponent<GameAudioInputObserver>();
        s_Bootstrapped = true;
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        ClearHoveredButton();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameAudioEvents.Raise(GameAudioEventType.MouseClick, m_CurrentHoveredButton, m_CurrentHoveredButton);
        }
    }

    private void HandleActiveSceneChanged(Scene current, Scene next)
    {
        ClearHoveredButton();
    }

    private void ClearHoveredButton()
    {
        if (m_CurrentHoveredButton == null)
        {
            return;
        }

        GameAudioEvents.Raise(GameAudioEventType.ButtonHoverExit, m_CurrentHoveredButton, m_CurrentHoveredButton);
        m_CurrentHoveredButton = null;
    }

    private Button ResolveHoveredButton()
    {
        if (EventSystem.current == null)
        {
            return null;
        }

        var pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        m_RaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, m_RaycastResults);

        for (int i = 0; i < m_RaycastResults.Count; i++)
        {
            var hoveredObject = m_RaycastResults[i].gameObject;
            if (hoveredObject == null)
            {
                continue;
            }

            Button button = hoveredObject.GetComponentInParent<Button>();
            if (button != null && button.isActiveAndEnabled && button.interactable)
            {
                return button;
            }
        }

        return null;
    }
}
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

[DisallowMultipleComponent]
public class GameAudioEventSoundPlayer : MonoBehaviour
{
    [SerializeField] private GameAudioCatalog catalog;

    private bool m_HasWarnedMissingSoundManager;
    private bool m_HasWarnedMissingCatalog;

    private GameAudioCatalog Catalog => catalog != null ? catalog : GameAudioCatalog.Instance;

    private void OnEnable()
    {
        GameAudioEvents.Raised += HandleGameAudioEvent;
    }

    private void OnDisable()
    {
        GameAudioEvents.Raised -= HandleGameAudioEvent;
    }

    private void HandleGameAudioEvent(GameAudioEvent audioEvent)
    {
        GameAudioCatalog activeCatalog = Catalog;
        if (activeCatalog == null)
        {
            if (!m_HasWarnedMissingCatalog)
            {
                Debug.LogWarning("[GameAudioEventSoundPlayer] 未找到 GameAudioCatalog，请在 Resources 中放置 GameAudioCatalog.asset。", this);
                m_HasWarnedMissingCatalog = true;
            }

            return;
        }

        m_HasWarnedMissingCatalog = false;

        if (!MMSoundManager.HasInstance)
        {
            if (!m_HasWarnedMissingSoundManager)
            {
                Debug.LogWarning($"[GameAudioEventSoundPlayer] 收到事件 {audioEvent.EventType}，但场景中没有启用的 MMSoundManager，无法播放音效。", this);
                m_HasWarnedMissingSoundManager = true;
            }

            return;
        }

        m_HasWarnedMissingSoundManager = false;

        IReadOnlyList<GameAudioEntry> bindings = activeCatalog.GetEventBindings(audioEvent.EventType);
        for (int i = 0; i < bindings.Count; i++)
        {
            PlayBinding(audioEvent, bindings[i], activeCatalog);
        }
    }

    private void PlayBinding(GameAudioEvent audioEvent, GameAudioEntry binding, GameAudioCatalog activeCatalog)
    {
        if (binding == null || binding.clip == null)
        {
            return;
        }

        MMSoundManagerPlayOptions options = MMSoundManagerPlayOptions.Default;
        options.MmSoundManagerTrack = binding.track;
        options.Volume = activeCatalog.ResolveVolume(binding);
        options.Pitch = activeCatalog.ResolvePitch(binding);
        options.Location = binding.useTargetPosition
            ? ResolveWorldPosition(audioEvent.Target != null ? audioEvent.Target : audioEvent.Source)
            : Vector3.zero;

        AudioSource playedSource = MMSoundManager.Instance.PlaySound(binding.clip, options);
        if (playedSource == null)
        {
            Debug.LogWarning($"[GameAudioEventSoundPlayer] 事件 {audioEvent.EventType} 已命中 {binding.entryId}，但 MMSoundManager 未返回 AudioSource。", this);
        }
    }

    private static Vector3 ResolveWorldPosition(UnityEngine.Object target)
    {
        if (target is Component component)
        {
            return component.transform.position;
        }

        if (target is GameObject gameObject)
        {
            return gameObject.transform.position;
        }

        return Vector3.zero;
    }
}

using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

[DisallowMultipleComponent]
public class GameAudioEventSoundPlayer : MonoBehaviour
{
    [Serializable]
    private struct AudioBinding
    {
        public GameAudioEventType eventType;
        public AudioClip clip;
        public MMSoundManager.MMSoundManagerTracks track;
        [Range(0f, 2f)] public float volume;
        [Range(-3f, 3f)] public float pitch;
        public bool useTargetPosition;
    }

    [SerializeField] private List<AudioBinding> audioBindings = new List<AudioBinding>();

    private bool m_HasWarnedMissingSoundManager;

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

        for (int i = 0; i < audioBindings.Count; i++)
        {
            AudioBinding binding = audioBindings[i];
            if (binding.eventType != audioEvent.EventType || binding.clip == null)
            {
                continue;
            }
            Debug.Log($"Playing sound for event {audioEvent.EventType} with clip {binding.clip.name}");
            MMSoundManagerPlayOptions options = MMSoundManagerPlayOptions.Default;
            options.MmSoundManagerTrack = binding.track;
            options.Volume = binding.volume <= 0f ? 1f : binding.volume;
            options.Pitch = Mathf.Approximately(binding.pitch, 0f) ? 1f : binding.pitch;
            options.Location = binding.useTargetPosition
                ? ResolveWorldPosition(audioEvent.Target != null ? audioEvent.Target : audioEvent.Source)
                : Vector3.zero;

            AudioSource playedSource = MMSoundManager.Instance.PlaySound(binding.clip, options);
            if (playedSource == null)
            {
                Debug.LogWarning($"[GameAudioEventSoundPlayer] 事件 {audioEvent.EventType} 已命中绑定 {binding.clip.name}，但 MMSoundManager 未返回 AudioSource。请检查 AudioClip、AudioListener 和音轨设置。", this);
            }
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
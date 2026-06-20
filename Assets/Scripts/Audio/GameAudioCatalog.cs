using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

public enum GameAudioCategory
{
    BGM = 0,
    UI = 1,
    Combat = 2,
    System = 3,
    Other = 4
}

[Serializable]
public class GameAudioEntry
{
    [Tooltip("唯一标识，例如 ui.mouse_click、bgm.lobby")]
    public string entryId;

    [Tooltip("编辑器显示名称")]
    public string displayName;

    public GameAudioCategory category = GameAudioCategory.Other;
    public AudioClip clip;

    [Header("事件绑定")]
    public bool bindToEvent;
    public GameAudioEventType eventType;

    [Header("BGM 绑定")]
    public bool bindToBgm;
    public BGMPlayer.BGMType bgmType;

    [Header("播放参数")]
    public MMSoundManager.MMSoundManagerTracks track = MMSoundManager.MMSoundManagerTracks.Sfx;
    [Range(0f, 2f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f;
    public bool useTargetPosition;
    public bool loop;
}

[CreateAssetMenu(fileName = "GameAudioCatalog", menuName = "配置/音频配置表")]
public class GameAudioCatalog : ScriptableObject
{
    private static GameAudioCatalog s_Instance;

    [SerializeField] private List<GameAudioEntry> entries = new List<GameAudioEntry>();

    public IReadOnlyList<GameAudioEntry> Entries => entries;

    public static GameAudioCatalog Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = Resources.Load<GameAudioCatalog>("GameAudioCatalog");
            }

            return s_Instance;
        }
    }

    private void OnEnable()
    {
        RebuildCaches();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        m_CachesBuilt = false;

        if (!Application.isPlaying)
        {
            return;
        }

        BGMPlayer.Instance?.RefreshCurrentBgmSettings();
    }
#endif

    private readonly Dictionary<BGMPlayer.BGMType, GameAudioEntry> m_BgmLookup =
        new Dictionary<BGMPlayer.BGMType, GameAudioEntry>();
    private readonly Dictionary<GameAudioEventType, List<GameAudioEntry>> m_EventLookup =
        new Dictionary<GameAudioEventType, List<GameAudioEntry>>();
    private static readonly List<GameAudioEntry> s_EmptyEventBindings = new List<GameAudioEntry>();
    private bool m_CachesBuilt;

    public void RebuildCaches()
    {
        m_BgmLookup.Clear();
        m_EventLookup.Clear();

        for (int i = 0; i < entries.Count; i++)
        {
            GameAudioEntry entry = entries[i];
            if (entry == null || entry.clip == null)
            {
                continue;
            }

            if (entry.bindToEvent)
            {
                if (!m_EventLookup.TryGetValue(entry.eventType, out List<GameAudioEntry> eventList))
                {
                    eventList = new List<GameAudioEntry>();
                    m_EventLookup[entry.eventType] = eventList;
                }

                eventList.Add(entry);
            }

            if (entry.bindToBgm && entry.bgmType != BGMPlayer.BGMType.None)
            {
                m_BgmLookup[entry.bgmType] = entry;
            }
        }

        m_CachesBuilt = true;
    }

    private void EnsureCaches()
    {
        if (!m_CachesBuilt)
        {
            RebuildCaches();
        }
    }

    public IReadOnlyList<GameAudioEntry> GetEventBindings(GameAudioEventType eventType)
    {
        EnsureCaches();
        return m_EventLookup.TryGetValue(eventType, out List<GameAudioEntry> eventList)
            ? eventList
            : s_EmptyEventBindings;
    }

    public bool TryGetBgmEntry(BGMPlayer.BGMType bgmType, out GameAudioEntry entry)
    {
        EnsureCaches();
        return m_BgmLookup.TryGetValue(bgmType, out entry);
    }

    public bool TryGetById(string entryId, out GameAudioEntry entry)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            GameAudioEntry candidate = entries[i];
            if (candidate != null && candidate.entryId == entryId)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }

    public float ResolveVolume(GameAudioEntry entry)
    {
        if (entry == null)
        {
            return 1f;
        }

        return Mathf.Clamp(entry.volume, 0f, 2f);
    }

    public float ResolvePitch(GameAudioEntry entry)
    {
        if (entry == null)
        {
            return 1f;
        }

        return Mathf.Approximately(entry.pitch, 0f) ? 1f : entry.pitch;
    }
}

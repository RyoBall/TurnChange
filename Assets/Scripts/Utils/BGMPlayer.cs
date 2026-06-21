using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BGMPlayer : MonoBehaviour
{
    public enum BGMType
    {
        None = 0,
        Lobby = 1,
        Backpack = 2,
        Battle = 3,
        Shop = 4,
        Result = 5
    }

    public static BGMPlayer Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameAudioCatalog catalog;

    [Header("通用战斗BGM（关卡无专属BGM时随机选取）")]
    [SerializeField] private List<AudioClip> genericBattleBgmClips = new List<AudioClip>();

    private Coroutine m_PlayCoroutine;

    public BGMType CurrentBGMType { get; private set; } = BGMType.None;

    private GameAudioCatalog Catalog => catalog != null ? catalog : GameAudioCatalog.Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // 避免 Prefab 上 Play On Awake 直接播 BGM，绕过配置表。
        audioSource.playOnAwake = false;
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void Start()
    {
        if (CurrentBGMType != BGMType.None)
        {
            return;
        }

        PlayBGM(BGMType.Lobby);
    }

    public void PlayBGM(BGMType bgmType, float delayBeforePlay = 0f)
    {
        if (bgmType == BGMType.None)
        {
            StopBGM();
            return;
        }

        if (!TryGetEntry(bgmType, out GameAudioEntry entry))
        {
            Debug.LogWarning($"[BGMPlayer] 未找到 {bgmType} 对应的 BGM 配置。", this);
            return;
        }

        if (m_PlayCoroutine != null)
        {
            StopCoroutine(m_PlayCoroutine);
        }

        m_PlayCoroutine = StartCoroutine(PlayBGMRoutine(bgmType, entry, Mathf.Max(0f, delayBeforePlay)));
    }

    /// <summary>
    /// 根据关卡ID播放BGM。优先使用关卡专属BGM，若为空则从通用战斗BGM中随机选取。
    /// 所有BGM均通过 GameAudioCatalog 获取播放参数（volume/pitch/loop）。
    /// </summary>
    public void PlayLevelBGM(string levelId, AudioClip levelBgmClip, float delayBeforePlay = 0f)
    {
        AudioClip clipToPlay = ResolveLevelBgmClip(levelId, levelBgmClip);

        if (clipToPlay == null)
        {
            Debug.LogWarning($"[BGMPlayer] 关卡 {levelId} 无可用BGM（既无专属BGM，也无通用战斗BGM）。", this);
            return;
        }

        GameAudioEntry entry = ResolveLevelBgmEntry(clipToPlay);

        if (m_PlayCoroutine != null)
        {
            StopCoroutine(m_PlayCoroutine);
        }

        m_PlayCoroutine = StartCoroutine(PlayBGMRoutine(BGMType.Battle, entry, Mathf.Max(0f, delayBeforePlay)));
    }

    private AudioClip ResolveLevelBgmClip(string levelId, AudioClip levelBgmClip)
    {
        if (levelBgmClip != null)
        {
            return levelBgmClip;
        }

        if (genericBattleBgmClips != null && genericBattleBgmClips.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, genericBattleBgmClips.Count);
            AudioClip randomClip = genericBattleBgmClips[randomIndex];
            if (randomClip != null)
            {
                return randomClip;
            }
        }

        return null;
    }

    /// <summary>
    /// 根据 AudioClip 解析对应的 GameAudioEntry。
    /// 优先从 GameAudioCatalog 中查找（以获取配置的 volume/pitch/loop），
    /// 找不到则构造默认 entry。
    /// </summary>
    private GameAudioEntry ResolveLevelBgmEntry(AudioClip clip)
    {
        GameAudioCatalog activeCatalog = Catalog;
        if (activeCatalog != null && activeCatalog.TryGetEntryByClip(clip, out GameAudioEntry existingEntry))
        {
            return existingEntry;
        }

        // 构造默认 entry：loop=true，volume/pitch 使用默认值
        return new GameAudioEntry
        {
            clip = clip,
            loop = true,
            volume = 1f,
            pitch = 1f
        };
    }

    public void PauseBGM()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.UnPause();
        }
    }

    public void StopBGM()
    {
        if (m_PlayCoroutine != null)
        {
            StopCoroutine(m_PlayCoroutine);
            m_PlayCoroutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        CurrentBGMType = BGMType.None;
    }

    public bool TryGetClip(BGMType bgmType, out AudioClip clip)
    {
        if (TryGetEntry(bgmType, out GameAudioEntry entry))
        {
            clip = entry.clip;
            return clip != null;
        }

        clip = null;
        return false;
    }

    private bool TryGetEntry(BGMType bgmType, out GameAudioEntry entry)
    {
        GameAudioCatalog activeCatalog = Catalog;
        if (activeCatalog == null)
        {
            entry = null;
            return false;
        }

        return activeCatalog.TryGetBgmEntry(bgmType, out entry);
    }

    private IEnumerator PlayBGMRoutine(BGMType bgmType, GameAudioEntry entry, float delayBeforePlay)
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }

        if (delayBeforePlay > 0f)
        {
            yield return new WaitForSeconds(delayBeforePlay);
        }

        audioSource.Stop();
        audioSource.clip = entry.clip;
        audioSource.loop = entry.loop;
        ApplyEntrySettings(entry);
        audioSource.Play();

        CurrentBGMType = bgmType;
        m_PlayCoroutine = null;
    }

    public void RefreshCurrentBgmSettings()
    {
        if (CurrentBGMType == BGMType.None || audioSource == null)
        {
            return;
        }

        if (!TryGetEntry(CurrentBGMType, out GameAudioEntry entry))
        {
            return;
        }

        ApplyEntrySettings(entry);
    }

    private void ApplyEntrySettings(GameAudioEntry entry)
    {
        GameAudioCatalog activeCatalog = Catalog;
        audioSource.volume = activeCatalog != null ? activeCatalog.ResolveVolume(entry) : 1f;
        audioSource.pitch = activeCatalog != null ? activeCatalog.ResolvePitch(entry) : 1f;
    }
}

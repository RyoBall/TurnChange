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

    [Serializable]
    public class BGMEntry
    {
        public BGMType bgmType;
        public AudioClip clip;
    }

    public static BGMPlayer Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<BGMEntry> bgmEntries = new List<BGMEntry>();
    [SerializeField] private bool dontDestroyOnLoad = true;

    private readonly Dictionary<BGMType, AudioClip> m_BgmClipLookup = new Dictionary<BGMType, AudioClip>();

    private Coroutine m_PlayCoroutine;

    public BGMType CurrentBGMType { get; private set; } = BGMType.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
        RebuildLookup();
    }

    public void PlayBGM(BGMType bgmType, float delayBeforePlay = 0f)
    {
        if (bgmType == BGMType.None)
        {
            StopBGM();
            return;
        }

        if (!TryGetClip(bgmType, out AudioClip clip))
        {
            Debug.LogWarning($"[BGMPlayer] 未找到 {bgmType} 对应的 BGM。", this);
            return;
        }

        if (m_PlayCoroutine != null)
        {
            StopCoroutine(m_PlayCoroutine);
        }

        m_PlayCoroutine = StartCoroutine(PlayBGMRoutine(bgmType, clip, Mathf.Max(0f, delayBeforePlay)));
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
        if (m_BgmClipLookup.Count == 0)
        {
            RebuildLookup();
        }

        return m_BgmClipLookup.TryGetValue(bgmType, out clip) && clip != null;
    }

    private IEnumerator PlayBGMRoutine(BGMType bgmType, AudioClip clip, float delayBeforePlay)
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
        audioSource.clip = clip;
        audioSource.Play();

        CurrentBGMType = bgmType;
        m_PlayCoroutine = null;
    }

    private void RebuildLookup()
    {
        m_BgmClipLookup.Clear();

        for (int i = 0; i < bgmEntries.Count; i++)
        {
            BGMEntry entry = bgmEntries[i];
            if (entry == null || entry.clip == null || entry.bgmType == BGMType.None)
            {
                continue;
            }

            m_BgmClipLookup[entry.bgmType] = entry.clip;
        }
    }
}

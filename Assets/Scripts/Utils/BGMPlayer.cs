using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Linq;
public class BGMPlayer : MonoBehaviour
{
    public static BGMPlayer Instance { get; private set; }
    
    [SerializeField] private List<KeyValuePair<string, AudioClip>> bgmClips;
    public Dictionary<string, AudioClip> BGMClips { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void ChangeBGMClipsDic()
    {
        BGMClips = bgmClips.ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}

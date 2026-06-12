using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

public class GlobalFeedbacks : MonoBehaviour
{
    public static GlobalFeedbacks Instance { get; private set; }
    [Header("全局反馈")]
    public MMF_Player skillFeedback;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}

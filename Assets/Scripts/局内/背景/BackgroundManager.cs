using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public Image backgroundImage;
    public Color defaultColor = Color.white;
    public Color darkColor;
    public float duration;
    public Ease easeType = Ease.InOutQuad;
    public Tween ChangeBackground(bool enter)
    {
        if(enter)
        {
            return backgroundImage.DOColor(darkColor, duration).SetEase(easeType);
        }
        else
        {
            return backgroundImage.DOColor(defaultColor, duration).SetEase(easeType);
        }
    }
}

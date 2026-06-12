using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public RectTransform mainCanvasRect;
    public Volume globalVolume;
    public static GameManager Instance { get; private set; }
    public void Awake()
    {
        Instance = this;
    }
}

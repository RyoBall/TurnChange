using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PromptText : MonoBehaviour
{
    public static PromptText Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
}

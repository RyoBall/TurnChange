using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class CommandText : MonoBehaviour
{
    TMP_Text commandText;
    private void Awake()
    {
        commandText = GetComponent<TMP_Text>();
        commandText.text = "";
    }
    public void Update()
    {
        commandText.text =$"指挥点:{Commander.GetInstance().CommandPoints}";
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Commander : MonoBehaviour
{
    private static Commander Instance ;
    public static Commander GetInstance()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("Commander");
            Instance = obj.AddComponent<Commander>();
            DontDestroyOnLoad(obj);
        }
        return Instance;
    }
    private int commandPoints = 3;
    public int CommandPoints
    {
        get { return commandPoints; }
        private set{;}
    }
    private int maxCommandPoints = 5;
    public bool UseCommandPoints(int amount)
    {
        if (amount <= commandPoints)
        {
            commandPoints -= amount;
            return true;
            //这里可以添加一些使用指示点后的逻辑，比如UI更新等
        }
        else
        {
            return false;
        }
    }
}

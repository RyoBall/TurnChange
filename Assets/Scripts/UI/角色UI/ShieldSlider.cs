using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldSlider : MonoBehaviour
{
    Character targetCharacter;
    public void SetTarget(Character character)
    {
        targetCharacter = character;
    }
    void Update()
    {
        Refresh();
    }
    void Refresh()
    {
        if(targetCharacter==null)
        {
            return;
        }
        float shieldPercent = targetCharacter.currentShield / targetCharacter.maxHP;
        GetComponent<UnityEngine.UI.Slider>().value = shieldPercent;
    }
}

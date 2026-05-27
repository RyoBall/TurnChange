using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldSlider : MonoBehaviour
{
    UnitCombatant targetCharacter;
    public void SetTarget(UnitCombatant character)
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
        if(targetCharacter.maxHP<=0)
        {
            GetComponent<UnityEngine.UI.Slider>().value=0;
            return;
        }
        float shieldPercent = targetCharacter.currentShield / targetCharacter.maxHP;
        GetComponent<UnityEngine.UI.Slider>().value = Mathf.Clamp01(shieldPercent);
    }
}

using UnityEngine;
using UnityEngine.UI;

public class UnitHealthBarController : MonoBehaviour
{
    [Header("绑定")]
    [SerializeField] private UnitCombatant targetUnit;
    [SerializeField] private Slider hpSlider;

    private void Reset()
    {
        hpSlider = GetComponent<Slider>();
        ConfigureSlider();
    }

    private void Awake()
    {
        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
        }

        ConfigureSlider();
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void SetTarget(UnitCombatant unit)
    {
        targetUnit = unit;
        Refresh();
    }

    private void Refresh()
    {
        if (hpSlider == null)
        {
            return;
        }

        if (targetUnit == null || targetUnit.maxHP <= 0)
        {
            hpSlider.value = 0f;
            return;
        }

        float hpPercent = (float)targetUnit.currentHP / targetUnit.maxHP;
        hpSlider.value = Mathf.Clamp01(hpPercent);
    }

    private void ConfigureSlider()
    {
        if (hpSlider == null)
        {
            return;
        }

        hpSlider.minValue = 0f;
        hpSlider.maxValue = 1f;
        hpSlider.wholeNumbers = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
        }

        ConfigureSlider();

        if (!Application.isPlaying)
        {
            Refresh();
        }
    }
#endif
}

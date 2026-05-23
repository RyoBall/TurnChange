using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class EnemyHealthUIController : MonoBehaviour
{
    [Header("绑定")]
    [SerializeField] private Enemy targetEnemy;
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


    private void Refresh()
    {
        if (hpSlider == null)
        {
            return;
        }

        if (targetEnemy == null || targetEnemy.maxHP <= 0)
        {
            hpSlider.value = 0f;
            return;
        }

        float hpPercent = (float)targetEnemy.currentHP / targetEnemy.maxHP;
        if(hpPercent<=0)
        {
            gameObject.SetActive(false);
            return;
        }
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

using UnityEngine;
using UnityEngine.UI;

public class ChaosSlider : MonoBehaviour
{
    [Header("绑定")]
    [SerializeField] private Character character;
    [SerializeField] private Slider chaosSlider;

    private void Reset()
    {
        chaosSlider = GetComponent<Slider>();
        ConfigureSlider();
    }

    private void Awake()
    {
        if (chaosSlider == null)
        {
            chaosSlider = GetComponent<Slider>();
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

    public void SetTarget(Character character)
    {
        this.character = character;
        Refresh();
    }

    private void Refresh()
    {
        if (chaosSlider == null)
        {
            return;
        }

        if (character == null || character.ChaosValue <= 0)
        {
            chaosSlider.value = 0f;
            return;
        }

        float chaosPercent = (float)character.ChaosValue / character.MaxChaosValueConst;
        chaosSlider.value = Mathf.Clamp01(chaosPercent);
    }

    private void ConfigureSlider()
    {
        if (chaosSlider == null)
        {
            return;
        }

        chaosSlider.minValue = 0f;
        chaosSlider.maxValue = 1f;
        chaosSlider.wholeNumbers = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (chaosSlider == null)
        {
            chaosSlider = GetComponent<Slider>();
        }

        ConfigureSlider();

        if (!Application.isPlaying)
        {
            Refresh();
        }
    }
#endif
}

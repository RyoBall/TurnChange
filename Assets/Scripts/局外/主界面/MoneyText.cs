using TMPro;
using UnityEngine;

public class MoneyText : MonoBehaviour
{
    private TMP_Text m_Text;

    private void Awake()
    {
        m_Text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        m_Text.text = Datas.Instance.GetGold().ToString();
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }
    [Header("换人")]
    public GameObject changeCharacter;
    [Header("选敌提示")]
    public TMP_Text targetPromptText;
    //选敌相关
    private readonly List<Enemy> m_selectedEnemies = new List<Enemy>();
    private int m_requiredEnemyCount;
    private bool m_isSelectingEnemies;

    public bool IsSelectingEnemies => m_isSelectingEnemies;
    //选友相关
    private readonly List<Character> m_selectedCharacters = new List<Character>();
    private int m_requiredCharacterCount;
    private bool m_isSelectingCharacters;
    public bool IsSelectingCharacters => m_isSelectingCharacters;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SetPromptVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public IEnumerator SelectEnemiesCoroutine(int requiredCount, List<Enemy> selectedResult)
    {
        int aliveCount = EnemyManager.Instance != null ? EnemyManager.Instance.AliveEnemies.Count : 0;
        int safeRequiredCount = Mathf.Max(1, requiredCount);
        if (aliveCount <= 0)
        {
            Debug.LogWarning("[SkillManager] 当前场上没有敌人，无法进行选敌");
            selectedResult?.Clear();
            yield break;
        }

        if (aliveCount < safeRequiredCount)
        {
            // 场上敌人少于目标数量时，自动下调需求避免协程卡死。
            safeRequiredCount = aliveCount;
        }

        m_requiredEnemyCount = safeRequiredCount;
        m_selectedEnemies.Clear();
        m_isSelectingEnemies = true;
        UpdatePromptText($"请选择技能作用的敌人:{m_selectedEnemies.Count}/{m_requiredEnemyCount}");
        SetPromptVisible(true);

        yield return new WaitUntil(() => m_selectedEnemies.Count >= m_requiredEnemyCount||Input.GetKeyDown(KeyCode.Mouse1));
        selectedResult?.Clear();
        if (selectedResult != null)
        {
            selectedResult.AddRange(m_selectedEnemies);
        }

        var names = new List<string>();
        for (int i = 0; i < m_selectedEnemies.Count; i++)
        {
            if (m_selectedEnemies[i] != null)
            {
                names.Add(m_selectedEnemies[i].name);
            }
        }

        Debug.Log($"[SkillManager] 选中的敌人: {string.Join(", ", names)}");

        ClearSelectionState();
    }

    public void OnEnemyClicked(Enemy enemy)
    {
        if (!m_isSelectingEnemies || enemy == null)
            return;

        if (EnemyManager.Instance != null)
        {
            bool exists = false;
            var aliveEnemies = EnemyManager.Instance.AliveEnemies;
            for (int i = 0; i < aliveEnemies.Count; i++)
            {
                if (aliveEnemies[i] == enemy)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                return;
        }

        if (m_selectedEnemies.Contains(enemy))
        {
            m_selectedEnemies.Remove(enemy);
            enemy.SetSelectedVisual(false);
        }
        else
        {
            if (m_selectedEnemies.Count >= m_requiredEnemyCount)
                return;

            m_selectedEnemies.Add(enemy);
            enemy.SetSelectedVisual(true);
        }

        UpdatePromptText();
    }

    public IEnumerator SelectCharactersCoroutine(int requiredCount, List<Character> selectedResult, Character sourceCharacter = null)
    {
        int aliveCount = CharacterManager.Instance != null ? CharacterManager.Instance.fieldCharacters.Count : 0;
        int safeRequiredCount = Mathf.Max(1, requiredCount);
        if (aliveCount <= 1)
        {
            Debug.LogWarning("[SkillManager] 当前场上没有可作用的我方角色，无法进行选友");
            selectedResult?.Clear();
            yield break;
        }

        if (aliveCount < safeRequiredCount)
        {
            safeRequiredCount = aliveCount;
        }

        m_requiredCharacterCount = safeRequiredCount;
        m_selectedCharacters.Clear();
        m_isSelectingCharacters = true;
        UpdatePromptText($"请选择技能作用的我方角色:{m_selectedCharacters.Count}/{m_requiredCharacterCount}");
        SetPromptVisible(true);
        yield return new WaitUntil(() => m_selectedCharacters.Count >= m_requiredCharacterCount||Input.GetKeyDown(KeyCode.Mouse1));

        selectedResult?.Clear();
        if (selectedResult != null)
        {
            selectedResult.AddRange(m_selectedCharacters);
        }

        ClearSelectionState();

    }

    public void OnCharacterClicked(Character character)
    {
        if (!m_isSelectingCharacters || character == null)
        {
            return;
        }

        if (CharacterManager.Instance != null && !CharacterManager.Instance.fieldCharacters.Contains(character))
        {
            return;
        }

        if (m_selectedCharacters.Contains(character))
        {
            m_selectedCharacters.Remove(character);
            character.SetSelectedVisual(false);
        }
        else
        {
            if (m_selectedCharacters.Count >= m_requiredCharacterCount)
            {
                return;
            }

            m_selectedCharacters.Add(character);
            character.SetSelectedVisual(true);
        }

        UpdatePromptText($"请选择技能作用的我方角色:{m_selectedCharacters.Count}/{m_requiredCharacterCount}");
    }

    private void UpdatePromptText(string text = "")//更新提示文本内容
    {
        if (targetPromptText == null)
            return;

        targetPromptText.text = $"{text}";
    }

    private void SetPromptVisible(bool visible)//设置提示文本是否可见
    {
        if (targetPromptText == null)
            return;

        targetPromptText.gameObject.SetActive(visible);
    }

    private void ClearSelectionState()
    {
        for (int i = 0; i < m_selectedEnemies.Count; i++)
        {
            if (m_selectedEnemies[i] != null)
            {
                m_selectedEnemies[i].SetSelectedVisual(false);
            }
        }

        for (int i = 0; i < m_selectedCharacters.Count; i++)
        {
            if (m_selectedCharacters[i] != null)
            {
                m_selectedCharacters[i].SetSelectedVisual(false);
            }
        }

        m_selectedEnemies.Clear();
        m_selectedCharacters.Clear();
        m_requiredEnemyCount = 0;
        m_requiredCharacterCount = 0;
        m_isSelectingEnemies = false;
        m_isSelectingCharacters = false;
        SetPromptVisible(false);
    }
}

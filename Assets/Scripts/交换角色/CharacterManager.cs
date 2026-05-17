using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterManager : MonoBehaviour
{
	public static CharacterManager Instance { get; private set; }
	public event System.Action<Character, Character> OnFieldCharacterSwapped;

	[Header("角色列表")]
	[Tooltip("所有可用角色，通常在 Inspector 中配置后不再改变")]
	public List<Character> allCharacters = new List<Character>();

	[Tooltip("当前场上角色，会频繁变化")]
	public List<Character> fieldCharacters = new List<Character>();

	[Tooltip("候补角色，会频繁变化")]
	public List<Character> reserveCharacters = new List<Character>();

	[Header("换人提示")]
	public TMP_Text promptText;

	[Header("候补按钮UI")]
	public RectTransform reserveButtonContainer;
	public Button reserveButtonPrefab;
	public float buttonSpacing = 72f;
	public float buttonSlideDuration = 0.2f;
	public float buttonStagger = 0.05f;
	public float hiddenOffsetX = -420f;

	private readonly List<Button> m_runtimeButtons = new List<Button>();

	private bool m_isSelectingFieldCharacter;
	public bool IsSelectingFieldCharacter => m_isSelectingFieldCharacter;
	private bool m_isSelectingReserveCharacter;
	public bool IsSelectingReserveCharacter => m_isSelectingReserveCharacter;
	private Character m_selectedFieldCharacter;
	private Character m_selectedReserveCharacter;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	private void Start()
	{
		SetPromptVisible(false);
		HideReserveButtonsImmediate();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}
	#region  换人流程
	public IEnumerator SelectAndSwapCoroutine()//换人协程，处理整个换人流程
	{
		if (fieldCharacters.Count <= 0)
		{
			Debug.LogWarning("[CharacterManager] 场上角色列表为空，无法执行换人");
			yield break;
		}

		if (reserveCharacters.Count <= 0)
		{
			Debug.LogWarning("[CharacterManager] 候补角色列表为空，无法执行换人");
			yield break;
		}

		m_selectedFieldCharacter = null;
		m_selectedReserveCharacter = null;
		m_isSelectingReserveCharacter = false;
		m_isSelectingFieldCharacter = true;
		//第一步:选择场上的角色
		UpdatePromptText("请选择一个场上角色进行更换");
		SetPromptVisible(true);
		Debug.Log("[CharacterManager] 进入换人流程：请选择一个场上角色进行更换");
		yield return new WaitUntil(() => m_selectedFieldCharacter != null);
		m_isSelectingFieldCharacter = false;
		m_isSelectingReserveCharacter = true;
		//第二步:选择候补角色
		BuildReserveButtons();
		PlayReserveButtonsEnterAnim();
		UpdatePromptText($"请选择替换 {m_selectedFieldCharacter.name} 的候补角色");

		yield return new WaitUntil(() => m_selectedReserveCharacter != null);

		m_isSelectingReserveCharacter = false;

		if (m_selectedFieldCharacter != null)
		{
			m_selectedFieldCharacter.SetSelectedVisual(false);
		}
		//第三步:执行替换
		HideReserveButtonsImmediate();
		yield return StartCoroutine(ReplaceCharacter(m_selectedFieldCharacter, m_selectedReserveCharacter));
		//第四步:清理UI
		SetPromptVisible(false);
	}

	public void OnFieldCharacterClicked(Character character)//处理场上角色被点击的逻辑
	{
		if (!m_isSelectingFieldCharacter || character == null)
		{
			return;
		}

		if (!fieldCharacters.Contains(character))
		{
			return;
		}

		if (m_selectedFieldCharacter != null)
		{
			m_selectedFieldCharacter.SetSelectedVisual(false);
		}

		m_selectedFieldCharacter = character;
		m_selectedFieldCharacter.SetSelectedVisual(true);
	}

	private IEnumerator ReplaceCharacter(Character oldCharacter, Character newCharacter)//执行角色替换
	{
		if (oldCharacter == null || newCharacter == null)
		{
			Debug.LogWarning("[CharacterManager] 替换失败：角色为空");
			yield break;
		}

		if (!fieldCharacters.Contains(oldCharacter) || !reserveCharacters.Contains(newCharacter))
		{
			Debug.LogWarning("[CharacterManager] 替换失败：角色不在正确列表中");
			yield break;
		}
		//设置入场角色位置
		newCharacter.transform.position = oldCharacter.transform.position;
		//执行退场技能
		SkillExecuteManager.ExecuteSkill(oldCharacter, SkillDictionaryManager.GetSkill(oldCharacter.exitSkill));
		yield return new WaitUntil(() => !SkillExecuteManager.s_isExecutingSkill);
		//执行退场动画
		yield return oldCharacter.PlayExitAnimation();
		//交换角色列表中的角色
		int fieldIndex = fieldCharacters.IndexOf(oldCharacter);
		fieldCharacters[fieldIndex] = newCharacter;

		reserveCharacters.Remove(newCharacter);
		reserveCharacters.Add(oldCharacter);
		//执行入场技能
		SkillExecuteManager.ExecuteSkill(newCharacter, SkillDictionaryManager.GetSkill(newCharacter.enterSkill));
		yield return new WaitUntil(() => !SkillExecuteManager.s_isExecutingSkill);
		//执行入场动画
		yield return newCharacter.PlayEnterAnimation();
		//更新 TurnManager 中的角色引用，确保回合顺序正确
		if (TurnManager.Instance != null)
		{
			Debug.Log($"[CharacterManager] 更新 TurnManager 中的角色引用，将 {oldCharacter.name} 替换为 {newCharacter.name}");
			float oldActionValue = oldCharacter.currentActionValue;
			TurnManager.Instance.RemoveCombatant(oldCharacter);
			newCharacter.ChangeActionValue(0f); //换入角色立即插入回合
			TurnManager.Instance.InsertCombatant(newCharacter, false);
		}

		Debug.Log($"[CharacterManager] 已将场上角色 {oldCharacter.name} 替换为 {newCharacter.name}");
		OnFieldCharacterSwapped?.Invoke(oldCharacter, newCharacter);
		yield break;
	}

	private void BuildReserveButtons()//构建候补角色选择按钮
	{
		HideReserveButtonsImmediate();

		if (reserveButtonContainer == null || reserveButtonPrefab == null)
		{
			Debug.LogWarning("[CharacterManager] 未配置候补按钮容器或按钮预制体");
			return;
		}

		for (int i = 0; i < reserveCharacters.Count; i++)
		{
			Character reserve = reserveCharacters[i];
			if (reserve == null)
			{
				continue;
			}

			Button button = Instantiate(reserveButtonPrefab, reserveButtonContainer);
			RectTransform rect = button.GetComponent<RectTransform>();
			if (rect != null)
			{
				rect.anchoredPosition = new Vector2(hiddenOffsetX, -i * buttonSpacing);
			}

			TMP_Text label = button.GetComponentInChildren<TMP_Text>();
			if (label != null)
			{
				label.text = reserve.combatantName;
				if (string.IsNullOrEmpty(label.text))
				{
					label.text = reserve.name;
				}
			}

			Character captured = reserve;
			button.GetComponent<EnterCharacterButton>()?.Initialize(captured);
			button.onClick.AddListener(() => OnReserveButtonClicked(captured));
			m_runtimeButtons.Add(button);
		}
	}

	private void PlayReserveButtonsEnterAnim()
	{
		for (int i = 0; i < m_runtimeButtons.Count; i++)
		{
			Button button = m_runtimeButtons[i];
			if (button == null)
			{
				continue;
			}

			RectTransform rect = button.GetComponent<RectTransform>();
			if (rect == null)
			{
				continue;
			}

			Vector2 targetPosition = new Vector2(0f, -i * buttonSpacing);
			rect.DOAnchorPos(targetPosition, buttonSlideDuration)
				.SetDelay(i * buttonStagger)
				.SetEase(Ease.OutCubic);
		}
	}

	private void OnReserveButtonClicked(Character reserveCharacter)
	{
		if (!m_isSelectingReserveCharacter || reserveCharacter == null)
		{
			return;
		}

		m_selectedReserveCharacter = reserveCharacter;
		SkillDescription.Instance.ChangeDescription(null);
	}

	private void HideReserveButtonsImmediate()
	{
		for (int i = 0; i < m_runtimeButtons.Count; i++)
		{
			if (m_runtimeButtons[i] != null)
			{
				Destroy(m_runtimeButtons[i].gameObject);
			}
		}

		m_runtimeButtons.Clear();
	}
	#endregion
	private void UpdatePromptText(string text)
	{
		if (promptText == null)
		{
			return;
		}

		promptText.text = text;
	}

	private void SetPromptVisible(bool visible)
	{
		if (promptText == null)
		{
			return;
		}

		promptText.gameObject.SetActive(visible);
	}

	public void InitializeCharacters(List<Character> allRuntimeCharacters, List<Character> fieldRuntimeCharacters)
	{
		allCharacters = allRuntimeCharacters != null
			? new List<Character>(allRuntimeCharacters)
			: new List<Character>();

		fieldCharacters = fieldRuntimeCharacters != null
			? new List<Character>(fieldRuntimeCharacters)
			: new List<Character>();

		reserveCharacters = new List<Character>();
		for (int i = 0; i < allCharacters.Count; i++)
		{
			Character character = allCharacters[i];
			if (character == null || fieldCharacters.Contains(character))
			{
				continue;
			}

			reserveCharacters.Add(character);
		}
	}

	#region 角色相关工具
	public Character GetCharacterByRand()
	{
		float totalWeight = 0f;
		foreach (var character in fieldCharacters)
		{
			totalWeight += character.GetAttractCount();
		}
		float rand = Random.Range(0f, totalWeight);
		float cumulativeWeight = 0f;
		foreach (var character in fieldCharacters)
		{
			cumulativeWeight += character.GetAttractCount();
			if (rand <= cumulativeWeight)
			{
				return character;
			}
		}
		return null;
	}
	#endregion
}

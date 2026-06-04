using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterManager : MonoBehaviour
{
	public static CharacterManager Instance { get; private set; }
	/// <summary>换人完成时的静态事件（供教程系统监听）</summary>
	public static event System.Action SwapCompleted;
	public event System.Action<Character, Character> OnFieldCharacterSwapped;
	public event System.Action OnFieldCharactersReordered;
	public event System.Action OnReserveSwapAvailabilityChanged;

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
	private readonly List<Character> m_boundReserveCharacters = new List<Character>();

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
		UnbindReserveCharacterEvents();

		if (Instance == this)
		{
			Instance = null;
		}
	}
	#region  换人流程
	public IEnumerator SelectAndSwapCoroutine(Character selectedFieldCharacter = null)//换人协程，处理整个换人流程
	{
		if (fieldCharacters.Count <= 0)
		{
			Debug.LogWarning("[CharacterManager] 场上角色列表为空，无法执行换人");
			yield break;
		}

		if (!CanStartSwapFlow())
		{
			Debug.LogWarning(reserveCharacters.Count <= 0
				? "[CharacterManager] 候补角色列表为空，无法执行换人"
				: "[CharacterManager] 所有候补角色均处于换人冷却中，无法执行换人");
			yield break;
		}

		m_selectedFieldCharacter = selectedFieldCharacter;
		m_selectedReserveCharacter = null;
		m_isSelectingReserveCharacter = false;
		m_isSelectingFieldCharacter = true;
		//第一步:选择场上的角色
		if (selectedFieldCharacter == null)
		{
			UpdatePromptText("请选择一个场上角色进行更换");
			SetPromptVisible(true);
			Debug.Log("[CharacterManager] 进入换人流程：请选择一个场上角色进行更换");
			yield return new WaitUntil(() => m_selectedFieldCharacter != null || Input.GetKeyDown(KeyCode.Mouse1));
			if (m_selectedFieldCharacter == null)
			{
				Debug.Log("[CharacterManager] 换人流程取消：未选择场上角色");
				SetPromptVisible(false);
				yield break;
			}
		}
		m_isSelectingFieldCharacter = false;
		m_isSelectingReserveCharacter = true;
		//第二步:选择候补角色
		BuildReserveButtons();
		PlayReserveButtonsEnterAnim();
		UpdatePromptText($"请选择替换 {m_selectedFieldCharacter.combatantName} 的候补角色");

		yield return new WaitUntil(() => m_selectedReserveCharacter != null || Input.GetKeyDown(KeyCode.Mouse1));
		if (m_selectedReserveCharacter == null)
		{
			Debug.Log("[CharacterManager] 换人流程取消：未选择候补角色");
			HideReserveButtonsImmediate();
			SetPromptVisible(false);
			yield break;
		}

		m_isSelectingReserveCharacter = false;

		if (m_selectedFieldCharacter != null)
		{
			m_selectedFieldCharacter.SetSelectedVisual(false);
		}
		//第三步:执行替换
		HideReserveButtonsImmediate();
		yield return StartCoroutine(ReplaceCharacter(m_selectedFieldCharacter, m_selectedReserveCharacter, true));
		//第四步:清理UI
		SetPromptVisible(false);
		m_selectedFieldCharacter = null;
		m_selectedReserveCharacter = null;
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

	public Character GetFieldCharacterByStandPosition(int standPosition)
	{
		for (int i = 0; i < fieldCharacters.Count; i++)
		{
			Character character = fieldCharacters[i];
			if (character != null && character.standPosition == standPosition)
			{
				return character;
			}
		}

		return null;
	}

	public bool SwapFieldCharacters(Character first, Character second)
	{
		if (first == null || second == null || first == second)
		{
			return false;
		}

		int firstIndex = fieldCharacters.IndexOf(first);
		int secondIndex = fieldCharacters.IndexOf(second);
		if (firstIndex < 0 || secondIndex < 0)
		{
			return false;
		}

		Vector3 firstPosition = first.transform.position;
		int firstStandPosition = first.standPosition;

		first.transform.position = second.transform.position;
		second.transform.position = firstPosition;

		first.standPosition = second.standPosition;
		second.standPosition = firstStandPosition;

		fieldCharacters[firstIndex] = second;
		fieldCharacters[secondIndex] = first;

		first.ChangeActionValue(first.currentActionValue);
		second.ChangeActionValue(second.currentActionValue);
		OnFieldCharactersReordered?.Invoke();
		return true;
	}

	private IEnumerator ReplaceCharacter(Character oldCharacter, Character newCharacter, bool applySwapOutPenalty)//执行角色替换
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
		//设置入场角色位置与站位
		var exitPosition = newCharacter.transform.position;
		int targetStandPosition = oldCharacter.standPosition;
		newCharacter.standPosition = targetStandPosition;
		if (LevelCharacterSpawner.TryGetSpawnPosition(targetStandPosition, out Vector3 spawnPosition))
		{
			newCharacter.transform.position = spawnPosition;
		}
		else
		{
			Debug.LogWarning($"[CharacterManager] 替换警告：无法找到站位 {targetStandPosition} 的出生点，入场角色将直接出现在退场角色位置");
			newCharacter.transform.position = oldCharacter.transform.position;
		}
		oldCharacter.transform.position = exitPosition;
		//执行退场动画
		if (TurnManager.Instance != null)
		{
			TurnManager.Instance.RemoveCombatant(oldCharacter);
		}
		yield return oldCharacter.PlayExitAnimation();
		oldCharacter.transform.position = exitPosition;
		//交换角色列表中的角色
		int fieldIndex = fieldCharacters.IndexOf(oldCharacter);
		fieldCharacters[fieldIndex] = newCharacter;
		if (applySwapOutPenalty)
		{
			oldCharacter.TriggerSwapCooldown();
			oldCharacter.ReduceChaos(3);
		}

		reserveCharacters.Remove(newCharacter);
		reserveCharacters.Add(oldCharacter);
		RefreshReserveCharacterBindings();
		OnReserveSwapAvailabilityChanged?.Invoke();
		newCharacter.ChangeActionValue(newCharacter.BaseActionValue, false);
		//执行入场动画
		yield return newCharacter.PlayEnterAnimation();
		//执行入场技能
		SkillExecuteManager.ExecuteSkill(newCharacter, newCharacter.GetEnterSkillInstance(), true);
		yield return new WaitUntil(() => !SkillExecuteManager.s_isExecutingSkill);
		//更新 TurnManager 中的角色引用，确保回合顺序正确
		if (TurnManager.Instance != null)
		{
			Debug.Log($"[CharacterManager] 更新 TurnManager 中的角色引用，将 {oldCharacter.name} 替换为 {newCharacter.name}");
			TurnManager.Instance.InsertCombatant(newCharacter);
		}

		Debug.Log($"[CharacterManager] 已将场上角色 {oldCharacter.name} 替换为 {newCharacter.name}");

		// 通知状态系统角色交换（王棋/车棋等需要转移的状态）
		oldCharacter.NotifyStatesOwnerSwappedOut(newCharacter);

		BattleRuntimeEvents.RaisePlayerCharacterSwapped();
		TemporaryBattleModifierRuntimeManager.NotifyPlayerCharacterSwapped(oldCharacter, newCharacter);
		OnFieldCharacterSwapped?.Invoke(oldCharacter, newCharacter);
		SwapCompleted?.Invoke();
		yield break;
	}

	public bool TryAutoSwapToFirstReserve(Character oldCharacter, bool applySwapOutPenalty = false)
	{
		if (oldCharacter == null || m_isSelectingFieldCharacter || m_isSelectingReserveCharacter)
		{
			return false;
		}

		Character reserveCharacter = GetFirstAvailableReserveCharacter(requireAvailableForSwap: true);
		if (reserveCharacter == null || !fieldCharacters.Contains(oldCharacter) || !reserveCharacters.Contains(reserveCharacter))
		{
			return false;
		}

		StartCoroutine(ReplaceCharacter(oldCharacter, reserveCharacter, applySwapOutPenalty));
		return true;
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
			button.interactable = !captured.IsSwapOnCooldown;
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

			RefreshReserveCharacterBindings();
	}

		private void RefreshReserveCharacterBindings()
		{
			UnbindReserveCharacterEvents();

			for (int i = 0; i < reserveCharacters.Count; i++)
			{
				Character reserveCharacter = reserveCharacters[i];
				if (reserveCharacter == null)
				{
					continue;
				}

				reserveCharacter.OnSwapCooldownAvailabilityChanged += HandleReserveSwapCooldownAvailabilityChanged;
				m_boundReserveCharacters.Add(reserveCharacter);
			}
		}

		private void UnbindReserveCharacterEvents()
		{
			for (int i = 0; i < m_boundReserveCharacters.Count; i++)
			{
				Character reserveCharacter = m_boundReserveCharacters[i];
				if (reserveCharacter == null)
				{
					continue;
				}

				reserveCharacter.OnSwapCooldownAvailabilityChanged -= HandleReserveSwapCooldownAvailabilityChanged;
			}

			m_boundReserveCharacters.Clear();
		}

		private void HandleReserveSwapCooldownAvailabilityChanged(Character reserveCharacter)
		{
			OnReserveSwapAvailabilityChanged?.Invoke();
		}

	#region 角色相关工具
	public Character GetPendingSwapInCharacter(Character swappingOutCharacter = null)
	{
		if (m_selectedReserveCharacter == null)
		{
			return null;
		}

		if (swappingOutCharacter != null && m_selectedFieldCharacter != null && m_selectedFieldCharacter != swappingOutCharacter)
		{
			return null;
		}

		return m_selectedReserveCharacter;
	}

	public Character GetAnotherFieldCharacter(Character character)
	{
		foreach (var c in fieldCharacters)
		{
			if (c != null && c != character)
			{
				return c;
			}
		}
		return null;
	}
	public Character GetCharacterByRand()
	{
		float totalWeight = 0f;
		foreach (var character in fieldCharacters)
		{
			if (character == null || character.IsDead)
			{
				continue;
			}

			totalWeight += character.GetAttractCount();
		}

		if (totalWeight <= 0f)
		{
			return null;
		}

		float rand = Random.Range(0f, totalWeight);
		float cumulativeWeight = 0f;
		foreach (var character in fieldCharacters)
		{
			if (character == null || character.IsDead)
			{
				continue;
			}

			cumulativeWeight += character.GetAttractCount();
			if (rand <= cumulativeWeight)
			{
				return character;
			}
		}
		return null;
	}

	public bool CanStartSwapFlow()
	{
		return GetFirstAvailableReserveCharacter(requireAvailableForSwap: true) != null;
	}

	private Character GetFirstAvailableReserveCharacter(bool requireAvailableForSwap = false)
	{
		for (int i = 0; i < reserveCharacters.Count; i++)
		{
			Character reserveCharacter = reserveCharacters[i];
			if (reserveCharacter == null)
			{
				continue;
			}

			if (requireAvailableForSwap && reserveCharacter.IsSwapOnCooldown)
			{
				continue;
			}

			if (reserveCharacter != null)
			{
				return reserveCharacter;
			}
		}

		return null;
	}
	#endregion
}

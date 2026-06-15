using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum ManagedCameraType
{
	None = 0,
	Main = 1,
	Attack = 2,
	Help = 3,
	Settlement = 4,
}

public class CinemachineCameraManager : MonoBehaviour
{
	public static CinemachineCameraManager Instance { get; private set; }

	[Serializable]
	public class CameraEntry
	{
		public ManagedCameraType cameraType;
		public CinemachineVirtualCameraBase virtualCamera;
	}

	[Header("相机列表")]
	[SerializeField] private List<CameraEntry> cameras = new List<CameraEntry>();
	[SerializeField] private ManagedCameraType defaultCamera = ManagedCameraType.Main;
	[Header("Main 相机晃动")]
	[SerializeField] private bool enableMainCameraSway = true;
	[SerializeField] private float mainCameraSwayAmplitude = 0.15f;
	[SerializeField] private float mainCameraSwayFrequency = 0.35f;
	[SerializeField] [Min(0f)] private float mainCameraSwayEdgeHoldDuration = 0.12f;
	[SerializeField] [Range(0f, 1f)] private float mainCameraSwayBreathBlend = 0.2f;
	[SerializeField] private bool enableMainCameraVerticalFloat = true;
	[SerializeField] private float mainCameraVerticalFloatAmplitude = 0.03f;
	[SerializeField] private bool enableMainCameraRotation = true;
	[SerializeField] private float mainCameraRotationAmplitude = 0.5f;
	[Header("Main 相机 Vignette 联动")]
	[SerializeField] private bool enableMainCameraVignettePulse = true;
	[SerializeField] private Volume globalVolume;
	[SerializeField] [Range(0f, 1f)] private float mainCameraVignetteBaseIntensity = 0f;
	[SerializeField] private float mainCameraVignettePulseMinimum = 0f;
	[SerializeField] private float mainCameraVignettePulseAmplitude = 0.08f;
	[Header("开场镜头过渡")]
	[SerializeField] private bool playOpeningIntroOnStart = true;
	[SerializeField] private CinemachineVirtualCameraBase openingStartCamera;
	[SerializeField] [Min(0f)] private float openingIntroDuration = 1.2f;
	[SerializeField] [Range(0f, 1f)] private float openingIntroStartVignetteIntensity = 0.45f;
	[Header("技能镜头 Vignette 过渡")]
	[SerializeField] private bool enableSkillCameraVignetteTransition = true;
	[SerializeField] [Min(0f)] private float skillCameraTransitionDuration = 0.35f;
	[SerializeField] [Range(0f, 1f)] private float skillCameraVignetteIntensity = 0.32f;
	[Header("结算镜头 Vignette 过渡")]
	[SerializeField] private bool enableSettlementCameraVignetteTransition = true;
	[SerializeField] [Min(0f)] private float settlementCameraTransitionDuration = 0.45f;
	[SerializeField] [Range(0f, 1f)] private float settlementCameraVignetteIntensity = 0.4f;

	private readonly Dictionary<ManagedCameraType, CinemachineVirtualCameraBase> m_cameraMap =
		new Dictionary<ManagedCameraType, CinemachineVirtualCameraBase>();
	private readonly Dictionary<ManagedCameraType, Vector3> m_cameraInitialLocalPositions =
		new Dictionary<ManagedCameraType, Vector3>();
	private readonly Dictionary<ManagedCameraType, Quaternion> m_cameraInitialLocalRotations =
		new Dictionary<ManagedCameraType, Quaternion>();

	private Vignette m_mainCameraVignette;
	private bool m_skillVignetteOverrideActive;
	private bool m_fieldDomainVignetteSuppressed;
	private float m_mainCameraSwayCycleStartTime;
	private bool m_isPlayingOpeningIntro;
	private bool m_hasCompletedOpeningIntro;
	private bool m_isSwayPaused;
	private float m_pausedSwayElapsed;
	public bool isOP=>m_isPlayingOpeningIntro;
	public bool HasCompletedOpeningIntro => m_hasCompletedOpeningIntro;

	public void SetFieldDomainVignetteSuppressed(bool suppressed)
	{
		m_fieldDomainVignetteSuppressed = suppressed;

		if (!suppressed && !m_skillVignetteOverrideActive)
		{
			ApplyMainCameraMinimumVignette();
		}
	}

	/// <summary>
	/// 暂停摄像机摆动动画，相机保持在当前位置
	/// </summary>
	public void PauseSway()
	{
		m_isSwayPaused = true;
		m_pausedSwayElapsed = Mathf.Max(0f, Time.time - m_mainCameraSwayCycleStartTime);
	}

	/// <summary>
	/// 继续摄像机摆动动画，从暂停时的相位恢复
	/// </summary>
	public void ResumeSway()
	{
		m_isSwayPaused = false;
		m_mainCameraSwayCycleStartTime = Time.time - m_pausedSwayElapsed;
	}

	public ManagedCameraType CurrentCameraType { get; private set; } = ManagedCameraType.None;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		m_hasCompletedOpeningIntro = !playOpeningIntroOnStart;
		RebuildCameraMap();
	}

	private void Start()
	{
		if (playOpeningIntroOnStart)
		{
			StartCoroutine(PlayOpeningIntro());
		}
		else if (defaultCamera != ManagedCameraType.None)
		{
			SwitchCamera(defaultCamera);
		}
		else
		{
			DisableAllCameras();
		}
	}

	private void LateUpdate()
	{
		UpdateMainCameraSway();
	}

	private void OnDestroy()
	{
		ResetMainCameraSway();

		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void SwitchCamera(ManagedCameraType cameraType)
	{
		if (CurrentCameraType != cameraType)
		{
			ResetMainCameraSway();
		}

		if (cameraType == ManagedCameraType.None)
		{
			DisableAllCameras();
			CurrentCameraType = ManagedCameraType.None;
			return;
		}

		if (!m_cameraMap.TryGetValue(cameraType, out CinemachineVirtualCameraBase targetCamera) || targetCamera == null)
		{
			Debug.LogWarning($"[CinemachineCameraManager] 未找到枚举 {cameraType} 对应的虚拟摄像机");
			return;
		}

		foreach (KeyValuePair<ManagedCameraType, CinemachineVirtualCameraBase> pair in m_cameraMap)
		{
			if (pair.Value != null)
			{
				pair.Value.gameObject.SetActive(pair.Key == cameraType);
			}
		}

		CurrentCameraType = cameraType;
		if (cameraType == ManagedCameraType.Main)
		{
			RestartMainCameraSwayCycle();
		}
	}
	public IEnumerator TransitionIntoSkillCamera(ManagedCameraType skillCameraType)
	{
		Debug.Log("[CinemachineCameraManager] 过渡进入技能镜头");
		SwitchCamera(skillCameraType);
		yield return AnimateSkillCameraVignette(skillCameraVignetteIntensity, true);
	}

	public IEnumerator TransitionOutOfSkillCamera()
	{
		CacheVignetteReference();
		SwitchCamera(ManagedCameraType.Main);
		yield return AnimateSkillCameraVignette(GetMainCameraMinimumVignetteIntensity(), false);
		RestartMainCameraSwayCycle();
	}

	public IEnumerator TransitionIntoSettlementCamera(ManagedCameraType settlementCameraType)
	{
		SwitchCamera(settlementCameraType);
		yield return AnimateVignetteOverride(
			settlementCameraVignetteIntensity,
			settlementCameraTransitionDuration,
			enableSettlementCameraVignetteTransition,
			true);
	}

	public CinemachineVirtualCameraBase GetCamera(ManagedCameraType cameraType)
	{
		m_cameraMap.TryGetValue(cameraType, out CinemachineVirtualCameraBase virtualCamera);
		return virtualCamera;
	}
#region 开场镜头过渡
	public IEnumerator PlayOpeningIntro()
	{
		if (m_isPlayingOpeningIntro)
		{
			yield break;
		}

		if (!m_cameraMap.TryGetValue(ManagedCameraType.Main, out CinemachineVirtualCameraBase mainCamera) || mainCamera == null)
		{
			if (defaultCamera != ManagedCameraType.None)
			{
				SwitchCamera(defaultCamera);
			}
			m_hasCompletedOpeningIntro = true;
			yield break;
		}

		if (openingStartCamera == null)
		{
			SwitchCamera(ManagedCameraType.Main);
			m_hasCompletedOpeningIntro = true;
			yield break;
		}

		m_hasCompletedOpeningIntro = false;
		m_isPlayingOpeningIntro = true;
		CacheVignetteReference();
		float startVignette = Mathf.Clamp01(openingIntroStartVignetteIntensity);
		if (m_mainCameraVignette != null)
		{
			m_skillVignetteOverrideActive = true;
			m_mainCameraVignette.intensity.Override(startVignette);
		}

		SwitchCamera(ManagedCameraType.Main);
		ResetMainCameraTransform();

		Transform mainParent = mainCamera.transform.parent;
		Vector3 targetLocalPosition = mainCamera.transform.localPosition;
		Quaternion targetLocalRotation = mainCamera.transform.localRotation;
		Vector3 startLocalPosition = mainParent != null
			? mainParent.InverseTransformPoint(openingStartCamera.transform.position)
			: openingStartCamera.transform.position;
		Quaternion startLocalRotation = mainParent != null
			? Quaternion.Inverse(mainParent.rotation) * openingStartCamera.transform.rotation
			: openingStartCamera.transform.rotation;
		mainCamera.transform.SetLocalPositionAndRotation(startLocalPosition, startLocalRotation);

		float duration = Mathf.Max(0f, openingIntroDuration);

		if (duration <= Mathf.Epsilon)
		{
			mainCamera.transform.SetLocalPositionAndRotation(targetLocalPosition, targetLocalRotation);
			if (m_mainCameraVignette != null)
			{
				m_mainCameraVignette.intensity.Override(GetMainCameraMinimumVignetteIntensity());
			}
			m_skillVignetteOverrideActive = false;
			RestartMainCameraSwayCycle();
			m_isPlayingOpeningIntro = false;
			m_hasCompletedOpeningIntro = true;
			yield break;
		}

		startVignette = m_mainCameraVignette != null ? startVignette : 0f;
		float targetVignette = GetMainCameraMinimumVignetteIntensity();
		float elapsed = 0f;
		while (elapsed < duration)
		{
			float progress = Mathf.Clamp01(elapsed / duration);
			float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
			Vector3 currentLocalPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, easedProgress);
			Quaternion currentLocalRotation = Quaternion.Slerp(startLocalRotation, targetLocalRotation, easedProgress);
			mainCamera.transform.SetLocalPositionAndRotation(currentLocalPosition, currentLocalRotation);

			if (m_mainCameraVignette != null)
			{
				float currentVignette = Mathf.Lerp(startVignette, targetVignette, easedProgress);
				m_mainCameraVignette.intensity.Override(currentVignette);
			}

			yield return null;
			elapsed += Time.deltaTime;
		}

		mainCamera.transform.SetLocalPositionAndRotation(targetLocalPosition, targetLocalRotation);
		if (m_mainCameraVignette != null)
		{
			m_mainCameraVignette.intensity.Override(GetMainCameraMinimumVignetteIntensity());
		}

		m_skillVignetteOverrideActive = false;
		RestartMainCameraSwayCycle();
		m_isPlayingOpeningIntro = false;
		m_hasCompletedOpeningIntro = true;
	}
#endregion
	public void RebuildCameraMap()
	{
		m_cameraMap.Clear();
		m_cameraInitialLocalPositions.Clear();
		m_cameraInitialLocalRotations.Clear();

		for (int i = 0; i < cameras.Count; i++)
		{
			CameraEntry entry = cameras[i];
			if (entry == null || entry.cameraType == ManagedCameraType.None || entry.virtualCamera == null)
			{
				continue;
			}

			if (m_cameraMap.ContainsKey(entry.cameraType))
			{
				Debug.LogWarning($"[CinemachineCameraManager] 枚举 {entry.cameraType} 重复配置，已忽略后续相机");
				continue;
			}

			m_cameraMap.Add(entry.cameraType, entry.virtualCamera);
			m_cameraInitialLocalPositions[entry.cameraType] = entry.virtualCamera.transform.localPosition;
			m_cameraInitialLocalRotations[entry.cameraType] = entry.virtualCamera.transform.localRotation;
		}

		CacheVignetteReference();
	}

	private void DisableAllCameras()
	{
		ResetMainCameraSway();

		foreach (KeyValuePair<ManagedCameraType, CinemachineVirtualCameraBase> pair in m_cameraMap)
		{
			if (pair.Value != null)
			{
				pair.Value.gameObject.SetActive(false);
			}
		}
	}
	
	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			CacheVignetteReference();
			return;
		}

		RebuildCameraMap();
	}

	private void UpdateMainCameraSway()//计算主摄像头的晃动效果
	{
		if (m_isPlayingOpeningIntro || m_isSwayPaused)
		{
			return;
		}

		if (!enableMainCameraSway || CurrentCameraType != ManagedCameraType.Main)
		{
			ResetMainCameraSway();
			return;
		}

		if (!m_cameraMap.TryGetValue(ManagedCameraType.Main, out CinemachineVirtualCameraBase mainCamera) || mainCamera == null)
		{
			return;
		}

		if (!m_cameraInitialLocalPositions.TryGetValue(ManagedCameraType.Main, out Vector3 initialLocalPosition))
		{
			initialLocalPosition = mainCamera.transform.localPosition;
			m_cameraInitialLocalPositions[ManagedCameraType.Main] = initialLocalPosition;
		}

		if (!m_cameraInitialLocalRotations.TryGetValue(ManagedCameraType.Main, out Quaternion initialLocalRotation))
		{
			initialLocalRotation = mainCamera.transform.localRotation;
			m_cameraInitialLocalRotations[ManagedCameraType.Main] = initialLocalRotation;
		}

		float elapsed = Mathf.Max(0f, Time.time - m_mainCameraSwayCycleStartTime);
		ApplyMainCameraSwayNormally(mainCamera, initialLocalPosition, elapsed);
	}

	private void ApplyMainCameraSway(
		CinemachineVirtualCameraBase mainCamera,
		Vector3 initialLocalPosition,
		Quaternion initialLocalRotation,
		float elapsed)
	{
		float cycle = elapsed * mainCameraSwayFrequency * Mathf.PI;
		float horizontalWave = Mathf.Sin(cycle);
		float breathValue = 0.5f - 0.5f * Mathf.Cos(cycle);//0~1周期性变化的值，用于模拟呼吸对晃动的影响
		float breathEnvelope = Mathf.Lerp(
			1f - mainCameraSwayBreathBlend,
			1f,
			breathValue);//呼吸波动的强度参数
		float breathingEase = EvaluateBreathingEase(breathValue);
		float swayOffset = horizontalWave * mainCameraSwayAmplitude * breathEnvelope;//决定水平晃动的偏移量
		float verticalOffset = enableMainCameraVerticalFloat
			? EvaluateVerticalBreathingOffset(breathingEase)
			: 0f;
		float rotationOffset = enableMainCameraRotation
			? horizontalWave * mainCameraRotationAmplitude * breathEnvelope
			: 0f;

		Vector3 positionOffset = Vector3.right * swayOffset + Vector3.up * verticalOffset;
		mainCamera.transform.localPosition = initialLocalPosition + positionOffset;
		mainCamera.transform.localRotation = initialLocalRotation * Quaternion.Euler(0f, 0f, rotationOffset);
		UpdateMainCameraVignette(1f - breathValue);
	}
	private void ApplyMainCameraSwayNormally(CinemachineVirtualCameraBase mainCamera, Vector3 initialLocalPosition,float elapsed)
	{
		float normalizedOffset = EvaluateHeldSineOffset(elapsed);
		float swayOffset = normalizedOffset * mainCameraSwayAmplitude;
		Vector3 positionOffset = Vector3.right * swayOffset; 
		mainCamera.transform.localPosition = initialLocalPosition + positionOffset;
		UpdateMainCameraVignette(0.5f - 0.5f * normalizedOffset);
	}

	private float EvaluateHeldSineOffset(float elapsed)
	{
		float safeFrequency = Mathf.Max(0.0001f, mainCameraSwayFrequency);
		float moveDuration = 2f / safeFrequency;
		float halfEdgeMoveDuration = moveDuration * 0.25f;
		float fullEdgeMoveDuration = moveDuration * 0.5f;
		float holdDuration = Mathf.Max(0f, mainCameraSwayEdgeHoldDuration);
		float totalDuration = moveDuration + holdDuration * 2f;
		float cycleTime = Mathf.Repeat(elapsed, totalDuration);

		if (cycleTime < halfEdgeMoveDuration)
		{
			float progress = cycleTime / halfEdgeMoveDuration;
			return Mathf.Sin(progress * Mathf.PI * 0.5f);
		}

		cycleTime -= halfEdgeMoveDuration;
		if (cycleTime < holdDuration)
		{
			return 1f;
		}

		cycleTime -= holdDuration;
		if (cycleTime < fullEdgeMoveDuration)
		{
			float progress = cycleTime / fullEdgeMoveDuration;
			return Mathf.Cos(progress * Mathf.PI);
		}

		cycleTime -= fullEdgeMoveDuration;
		if (cycleTime < holdDuration)
		{
			return -1f;
		}

		cycleTime -= holdDuration;
		if (halfEdgeMoveDuration <= Mathf.Epsilon)
		{
			return 0f;
		}

		float finalProgress = cycleTime / halfEdgeMoveDuration;
		return -Mathf.Cos(finalProgress * Mathf.PI * 0.5f);
	}

	private void RestartMainCameraSwayCycle()
	{
		m_mainCameraSwayCycleStartTime = Time.time;

		if (!m_cameraMap.TryGetValue(ManagedCameraType.Main, out CinemachineVirtualCameraBase mainCamera) || mainCamera == null)
		{
			return;
		}

		if (!m_cameraInitialLocalPositions.TryGetValue(ManagedCameraType.Main, out Vector3 initialLocalPosition))
		{
			initialLocalPosition = mainCamera.transform.localPosition;
			m_cameraInitialLocalPositions[ManagedCameraType.Main] = initialLocalPosition;
		}

		if (!m_cameraInitialLocalRotations.TryGetValue(ManagedCameraType.Main, out Quaternion initialLocalRotation))
		{
			initialLocalRotation = mainCamera.transform.localRotation;
			m_cameraInitialLocalRotations[ManagedCameraType.Main] = initialLocalRotation;
		}

		if (!enableMainCameraSway)
		{
			ResetMainCameraTransform();
			if (!m_skillVignetteOverrideActive)
			{
				ApplyMainCameraMinimumVignette();
			}
			return;
		}

		mainCamera.transform.localPosition = initialLocalPosition;
		mainCamera.transform.localRotation = initialLocalRotation;
		if (!m_skillVignetteOverrideActive)
		{
			ApplyMainCameraMinimumVignette();
		}
	}

	private void ResetMainCameraSway()//重置主摄像机的运动参数
	{
		ResetMainCameraTransform();
		if (!m_skillVignetteOverrideActive)
		{
			ResetMainCameraVignette();
		}
	}

	private void ResetMainCameraTransform()
	{
		if (!m_cameraMap.TryGetValue(ManagedCameraType.Main, out CinemachineVirtualCameraBase mainCamera) || mainCamera == null)
		{
			return;
		}

		if (!m_cameraInitialLocalPositions.TryGetValue(ManagedCameraType.Main, out Vector3 initialLocalPosition))
		{
			m_cameraInitialLocalPositions[ManagedCameraType.Main] = mainCamera.transform.localPosition;
		}

		mainCamera.transform.localPosition = initialLocalPosition;

		if (!m_cameraInitialLocalRotations.TryGetValue(ManagedCameraType.Main, out Quaternion initialLocalRotation))
		{
			m_cameraInitialLocalRotations[ManagedCameraType.Main] = mainCamera.transform.localRotation;
		}
		else
		{
			mainCamera.transform.localRotation = initialLocalRotation;
		}
	}

	private void UpdateMainCameraVignette(float horizontalProgress)
	{
		if (m_skillVignetteOverrideActive || m_fieldDomainVignetteSuppressed)
		{
			return;
		}

		if (!enableMainCameraVignettePulse)
		{
			ResetMainCameraVignette();
			return;
		}

		CacheVignetteReference();
		if (m_mainCameraVignette == null)
		{
			return;
		}

		horizontalProgress = Mathf.Clamp01(horizontalProgress);
		float leftWeightedProgress = 1f - horizontalProgress;
		float pulseMinimum = GetMainCameraVignettePulseMinimum();
		float pulseMaximum = GetMainCameraVignettePulseMaximum();
		float pulseValue = Mathf.Lerp(pulseMinimum, pulseMaximum, leftWeightedProgress);
		float targetIntensity = GetMainCameraBaseVignetteIntensity() + pulseValue;
		m_mainCameraVignette.intensity.Override(Mathf.Clamp01(targetIntensity));
	}

	private float GetMainCameraBaseVignetteIntensity()
	{
		return Mathf.Clamp01(mainCameraVignetteBaseIntensity);
	}

	private float GetMainCameraVignettePulseMinimum()
	{
		return Mathf.Max(0f, Mathf.Min(mainCameraVignettePulseMinimum, mainCameraVignettePulseAmplitude));
	}

	private float GetMainCameraVignettePulseMaximum()
	{
		float pulseMinimum = GetMainCameraVignettePulseMinimum();
		return Mathf.Max(pulseMinimum, mainCameraVignettePulseAmplitude);
	}

	private float GetMainCameraMinimumVignetteIntensity()
	{
		if (!enableMainCameraVignettePulse)
		{
			return GetMainCameraBaseVignetteIntensity();
		}

		return Mathf.Clamp01(GetMainCameraBaseVignetteIntensity() + GetMainCameraVignettePulseMinimum());
	}

	private void ApplyMainCameraMinimumVignette()
	{
		if (m_mainCameraVignette == null)
		{
			return;
		}

		m_mainCameraVignette.intensity.Override(GetMainCameraMinimumVignetteIntensity());
	}

	private void ResetMainCameraVignette()//重置主摄像机的晕影参数
	{
		if (m_mainCameraVignette == null)
		{
			return;
		}

		m_mainCameraVignette.intensity.Override(GetMainCameraBaseVignetteIntensity());
	}

	private void CacheVignetteReference()//获取引用
	{
		if (globalVolume == null && GameManager.Instance != null)
		{
			globalVolume = GameManager.Instance.globalVolume;
		}

		if (globalVolume == null)
		{
			m_mainCameraVignette = null;
			return;
		}

		VolumeProfile profile = globalVolume.profile;
		if (profile == null || !profile.TryGet(out Vignette vignette) || vignette == null)
		{
			m_mainCameraVignette = null;
			return;
		}

		if (m_mainCameraVignette != vignette)
		{
			m_mainCameraVignette = vignette;
		}
	}

	private IEnumerator AnimateSkillCameraVignette(float targetIntensity, bool keepOverrideAfterFinish)//技能镜头晕影动画
	{
		yield return AnimateVignetteOverride(
			targetIntensity,
			skillCameraTransitionDuration,
			enableSkillCameraVignetteTransition,
			keepOverrideAfterFinish);
	}

	private IEnumerator AnimateVignetteOverride(float targetIntensity, float duration, bool enableTransition, bool keepOverrideAfterFinish)
	{
		CacheVignetteReference();
		duration = Mathf.Max(0f, duration);
		if (!enableTransition || m_mainCameraVignette == null)
		{
			if (duration > 0f)
			{
				yield return new WaitForSeconds(duration);
			}
			yield break;
		}

		m_skillVignetteOverrideActive = true;
		float startIntensity = m_mainCameraVignette.intensity.value;
		targetIntensity = Mathf.Clamp01(targetIntensity);

		if (duration <= Mathf.Epsilon)
		{
			m_mainCameraVignette.intensity.Override(targetIntensity);
			m_skillVignetteOverrideActive = keepOverrideAfterFinish;
			yield break;
		}

		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(elapsed / duration);
			float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
			float intensity = Mathf.Lerp(startIntensity, targetIntensity, easedProgress);
			m_mainCameraVignette.intensity.Override(intensity);
			yield return null;
		}

		m_mainCameraVignette.intensity.Override(targetIntensity);
		m_skillVignetteOverrideActive = keepOverrideAfterFinish;
	}

	private float EvaluateBreathingEase(float value)//计算呼吸缓动值
	{
		value = Mathf.Clamp01(value);
		return value * value * value * (value * (value * 6f - 15f) + 10f);
	}

	private float EvaluateVerticalBreathingOffset(float breathingEase)//计算垂直呼吸偏移
	{
		float inhaleLift = Mathf.SmoothStep(-0.2f, 1f, breathingEase);
		return inhaleLift * mainCameraVerticalFloatAmplitude;
	}
}
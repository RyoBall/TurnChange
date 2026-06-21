#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CharacterSwitchAnimationSetup
{
    private const string ArtRoot = "Assets/Resources/Art/切人动画";
    private const string ClipOutputFolder = "Assets/Resources/Art/切人动画/Clips";
    private const string ControllerPath = "Assets/Resources/Art/切人动画/CharacterSwitchPopup.controller";
    private const string FightScenePath = "Assets/Scenes/Fight.unity";
    private const string SwitchPopupObjectName = "切换动画";
    private const float FrameRate = 24f;

    private static readonly (CharacterType type, string popupFolder)[] s_characterMappings =
    {
        (CharacterType.DotMain, "popup1"),
        (CharacterType.DotSub, "popup2"),
        (CharacterType.DotSupport, "popup3"),
        (CharacterType.DirectMain, "popup4"),
        (CharacterType.DirectSub, "popup5"),
        (CharacterType.DirectSupport, "popup6"),
    };

    [MenuItem("Tools/TA/生成切人切换动画资源")]
    public static void GenerateAnimationAssets()
    {
        EnsureFolder(ClipOutputFolder);

        Dictionary<CharacterType, AnimationClip> clips = new Dictionary<CharacterType, AnimationClip>();
        foreach ((CharacterType type, string popupFolder) in s_characterMappings)
        {
            AnimationClip clip = CreateSpriteSequenceClip(type, popupFolder);
            if (clip == null)
            {
                Debug.LogError($"[CharacterSwitchAnimationSetup] 未能为 {type} 生成动画，请检查 {ArtRoot}/{popupFolder} 下的序列帧。");
                return;
            }

            clips[type] = clip;
        }

        CreateAnimatorController(clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CharacterSwitchAnimationSetup] 切人切换动画资源已生成。");
    }

    [MenuItem("Tools/TA/设置Fight场景切人动画物体")]
    public static void SetupFightSceneSwitchPopup()
    {
        GenerateAnimationAssets();

        Scene fightScene = EditorSceneManager.OpenScene(FightScenePath, OpenSceneMode.Single);
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CharacterSwitchAnimationSetup] Fight 场景中未找到 Canvas。");
            return;
        }

        CharacterSwitchPopupView popupView = FindOrCreateSwitchPopup(canvas.transform);
        CharacterManager characterManager = UnityEngine.Object.FindObjectOfType<CharacterManager>();
        if (characterManager == null)
        {
            Debug.LogError("[CharacterSwitchAnimationSetup] Fight 场景中未找到 CharacterManager。");
            return;
        }

        SerializedObject serializedCharacterManager = new SerializedObject(characterManager);
        serializedCharacterManager.FindProperty("m_switchPopupView").objectReferenceValue = popupView;
        serializedCharacterManager.ApplyModifiedPropertiesWithoutUndo();

        popupView.transform.SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(fightScene);
        EditorSceneManager.SaveScene(fightScene);
        Debug.Log("[CharacterSwitchAnimationSetup] Fight 场景切人动画物体已配置完成。");
    }

    private static CharacterSwitchPopupView FindOrCreateSwitchPopup(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find(SwitchPopupObjectName);
        GameObject popupObject;
        if (existing != null)
        {
            popupObject = existing.gameObject;
        }
        else
        {
            popupObject = new GameObject(SwitchPopupObjectName, typeof(RectTransform));
            popupObject.transform.SetParent(canvasTransform, false);
        }

        RectTransform rectTransform = popupObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        Image image = popupObject.GetComponent<Image>();
        if (image == null)
        {
            image = popupObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        image.preserveAspect = true;
        image.enabled = false;

        Animator animator = popupObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = popupObject.AddComponent<Animator>();
        }

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        animator.runtimeAnimatorController = controller;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        CharacterSwitchPopupView popupView = popupObject.GetComponent<CharacterSwitchPopupView>();
        if (popupView == null)
        {
            popupView = popupObject.AddComponent<CharacterSwitchPopupView>();
        }

        SerializedObject serializedPopupView = new SerializedObject(popupView);
        serializedPopupView.FindProperty("m_popupImage").objectReferenceValue = image;
        serializedPopupView.FindProperty("m_animator").objectReferenceValue = animator;
        serializedPopupView.ApplyModifiedPropertiesWithoutUndo();

        return popupView;
    }

    private static AnimationClip CreateSpriteSequenceClip(CharacterType characterType, string popupFolder)
    {
        string spriteFolder = $"{ArtRoot}/{popupFolder}/渲染这个！";
        if (!AssetDatabase.IsValidFolder(spriteFolder))
        {
            Debug.LogError($"[CharacterSwitchAnimationSetup] 找不到序列帧目录: {spriteFolder}");
            return null;
        }

        List<Sprite> sprites = LoadSortedSprites(spriteFolder);
        if (sprites.Count == 0)
        {
            Debug.LogError($"[CharacterSwitchAnimationSetup] 目录中没有 Sprite: {spriteFolder}");
            return null;
        }

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / FrameRate,
                value = sprites[i],
            };
        }

        AnimationClip clip = new AnimationClip
        {
            frameRate = FrameRate,
            name = characterType.ToString(),
        };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string clipPath = $"{ClipOutputFolder}/{characterType}.anim";
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existingClip != null)
        {
            EditorUtility.CopySerialized(clip, existingClip);
            UnityEngine.Object.DestroyImmediate(clip);
            EditorUtility.SetDirty(existingClip);
            return existingClip;
        }

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static List<Sprite> LoadSortedSprites(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        List<(string name, Sprite sprite)> sprites = new List<(string, Sprite)>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                continue;
            }

            sprites.Add((Path.GetFileNameWithoutExtension(assetPath), sprite));
        }

        return sprites
            .OrderBy(entry => entry.name, StringComparer.Ordinal)
            .Select(entry => entry.sprite)
            .ToList();
    }

    private static void CreateAnimatorController(Dictionary<CharacterType, AnimationClip> clips)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState emptyState = stateMachine.AddState("Empty");
        emptyState.writeDefaultValues = false;
        stateMachine.defaultState = emptyState;

        foreach ((CharacterType type, string _) in s_characterMappings)
        {
            if (!clips.TryGetValue(type, out AnimationClip clip) || clip == null)
            {
                continue;
            }

            AnimatorState popupState = stateMachine.AddState(type.ToString());
            popupState.motion = clip;
            // 不添加自动回退过渡：回退由 CharacterSwitchPopupView 代码主动控制，
            // 避免 Animator exitTime 过渡与 WaitForStateComplete 检测产生竞态导致最后一帧卡住。
        }

        EditorUtility.SetDirty(controller);
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        ChildAnimatorState[] childStates = stateMachine.states;
        for (int i = childStates.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveState(childStates[i].state);
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif

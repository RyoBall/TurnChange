#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 创建可复用的 UI 环境漂浮粒子 Prefab。
/// </summary>
public static class AmbientFloatingParticlesPrefabCreator
{
    private const string PrefabPath = "Assets/Resources/Prefabs/通用物体/AmbientFloatingParticles.prefab";

    [InitializeOnLoadMethod]
    private static void EnsurePrefabOnLoad()
    {
        EditorApplication.delayCall += EnsurePrefabExists;
    }

    [MenuItem("Tools/Create Ambient Floating Particles Prefab")]
    public static void CreatePrefabFromMenu()
    {
        CreatePrefabAsset();
        EditorUtility.DisplayDialog("Ambient Floating Particles", $"已创建/更新 Prefab：\n{PrefabPath}", "确定");
    }

    [MenuItem("Tools/Update Ambient Floating Particles Prefab")]
    public static void UpdatePrefabFromMenu()
    {
        CreatePrefabFromMenu();
    }

    private static void EnsurePrefabExists()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            CreatePrefabAsset();
        }
    }

    private static void CreatePrefabAsset()
    {
        EnsureFolderExists("Assets/Resources/Prefabs/通用物体");

        GameObject instance = new GameObject(
            "AmbientFloatingParticles",
            typeof(RectTransform),
            typeof(AmbientFloatingParticles));
        instance.layer = 5;

        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        StretchRect(rectTransform);

        RemoveLegacyParticleComponents(instance);

        AmbientFloatingParticles ambient = instance.GetComponent<AmbientFloatingParticles>();
        Sprite builtinSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        SerializedObject serializedAmbient = new SerializedObject(ambient);
        serializedAmbient.FindProperty("m_particleSprite").objectReferenceValue = builtinSprite;
        serializedAmbient.FindProperty("m_playOnAwake").boolValue = false;
        serializedAmbient.ApplyModifiedPropertiesWithoutUndo();
        ambient.ApplySettings();

        GameObject prefabRoot = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);

        if (prefabRoot != null)
        {
            prefabRoot.GetComponent<AmbientFloatingParticles>()?.ApplySettings();
            EditorUtility.SetDirty(prefabRoot);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AmbientFloatingParticles] Prefab 已保存至 {PrefabPath}");
        }
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolderExists(parent);
        }

        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static void RemoveLegacyParticleComponents(GameObject target)
    {
        ParticleSystemRenderer renderer = target.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Object.DestroyImmediate(renderer);
        }

        ParticleSystem particleSystem = target.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            Object.DestroyImmediate(particleSystem);
        }
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }
}
#endif

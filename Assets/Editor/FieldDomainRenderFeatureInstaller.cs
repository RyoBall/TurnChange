#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class FieldDomainRenderFeatureInstaller
{
    private const string ShaderPath = "Assets/VFX/FieldDomain/Shaders/FieldDomainEffect.shader";
    private const string NoiseSourcePath = "Assets/VFX/Textures/NoiseSmooth04.png";
    private const string NoiseResourcesPath = "Assets/Resources/FieldDomain/NoiseSmooth04.png";
    private const string ProfilesDirectory = "Assets/VFX/FieldDomain/Profiles";
    private const string FightScenePath = "Assets/Scenes/Fight.unity";

    private static readonly string[] s_RendererPaths =
    {
        "Assets/Settings/URP-Performant-Renderer.asset",
        "Assets/Settings/URP-Balanced-Renderer.asset",
        "Assets/Settings/URP-HighFidelity-Renderer.asset",
    };

    [InitializeOnLoadMethod]
    private static void InstallOnLoad()
    {
        EditorApplication.delayCall += () => { TryInstallAll(); };
    }

    [MenuItem("TurnChange/Field Domain/Install Render Feature")]
    public static void InstallFromMenu()
    {
        bool installed = TryInstallAll();
        bool wired = WireFightSceneProfiles();
        if (installed)
        {
            Debug.Log(wired
                ? "[FieldDomain] 已修复 Render Feature、Profile、Resources 与 Fight 场景引用。"
                : "[FieldDomain] 已修复 Render Feature、Profile 与 Resources 资源。");
        }
        else
        {
            Debug.LogWarning("[FieldDomain] 部分修复失败，请查看 Console。");
        }
    }

    [MenuItem("TurnChange/Field Domain/Wire Fight Scene Profiles")]
    public static void WireFightSceneProfilesFromMenu()
    {
        if (WireFightSceneProfiles())
        {
            Debug.Log("[FieldDomain] Fight 场景 Profile 引用已更新。");
        }
    }

    [MenuItem("TurnChange/Field Domain/Validate Setup")]
    public static void ValidateSetup()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        Texture2D noise = AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseResourcesPath);

        Debug.Log($"[FieldDomain] Shader: {(shader != null ? shader.name : "缺失")} | supported={shader != null && shader.isSupported}");
        Debug.Log($"[FieldDomain] Resources 噪声: {(noise != null ? noise.name : "缺失")}");

        foreach (string rendererPath in s_RendererPaths)
        {
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                Debug.LogWarning($"[FieldDomain] Renderer 缺失: {rendererPath}");
                continue;
            }

            int featureCount = 0;
            foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
            {
                if (feature is FieldDomainRenderFeature fieldFeature)
                {
                    featureCount++;
                    SerializedObject so = new SerializedObject(fieldFeature);
                    Object shaderRef = so.FindProperty("effectShader").objectReferenceValue;
                    Debug.Log($"[FieldDomain] {rendererData.name}: active={fieldFeature.isActive}, shaderRef={(shaderRef != null ? shaderRef.name : "空")}");
                }
            }

            if (featureCount == 0)
            {
                Debug.LogWarning($"[FieldDomain] {rendererData.name} 中未找到 FieldDomainRenderFeature。");
            }
            else if (featureCount > 1)
            {
                Debug.LogWarning($"[FieldDomain] {rendererData.name} 存在 {featureCount} 个重复 FieldDomainRenderFeature。");
            }
        }

        LogProfileStatus("VerdictProfile.asset", EnvironmentType.Gravity);
        LogProfileStatus("DesperationProfile.asset", EnvironmentType.DesperationField);
        LogProfileStatus("MiracleProfile.asset", EnvironmentType.MiracleField);
    }

    private static void LogProfileStatus(string fileName, EnvironmentType environmentType)
    {
        string path = $"{ProfilesDirectory}/{fileName}";
        FieldDomainEffectProfile profile = AssetDatabase.LoadAssetAtPath<FieldDomainEffectProfile>(path);
        Debug.Log($"[FieldDomain] Profile {fileName}: {(profile != null ? "OK" : "缺失")} | style={(profile != null ? profile.visualStyle.ToString() : "-")} | env={environmentType}");
    }

    private static bool TryInstallAll()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            Debug.LogError("[FieldDomain] 找不到 FieldDomainEffect Shader。");
            return false;
        }

        bool success = true;
        success &= EnsureNoiseInResources();
        success &= EnsureProfileAssets();
        success &= InstallFeatureOnAllRenderers(shader);
        return success;
    }

    private static bool EnsureNoiseInResources()
    {
        if (!File.Exists(NoiseSourcePath))
        {
            Debug.LogError($"[FieldDomain] 找不到噪声源贴图: {NoiseSourcePath}");
            return false;
        }

        string directory = Path.GetDirectoryName(NoiseResourcesPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        if (!File.Exists(NoiseResourcesPath))
        {
            if (!AssetDatabase.CopyAsset(NoiseSourcePath, NoiseResourcesPath))
            {
                Debug.LogError("[FieldDomain] 复制噪声贴图到 Resources 失败。");
                return false;
            }
        }

        return true;
    }

    private static bool EnsureProfileAssets()
    {
        if (!Directory.Exists(ProfilesDirectory))
        {
            Directory.CreateDirectory(ProfilesDirectory);
            AssetDatabase.Refresh();
        }

        Texture2D noise = AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseResourcesPath)
            ?? AssetDatabase.LoadAssetAtPath<Texture2D>(NoiseSourcePath);

        bool success = true;
        success &= CreateOrUpdateProfile("VerdictProfile.asset", EnvironmentType.Gravity, noise);
        success &= CreateOrUpdateProfile("DesperationProfile.asset", EnvironmentType.DesperationField, noise);
        success &= CreateOrUpdateProfile("MiracleProfile.asset", EnvironmentType.MiracleField, noise);
        AssetDatabase.SaveAssets();
        return success;
    }

    private static bool CreateOrUpdateProfile(string fileName, EnvironmentType environmentType, Texture2D noise)
    {
        string path = $"{ProfilesDirectory}/{fileName}";
        FieldDomainEffectProfile profile = AssetDatabase.LoadAssetAtPath<FieldDomainEffectProfile>(path);
        bool isNew = profile == null;

        if (isNew)
        {
            profile = ScriptableObject.CreateInstance<FieldDomainEffectProfile>();
            profile.name = Path.GetFileNameWithoutExtension(fileName);
            AssetDatabase.CreateAsset(profile, path);
        }

        FieldDomainEffectProfile.ApplyPreset(profile, environmentType);
        if (noise != null && environmentType != EnvironmentType.MiracleField)
        {
            profile.flameNoiseTexture = noise;
        }

        EditorUtility.SetDirty(profile);
        return true;
    }

    private static bool InstallFeatureOnAllRenderers(Shader shader)
    {
        bool success = true;
        foreach (string rendererPath in s_RendererPaths)
        {
            success &= InstallFeatureOnRenderer(rendererPath, shader);
        }

        AssetDatabase.SaveAssets();
        return success;
    }

    private static bool InstallFeatureOnRenderer(string rendererPath, Shader shader)
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (rendererData == null)
        {
            Debug.LogWarning($"[FieldDomain] Renderer 缺失: {rendererPath}");
            return false;
        }

        FieldDomainRenderFeature primaryFeature = null;
        var duplicates = new List<FieldDomainRenderFeature>();

        foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
        {
            if (feature is not FieldDomainRenderFeature fieldFeature)
            {
                continue;
            }

            if (primaryFeature == null)
            {
                primaryFeature = fieldFeature;
            }
            else
            {
                duplicates.Add(fieldFeature);
            }
        }

        if (primaryFeature == null)
        {
            primaryFeature = ScriptableObject.CreateInstance<FieldDomainRenderFeature>();
            primaryFeature.name = "FieldDomainRenderFeature";
            AssetDatabase.AddObjectToAsset(primaryFeature, rendererData);
            rendererData.rendererFeatures.Add(primaryFeature);
        }

        for (int i = duplicates.Count - 1; i >= 0; i--)
        {
            FieldDomainRenderFeature duplicate = duplicates[i];
            rendererData.rendererFeatures.Remove(duplicate);
            Object.DestroyImmediate(duplicate, true);
        }

        SerializedObject featureObject = new SerializedObject(primaryFeature);
        featureObject.FindProperty("effectShader").objectReferenceValue = shader;
        featureObject.FindProperty("m_Active").boolValue = true;
        featureObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(primaryFeature);
        EditorUtility.SetDirty(rendererData);
        return true;
    }

    private static bool WireFightSceneProfiles()
    {
        if (!File.Exists(FightScenePath))
        {
            return true;
        }

        FieldDomainEffectProfile verdictProfile =
            AssetDatabase.LoadAssetAtPath<FieldDomainEffectProfile>($"{ProfilesDirectory}/VerdictProfile.asset");
        FieldDomainEffectProfile desperationProfile =
            AssetDatabase.LoadAssetAtPath<FieldDomainEffectProfile>($"{ProfilesDirectory}/DesperationProfile.asset");
        FieldDomainEffectProfile miracleProfile =
            AssetDatabase.LoadAssetAtPath<FieldDomainEffectProfile>($"{ProfilesDirectory}/MiracleProfile.asset");

        if (verdictProfile == null || desperationProfile == null || miracleProfile == null)
        {
            Debug.LogWarning("[FieldDomain] Profile 资源未就绪，跳过 Fight 场景绑定。");
            return false;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        string previousScenePath = activeScene.path;
        bool hadOpenScene = !string.IsNullOrEmpty(previousScenePath);

        try
        {
            Scene fightScene = EditorSceneManager.OpenScene(FightScenePath, OpenSceneMode.Single);
            FieldDomainScreenEffectController[] controllers =
                Object.FindObjectsOfType<FieldDomainScreenEffectController>(true);

            if (controllers.Length == 0)
            {
                Debug.LogWarning("[FieldDomain] Fight 场景中未找到 FieldDomainScreenEffectController。");
                return false;
            }

            foreach (FieldDomainScreenEffectController controller in controllers)
            {
                SerializedObject serializedController = new SerializedObject(controller);
                serializedController.FindProperty("verdictProfile").objectReferenceValue = verdictProfile;
                serializedController.FindProperty("desperationProfile").objectReferenceValue = desperationProfile;
                serializedController.FindProperty("miracleProfile").objectReferenceValue = miracleProfile;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }

            EditorSceneManager.SaveScene(fightScene);
            return true;
        }
        finally
        {
            if (hadOpenScene && previousScenePath != FightScenePath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }
    }
}
#endif

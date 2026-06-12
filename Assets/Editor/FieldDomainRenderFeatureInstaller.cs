#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class FieldDomainRenderFeatureInstaller
{
    private const string HighFidelityRendererPath = "Assets/Settings/URP-HighFidelity-Renderer.asset";
    private const string ShaderPath = "Assets/VFX/FieldDomain/Shaders/FieldDomainEffect.shader";

    [InitializeOnLoadMethod]
    private static void InstallOnLoad()
    {
        EditorApplication.delayCall += () => { TryInstall(); };
    }

    [MenuItem("TurnChange/Field Domain/Install Render Feature")]
    public static void InstallFromMenu()
    {
        if (TryInstall())
        {
            Debug.Log("[FieldDomain] Render Feature 已安装/修复到 URP-HighFidelity-Renderer。");
        }
        else
        {
            Debug.LogWarning("[FieldDomain] Render Feature 安装失败，请查看 Console。");
        }
    }

    [MenuItem("TurnChange/Field Domain/Validate Setup")]
    public static void ValidateSetup()
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(HighFidelityRendererPath);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

        Debug.Log($"[FieldDomain] RendererData: {(rendererData != null ? rendererData.name : "缺失")}");
        Debug.Log($"[FieldDomain] Shader: {(shader != null ? shader.name : "缺失")} | supported={shader != null && shader.isSupported}");

        if (rendererData == null)
        {
            return;
        }

        bool hasFeature = false;
        foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
        {
            if (feature is FieldDomainRenderFeature fieldFeature)
            {
                hasFeature = true;
                SerializedObject so = new SerializedObject(fieldFeature);
                UnityEngine.Object shaderRef = so.FindProperty("effectShader").objectReferenceValue;
                Debug.Log($"[FieldDomain] Feature active={fieldFeature.isActive}, shaderRef={(shaderRef != null ? shaderRef.name : "空")}");
            }
        }

        if (!hasFeature)
        {
            Debug.LogWarning("[FieldDomain] URP-HighFidelity-Renderer 中未找到 FieldDomainRenderFeature。");
        }
    }

    private static bool TryInstall()
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(HighFidelityRendererPath);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (rendererData == null || shader == null)
        {
            return false;
        }

        FieldDomainRenderFeature existingFeature = null;
        foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
        {
            if (feature is FieldDomainRenderFeature fieldFeature)
            {
                existingFeature = fieldFeature;
                break;
            }
        }

        if (existingFeature == null)
        {
            existingFeature = ScriptableObject.CreateInstance<FieldDomainRenderFeature>();
            existingFeature.name = "FieldDomainRenderFeature";

            AssetDatabase.AddObjectToAsset(existingFeature, rendererData);
            rendererData.rendererFeatures.Add(existingFeature);
            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(existingFeature);
        }

        SerializedObject featureObject = new SerializedObject(existingFeature);
        featureObject.FindProperty("effectShader").objectReferenceValue = shader;
        featureObject.FindProperty("m_Active").boolValue = true;
        featureObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(existingFeature);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }
}
#endif

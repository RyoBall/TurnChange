using UnityEngine;
using UnityEditor;

public class CFXFixer : Editor
{
    [MenuItem("Tools/TA/一键修复所有特效的自动销毁")]
    public static void FixAllParticlesStopAction()
    {
        // 找到所有的预制体
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int modifiedCount = 0;

        // 显示一个进度条，装X且实用
        EditorUtility.DisplayProgressBar("修复工具执行中", "正在扫描所有预制体...", 0f);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            bool isModified = false;

            // 1. 修复原生的 ParticleSystem
            ParticleSystem[] particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in particles)
            {
                var main = ps.main;
                if (main.stopAction == ParticleSystemStopAction.Destroy)
                {
                    main.stopAction = ParticleSystemStopAction.Disable;
                    isModified = true;
                }
            }

            // 2. 修复 CFX 插件自带的 CFXR_Effect 脚本
            // 使用 Component 遍历而不是具体类型，防止因为没有引入插件命名空间而报错
            Component[] allComponents = prefab.GetComponentsInChildren<Component>(true);
            foreach (Component comp in allComponents)
            {
                // 只要组件名字叫 CFXR_Effect 就盘它
                if (comp != null && comp.GetType().Name == "CFXR_Effect")
                {
                    // 使用序列化对象修改，最安全
                    SerializedObject so = new SerializedObject(comp);
                    SerializedProperty clearProp = so.FindProperty("clearBehavior");
                    
                    if (clearProp != null)
                    {
                        // 获取枚举所有的选项名称
                        string[] enumNames = clearProp.enumNames;
                        int destroyIndex = System.Array.IndexOf(enumNames, "Destroy");
                        int disableIndex = System.Array.IndexOf(enumNames, "Disable");

                        // 如果当前选中的是 Destroy，且枚举里有 Disable 选项，就替换过去
                        if (destroyIndex != -1 && disableIndex != -1 && clearProp.enumValueIndex == destroyIndex)
                        {
                            clearProp.enumValueIndex = disableIndex;
                            so.ApplyModifiedProperties();
                            isModified = true;
                        }
                    }
                }
            }

            // 如果修改过了，就保存
            if (isModified)
            {
                EditorUtility.SetDirty(prefab);
                modifiedCount++;
            }

            // 更新进度条
            EditorUtility.DisplayProgressBar("TA工具执行中", $"已扫描: {i}/{guids.Length}", (float)i / guids.Length);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets(); // 统一保存所有修改过的资产

        Debug.Log($"<color=#00FF00><b>【TA工具执行完毕】</b></color> 成功修复了 <b>{modifiedCount}</b> 个特效预制体！现在它们都能被 MMF 完美复用了！");
    }
}
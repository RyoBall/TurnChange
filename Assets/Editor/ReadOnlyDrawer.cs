using UnityEngine;
using UnityEditor;

// 告诉 Unity，这个绘制器是专门为 ReadOnlyAttribute 服务的
[CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 关键点：禁用 GUI 的交互功能
        GUI.enabled = false;
        // 绘制该属性的标准字段
        EditorGUI.PropertyField(position, property, label, true);
        // 恢复 GUI 的交互功能
        GUI.enabled = true;
    }

    // 这个方法是可选的，用于确保字段高度正确，防止显示问题
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}

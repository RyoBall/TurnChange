using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnimationClipOverrideEntry))]
public class CharacterAnimationOverrideEntryDrawer : PropertyDrawer
{
    private const float VerticalSpacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty layerNameProperty = property.FindPropertyRelative("layerName");
        SerializedProperty stateNameProperty = property.FindPropertyRelative("stateName");
        SerializedProperty overrideClipProperty = property.FindPropertyRelative("overrideClip");
        SerializedProperty originalClipProperty = property.FindPropertyRelative("originalClip");
        SerializedProperty baseControllerProperty = property.serializedObject.FindProperty("baseController");

        RuntimeAnimatorController baseController = baseControllerProperty != null
            ? baseControllerProperty.objectReferenceValue as RuntimeAnimatorController
            : null;

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(currentRect, label);

        currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        DrawLayerPopup(currentRect, layerNameProperty, baseController);

        currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        DrawStatePopup(currentRect, stateNameProperty, layerNameProperty.stringValue, baseController);

        currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        EditorGUI.PropertyField(currentRect, overrideClipProperty);

        currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.PropertyField(currentRect, originalClipProperty, new GUIContent("Resolved Original Clip"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 4f + VerticalSpacing * 3f + EditorGUIUtility.singleLineHeight;
    }

    private static void DrawLayerPopup(Rect position, SerializedProperty layerNameProperty, RuntimeAnimatorController baseController)
    {
        string[] layerNames = GetLayerNames(baseController);
        if (layerNames.Length == 0)
        {
            EditorGUI.PropertyField(position, layerNameProperty);
            return;
        }

        int selectedIndex = GetSelectedIndex(layerNames, layerNameProperty.stringValue, 0);
        int nextIndex = EditorGUI.Popup(position, "Layer", selectedIndex, layerNames);
        layerNameProperty.stringValue = layerNames[Mathf.Clamp(nextIndex, 0, layerNames.Length - 1)];
    }

    private static void DrawStatePopup(Rect position, SerializedProperty stateNameProperty, string layerName, RuntimeAnimatorController baseController)
    {
        string[] stateNames = GetStateNames(baseController, layerName);
        if (stateNames.Length == 0)
        {
            EditorGUI.PropertyField(position, stateNameProperty);
            return;
        }

        int selectedIndex = GetSelectedIndex(stateNames, stateNameProperty.stringValue, 0);
        int nextIndex = EditorGUI.Popup(position, "State", selectedIndex, stateNames);
        stateNameProperty.stringValue = stateNames[Mathf.Clamp(nextIndex, 0, stateNames.Length - 1)];
    }

    private static int GetSelectedIndex(IReadOnlyList<string> options, string currentValue, int defaultIndex)
    {
        if (options == null || options.Count == 0)
        {
            return -1;
        }

        if (string.IsNullOrWhiteSpace(currentValue))
        {
            return Mathf.Clamp(defaultIndex, 0, options.Count - 1);
        }

        for (int i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i], currentValue, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return Mathf.Clamp(defaultIndex, 0, options.Count - 1);
    }

    private static string[] GetLayerNames(RuntimeAnimatorController baseController)
    {
        AnimatorController animatorController = baseController as AnimatorController;
        if (animatorController == null || animatorController.layers == null || animatorController.layers.Length == 0)
        {
            return Array.Empty<string>();
        }

        string[] layerNames = new string[animatorController.layers.Length];
        for (int i = 0; i < animatorController.layers.Length; i++)
        {
            layerNames[i] = animatorController.layers[i].name;
        }

        return layerNames;
    }

    private static string[] GetStateNames(RuntimeAnimatorController baseController, string layerName)
    {
        AnimatorController animatorController = baseController as AnimatorController;
        if (animatorController == null || animatorController.layers == null || animatorController.layers.Length == 0)
        {
            return Array.Empty<string>();
        }

        string targetLayerName = string.IsNullOrWhiteSpace(layerName) ? animatorController.layers[0].name : layerName;

        for (int i = 0; i < animatorController.layers.Length; i++)
        {
            AnimatorControllerLayer layer = animatorController.layers[i];
            if (!string.Equals(layer.name, targetLayerName, StringComparison.Ordinal))
            {
                continue;
            }

            List<string> stateNames = new List<string>();
            CollectStateNames(layer.stateMachine, stateNames);
            return stateNames.ToArray();
        }

        return Array.Empty<string>();
    }

    private static void CollectStateNames(AnimatorStateMachine stateMachine, List<string> stateNames)
    {
        if (stateMachine == null)
        {
            return;
        }

        for (int i = 0; i < stateMachine.states.Length; i++)
        {
            ChildAnimatorState childState = stateMachine.states[i];
            if (childState.state == null)
            {
                continue;
            }

            stateNames.Add(childState.state.name);
        }

        for (int i = 0; i < stateMachine.stateMachines.Length; i++)
        {
            CollectStateNames(stateMachine.stateMachines[i].stateMachine, stateNames);
        }
    }
}
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LevelEnemyEntry))]
public class LevelEnemyEntryDrawer : PropertyDrawer
{
    private const float VerticalSpacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty enemyDataProperty = property.FindPropertyRelative("enemyData");
        SerializedProperty levelProperty = property.FindPropertyRelative("level");
        SerializedProperty isChessSeriesEnemyProperty = property.FindPropertyRelative("isChessSeriesEnemy");
        SerializedProperty chessBossDataProperty = property.FindPropertyRelative("chessBossData");
        SerializedProperty enabledProperty = chessBossDataProperty != null
            ? chessBossDataProperty.FindPropertyRelative("enabled")
            : null;

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(currentRect, label);

        int previousIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = previousIndent + 1;

        currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        EditorGUI.PropertyField(currentRect, enemyDataProperty);

        currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        EditorGUI.PropertyField(currentRect, levelProperty);

        currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        EditorGUI.PropertyField(currentRect, isChessSeriesEnemyProperty, new GUIContent("是否为棋子系列敌人"));

        bool isChessSeriesEnemy = isChessSeriesEnemyProperty != null && isChessSeriesEnemyProperty.boolValue;
        if (enabledProperty != null)
        {
            enabledProperty.boolValue = isChessSeriesEnemy;
        }

        if (isChessSeriesEnemy && chessBossDataProperty != null)
        {
            currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
            float chessBossHeight = EditorGUI.GetPropertyHeight(chessBossDataProperty, true);
            currentRect.height = chessBossHeight;
            EditorGUI.PropertyField(currentRect, chessBossDataProperty, new GUIContent("棋子数据配置"), true);
        }

        EditorGUI.indentLevel = previousIndent;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty isChessSeriesEnemyProperty = property.FindPropertyRelative("isChessSeriesEnemy");
        SerializedProperty chessBossDataProperty = property.FindPropertyRelative("chessBossData");

        float totalHeight = EditorGUIUtility.singleLineHeight * 4f + VerticalSpacing * 3f;
        if (isChessSeriesEnemyProperty != null && isChessSeriesEnemyProperty.boolValue && chessBossDataProperty != null)
        {
            totalHeight += VerticalSpacing + EditorGUI.GetPropertyHeight(chessBossDataProperty, true);
        }

        return totalHeight;
    }
}

[CustomPropertyDrawer(typeof(ChessBossPendingData))]
public class ChessBossPendingDataDrawer : PropertyDrawer
{
    private const float VerticalSpacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty enabledProperty = property.FindPropertyRelative("enabled");
        SerializedProperty bossGroupIdProperty = property.FindPropertyRelative("bossGroupId");
        SerializedProperty unitRoleProperty = property.FindPropertyRelative("unitRole");
        SerializedProperty startHiddenUntilPhaseTwoProperty = property.FindPropertyRelative("startHiddenUntilPhaseTwo");
        SerializedProperty summonPawnDataProperty = property.FindPropertyRelative("summonPawnData");
        SerializedProperty summonPawnLevelProperty = property.FindPropertyRelative("summonPawnLevel");
        SerializedProperty pawnAdvanceOffsetProperty = property.FindPropertyRelative("pawnAdvanceOffset");
        SerializedProperty pawnAdvanceDurationProperty = property.FindPropertyRelative("pawnAdvanceDuration");
        SerializedProperty pawnPromotionStepsProperty = property.FindPropertyRelative("pawnPromotionSteps");
        SerializedProperty summonedPawnHealRatioProperty = property.FindPropertyRelative("summonedPawnHealRatio");
        SerializedProperty immuneToDazeProperty = property.FindPropertyRelative("immuneToDaze");
        SerializedProperty immuneToTauntProperty = property.FindPropertyRelative("immuneToTaunt");

        if (enabledProperty != null)
        {
            enabledProperty.boolValue = true;
        }

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(currentRect, label);

        int previousIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = previousIndent + 1;

        currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        EditorGUI.PropertyField(currentRect, bossGroupIdProperty);

        currentRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        EditorGUI.PropertyField(currentRect, unitRoleProperty);

        ChessBossUnitRole unitRole = unitRoleProperty != null
            ? (ChessBossUnitRole)unitRoleProperty.enumValueIndex
            : ChessBossUnitRole.None;

        if (unitRole == ChessBossUnitRole.QueenBoss)
        {
            DrawNextProperty(ref currentRect, startHiddenUntilPhaseTwoProperty);
            DrawNextProperty(ref currentRect, summonPawnDataProperty);
            DrawNextProperty(ref currentRect, summonPawnLevelProperty);
            DrawNextProperty(ref currentRect, summonedPawnHealRatioProperty);
            DrawNextProperty(ref currentRect, immuneToDazeProperty);
            DrawNextProperty(ref currentRect, immuneToTauntProperty);
        }
        else if (unitRole == ChessBossUnitRole.PromotionPawn)
        {
            DrawNextProperty(ref currentRect, pawnAdvanceOffsetProperty);
            DrawNextProperty(ref currentRect, pawnAdvanceDurationProperty);
            DrawNextProperty(ref currentRect, pawnPromotionStepsProperty);
        }

        EditorGUI.indentLevel = previousIndent;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty unitRoleProperty = property.FindPropertyRelative("unitRole");
        ChessBossUnitRole unitRole = unitRoleProperty != null
            ? (ChessBossUnitRole)unitRoleProperty.enumValueIndex
            : ChessBossUnitRole.None;

        int lineCount = 3;
        if (unitRole == ChessBossUnitRole.QueenBoss)
        {
            lineCount += 6;
        }
        else if (unitRole == ChessBossUnitRole.PromotionPawn)
        {
            lineCount += 3;
        }

        return lineCount * EditorGUIUtility.singleLineHeight + (lineCount - 1) * VerticalSpacing;
    }

    private static void DrawNextProperty(ref Rect rect, SerializedProperty property)
    {
        if (property == null)
        {
            return;
        }

        rect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
        EditorGUI.PropertyField(rect, property);
    }
}
using UnityEditor;
using UnityEngine;
 
/// <summary>
/// UnitData 인스펙터.
///
/// 레벨 보너스가 20개라 기본 인스펙터로는 스크롤이 감당이 안 됩니다.
/// 그래서 폴드아웃으로 접고, 접힌 상태에서도 내용을 알 수 있게 헤더에 요약을 붙입니다.
///
/// UnitLevelEntry 래퍼가 제거되면서 levelEntries가 List&lt;StatLevelBonus&gt;가 됐습니다.
/// 배열 원소가 곧 StatLevelBonus이므로 FindPropertyRelative를 한 겹 덜 탑니다.
/// </summary>
[CustomEditor(typeof(UnitData))]
public class UnitDataEditor : Editor
{
    private SerializedProperty levelEntriesProperty;
    private bool[] foldouts;
 
    private void OnEnable()
    {
        levelEntriesProperty = serializedObject.FindProperty("levelEntries");
        ResizeFoldoutArray();
    }
 
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
 
        // levelEntries만 빼고 나머지는 기본 방식으로 그립니다.
        DrawPropertiesExcluding(serializedObject, "m_Script", "levelEntries");
 
        ResizeLevelEntries();
        ResizeFoldoutArray();
 
        EditorGUILayout.Space();
 
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("레벨 보너스", EditorStyles.boldLabel);
 
            if (GUILayout.Button("모두 펼치기", GUILayout.Width(90))) SetAllFoldouts(true);
            if (GUILayout.Button("모두 접기", GUILayout.Width(90))) SetAllFoldouts(false);
        }
 
        for (int i = 0; i < levelEntriesProperty.arraySize; i++)
            DrawLevelEntry(i);
 
        serializedObject.ApplyModifiedProperties();
    }
 
    /// <summary>
    /// 배열 원소가 곧 StatLevelBonus입니다.
    /// 예전에는 UnitLevelEntry를 거쳐 levelBonus를 한 번 더 찾아야 했습니다.
    /// </summary>
    private void DrawLevelEntry(int index)
    {
        SerializedProperty entry = levelEntriesProperty.GetArrayElementAtIndex(index);
 
        SerializedProperty statType = entry.FindPropertyRelative("statType");
        SerializedProperty value = entry.FindPropertyRelative("value");
 
        // 필드명이 바뀌면 조용히 안 그려지므로 로그를 남깁니다.
        if (statType == null || value == null)
        {
            EditorGUILayout.HelpBox($"Lv.{index + 1} - StatLevelBonus의 필드를 찾을 수 없습니다. " +
                                    "필드명이 statType / value 인지 확인하세요.", MessageType.Error);
            return;
        }
 
        foldouts[index] = EditorGUILayout.Foldout(
            foldouts[index],
            $"Lv.{index + 1}    {BuildSummary(statType, value)}",
            true);
 
        if (!foldouts[index]) return;
 
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(statType, new GUIContent("스탯"));
        EditorGUILayout.PropertyField(value, new GUIContent("증가량"));
        EditorGUI.indentLevel--;
 
        EditorGUILayout.Space(2);
    }
 
    /// <summary>접힌 상태에서도 어떤 보너스인지 보이게 헤더에 붙일 문자열.</summary>
    private string BuildSummary(SerializedProperty statType, SerializedProperty value)
    {
        if (Mathf.Approximately(value.floatValue, 0f)) return "-";
 
        string statName = statType.enumDisplayNames[statType.enumValueIndex];
        return $"{statName} +{value.floatValue}";
    }
 
    private void SetAllFoldouts(bool open)
    {
        for (int i = 0; i < foldouts.Length; i++) foldouts[i] = open;
    }
 
    /// <summary>
    /// UNIT_MAX_LEVEL이 바뀌어도 배열 길이를 맞춥니다.
    /// UnitData.OnValidate도 같은 일을 하지만, 인스펙터를 여는 즉시 반영되도록 여기서도 합니다.
    /// </summary>
    private void ResizeLevelEntries()
    {
        int maxLevel = GlobalDefines.MAX_VALUE.UNIT_MAX_LEVEL;
 
        while (levelEntriesProperty.arraySize < maxLevel)
        {
            int newIndex = levelEntriesProperty.arraySize;
            levelEntriesProperty.InsertArrayElementAtIndex(newIndex);
 
            // InsertArrayElementAtIndex는 직전 원소를 복사합니다.
            // 그냥 두면 새 레벨이 이전 레벨의 보너스를 그대로 물려받습니다.
            SerializedProperty added = levelEntriesProperty.GetArrayElementAtIndex(newIndex);
 
            SerializedProperty statType = added.FindPropertyRelative("statType");
            SerializedProperty value = added.FindPropertyRelative("value");
            if (statType == null || value == null) break;   // 필드명 불일치 시 무한루프 방지
 
            // enumValueIndex가 아니라 intValue를 씁니다.
            // EStatType.None이 -1이라 인덱스와 실제 값이 다릅니다.
            statType.intValue = (int)EStatType.None;
            value.floatValue = 0f;
        }
 
        while (levelEntriesProperty.arraySize > maxLevel)
            levelEntriesProperty.DeleteArrayElementAtIndex(levelEntriesProperty.arraySize - 1);
    }
 
    private void ResizeFoldoutArray()
    {
        if (foldouts == null || foldouts.Length != levelEntriesProperty.arraySize)
            foldouts = new bool[levelEntriesProperty.arraySize];
    }
}
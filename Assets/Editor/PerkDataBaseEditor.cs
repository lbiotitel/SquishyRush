using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Assets/Editor 폴더 밑에 넣어야 합니다.
[CustomEditor(typeof(PerkDataBase), true)]
public class PerkDataBaseEditor : Editor
{
    private SerializedProperty effectIdsProp;

    // effectId(string) -> PerkEffectData 에셋 매핑 캐시
    private readonly Dictionary<string, PerkEffectData> idToAsset = new Dictionary<string, PerkEffectData>();

    private void OnEnable()
    {
        effectIdsProp = serializedObject.FindProperty("effectIds");
        RefreshCache();
    }

    private void RefreshCache()
    {
        idToAsset.Clear();

        string[] guids = AssetDatabase.FindAssets("t:PerkEffectData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PerkEffectData asset = AssetDatabase.LoadAssetAtPath<PerkEffectData>(path);
            if (asset != null && !string.IsNullOrEmpty(asset.PerkId))
            {
                idToAsset[asset.PerkId] = asset;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // effectIds를 제외한 나머지 필드는 기본 방식대로 그린다
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.propertyPath == effectIdsProp.propertyPath) continue;
            EditorGUILayout.PropertyField(prop, true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("실제로는 string id 값만 들어갑니다. Effects (PerkEffectData)", EditorStyles.boldLabel);

        if (GUILayout.Button("목록 새로고침"))
        {
            RefreshCache();
        }

        for (int i = 0; i < effectIdsProp.arraySize; i++)
        {
            SerializedProperty element = effectIdsProp.GetArrayElementAtIndex(i);
            string currentId = element.stringValue;

            idToAsset.TryGetValue(currentId, out PerkEffectData currentAsset);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            var newAsset = (PerkEffectData)EditorGUILayout.ObjectField(
                currentAsset, typeof(PerkEffectData), false);
            if (EditorGUI.EndChangeCheck())
            {
                element.stringValue = newAsset != null ? newAsset.PerkId : string.Empty;
            }

            // 매핑을 못 찾았는데 id는 남아있는 경우(예: 삭제된 에셋) 표시
            if (currentAsset == null && !string.IsNullOrEmpty(currentId))
            {
                EditorGUILayout.LabelField($"(id: {currentId})", GUILayout.Width(120));
            }

            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                effectIdsProp.DeleteArrayElementAtIndex(i);
                break; // 인덱스가 밀리므로 이번 프레임은 여기서 종료
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Effect 추가"))
        {
            effectIdsProp.InsertArrayElementAtIndex(effectIdsProp.arraySize);
            effectIdsProp.GetArrayElementAtIndex(effectIdsProp.arraySize - 1).stringValue = string.Empty;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
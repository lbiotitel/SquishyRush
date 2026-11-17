#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// SkillDataImporter의 VisualEffect 담당 부분입니다.
/// SkillDataImporter.cs와 같은 폴더(Editor 폴더 안)에 두세요.
///
/// 임포트 순서는 StatusEffect -> VisualEffect -> SkillEffect -> Skill 입니다.
/// SkillEffect가 VisualEffectID를 검증하려면 그 전에 VisualEffect 맵이 만들어져 있어야 합니다.
/// </summary>
public partial class SkillDataImporter
{
    private const string vfxOutputDir = "Assets/04 Datas/VisualEffect";
    private const string vfxPrefabSearchDir = "Assets/03 Prefabs/Skill Effects";

    //키로 찾지 못한 프리팹들. 리포트에 모아서 한 번에 보여줍니다.
    private readonly List<string> missingVfxPrefabs = new();

    // errors / warnings 와 같은 성격의 임포트 상태입니다.
    // 필드로 두면 ValidateAndExtractEffect의 긴 인자 목록을 더 늘리지 않아도 됩니다.
    //
    // 애셋이 아니라 ID만 들고 있는 이유:
    // 교차 검증에 필요한 건 "그 ID가 시트에 있는가"뿐이고,
    // dryRun에서는 애셋을 만들지 않기 때문에 애셋 맵을 쓰면 검증 모드에서 목록이 비어버립니다.
    private HashSet<string> vfxIds = new();
    private readonly HashSet<string> usedVfxIds = new();

    /// <summary>
    /// VisualEffect 시트를 SO로 굽습니다.
    /// 돌려주는 맵은 SkillEffect 단계에서 VisualEffectID 교차 검증에 씁니다.
    /// </summary>
    private HashSet<string> ImportVisualEffects(SheetTable sheet, bool dryRun, StringBuilder log)
    {
        var ids = new HashSet<string>();

        missingVfxPrefabs.Clear();

        if (sheet == null)
        {
            warnings.Add("VisualEffect 시트가 없습니다. 연출 없이 진행합니다.");
            return ids;
        }

        var prefabMap = BuildPrefabMap(vfxPrefabSearchDir);
        log.AppendLine($"연출 프리팹 캐시: {prefabMap.Count}개 ({vfxPrefabSearchDir})");

        if (!dryRun)
        {
            EnsureFolder(vfxOutputDir);
            AssetDatabase.StartAssetEditing();
        }

        try
        {
            for (int i = 0; i < sheet.RowCount; i++)
            {
                string id = sheet.GetString(i, "VisualEffectID");
                if (string.IsNullOrEmpty(id)) continue;

                // ID 등록은 dryRun 여부와 무관합니다. 검증 모드에서도 목록이 있어야 합니다.
                if (!ids.Add(id))
                {
                    errors.Add($"{sheet.Where(i)} VisualEffectID '{id}'가 중복입니다.");
                    continue;
                }

                EMoveType moveType = ReadEnum(sheet, i, "MoveType", EMoveType.Impact);

                // 프리팹 이름은 대개 VisualEffectID와 같습니다.
                // 그래서 컬럼이 비면 ID를 그대로 키로 씁니다(관례 우선).
                // 다른 연출의 프리팹을 재사용할 때만 컬럼에 이름을 적으면 됩니다.
                string prefabKey = sheet.GetString(i, "ProjectilePrefab");
                if (string.IsNullOrEmpty(prefabKey)) prefabKey = id;

                Vector2 offset = ParseVector2(sheet, i, "SpawnOffset");
                float speed = sheet.GetFloat(i, "MoveSpeed");
                float arcHeight = sheet.GetFloat(i, "ArcHeight");

                ValidateVfxRow(sheet, i, id, moveType, speed, arcHeight);

                GameObject prefab = FindVfxPrefab(prefabMap, prefabKey);

                if (dryRun) continue;

                var asset = LoadOrCreate<VisualEffectData>(vfxOutputDir, id);
                asset.Editor_SetFromImport(
                    id, moveType, prefab, offset, speed, arcHeight);

                EditorUtility.SetDirty(asset);
            }
        }
        finally
        {
            if (!dryRun) AssetDatabase.StopAssetEditing();
        }

        log.AppendLine($"VisualEffectData: {ids.Count}개 처리 완료");

        if (missingVfxPrefabs.Count > 0)
        {
            log.AppendLine($"찾지 못한 연출 프리팹 {missingVfxPrefabs.Count}개 (연출 미작업이면 정상입니다):");
            foreach (string key in missingVfxPrefabs)
                log.AppendLine($"  {key}");
        }

        return ids;
    }

    /// <summary>
    /// 행 하나의 규칙 검증.
    /// 여기서 잡히는 것들은 전부 "빌드는 되는데 화면에 아무것도 안 나오는" 종류라
    /// 임포트 시점에 걸러야 디버깅 시간을 아낍니다.
    ///
    /// 프리팹 존재 여부는 여기서 보지 않습니다. FindVfxPrefab이 모아서 따로 리포트합니다.
    /// </summary>
    private void ValidateVfxRow(
        SheetTable sheet, int i, string id,
        EMoveType moveType, float speed, float arcHeight)
    {
        // 해석 규칙은 VisualEffectPreset이 단독으로 쥐고 있습니다. 임포터는 물어보기만 합니다.
        if (VisualEffectPreset.IsProjectile(moveType))
        {
            if (speed <= 0f)
                errors.Add($"{sheet.Where(i)} '{id}'는 {moveType}인데 MoveSpeed가 {speed}입니다. " +
                           $"발사체가 영원히 도착하지 않습니다.");
        }
        else if (speed > 0f)
        {
            warnings.Add($"{sheet.Where(i)} '{id}'는 {moveType}(제자리)인데 " +
                         $"MoveSpeed({speed})가 채워져 있습니다. 무시됩니다.");
        }

        if (VisualEffectPreset.UsesArcHeight(moveType))
        {
            if (arcHeight <= 0f)
                warnings.Add($"{sheet.Where(i)} '{id}'는 Bezier인데 ArcHeight가 0입니다. 직선과 같아집니다.");
        }
        else if (arcHeight > 0f)
        {
            warnings.Add($"{sheet.Where(i)} '{id}'는 포물선이 아닌데 " +
                         $"ArcHeight({arcHeight})가 채워져 있습니다. 무시됩니다.");
        }
    }

    /// <summary>
    /// 연출 프리팹 폴더를 훑어 이름 -> 프리팹 맵을 만듭니다.
    /// 런타임에는 AssetDatabase를 쓸 수 없으니, 이름으로 에셋을 찾는 일은 여기서 끝냅니다.
    /// (StatusEffectData 아이콘을 다루는 BuildSpriteMap과 같은 방식입니다)
    /// </summary>
    private static Dictionary<string, GameObject> BuildPrefabMap(string searchDir)
    {
        var map = new Dictionary<string, GameObject>();
        if (string.IsNullOrEmpty(searchDir) || !AssetDatabase.IsValidFolder(searchDir))
            return map;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { searchDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;
            if (map.ContainsKey(prefab.name)) continue;

            map[prefab.name] = prefab;
        }

        return map;
    }

    private GameObject FindVfxPrefab(Dictionary<string, GameObject> map, string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (map.TryGetValue(key, out var prefab)) return prefab;

        missingVfxPrefabs.Add(key);
        return null;
    }

    /// <summary>"x,y" 형식을 읽습니다. 비어 있으면 (0,0).</summary>
    private Vector2 ParseVector2(SheetTable sheet, int i, string column)
    {
        string raw = sheet.GetString(i, column);
        if (string.IsNullOrWhiteSpace(raw)) return Vector2.zero;

        string[] parts = raw.Split(',');
        if (parts.Length != 2
            || !float.TryParse(parts[0].Trim(), out float x)
            || !float.TryParse(parts[1].Trim(), out float y))
        {
            warnings.Add($"{sheet.Where(i)} {column} '{raw}'를 읽지 못했습니다. " +
                         $"\"x,y\" 형식이어야 합니다. 0,0으로 둡니다.");
            return Vector2.zero;
        }

        return new Vector2(x, y);
    }

    /// <summary>
    /// SkillEffect 행의 VisualEffectID가 실제로 존재하는지 확인합니다.
    /// ValidateAndExtractEffect 안에서 visualEffectId를 읽은 직후에 부릅니다.
    ///
    /// 연출은 스킬보다 늦게 작업되는 게 정상이라 '없음'은 error가 아니라 warning입니다.
    /// 다만 언더바 누락 같은 오타는 조용히 넘기면 수십 개가 통째로 안 붙으므로,
    /// 흔한 실수 패턴은 고칠 값까지 찍어줍니다.
    /// </summary>
    private void ValidateVisualEffectRef(SheetTable sheet, int i, string visualEffectId)
    {
        if (string.IsNullOrEmpty(visualEffectId))
        {
            warnings.Add($"{sheet.Where(i)} VisualEffectID가 비어 있습니다. 연출 없이 즉시 적용됩니다.");
            return;
        }

        if (vfxIds.Contains(visualEffectId))
        {
            usedVfxIds.Add(visualEffectId);
            return;
        }

        // 흔한 오타: 'Effect' 앞 언더바 누락 (E_00002_0Effect -> E_00002_0_Effect)
        if (visualEffectId.EndsWith("Effect") && !visualEffectId.EndsWith("_Effect"))
        {
            string fixedId = visualEffectId.Substring(0, visualEffectId.Length - "Effect".Length) + "_Effect";
            if (vfxIds.Contains(fixedId))
            {
                errors.Add($"{sheet.Where(i)} VisualEffectID '{visualEffectId}'는 오타로 보입니다. " +
                           $"'{fixedId}'로 고쳐주세요. (언더바 누락)");
                return;
            }
        }

        warnings.Add($"{sheet.Where(i)} VisualEffectID '{visualEffectId}'가 VisualEffect 시트에 없습니다. " +
                     $"연출 미작업이면 무시하세요.");
    }

    /// <summary>SkillEffect 루프가 끝난 뒤 부릅니다. 아무 스킬도 참조하지 않는 연출을 찾아냅니다.</summary>
    private void ReportUnusedVfx(HashSet<string> all, HashSet<string> used, StringBuilder log)
    {
        if (all.Count == 0) return;

        int unused = 0;
        foreach (string id in all)
        {
            if (used.Contains(id)) continue;

            log.AppendLine($"  (미참조 연출) {id}");
            unused++;
        }

        log.AppendLine($"연출 참조: {used.Count}개 사용 / {all.Count}개 중 {unused}개 미참조");
    }
}
#endif
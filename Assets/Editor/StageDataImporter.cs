using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Stage_Data_v6.xlsx -> InGameStageDataContainer / InGameStageRowData 애셋 생성기.
/// 메뉴: Tools > 말랑 돌격대 > 스테이지 데이터 임포터
///
/// SkillDataImporter와 동일하게 XlsxReader / SheetTable / Logger를 그대로 사용합니다(재정의하지 않음).
///
/// v6 변경점
///  - 'Round Type' 열(Monster / EliteMonster / Boss / PerkShop)을 읽어 InGameStageRowData.RoundType에 기록합니다.
///  - 재능상점(PerkShop) 행에는 바로 다음 전투의 적 정보가 들어 있습니다(미리보기용). 유닛 목록은 다른 행과 똑같이 검증합니다.
///  - 컨테이너마다 라운드 구성을 검사합니다: 마지막 행이 Boss인지, 재능상점이 연속/마지막에 오지 않는지, 개수 요약.
/// </summary>
public class StageDataImporter : EditorWindow
{
    private const string PREF_XLSX = "MalrangStageImporter.Xlsx";
    private const string PREF_CONTAINER_DIR = "MalrangStageImporter.ContainerDir";
    private const string PREF_ROW_DIR = "MalrangStageImporter.RowDir";
    private const string PREF_PREFAB_DIR = "MalrangStageImporter.PrefabDir";

    private const string COL_ROUND_TYPE = "Round Type";

    private string xlsxPath;
    private string containerOutputDir;
    private string rowOutputDir;
    private string prefabSearchDir;

    private Vector2 scroll;
    private string reportText = "";

    private readonly List<string> errors = new();
    private readonly List<string> warnings = new();
    private readonly HashSet<string> missingPrefabs = new();

    [MenuItem("Tools/말랑 돌격대/스테이지 데이터 임포터")]
    private static void Open()
    {
        var window = GetWindow<StageDataImporter>("스테이지 데이터 임포터");
        window.minSize = new Vector2(540, 480);
    }

    private void OnEnable()
    {
        xlsxPath = EditorPrefs.GetString(PREF_XLSX, "");
        containerOutputDir = EditorPrefs.GetString(PREF_CONTAINER_DIR, "Assets/04 Datas/Stage");
        rowOutputDir = EditorPrefs.GetString(PREF_ROW_DIR, "Assets/04 Datas/StageRow");
        prefabSearchDir = EditorPrefs.GetString(PREF_PREFAB_DIR, "Assets");
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(PREF_XLSX, xlsxPath ?? "");
        EditorPrefs.SetString(PREF_CONTAINER_DIR, containerOutputDir ?? "");
        EditorPrefs.SetString(PREF_ROW_DIR, rowOutputDir ?? "");
        EditorPrefs.SetString(PREF_PREFAB_DIR, prefabSearchDir ?? "");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("엑셀 파일", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            xlsxPath = EditorGUILayout.TextField(xlsxPath);
            if (GUILayout.Button("찾기", GUILayout.Width(60)))
            {
                string picked = EditorUtility.OpenFilePanel("Stage Data 엑셀 선택", "", "xlsx");
                if (!string.IsNullOrEmpty(picked)) xlsxPath = picked;
            }
        }
        EditorGUILayout.HelpBox(
            "프로젝트 밖에 둬도 됩니다. 엑셀에서 파일을 열어 둔 상태로도 읽힙니다.\n" +
            "읽는 시트: Stage Data (필수, 1행 영문 헤더 + 2행 한글 설명행은 자동으로 건너뜀) / 검증 (있으면 자동 대조)\n" +
            "Round Type 열: Monster / EliteMonster / Boss / PerkShop (비어 있으면 Monster)",
            MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("출력 폴더", EditorStyles.boldLabel);
        containerOutputDir = EditorGUILayout.TextField("StageDataContainer", containerOutputDir);
        rowOutputDir = EditorGUILayout.TextField("StageRowData", rowOutputDir);
        prefabSearchDir = EditorGUILayout.TextField("프리팹 검색 경로", prefabSearchDir);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !string.IsNullOrEmpty(xlsxPath);
            if (GUILayout.Button("검증만 실행", GUILayout.Height(30))) Run(dryRun: true);
            if (GUILayout.Button("임포트 실행", GUILayout.Height(30))) Run(dryRun: false);
            GUI.enabled = true;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("결과", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(reportText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    // ================================================================== 본체

    private void Run(bool dryRun)
    {
        missingPrefabs.Clear();
        errors.Clear();
        warnings.Clear();
        var log = new StringBuilder();

        Dictionary<string, List<string[]>> book;
        try
        {
            book = XlsxReader.Read(xlsxPath);
        }
        catch (Exception ex)
        {
            reportText = $"엑셀을 읽지 못했습니다.\n{ex.Message}";
            Logger.LogError($"[스테이지 임포터] {ex.Message}");
            return;
        }

        if (!book.ContainsKey("Stage Data"))
        {
            reportText = $"'Stage Data' 시트가 없습니다.\n찾은 시트: {string.Join(", ", book.Keys)}";
            return;
        }

        SheetTable sheet;
        bool hasRoundTypeColumn;
        try
        {
            var rawRows = StripDescriptionRows(book["Stage Data"], "UnitLevel", out int stripped);
            if (stripped > 0)
                log.AppendLine($"한글 설명행 {stripped}개를 건너뛰었습니다.");

            hasRoundTypeColumn = rawRows.Count > 0 && Array.IndexOf(rawRows[0], COL_ROUND_TYPE) >= 0;
            sheet = new SheetTable("Stage Data", rawRows, "Stage Index");
        }
        catch (Exception ex)
        {
            reportText = ex.Message;
            return;
        }

        if (!hasRoundTypeColumn)
            warnings.Add($"'{COL_ROUND_TYPE}' 열이 없습니다. 모든 행을 Monster로 처리합니다(재능상점/엘리트/보스 구분 없음).");

        // 0) 검증 시트 대조 (있으면) -------------------------------------------
        // 엑셀에 이미 '유닛 수/성급 수/Pos 개수 일치 여부'를 계산해둔 시트가 있으므로,
        // SkillDataImporter가 Enum 시트와 대조하듯 여기서도 한 번 더 교차 확인합니다.
        if (book.ContainsKey("검증"))
        {
            try
            {
                VerifyValidationSheet(new SheetTable("검증", book["검증"], "Stage Index"), log);
            }
            catch (Exception ex)
            {
                warnings.Add($"검증 시트를 읽지 못해 대조를 건너뜁니다: {ex.Message}");
            }
        }
        else
        {
            warnings.Add("검증 시트가 없어 엑셀 자체 검증과의 대조를 건너뜁니다.");
        }

        // 1) 프리팹 캐시 ---------------------------------------------------------
        var prefabMap = BuildPrefabMap(prefabSearchDir);
        log.AppendLine($"프리팹 캐시: {prefabMap.Count}개 ({prefabSearchDir})");

        // 2) Stage Data 행 파싱 + InGameStageRowData 생성 ------------------------
        if (!dryRun)
        {
            EnsureFolder(rowOutputDir);
            EnsureFolder(containerOutputDir);
        }

        var seenStageIndex = new HashSet<string>();
        var containerRows = new Dictionary<string, List<string>>();   // containerKey -> stageIndex 목록(엑셀 등장 순서 유지)
        var containerTypes = new Dictionary<string, List<ERoundType>>(); // containerKey -> 행별 라운드 타입(같은 순서)
        var containerStageName = new Dictionary<string, string>();
        var containerPrefabKey = new Dictionary<string, string>();
        var aliveRowIds = new HashSet<string>();

        int rowCount = 0;

        try
        {
            if (!dryRun) AssetDatabase.StartAssetEditing();

            for (int i = 0; i < sheet.RowCount; i++)
            {
                string stageName = sheet.GetString(i, "StageName");
                string stageIndex = sheet.GetString(i, "Stage Index");

                if (string.IsNullOrEmpty(stageName) || string.IsNullOrEmpty(stageIndex))
                {
                    errors.Add($"{sheet.Where(i)} StageName 또는 Stage Index가 비어 있습니다.");
                    continue;
                }

                if (!seenStageIndex.Add(stageIndex))
                {
                    errors.Add($"{sheet.Where(i)} Stage Index '{stageIndex}'가 중복입니다.");
                    continue;
                }

                float levelRaw = sheet.GetFloat(i, "UnitLevel");
                int level = Mathf.RoundToInt(levelRaw);

                var unitIds = SplitList(sheet.GetString(i, "Stage Unit ID List"));
                var stars = SplitList(sheet.GetString(i, "Unit Star"));
                string posRaw = sheet.GetString(i, "Pos");
                string seedRaw = sheet.GetString(i, "Seed");
                string prefabKey = sheet.GetString(i, "Stage Prefab");

                // 라운드 타입 ------------------------------------------------
                ERoundType roundType = ERoundType.Monster;
                if (hasRoundTypeColumn)
                {
                    string roundTypeRaw = sheet.GetString(i, COL_ROUND_TYPE);
                    if (!string.IsNullOrEmpty(roundTypeRaw) &&
                        !Enum.TryParse(roundTypeRaw.Trim(), ignoreCase: false, out roundType))
                    {
                        errors.Add($"{sheet.Where(i)} Round Type '{roundTypeRaw}'을(를) ERoundType으로 읽지 못했습니다. " +
                                   $"(가능한 값: {string.Join(" / ", Enum.GetNames(typeof(ERoundType)))})");
                        continue;
                    }
                }

                // 재능상점 행도 다음 전투 미리보기용 유닛이 들어 있으므로 같은 기준으로 검사합니다.
                if (unitIds.Count == 0)
                {
                    errors.Add($"{sheet.Where(i)} 유닛 목록이 비어 있습니다." +
                               (roundType == ERoundType.PerkShop ? " (재능상점 행에는 다음 전투의 적을 넣어 주세요)" : ""));
                    continue;
                }

                if (unitIds.Count != stars.Count)
                {
                    errors.Add($"{sheet.Where(i)} 유닛 수({unitIds.Count})와 성급 수({stars.Count})가 일치하지 않습니다.");
                    continue;
                }

                int posOnes = posRaw?.Count(c => c == '1') ?? 0;
                if (posOnes != unitIds.Count)
                {
                    warnings.Add($"{sheet.Where(i)} Pos의 1 개수({posOnes})와 유닛 수({unitIds.Count})가 일치하지 않습니다.");
                }

                var units = new List<InGameStageRowUnitData>(unitIds.Count);
                for (int u = 0; u < unitIds.Count; u++)
                {
                    if (!int.TryParse(stars[u], out int star))
                    {
                        warnings.Add($"{sheet.Where(i)} 성급 파싱 실패 '{stars[u]}' -> 0으로 처리");
                        star = 0;
                    }
                    units.Add(new InGameStageRowUnitData { UnitId = unitIds[u], Level = level, Star = star });
                }

                var grid = ParseGrid(posRaw, sheet.Where(i));

                if (!dryRun)
                {
                    var rowAsset = LoadOrCreate<InGameStageRowData>(rowOutputDir, stageIndex);
                    rowAsset.UnitRowDatas = units;
                    rowAsset.RoundType = roundType;
                    rowAsset.grid = grid;
                    EditorUtility.SetDirty(rowAsset);
                }

                aliveRowIds.Add(stageIndex);

                // DemonCastle은 Seed열("S1 야생 광폭" 등)이 있으면 앞부분으로 컨테이너를 분리합니다.
                // v6부터 DemonCastle은 단일 트랙이라 Seed가 비어 있고, 컨테이너도 'DemonCastle' 하나입니다.
                string containerKey = stageName;
                if (stageName == "DemonCastle" && !string.IsNullOrEmpty(seedRaw))
                {
                    string seedCode = seedRaw.Split(' ')[0]; // "S1 야생 광폭" -> "S1"
                    containerKey = $"{stageName}_{seedCode}";
                }

                if (!containerRows.TryGetValue(containerKey, out var list))
                {
                    list = new List<string>();
                    containerRows[containerKey] = list;
                    containerTypes[containerKey] = new List<ERoundType>();
                    containerStageName[containerKey] = stageName;
                    containerPrefabKey[containerKey] = prefabKey;
                }
                list.Add(stageIndex);
                containerTypes[containerKey].Add(roundType);

                rowCount++;
            }
        }
        finally
        {
            if (!dryRun) AssetDatabase.StopAssetEditing();
        }
        log.AppendLine($"InGameStageRowData: {rowCount}개 생성 완료 (시트 {sheet.RowCount}행)");

        // 2.5) 컨테이너별 라운드 구성 검사 ----------------------------------------
        if (hasRoundTypeColumn)
        {
            foreach (var kv in containerTypes)
                VerifyRoundLayout(kv.Key, containerRows[kv.Key], kv.Value, log);
        }

        // 3) InGameStageDataContainer 생성 ---------------------------------------
        var aliveContainerKeys = new HashSet<string>();

        try
        {
            if (!dryRun) AssetDatabase.StartAssetEditing();

            foreach (var kv in containerRows)
            {
                string containerKey = kv.Key;
                List<string> stageIndices = kv.Value;
                aliveContainerKeys.Add(containerKey);

                if (dryRun) continue;

                var rowAssets = stageIndices
                    .Select(id => AssetDatabase.LoadAssetAtPath<InGameStageRowData>($"{rowOutputDir}/{id}.asset"))
                    .Where(a => a != null)
                    .ToList();

                GameObject prefab = FindPrefab(prefabMap, containerPrefabKey[containerKey], containerKey);

                var container = LoadOrCreate<InGameStageDataContainer>(containerOutputDir, containerKey);

                // stageName / stageMapPrefab / rows 는 전부 private + setter가 없어 SerializedObject로 기록합니다.
                var so = new SerializedObject(container);
                so.FindProperty("stageName").stringValue = containerStageName[containerKey];
                so.FindProperty("stageMapPrefab").objectReferenceValue = prefab;

                var rowsProp = so.FindProperty("rows");
                rowsProp.arraySize = rowAssets.Count;
                for (int r = 0; r < rowAssets.Count; r++)
                    rowsProp.GetArrayElementAtIndex(r).objectReferenceValue = rowAssets[r];

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(container);
            }
        }
        finally
        {
            if (!dryRun) AssetDatabase.StopAssetEditing();
        }
        log.AppendLine($"InGameStageDataContainer: {aliveContainerKeys.Count}개 생성/갱신 완료");

        if (missingPrefabs.Count > 0)
        {
            warnings.Add($"프리팹 {missingPrefabs.Count}종을 찾지 못했습니다(기존 값 유지): " +
                         string.Join(", ", missingPrefabs.Take(5)) +
                         (missingPrefabs.Count > 5 ? " 외" : ""));
        }
        log.AppendLine();

        // 4) 고아 애셋 검사 ---------------------------------------------------------
        // 엑셀에서 행/컨테이너가 사라져도 SO는 디스크에 남습니다. 자동 삭제는 참조를 끊을 수 있어
        // 위험하므로 목록만 알립니다. (v6에서 DemonCastle_S1~S6 컨테이너와 그 행들이 여기에 잡힙니다)
        ReportOrphans<InGameStageRowData>(rowOutputDir, aliveRowIds, "InGameStageRowData", log);
        ReportOrphans<InGameStageDataContainer>(containerOutputDir, aliveContainerKeys, "InGameStageDataContainer", log);

        // 5) 마무리 ------------------------------------------------------------------
        if (errors.Count > 0)
        {
            log.AppendLine($"에러 {errors.Count}건 - 애셋을 저장하지 않았습니다.");
            foreach (var e in errors) log.AppendLine("  " + e);
            log.AppendLine();
        }
        if (warnings.Count > 0)
        {
            log.AppendLine($"경고 {warnings.Count}건");
            foreach (var w in warnings) log.AppendLine("  " + w);
            log.AppendLine();
        }

        if (!dryRun && errors.Count == 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            log.AppendLine("애셋 저장 완료.");
            Logger.Log($"[스테이지 임포터] Row {rowCount}개 / Container {aliveContainerKeys.Count}개 임포트 완료");
        }
        else if (!dryRun)
        {
            Logger.LogError($"[스테이지 임포터] 에러 {errors.Count}건으로 중단했습니다.");
        }
        else
        {
            log.AppendLine(errors.Count == 0 ? "검증 통과." : "검증 실패.");
        }

        reportText = log.ToString();
        Repaint();
    }

    // ================================================================== 라운드 구성 검사

    /// <summary>
    /// 한 컨테이너의 라운드 타입 배열을 검사합니다.
    /// - 마지막 행은 Boss여야 합니다(StageManager의 피날레 유닛 표시가 Boss 행을 기준으로 함).
    /// - 재능상점은 연속되거나 마지막 행이면 안 됩니다(상점 행은 바로 다음 전투의 미리보기).
    /// </summary>
    private void VerifyRoundLayout(string containerKey, List<string> stageIndices, List<ERoundType> types, StringBuilder log)
    {
        int battles = types.Count(t => t != ERoundType.PerkShop);
        int shops = types.Count(t => t == ERoundType.PerkShop);
        int elites = types.Count(t => t == ERoundType.EliteMonster);
        int bosses = types.Count(t => t == ERoundType.Boss);

        log.AppendLine($"  [{containerKey}] 총 {types.Count}행 = 전투 {battles} (엘리트 {elites}, 보스 {bosses}) + 재능상점 {shops}");

        if (types.Count == 0) return;

        int last = types.Count - 1;
        if (types[last] != ERoundType.Boss)
            warnings.Add($"[{containerKey}] 마지막 행 '{stageIndices[last]}'이 Boss가 아닙니다 ({types[last]}).");

        if (bosses > 1)
            warnings.Add($"[{containerKey}] Boss 행이 {bosses}개입니다.");

        for (int i = 0; i < types.Count; i++)
        {
            if (types[i] != ERoundType.PerkShop) continue;

            if (i == types.Count - 1)
            {
                errors.Add($"[{containerKey}] 마지막 행 '{stageIndices[i]}'이 재능상점입니다. 상점 뒤에는 전투가 있어야 합니다.");
                continue;
            }

            if (types[i + 1] == ERoundType.PerkShop)
                errors.Add($"[{containerKey}] '{stageIndices[i]}', '{stageIndices[i + 1]}' 재능상점이 연속입니다.");
        }
    }

    // ================================================================== 검증 시트 대조

    private void VerifyValidationSheet(SheetTable sheet, StringBuilder log)
    {
        int checkedRows = 0;
        for (int i = 0; i < sheet.RowCount; i++)
        {
            string stageIndex = sheet.GetString(i, "Stage Index");
            if (string.IsNullOrEmpty(stageIndex)) continue;
            checkedRows++;

            string match = sheet.GetString(i, "일치 여부");
            if (!string.IsNullOrEmpty(match) && match != "OK")
                warnings.Add($"{sheet.Where(i)} 일치 여부가 '{match}'입니다 (엑셀 자체 검증 실패).");

            string r1Zero = sheet.GetString(i, "R1 전원 0성");
            if (!string.IsNullOrEmpty(r1Zero) && r1Zero != "OK" && r1Zero != "-")
                warnings.Add($"{sheet.Where(i)} R1 전원 0성 검증이 '{r1Zero}'입니다.");
        }
        log.AppendLine($"검증 시트 대조: {checkedRows}행");
    }

    // ================================================================== 파싱 유틸

    private InGameStageRowData.GridRow[] ParseGrid(string posRaw, string context)
    {
        var grid = new InGameStageRowData.GridRow[2]
        {
            new InGameStageRowData.GridRow { cols = new bool[3] },
            new InGameStageRowData.GridRow { cols = new bool[3] }
        };

        if (string.IsNullOrEmpty(posRaw)) return grid;

        string digits = posRaw.Trim();
        if (digits.Length != 6)
            warnings.Add($"{context} Pos 값 '{posRaw}'의 길이가 6이 아닙니다.");

        // 앞 3자리 = 1행(row0) col0~2, 뒤 3자리 = 2행(row1) col0~2
        for (int i = 0; i < digits.Length && i < 6; i++)
        {
            grid[i / 3].cols[i % 3] = digits[i] == '1';
        }
        return grid;
    }

    private static List<string> SplitList(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return new List<string>();
        return raw.Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
    }

    /// <summary>
    /// 헤더 바로 다음에 오는 한글 설명행을 걸러냅니다. checkColumn 값이 숫자로 안 읽히면 설명행으로 간주합니다.
    /// </summary>
    private static List<string[]> StripDescriptionRows(List<string[]> rawRows, string checkColumn, out int stripped)
    {
        stripped = 0;
        if (rawRows == null || rawRows.Count < 2) return rawRows;

        string[] header = rawRows[0];
        int idx = Array.IndexOf(header, checkColumn);
        if (idx < 0) return rawRows;

        var result = new List<string[]>(rawRows.Count) { header };
        for (int i = 1; i < rawRows.Count; i++)
        {
            string[] row = rawRows[i];
            string raw = idx < row.Length ? row[idx] : null;
            if (!string.IsNullOrEmpty(raw) && !float.TryParse(raw, out _))
            {
                stripped++;
                continue;
            }
            result.Add(row);
        }
        return result;
    }

    private GameObject FindPrefab(Dictionary<string, GameObject> map, string key, string contextForLog)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (map.TryGetValue(key, out var prefab)) return prefab;

        missingPrefabs.Add($"{key} ({contextForLog})");
        return null;
    }

    // ================================================================== 애셋 유틸

    /// <summary>
    /// 출력 폴더에 있는데 이번 임포트에서 참조되지 않은 애셋을 찾아 보고합니다. 지우지는 않습니다.
    /// </summary>
    private void ReportOrphans<T>(string folder, HashSet<string> aliveIds, string label, StringBuilder log)
        where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder)) return;

        var orphans = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!aliveIds.Contains(name)) orphans.Add(path);
        }

        if (orphans.Count == 0) return;

        log.AppendLine($"{label} 고아 애셋 {orphans.Count}개 (엑셀에 없음. 확인 후 직접 삭제하세요)");
        foreach (string path in orphans)
        {
            log.AppendLine("  " + path);
            warnings.Add($"{label} 고아 애셋: {path}");
        }
        log.AppendLine();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static T LoadOrCreate<T>(string folder, string assetName) where T : ScriptableObject
    {
        string path = $"{folder}/{assetName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        asset = CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static Dictionary<string, GameObject> BuildPrefabMap(string searchDir)
    {
        var map = new Dictionary<string, GameObject>();
        if (string.IsNullOrEmpty(searchDir) || !AssetDatabase.IsValidFolder(searchDir))
            return map;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { searchDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && !map.ContainsKey(prefab.name))
                map[prefab.name] = prefab;
        }
        return map;
    }
}
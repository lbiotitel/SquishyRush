using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 말랑_돌격대_시너지_데이터.xlsx -> SynergyData / SkillEffectData / StatusEffectData 애셋 생성기.
/// 메뉴: Tools > 말랑 돌격대 > 시너지 데이터 임포터
///
/// SkillDataImporter와 의도적으로 분리되어 있습니다. 헬퍼(LoadOrCreate, EnsureFolder,
/// BuildSpriteMap, Enum 대조)가 중복되지만, 스킬 쪽을 자유롭게 수정해도 시너지가
/// 깨지지 않는 쪽이 더 값어치 있다고 판단했습니다. 스킬 작업이 끝나면 합쳐도 됩니다.
///
/// 이펙트 SO는 SkillEffectData를 그대로 재사용합니다. SynergyEffect 시트의 컬럼이
/// SkillEffect와 1:1이라 새 타입을 만들면 필드 15개를 복사하는 것 외에 아무 일도 안 생깁니다.
/// 출력 폴더만 분리해 두면 스킬 임포터의 고아 애셋 검사와도 섞이지 않습니다.
///
/// SpecialSkillData 애셋은 만들지 않습니다. 파생 SO가 각자 자기 필드를 선언하는 구조라
/// 손으로 만들어야 합니다. 여기서는 SpecialSynergy 시트에 ID가 있는지 대조만 합니다.
/// </summary>
public class SynergyDataImporter : EditorWindow
{
    private const string PREF_XLSX = "MalrangSynergyImporter.Xlsx";
    private const string PREF_SYNERGY_DIR = "MalrangSynergyImporter.SynergyDir";
    private const string PREF_EFFECT_DIR = "MalrangSynergyImporter.EffectDir";
    private const string PREF_STATUS_DIR = "MalrangSynergyImporter.StatusDir";
    private const string PREF_ICON_DIR = "MalrangSynergyImporter.IconDir";
    private const string PREF_IMPORT_STATUS = "MalrangSynergyImporter.ImportStatus";

    private string xlsxPath;
    private string synergyOutputDir;
    private string effectOutputDir;
    private string statusOutputDir;
    private string iconSearchDir;

    /// <summary>
    /// StatusEffect 시트를 이 임포터가 함께 굽는지.
    ///
    /// StatusEffectData는 ID가 전역 유일이라 스킬 임포터와 같은 폴더를 씁니다.
    /// 두 워크북이 같은 시트를 들고 있으므로 나중에 돌린 쪽이 이깁니다.
    /// 지금은 0019~0023(질풍회피/유체화/바인드/바인드강화/암흑)이 시너지 워크북에만
    /// 있어서 켜야 하지만, 스킬 쪽에서 상태이상을 고치기 시작하면 꺼야 합니다.
    /// </summary>
    private bool importStatusSheet = true;

    private Vector2 scroll;
    private string reportText = "";

    private readonly List<string> errors = new();
    private readonly List<string> warnings = new();
    private readonly HashSet<string> missingIcons = new();

    [MenuItem("Tools/말랑 돌격대/시너지 데이터 임포터")]
    private static void Open()
    {
        var window = GetWindow<SynergyDataImporter>("시너지 데이터 임포터");
        window.minSize = new Vector2(540, 480);
    }

    private void OnEnable()
    {
        xlsxPath = EditorPrefs.GetString(PREF_XLSX, "");
        synergyOutputDir = EditorPrefs.GetString(PREF_SYNERGY_DIR, "Assets/04 Datas/Synergy");
        effectOutputDir = EditorPrefs.GetString(PREF_EFFECT_DIR, "Assets/04 Datas/SynergyEffect");
        statusOutputDir = EditorPrefs.GetString(PREF_STATUS_DIR, "Assets/04 Datas/StatusEffect");
        iconSearchDir = EditorPrefs.GetString(PREF_ICON_DIR, "Assets");
        importStatusSheet = EditorPrefs.GetBool(PREF_IMPORT_STATUS, true);
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(PREF_XLSX, xlsxPath ?? "");
        EditorPrefs.SetString(PREF_SYNERGY_DIR, synergyOutputDir ?? "");
        EditorPrefs.SetString(PREF_EFFECT_DIR, effectOutputDir ?? "");
        EditorPrefs.SetString(PREF_STATUS_DIR, statusOutputDir ?? "");
        EditorPrefs.SetString(PREF_ICON_DIR, iconSearchDir ?? "");
        EditorPrefs.SetBool(PREF_IMPORT_STATUS, importStatusSheet);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("엑셀 파일", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            xlsxPath = EditorGUILayout.TextField(xlsxPath);
            if (GUILayout.Button("찾기", GUILayout.Width(60)))
            {
                string picked = EditorUtility.OpenFilePanel("시너지 데이터 엑셀 선택", "", "xlsx");
                if (!string.IsNullOrEmpty(picked)) xlsxPath = picked;
            }
        }
        EditorGUILayout.HelpBox(
            "읽는 시트: Synergy / SynergyEffect / SpecialSynergy / StatusEffect / Enum\n" +
            "SpecialSkillData 애셋은 만들지 않습니다(손으로 만드는 구조). ID 대조만 합니다.",
            MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("출력 폴더", EditorStyles.boldLabel);
        synergyOutputDir = EditorGUILayout.TextField("SynergyData", synergyOutputDir);
        effectOutputDir = EditorGUILayout.TextField("SynergyEffect", effectOutputDir);
        iconSearchDir = EditorGUILayout.TextField("아이콘 검색 경로", iconSearchDir);

        EditorGUILayout.Space();
        importStatusSheet = EditorGUILayout.ToggleLeft(
            "StatusEffect 시트도 임포트 (스킬 임포터와 같은 폴더를 씁니다)", importStatusSheet);

        using (new EditorGUI.DisabledScope(!importStatusSheet))
            statusOutputDir = EditorGUILayout.TextField("StatusEffectData", statusOutputDir);

        if (importStatusSheet)
        {
            EditorGUILayout.HelpBox(
                "StatusEffectData는 ID가 전역 유일이라 스킬 임포터와 폴더를 공유합니다.\n" +
                "두 워크북이 같은 ID를 들고 있으면 나중에 돌린 쪽이 이깁니다.\n" +
                "스킬 쪽에서 상태이상을 고치기 시작하면 이 체크를 끄세요.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "끄면 StatusID가 이미 애셋으로 존재하는지만 확인합니다.",
                MessageType.None);
        }

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

    private class SpecialInfo
    {
        public string ClassName;
        public float Value;
        public float Value2;
    }

    private class EffectGroupItem
    {
        public string EffectId;
        public int Order;
    }

    private void Run(bool dryRun)
    {
        missingIcons.Clear();
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
            Logger.LogError($"[시너지 임포터] {ex.Message}");
            return;
        }

        foreach (string required in new[] { "Synergy", "SynergyEffect", "SpecialSynergy" })
        {
            if (!book.ContainsKey(required))
            {
                reportText = $"'{required}' 시트가 없습니다.\n찾은 시트: {string.Join(", ", book.Keys)}";
                return;
            }
        }

        SheetTable synergySheet, effectSheet, specialSheet;
        SheetTable statusSheet = null;
        try
        {
            synergySheet = new SheetTable("Synergy", book["Synergy"], "Synergy ID");
            effectSheet = new SheetTable("SynergyEffect", book["SynergyEffect"], "Synergy EffectID");
            specialSheet = new SheetTable("SpecialSynergy", book["SpecialSynergy"], "SpecialSkillID");

            if (book.ContainsKey("StatusEffect"))
                statusSheet = new SheetTable("StatusEffect", book["StatusEffect"], "StatusID");
        }
        catch (Exception ex)
        {
            reportText = ex.Message;
            return;
        }

        // 0) Enum 시트 대조 ---------------------------------------------------
        if (book.ContainsKey("Enum"))
        {
            try
            {
                VerifyEnumSheet(new SheetTable("Enum", book["Enum"], "Category"), log);
            }
            catch (Exception ex)
            {
                warnings.Add($"Enum 시트를 읽지 못해 대조를 건너뜁니다: {ex.Message}");
            }
        }
        else
        {
            warnings.Add("Enum 시트가 없어 코드 enum과의 대조를 건너뜁니다.");
        }

        // 1) SpecialSynergy (대조용. 애셋은 만들지 않습니다) --------------------
        var specialMap = new Dictionary<string, SpecialInfo>();
        for (int i = 0; i < specialSheet.RowCount; i++)
        {
            string id = specialSheet.GetString(i, "SpecialSkillID");
            if (specialMap.ContainsKey(id))
            {
                errors.Add($"{specialSheet.Where(i)} SpecialSkillID '{id}'가 중복입니다.");
                continue;
            }

            string className = specialSheet.GetString(i, "ClassName");
            if (string.IsNullOrEmpty(className))
                errors.Add($"{specialSheet.Where(i)} '{id}'의 ClassName이 비어 있습니다.");

            specialMap[id] = new SpecialInfo
            {
                ClassName = className,
                Value = specialSheet.GetFloat(i, "Value"),
                Value2 = specialSheet.GetFloat(i, "Value2"),
            };

            // 실행 클래스가 등록되지 않으면 로드 단계에서 통째로 빠집니다.
            // 데이터 잘못이 아니라 아직 구현 안 된 것이므로 경고로만 둡니다.
            if (!SpecialEffectRegistry.Contains(id))
                warnings.Add($"SpecialSkillID '{id}'({className})가 SpecialEffectRegistry에 없습니다. " +
                             "실행 클래스와 SpecialSkillData 애셋을 아직 만들지 않았다면 정상입니다.");
        }
        log.AppendLine($"SpecialSynergy: {specialMap.Count}개 (대조용)");

        // 2) 아이콘 캐시 ------------------------------------------------------
        var spriteMap = BuildSpriteMap(iconSearchDir);
        log.AppendLine($"스프라이트 캐시: {spriteMap.Count}개 ({iconSearchDir})");

        // 3) StatusEffectData -------------------------------------------------
        var statusIds = importStatusSheet
            ? ImportStatusEffects(statusSheet, spriteMap, dryRun, log)
            : CollectExistingStatusIds(statusOutputDir, log);

        // 4) SkillEffectData (시너지용) 및 그룹 매핑 ---------------------------
        if (!dryRun) EnsureFolder(effectOutputDir);

        var groupToEffects = new Dictionary<string, List<EffectGroupItem>>();
        var seenEffectIds = new HashSet<string>();
        int effectCount = 0;

        try
        {
            if (!dryRun) AssetDatabase.StartAssetEditing();

            for (int i = 0; i < effectSheet.RowCount; i++)
            {
                string effectId = effectSheet.GetString(i, "Synergy EffectID");
                if (!seenEffectIds.Add(effectId))
                {
                    errors.Add($"{effectSheet.Where(i)} Synergy EffectID '{effectId}'가 중복입니다.");
                    continue;
                }

                string gid = effectSheet.GetString(i, "Synergy EffectGroupID");
                if (string.IsNullOrEmpty(gid))
                {
                    errors.Add($"{effectSheet.Where(i)} Synergy EffectGroupID가 비어 있습니다.");
                    continue;
                }

                if (!ValidateAndExtractEffect(effectSheet, i, statusIds, specialMap,
                        out int order, out string visualEffectId, out EEffectType effectType,
                        out ETargetType targetType, out ETargetSelectType selectType, out EAreaType areaType,
                        out float stack,
                        out EValueModifierType modType, out EStatType statType, out float modValue,
                        out string statusId, out string specialId))
                {
                    continue;
                }

                if (!dryRun)
                {
                    var effectAsset = LoadOrCreate<SkillEffectData>(effectOutputDir, effectId);

                    // 시너지에는 성(星) 개념이 없습니다. SkillEffectData가 성별 배열을 받으므로
                    // 길이 1 배열로 넣고 런타임은 항상 star 0으로 읽습니다.
                    effectAsset.Editor_SetFromImport(
                        effectId, gid, order, visualEffectId,
                        effectType, targetType, selectType, areaType,
                        new[] { stack },
                        modType, statType, new[] { modValue },
                        statusId, specialId);

                    EditorUtility.SetDirty(effectAsset);
                }

                if (!groupToEffects.TryGetValue(gid, out var list))
                {
                    list = new List<EffectGroupItem>();
                    groupToEffects[gid] = list;
                }
                list.Add(new EffectGroupItem { EffectId = effectId, Order = order });

                effectCount++;
            }
        }
        finally
        {
            if (!dryRun) AssetDatabase.StopAssetEditing();
        }

        // 같은 그룹 안에서 Order가 겹치면 실행 순서가 정해지지 않습니다.
        foreach (var pair in groupToEffects)
        {
            var seenOrders = new HashSet<int>();
            foreach (var item in pair.Value)
            {
                if (!seenOrders.Add(item.Order))
                    errors.Add($"그룹 '{pair.Key}'에 Order {item.Order}이 중복입니다 ({item.EffectId}).");
            }
        }

        log.AppendLine($"SynergyEffect: {effectCount}개 생성 완료 ({groupToEffects.Count}개 그룹 매핑)");

        // 5) SynergyData ------------------------------------------------------
        if (!dryRun) EnsureFolder(synergyOutputDir);

        int synergyCount = 0;
        var usedGroups = new HashSet<string>();
        var seenSynergyIds = new HashSet<string>();

        // 같은 축에 RequiredCount가 겹치면 어느 단계가 이기는지 알 수 없습니다.
        var axisCounts = new Dictionary<string, HashSet<int>>();

        try
        {
            if (!dryRun) AssetDatabase.StartAssetEditing();

            for (int i = 0; i < synergySheet.RowCount; i++)
            {
                string synergyId = synergySheet.GetString(i, "Synergy ID");
                if (!seenSynergyIds.Add(synergyId))
                {
                    errors.Add($"{synergySheet.Where(i)} Synergy ID '{synergyId}'가 중복입니다.");
                    continue;
                }

                string gid = synergySheet.GetString(i, "SynergyEffectGroupID");
                if (string.IsNullOrEmpty(gid))
                {
                    errors.Add($"{synergySheet.Where(i)} '{synergyId}'의 SynergyEffectGroupID가 비어 있습니다.");
                    continue;
                }

                if (!groupToEffects.TryGetValue(gid, out var groupItems))
                {
                    errors.Add($"{synergySheet.Where(i)} '{synergyId}'의 그룹 '{gid}'에 해당하는 " +
                               "SynergyEffect 행이 없습니다.");
                    continue;
                }

                usedGroups.Add(gid);

                var element = ReadEnum(synergySheet, i, "Element", EElementType.None);
                var race = ReadEnum(synergySheet, i, "Race", ERace.None);

                bool hasElement = element != EElementType.None;
                bool hasRace = race != ERace.None;

                if (hasElement && hasRace)
                {
                    errors.Add($"{synergySheet.Where(i)} '{synergyId}'에 Element({element})와 " +
                               $"Race({race})가 둘 다 채워져 있습니다. 하나만 채워야 합니다.");
                    continue;
                }
                if (!hasElement && !hasRace)
                {
                    errors.Add($"{synergySheet.Where(i)} '{synergyId}'에 Element와 Race가 둘 다 비어 있습니다.");
                    continue;
                }

                int required = synergySheet.GetInt(i, "Required Count", 0);
                if (required < 1)
                {
                    errors.Add($"{synergySheet.Where(i)} '{synergyId}'의 Required Count가 {required}입니다. " +
                               "1 이상이어야 합니다.");
                    continue;
                }

                string axis = hasElement ? $"E:{element}" : $"R:{race}";
                if (!axisCounts.TryGetValue(axis, out var counts))
                {
                    counts = new HashSet<int>();
                    axisCounts[axis] = counts;
                }
                if (!counts.Add(required))
                {
                    errors.Add($"{synergySheet.Where(i)} 축 '{axis}'에 Required Count {required}이 중복입니다. " +
                               "같은 인원수에서 두 시너지가 동시에 켜집니다.");
                }

                // 단계 순서대로 실행되도록 Order 오름차순으로 정렬합니다.
                List<string> effectIds = groupItems
                    .OrderBy(item => item.Order)
                    .Select(item => item.EffectId)
                    .ToList();

                if (!dryRun)
                {
                    var asset = LoadOrCreate<SynergyData>(synergyOutputDir, synergyId);
                    string iconKey = synergySheet.GetString(i, "Synergy Icon");

                    asset.Editor_SetFromImport(
                        synergyId,
                        synergySheet.GetString(i, "SkillNameKey"),
                        synergySheet.GetString(i, "DescriptionKey"),
                        iconKey, FindSprite(spriteMap, iconKey),
                        element, race, required,
                        effectIds);

                    EditorUtility.SetDirty(asset);
                }

                synergyCount++;
            }
        }
        finally
        {
            if (!dryRun) AssetDatabase.StopAssetEditing();
        }

        foreach (var gid in groupToEffects.Keys.Where(g => !usedGroups.Contains(g)))
            warnings.Add($"SynergyEffectGroupID '{gid}'를 참조하는 시너지가 없습니다. 고아 데이터입니다.");

        if (missingIcons.Count > 0)
            warnings.Add($"아이콘 {missingIcons.Count}종을 찾지 못했습니다(기존 값 유지): " +
                         string.Join(", ", missingIcons.Take(5)) + (missingIcons.Count > 5 ? " 외" : ""));

        log.AppendLine($"SynergyData: {synergyCount}개 생성 완료");
        log.AppendLine();

        // 6) 고아 애셋 검사 ----------------------------------------------------
        ReportOrphans<SkillEffectData>(effectOutputDir, seenEffectIds, "SynergyEffect", log);
        ReportOrphans<SynergyData>(synergyOutputDir, seenSynergyIds, "SynergyData", log);

        // 7) 마무리 ------------------------------------------------------------
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
            Logger.Log($"[시너지 임포터] 시너지 {synergyCount}개 / 이펙트 {effectCount}개 임포트 완료");
        }
        else if (!dryRun)
        {
            Logger.LogError($"[시너지 임포터] 에러 {errors.Count}건으로 중단했습니다.");
        }
        else
        {
            log.AppendLine(errors.Count == 0 ? "검증 통과." : "검증 실패.");
        }

        reportText = log.ToString();
        Repaint();
    }

    // ================================================================== StatusEffect

    /// <summary>
    /// StatusEffect 시트를 굽습니다. 스킬 임포터와 같은 규칙입니다.
    /// 같은 StatusID가 Order만 다르게 여러 행으로 올 수 있습니다(광폭 = 피해 +100% + 최대체력 -50%).
    /// </summary>
    private HashSet<string> ImportStatusEffects(
        SheetTable sheet, Dictionary<string, Sprite> spriteMap, bool dryRun, StringBuilder log)
    {
        var ids = new HashSet<string>();

        if (sheet == null)
        {
            errors.Add("StatusEffect 시트를 임포트하도록 켜져 있는데 시트가 없습니다.");
            return ids;
        }

        if (!dryRun) EnsureFolder(statusOutputDir);

        var statusRows = new Dictionary<string, List<int>>();
        var order = new List<string>();
        for (int i = 0; i < sheet.RowCount; i++)
        {
            string id = sheet.GetString(i, "StatusID");
            if (string.IsNullOrEmpty(id)) continue;

            if (!statusRows.TryGetValue(id, out var list))
            {
                list = new List<int>();
                statusRows[id] = list;
                order.Add(id);
            }
            list.Add(i);
        }

        try
        {
            if (!dryRun) AssetDatabase.StartAssetEditing();

            foreach (string id in order)
            {
                var rowIndices = statusRows[id];
                rowIndices.Sort((a, b) => sheet.GetInt(a, "Order").CompareTo(sheet.GetInt(b, "Order")));

                var seenOrders = new HashSet<int>();
                foreach (int r in rowIndices)
                {
                    if (!seenOrders.Add(sheet.GetInt(r, "Order")))
                        errors.Add($"{sheet.Where(r)} StatusID '{id}'에 Order가 중복입니다.");
                }

                int head = rowIndices[0];

                var statusType = ReadEnum(sheet, head, "StatusType", EStatusType.Buff);
                var effectType = ReadEnum(sheet, head, "StatusEffectType", EStatusEffectType.StatModifier);
                var durType = ReadEnum(sheet, head, "DurationType", EDurationType.Time);
                var stack = ReadEnum(sheet, head, "StackBehavior", EStackBehavior.Refresh);

                int maxStack = sheet.GetInt(head, "MaxStack", 1);
                if (stack != EStackBehavior.Add && maxStack != 1)
                {
                    warnings.Add($"{sheet.Where(head)} StackBehavior가 {stack}인데 MaxStack이 {maxStack}입니다. " +
                                 "Add가 아니면 1이어야 합니다.");
                }

                // 기준 스탯이 코드에 고정된 타입은 StatType이 비어 있는 것이 정상입니다.
                bool baseStatIsHardcoded =
                    effectType == EStatusEffectType.Reflect ||
                    effectType == EStatusEffectType.OnDamagedBonus;

                var modifiers = new List<StatusModifierEntry>();
                foreach (int r in rowIndices)
                {
                    var statType = ReadEnum(sheet, r, "StatType", EStatType.None);
                    var modType = ReadEnum(sheet, r, "ModifierType", EValueModifierType.Flat);
                    float modValue = sheet.GetFloat(r, "ModifierValue");

                    if (modType == EValueModifierType.StatPercent && statType == EStatType.None
                        && !baseStatIsHardcoded)
                    {
                        errors.Add($"{sheet.Where(r)} StatPercent인데 StatType이 비어 있습니다.");
                    }

                    modifiers.Add(new StatusModifierEntry(statType, modType, modValue));
                }

                if (effectType == EStatusEffectType.StatModifier &&
                    modifiers.TrueForAll(m => m.statType == EStatType.None))
                {
                    errors.Add($"{sheet.Where(head)} StatModifier인데 StatType이 있는 행이 하나도 없습니다.");
                }

                if (!dryRun)
                {
                    var asset = LoadOrCreate<StatusEffectData>(statusOutputDir, id);
                    string iconKey = sheet.GetString(head, "Icon");

                    asset.Editor_SetFromImport(
                        id,
                        sheet.GetString(head, "NameKey"),
                        sheet.GetString(head, "DescriptionKey"),
                        statusType, effectType,
                        modifiers,
                        durType,
                        sheet.GetFloat(head, "Duration"),
                        sheet.GetFloat(head, "TickInterval"),
                        sheet.GetBool(head, "TickOnApply"),
                        maxStack, stack, iconKey, FindSprite(spriteMap, iconKey));

                    EditorUtility.SetDirty(asset);
                }

                ids.Add(id);
            }
        }
        finally
        {
            if (!dryRun) AssetDatabase.StopAssetEditing();
        }

        log.AppendLine($"StatusEffectData: {ids.Count}개 생성 완료 (시트 {sheet.RowCount}행)");
        log.AppendLine("  ※ 스킬 워크북과 같은 폴더입니다. 나중에 돌린 쪽이 이깁니다.");
        return ids;
    }

    /// <summary>시트를 굽지 않을 때, 폴더에 이미 있는 StatusEffectData 애셋 ID를 모읍니다.</summary>
    private HashSet<string> CollectExistingStatusIds(string folder, StringBuilder log)
    {
        var ids = new HashSet<string>();

        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
        {
            warnings.Add($"StatusEffectData 폴더 '{folder}'를 찾지 못해 StatusID 대조를 건너뜁니다.");
            return ids;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:StatusEffectData", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ids.Add(System.IO.Path.GetFileNameWithoutExtension(path));
        }

        log.AppendLine($"StatusEffectData: 임포트 생략, 기존 애셋 {ids.Count}개로 대조합니다.");
        return ids;
    }

    // ================================================================== 이펙트 파싱

    private bool ValidateAndExtractEffect(
        SheetTable sheet, int i,
        HashSet<string> statusIds,
        Dictionary<string, SpecialInfo> specialMap,
        out int order, out string visualEffectId,
        out EEffectType effectType, out ETargetType targetType,
        out ETargetSelectType selectType, out EAreaType areaType,
        out float stack,
        out EValueModifierType modType, out EStatType statType, out float modValue,
        out string statusId, out string specialId)
    {
        order = sheet.GetInt(i, "Order");
        visualEffectId = sheet.GetString(i, "VisualEffectID");

        effectType = ReadEnum(sheet, i, "EffectType", EEffectType.StatModify);
        targetType = ReadEnum(sheet, i, "TargetType", ETargetType.Team);
        selectType = ReadEnum(sheet, i, "TargetSelectType", ETargetSelectType.Self);
        areaType = ReadEnum(sheet, i, "AreaType", EAreaType.All);
        modType = ReadEnum(sheet, i, "ModifierType", EValueModifierType.None);
        statType = ReadEnum(sheet, i, "StatType", EStatType.None);

        statusId = sheet.GetString(i, "StatusID");
        specialId = sheet.GetString(i, "Special Skill ID");

        // 시너지 시트의 스택 컬럼 이름은 SkillEffect의 StatusStack이 아니라 Value입니다.
        stack = sheet.GetFloat(i, "Value", 1f);
        modValue = sheet.GetFloat(i, "ModifierValue");

        if (!string.IsNullOrEmpty(statusId) && !statusIds.Contains(statusId))
            errors.Add($"{sheet.Where(i)} StatusID '{statusId}'에 해당하는 StatusEffectData가 없습니다.");

        if (modType == EValueModifierType.StatPercent && statType == EStatType.None)
            errors.Add($"{sheet.Where(i)} ModifierType이 StatPercent인데 StatType이 비어 있습니다.");

        if (modType == EValueModifierType.None && statType != EStatType.None)
            warnings.Add($"{sheet.Where(i)} ModifierType이 None인데 StatType('{statType}')이 채워져 있습니다.");

        switch (effectType)
        {
            case EEffectType.StatModify:
                // 시너지의 주력 행입니다. 어떤 스탯을 얼마나 올릴지가 둘 다 있어야 의미가 있습니다.
                if (statType == EStatType.None)
                    errors.Add($"{sheet.Where(i)} StatModify인데 StatType이 비어 있습니다.");

                if (modType == EValueModifierType.None)
                    errors.Add($"{sheet.Where(i)} StatModify인데 ModifierType이 None입니다. " +
                               "절대 가산이면 Flat, 퍼센트 증가면 StatPercent입니다.");

                // 기본값이 1인 배수 스탯에 Flat을 걸면 +8이 되어 피해 900%가 됩니다.
                if (modType == EValueModifierType.Flat &&
                    (statType == EStatType.DmgMultiplier || statType == EStatType.StatusEffectDmg))
                {
                    warnings.Add($"{sheet.Where(i)} {statType}는 기본값이 1인 배수 스탯입니다. " +
                                 "Flat으로 걸면 절대값이 더해집니다. StatPercent가 맞는지 확인하세요.");
                }

                // 기본값이 0에 가까운 스탯에 퍼센트를 걸면 0 그대로입니다.
                if (modType == EValueModifierType.StatPercent &&
                    (statType == EStatType.CritRate || statType == EStatType.Evasion))
                {
                    warnings.Add($"{sheet.Where(i)} {statType}는 기본값이 낮아 StatPercent가 거의 효과가 없습니다. " +
                                 "Flat이 맞는지 확인하세요.");
                }
                break;

            case EEffectType.ApplyStatus:
                if (string.IsNullOrEmpty(statusId))
                    errors.Add($"{sheet.Where(i)} ApplyStatus인데 StatusID가 비어 있습니다.");
                break;

            case EEffectType.Special:
                if (string.IsNullOrEmpty(specialId))
                {
                    errors.Add($"{sheet.Where(i)} Special인데 Special Skill ID가 비어 있습니다.");
                }
                else if (!specialMap.ContainsKey(specialId))
                {
                    errors.Add($"{sheet.Where(i)} Special Skill ID '{specialId}'가 SpecialSynergy 시트에 없습니다.");
                }

                // Special 수치는 SpecialSynergy 시트가 단일 출처입니다.
                if (modType != EValueModifierType.None)
                    warnings.Add($"{sheet.Where(i)} Special 행은 ModifierValue를 쓰지 않습니다. " +
                                 "수치는 SpecialSynergy 시트에 둡니다.");
                break;

            case EEffectType.Damage:
            case EEffectType.Heal:
                warnings.Add($"{sheet.Where(i)} EffectType이 {effectType}입니다. " +
                             "시너지는 시전자가 없어 기준 스탯을 정할 수 없습니다. 의도한 것인지 확인하세요.");
                break;
        }

        if (areaType == EAreaType.All &&
            selectType != ETargetSelectType.Random && selectType != ETargetSelectType.Self)
        {
            warnings.Add($"{sheet.Where(i)} AreaType이 All이면 TargetSelectType은 무시됩니다.");
        }

        return true;
    }

    // ================================================================== Enum 대조

    private void VerifyEnumSheet(SheetTable sheet, StringBuilder log)
    {
        var categories = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "EffectType", typeof(EEffectType) },
            { "TargetType", typeof(ETargetType) },
            { "TargetSelectType", typeof(ETargetSelectType) },
            { "AreaType", typeof(EAreaType) },
            { "ModifierType", typeof(EValueModifierType) },
            { "StatusType", typeof(EStatusType) },
            { "StatusEffectType", typeof(EStatusEffectType) },
            { "StatType", typeof(EStatType) },
            { "DurationType", typeof(EDurationType) },
            { "StackBehavior", typeof(EStackBehavior) },
        };

        int checkedRows = 0;

        for (int i = 0; i < sheet.RowCount; i++)
        {
            string category = sheet.GetString(i, "Category");
            string value = sheet.GetString(i, "Value");
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(value)) continue;
            if (!categories.TryGetValue(category, out Type enumType)) continue;

            checkedRows++;

            if (!Enum.GetNames(enumType).Any(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"{sheet.Where(i)} {category}.{value} 가 코드의 {enumType.Name}에 없습니다. " +
                           $"코드에 있는 값: {string.Join(", ", Enum.GetNames(enumType))}");
                continue;
            }

            // 번호가 어긋나면 이미 저장된 SO의 직렬화 값이 통째로 밀립니다.
            // 예전에 Square/All, Special/StatModify로 두 번 겪은 사고라 경고가 아니라 에러로 둡니다.
            string rawNum = sheet.GetString(i, "Num");
            if (!string.IsNullOrEmpty(rawNum))
            {
                int sheetNum = sheet.GetInt(i, "Num");
                int codeNum = (int)Enum.Parse(enumType, value, true);
                if (sheetNum != codeNum)
                {
                    errors.Add($"{sheet.Where(i)} {category}.{value} 의 Num이 시트 {sheetNum} / 코드 {codeNum} 로 다릅니다. " +
                               "이대로 임포트하면 기존 애셋의 값이 밀립니다.");
                }
            }
        }

        log.AppendLine($"Enum 대조: {checkedRows}행");
    }

    // ================================================================== 유틸
    // 아래는 SkillDataImporter와 같은 구현입니다.
    // 스킬 임포터를 건드리지 않기 위해 의도적으로 복사했습니다.

    private T ReadEnum<T>(SheetTable sheet, int row, string column, T fallback) where T : struct, Enum
    {
        var value = sheet.GetEnum(row, column, fallback, out string error);
        if (error != null) errors.Add(error);
        return value;
    }

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

    private static Dictionary<string, Sprite> BuildSpriteMap(string searchDir)
    {
        var map = new Dictionary<string, Sprite>();
        if (string.IsNullOrEmpty(searchDir) || !AssetDatabase.IsValidFolder(searchDir))
            return map;

        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { searchDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is Sprite sprite && !map.ContainsKey(sprite.name))
                    map[sprite.name] = sprite;
            }
        }
        return map;
    }

    private Sprite FindSprite(Dictionary<string, Sprite> map, string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (map.TryGetValue(key, out var sprite)) return sprite;

        missingIcons.Add(key);
        return null;
    }
}
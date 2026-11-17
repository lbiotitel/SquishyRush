using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 말랑_돌격대_Skill_Data_v8 (Skill / SkillEffect 시트) -> SkillData / SkillEffectData 애셋 생성기.
/// 메뉴: Tools > 말랑 돌격대 > 스킬 데이터 임포터
///
/// v8 변경점
///  - StatusEffectData 애셋은 만들지 않습니다. SkillEffect의 StatusID는 문자열 ID로만 저장합니다.
///  - SpecialSkill 시트가 없어도 됩니다. Special Skill ID도 문자열 ID로만 저장합니다(SP SO는 별도 관리).
///  - SkillEffect에 SFX 컬럼 추가.
///  - 필수 시트는 Skill / SkillEffect 두 개뿐입니다. Enum / VisualEffect 시트는 있으면 쓰고 없으면 건너뜁니다.
/// </summary>
public partial class SkillDataImporter : EditorWindow
{
    private const string PREF_XLSX = "MalrangSkillImporter.Xlsx";
    private const string PREF_SKILL_DIR = "MalrangSkillImporter.SkillDir";
    private const string PREF_EFFECT_DIR = "MalrangSkillImporter.EffectDir";
    private const string PREF_ICON_DIR = "MalrangSkillImporter.IconDir";

    private string xlsxPath;
    private string skillOutputDir;
    private string effectOutputDir;
    private string iconSearchDir;

    private Vector2 scroll;
    private string reportText = "";

    private readonly List<string> errors = new();
    private readonly List<string> warnings = new();
    private readonly HashSet<string> missingIcons = new();

    [MenuItem("Tools/말랑 돌격대/스킬 데이터 임포터")]
    private static void Open()
    {
        var window = GetWindow<SkillDataImporter>("스킬 데이터 임포터");
        window.minSize = new Vector2(540, 480);
    }

    private void OnEnable()
    {
        xlsxPath = EditorPrefs.GetString(PREF_XLSX, "");
        skillOutputDir = EditorPrefs.GetString(PREF_SKILL_DIR, "Assets/04 Datas/Skill");
        effectOutputDir = EditorPrefs.GetString(PREF_EFFECT_DIR, "Assets/04 Datas/SkillEffect");
        iconSearchDir = EditorPrefs.GetString(PREF_ICON_DIR, "Assets");
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(PREF_XLSX, xlsxPath ?? "");
        EditorPrefs.SetString(PREF_SKILL_DIR, skillOutputDir ?? "");
        EditorPrefs.SetString(PREF_EFFECT_DIR, effectOutputDir ?? "");
        EditorPrefs.SetString(PREF_ICON_DIR, iconSearchDir ?? "");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("엑셀 파일", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            xlsxPath = EditorGUILayout.TextField(xlsxPath);
            if (GUILayout.Button("찾기", GUILayout.Width(60)))
            {
                string picked = EditorUtility.OpenFilePanel("스킬 데이터 엑셀 선택", "", "xlsx");
                if (!string.IsNullOrEmpty(picked)) xlsxPath = picked;
            }
        }
        EditorGUILayout.HelpBox(
            "프로젝트 밖에 둬도 됩니다. 엑셀에서 파일을 열어 둔 상태로도 읽힙니다.\n" +
            "필수 시트: Skill / SkillEffect\n" +
            "선택 시트: VisualEffect / Enum (없으면 건너뜀)\n" +
            "StatusEffect / SpecialSkill 애셋은 만들지 않고 ID 문자열만 저장합니다.",
            MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("출력 폴더", EditorStyles.boldLabel);
        skillOutputDir = EditorGUILayout.TextField("SkillData", skillOutputDir);
        effectOutputDir = EditorGUILayout.TextField("SkillEffectData", effectOutputDir);
        iconSearchDir = EditorGUILayout.TextField("아이콘 검색 경로", iconSearchDir);

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
            Logger.LogError($"[스킬 임포터] {ex.Message}");
            return;
        }

        foreach (string required in new[] { "Skill", "SkillEffect" })
        {
            if (!book.ContainsKey(required))
            {
                reportText = $"'{required}' 시트가 없습니다.\n찾은 시트: {string.Join(", ", book.Keys)}";
                return;
            }
        }

        SheetTable skillSheet, effectSheet;
        SheetTable vfxSheet = null;
        try
        {
            skillSheet = new SheetTable("Skill", book["Skill"], "SkillID");
            effectSheet = new SheetTable("SkillEffect", book["SkillEffect"], "EffectID");

            // VisualEffect는 선택 시트입니다. 없으면 VisualEffectID는 문자열로만 저장됩니다.
            if (book.ContainsKey("VisualEffect"))
                vfxSheet = new SheetTable("VisualEffect", book["VisualEffect"], "VisualEffectID");
        }
        catch (Exception ex)
        {
            reportText = ex.Message;
            return;
        }

        // 0) Enum 시트 대조 (선택) --------------------------------------------
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
            log.AppendLine("Enum 시트 없음 - 코드 enum 대조 생략");
        }

        // 1) 아이콘 캐시 ------------------------------------------------------
        var spriteMap = BuildSpriteMap(iconSearchDir);
        log.AppendLine($"스프라이트 캐시: {spriteMap.Count}개 ({iconSearchDir})");

        // 2) VisualEffectData (시트가 있을 때만) -------------------------------
        // vfxSheet가 없으면 연출 검증/임포트를 모두 건너뜁니다.
        bool hasVfxSheet = vfxSheet != null;
        if (hasVfxSheet)
        {
            vfxIds = ImportVisualEffects(vfxSheet, dryRun, log);
            usedVfxIds.Clear();
        }
        else
        {
            log.AppendLine("VisualEffect 시트 없음 - VisualEffectID는 문자열로만 저장");
        }

        // 3) SkillEffectData 독립 SO 생성 및 EffectGroupID 그룹화 ------------
        if (!dryRun) EnsureFolder(effectOutputDir);

        var groupToEffects = new Dictionary<string, List<EffectGroupItem>>();
        var seenEffectIds = new HashSet<string>();
        int effectCount = 0;

        try
        {
            if (!dryRun) AssetDatabase.StartAssetEditing();

            for (int i = 0; i < effectSheet.RowCount; i++)
            {
                string effectId = effectSheet.GetString(i, "EffectID");
                if (string.IsNullOrEmpty(effectId)) continue;

                if (!seenEffectIds.Add(effectId))
                {
                    errors.Add($"{effectSheet.Where(i)} EffectID '{effectId}'가 중복입니다.");
                    continue;
                }

                string gid = effectSheet.GetString(i, "EffectGroupID");
                if (string.IsNullOrEmpty(gid))
                {
                    errors.Add($"{effectSheet.Where(i)} EffectGroupID가 비어 있습니다.");
                    continue;
                }

                // 파싱 및 규칙 검증 후 값 할당
                if (ValidateAndExtractEffect(effectSheet, i, hasVfxSheet,
                    out int order, out string sfxId, out string visualEffectId, out EEffectType effectType,
                    out ETargetType targetType, out ETargetSelectType selectType, out EAreaType areaType,
                    out float[] stacks,
                    out EValueModifierType modType, out EStatType statType, out float[] modVals,
                    out string statusId, out string specialId))
                {
                    if (!dryRun)
                    {
                        var effectAsset = LoadOrCreate<SkillEffectData>(effectOutputDir, effectId);
                        effectAsset.Editor_SetFromImport(
                            effectId, gid, order, visualEffectId,
                            effectType, targetType, selectType, areaType,
                            stacks,
                            modType, statType, modVals,
                            statusId, specialId);   

                        EditorUtility.SetDirty(effectAsset);
                    }

                    // EffectGroupID별로 EffectID 묶기
                    if (!groupToEffects.TryGetValue(gid, out var list))
                    {
                        list = new List<EffectGroupItem>();
                        groupToEffects[gid] = list;
                    }
                    list.Add(new EffectGroupItem { EffectId = effectId, Order = order });

                    effectCount++;
                }
            }
        }
        finally
        {
            if (!dryRun) AssetDatabase.StopAssetEditing();
        }
        log.AppendLine($"SkillEffectData: {effectCount}개 생성 완료 ({groupToEffects.Count}개 그룹 매핑)");

        if (hasVfxSheet) ReportUnusedVfx(vfxIds, usedVfxIds, log);

        // 4) SkillData (EffectGroupID에 매핑된 EffectID 리스트를 주입) --------
        if (!dryRun) EnsureFolder(skillOutputDir);

        int skillCount = 0;
        var usedGroups = new HashSet<string>();
        var seenSkillIds = new HashSet<string>();

        try
        {
            if (!dryRun) AssetDatabase.StartAssetEditing();

            for (int i = 0; i < skillSheet.RowCount; i++)
            {
                string skillId = skillSheet.GetString(i, "SkillID");
                if (string.IsNullOrEmpty(skillId)) continue;

                if (!seenSkillIds.Add(skillId))
                {
                    errors.Add($"{skillSheet.Where(i)} SkillID '{skillId}'가 중복입니다.");
                    continue;
                }

                string gid = skillSheet.GetString(i, "EffectGroupID");
                if (string.IsNullOrEmpty(gid))
                {
                    errors.Add($"{skillSheet.Where(i)} '{skillId}'의 EffectGroupID가 비어 있습니다.");
                    continue;
                }

                if (!groupToEffects.TryGetValue(gid, out var groupItems))
                {
                    errors.Add($"{skillSheet.Where(i)} '{skillId}'의 EffectGroupID '{gid}'에 해당하는 SkillEffect 행이 존재하지 않습니다.");
                    continue;
                }

                usedGroups.Add(gid);

                // Order 순서대로 정렬하여 EffectID 추출
                List<string> effectIds = groupItems
                    .OrderBy(item => item.Order)
                    .Select(item => item.EffectId)
                    .ToList();

                var trigger = ReadEnum(skillSheet, i, "TriggerType", ETriggerType.OnCast);

                if (!dryRun)
                {
                    var asset = LoadOrCreate<SkillData>(skillOutputDir, skillId);
                    string iconKey = skillSheet.GetString(i, "SkillIcon");

                    asset.Editor_SetFromImport(
                        skillId,
                        skillSheet.GetString(i, "SkillNameKey"),
                        skillSheet.GetString(i, "DescriptionKey"),
                        null, null,   // skillName / skillDesc: DESC를 안 읽으므로 기존 값 유지
                        iconKey, FindSprite(spriteMap, iconKey),
                        trigger,
                        skillSheet.GetInt(i, "MaxCastCount", -1),
                        effectIds); // EffectGroupID에 묶인 Order순 EffectID 리스트 전달

                    EditorUtility.SetDirty(asset);
                }

                skillCount++;
            }
        }
        finally
        {
            if (!dryRun) AssetDatabase.StopAssetEditing();
        }

        // 고아 데이터 검증
        foreach (var gid in groupToEffects.Keys.Where(g => !usedGroups.Contains(g)))
            warnings.Add($"EffectGroupID '{gid}'를 참조하는 스킬이 없습니다. 고아 데이터입니다.");

        if (missingIcons.Count > 0)
            warnings.Add($"아이콘 {missingIcons.Count}종을 찾지 못했습니다(기존 값 유지): " +
                         string.Join(", ", missingIcons.Take(5)) +
                         (missingIcons.Count > 5 ? " 외" : ""));

        log.AppendLine($"SkillData: {skillCount}개 생성 완료");
        log.AppendLine();

        // 4-b) 고아 애셋 검사 --------------------------------------------------
        // 엑셀에서 행이 사라져도 SO는 디스크에 남습니다. 자동 삭제는 참조를 끊을 수 있어 목록만 알립니다.
        ReportOrphans<SkillEffectData>(effectOutputDir, seenEffectIds, "SkillEffectData", log);
        ReportOrphans<SkillData>(skillOutputDir, seenSkillIds, "SkillData", log);

        // 5) 마무리 -----------------------------------------------------------
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
            Logger.Log($"[스킬 임포터] 스킬 {skillCount}개 / 이펙트 {effectCount}개 임포트 완료");
        }
        else if (!dryRun)
        {
            Logger.LogError($"[스킬 임포터] 에러 {errors.Count}건으로 중단했습니다.");
        }
        else
        {
            log.AppendLine(errors.Count == 0 ? "검증 통과." : "검증 실패.");
        }

        reportText = log.ToString();
        Repaint();
    }

    // ================================================================== Enum 대조

    private void VerifyEnumSheet(SheetTable sheet, StringBuilder log)
    {
        var categories = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "TriggerType", typeof(ETriggerType) },
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
            { "MoveType", typeof(EMoveType) },
        };

        var alias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Hp", "MaxHp" },
            { "Attack", "Atk" },
            { "Bazier", "Bezier" },   // 시트 초기 오타. 시트를 고쳤다면 이 줄은 지워도 됩니다.
        };

        var found = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        int checkedRows = 0;

        for (int i = 0; i < sheet.RowCount; i++)
        {
            string category = sheet.GetString(i, "Category");
            string value = sheet.GetString(i, "Value");
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(value)) continue;
            if (!categories.TryGetValue(category, out Type enumType)) continue;

            checkedRows++;
            string codeName = alias.TryGetValue(value, out string mapped) ? mapped : value;

            if (!Enum.IsDefined(enumType, ToEnumSafe(enumType, codeName)))
            {
                string detail = codeName == value ? value : $"{value} (별칭 → {codeName})";
                errors.Add($"{sheet.Where(i)} {category}.{detail} 가 코드의 {enumType.Name}에 없습니다. " +
                           $"코드에 있는 값: {string.Join(", ", Enum.GetNames(enumType))}");
                continue;
            }

            if (!found.TryGetValue(category, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                found[category] = set;
            }
            set.Add(codeName);

            string rawNum = sheet.GetString(i, "Num");
            if (!string.IsNullOrEmpty(rawNum))
            {
                int sheetNum = sheet.GetInt(i, "Num");
                int codeNum = (int)Enum.Parse(enumType, codeName, true);
                if (sheetNum != codeNum)
                {
                    warnings.Add($"{sheet.Where(i)} {category}.{value} 의 Num이 시트 {sheetNum} / 코드 {codeNum} 로 다릅니다.");
                }
            }
        }

        var skipMissing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "None" };

        foreach (var pair in categories)
        {
            if (!found.TryGetValue(pair.Key, out var set)) continue;
            foreach (string name in Enum.GetNames(pair.Value))
            {
                if (skipMissing.Contains(name)) continue;
                if (!set.Contains(name))
                    warnings.Add($"{pair.Value.Name}.{name} 이 Enum 시트에 없습니다.");
            }
        }

        log.AppendLine($"Enum 대조: {checkedRows}행 / {found.Count}개 카테고리");
    }

    private static object ToEnumSafe(Type enumType, string name)
    {
        return Enum.GetNames(enumType).Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
            ? Enum.Parse(enumType, name, true)
            : (object)name;
    }

    // ================================================================== 이펙트 데이터 파싱

    private bool ValidateAndExtractEffect(
        SheetTable sheet, int i, bool hasVfxSheet,
        out int order, out string sfxId, out string visualEffectId,
        out EEffectType effectType, out ETargetType targetType,
        out ETargetSelectType selectType, out EAreaType areaType,
        out float[] statusStacks,
        out EValueModifierType modType, out EStatType statType, out float[] modValues,
        out string statusId, out string specialId)
    {
        order = sheet.GetInt(i, "Order");
        sfxId = sheet.GetString(i, "SFX");
        visualEffectId = sheet.GetString(i, "VisualEffectID");
        if (hasVfxSheet) ValidateVisualEffectRef(sheet, i, visualEffectId);

        effectType = ReadEnum(sheet, i, "EffectType", EEffectType.Damage);
        targetType = ReadEnum(sheet, i, "TargetType", ETargetType.Enemy);
        selectType = ReadEnum(sheet, i, "TargetSelectType", ETargetSelectType.Random);
        areaType = ReadEnum(sheet, i, "AreaType", EAreaType.Single);
        modType = ReadEnum(sheet, i, "ModifierType", EValueModifierType.None);
        statType = ReadStatType(sheet, i, "StatType");

        // StatusID / Special Skill ID는 ID 문자열만 저장합니다(상태이상·특수스킬 SO는 이 임포터가 만들지 않음).
        statusId = sheet.GetString(i, "StatusID");
        specialId = sheet.GetString(i, "Special Skill ID");

        // 성(0~3)별 다중값은 "a/b/c/d", 단일값은 "a" 로 적습니다.
        statusStacks = sheet.GetFloatArray(i, "StatusStack", out string stackError);
        if (stackError != null) errors.Add(stackError);

        modValues = sheet.GetFloatArray(i, "ModifierValue", out string modError);
        if (modError != null) errors.Add(modError);

        // --- 검증 규칙 ---
        if (modType != EValueModifierType.StatPercent && statType != EStatType.None)
            warnings.Add($"{sheet.Where(i)} ModifierType이 {modType}인데 StatType('{statType}')이 채워져 있습니다.");

        switch (effectType)
        {
            case EEffectType.ApplyStatus:
                if (string.IsNullOrEmpty(statusId))
                    errors.Add($"{sheet.Where(i)} ApplyStatus인데 StatusID가 비어 있습니다.");
                // ModifierType이 채워져 있으면 StatusEffect의 수치를 덮어씁니다(실드처럼 스킬마다 값이 다른 경우).
                if (modType == EValueModifierType.StatPercent && statType == EStatType.None)
                    errors.Add($"{sheet.Where(i)} 상태이상 수치를 덮어쓰려는데 StatType이 비어 있습니다.");
                break;

            case EEffectType.Damage:
                if (modType == EValueModifierType.StatPercent && statType == EStatType.None)
                    errors.Add($"{sheet.Where(i)} Damage가 StatPercent인데 StatType이 비어 있습니다.");
                else if (modType == EValueModifierType.StatPercent && statType != EStatType.Atk)
                    warnings.Add($"{sheet.Where(i)} Damage의 StatType이 '{statType}'입니다.");
                break;

            case EEffectType.Heal:
                if (modType == EValueModifierType.StatPercent && statType == EStatType.None)
                    errors.Add($"{sheet.Where(i)} Heal이 StatPercent인데 StatType이 비어 있습니다.");
                else if (modType == EValueModifierType.StatPercent && statType != EStatType.MaxHp)
                    warnings.Add($"{sheet.Where(i)} Heal의 StatType이 '{statType}'입니다.");
                break;

            case EEffectType.Special:
                if (string.IsNullOrEmpty(specialId))
                    errors.Add($"{sheet.Where(i)} Special인데 Special Skill ID가 비어 있습니다.");
                // Special 수치는 SpecialSkill SO가 단일 출처입니다.
                if (modType != EValueModifierType.None)
                    warnings.Add($"{sheet.Where(i)} Special 행은 ModifierValue를 쓰지 않습니다. 수치는 SpecialSkill SO에 둡니다.");
                break;
        }

        if (areaType == EAreaType.All && selectType != ETargetSelectType.Random && selectType != ETargetSelectType.Self)
        {
            warnings.Add($"{sheet.Where(i)} AreaType이 All이면 TargetSelectType은 무시됩니다.");
        }

        return true;
    }

    // ================================================================== 유틸

    private T ReadEnum<T>(SheetTable sheet, int row, string column, T fallback) where T : struct, Enum
    {
        var value = sheet.GetEnum(row, column, fallback, out string error);
        if (error != null) errors.Add(error);
        return value;
    }

    private EStatType ReadStatType(SheetTable sheet, int row, string column)
    {
        string raw = sheet.GetString(row, column);
        if (string.IsNullOrEmpty(raw)) return EStatType.None;

        switch (raw.ToLowerInvariant())
        {
            case "maxhp":
            case "hp":
                return EStatType.MaxHp;
            case "attack":
                return EStatType.Atk;
        }

        return ReadEnum(sheet, row, column, EStatType.None);
    }

    /// <summary>
    /// 출력 폴더에 있는데 엑셀에는 없는 애셋을 찾아 보고합니다. 지우지는 않습니다.
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
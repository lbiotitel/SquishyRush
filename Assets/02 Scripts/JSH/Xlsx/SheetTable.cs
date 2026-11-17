using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// 원시 2차원 문자열을 "헤더 이름으로 접근하는 표"로 감쌉니다.
/// 열 순서가 바뀌어도 임포터가 깨지지 않게 하려는 목적입니다.
///
/// 엑셀 시트 구조 가정:
///   - 헤더가 있는 행을 자동으로 찾습니다(지정한 키 컬럼이 들어 있는 첫 행).
///   - 헤더 바로 다음 행은 "설명 행"이라 건너뜁니다.
///   - 그 아래부터 데이터입니다.
/// </summary>
public class SheetTable
{
    private readonly Dictionary<string, int> columns = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string[]> rows = new();

    public string SheetName { get; }
    public int RowCount => rows.Count;

    /// <summary>엑셀에서의 실제 행 번호(1-based). 에러 로그용.</summary>
    private readonly List<int> sourceLineNumbers = new();

    public SheetTable(string sheetName, List<string[]> raw, string keyColumnName)
    {
        SheetName = sheetName;

        int headerIndex = -1;
        for (int i = 0; i < raw.Count; i++)
        {
            if (raw[i].Any(c => string.Equals(c?.Trim(), keyColumnName, StringComparison.OrdinalIgnoreCase)))
            {
                headerIndex = i;
                break;
            }
        }

        if (headerIndex < 0)
            throw new InvalidOperationException(
                $"[{sheetName}] 시트에서 헤더 행을 찾지 못했습니다. '{keyColumnName}' 컬럼이 있는지 확인하세요.");

        var header = raw[headerIndex];
        for (int c = 0; c < header.Length; c++)
        {
            string name = header[c]?.Trim();
            if (string.IsNullOrEmpty(name)) continue;
            if (!columns.ContainsKey(name)) columns[name] = c;
        }

        int keyCol = columns[keyColumnName];

        // 헤더 다음 행은 보통 "설명 행"입니다. 다만 나중에 그 행을 지울 수도 있으므로
        // 위치로 단정하지 않고 내용으로 판별합니다.
        // ID는 공백이 없고 짧습니다("Skill_1_00001"). 설명은 긴 한국어 문장입니다.
        int start = headerIndex + 1;
        if (start < raw.Count && IsDescriptionRow(raw[start], keyCol)) start++;

        for (int i = start; i < raw.Count; i++)
        {
            var row = raw[i];
            // 키가 비면 데이터 행이 아닙니다(엑셀이 남긴 빈 행).
            if (keyCol >= row.Length || string.IsNullOrWhiteSpace(row[keyCol])) continue;

            rows.Add(row);
            sourceLineNumbers.Add(i + 1);
        }
    }

    private static bool IsDescriptionRow(string[] row, int keyCol)
    {
        if (keyCol >= row.Length) return false;
        string cell = row[keyCol]?.Trim();
        if (string.IsNullOrEmpty(cell)) return false;
        return cell.Length > 40 || cell.Contains(" ");
    }

    public bool HasColumn(string name) => columns.ContainsKey(name);

    public int GetSourceLine(int rowIndex) => sourceLineNumbers[rowIndex];

    /// <summary>에러 메시지 앞머리. "[SkillEffect 12행]" 형태.</summary>
    public string Where(int rowIndex) => $"[{SheetName} {GetSourceLine(rowIndex)}행]";

    public string GetString(int rowIndex, string column, string fallback = "")
    {
        if (!columns.TryGetValue(column, out int c)) return fallback;
        var row = rows[rowIndex];
        if (c >= row.Length) return fallback;
        string v = row[c]?.Trim();
        return string.IsNullOrEmpty(v) ? fallback : v;
    }

    public bool IsEmpty(int rowIndex, string column) => string.IsNullOrEmpty(GetString(rowIndex, column));

    public float GetFloat(int rowIndex, string column, float fallback = 0f)
    {
        string raw = GetString(rowIndex, column);
        if (string.IsNullOrEmpty(raw)) return fallback;
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
            ? v : fallback;
    }

    public int GetInt(int rowIndex, string column, int fallback = 0)
    {
        string raw = GetString(rowIndex, column);
        if (string.IsNullOrEmpty(raw)) return fallback;
        // "1.0" 처럼 실수로 저장된 정수도 받아 줍니다.
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
            ? (int)Math.Round(v) : fallback;
    }

    public bool GetBool(int rowIndex, string column, bool fallback = false)
    {
        string raw = GetString(rowIndex, column);
        if (string.IsNullOrEmpty(raw)) return fallback;
        raw = raw.ToUpperInvariant();
        if (raw == "TRUE" || raw == "1" || raw == "Y" || raw == "O") return true;
        if (raw == "FALSE" || raw == "0" || raw == "N" || raw == "X") return false;
        return fallback;
    }

    /// <summary>
    /// enum 파싱. 실패하면 fallback을 돌려주고 error에 사유를 담습니다.
    /// 조용히 기본값으로 넘어가면 데이터 오타를 영영 못 잡기 때문에 반드시 보고합니다.
    /// </summary>
    public T GetEnum<T>(int rowIndex, string column, T fallback, out string error) where T : struct, Enum
    {
        error = null;
        string raw = GetString(rowIndex, column);
        if (string.IsNullOrEmpty(raw)) return fallback;

        if (Enum.TryParse(raw, true, out T parsed)) return parsed;

        error = $"{Where(rowIndex)} {column} = '{raw}' 은(는) {typeof(T).Name}에 없는 값입니다. " +
                $"가능한 값: {string.Join(", ", Enum.GetNames(typeof(T)))}";
        return fallback;
    }
    
    /// <summary>
    /// "100/150/200/300" 을 float[4] 로 읽습니다. 값이 하나면 길이 1 배열.
    /// 2~3개는 오타일 가능성이 높아 오류로 잡습니다.
    /// (100/200 이라고 잘못 적으면 2·3성이 조용히 200으로 굳어버립니다)
    /// </summary>
    public float[] GetFloatArray(int rowIndex, string column, out string error, char sep = '/')
    {
        error = null;
        string raw = GetString(rowIndex, column);
        if (string.IsNullOrEmpty(raw)) return new[] { 0f };

        var parts = raw.Split(sep);
        int starCount = GlobalDefines.MAX_VALUE.IG_MAX_STAR + 1;   // 0성~3성 = 4개

        if (parts.Length != 1 && parts.Length != starCount)
        {
            error = $"{Where(rowIndex)} {column} = '{raw}' 은(는) 값이 {parts.Length}개입니다. " +
                    $"1개 또는 {starCount}개여야 합니다.";
            return new[] { 0f };
        }

        var result = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                error = $"{Where(rowIndex)} {column} 의 {i}번째 값 '{parts[i]}' 을(를) 숫자로 못 읽었습니다.";
        }
        return result;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// 외부 DLL(EPPlus, NPOI) 없이 .xlsx를 읽습니다.
/// xlsx는 XML 몇 개를 담은 zip일 뿐이라, System.IO.Compression + System.Xml.Linq로 충분합니다.
/// CSV로 내보내는 수작업 단계를 없애려고 이 방식을 택했습니다.
///
/// 지원: 공유 문자열, 인라인 문자열, 수식 셀의 캐시값, 불린, 빈 셀 건너뛰기.
/// 미지원: 서식(날짜 시리얼 값은 숫자 그대로 나옵니다), 병합 셀(좌상단만 값이 있습니다).
/// </summary>
public static class XlsxReader
{
    private static readonly XNamespace Main =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PkgRel =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    /// <summary>
    /// 통합문서 전체를 읽어 시트 이름 -> 표 로 반환합니다.
    /// </summary>
    public static Dictionary<string, List<string[]>> Read(string xlsxPath)
    {
        if (!File.Exists(xlsxPath))
            throw new FileNotFoundException($"엑셀 파일을 찾을 수 없습니다: {xlsxPath}");

        var result = new Dictionary<string, List<string[]>>();

        // FileShare.ReadWrite: 엑셀에서 파일을 열어 둔 채로도 읽히게 합니다.
        using (var fs = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            string[] sharedStrings = ReadSharedStrings(zip);
            var relMap = ReadRelationships(zip);

            var workbook = LoadXml(zip, "xl/workbook.xml");
            if (workbook == null)
                throw new InvalidDataException("xl/workbook.xml이 없습니다. 올바른 xlsx 파일이 아닙니다.");

            foreach (var sheetNode in workbook.Descendants(Main + "sheet"))
            {
                string sheetName = (string)sheetNode.Attribute("name");
                string relId = (string)sheetNode.Attribute(Rel + "id");
                if (string.IsNullOrEmpty(sheetName) || relId == null) continue;
                if (!relMap.TryGetValue(relId, out string target)) continue;

                var sheetXml = LoadXml(zip, target);
                if (sheetXml == null) continue;

                result[sheetName] = ReadSheet(sheetXml, sharedStrings);
            }
        }

        return result;
    }

    private static XDocument LoadXml(ZipArchive zip, string entryPath)
    {
        var entry = zip.GetEntry(entryPath);
        if (entry == null) return null;
        using (var stream = entry.Open())
            return XDocument.Load(stream);
    }

    private static string[] ReadSharedStrings(ZipArchive zip)
    {
        var doc = LoadXml(zip, "xl/sharedStrings.xml");
        if (doc == null) return Array.Empty<string>();

        var list = new List<string>();
        foreach (var si in doc.Root.Elements(Main + "si"))
        {
            // <si>는 <t> 하나이거나, 서식이 섞이면 <r><t>가 여러 개입니다. 전부 이어 붙입니다.
            var sb = new StringBuilder();
            foreach (var t in si.Descendants(Main + "t"))
                sb.Append(t.Value);
            list.Add(sb.ToString());
        }
        return list.ToArray();
    }

    private static Dictionary<string, string> ReadRelationships(ZipArchive zip)
    {
        var map = new Dictionary<string, string>();
        var doc = LoadXml(zip, "xl/_rels/workbook.xml.rels");
        if (doc == null) return map;

        foreach (var rel in doc.Root.Elements(PkgRel + "Relationship"))
        {
            string id = (string)rel.Attribute("Id");
            string target = (string)rel.Attribute("Target");
            if (id == null || target == null) continue;

            target = target.Replace("\\", "/");
            if (target.StartsWith("/")) target = target.Substring(1);
            else if (!target.StartsWith("xl/")) target = "xl/" + target;

            map[id] = target;
        }
        return map;
    }

    private static List<string[]> ReadSheet(XDocument sheetXml, string[] sharedStrings)
    {
        var rows = new List<string[]>();
        var data = sheetXml.Root.Element(Main + "sheetData");
        if (data == null) return rows;

        foreach (var rowNode in data.Elements(Main + "row"))
        {
            var cells = new List<string>();

            foreach (var cellNode in rowNode.Elements(Main + "c"))
            {
                int colIndex = ColumnIndexFromRef((string)cellNode.Attribute("r"));

                // 값이 없는 셀은 XML에 아예 없습니다. 그 자리를 빈 문자열로 메워야 열이 밀리지 않습니다.
                if (colIndex >= 0)
                {
                    while (cells.Count < colIndex) cells.Add(string.Empty);
                }

                cells.Add(ReadCellValue(cellNode, sharedStrings));
            }

            rows.Add(cells.ToArray());
        }

        return rows;
    }

    private static string ReadCellValue(XElement cellNode, string[] sharedStrings)
    {
        string type = (string)cellNode.Attribute("t");

        if (type == "inlineStr")
        {
            var inline = cellNode.Element(Main + "is");
            return inline == null
                ? string.Empty
                : string.Concat(inline.Descendants(Main + "t").Select(t => t.Value));
        }

        var v = cellNode.Element(Main + "v");
        if (v == null) return string.Empty;
        string raw = v.Value;

        switch (type)
        {
            case "s":
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx)
                    && idx >= 0 && idx < sharedStrings.Length)
                    return sharedStrings[idx];
                return string.Empty;

            case "b":
                return raw == "1" ? "TRUE" : "FALSE";

            case "e":   // 수식 에러(#REF! 등). 그대로 넘겨서 검증 단계에서 걸리게 둡니다.
                return raw;

            default:
                return raw;
        }
    }

    /// <summary>"B5" -> 1 (0-based 열 번호). 실패하면 -1.</summary>
    private static int ColumnIndexFromRef(string cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return -1;

        int col = 0;
        int i = 0;
        while (i < cellRef.Length && char.IsLetter(cellRef[i]))
        {
            col = col * 26 + (char.ToUpperInvariant(cellRef[i]) - 'A' + 1);
            i++;
        }

        return i == 0 ? -1 : col - 1;
    }
}
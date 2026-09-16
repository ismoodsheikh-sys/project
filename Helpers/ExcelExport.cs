using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace VehicleFleetMS.Helpers;

/// <summary>A single worksheet cell: a value plus whether it renders bold.</summary>
public readonly record struct ExcelCell(object? Value, bool Bold = false)
{
    public static implicit operator ExcelCell(string? value) => new(value);
    public static implicit operator ExcelCell(double value) => new(value);
    public static implicit operator ExcelCell(int value) => new(value);
}

/// <summary>Builds a minimal but valid .xlsx workbook (no external Excel library needed).</summary>
public static class ExcelExport
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Build(IEnumerable<string> headers, IEnumerable<IEnumerable<object?>> rows)
    {
        var cellRows = new List<IEnumerable<ExcelCell>> { headers.Select(h => new ExcelCell(h, Bold: true)) };
        cellRows.AddRange(rows.Select(row => row.Select(v => new ExcelCell(v))));
        return BuildWorkbook([("Sheet1", cellRows)]);
    }

    public static byte[] BuildMultiSheet(IEnumerable<(string Name, IEnumerable<string> Headers, IEnumerable<IEnumerable<object?>> Rows)> sheets)
    {
        var sheetRows = sheets.Select(s =>
        {
            var cellRows = new List<IEnumerable<ExcelCell>> { s.Headers.Select(h => new ExcelCell(h, Bold: true)) };
            cellRows.AddRange(s.Rows.Select(row => row.Select(v => new ExcelCell(v))));
            return (s.Name, (IEnumerable<IEnumerable<ExcelCell>>)cellRows);
        });
        return BuildWorkbook(sheetRows);
    }

    /// <summary>Builds a single free-form sheet where each row explicitly controls which cells are bold — used for document-style layouts like invoices.</summary>
    public static byte[] BuildFreeform(string sheetName, IEnumerable<IEnumerable<ExcelCell>> rows) =>
        BuildWorkbook([(sheetName, rows)]);

    private static byte[] BuildWorkbook(IEnumerable<(string Name, IEnumerable<IEnumerable<ExcelCell>> Rows)> sheets)
    {
        var sheetList = sheets.ToList();
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(zip, "[Content_Types].xml", ContentTypesXml(sheetList.Count));
            WriteEntry(zip, "_rels/.rels", RootRelsXml);
            WriteEntry(zip, "xl/workbook.xml", WorkbookXml(sheetList));
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml(sheetList.Count));
            WriteEntry(zip, "xl/styles.xml", StylesXml);
            for (var i = 0; i < sheetList.Count; i++)
            {
                WriteEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", SheetXml(sheetList[i].Rows));
            }
        }
        return stream.ToArray();
    }

    private static void WriteEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private const string XmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";

    private static string ContentTypesXml(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append(XmlHeader);
        sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
        sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
        for (var i = 1; i <= sheetCount; i++)
        {
            sb.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        }
        sb.Append("</Types>");
        return sb.ToString();
    }

    private const string RootRelsXml =
        XmlHeader +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    private static string WorkbookXml(List<(string Name, IEnumerable<IEnumerable<ExcelCell>> Rows)> sheets)
    {
        var sb = new StringBuilder();
        sb.Append(XmlHeader);
        sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
        var usedNames = new HashSet<string>();
        for (var i = 0; i < sheets.Count; i++)
        {
            var name = UniqueSheetName(SanitizeSheetName(sheets[i].Name), usedNames);
            sb.Append($"<sheet name=\"{Escape(name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
        }
        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private static string WorkbookRelsXml(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append(XmlHeader);
        sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
        for (var i = 1; i <= sheetCount; i++)
        {
            sb.Append($"<Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>");
        }
        sb.Append($"<Relationship Id=\"rId{sheetCount + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private const string StylesXml =
        XmlHeader +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
        "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
        "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"2\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
        "</cellXfs>" +
        "</styleSheet>";

    private static string SheetXml(IEnumerable<IEnumerable<ExcelCell>> rows)
    {
        var sb = new StringBuilder();
        sb.Append(XmlHeader);
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        var rowIndex = 1;
        foreach (var row in rows)
        {
            sb.Append($"<row r=\"{rowIndex}\">");
            var c = 0;
            foreach (var cell in row)
            {
                sb.Append(CellXml(c, rowIndex, cell.Value, cell.Bold));
                c++;
            }
            sb.Append("</row>");
            rowIndex++;
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string CellXml(int col, int row, object? value, bool bold)
    {
        var cellRef = $"{ColumnLetter(col)}{row}";
        var style = bold ? " s=\"1\"" : "";
        if (IsNumeric(value, out var number))
        {
            return $"<c r=\"{cellRef}\"{style}><v>{number.ToString(CultureInfo.InvariantCulture)}</v></c>";
        }
        var text = FormatValue(value);
        if (text.Length == 0)
        {
            return $"<c r=\"{cellRef}\"{style}/>";
        }
        return $"<c r=\"{cellRef}\"{style} t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(text)}</t></is></c>";
    }

    private static bool IsNumeric(object? value, out double number)
    {
        switch (value)
        {
            case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            default:
                number = 0;
                return false;
        }
    }

    private static string FormatValue(object? v) => v switch
    {
        null => "",
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool b => b ? "Yes" : "No",
        _ => v.ToString() ?? "",
    };

    private static string ColumnLetter(int index)
    {
        index++;
        var letter = "";
        while (index > 0)
        {
            var rem = (index - 1) % 26;
            letter = (char)('A' + rem) + letter;
            index = (index - 1) / 26;
        }
        return letter;
    }

    private static string SanitizeSheetName(string name)
    {
        var invalid = new HashSet<char> { ':', '\\', '/', '?', '*', '[', ']' };
        var clean = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        if (clean.Length > 31) clean = clean[..31];
        return clean.Length == 0 ? "Sheet" : clean;
    }

    private static string UniqueSheetName(string name, HashSet<string> used)
    {
        var candidate = name;
        var suffix = 2;
        while (!used.Add(candidate))
        {
            var basePart = name.Length > 28 ? name[..28] : name;
            candidate = $"{basePart} {suffix++}";
        }
        return candidate;
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
}

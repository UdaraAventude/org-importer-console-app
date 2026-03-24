using ClosedXML.Excel;
using OrgImporter.Models;

namespace OrgImporter.Services;

/// <summary>
/// Reads strict pink-highlighted organization records from Excel.
/// </summary>
public class ExcelReader
{
    /// <summary>Reads highlighted records from an .xlsx/.xlsm workbook.</summary>
    public List<OrganizationRecord> ReadRecords(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                $"Input file not found: {filePath}. " +
                "Provide a valid .xlsx or .xlsm path via appsettings.json or command-line argument.");

        var extension = Path.GetExtension(filePath);

        if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".xlsm", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(
                $"Unsupported input format: {extension}. Supported formats are .xlsx and .xlsm.");

        return ReadHighlightedRowsFromExcel(filePath);
    }

    private static List<OrganizationRecord> ReadHighlightedRowsFromExcel(string filePath)
    {
        var records = new List<OrganizationRecord>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook(filePath);

        foreach (var sheet in workbook.Worksheets)
        {
            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var orgCode = row.Cell(1).GetString().Trim();
                var description = row.Cell(2).GetString().Trim();

                if (string.IsNullOrEmpty(orgCode))
                    continue;

                var dedupeKey = $"{orgCode}|{description}";

                bool highlighted = IsHighlighted(row.Cell(1)) ||
                                   IsHighlighted(row.Cell(2)) ||
                                   IsHighlighted(row.Cell(3));

                if (highlighted && seen.Add(dedupeKey))
                {
                    records.Add(new OrganizationRecord
                    {
                        OrgCode = orgCode,
                        Description = description
                    });
                }
            }
        }

        return records;
    }

    /// <summary>
    /// Determines whether the cell or its row has the pink highlight convention.
    /// </summary>
    private static bool IsHighlighted(IXLCell cell)
    {
        return IsHighlightedFill(cell.Style.Fill) ||
               IsHighlightedFill(cell.WorksheetRow().Style.Fill);
    }

    private static bool IsHighlightedFill(IXLFill fill)
    {
        if (fill.PatternType == XLFillPatternValues.None)
            return false;

        if (IsPinkLike(fill.BackgroundColor) || IsPinkLike(fill.PatternColor))
            return true;

        if (fill.BackgroundColor.ColorType == XLColorType.Theme)
        {
             return fill.BackgroundColor.ThemeColor == XLThemeColor.Accent5 ||
                 fill.BackgroundColor.ThemeColor == XLThemeColor.Accent4;
        }

        if (fill.PatternColor.ColorType == XLColorType.Theme)
        {
             return fill.PatternColor.ThemeColor == XLThemeColor.Accent5 ||
                 fill.PatternColor.ThemeColor == XLThemeColor.Accent4;
        }

        return false;
    }

    private static bool IsPinkLike(XLColor color)
    {
        if (color.ColorType != XLColorType.Color)
            return false;

        var c = color.Color;

        // Accept pink/lavender tones where red+blue dominate green.
        return c.R >= 140 && c.B >= 120 &&
               c.R >= c.G + 15 && c.B >= c.G + 15;
    }
}
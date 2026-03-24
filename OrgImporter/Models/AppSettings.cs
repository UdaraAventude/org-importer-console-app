namespace OrgImporter.Models;

/// <summary>Bound from appsettings.json.</summary>
public class AppSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ExcelFilePath { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
}
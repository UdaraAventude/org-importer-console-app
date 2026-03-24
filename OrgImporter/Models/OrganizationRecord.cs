namespace OrgImporter.Models;

/// <summary>
/// Represents a single organization code record extracted
/// from the Excel file and stored in the staging table.
/// </summary>
public class OrganizationRecord
{
    /// <summary>Auto-increment primary key set by the database.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Organization code from column A of the Excel file.
    /// </summary>
    public string OrgCode { get; set; } = string.Empty;

    /// <summary>
    /// Organization name from column B of the Excel file.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>UTC timestamp set by database DEFAULT GETUTCDATE().</summary>
    public DateTime InsertedAt { get; set; }
}
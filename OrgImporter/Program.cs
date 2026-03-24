using System.Text.Json;
using OrgImporter.Models;
using OrgImporter.Services;

try
{
    var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    var settings = JsonSerializer.Deserialize<AppSettings>(
        File.ReadAllText(configPath),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException("Failed to load appsettings.json");

    var excelPath = ResolveExcelPath(args, settings.ExcelFilePath);

    var inputReader = new ExcelReader();
    var databaseService = new DatabaseService(settings.ConnectionString, settings.TableName);

    PrintHeader();

    PrintStep(1, "Reading records from input file");
    Console.WriteLine("  Excel : strict pink-only import.");

    var records = inputReader.ReadRecords(excelPath);

    Console.WriteLine($"  Found {records.Count} records.");

    if (records.Count == 0)
    {
        throw new InvalidOperationException(
            "No pink-highlighted rows found in Excel. Ensure the target rows are actually filled with pink color and saved in the workbook.");
    }

    Console.WriteLine();

    PrintStep(2, "Preparing staging table in database");

    await databaseService.EnsureTableAsync();

    Console.WriteLine($"  Table [{settings.TableName}] ready.");
    Console.WriteLine();

    PrintStep(3, "Inserting records");

    int inserted = await databaseService.InsertAllAsync(records);

    Console.WriteLine();
    Console.WriteLine($"  Inserted {inserted} records.");
    Console.WriteLine();

    PrintStep(4, "Verifying records in database");

    var dbRecords = await databaseService.ReadAllAsync();

    Console.WriteLine($"  Expected : {records.Count}");
    Console.WriteLine($"  In DB    : {dbRecords.Count}");
    Console.WriteLine();

    var missingCodes = records
        .Where(r => !dbRecords.Any(db =>
            db.OrgCode.Equals(r.OrgCode, StringComparison.OrdinalIgnoreCase)))
        .ToList();

    if (missingCodes.Count > 0)
    {
        Console.WriteLine("  MISSING RECORDS:");
        foreach (var m in missingCodes)
            Console.WriteLine($"    {m.OrgCode} — {m.Description}");
        throw new InvalidOperationException("Verification failed — missing records.");
    }

    Console.WriteLine("  VERIFICATION PASSED — All records confirmed in database.");
    Console.WriteLine();

    Console.WriteLine("  First 5 records:");
    foreach (var r in dbRecords.Take(5))
        Console.WriteLine($"    Id={r.Id,-5} {r.OrgCode,-15} {r.Description}");

    Console.WriteLine();
    Console.WriteLine("  Last 5 records:");
    foreach (var r in dbRecords.TakeLast(5))
        Console.WriteLine($"    Id={r.Id,-5} {r.OrgCode,-15} {r.Description}");

    Console.WriteLine();

    Console.WriteLine("==============================================");
    Console.WriteLine("  ALL STEPS COMPLETE");
    Console.WriteLine("==============================================");
    Console.WriteLine($"  Records inserted : {inserted}");
    Console.WriteLine($"  Records verified : {dbRecords.Count}");
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"  ERROR: {ex.Message}");
    Console.WriteLine("==============================================");
    Environment.Exit(1);
}

static void PrintHeader()
{
    Console.WriteLine("==============================================");
    Console.WriteLine("  OrgImporter — Organization Code Importer");
    Console.WriteLine("==============================================");
    Console.WriteLine();
}

static void PrintStep(int number, string description)
{
    Console.WriteLine($"STEP {number} — {description}...");
}

static string ResolveExcelPath(string[] args, string configuredPath)
{
    var argExcelPath = args.FirstOrDefault(a =>
        !string.IsNullOrWhiteSpace(a) &&
        (a.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
         a.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase)));

    var candidatePath = string.IsNullOrWhiteSpace(argExcelPath)
        ? configuredPath
        : argExcelPath;

    if (Path.IsPathRooted(candidatePath))
        return candidatePath;

    var outputRelative = Path.Combine(AppContext.BaseDirectory, candidatePath);
    if (File.Exists(outputRelative))
        return outputRelative;

    var workingRelative = Path.Combine(Directory.GetCurrentDirectory(), candidatePath);
    return workingRelative;
}
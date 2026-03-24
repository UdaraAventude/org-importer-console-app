# OrgImporter

Small .NET console application to import organization codes from pink-highlighted rows in an Excel file into a SQL Server table.

This project implements exactly this requirement:

> Read the Excel file and insert the organization code of the pink-highlighted rows into a database table. You can create a new table in your local database and work on that.

## What the app does

1. Reads an Excel workbook (`.xlsx` or `.xlsm`).
2. Scans all worksheets.
3. Selects rows marked with the pink highlight convention.
4. Reads:
   - Column A -> `OrgCode`
   - Column B -> `Description`
5. Creates (or truncates) a local staging table.
6. Inserts all selected records in one transaction.
7. Verifies inserted records.

## Project structure

- `OrgImporter/Program.cs`: app flow, configuration loading, run summary.
- `OrgImporter/Services/ExcelReader.cs`: Excel parsing and pink highlight detection.
- `OrgImporter/Services/DatabaseService.cs`: table creation, insert, and read-back verification.
- `OrgImporter/appsettings.json`: connection string, excel path, table name.

## Prerequisites

- .NET SDK 10.0
- SQL Server instance available locally (for example `localhost\\SQLEXPRESS`)
- Access to the input Excel file

## Configuration

Edit `OrgImporter/appsettings.json`:

```json
{
  "ConnectionString": "Server=localhost\\SQLEXPRESS;Database=OrgMigrationStaging;Trusted_Connection=True;TrustServerCertificate=True;",
  "ExcelFilePath": "UserAdmOrganisations.xlsx",
  "TableName": "OrganizationCodesToRemove"
}
```

Notes:
- `ExcelFilePath` can be relative or absolute.
- You can also pass the Excel path as a command-line argument.

## Run

From workspace root:

```powershell
dotnet run --project "C:\Users\AVE-LP-05\Desktop\OrgImporter\OrgImporter\OrgImporter.csproj" -- "C:\Users\AVE-LP-05\Downloads\UserAdmOrganisations (1).xlsx"
```

Or from the project folder:

```powershell
cd C:\Users\AVE-LP-05\Desktop\OrgImporter\OrgImporter
dotnet run -- "C:\Users\AVE-LP-05\Downloads\UserAdmOrganisations (1).xlsx"
```

## Build

```powershell
cd C:\Users\AVE-LP-05\Desktop\OrgImporter\OrgImporter
dotnet restore
dotnet build
```

## Database behavior

On each run:

- If table does not exist: table is created.
- If table exists: table is truncated.
- Insert happens in a transaction.
- If any row fails, transaction is rolled back.

## Verify output in SQL

```sql
SELECT Id, OrgCode, Description, InsertedAt
FROM [OrganizationCodesToRemove]
ORDER BY Id;
```

## Expected console result

A successful run ends with:

- `ALL STEPS COMPLETE`
- `Records inserted : <n>`
- `Records verified : <n>`

## Troubleshooting

### Found 0 records

- Confirm file is `.xlsx` or `.xlsm`.
- Confirm target rows are really pink-highlighted in Excel (fill color saved in workbook).
- Confirm organization code exists in Column A.

### Excel file not found

- Check the path passed in command line.
- If using config path, ensure `ExcelFilePath` is correct.

### SQL connection errors

- Verify SQL Server instance name in connection string.
- Verify database exists and your account has permissions.

## Debug in VS

1. Open Run and Debug.
2. Use a .NET launch profile for the project.
3. Set program argument to the Excel file path.
4. Put breakpoints in `Program.cs` and `ExcelReader.cs`.
5. Start debugging (`F5`).

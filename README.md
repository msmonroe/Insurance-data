# Insurance Lead Data Generator

## Purpose

This repository contains a .NET 8 console application that generates large synthetic insurance lead datasets for testing CSV import, normalization, and deduplication workflows in SQL Server. The generated data is deterministic, entirely fictional, UTF-8 encoded, and labeled so downstream processes can measure duplicate-detection accuracy.

The generator creates:

- `insurance_leads_001.csv` through `insurance_leads_010.csv`
- `insurance_leads_manifest.csv`
- SQL Server companion scripts in `/SqlScripts`

## Build Instructions

```bash
cd /home/runner/work/Insurance-data/Insurance-data
dotnet build InsuranceData.slnx
```

## Usage

Run the generator from the repository root or directly from the project folder.

```bash
dotnet run --project src/InsuranceLeadGenerator -- --output "/tmp/LeadData"
```

### Example Commands

Generate **100,000** records:

```bash
dotnet run --project src/InsuranceLeadGenerator -- --output "/tmp/LeadData100k" --records 100000 --files 2 --seed 8675309 --validation-mode
```

Generate **1,000,000** records:

```bash
dotnet run --project src/InsuranceLeadGenerator -- --output "/tmp/LeadData1m" --records 1000000 --files 5 --seed 8675309
```

Generate **50,000,000** records:

```bash
dotnet run --project src/InsuranceLeadGenerator -- --output "D:\LeadData" --records 50000000 --files 10 --seed 8675309
```

## Command-Line Options

| Option | Description | Default |
| --- | --- | --- |
| `--output` | Output directory for CSV files and the manifest | `./GeneratedLeadData` |
| `--records` | Total records to generate | `50000000` |
| `--files` | Number of CSV files to create | `10` |
| `--seed` | Deterministic pseudorandom seed | `8675309` |
| `--exact-duplicate-percent` | Approximate exact duplicate percentage | `10` |
| `--modified-duplicate-percent` | Approximate modified duplicate percentage | `16` |
| `--invalid-percent` | Approximate intentionally invalid percentage | `2` |
| `--validation-mode` | Generates a smaller dataset and fully inspects the output | Off |
| `--include-structural-errors` | Allows some intentionally malformed CSV structure in invalid rows | Off |

## Output and Disk-Space Expectations

Each file contains a header row followed by its share of generated leads. With the default 50,000,000-record configuration, expect approximately:

- **10 CSV files** with **5,000,000 records each**
- **12 GB to 18 GB** of CSV output depending on duplicate mix and invalid-row text lengths
- A small manifest file with per-file row counts, duplicate totals, file size, timestamps, and SHA-256 checksums

## Duplicate Types

`DuplicateType` is emitted in every row to support deduplication validation.

- `UNIQUE`
- `EXACT`
- `EMAIL_CHANGED`
- `PHONE_CHANGED`
- `ADDRESS_CHANGED`
- `NAME_VARIATION`
- `FORMATTING_VARIATION`
- `MULTIPLE_FIELDS_CHANGED`
- `INTENTIONALLY_INVALID`

`OriginalLeadId` is blank for unique and intentionally invalid rows. Duplicate rows reference the lead id of the earlier synthetic record they were derived from.

## Deterministic Seeds

The generator uses a lightweight deterministic pseudorandom number generator instead of expensive cryptographic randomness. The same seed and record/file configuration produce the same logical sequence of synthetic people, duplicate labels, and dirty-data mutations for a given run configuration.

## Validation Mode

Validation mode is intended for 10,000- to 100,000-record dry runs.

```bash
dotnet run --project src/InsuranceLeadGenerator -- --output "/tmp/LeadValidation" --records 100000 --files 2 --validation-mode
```

Validation mode performs extra checks, including:

- header verification
- unique `LeadId` validation
- exact duplicate comparisons against original rows
- modified duplicate mutation checks
- CSV quoting/escaping verification by reparsing the generated files
- duplicate percentage sanity checks

## SQL Server Import Guidance

The `/SqlScripts` folder includes:

1. `01_CreateRawLeadTable.sql`
2. `02_CreateImportBatchTable.sql`
3. `03_CreateDuplicateAuditTable.sql`
4. `04_BulkImportExample.sql`
5. `05_NormalizationExample.sql`
6. `06_ExactDeduplicationExample.sql`
7. `07_DeduplicationValidation.sql`

Suggested import order:

1. Create the raw and audit tables.
2. Load one generated file with `BULK INSERT` or `bcp`.
3. Update the import batch metadata.
4. Normalize the raw string fields using the sample view.
5. Compare import counts against the manifest.
6. Test exact and fuzzy deduplication logic against `DuplicateType` and `OriginalLeadId`.

### Verify Imported Row Counts

After import:

```sql
SELECT COUNT(*) AS ImportedRows FROM dbo.RawInsuranceLead;
SELECT DuplicateType, COUNT(*) AS GeneratedRows FROM dbo.RawInsuranceLead GROUP BY DuplicateType;
```

Compare those totals to `insurance_leads_manifest.csv` and the expected file sizes/checksums.

## Safe Stop and Resume

The application handles `Ctrl+C` gracefully:

- the active file writer is flushed and closed
- completed files remain on disk
- a manifest is written for generated files
- the process exits with a nonzero exit code on cancellation

The generator does **not** resume from the middle of a partially written file. The recommended approach is to generate into a fresh output directory or delete any incomplete file before rerunning.

## Performance Notes

The application is designed for large output volumes and uses:

- streamed row generation
- asynchronous buffered file I/O
- bounded-memory duplicate source tracking
- deterministic regeneration of base records
- reusable `StringBuilder` buffers in the CSV writer
- periodic progress messages with rows/sec, elapsed time, current file number, total rows written, and approximate output size

### Performance-Tuning Suggestions

- Generate to a fast local SSD or high-throughput attached volume.
- Keep `--files` at 10 unless downstream tooling benefits from a different split.
- Test with `100000` or `1000000` rows before starting a 50,000,000-row run.
- Avoid antivirus or backup tools scanning the output directory during generation.
- Run imports on the same machine or storage tier when possible to reduce file-copy time.

## Known Limitations

- Structural CSV corruption is only introduced when `--include-structural-errors` is supplied.
- Validation mode rereads generated files and is intentionally meant for smaller sample sizes, not the full 50,000,000-row run.
- Resume is file-based rather than checkpoint-based.
- The SQL scripts are examples and should be adapted to local database names, file paths, and operational controls.

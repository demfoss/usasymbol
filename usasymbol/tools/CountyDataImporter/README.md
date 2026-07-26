# County data importer

Build-time importer for county metrics. It downloads official source files,
joins records only by five-digit county FIPS, writes one canonical YAML file per
county, and emits a compact JSON index for the web runtime.

Run from the repository root:

```powershell
dotnet run --project tools/CountyDataImporter/CountyDataImporter.csproj
```

Downloaded source files are cached under `tools/CountyDataImporter/cache/` and
are intentionally excluded from source control. Pass `--refresh` to download
them again.

Sources:

- U.S. Census Bureau, 2024 ACS 5-year table-based summary files
- U.S. Bureau of Labor Statistics, LAUS county time series
- County Health Rankings & Roadmaps, 2025 analytic data

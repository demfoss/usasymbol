param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$statesDirectory = Join-Path $ProjectRoot 'Content/states'
$listingPath = Join-Path $ProjectRoot 'Content/symbols/insects.yml'

function Read-TopLevelYamlValue {
    param(
        [string]$Yaml,
        [string]$Key
    )

    $match = [regex]::Match(
        $Yaml,
        "(?m)^$([regex]::Escape($Key)):\s*(.+?)\s*$")

    if (-not $match.Success) {
        return ''
    }

    return $match.Groups[1].Value.Trim().Trim('"', "'")
}

function ConvertTo-SymbolSlug {
    param([string]$Name)

    $normalized = $Name.ToLowerInvariant().Normalize(
        [System.Text.NormalizationForm]::FormD)
    $builder = [System.Text.StringBuilder]::new()

    foreach ($character in $normalized.ToCharArray()) {
        $category = [Globalization.CharUnicodeInfo]::GetUnicodeCategory($character)
        if ($category -eq [Globalization.UnicodeCategory]::NonSpacingMark) {
            continue
        }

        if ([char]::IsLetterOrDigit($character)) {
            [void]$builder.Append($character)
        }
        elseif ($character -in @(' ', '-')) {
            [void]$builder.Append('-')
        }
    }

    return ([regex]::Replace($builder.ToString(), '-+', '-')).Trim('-')
}

function Quote-Yaml {
    param([string]$Value)

    return '"' + $Value.Replace('\', '\\').Replace('"', '\"') + '"'
}

$rows = foreach ($file in Get-ChildItem -LiteralPath $statesDirectory -Recurse -File -Filter 'insect*.yaml') {
    $yaml = Get-Content -Raw -LiteralPath $file.FullName
    $stateSlug = $file.Directory.Name
    $name = Read-TopLevelYamlValue -Yaml $yaml -Key 'name'

    [pscustomobject]@{
        State = Read-TopLevelYamlValue -Yaml $yaml -Key 'state'
        StateSlug = $stateSlug
        Name = $name
        ScientificName = Read-TopLevelYamlValue -Yaml $yaml -Key 'binomial_name'
        Designation = Read-TopLevelYamlValue -Yaml $yaml -Key 'designation'
        AdoptedYear = Read-TopLevelYamlValue -Yaml $yaml -Key 'adopted_year'
        Image = Read-TopLevelYamlValue -Yaml $yaml -Key 'hero_image'
        Slug = ConvertTo-SymbolSlug -Name $name
    }
}

$rows = $rows | Sort-Object State, Designation, Name
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('table:')
$lines.Add('  columns:')
$lines.Add('    symbol_image: "Image"')
$lines.Add('    state: "State"')
$lines.Add('    insect: "Insect"')
$lines.Add('    scientific_name: "Scientific Name"')
$lines.Add('    designation: "Designation"')
$lines.Add('    adopted_year: "Adopted"')
$lines.Add('')
$lines.Add('  rows:')

foreach ($row in $rows) {
    $lines.Add("    - symbol_image: $(Quote-Yaml $row.Image)")
    $lines.Add("      state: $(Quote-Yaml $row.State)")
    $lines.Add("      state_slug: $(Quote-Yaml $row.StateSlug)")
    $lines.Add("      insect: $(Quote-Yaml $row.Name)")
    $lines.Add("      scientific_name: $(Quote-Yaml $row.ScientificName)")
    $lines.Add("      insect_slug: $(Quote-Yaml $row.Slug)")
    $lines.Add("      designation: $(Quote-Yaml $row.Designation)")
    $lines.Add("      adopted_year: $($row.AdoptedYear)")
    $lines.Add("      symbol_url: $(Quote-Yaml "/states/$($row.StateSlug)/insect/$($row.Slug)")")
    $lines.Add('')
}

$listing = Get-Content -Raw -LiteralPath $listingPath
$tableBlock = ($lines -join "`n") + "`n"
$updated = [regex]::Replace(
    $listing,
    '(?ms)^table:\r?\n.*?(?=^faq:)',
    $tableBlock)

if ($updated -eq $listing) {
    throw "Could not replace the table block in $listingPath."
}

[System.IO.File]::WriteAllText(
    $listingPath,
    $updated,
    [System.Text.UTF8Encoding]::new($false))

Write-Output "Updated $listingPath with $($rows.Count) insect rows."

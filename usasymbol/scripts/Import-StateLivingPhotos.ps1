param(
    [Parameter(Mandatory = $true)]
    [string]$PexelsKey,

    [Parameter(Mandatory = $true)]
    [string]$PixabayKey,

    [int]$PhotosPerState = 4
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$parkContentRoot = Join-Path $projectRoot 'Content\parks\national'
$manifestRoot = Join-Path $projectRoot 'Content\state-living'
$imageRoot = Join-Path $projectRoot 'wwwroot\images\state-living'

$states = @(
    @{ Name = 'Alabama'; Slug = 'alabama'; Code = 'AL' },
    @{ Name = 'Alaska'; Slug = 'alaska'; Code = 'AK' },
    @{ Name = 'Arizona'; Slug = 'arizona'; Code = 'AZ' },
    @{ Name = 'Arkansas'; Slug = 'arkansas'; Code = 'AR' },
    @{ Name = 'California'; Slug = 'california'; Code = 'CA' },
    @{ Name = 'Colorado'; Slug = 'colorado'; Code = 'CO' },
    @{ Name = 'Connecticut'; Slug = 'connecticut'; Code = 'CT' },
    @{ Name = 'Delaware'; Slug = 'delaware'; Code = 'DE' },
    @{ Name = 'Florida'; Slug = 'florida'; Code = 'FL' },
    @{ Name = 'Georgia'; Slug = 'georgia'; Code = 'GA' },
    @{ Name = 'Hawaii'; Slug = 'hawaii'; Code = 'HI' },
    @{ Name = 'Idaho'; Slug = 'idaho'; Code = 'ID' },
    @{ Name = 'Illinois'; Slug = 'illinois'; Code = 'IL' },
    @{ Name = 'Indiana'; Slug = 'indiana'; Code = 'IN' },
    @{ Name = 'Iowa'; Slug = 'iowa'; Code = 'IA' },
    @{ Name = 'Kansas'; Slug = 'kansas'; Code = 'KS' },
    @{ Name = 'Kentucky'; Slug = 'kentucky'; Code = 'KY' },
    @{ Name = 'Louisiana'; Slug = 'louisiana'; Code = 'LA' },
    @{ Name = 'Maine'; Slug = 'maine'; Code = 'ME' },
    @{ Name = 'Maryland'; Slug = 'maryland'; Code = 'MD' },
    @{ Name = 'Massachusetts'; Slug = 'massachusetts'; Code = 'MA' },
    @{ Name = 'Michigan'; Slug = 'michigan'; Code = 'MI' },
    @{ Name = 'Minnesota'; Slug = 'minnesota'; Code = 'MN' },
    @{ Name = 'Mississippi'; Slug = 'mississippi'; Code = 'MS' },
    @{ Name = 'Missouri'; Slug = 'missouri'; Code = 'MO' },
    @{ Name = 'Montana'; Slug = 'montana'; Code = 'MT' },
    @{ Name = 'Nebraska'; Slug = 'nebraska'; Code = 'NE' },
    @{ Name = 'Nevada'; Slug = 'nevada'; Code = 'NV' },
    @{ Name = 'New Hampshire'; Slug = 'new-hampshire'; Code = 'NH' },
    @{ Name = 'New Jersey'; Slug = 'new-jersey'; Code = 'NJ' },
    @{ Name = 'New Mexico'; Slug = 'new-mexico'; Code = 'NM' },
    @{ Name = 'New York'; Slug = 'new-york'; Code = 'NY' },
    @{ Name = 'North Carolina'; Slug = 'north-carolina'; Code = 'NC' },
    @{ Name = 'North Dakota'; Slug = 'north-dakota'; Code = 'ND' },
    @{ Name = 'Ohio'; Slug = 'ohio'; Code = 'OH' },
    @{ Name = 'Oklahoma'; Slug = 'oklahoma'; Code = 'OK' },
    @{ Name = 'Oregon'; Slug = 'oregon'; Code = 'OR' },
    @{ Name = 'Pennsylvania'; Slug = 'pennsylvania'; Code = 'PA' },
    @{ Name = 'Rhode Island'; Slug = 'rhode-island'; Code = 'RI' },
    @{ Name = 'South Carolina'; Slug = 'south-carolina'; Code = 'SC' },
    @{ Name = 'South Dakota'; Slug = 'south-dakota'; Code = 'SD' },
    @{ Name = 'Tennessee'; Slug = 'tennessee'; Code = 'TN' },
    @{ Name = 'Texas'; Slug = 'texas'; Code = 'TX' },
    @{ Name = 'Utah'; Slug = 'utah'; Code = 'UT' },
    @{ Name = 'Vermont'; Slug = 'vermont'; Code = 'VT' },
    @{ Name = 'Virginia'; Slug = 'virginia'; Code = 'VA' },
    @{ Name = 'Washington'; Slug = 'washington'; Code = 'WA' },
    @{ Name = 'West Virginia'; Slug = 'west-virginia'; Code = 'WV' },
    @{ Name = 'Wisconsin'; Slug = 'wisconsin'; Code = 'WI' },
    @{ Name = 'Wyoming'; Slug = 'wyoming'; Code = 'WY' },
    @{ Name = 'District of Columbia'; Slug = 'district-of-columbia'; Code = 'DC' }
)

$queryOverrides = @{
    'district-of-columbia' = 'Washington DC skyline landscape'
    'georgia' = 'Georgia USA landscape'
    'new-york' = 'New York State landscape'
    'washington' = 'Washington State USA landscape'
}

function Normalize-SearchText([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ''
    }

    return (($Value.ToLowerInvariant() -replace '[^a-z0-9]+', ' ').Trim())
}

$allStateNames = @($states | ForEach-Object { Normalize-SearchText $_.Name })

function Test-ExactMatch([string]$Text, [hashtable]$State) {
    $normalized = Normalize-SearchText $Text
    $stateName = Normalize-SearchText $State.Name

    if ($State.Code -eq 'DC') {
        $matchesCurrent = $normalized.Contains('washington dc') -or
            $normalized.Contains('district of columbia')
    }
    else {
        $matchesCurrent = $normalized.Contains($stateName)
    }

    if (-not $matchesCurrent) {
        return $false
    }

    foreach ($otherState in $allStateNames) {
        if ($otherState -eq $stateName) {
            continue
        }
        if ($normalized.Contains($otherState)) {
            return $false
        }
    }

    if ($State.Code -eq 'WA' -and
        ($normalized.Contains('washington dc') -or $normalized.Contains('district of columbia'))) {
        return $false
    }

    return $true
}

function Test-ScenicCandidate([string]$Text, [hashtable]$State) {
    $normalized = Normalize-SearchText $Text
    $excluded = @(
        ' dog ', ' cat ', ' portrait ', ' close up ', ' closeup ', ' detail ',
        ' frog ', ' koi ', ' indoor ', ' food ', ' selfie '
    )
    $padded = " $normalized "
    foreach ($term in $excluded) {
        if ($padded.Contains($term)) {
            return $false
        }
    }

    if ($State.Slug -eq 'mississippi' -and $padded.Contains(' mississippi river ')) {
        return $false
    }

    return $true
}

function Get-ScenicScore([string]$Text) {
    $normalized = Normalize-SearchText $Text
    $score = 0
    foreach ($term in @(
        'landscape', 'scenic', 'nature', 'sunset', 'sunrise', 'park', 'lake',
        'ocean', 'mountain', 'forest', 'beach', 'field', 'skyline', 'wetlands',
        'canyon', 'farm', 'foliage', 'coast', 'waterfall', 'prairie', 'river'
    )) {
        if ($normalized.Contains($term)) {
            $score++
        }
    }
    return $score
}

function Get-PixabayAlt([string]$Tags, [hashtable]$State) {
    $seen = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    $excluded = @(
        (Normalize-SearchText $State.Name), 'usa', 'america', 'united states',
        'landscape', 'nature', 'outdoors', 'outside', 'travel', 'scenic',
        'green', 'blue', 'brown', 'black', 'beautiful'
    )
    $descriptors = [System.Collections.Generic.List[string]]::new()

    foreach ($rawTag in ($Tags -split ',')) {
        $tag = (Normalize-SearchText $rawTag).Trim()
        if ([string]::IsNullOrWhiteSpace($tag) -or
            $excluded -contains $tag -or
            -not $seen.Add($tag)) {
            continue
        }
        $descriptors.Add($tag)
        if ($descriptors.Count -ge 4) {
            break
        }
    }

    if ($descriptors.Count -eq 0) {
        return "$($State.Name) scenery"
    }
    return "$($State.Name) scenery featuring $($descriptors -join ', ')"
}

function Select-DiverseCandidates([object[]]$Candidates, [int]$Needed) {
    $selected = [System.Collections.Generic.List[hashtable]]::new()
    $creditCounts = @{}

    foreach ($candidate in $Candidates |
        Sort-Object @{ Expression = { Get-ScenicScore $_.Alt }; Descending = $true }) {
        $creditKey = [string]$candidate.Credit
        $creditCount = if ($creditCounts.ContainsKey($creditKey)) {
            [int]$creditCounts[$creditKey]
        }
        else {
            0
        }

        if ($creditCount -ge 2) {
            continue
        }

        $selected.Add($candidate)
        $creditCounts[$creditKey] = $creditCount + 1
        if ($selected.Count -ge $Needed) {
            break
        }
    }

    return @($selected)
}

function Get-ParkCoveredCodes {
    $covered = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)

    foreach ($file in Get-ChildItem -LiteralPath $parkContentRoot -Filter '*.yml' -File) {
        $yaml = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)

        foreach ($match in [regex]::Matches($yaml, '(?m)^\s*state_code:\s*["'']?([A-Z]{2})')) {
            [void]$covered.Add($match.Groups[1].Value)
        }

        foreach ($match in [regex]::Matches($yaml, '(?ms)^\s*state_codes:\s*\r?\n((?:\s*-\s*[A-Z]{2}\s*\r?\n?)+)')) {
            foreach ($code in [regex]::Matches($match.Groups[1].Value, '[A-Z]{2}')) {
                [void]$covered.Add($code.Value)
            }
        }

        foreach ($match in [regex]::Matches($yaml, '(?m)^\s*state_codes:\s*\[([^\]]+)\]')) {
            foreach ($code in [regex]::Matches($match.Groups[1].Value, '[A-Z]{2}')) {
                [void]$covered.Add($code.Value)
            }
        }
    }

    return $covered
}

function Get-PexelsCandidates([hashtable]$State, [int]$Needed) {
    $query = if ($queryOverrides.ContainsKey($State.Slug)) {
        $queryOverrides[$State.Slug]
    }
    else {
        "$($State.Name) USA landscape"
    }

    $uri = 'https://api.pexels.com/v1/search?query=' +
        [uri]::EscapeDataString($query) +
        '&orientation=landscape&size=large&per_page=40'
    $result = Invoke-RestMethod -Uri $uri -Headers @{ Authorization = $PexelsKey }

    $candidates = @(
        $result.photos |
            Where-Object {
                (Test-ExactMatch "$($_.alt) $($_.url)" $State) -and
                (Test-ScenicCandidate "$($_.alt) $($_.url)" $State)
            } |
            ForEach-Object {
                @{
                    Provider = 'pexels'
                    Id = [string]$_.id
                    DownloadUrl = [string]$_.src.large
                    PageUrl = [string]$_.url
                    Credit = "Photo by $($_.photographer) on Pexels"
                    CreditUrl = [string]$_.photographer_url
                    Alt = if ([string]::IsNullOrWhiteSpace($_.alt)) {
                        "$($State.Name) landscape"
                    }
                    else {
                        [string]$_.alt
                    }
                }
            }
    )
    return @(Select-DiverseCandidates $candidates $Needed)
}

function Get-PixabayCandidates([hashtable]$State, [int]$Needed) {
    $query = if ($State.Code -eq 'DC') {
        'Washington DC landscape'
    }
    elseif ($State.Code -eq 'WA') {
        'Washington State landscape'
    }
    else {
        "$($State.Name) landscape"
    }

    $uri = 'https://pixabay.com/api/?key=' +
        [uri]::EscapeDataString($PixabayKey) +
        '&q=' + [uri]::EscapeDataString($query) +
        '&image_type=photo&orientation=horizontal&safesearch=true&per_page=50'
    $result = Invoke-RestMethod -Uri $uri

    $candidates = @(
        $result.hits |
            Where-Object {
                (Test-ExactMatch "$($_.tags) $($_.pageURL)" $State) -and
                (Test-ScenicCandidate "$($_.tags) $($_.pageURL)" $State)
            } |
            ForEach-Object {
                @{
                    Provider = 'pixabay'
                    Id = [string]$_.id
                    DownloadUrl = [string]$_.largeImageURL
                    PageUrl = [string]$_.pageURL
                    Credit = "Image by $($_.user) on Pixabay"
                    CreditUrl = "https://pixabay.com/users/$($_.user)-$($_.user_id)/"
                    Alt = Get-PixabayAlt ([string]$_.tags) $State
                }
            }
    )
    return @(Select-DiverseCandidates $candidates $Needed)
}

function Save-Candidate(
    [hashtable]$Candidate,
    [hashtable]$State,
    [string]$StateImageRoot
) {
    $fileName = "$($Candidate.Provider)-$($Candidate.Id).jpg"
    $filePath = Join-Path $StateImageRoot $fileName

    if (-not (Test-Path -LiteralPath $filePath)) {
        Invoke-WebRequest -Uri $Candidate.DownloadUrl -OutFile $filePath -UseBasicParsing
    }

    $file = Get-Item -LiteralPath $filePath
    if ($file.Length -lt 20000) {
        throw "Downloaded image is unexpectedly small: $filePath ($($file.Length) bytes)"
    }

    return [ordered]@{
        imageUrl = "/images/state-living/$($State.Slug)/$fileName"
        alt = $Candidate.Alt
        credit = $Candidate.Credit
        creditUrl = $Candidate.CreditUrl
        locationName = "$($State.Name) scenery"
        locationUrl = $Candidate.PageUrl
        sourceName = if ($Candidate.Provider -eq 'pexels') { 'Pexels' } else { 'Pixabay' }
    }
}

[System.IO.Directory]::CreateDirectory($manifestRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($imageRoot) | Out-Null

$parkCoveredCodes = Get-ParkCoveredCodes
$statesToImport = @($states | Where-Object {
    $_.Code -ne 'DC' -and -not $parkCoveredCodes.Contains($_.Code)
})
$manifestStates = [ordered]@{}
$report = [System.Collections.Generic.List[object]]::new()

foreach ($state in $statesToImport) {
    $stateImageRoot = Join-Path $imageRoot $state.Slug
    [System.IO.Directory]::CreateDirectory($stateImageRoot) | Out-Null

    $candidates = [System.Collections.Generic.List[hashtable]]::new()
    if ($state.Slug -ne 'mississippi') {
        foreach ($candidate in Get-PexelsCandidates $state $PhotosPerState) {
            $candidates.Add($candidate)
        }
    }

    if ($candidates.Count -lt $PhotosPerState) {
        $needed = $PhotosPerState - $candidates.Count
        foreach ($candidate in Get-PixabayCandidates $state $needed) {
            $candidates.Add($candidate)
        }
    }

    $photos = [System.Collections.Generic.List[object]]::new()
    foreach ($candidate in $candidates | Select-Object -First $PhotosPerState) {
        $photos.Add((Save-Candidate $candidate $state $stateImageRoot))
    }

    if ($photos.Count -gt 0) {
        $manifestStates[$state.Slug] = @($photos)
    }

    $selectedNames = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($photo in $photos) {
        [void]$selectedNames.Add([System.IO.Path]::GetFileName([string]$photo.imageUrl))
    }
    $resolvedImageRoot = [System.IO.Path]::GetFullPath($imageRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $resolvedStateRoot = [System.IO.Path]::GetFullPath($stateImageRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedStateRoot.StartsWith($resolvedImageRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to prune files outside the state-living image root: $resolvedStateRoot"
    }
    foreach ($file in Get-ChildItem -LiteralPath $stateImageRoot -File) {
        if (-not $selectedNames.Contains($file.Name)) {
            Remove-Item -LiteralPath $file.FullName
        }
    }

    $report.Add([pscustomobject]@{
        State = $state.Name
        Imported = $photos.Count
        Pexels = @($candidates | Where-Object Provider -eq 'pexels').Count
        Pixabay = @($candidates | Where-Object Provider -eq 'pixabay').Count
    })
}

$manifest = [ordered]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString('O')
    photosPerState = $PhotosPerState
    states = $manifestStates
}

$json = $manifest | ConvertTo-Json -Depth 8
$manifestPath = Join-Path $manifestRoot 'photos.json'
[System.IO.File]::WriteAllText(
    $manifestPath,
    $json,
    [System.Text.UTF8Encoding]::new($false))

$report | Format-Table -AutoSize
"Manifest: $manifestPath"
"Park-covered states skipped: $($parkCoveredCodes.Count)"
"External-photo states written: $($manifestStates.Count)"

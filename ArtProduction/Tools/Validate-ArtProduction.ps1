[CmdletBinding()]
param(
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'

$artRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = Split-Path -Parent $artRoot
$validationEvidenceRoot = Join-Path $artRoot 'Validation'
$generatedReportRoot = Join-Path $artRoot 'GeneratedReports'
$indexPath = Join-Path $artRoot 'ART_PRODUCTION_INDEX.csv'
$manifestPath = Join-Path $artRoot 'MANIFEST.json'
$jsonReportPath = Join-Path $generatedReportRoot 'ART_PRODUCTION_VALIDATION.json'
$markdownReportPath = Join-Path $generatedReportRoot 'ART_PRODUCTION_VALIDATION.md'
$workbookValidationPath = Join-Path $validationEvidenceRoot 'WORKBOOK_LIVE_VALIDATION.json'

$task014 = Join-Path $artRoot 'IdleMedievalLegends_Task014_VisualBible'
$task015 = Join-Path $artRoot 'IdleMedievalLegends_Task015_AssetCatalog'
$task016 = Join-Path $artRoot 'IdleMedievalLegends_Task016_CharacterDesignPack'
$task017 = Join-Path $artRoot 'IdleMedievalLegends_Task017_EquipmentTierRarity'
$task018 = Join-Path $artRoot 'IdleMedievalLegends_Task018_EnvironmentStations'
$task019 = Join-Path $artRoot 'IdleMedievalLegends_Task019_MeshyOperations'

$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$pathChecks = 0
$checksumEntries = 0
$manifestPathChecks = 0
$readablePackageFiles = 0
$canonicalSourcesChecked = 0
$promptMirrorsChecked = 0

function Add-Error
{
    param([string]$Message)

    $script:errors.Add($Message)
}

function Convert-ToForwardSlash
{
    param([string]$Path)

    return $Path.Replace('\', '/')
}

function Test-IsAbsoluteMachinePath
{
    param([string]$Path)

    if (-not $Path)
    {
        return $false
    }

    return [IO.Path]::IsPathRooted($Path) -or
        $Path -match '^[A-Za-z]:[\\/]' -or
        $Path -match '^/(?:Users|home)/' -or
        $Path.StartsWith('\\')
}

function Resolve-RepositoryPath
{
    param([string]$RelativePath)

    return Join-Path $repositoryRoot (Convert-ToForwardSlash $RelativePath)
}

function Get-ArtRelativePath
{
    param([string]$FullPath)

    $relativePath = [IO.Path]::GetRelativePath($artRoot, $FullPath)
    return Convert-ToForwardSlash $relativePath
}

function Assert-RepositoryPath
{
    param(
        [string]$RelativePath,
        [string]$Label,
        [switch]$Directory
    )

    if (-not $RelativePath)
    {
        Add-Error "$Label possui caminho vazio."
        return $null
    }
    if (Test-IsAbsoluteMachinePath $RelativePath)
    {
        Add-Error "$Label deve ser relativo ao repositório: $RelativePath"
        return $null
    }
    if ($RelativePath -match '\\')
    {
        Add-Error "$Label deve usar barras normais: $RelativePath"
        return $null
    }

    $candidate = Resolve-RepositoryPath $RelativePath
    $fullCandidate = [IO.Path]::GetFullPath($candidate)
    $fullRepositoryRoot = [IO.Path]::GetFullPath($repositoryRoot)
    $relativeToRepository = Convert-ToForwardSlash ([IO.Path]::GetRelativePath($fullRepositoryRoot, $fullCandidate))
    if ($relativeToRepository -eq '..' -or
        $relativeToRepository.StartsWith('../') -or
        [IO.Path]::IsPathRooted($relativeToRepository))
    {
        Add-Error "$Label escapa da raiz do repositório: $RelativePath"
        return $null
    }

    $script:manifestPathChecks++
    $pathType = if ($Directory) { 'Container' } else { 'Leaf' }
    if (-not (Test-Path -LiteralPath $fullCandidate -PathType $pathType))
    {
        Add-Error "$Label referencia caminho ausente: $RelativePath"
        return $null
    }

    return $fullCandidate
}

function Import-Catalog
{
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf))
    {
        Add-Error "Catálogo ausente: $(Get-ArtRelativePath $Path)"
        return @()
    }

    return @(Import-Csv -LiteralPath $Path)
}

function Assert-ValidAssetIds
{
    param(
        [object[]]$Rows,
        [string]$IdColumn,
        [string]$Label
    )

    foreach ($row in $Rows)
    {
        $id = [string]$row.$IdColumn
        if ($id -cnotmatch '^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$')
        {
            Add-Error "$Label contém ID fora da convenção snake_case ASCII: $id"
        }
    }
}

function New-IdMap
{
    param(
        [object[]]$Rows,
        [string]$IdColumn
    )

    $map = @{}
    foreach ($row in $Rows)
    {
        $id = [string]$row.$IdColumn
        if ($id)
        {
            $map[$id] = $row
        }
    }

    return $map
}

function Assert-Unique
{
    param(
        [object[]]$Rows,
        [string[]]$Columns,
        [string]$Label
    )

    $seen = @{}
    foreach ($row in $Rows)
    {
        $keyParts = foreach ($column in $Columns)
        {
            [string]$row.$column
        }
        $key = $keyParts -join '|'
        if (-not $key)
        {
            Add-Error "$Label contém uma chave vazia."
            continue
        }
        if ($seen.ContainsKey($key))
        {
            Add-Error "$Label contém chave duplicada: $key"
        }
        else
        {
            $seen[$key] = $true
        }
    }
}

function Assert-SameSet
{
    param(
        [string[]]$Expected,
        [string[]]$Actual,
        [string]$Label
    )

    $difference = @(Compare-Object -ReferenceObject @($Expected | Sort-Object -Unique) -DifferenceObject @($Actual | Sort-Object -Unique))
    foreach ($item in $difference)
    {
        Add-Error "$Label diverge em '$($item.InputObject)' ($($item.SideIndicator))."
    }
}

function Assert-Subset
{
    param(
        [string[]]$Values,
        [string[]]$Allowed,
        [string]$Label
    )

    $allowedSet = @{}
    foreach ($value in $Allowed)
    {
        $allowedSet[[string]$value] = $true
    }

    foreach ($value in @($Values | Sort-Object -Unique))
    {
        if ($value -and -not $allowedSet.ContainsKey([string]$value))
        {
            Add-Error "$Label referencia ID inexistente: $value"
        }
    }
}

function Assert-PackagePath
{
    param(
        [string]$PackageRoot,
        [string]$RelativePath,
        [string]$Label
    )

    if (-not $RelativePath)
    {
        Add-Error "$Label possui caminho vazio."
        return
    }
    if ($RelativePath -match '^(Assets/|https?://)' -or
        (Test-IsAbsoluteMachinePath $RelativePath) -or
        $RelativePath -match '\\' -or
        $RelativePath -match '(^|/)\.\.(/|$)')
    {
        Add-Error "$Label usa caminho não relativo ao pacote: $RelativePath"
        return
    }

    $script:pathChecks++
    $candidate = Join-Path $PackageRoot $RelativePath
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf))
    {
        Add-Error "$Label não resolve a partir da raiz do pacote: $RelativePath"
    }
}

function Assert-DocumentPath
{
    param(
        [string]$DocumentPath,
        [string]$RelativePath,
        [string]$Label
    )

    $script:pathChecks++
    $documentRoot = Split-Path -Parent $DocumentPath
    if (Test-IsAbsoluteMachinePath $RelativePath)
    {
        Add-Error "$Label usa caminho absoluto de máquina: $RelativePath"
        return
    }
    $candidate = Join-Path $documentRoot $RelativePath
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf))
    {
        Add-Error "$Label não resolve a partir do documento: $RelativePath"
    }
}

function Assert-ChecksumManifest
{
    param([string]$PackageRoot)

    $manifestPath = Join-Path $PackageRoot 'SHA256SUMS.txt'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf))
    {
        return
    }

    foreach ($line in Get-Content -LiteralPath $manifestPath)
    {
        if ($line -notmatch '^([0-9a-fA-F]{64})  (.+)$')
        {
            Add-Error "Linha inválida em $(Get-ArtRelativePath $manifestPath): $line"
            continue
        }

        $script:checksumEntries++
        $expected = $matches[1].ToLowerInvariant()
        $relativePath = $matches[2]
        $candidate = Join-Path $PackageRoot $relativePath
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf))
        {
            Add-Error "Checksum referencia arquivo ausente: $(Get-ArtRelativePath $candidate)"
            continue
        }

        $actual = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $expected)
        {
            Add-Error "Checksum divergente: $(Get-ArtRelativePath $candidate)"
        }
    }
}

function Assert-Snapshot
{
    param(
        [string]$UpstreamRoot,
        [string]$SnapshotRoot
    )

    foreach ($snapshotFile in Get-ChildItem -LiteralPath $SnapshotRoot -Recurse -File)
    {
        $relativePath = [IO.Path]::GetRelativePath($SnapshotRoot, $snapshotFile.FullName)
        $upstreamFile = Join-Path $UpstreamRoot $relativePath
        if (-not (Test-Path -LiteralPath $upstreamFile -PathType Leaf))
        {
            Add-Error "Snapshot sem origem: $(Get-ArtRelativePath $snapshotFile.FullName)"
            continue
        }

        $snapshotHash = (Get-FileHash -LiteralPath $snapshotFile.FullName -Algorithm SHA256).Hash
        $upstreamHash = (Get-FileHash -LiteralPath $upstreamFile -Algorithm SHA256).Hash
        if ($snapshotHash -ne $upstreamHash)
        {
            Add-Error "Snapshot divergiu da origem: $(Get-ArtRelativePath $snapshotFile.FullName)"
        }
    }
}

function Assert-MirroredPromptTree
{
    param(
        [string]$SourceRoot,
        [string]$MirrorRoot,
        [string]$Label
    )

    $sourcePrompts = @(Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Filter *.txt)
    $mirrorPrompts = @(Get-ChildItem -LiteralPath $MirrorRoot -Recurse -File -Filter *.txt)
    $sourceRelativePaths = @($sourcePrompts | ForEach-Object {
        Convert-ToForwardSlash ([IO.Path]::GetRelativePath($SourceRoot, $_.FullName))
    })
    $mirrorRelativePaths = @($mirrorPrompts | ForEach-Object {
        Convert-ToForwardSlash ([IO.Path]::GetRelativePath($MirrorRoot, $_.FullName))
    })
    Assert-SameSet $sourceRelativePaths $mirrorRelativePaths "$Label caminhos"

    foreach ($sourcePrompt in $sourcePrompts)
    {
        $relativePath = [IO.Path]::GetRelativePath($SourceRoot, $sourcePrompt.FullName)
        $mirrorPrompt = Join-Path $MirrorRoot $relativePath
        if (-not (Test-Path -LiteralPath $mirrorPrompt -PathType Leaf))
        {
            continue
        }

        $script:promptMirrorsChecked++
        $sourceHash = (Get-FileHash -LiteralPath $sourcePrompt.FullName -Algorithm SHA256).Hash
        $mirrorHash = (Get-FileHash -LiteralPath $mirrorPrompt -Algorithm SHA256).Hash
        if ($sourceHash -ne $mirrorHash)
        {
            Add-Error "Prompt operacional divergiu da origem em $Label/$relativePath"
        }
    }
}

function Assert-PromptPathConvention
{
    param(
        [string]$RelativePath,
        [string]$Label
    )

    if ($RelativePath -notmatch '^[A-Za-z0-9_./-]+\.txt$')
    {
        Add-Error "$Label possui nome de prompt fora da convenção: $RelativePath"
    }
}

function Assert-PackageHygiene
{
    param([string]$PackageRoot)

    $archiveExtensions = @('.zip', '.7z', '.rar', '.tar', '.gz')
    $textExtensions = @('.csv', '.json', '.md', '.txt', '.yaml', '.yml')

    foreach ($file in Get-ChildItem -LiteralPath $PackageRoot -Recurse -File)
    {
        try
        {
            $stream = [IO.File]::Open(
                $file.FullName,
                [IO.FileMode]::Open,
                [IO.FileAccess]::Read,
                [IO.FileShare]::ReadWrite
            )
            $stream.Dispose()
            $script:readablePackageFiles++
        }
        catch
        {
            Add-Error "Arquivo inacessível: $(Get-ArtRelativePath $file.FullName)"
            continue
        }

        if ($archiveExtensions -contains $file.Extension.ToLowerInvariant())
        {
            Add-Error "Arquivo compactado aninhado no pacote: $(Get-ArtRelativePath $file.FullName)"
        }

        if ($file.Name -match '(^~\$)|\.(?:tmp|temp|part|crdownload|download|bak|bridge)$')
        {
            Add-Error "Arquivo temporário no pacote: $(Get-ArtRelativePath $file.FullName)"
        }

        $relativePath = Convert-ToForwardSlash ([IO.Path]::GetRelativePath($PackageRoot, $file.FullName))
        if ($relativePath -match '(^|/)(?:Library|Temp|obj|\.cache|Cache)(/|$)')
        {
            Add-Error "Arquivo do pacote está em diretório proibido: $(Get-ArtRelativePath $file.FullName)"
        }

        if ($textExtensions -contains $file.Extension.ToLowerInvariant())
        {
            $content = Get-Content -LiteralPath $file.FullName -Raw
            if ($content -match '(?im)(?:^|[\s`"''])(?:[A-Za-z]:\\|/Users/[^/\s]+/|/home/[^/\s]+/)')
            {
                Add-Error "Referência absoluta de máquina em $(Get-ArtRelativePath $file.FullName)."
            }
        }
    }
}

$integrationManifest = $null
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf))
{
    Add-Error 'Manifesto de integração ausente: MANIFEST.json'
}
else
{
    try
    {
        $integrationManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    }
    catch
    {
        Add-Error "Manifesto de integração inválido: $($_.Exception.Message)"
    }
}

if ($null -ne $integrationManifest)
{
    if ($integrationManifest.schemaVersion -ne 1)
    {
        Add-Error "schemaVersion do manifesto não suportada: $($integrationManifest.schemaVersion)"
    }
    if ($integrationManifest.pathBase -ne 'repository-root')
    {
        Add-Error "pathBase do manifesto deve ser repository-root."
    }

    $manifestPackages = @($integrationManifest.packages)
    Assert-Unique $manifestPackages @('taskId') 'MANIFEST packages/taskId'
    Assert-Unique $manifestPackages @('packageName') 'MANIFEST packages/packageName'
    Assert-Unique $manifestPackages @('rootPath') 'MANIFEST packages/rootPath'
    Assert-SameSet @('014', '015', '016', '017', '018', '019') $manifestPackages.taskId 'MANIFEST task IDs'

    foreach ($package in $manifestPackages)
    {
        if (-not $package.version -or -not $package.importedAt -or -not $package.sourceZipName)
        {
            Add-Error "Pacote $($package.taskId) não possui version, importedAt ou sourceZipName."
        }
        if ($package.sourceZipName -notmatch '\.zip$')
        {
            Add-Error "sourceZipName inválido no pacote $($package.taskId): $($package.sourceZipName)"
        }

        $packageRoot = Assert-RepositoryPath $package.rootPath "MANIFEST/$($package.taskId)/rootPath" -Directory
        foreach ($documentPath in @($package.mainDocuments))
        {
            Assert-RepositoryPath $documentPath "MANIFEST/$($package.taskId)/mainDocuments" | Out-Null
        }
        foreach ($catalogPath in @($package.mainCatalogs))
        {
            Assert-RepositoryPath $catalogPath "MANIFEST/$($package.taskId)/mainCatalogs" | Out-Null
        }

        if ($package.checksum)
        {
            if ($package.checksum -notmatch '^sha256:([0-9a-f]{64})$')
            {
                Add-Error "Checksum inválido no pacote $($package.taskId): $($package.checksum)"
            }
            else
            {
                $expectedChecksum = $matches[1]
                $checksumSource = Assert-RepositoryPath $package.checksumSource "MANIFEST/$($package.taskId)/checksumSource"
                if ($checksumSource)
                {
                    $actualChecksum = (Get-FileHash -LiteralPath $checksumSource -Algorithm SHA256).Hash.ToLowerInvariant()
                    if ($actualChecksum -ne $expectedChecksum)
                    {
                        Add-Error "Checksum do manifesto diverge para o pacote $($package.taskId)."
                    }
                }
            }
        }
        elseif ($package.checksumSource)
        {
            Add-Error "Pacote $($package.taskId) possui checksumSource sem checksum."
        }

        if ($packageRoot)
        {
            Assert-PackageHygiene $packageRoot
        }
    }

    $truthSources = @($integrationManifest.truthSources)
    Assert-Unique $truthSources @('subject') 'MANIFEST truthSources/subject'
    Assert-Unique $truthSources @('canonicalPath') 'MANIFEST truthSources/canonicalPath'
    Assert-SameSet @(
        'visual-bible',
        'asset-production-standard',
        'characters-goblins-and-heroes',
        'character-rig-and-sockets',
        'equipment-tier-and-rarity',
        'equipment-modularity',
        'environments-and-stations',
        'modular-environment-standard',
        'crafting-station-evolution',
        'meshy-operational-queue',
        'meshy-operations-manual',
        'meshy-generation-settings'
    ) $truthSources.subject 'MANIFEST fontes da verdade'

    foreach ($truthSource in $truthSources)
    {
        $canonicalPath = Assert-RepositoryPath $truthSource.canonicalPath "Fonte canônica/$($truthSource.subject)"
        if (-not $canonicalPath)
        {
            continue
        }

        $script:canonicalSourcesChecked++
        $canonicalHash = (Get-FileHash -LiteralPath $canonicalPath -Algorithm SHA256).Hash
        $declaredCopies = @{}
        $declaredCopies[(Convert-ToForwardSlash $truthSource.canonicalPath)] = $true
        foreach ($mirrorPath in @($truthSource.historicalMirrors))
        {
            $declaredCopies[(Convert-ToForwardSlash $mirrorPath)] = $true
            $mirror = Assert-RepositoryPath $mirrorPath "Mirror histórico/$($truthSource.subject)"
            if ($mirror)
            {
                $mirrorHash = (Get-FileHash -LiteralPath $mirror -Algorithm SHA256).Hash
                if ($mirrorHash -ne $canonicalHash)
                {
                    Add-Error "Mirror histórico diverge da fonte canônica '$($truthSource.subject)': $mirrorPath"
                }
            }
        }

        $canonicalFileName = Split-Path -Leaf $canonicalPath
        foreach ($candidate in Get-ChildItem -LiteralPath $artRoot -Recurse -File -Filter $canonicalFileName)
        {
            $candidateRelativePath = Convert-ToForwardSlash ([IO.Path]::GetRelativePath($repositoryRoot, $candidate.FullName))
            if (-not $declaredCopies.ContainsKey($candidateRelativePath))
            {
                Add-Error "Cópia não declarada da fonte '$($truthSource.subject)': $candidateRelativePath"
            }
        }
    }
}

$warnings.Add('Os nomes dos ZIPs de origem foram preservados no manifesto; os arquivos ZIP não foram retidos ao lado do conteúdo extraído.')

$masterPath = Join-Path $task015 'Examples/ASSET_MASTER_CATALOG.csv'
$characterPath = Join-Path $task016 'Examples/CHARACTER_PRODUCTION_SHEETS.csv'
$equipmentFamilyPath = Join-Path $task017 'Examples/EQUIPMENT_FAMILY_MATRIX.csv'
$equipmentPath = Join-Path $task017 'Examples/EQUIPMENT_PRODUCTION_CATALOG.csv'
$equipmentTierPath = Join-Path $task017 'Examples/EQUIPMENT_TIER_MATRIX.csv'
$equipmentPromptPath = Join-Path $task017 'Examples/EQUIPMENT_PROMPT_MANIFEST.csv'
$environmentModulePath = Join-Path $task018 'Examples/ENVIRONMENT_MODULE_MATRIX.csv'
$stationPath = Join-Path $task018 'Examples/STATION_TIER_MATRIX.csv'
$environmentPath = Join-Path $task018 'Examples/ENVIRONMENT_STATION_PRODUCTION_CATALOG.csv'
$environmentPromptPath = Join-Path $task018 'Examples/ENVIRONMENT_STATION_PROMPT_MANIFEST.csv'
$queuePath = Join-Path $task019 'Examples/MESHY_ASSET_QUEUE.csv'
$batchPath = Join-Path $task019 'Examples/MESHY_BATCH_PLAN.csv'
$masterPromptPath = Join-Path $task019 'Examples/MESHY_MASTER_PROMPT_MANIFEST.csv'

$master = Import-Catalog $masterPath
$characters = Import-Catalog $characterPath
$equipmentFamilies = Import-Catalog $equipmentFamilyPath
$equipment = Import-Catalog $equipmentPath
$equipmentTiers = Import-Catalog $equipmentTierPath
$equipmentPrompts = Import-Catalog $equipmentPromptPath
$environmentModules = Import-Catalog $environmentModulePath
$stations = Import-Catalog $stationPath
$environment = Import-Catalog $environmentPath
$environmentPrompts = Import-Catalog $environmentPromptPath
$queue = Import-Catalog $queuePath
$batches = Import-Catalog $batchPath
$masterPrompts = Import-Catalog $masterPromptPath

Assert-Unique $master @('asset_id') 'Task015/ASSET_MASTER_CATALOG'
Assert-Unique $characters @('character_id') 'Task016/CHARACTER_PRODUCTION_SHEETS'
Assert-Unique $equipmentFamilies @('family_key') 'Task017/EQUIPMENT_FAMILY_MATRIX'
Assert-Unique $equipment @('asset_id') 'Task017/EQUIPMENT_PRODUCTION_CATALOG'
Assert-Unique $equipmentTiers @('tier') 'Task017/EQUIPMENT_TIER_MATRIX'
Assert-Unique $equipmentPrompts @('asset_id', 'prompt_type') 'Task017/EQUIPMENT_PROMPT_MANIFEST'
Assert-Unique $environmentModules @('asset_id') 'Task018/ENVIRONMENT_MODULE_MATRIX'
Assert-Unique $stations @('asset_id') 'Task018/STATION_TIER_MATRIX'
Assert-Unique $environment @('asset_id') 'Task018/ENVIRONMENT_STATION_PRODUCTION_CATALOG'
Assert-Unique $environmentPrompts @('asset_id') 'Task018/ENVIRONMENT_STATION_PROMPT_MANIFEST'
Assert-Unique $queue @('asset_id') 'Task019/MESHY_ASSET_QUEUE'
Assert-Unique $batches @('batch_id') 'Task019/MESHY_BATCH_PLAN'
Assert-Unique $masterPrompts @('prompt_id') 'Task019/MESHY_MASTER_PROMPT_MANIFEST'
Assert-Unique $masterPrompts @('path') 'Task019/MESHY_MASTER_PROMPT_MANIFEST paths'

Assert-ValidAssetIds $master 'asset_id' 'Task015/ASSET_MASTER_CATALOG'
Assert-ValidAssetIds $characters 'character_id' 'Task016/CHARACTER_PRODUCTION_SHEETS'
Assert-ValidAssetIds $equipment 'asset_id' 'Task017/EQUIPMENT_PRODUCTION_CATALOG'
Assert-ValidAssetIds $environment 'asset_id' 'Task018/ENVIRONMENT_STATION_PRODUCTION_CATALOG'
Assert-ValidAssetIds $queue 'asset_id' 'Task019/MESHY_ASSET_QUEUE'

Assert-Subset $characters.character_id $master.asset_id 'Task016 -> Task015'
Assert-Subset $equipment.family_key $equipmentFamilies.family_key 'Task017 equipment -> families'
Assert-Subset $equipment.tier $equipmentTiers.tier 'Task017 equipment -> tiers'
Assert-Subset $equipmentPrompts.asset_id $equipment.asset_id 'Task017 prompts -> equipment'

foreach ($row in $equipment)
{
    $assetPrompts = @($equipmentPrompts | Where-Object { $_.asset_id -eq $row.asset_id })
    Assert-SameSet @('concept_sheet', 'meshy_geometry', 'texture_variants') $assetPrompts.prompt_type "Task017 prompt types de $($row.asset_id)"
}

$task018Ids = @($environmentModules.asset_id) + @($stations.asset_id)
Assert-SameSet $task018Ids $environment.asset_id 'Task018 matrizes -> catálogo'
Assert-SameSet $environment.asset_id $environmentPrompts.asset_id 'Task018 catálogo -> manifest de prompts'

$specializedIds = @($characters.character_id) + @($equipment.asset_id) + @($environment.asset_id)
Assert-SameSet $specializedIds $queue.asset_id 'Task016+Task017+Task018 -> Task019 queue'
Assert-Subset $queue.batch_id $batches.batch_id 'Task019 queue -> batch plan'

foreach ($batch in $batches)
{
    $actualCount = @($queue | Where-Object { $_.batch_id -eq $batch.batch_id }).Count
    if ($actualCount -ne [int]$batch.asset_count)
    {
        Add-Error "Lote $($batch.batch_id) declara $($batch.asset_count) assets, mas a fila contém $actualCount."
    }
}

$queueById = New-IdMap $queue 'asset_id'
$masterPromptPaths = @($masterPrompts.path)
$specializedOwnership = @{}
foreach ($ownership in @(
    [pscustomobject]@{ Task = 'Task016'; Ids = @($characters.character_id) },
    [pscustomobject]@{ Task = 'Task017'; Ids = @($equipment.asset_id) },
    [pscustomobject]@{ Task = 'Task018'; Ids = @($environment.asset_id) }
))
{
    foreach ($id in $ownership.Ids)
    {
        if ($specializedOwnership.ContainsKey($id))
        {
            Add-Error "ID $id pertence a mais de um catálogo especializado: $($specializedOwnership[$id]) e $($ownership.Task)."
        }
        else
        {
            $specializedOwnership[$id] = $ownership.Task
        }
    }
}

$masterIdentityById = New-IdMap $master 'asset_id'
foreach ($specializedRow in @($characters) + @($equipment) + @($environment))
{
    $id = if ($specializedRow.PSObject.Properties.Name -contains 'character_id')
    {
        [string]$specializedRow.character_id
    }
    else
    {
        [string]$specializedRow.asset_id
    }
    if ($masterIdentityById.ContainsKey($id))
    {
        $masterIdentity = $masterIdentityById[$id]
        if ($specializedRow.display_name_pt -ne $masterIdentity.display_name_pt -or
            $specializedRow.display_name_en -ne $masterIdentity.display_name_en)
        {
            Add-Error "Identidade nominal divergente entre catálogo mestre e especializado: $id."
        }
    }
}

foreach ($row in $queue)
{
    $expectedSource = if ($row.asset_id -in $characters.character_id)
    {
        'Task016'
    }
    elseif ($row.asset_id -in $equipment.asset_id)
    {
        'Task017'
    }
    else
    {
        'Task018'
    }

    if ($row.source_task -ne $expectedSource)
    {
        Add-Error "Task019 source_task incorreto para $($row.asset_id): $($row.source_task), esperado $expectedSource."
    }

    $sourceRow = if ($expectedSource -eq 'Task016')
    {
        $characters | Where-Object { $_.character_id -eq $row.asset_id } | Select-Object -First 1
    }
    elseif ($expectedSource -eq 'Task017')
    {
        $equipment | Where-Object { $_.asset_id -eq $row.asset_id } | Select-Object -First 1
    }
    else
    {
        $environment | Where-Object { $_.asset_id -eq $row.asset_id } | Select-Object -First 1
    }
    if ($row.display_name_pt -ne $sourceRow.display_name_pt -or $row.display_name_en -ne $sourceRow.display_name_en)
    {
        Add-Error "Task019 diverge na identidade nominal de $($row.asset_id)."
    }

    foreach ($column in @('concept_prompt', 'geometry_prompt', 'texture_prompt'))
    {
        $relativePath = [string]$row.$column
        Assert-PackagePath $task019 $relativePath "Task019/$($row.asset_id)/$column"
        Assert-PromptPathConvention $relativePath "Task019/$($row.asset_id)/$column"
        if ($relativePath -notin $masterPromptPaths)
        {
            Add-Error "Prompt da fila não consta no manifest mestre: $relativePath"
        }
    }

    $pipelinePath = Join-Path $task019 "Combined/$($row.asset_id)_PIPELINE.md"
    if (-not (Test-Path -LiteralPath $pipelinePath -PathType Leaf))
    {
        Add-Error "Pipeline ausente para $($row.asset_id)."
    }
    else
    {
        $pipelineContent = Get-Content -LiteralPath $pipelinePath -Raw
        $pipelineMatches = [regex]::Matches($pipelineContent, '`\.\./(Prompts/[^`]+)`')
        if ($pipelineMatches.Count -ne 3)
        {
            Add-Error "Pipeline $($row.asset_id) deve conter três caminhos ../Prompts relativos ao documento."
        }
        foreach ($match in $pipelineMatches)
        {
            Assert-DocumentPath $pipelinePath ("../" + $match.Groups[1].Value) "Pipeline $($row.asset_id)"
        }
    }
}

foreach ($row in $masterPrompts)
{
    Assert-PackagePath $task019 $row.path "Task019 manifest/$($row.prompt_id)"
    Assert-PromptPathConvention $row.path "Task019 manifest/$($row.prompt_id)"
    $candidate = Join-Path $task019 $row.path
    if (Test-Path -LiteralPath $candidate -PathType Leaf)
    {
        $actualHash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -ne $row.sha256.ToLowerInvariant())
        {
            Add-Error "Hash de prompt divergente: $($row.path)"
        }
    }
}

foreach ($row in $equipment)
{
    foreach ($column in @('concept_prompt_path', 'meshy_prompt_path', 'texture_variants_prompt_path'))
    {
        Assert-PackagePath $task017 $row.$column "Task017/$($row.asset_id)/$column"
    }
}
foreach ($row in $equipmentPrompts)
{
    Assert-PackagePath $task017 $row.path "Task017 manifest/$($row.asset_id)/$($row.prompt_type)"
    Assert-PromptPathConvention $row.path "Task017 manifest/$($row.asset_id)/$($row.prompt_type)"
}
foreach ($row in $environment)
{
    foreach ($column in @('concept_prompt_path', 'meshy_prompt_path', 'texture_prompt_path'))
    {
        Assert-PackagePath $task018 $row.$column "Task018/$($row.asset_id)/$column"
    }
}
foreach ($row in $environmentPrompts)
{
    foreach ($column in @('concept_prompt', 'meshy_prompt', 'texture_prompt'))
    {
        Assert-PackagePath $task018 $row.$column "Task018 manifest/$($row.asset_id)/$column"
        Assert-PromptPathConvention $row.$column "Task018 manifest/$($row.asset_id)/$column"
    }
}

$characterPromptReadme = Join-Path $task019 'Prompts/Characters/README.md'
$characterReadmeContent = Get-Content -LiteralPath $characterPromptReadme -Raw
$characterReadmeMatches = [regex]::Matches($characterReadmeContent, '`((?:ConceptArt|Meshy|Texture)/[^`]+)`')
if ($characterReadmeMatches.Count -ne 36)
{
    Add-Error 'Task019/Prompts/Characters/README.md deve conter 36 caminhos relativos ao documento.'
}
foreach ($match in $characterReadmeMatches)
{
    Assert-DocumentPath $characterPromptReadme $match.Groups[1].Value 'Índice de prompts de personagens'
}

$roster014 = Import-Catalog (Join-Path $task014 'Examples/MONSTER_FACTIONS_ROSTER.csv')
$roster015 = Import-Catalog (Join-Path $task015 'Examples/MONSTER_FACTIONS_ROSTER.csv')
Assert-Unique $roster014 @('stable_id') 'Task014/MONSTER_FACTIONS_ROSTER'
Assert-ValidAssetIds $roster014 'stable_id' 'Task014/MONSTER_FACTIONS_ROSTER'
Assert-SameSet $roster014.stable_id $roster015.stable_id 'Task014 -> Task015 roster'

Assert-Snapshot $task016 (Join-Path $task019 'Sources/Task016')
Assert-Snapshot $task017 (Join-Path $task019 'Sources/Task017')
Assert-Snapshot $task018 (Join-Path $task019 'Sources/Task018')
Assert-MirroredPromptTree (Join-Path $task016 'Prompts') (Join-Path $task019 'Prompts/Characters') 'Task016 -> Task019/Characters'
Assert-MirroredPromptTree (Join-Path $task017 'Prompts') (Join-Path $task019 'Prompts/Equipment') 'Task017 -> Task019/Equipment'
Assert-MirroredPromptTree (Join-Path $task018 'Prompts') (Join-Path $task019 'Prompts/EnvironmentStations') 'Task018 -> Task019/EnvironmentStations'

foreach ($packageRoot in @($task015, $task016, $task017, $task018, $task019))
{
    Assert-ChecksumManifest $packageRoot
}

$masterById = New-IdMap $master 'asset_id'
$characterById = New-IdMap $characters 'character_id'
$equipmentById = New-IdMap $equipment 'asset_id'
$environmentById = New-IdMap $environment 'asset_id'

$allIds = @($master.asset_id) + @($characters.character_id) + @($equipment.asset_id) + @($environment.asset_id)
$allIds = @($allIds | Sort-Object -Unique)
$overlappingDefinitionIds = @($allIds | Where-Object {
    $definitionCount = 0
    if ($masterById.ContainsKey($_)) { $definitionCount++ }
    if ($characterById.ContainsKey($_)) { $definitionCount++ }
    if ($equipmentById.ContainsKey($_)) { $definitionCount++ }
    if ($environmentById.ContainsKey($_)) { $definitionCount++ }
    $definitionCount -gt 1
})

$expandedEquipmentIds = @($equipment.asset_id | Where-Object { -not $masterById.ContainsKey($_) })
$warnings.Add("Task017 expande o catálogo mestre com $($expandedEquipmentIds.Count) IDs de armaduras; eles são preservados no índice consolidado.")
$warnings.Add('Campos Assets/... são destinos planejados do Unity e não são tratados como arquivos existentes nem importados.')
$warnings.Add('Task019/Sources preserva snapshots; os caminhos internos desses CSVs continuam relativos à raiz do pacote de origem.')
$warnings.Add('Os CSVs permanecem como fontes tabulares auditáveis; a validação dos workbooks do Excel é complementar e registrada separadamente.')

$artDocumentsInAssets = @(Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'Assets') -Recurse -File | Where-Object {
    $_.Extension.ToLowerInvariant() -in @('.csv', '.xls', '.xlsx', '.md', '.txt')
})
foreach ($file in $artDocumentsInAssets)
{
    Add-Error "Documento, planilha ou prompt indevido em Assets: $([IO.Path]::GetRelativePath($repositoryRoot, $file.FullName))"
}

$workbookValidation = $null
if (Test-Path -LiteralPath $workbookValidationPath -PathType Leaf)
{
    try
    {
        $workbookValidation = Get-Content -LiteralPath $workbookValidationPath -Raw | ConvertFrom-Json
    }
    catch
    {
        Add-Error "Evidência de validação dos workbooks inválida: $(Get-ArtRelativePath $workbookValidationPath)"
    }
}
else
{
    $warnings.Add('Não há evidência registrada de validação ao vivo dos workbooks.')
}

$indexRows = foreach ($id in $allIds)
{
    $masterRow = if ($masterById.ContainsKey($id)) { $masterById[$id] } else { $null }
    $characterRow = if ($characterById.ContainsKey($id)) { $characterById[$id] } else { $null }
    $equipmentRow = if ($equipmentById.ContainsKey($id)) { $equipmentById[$id] } else { $null }
    $environmentRow = if ($environmentById.ContainsKey($id)) { $environmentById[$id] } else { $null }
    $queueRow = if ($queueById.ContainsKey($id)) { $queueById[$id] } else { $null }

    $canonicalTask = 'Task015'
    $canonicalCatalog = Get-ArtRelativePath $masterPath
    $displayNamePt = [string]$masterRow.display_name_pt
    $displayNameEn = [string]$masterRow.display_name_en
    $assetClass = [string]$masterRow.asset_class
    $domain = [string]$masterRow.category
    $category = [string]$masterRow.subcategory
    $productionPhase = [string]$masterRow.production_phase
    $priority = [string]$masterRow.priority
    $tier = [string]$masterRow.tier
    $generationMethod = [string]$masterRow.meshy_method
    $catalogStatus = [string]$masterRow.status
    $unityTargetPath = [string]$masterRow.unity_import_path

    if ($characterRow)
    {
        $canonicalTask = 'Task016'
        $canonicalCatalog = Get-ArtRelativePath $characterPath
        $displayNamePt = [string]$characterRow.display_name_pt
        $displayNameEn = [string]$characterRow.display_name_en
        if (-not $assetClass) { $assetClass = '3D Model' }
        $domain = 'Character'
        $category = [string]$characterRow.category
        $generationMethod = [string]$characterRow.generation_method
    }
    elseif ($equipmentRow)
    {
        $canonicalTask = 'Task017'
        $canonicalCatalog = Get-ArtRelativePath $equipmentPath
        $displayNamePt = [string]$equipmentRow.display_name_pt
        $displayNameEn = [string]$equipmentRow.display_name_en
        if (-not $assetClass) { $assetClass = '3D Model' }
        $domain = 'Equipment'
        $category = [string]$equipmentRow.category
        $productionPhase = [string]$equipmentRow.production_phase
        $priority = [string]$equipmentRow.priority
        $tier = "T$($equipmentRow.tier)"
        $generationMethod = [string]$equipmentRow.generation_method
        $catalogStatus = [string]$equipmentRow.status
        $unityTargetPath = [string]$equipmentRow.unity_model_path
    }
    elseif ($environmentRow)
    {
        $canonicalTask = 'Task018'
        $canonicalCatalog = Get-ArtRelativePath $environmentPath
        $displayNamePt = [string]$environmentRow.display_name_pt
        $displayNameEn = [string]$environmentRow.display_name_en
        $assetClass = [string]$environmentRow.asset_class
        $domain = [string]$environmentRow.category
        $category = [string]$environmentRow.subcategory
        $productionPhase = [string]$environmentRow.production_phase
        $priority = [string]$environmentRow.priority
        $tier = [string]$environmentRow.tier
        $generationMethod = [string]$environmentRow.meshy_method
        $catalogStatus = [string]$environmentRow.status
        $unityTargetPath = [string]$environmentRow.unity_import_path
    }

    $sourceCatalogs = [System.Collections.Generic.List[string]]::new()
    if ($masterRow) { $sourceCatalogs.Add('Task015') }
    if ($characterRow) { $sourceCatalogs.Add('Task016') }
    if ($equipmentRow) { $sourceCatalogs.Add('Task017') }
    if ($environmentRow) { $sourceCatalogs.Add('Task018') }
    if ($queueRow) { $sourceCatalogs.Add('Task019') }

    $definitionCount = @(@($masterRow, $characterRow, $equipmentRow, $environmentRow) | Where-Object { $null -ne $_ }).Count
    $conceptPrompt = ''
    $geometryPrompt = ''
    $texturePrompt = ''
    $pipelinePath = ''
    $promptCount = 0
    if ($queueRow)
    {
        $conceptPrompt = Get-ArtRelativePath (Join-Path $task019 $queueRow.concept_prompt)
        $geometryPrompt = Get-ArtRelativePath (Join-Path $task019 $queueRow.geometry_prompt)
        $texturePrompt = Get-ArtRelativePath (Join-Path $task019 $queueRow.texture_prompt)
        $pipelinePath = Get-ArtRelativePath (Join-Path $task019 "Combined/$id`_PIPELINE.md")
        $promptCount = 3
        $unityTargetPath = [string]$queueRow.unity_path
    }

    [pscustomobject][ordered]@{
        asset_id = $id
        display_name_pt = $displayNamePt
        display_name_en = $displayNameEn
        canonical_task = $canonicalTask
        canonical_catalog = $canonicalCatalog
        source_catalogs = $sourceCatalogs -join ';'
        definition_count = $definitionCount
        asset_class = $assetClass
        domain = $domain
        category = $category
        production_phase = $productionPhase
        priority = $priority
        tier = $tier
        generation_method = $generationMethod
        catalog_status = $catalogStatus
        task019_queue = if ($queueRow) { 'Yes' } else { 'No' }
        prompt_count = $promptCount
        concept_prompt_path = $conceptPrompt
        geometry_prompt_path = $geometryPrompt
        texture_prompt_path = $texturePrompt
        pipeline_path = $pipelinePath
        unity_target_path = $unityTargetPath
        unity_target_kind = if ($unityTargetPath) { 'Planned destination; existence not asserted' } else { '' }
    }
}

Assert-Unique @($indexRows) @('asset_id') 'ART_PRODUCTION_INDEX'

$result = if ($errors.Count -eq 0) { 'PASSED' } else { 'FAILED' }
$report = [ordered]@{
    generated_at_utc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    result = $result
    unique_asset_ids = $allIds.Count
    authoritative_catalog_rows = [ordered]@{
        task015 = $master.Count
        task016 = $characters.Count
        task017 = $equipment.Count
        task018 = $environment.Count
        task019_queue = $queue.Count
    }
    intentional_cross_catalog_overlaps = $overlappingDefinitionIds.Count
    consistent_overlapping_identities = $overlappingDefinitionIds.Count
    task017_expansion_ids_not_in_task015 = $expandedEquipmentIds.Count
    unique_prompt_ids_task019 = $masterPrompts.Count
    relative_file_paths_checked = $pathChecks
    manifest_paths_checked = $manifestPathChecks
    checksum_entries_checked = $checksumEntries
    readable_package_files = $readablePackageFiles
    canonical_sources_checked = $canonicalSourcesChecked
    prompt_mirrors_checked = $promptMirrorsChecked
    live_workbook_validation = $workbookValidation
    errors = @($errors)
    warnings = @($warnings)
}

if (-not $CheckOnly)
{
    New-Item -ItemType Directory -Force -Path $generatedReportRoot | Out-Null
    $indexRows | Export-Csv -LiteralPath $indexPath -NoTypeInformation -Encoding utf8
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonReportPath -Encoding utf8

    $markdown = @"
# Validação integrada de ArtProduction

## Resultado

**$result**

## Contagens

- IDs de asset únicos no índice: $($allIds.Count)
- Linhas dos catálogos autoritativos: Task015=$($master.Count), Task016=$($characters.Count), Task017=$($equipment.Count), Task018=$($environment.Count)
- Sobreposições intencionais entre catálogo mestre e catálogos especializados: $($overlappingDefinitionIds.Count)
- IDs de expansão da Task017 ausentes na Task015 e incorporados pela união: $($expandedEquipmentIds.Count)
- Assets na fila operacional Task019: $($queue.Count)
- Prompt IDs únicos na Task019: $($masterPrompts.Count)
- Caminhos de arquivo relativos verificados: $pathChecks
- Caminhos do manifesto verificados: $manifestPathChecks
- Entradas SHA-256 verificadas: $checksumEntries
- Arquivos dos pacotes acessíveis: $readablePackageFiles
- Fontes canônicas verificadas contra mirrors: $canonicalSourcesChecked
- Prompts operacionais verificados contra origens: $promptMirrorsChecked

## Regras verificadas

- unicidade das chaves autoritativas e das chaves compostas de manifest;
- referências Task016 → Task015, Task017 → famílias/Tiers, Task018 → matrizes e Task019 → catálogos/lotes;
- existência e hash dos prompts;
- caminhos dos catálogos relativos à raiz do pacote;
- caminhos dos pipelines `Combined` relativos ao próprio documento;
- integridade dos snapshots em `Task019/Sources`;
- integridade dos manifests `SHA256SUMS.txt`.
- manifesto relativo à raiz do repositório e fontes canônicas sem duplicação;
- convenção de IDs e nomes de prompt;
- ausência de caminhos absolutos de máquina, temporários, caches e ZIPs aninhados.
- ausência de Markdown, CSV, planilhas e prompts dentro de `Assets`.

## Validação complementar dos workbooks

$(
    if ($null -eq $workbookValidation)
    {
        'Não registrada.'
    }
    else
    {
        "- Resultado: $($workbookValidation.result)`n- Método: $($workbookValidation.method)`n- Validado em UTC: $($workbookValidation.validated_at_utc)`n- Workbooks inspecionados: $(@($workbookValidation.workbooks).Count)"
    }
)

## Avisos de integração

$(@($warnings | ForEach-Object { "- $_" }) -join "`n")

## Erros

$(
    if ($errors.Count -eq 0)
    {
        'Nenhum.'
    }
    else
    {
        @($errors | ForEach-Object { "- $_" }) -join "`n"
    }
)
"@
    Set-Content -LiteralPath $markdownReportPath -Value $markdown -Encoding utf8
}

Write-Output "ArtProduction validation: $result"
Write-Output "Unique asset IDs: $($allIds.Count)"
Write-Output "Relative paths checked: $pathChecks"
Write-Output "Manifest paths checked: $manifestPathChecks"
Write-Output "Checksum entries checked: $checksumEntries"
Write-Output "Readable package files: $readablePackageFiles"
Write-Output "Canonical sources checked: $canonicalSourcesChecked"
Write-Output "Prompt mirrors checked: $promptMirrorsChecked"
if ($errors.Count -gt 0)
{
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

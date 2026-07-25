[CmdletBinding()]
param(
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'

$artRoot = Split-Path -Parent $PSScriptRoot
$validationRoot = Join-Path $artRoot 'Validation'
$indexPath = Join-Path $artRoot 'ART_PRODUCTION_INDEX.csv'
$jsonReportPath = Join-Path $validationRoot 'ART_PRODUCTION_VALIDATION.json'
$markdownReportPath = Join-Path $validationRoot 'ART_PRODUCTION_VALIDATION.md'
$workbookValidationPath = Join-Path $validationRoot 'WORKBOOK_LIVE_VALIDATION.json'

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

function Get-ArtRelativePath
{
    param([string]$FullPath)

    $relativePath = [IO.Path]::GetRelativePath($artRoot, $FullPath)
    return Convert-ToForwardSlash $relativePath
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
    if ($RelativePath -match '^(Assets/|https?://)')
    {
        Add-Error "$Label usa caminho de destino ou URL onde era esperado arquivo do pacote: $RelativePath"
        return
    }

    $script:pathChecks++
    $candidate = Join-Path $PackageRoot ($RelativePath.Replace('/', '\'))
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
    $candidate = Join-Path $documentRoot ($RelativePath.Replace('/', '\'))
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
        $candidate = Join-Path $PackageRoot ($relativePath.Replace('/', '\'))
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
    $candidate = Join-Path $task019 ($row.path.Replace('/', '\'))
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
Assert-SameSet $roster014.stable_id $roster015.stable_id 'Task014 -> Task015 roster'

Assert-Snapshot $task016 (Join-Path $task019 'Sources/Task016')
Assert-Snapshot $task017 (Join-Path $task019 'Sources/Task017')
Assert-Snapshot $task018 (Join-Path $task019 'Sources/Task018')

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
        $conceptPrompt = Get-ArtRelativePath (Join-Path $task019 $queueRow.concept_prompt.Replace('/', '\'))
        $geometryPrompt = Get-ArtRelativePath (Join-Path $task019 $queueRow.geometry_prompt.Replace('/', '\'))
        $texturePrompt = Get-ArtRelativePath (Join-Path $task019 $queueRow.texture_prompt.Replace('/', '\'))
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
    checksum_entries_checked = $checksumEntries
    live_workbook_validation = $workbookValidation
    errors = @($errors)
    warnings = @($warnings)
}

if (-not $CheckOnly)
{
    New-Item -ItemType Directory -Force -Path $validationRoot | Out-Null
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
- Entradas SHA-256 verificadas: $checksumEntries

## Regras verificadas

- unicidade das chaves autoritativas e das chaves compostas de manifest;
- referências Task016 → Task015, Task017 → famílias/Tiers, Task018 → matrizes e Task019 → catálogos/lotes;
- existência e hash dos prompts;
- caminhos dos catálogos relativos à raiz do pacote;
- caminhos dos pipelines `Combined` relativos ao próprio documento;
- integridade dos snapshots em `Task019/Sources`;
- integridade dos manifests `SHA256SUMS.txt`.

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
Write-Output "Checksum entries checked: $checksumEntries"
if ($errors.Count -gt 0)
{
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

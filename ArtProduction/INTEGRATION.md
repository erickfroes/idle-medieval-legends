# Integração dos catálogos de arte

Este documento define como consumir em conjunto os pacotes `Task014` a `Task019` sem alterar decisões artísticas e sem tratar destinos planejados do Unity como assets já produzidos.

## Ponto de entrada

O índice consolidado é [ART_PRODUCTION_INDEX.csv](ART_PRODUCTION_INDEX.csv). Ele contém uma linha por `asset_id`, totalizando 566 IDs únicos, e referencia os catálogos, prompts e pipelines por caminhos relativos à pasta `ArtProduction`.

O índice é uma visão de integração gerada. Ele não substitui os catálogos especializados nem deve ser editado manualmente.

## Autoridade por domínio

Quando o mesmo `asset_id` aparece em mais de um catálogo, a repetição representa especialização e não a criação de um novo asset:

1. `Task015` é o catálogo mestre de escopo geral.
2. `Task016` é a autoridade para personagens, inimigos e chefes.
3. `Task017` é a autoridade para equipamentos, Tiers e raridades.
4. `Task018` é a autoridade para ambientes modulares e estações.
5. `Task019` é uma fila operacional derivada das Tasks 016–018; não redefine identidade nem direção visual.

Há 151 sobreposições intencionais entre o catálogo mestre e os catálogos especializados. A `Task017` também introduz 108 IDs de armaduras que não estavam na `Task015`; esses IDs são válidos e entram na união consolidada.

Os arquivos em `Task019/Sources` são snapshots de proveniência idênticos às respectivas origens. Eles não são um segundo conjunto autoritativo. Caminhos armazenados nesses snapshots continuam relativos à raiz do pacote original (`Task016`, `Task017` ou `Task018`).

## Convenção de caminhos

Há três bases de resolução explícitas:

- No `ART_PRODUCTION_INDEX.csv`, caminhos de catálogos, prompts e pipelines são relativos à pasta `ArtProduction`.
- Nos CSVs de uma Task, caminhos de prompts são relativos à raiz daquela Task.
- Nos arquivos `Task019/Combined/*_PIPELINE.md`, caminhos iniciados por `../Prompts/` são relativos ao próprio documento.

Caminhos iniciados por `Assets/` são destinos planejados a partir da raiz do projeto Unity. Eles não comprovam que um `.fbx`, `.glb`, prefab ou material exista. A integração não cria diretórios, não baixa modelos e não importa placeholders para satisfazer esses destinos.

## Chaves e referências

- `asset_id` é a chave estável de assets e não pode ser renomeada durante a integração.
- A chave do manifest de prompts de equipamento é composta por `asset_id + prompt_type`; por isso o mesmo `asset_id` aparece três vezes de forma válida.
- `family_key` e `tier` da `Task017` devem existir nas matrizes de famílias e Tiers.
- Os IDs das matrizes de ambientes e estações da `Task018` devem formar exatamente o catálogo de produção da Task.
- A fila da `Task019` deve ser exatamente a união dos catálogos das Tasks 016, 017 e 018.
- Todo `batch_id` da fila deve existir no plano de lotes e sua contagem deve coincidir.
- Cada asset da fila deve ter três prompts existentes, presentes no manifest mestre, e um pipeline `Combined`.

Campos como `rig_profile`, `animation_set`, sockets e grupos de reutilização são chaves de perfil ou integração. Eles não devem ser promovidos automaticamente a modelos 3D ou novos registros de asset.

## Regeneração e validação

Execute a partir da raiz do repositório:

```powershell
& .\ArtProduction\Tools\Validate-ArtProduction.ps1
```

O comando regenera o índice e os relatórios em `ArtProduction/Validation`, valida unicidade, relações entre catálogos, caminhos relativos, hashes de prompts, snapshots e todos os manifests `SHA256SUMS.txt`. Ele retorna código diferente de zero se encontrar uma inconsistência.

Para validar sem regravar o índice e os relatórios:

```powershell
& .\ArtProduction\Tools\Validate-ArtProduction.ps1 -CheckOnly
```

O relatório atual está em [Validation/ART_PRODUCTION_VALIDATION.md](Validation/ART_PRODUCTION_VALIDATION.md).

## Workbooks

Os CSVs continuam sendo as fontes tabulares auditáveis usadas pela integração. Os workbooks `.xlsx` são apresentações operacionais derivadas e foram validados em uma sessão conectada do Microsoft Excel em 25 de julho de 2026:

- `IdleMedievalLegends_Asset_Master_Catalog.xlsx`: 7 abas inspecionadas, 458 `asset_id` únicos, 28 referências do roster e 34 dependências com formato de ID resolvidas, sem erros de fórmula.
- `IdleMedievalLegends_Meshy_Production_Tracker.xlsx`: 10 abas inspecionadas, 259 `asset_id`, 25 `batch_id` e 785 `prompt_id` únicos, sem erros de fórmula.
- No tracker, 777 referências da fila e 785 caminhos do manifest foram normalizados para `Prompts/Characters/...`, `Prompts/Equipment/...` ou `Prompts/EnvironmentStations/...`.
- As 777 referências de prompts da fila resolvem no manifest mestre; os 8 prompts adicionais do manifest são índices de visão geral/evolução e não correspondem a modelos novos.

A evidência estruturada dessa inspeção está em [Validation/WORKBOOK_LIVE_VALIDATION.json](Validation/WORKBOOK_LIVE_VALIDATION.json). O validador PowerShell continua auditando as fontes CSV; a inspeção do Excel é complementar e deve ser refeita após mudanças estruturais nos workbooks.

Valores de `animation_set` como `anim_station_*` e `anim_enemy_imp_*` são perfis planejados de animação, não chaves estrangeiras de modelos. Eles foram preservados sem criar ou importar assets inexistentes.

## Limites desta integração

- Nenhum texto de conceito, prompt, paleta, silhueta, raridade, Tier ou critério de aprovação foi alterado.
- Nenhum modelo, material, textura, rig, animação ou prefab ausente foi criado ou importado.
- Nenhum caminho `Assets/...` foi marcado como arquivo existente.
- Não houve mudança em economia, gameplay, cenas ou configurações do Unity.

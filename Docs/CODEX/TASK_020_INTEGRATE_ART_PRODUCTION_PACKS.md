# TASK 020 — Integração dos pacotes de produção artística 014–019

Data: **25 de julho de 2026**  
Branch: `chore/integrate-art-production-packs`  
Commit automático: **não**

## Objetivo

Integrar os seis pacotes de direção e operação artística fora de `Assets`, mantendo-os versionados, navegáveis e verificáveis. A integração preserva IDs, prompts e decisões artísticas e não trata caminhos planejados do Unity como arquivos existentes.

## Estado inicial

- `AGENTS.md` e `Docs/PROJECT_STRUCTURE.md` foram lidos antes de qualquer alteração.
- Não existem `AGENTS.md` adicionais em subpastas.
- O Git estava limpo, sem alterações preexistentes.
- Os seis pacotes já estavam extraídos sob `ArtProduction`.
- Foram encontrados 1.952 arquivos, todos acessíveis, e nenhum ZIP aninhado.
- O executável exato do Unity 6000.5.4f1 estava disponível.
- `Assets/_Game/Art` não existia; nenhuma pasta placeholder foi criada porque não há asset final para importar.

## Pacotes integrados

| Task | Pacote | Versão | Conteúdo principal |
|---:|---|---:|---|
| 014 | `IdleMedievalLegends_Task014_VisualBible` | 1.0 | Visual Bible, referência e roster |
| 015 | `IdleMedievalLegends_Task015_AssetCatalog` | 1.0 | catálogo mestre, budgets e workbook |
| 016 | `IdleMedievalLegends_Task016_CharacterDesignPack` | 1.0 | 12 fichas, rig/sockets e 36 prompts |
| 017 | `IdleMedievalLegends_Task017_EquipmentTierRarity` | 1.0 | 162 bases, matrizes e 486 prompts |
| 018 | `IdleMedievalLegends_Task018_EnvironmentStations` | 1.0 | 85 assets planejados e 263 prompts |
| 019 | `IdleMedievalLegends_Task019_MeshyOperations` | 1.0 | fila de 259 assets, 25 lotes e 785 prompts |

Os nomes dos ZIPs de origem foram registrados em `ArtProduction/MANIFEST.json`. Os ZIPs não foram retidos ao lado das extrações. O pacote 014 não trazia `SHA256SUMS.txt`; os pacotes 015–019 mantêm seus manifests de checksum.

## Estrutura final

```text
ArtProduction/
├── IdleMedievalLegends_Task014_VisualBible/
├── IdleMedievalLegends_Task015_AssetCatalog/
├── IdleMedievalLegends_Task016_CharacterDesignPack/
├── IdleMedievalLegends_Task017_EquipmentTierRarity/
├── IdleMedievalLegends_Task018_EnvironmentStations/
├── IdleMedievalLegends_Task019_MeshyOperations/
├── Incoming/
├── Approved/
├── GeneratedReports/
├── Tools/
├── ART_PRODUCTION_INDEX.csv
└── MANIFEST.json

Docs/
├── Art/
│   ├── ART_PRODUCTION_INDEX.md
│   ├── ART_DIRECTORY_CONVENTIONS.md
│   └── fontes canônicas das Tasks 014–019
└── CODEX/
    └── TASK_020_INTEGRATE_ART_PRODUCTION_PACKS.md
```

Os nomes históricos dos diretórios dos pacotes foram preservados para não quebrar caminhos, checksums ou proveniência. A estrutura recomendada é representada pelas áreas funcionais novas e pelo manifesto, sem renomear milhares de arquivos.

## Arquivos canônicos

- Visual Bible: `Docs/Art/IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md`.
- Catálogo mestre: `ArtProduction/IdleMedievalLegends_Task015_AssetCatalog/Examples/ASSET_MASTER_CATALOG.csv`.
- Goblins e heróis: `Docs/Art/IDLE_MEDIEVAL_LEGENDS_CHARACTER_DESIGN_BIBLE.md`.
- Equipamentos: `Docs/Art/IDLE_MEDIEVAL_LEGENDS_EQUIPMENT_TIER_RARITY_BIBLE.md`.
- Ambientes e estações: `Docs/Art/IDLE_MEDIEVAL_LEGENDS_ENVIRONMENT_AND_STATION_BIBLE.md`.
- Fila operacional Meshy: `ArtProduction/IdleMedievalLegends_Task019_MeshyOperations/Examples/MESHY_ASSET_QUEUE.csv`.

O manifesto também registra os padrões de rig, modularidade, módulos, evolução de estações, manual e configurações Meshy.

## Duplicações resolvidas

- As cópias da Visual Bible nas Tasks 014, 015, 016 e no snapshot da Task 019 têm o mesmo conteúdo.
- As cópias do padrão técnico das Tasks 015, 016 e do snapshot da Task 019 têm o mesmo conteúdo.
- Os documentos e CSVs em `Task019/Sources` têm hashes idênticos às origens das Tasks 016–018.
- Os 785 prompts operacionais da Task 019 foram comparados com as origens e são idênticos.
- As 11 publicações canônicas em `Docs/Art` foram copiadas byte a byte e são verificadas contra os mirrors declarados.
- READMEs e relatórios com nomes iguais, mas escopos distintos, foram mantidos separadamente.

Nenhuma versão diferente foi sobrescrita e nenhum texto artístico foi reescrito.

## Scripts

`ArtProduction/Tools/Validate-ArtProduction.ps1` foi ampliado para validar:

- IDs e chaves duplicadas;
- IDs fora de `snake_case` ASCII;
- referências entre Tasks e catálogos;
- caminhos e hashes de prompts;
- caminhos relativos no manifesto;
- documentos e catálogos principais presentes;
- fontes canônicas e mirrors históricos;
- snapshots e prompts operacionais da Task 019;
- todos os `SHA256SUMS.txt`;
- arquivos inacessíveis;
- caminhos absolutos de máquina;
- temporários, caches e ZIPs aninhados;
- documentos, CSVs, planilhas e prompts colocados em `Assets`;
- geração do índice consolidado e relatórios legíveis;
- código de saída diferente de zero quando há erros.

O script usa PowerShell 7 e APIs .NET sem dependências pagas. Os joins de caminho usam separadores portáveis, permitindo execução em Windows, Linux e macOS com `pwsh`. A execução desta task foi realizada no Windows.

## Planilhas

Os CSVs continuam sendo as fontes tabulares auditáveis. Os dois workbooks coincidem com os checksums versionados e com `ArtProduction/Validation/WORKBOOK_LIVE_VALIDATION.json`, evidência da sessão conectada do Microsoft Excel de 25 de julho de 2026:

- catálogo mestre: sete abas, 458 IDs únicos, zero duplicatas e zero erros de fórmula;
- tracker Meshy: dez abas, 259 asset IDs, 25 batch IDs, 785 prompt IDs, referências relativas corrigidas e zero erros de fórmula.

Nenhuma célula, fórmula ou decisão artística foi alterada nesta task.

## Validações executadas

### Integração artística

```powershell
& .\ArtProduction\Tools\Validate-ArtProduction.ps1 -CheckOnly
& .\ArtProduction\Tools\Validate-ArtProduction.ps1
```

Resultado: `PASSED`.

- 566 IDs únicos;
- 1.952 arquivos acessíveis;
- 3.857 caminhos relativos de catálogos/prompts;
- 82 caminhos do manifesto;
- 1.943 entradas SHA-256;
- 12 fontes canônicas;
- 785 mirrors de prompt;
- zero erro.

### Unity

Compilação headless no Unity `6000.5.4f1`: código 0, zero erro de compilador.

Validador estrutural:

```text
Projeto válido. 1 aviso(s).
```

O aviso preexistente informa que Player Settings ainda usa `DefaultCompany`.

Testes com XML:

| Plataforma | Resultado | Total | Aprovados | Falhas | Ignorados |
|---|---|---:|---:|---:|---:|
| EditMode | Passed | 307 | 307 | 0 | 0 |
| PlayMode | Passed | 21 | 21 | 0 | 0 |

A primeira tentativa de EditMode usou `-quit`, retornou 0 mas não gerou XML e não foi considerada teste válido. A repetição sem `-quit` gerou o XML acima.

Logs e XML foram gravados fora do repositório em `%TEMP%/IdleMedievalLegends-task020-validation`.

## Git, ignore e LFS

- `.gitignore` ignora somente saídas temporárias de ArtProduction: relatórios gerados, renders temporários, caches, downloads incompletos e arquivos temporários de ponte.
- Documentos, CSVs, workbooks e prompts necessários continuam visíveis ao Git.
- `.gitattributes` já cobria FBX, GLB, PSD, TGA, WAV e MP4; EXR foi adicionado ao Git LFS.
- Os CSVs importados preservam o contrato de bytes dos manifests: CRLF nos catálogos operacionais e LF nos dois rosters históricos.
- `SHA256SUMS.txt` preserva CRLF nas Tasks 015 e 019 e LF nas demais, conforme os arquivos de origem.
- Nenhum arquivo inexistente foi criado ou movido para testar LFS.

## Correções após revisão

- A convenção de IDs usa `-cnotmatch`; `Goblin_warrior` é rejeitado e `goblin_warrior` é aceito.
- Regras específicas de EOL sobrescrevem a normalização global somente dentro dos pacotes imutáveis.
- Com `core.autocrlf=true`, a saída de checkout calculada por `git cat-file --filters` foi comparada com os bytes validados de todos os 31 CSVs e cinco manifests `SHA256SUMS.txt`; não houve divergência.
- Um prompt representativo com EOL LF também foi comparado sem divergência.
- O catálogo mestre mantém SHA-256 `b0be6d36f0f2e86aedb4d8c5271695f7ccdc6a15fd17868431e0b64a93506f59` após os filtros de checkout.

## Riscos

- O nome de cada ZIP foi inferido do nome do pacote extraído porque os ZIPs não foram preservados; essa decisão está explícita no manifesto.
- O pacote 014 não possui manifest de checksum próprio; suas fontes canônicas ainda são comparadas por SHA-256 durante a validação.
- A validação de portabilidade foi executada apenas no Windows, embora o script não use caminhos específicos do Windows.
- Workbooks são apresentações operacionais derivadas; mudanças futuras neles exigem nova inspeção ao vivo no Excel.
- Player Settings continua com `DefaultCompany`.

## Pendências genuínas

- Aprovar concepts antes de gerar qualquer modelo.
- Executar o lote `B00_CALIBRATION` somente quando a produção Meshy for autorizada.
- Criar `Assets/_Game/Art` e seus `.meta` apenas quando existirem assets reais aprovados para importar.
- Validar builds Android e iOS por Build Profiles e em dispositivo/simulador em uma task própria.

## Não implementado

- modelos, imagens, texturas, materiais, rigs, animações, prefabs ou VFX;
- plugin ou API do Meshy;
- Addressables;
- importação de modelos inexistentes;
- edição de cenas Unity;
- mudanças de gameplay, balanceamento, economia ou IDs persistidos;
- build Android/iOS;
- commit Git.

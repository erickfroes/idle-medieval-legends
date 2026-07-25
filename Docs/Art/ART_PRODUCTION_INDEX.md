# Índice da produção artística

Versão da integração: **Task 020 / 1.0**  
Manifesto verificável: [`ArtProduction/MANIFEST.json`](../../ArtProduction/MANIFEST.json)  
Índice consolidado por asset: [`ArtProduction/ART_PRODUCTION_INDEX.csv`](../../ArtProduction/ART_PRODUCTION_INDEX.csv)

## Fontes da verdade

| Assunto | Fonte canônica | Origem histórica |
|---|---|---|
| Visual Bible | [`IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md`](IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md) | Task 014 |
| Padrão técnico geral | [`IDLE_MEDIEVAL_LEGENDS_ASSET_PRODUCTION_STANDARD.md`](IDLE_MEDIEVAL_LEGENDS_ASSET_PRODUCTION_STANDARD.md) | Task 015 |
| Goblins e heróis | [`IDLE_MEDIEVAL_LEGENDS_CHARACTER_DESIGN_BIBLE.md`](IDLE_MEDIEVAL_LEGENDS_CHARACTER_DESIGN_BIBLE.md) | Task 016 |
| Rig e sockets | [`RIG_AND_SOCKET_STANDARD.md`](RIG_AND_SOCKET_STANDARD.md) | Task 016 |
| Equipamentos | [`IDLE_MEDIEVAL_LEGENDS_EQUIPMENT_TIER_RARITY_BIBLE.md`](IDLE_MEDIEVAL_LEGENDS_EQUIPMENT_TIER_RARITY_BIBLE.md) | Task 017 |
| Modularidade de equipamentos | [`EQUIPMENT_MODULARITY_AND_VARIANT_STANDARD.md`](EQUIPMENT_MODULARITY_AND_VARIANT_STANDARD.md) | Task 017 |
| Ambientes e estações | [`IDLE_MEDIEVAL_LEGENDS_ENVIRONMENT_AND_STATION_BIBLE.md`](IDLE_MEDIEVAL_LEGENDS_ENVIRONMENT_AND_STATION_BIBLE.md) | Task 018 |
| Módulos de ambiente | [`MODULAR_ENVIRONMENT_STANDARD.md`](MODULAR_ENVIRONMENT_STANDARD.md) | Task 018 |
| Evolução de estações | [`CRAFTING_STATION_TIER_EVOLUTION_STANDARD.md`](CRAFTING_STATION_TIER_EVOLUTION_STANDARD.md) | Task 018 |
| Fila operacional Meshy | [`MESHY_ASSET_QUEUE.csv`](../../ArtProduction/IdleMedievalLegends_Task019_MeshyOperations/Examples/MESHY_ASSET_QUEUE.csv) | Task 019 |
| Operação Meshy | [`IDLE_MEDIEVAL_LEGENDS_MESHY_OPERATIONS_MANUAL.md`](IDLE_MEDIEVAL_LEGENDS_MESHY_OPERATIONS_MANUAL.md) | Task 019 |
| Configurações Meshy | [`MESHY_GENERATION_SETTINGS_MATRIX.md`](MESHY_GENERATION_SETTINGS_MATRIX.md) | Task 019 |

As cópias canônicas em `Docs/Art` são byte a byte iguais às origens registradas no manifesto. As cópias internas dos pacotes e os snapshots em `Task019/Sources` permanecem como histórico e proveniência; não constituem fontes concorrentes.

## Pacotes integrados

| Pacote | Versão | Finalidade | Fonte relacionada | Arquivos principais | Dependências | Status | Próximos passos |
|---|---:|---|---|---|---|---|---|
| Task 014 — Visual Bible | 1.0 | Direção artística macro e roster de facções | Visual Bible | `README.md`, Visual Bible, `MONSTER_FACTIONS_ROSTER.csv` | `Docs/Architecture_GDD.md` | Integrado; especificação, não asset final | Aprovar referências e manter decisões vinculantes |
| Task 015 — Asset Catalog | 1.0 | Catálogo mestre e orçamentos técnicos | Catálogo mestre | `ASSET_MASTER_CATALOG.csv`, workbook, padrão de produção | Task 014 | Integrado; 458 registros | Usar o CSV como base tabular auditável |
| Task 016 — Character Design | 1.0 | Três heróis e nove Goblins | Character Design Bible | fichas CSV, 36 prompts, padrão de rig | Tasks 014–015 | Integrado; 12 personagens planejados | Produzir e aprovar concepts antes de gerar 3D |
| Task 017 — Equipment Tier/Rarity | 1.0 | 18 famílias, nove Tiers e seis raridades | Equipment Bible | catálogo de 162 bases, matrizes, 486 prompts | Tasks 014–016 | Integrado; 972 variantes lógicas | Validar lote T1 antes da produção em escala |
| Task 018 — Environment/Stations | 1.0 | 60 módulos e 25 estações | Environment & Station Bible | catálogo de 85 assets, matrizes, 263 prompts | Tasks 014–017 | Integrado; especificação modular | Produzir o kit Goblin P0 e estações T1 |
| Task 019 — Meshy Operations | 1.0 | Fila, lotes, prompts e operação | Fila `MESHY_ASSET_QUEUE.csv` | fila de 259 assets, 25 lotes, 785 prompts, tracker | Tasks 016–018 | Integrado; fila planejada, nada gerado | Iniciar por `B00_CALIBRATION.md` após aprovação |

## Regras de navegação

- Caminhos em `ArtProduction/MANIFEST.json` são relativos à raiz do repositório.
- Caminhos nos CSVs das Tasks são relativos à raiz do próprio pacote.
- Caminhos `../Prompts/...` em `Task019/Combined` são relativos ao documento de pipeline.
- Caminhos `Assets/...` são destinos planejados; não afirmam que modelos, materiais ou prefabs existam.
- O índice CSV é gerado pelo validador e não substitui os catálogos especializados.

## Duplicações resolvidas

- A Visual Bible aparece nas Tasks 014, 015 e 016 com o mesmo SHA-256.
- O padrão geral de produção aparece nas Tasks 015 e 016 com o mesmo SHA-256.
- `Task019/Sources` contém snapshots idênticos às Tasks 016–018.
- Os prompts copiados para a operação da Task 019 mantêm o conteúdo das origens.
- READMEs e relatórios de validação com nomes iguais, mas finalidades distintas, não são consolidados.

As relações e hashes são validados por [`Validate-ArtProduction.ps1`](../../ArtProduction/Tools/Validate-ArtProduction.ps1).

# TASK 018 — Integrar ambientes modulares, biomas e estações de crafting

## Branch

```text
feat/modular-environments-and-crafting-stations
```

## Prompt para o Codex

```text
TASK 018 — Integrar catálogo artístico de ambientes modulares e estações

Antes de alterar arquivos:
1. Leia AGENTS.md.
2. Leia Docs/Art/IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md.
3. Leia os documentos das Tasks 015, 016 e 017.
4. Inspecione a estrutura atual de Art, Data, Editor, Prefabs e Tests.
5. Execute git status.
6. Apresente um plano curto.
7. Não importe arquivos FBX inexistentes e não instale SDK do Meshy.

Objetivo:
Integrar como fonte de verdade o pacote da Task 018, criando modelos de catálogo,
importação CSV Editor-only, validações de grade/pivô/metadados e documentação.

Escopo obrigatório:
1. Copiar para Docs/Art:
   - IDLE_MEDIEVAL_LEGENDS_ENVIRONMENT_AND_STATION_BIBLE.md;
   - MODULAR_ENVIRONMENT_STANDARD.md;
   - CRAFTING_STATION_TIER_EVOLUTION_STANDARD.md.
2. Copiar os CSVs para uma pasta de dados-fonte versionada.
3. Criar tipos C# de catálogo separados de instâncias runtime.
4. Representar:
   - EnvironmentAssetDefinition;
   - CraftingStationArtDefinition;
   - ModularSnapDefinition;
   - AssetPromptReferences;
   - EnvironmentBiomeId;
   - StationMilestoneTier.
5. Preservar os asset_id existentes.
6. Não usar enum reordenável como ID principal.
7. Criar importador CSV Editor-only idempotente.
8. Não gerar centenas de ScriptableObjects sem justificar; um catálogo agregado é aceitável.
9. Validar:
   - IDs duplicados;
   - categoria;
   - bioma;
   - profissão;
   - Tiers permitidos T1/T3/T5/T7/T9 para estações;
   - escala positiva;
   - orçamento de triângulos;
   - paths de prompt;
   - método Meshy;
   - source art obrigatório;
   - sockets obrigatórios;
   - módulos de grid com regra de snap;
   - referências ausentes.
10. Criar menu:
    Tools → Idle Medieval Legends → Validate Environment Art Catalog
11. Criar relatório por:
    - bioma;
    - categoria;
    - prioridade;
    - fase;
    - método Meshy;
    - estação/profissão;
    - status.
12. Criar metadata/runtime placeholders para:
    - grid size;
    - pivot convention;
    - collider strategy;
    - LOD targets;
    - Unity import path;
    - prefab path;
    - socket names.
13. Não gerar modelos, texturas, materiais ou VFX.
14. Não editar cenas manualmente em YAML.
15. Criar testes EditMode para parsing e validação.
16. Criar Docs/CODEX/TASK_018_MODULAR_ENVIRONMENT_IMPLEMENTATION.md.

Critérios de aceite:
- 60 assets de ambiente e 25 estações carregados.
- IDs únicos.
- Três biomas reconhecidos.
- Cinco profissões e cinco marcos por profissão.
- Paths dos 255 prompts individuais válidos no pacote-fonte.
- Erros claros para escala, Tier, snap, socket e referência inválidos.
- Nenhum modelo inexistente referenciado como importado.
- Testes existentes preservados.
- Nenhum commit automático.

Ao terminar, liste arquivos, comandos, testes executados, resultados reais,
riscos e pendências. Não alegue que Unity ou testes passaram sem execução real.
```

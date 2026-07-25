# TASK 015 — Integrar o catálogo mestre e os orçamentos técnicos de assets

## Branch

```text
chore/art-master-asset-catalog
```

## Prompt para o Codex

```text
TASK 015 — Integrar o catálogo mestre de assets ao projeto Unity

Antes de editar:
1. Leia AGENTS.md.
2. Leia Docs/Art/IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md.
3. Leia Docs/Art/IDLE_MEDIEVAL_LEGENDS_ASSET_PRODUCTION_STANDARD.md.
4. Inspecione Examples/ASSET_MASTER_CATALOG.csv e ASSET_BUDGET_RULES.csv.
5. Execute git status e apresente um plano curto.

Objetivo:
Integrar o catálogo como fonte operacional de planejamento e criar validações de Editor sem gerar modelos 3D nesta task.

Escopo obrigatório:
- preservar todos os asset_id;
- copiar/organizar documentos e planilhas na estrutura do projeto;
- criar um parser somente de Editor para o CSV;
- criar validações de IDs duplicados, caminhos inválidos, budgets ausentes e dependências inexistentes;
- criar relatório por categoria, prioridade, fase, método Meshy e status;
- criar menu Tools/Idle Medieval Legends/Art/Validate Asset Catalog;
- criar menu Tools/Idle Medieval Legends/Art/Export Asset Production Report;
- não criar assets reais, modelos, texturas, cenas ou prefabs vazios;
- não transformar cada linha em ScriptableObject automaticamente;
- não alterar o conteúdo vinculante da Visual Bible;
- criar testes EditMode do parser e das validações;
- documentar arquivos, comandos, testes, riscos e limitações.

Critérios de aceite:
- catálogo é lido sem alterar IDs;
- duplicatas e specs inválidas são rejeitadas;
- relatório é reproduzível;
- código fica em assembly Editor;
- runtime não carrega o CSV;
- nenhum modelo 3D é gerado nesta task;
- testes reais são executados ou declarados como não executados.

Não faça commit automaticamente.
```

# TASK 017 — Equipment, Tier and Rarity Visual System

## Branch

```text
feat/equipment-tier-rarity-visual-system
```

## Prompt para o Codex

```text
TASK 017 — Integrar o sistema visual e o catálogo de equipamentos

Antes de alterar arquivos:
1. Leia AGENTS.md.
2. Leia a Visual Bible da Task 014.
3. Leia o catálogo da Task 015.
4. Leia a Character Design Bible e o padrão de sockets da Task 016.
5. Leia todos os arquivos desta Task 017.
6. Execute git status.
7. Apresente um plano curto.

Objetivo:
Integrar ao repositório a fonte de verdade de 18 famílias de equipamento,
nove Tiers e seis raridades, sem gerar modelos 3D e sem criar 972 assets
ScriptableObject manualmente.

Escopo obrigatório:
- Copiar os documentos para Docs/Art.
- Copiar os CSVs para Docs/Art/Data ou pasta equivalente.
- Criar modelos de catálogo C# para EquipmentVisualFamily, TierVisualDefinition,
  RarityVisualDefinition e EquipmentProductionEntry.
- Criar importador Editor idempotente para os CSVs.
- Não usar nomes visuais como IDs persistidos.
- Preservar os IDs de raridade Common=0, Uncommon=1, Rare=2, Epic=3,
  Legendary=4 e Mythic=5.
- Validar 18 famílias, nove Tiers, 162 bases e seis raridades.
- Validar caminhos de prompt e IDs duplicados.
- Criar menu Tools > Idle Medieval Legends > Validate Equipment Art Catalog.
- Criar relatório com contagem por família, Tier, profissão, prioridade e método.
- Preparar sockets e metadata, mas não importar FBX inexistentes.
- Não gerar ScriptableObjects em massa sem necessidade; preferir catálogo
  consolidado ou geração determinística.
- Criar testes EditMode para parsing, IDs, Tier, raridade, contagens e referências.
- Criar Docs/CODEX/TASK_017_EQUIPMENT_TIER_RARITY_IMPLEMENTATION.md.

Restrições:
- Não gerar arte.
- Não instalar SDK do Meshy.
- Não criar shaders definitivos.
- Não alterar balanceamento.
- Não transformar armaduras em sistema modular skinned no MVP.
- Não fazer commit automaticamente.

Critérios de aceite:
- 162 bases reconhecidas.
- 972 variantes lógicas deriváveis.
- Todos os IDs únicos.
- Tier e raridade continuam conceitos separados.
- Validador informa referências ausentes.
- Testes executados quando o Unity estiver disponível.
- Nenhum asset de produção é falsamente marcado como gerado ou aprovado.
```

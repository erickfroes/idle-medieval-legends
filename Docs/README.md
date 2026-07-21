# Idle Medieval Legends — Starter Architecture v2

Esta revisão adiciona as seis raridades e um domínio completo de profissões/crafting ao pacote inicial.

## Principais mudanças

- Raridades: Comum, Incomum, Raro, Épico, Lendário e Mítico.
- Enum compartilhado entre heróis e itens, com migração explícita da v1.
- Profissões: Ferreiro, Costureiro, Encantador, Alquimista e Coletador.
- Progressão de nível 1–100, graus Aprendiz/Proficiente/Mestre/Grão-Mestre/Deus e Tiers 1–9.
- Especialização suave: todas as profissões podem chegar ao T9; uma profissão principal recebe eficiência, não exclusividade funcional.
- Foco artesanal, estações, jobs com materiais reservados e rolagem de raridade no servidor.
- Orçamento de equipamentos separado por Tier, raridade, afixos e aprimoramento.
- Comissões de crafting com ingredientes e pagamento em escrow.
- Proveniência de itens e unicidade `(job_id, output_index)` para impedir duplicação.
- Cache JSON v2 contendo inventário e profissões.

## Arquivos principais

- `Architecture_GDD.md`: regras de balanceamento, economia, mercado e segurança.
- `Assets/_Game/Scripts/Domain/Common/ProgressionTypes.cs`: enums persistidos.
- `Assets/_Game/Scripts/Domain/Crafting/`: modelos, progressão, elegibilidade e qualidade.
- `Assets/_Game/Scripts/Domain/Equipment/EquipmentBudgetCalculator.cs`: orçamento de equipamento.
- `Assets/_Game/Scripts/Domain/Inventory/InventoryModels.cs`: Tier, raridade, reserva e proveniência.
- `Backend/postgresql_schema_v2.example.sql`: DDL relacional de referência.
- `Backend/crafting_job_transaction.pseudocode.txt`: início/finalização idempotente.
- `Backend/crafting_commission_transaction.pseudocode.txt`: ordens de serviço.
- `Examples/crafting_catalog.example.json`: receitas com dependências cruzadas.
- `Examples/profession_recipe_families_t1_t9.example.csv`: matriz de 45 famílias de receita, cobrindo as cinco profissões em todos os nove Tiers.
- `Examples/player_cache.example.json`: snapshot local v2.

## Migração v1 → v2

A enumeração antiga de heróis era:

```text
0 Common, 1 Rare, 2 Epic, 3 Legendary
```

A v2 é:

```text
0 Common, 1 Uncommon, 2 Rare, 3 Epic, 4 Legendary, 5 Mythic
```

Antes de publicar a atualização:

1. Remapear raridades numéricas antigas com `Backend/rarity_schema_v1_to_v2.sql.example`.
2. Recriar ou migrar assets Unity que tenham serializado o enum antigo.
3. Publicar `CombatBalanceTuning.version = 2` no cliente e servidor.
4. Fazer o backend retornar snapshots `schemaVersion = 2`.
5. Bloquear clientes antigos antes de habilitar conteúdo Incomum/Mítico ou mercado de crafting.

Para dados novos, prefira IDs textuais estáveis no backend (`common`, `uncommon`, `rare`, `epic`, `legendary`, `mythic`).

## Autoridade

Os scripts de domínio podem ser compartilhados com backend C#, mas o aplicativo Unity continua sendo somente cliente. O backend deve controlar saldo, XP profissional, Foco, relógio, ingredientes consumidos, seeds, raridade, pity, criação de instâncias e liquidação do mercado.

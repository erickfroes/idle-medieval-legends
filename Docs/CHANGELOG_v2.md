# Changelog — v2 Raridades e Profissões

## Domínio de progressão

- Adicionadas as raridades Comum, Incomum, Raro, Épico, Lendário e Mítico.
- Adicionadas as profissões Ferreiro, Costureiro, Encantador, Alquimista e Coletador.
- Adicionados os graus Aprendiz, Proficiente, Mestre, Grão-Mestre e Deus.
- Adicionados Tiers persistidos de T1 a T9 e migração explícita da enumeração de raridade v1.

## Crafting e economia

- Progressão profissional de nível 1 a 100, XP, Foco, estação, receitas e jobs.
- Especialização suave com uma profissão principal, sem impedir que a conta maximize as cinco.
- Qualidade de crafting, limites de raridade por Tier e pity Mítico por profissão.
- Orçamento de equipamento por Tier, raridade, afixos e aprimoramento.
- Reservas de materiais, proveniência de item, finalização idempotente e comissões P2P.
- Matriz de conteúdo cobrindo 45 famílias: cinco profissões × nove Tiers.

## Persistência e segurança

- Cache JSON e snapshots atualizados para schema 2.
- Esquema PostgreSQL, regras/índices Firestore e pseudocódigo de transações atualizados.
- Itens craftados registram receita, artesão, transação de origem e hash de seed.
- Saídas usam unicidade lógica `(job_id, output_index)` para impedir duplicação em retries.

## Testes adicionados

- Mapeamento nível → grau/Tier.
- Requisitos para Mítico T9.
- Soma dos pesos de raridade.
- Bônus de duração da profissão principal.
- Soft pity e hard pity do crafting Mítico.
- Orçamento de equipamentos e migração de cache v1.

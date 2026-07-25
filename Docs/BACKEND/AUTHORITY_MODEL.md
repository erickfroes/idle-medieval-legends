# Modelo de autoridade de dados

## Regra central

O cliente apresenta snapshots e envia intenções. O servidor autentica o
jogador, usa seu próprio relógio e catálogo, valida a revisão, calcula o
resultado e persiste a mutação. Um valor em cache nunca é evidência suficiente
de saldo, propriedade, progresso ou resultado.

Classificações:

- **Server Authoritative**: única fonte de verdade mutável.
- **Client Cached**: cópia descartável de dados autoritativos, identificada por
  revisão e substituível pelo servidor.
- **Client Preference**: dado local que não altera economia ou elegibilidade.
- **Static Catalog**: definição versionada; o cliente pode empacotar uma cópia,
  mas o servidor escolhe a versão oficial para comandos.
- **Derived View**: projeção recalculável a partir de dados e regras
  autoritativos; nunca é entrada confiável para uma mutação.

## Matriz de autoridade

| Dado | Classe primária | Cópia/visão no cliente | Regra |
|---|---|---|---|
| Saldo de ouro | Server Authoritative | Client Cached | `long`; somente ledger/transação altera |
| Saldo de Gemas e Gemas em hold | Server Authoritative | Client Cached | `long`; origem e chargeback auditáveis |
| Fragmentos por herói | Server Authoritative | Client Cached | pull e consumo são transacionais |
| Heróis/posse | Server Authoritative | Client Cached | IDs de instância imutáveis |
| Nível e XP de herói | Server Authoritative | Client Cached | custo e progressão recalculados no servidor |
| Ascensão | Server Authoritative | Client Cached | fragmentos debitados na mesma transação |
| Raridade/promover herói | Server Authoritative | Client Cached | enum/ID versionado |
| Inventário | Server Authoritative | Client Cached | snapshot por revisão; nunca merge cego |
| Item state/binding/owner | Server Authoritative | Client Cached | transições validadas; escrow/reserva exclusivos |
| Crafting jobs e outputs | Server Authoritative | Client Cached | relógio/RNG/outputs do servidor |
| Profissões, XP, maestria, estação e Foco | Server Authoritative | Client Cached | Foco usa relógio do servidor |
| Pity de crafting e gacha | Server Authoritative | Client Cached | atualizado junto do resultado |
| Energia | Server Authoritative | Client Cached | regeneração agregada pelo relógio do servidor |
| Campanha, first clears e equipe ativa | Server Authoritative | Client Cached | first clear único por estágio |
| Recompensa offline/report | Server Authoritative | Client Cached | report e claim únicos; cliente não recalcula |
| Dungeon run/tentativas/claim | Server Authoritative | Client Cached | seed, conclusão e recompensa no servidor |
| Gacha pull/history | Server Authoritative | Client Cached | RNG, custo, pity e histórico atômicos |
| Market listing/escrow | Server Authoritative | Client Cached | no máximo um listing ativo por item |
| Market sale/transaction | Server Authoritative | Client Cached | liquidação tudo-ou-nada |
| Account power | Derived View | Client Cached | servidor calcula e pode materializar cache |
| Team power/competitive power | Derived View | Client Cached | derivado de equipe, itens, catálogo e rulesVersion |
| Odds, receitas, custos, estágios e banners | Static Catalog | Static Catalog | comando informa versão; servidor decide suporte |
| Feature flags | Server Authoritative | Client Cached | flags não concedem economia sem validação |
| Idioma, volume, vibração, contraste, escala e velocidade visual | Client Preference | Client Preference | não altera simulação/recompensa |
| Estado de tela, filtros e ordenação local | Client Preference | Client Preference | não persistir como progresso econômico |

## Regras de cache e revisão

1. O snapshot possui `revision` global monotônica por jogador e
   `rulesVersion`/`catalogVersion`.
2. Um patch só é aplicado sobre sua `baseRevision`. Patch fora de ordem causa
   novo `GET /v1/player/snapshot`.
3. O cliente não incrementa `serverRevision`; mutações locais podem usar estado
   visual separado e pendente.
4. Resposta atrasada com revisão inferior nunca substitui snapshot superior.
5. IDs duplicados, enum desconhecido obrigatório ou estado impossível invalidam
   o snapshot inteiro; não se aceita conteúdo parcial silenciosamente.
6. Preferências locais não entram no hash de payload econômico nem na revisão
   do jogador.

## Intenção versus fato

Permitido no payload: `heroInstanceId`, `itemInstanceId`, `recipeId`,
`quantity`, `teamHeroInstanceIds`, `listingId`, preço proposto e seleção de
ferramenta/catalisador.

Não confiável como fato: `playerId`, saldo, owner, winner, reward, rarity,
seed, pity final, taxa final, output item, tempo decorrido ou transferência
concluída. Se algum desses dados for enviado para diagnóstico/replay, ele é
tratado como evidência não autoritativa e verificado contra estado do servidor.

## Relações

- Contratos HTTP: [API_CONTRACTS.md](API_CONTRACTS.md)
- Concorrência: [IDEMPOTENCY_AND_CONCURRENCY.md](IDEMPOTENCY_AND_CONCURRENCY.md)
- Persistência: [DATABASE_SCHEMA.md](DATABASE_SCHEMA.md)
- Segurança: [SECURITY_MODEL.md](SECURITY_MODEL.md)


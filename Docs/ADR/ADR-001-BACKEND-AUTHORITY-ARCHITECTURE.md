# ADR-001 — Arquitetura de autoridade do backend

- Status: Aceita para orientar a implementação futura
- Data: 2026-07-25
- Decisores: equipe de Idle Medieval Legends
- Escopo: contratos e arquitetura; nenhuma infraestrutura de produção foi criada

## Contexto

Idle Medieval Legends possui economia com carteira, inventário de instâncias,
crafting assíncrono, pity, recompensa offline e mercado P2P. A liquidação de
uma venda envolve comprador, vendedor, item em escrow, taxa, ledger,
transação e outbox. O cliente Unity apenas envia intenção; autenticação,
validação, relógio, RNG, saldos, propriedade e resultado econômico são
responsabilidade do servidor.

A decisão assume inicialmente uma equipe pequena com experiência em C# e
necessidade de rastrear incidentes econômicos. Capacidade operacional, volume,
preço dos fornecedores e requisitos regulatórios devem ser reavaliados antes
da produção.

## Opções avaliadas

Escala qualitativa: `Alta` é favorável nas linhas de capacidade e desfavorável
nas linhas de custo/complexidade/lock-in.

| Critério | ASP.NET Core + PostgreSQL | Firebase/Firestore | PlayFab | Unity Gaming Services | Híbrida |
|---|---|---|---|---|---|
| Transações multi-entidade | Alta; transação ACID e locks explícitos | Média/alta dentro dos limites de transação por documentos | Média; APIs econômicas ajudam, mas a liquidação cross-player exige desenho adicional | Média; write locks são por dado e fluxos multi-entidade exigem saga ou banco transacional | Alta quando wallet/mercado ficam em PostgreSQL |
| Mercado P2P | Alta; modelo relacional e índice único parcial | Média; possível, com contenção/custos e reconciliação cuidadosamente modelados | Média; inventário gerenciado, mas escrow e settlement cross-player continuam específicos do jogo | Baixa/média sem componente transacional próprio | Alta |
| Wallet ledger | Alta; append-only, constraints e reconciliação SQL | Média; ledger documental é possível, mas consultas/custo e invariantes relacionais exigem disciplina | Média/alta para operações cobertas; ledger próprio ainda é útil para a semântica do jogo | Média; exige modelagem própria | Alta |
| Idempotência | Alta; tabela e resposta armazenada sob a mesma transação | Alta se implementada transacionalmente | Alta nas APIs que suportam `IdempotencyId`, com retenção definida pelo fornecedor | Média; precisa de contrato e armazenamento próprios | Alta |
| Escalabilidade | Alta, com particionamento, réplicas e workers planejados | Alta e gerenciada | Alta e gerenciada | Alta e gerenciada | Alta, com mais componentes |
| Custo | Infra e operação previsíveis, mas exige engenharia/plantão | Baixa entrada; custo varia por leituras, escritas e índices | Baixa entrada; custo cresce por MAU/uso e serviços | Baixa entrada; custo cresce por uso | Maior risco de custo duplicado e egress |
| Complexidade | Média/alta de implementação e operação | Média; baixa infra, alta complexidade de invariantes P2P | Média; integrações específicas e limites do produto | Média; integrações específicas e composição de serviços | Alta |
| Observabilidade | Alta e customizável | Alta via Google Cloud, com correlação a configurar | Boa no ecossistema, com dependência das superfícies expostas | Boa no ecossistema, com dependência das superfícies expostas | Alta, porém correlação distribuída é obrigatória |
| Prevenção de duplicação | Alta com constraints, dedup, ledger e outbox | Boa com transações e chaves determinísticas | Boa nas operações idempotentes suportadas | Depende do desenho de write locks/dedup | Alta |
| Suporte a Unity | HTTP/JSON simples; SDK próprio pequeno | SDK Unity e REST | SDK e serviços voltados a jogos | Integração Unity nativa | Boa, mas com mais adaptadores |
| Lock-in | Baixo/médio; PostgreSQL e HTTP são portáveis | Alto para modelo/SDK/regras Firestore | Alto para identidade/economia/catálogo | Alto para Cloud Code/Cloud Save | Médio/alto conforme os serviços escolhidos |
| Operação pela equipe | Exige ownership de API, banco, deploy, backup e incidentes | Menor carga de banco; requer domínio de Firebase/GCP | Menor carga de plataforma; requer domínio de PlayFab/Azure | Menor carga inicial para equipe Unity | Maior carga de integração e fornecedores |

Fontes oficiais consultadas:

- Firestore oferece transações atômicas, com retry e limites próprios:
  <https://firebase.google.com/docs/firestore/manage-data/transactions>.
- PlayFab Economy documenta idempotência e concorrência por ETag:
  <https://learn.microsoft.com/en-us/gaming/playfab/economy-monetization/economy-v2/tutorials/idempotent-transactions-and-retries>
  e
  <https://learn.microsoft.com/en-us/gaming/playfab/economy-monetization/economy-v2/tutorials/etags-and-concurrency-control>.
- UGS Cloud Code/Cloud Save oferece autoridade de servidor e write locks:
  <https://docs.unity.com/en-us/cloud-save/tutorials/cloud-code> e
  <https://docs.unity.com/en-us/cloud-save/concepts/write-locks>.
- PostgreSQL documenta isolamento, locks e índices:
  <https://www.postgresql.org/docs/current/mvcc.html>,
  <https://www.postgresql.org/docs/current/explicit-locking.html> e
  <https://www.postgresql.org/docs/current/indexes-partial.html>.

## Decisão

Adotar como arquitetura-alvo:

```text
Unity Client
    → HTTPS/JSON API
ASP.NET Core
    → serviços de aplicação e domínio
PostgreSQL
    → transações ACID, wallet ledger, dedup, audit e outbox
Workers
    → finalização de jobs, expiração, publicação e reconciliação
Outbox
    → notificações, analytics e integrações após commit
```

O núcleo econômico — carteiras, ledger, inventário, crafting, pity, gacha,
idle, dungeons e mercado — permanece no serviço próprio e no PostgreSQL. Uma
arquitetura híbrida é permitida para identidade federada, App Attestation,
push, analytics, catálogo/CDN ou validação de recibos, desde que esses serviços
não se tornem autoridade paralela sobre saldos ou propriedade.

## Justificativa

PostgreSQL reduz a distância entre as invariantes do jogo e as garantias do
banco: compra P2P em tudo-ou-nada, unicidade de listing/transação/output,
wallet não negativa, deduplicação e outbox podem compartilhar uma transação.
ASP.NET Core alinha-se à linguagem C# já usada pela equipe e mantém o contrato
do cliente em HTTPS/OpenAPI, sem compartilhar classes de domínio por binário.

Firebase, PlayFab e UGS continuam opções válidas para escopos menores e
integrações gerenciadas. Eles não foram escolhidos como autoridade econômica
principal porque o mercado P2P e o ledger exigem semântica multi-entidade
específica, auditoria e reconciliação. A opção híbrida torna-se a alternativa
preferida se autenticação, attestation ou notificações gerenciadas reduzirem
custo operacional sem fragmentar a fonte de verdade.

## Consequências

- A equipe deverá operar API, PostgreSQL, migrations, backup, observabilidade,
  deploy e resposta a incidentes.
- Toda mutação econômica usa autenticação, idempotência, revisão, transação,
  ledger/auditoria quando aplicável e outbox.
- Workers são consumidores idempotentes; não publicam efeitos externos antes
  do commit.
- Read models e caches podem ser eventualmente consistentes, mas a escrita
  econômica tem uma única fonte autoritativa.
- O OpenAPI e os contratos versionados são a fronteira entre Unity e backend.
- Nenhum provedor, attestation, regra de segurança ou arquitetura impede fraude
  completamente. Camadas técnicas reduzem risco e fornecem sinais; regras de
  negócio, monitoramento, limites, investigação e compensação continuam
  necessários.

## Alternativas e gatilhos de revisão

Reavaliar a decisão se a equipe não conseguir sustentar operação 24/7, se o
volume exigir especialização de banco, se requisitos de lançamento favorecerem
um backend gerenciado, ou se uma prova de conceito demonstrar atomicidade,
auditoria, custo e operação superiores em outra plataforma. A revisão deve
preservar os contratos de autoridade, idempotência e ledger, não apenas trocar
tecnologia.


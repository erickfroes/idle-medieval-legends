# TASK 008 — Profissões e crafting como vertical slice

Data: 2026-07-22

Branch: `feat/crafting-professions-vertical-slice`

Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo e fronteira de autoridade

Esta task implementa um protótipo local completo para validar arquitetura,
regras, UX e testes de profissões/crafting. `LocalCraftingService` simula o
limite transacional do servidor e trabalha com um relógio injetado. Ele não é
autoridade de produção: início, reserva, ouro, seed, conclusão, XP, pity e
criação de itens deverão ser recalculados e persistidos pelo backend futuro.

Não foram implementados mercado P2P, comissões entre jogadores, Gemas,
compras, anúncios ou backend.

## Arquitetura

- `ProfessionProgress` mantém um progresso independente para cada uma das cinco
  profissões: tipo, nível, XP acumulado, grau, maior Tier, XP/pontos/nodes de
  maestria, especialização, estação, Foco, pity, receitas e revisão.
- `ProfessionProgressionTuning` centraliza os thresholds; grau e Tier são sempre
  derivados por `ProfessionProgression`, sem tabelas paralelas na UI.
- `CraftingRuntimeTuning` centraliza bônus, filas, cancelamento, XP, Foco,
  qualidade, tabelas de raridade por Tier e pity.
- `CraftingJob` contém identidade, dono, profissão, receita, quantidade, estado,
  tempos do servidor, reservas, ferramenta/catalisador, seed, saídas, versões,
  falha e proveniência.
- `CraftingQueue` controla slots e estados. A fila é compartilhada no protótipo.
- `LocalCraftingService` orquestra catálogo, progresso, economia local,
  inventário, relógio e RNG; apresentação somente envia intenção.
- `IServerClock` é a porta futura do relógio autoritativo. Testes usam
  `ManualServerClock`; o protótipo usa `LocalPrototypeServerClock`, monotônico a
  partir de um instante UTC local apenas para demonstração.
- `IGoldEconomyService` separa o débito/crédito. `LocalGoldEconomyService` usa
  `long` e existe somente como simulação.
- `PlayerInventory` oferece reserva, liberação e consumo por `reservationId`.
  Itens reservados ficam `ReservedByServer` e as demais mutações continuam
  bloqueadas.

## Profissões, graus, Tiers e estações

As cinco profissões e estações são:

| Profissão | Estação |
|---|---|
| Ferreiro | Forja |
| Costureiro | Ateliê |
| Encantador | Mesa Arcana |
| Alquimista | Laboratório |
| Coletador | Acampamento de Expedição |

Thresholds padrão:

| Grau | Níveis | Tiers e níveis de liberação |
|---|---:|---|
| Aprendiz | 1–19 | T1 no 1; T2 no 10 |
| Proficiente | 20–39 | T3 no 20; T4 no 30 |
| Mestre | 40–63 | T5 no 40; T6 no 52 |
| Grão-Mestre | 64–89 | T7 no 64; T8 no 76 |
| Deus | 90–100 | T9 no 90 |

Cada profissão progride separadamente e pode chegar a Deus/T9. Estação também
possui Tier 1–9 e nunca é ignorada pelo grau ou especialização.

## Especialização suave

Uma única profissão é `Primary`. O padrão configurado concede:

- +2.000 bp (+20%) de XP;
- -1.500 bp (-15%) de duração;
- +5 pontos de qualidade;
- +1 slot ao atingir Mestre.

A troca preserva nível, XP, Tier, receitas, maestria e pity. Após a seleção
inicial, usa cooldown padrão de 604.800 segundos (sete dias) e 10.000 de ouro.
Não há Gemas. Repetir a mesma seleção ou trocar durante cooldown é rejeitado.

## Foco, filas e maestria

Foco inicial/máximo é 100 por progresso no protótipo. O custo efetivo usa
inteiros:

```text
Foco = ceil(custoBase × quantidade × (10.000 - reduçãoBp) / 10.000)
```

`reduçãoBp` é limitada de modo que o custo nunca fique abaixo de 50% do custo
base. A fila possui dois slots-base, bônus configurável por grau, +1 para a
profissão principal Mestre e bônus externos/nodes com teto configurável.

Maestria começa em Mestre e somente receitas entre os dois maiores Tiers
liberados concedem XP de maestria. A fundação inclui os ramos Eficiência,
Excelência e Comércio, com nodes demonstrativos reconhecidos pelas regras:

- `efficiency_focus_1` e `efficiency_duration_1`;
- `excellence_quality_1`;
- `commerce_queue_1`.

Os limites econômicos permanecem: Foco mínimo de 50%, nenhum node duplica item
único, substitui Catalisador Divino, altera queima futura do mercado ou cria
saída sem proveniência.

## Início, reserva, duração e cancelamento

O início rejeita receita ausente/desabilitada/não aprendida, profissão errada,
nível, grau, Tier ou estação insuficientes, Foco/ouro/material insuficiente,
fila cheia, quantidade inválida e ferramenta/catalisador bloqueado, equipado,
reservado ou em escrow.

Após validar, o serviço reserva materiais e ferramenta/catalisador, consome
Foco, debita ouro, cria seed internamente, cria o job e registra
`craft_started`. Nenhuma saída nasce no início.

Duração:

```text
duração = ceil(duraçãoBase × (10.000 - reduçõesBp) / 10.000)
```

O cancelamento padrão é permitido até 50% da duração: devolve materiais, não
devolve Foco, devolve 50% do ouro e aplica penalidade de 50%. Job pronto para
coleta ou concluído não pode ser cancelado. A soma configurada de reembolso e
penalidade deve ser exatamente 10.000 bp.

## Estados e idempotência

Estados persistidos: `Pending`, `Running`, `ReadyToClaim`, `Completed`,
`Cancelled` e `Failed`. Aliases obsoletos preservam os IDs numéricos anteriores
do cache.

Ao atingir `completesAtServerTime`, o job passa a `ReadyToClaim`. A conclusão:

1. resolve qualidade/raridade com seed do job;
2. consome reservas materiais e libera recursos não consumíveis;
3. cria saídas com chave lógica `jobId + outputIndex`;
4. concede XP/maestria e atualiza pity;
5. grava resultado/proveniência e marca `Completed`.

IDs locais seguem `jobId_output_0000`. Repetir `CompleteCraft(jobId)` retorna o
mesmo `CraftingResult`; não consome novamente, não concede XP novamente e não
cria outra instância.

## Qualidade, raridade e RNG

Qualidade é uma pontuação 0–100 separada de raridade:

```text
qualidade = margemDeNível
          + 8 × excedenteDeEstação
          + ferramenta
          + catalisador
          + maestria
          + 5 se profissão principal
```

Os thresholds configurados selecionam as faixas Padrão, Habilidoso,
Especialista, Dominado e Divino. Raridade é rolada depois, exclusivamente pelo
serviço, com `DeterministicCraftingRandom` e basis points inteiros.

Cada Tier possui tabela configurável; cada linha soma exatamente 10.000 bp.
Caps padrão: T1–T2 Raro, T3–T4 Épico, T5–T8 Lendário e T9 Mítico somente quando
elegível. Pesos acima do cap são incorporados à maior raridade permitida.

Tier continua determinando orçamento-base. Raridade usa os multiplicadores,
quantidade de afixos e teto de melhoria do catálogo: Comum, Incomum, Raro,
Épico, Lendário e Mítico. O resultado expõe raridade, qualidade e afixos
separadamente.

## Pity Mítico

Elegibilidade exige simultaneamente T9, grau Deus, receita marcada e
`material_divine_catalyst_t9`. O contador representa falhas elegíveis anteriores:

- tentativa 1 usa contador 0 e chance-base;
- tentativa 50 usa contador 49 e chance-base;
- após falhar a 50ª, o contador vira 50;
- tentativa 51 recebe o primeiro +5 bp;
- tentativa 100 usa contador 99 e é garantida;
- falha elegível incrementa somente a profissão do job;
- Mítico zera somente aquela profissão;
- tentativa não elegível não altera contador.

Há teste de simulação das 100 tentativas, além dos testes pontuais de soft pity,
separação e reset.

## XP profissional

O XP usa Tier, duração, quantidade, sucesso, especialização e distância do maior
Tier liberado:

```text
base = XPBaseDoTier + floor(duraçãoSegundos / 60) × XPPorMinuto
XP = base × quantidade
   × multiplicadorDeDefasagem
   × multiplicadorDeSucesso
   × multiplicadorDeEspecialização
```

Multiplicadores de defasagem padrão: 100%, 65%, 25% e 5% para zero, um, dois e
três ou mais Tiers abaixo. Isso impede T1 de ser a melhor rota até nível 100.
Os cálculos de proporção usam basis points; a curva de nível permanece a curva
configurável `round(100 × L^1,55)` já estabelecida no GDD.

## Conteúdo e interface

Receitas jogáveis demonstrativas:

- Ferreiro: Refinar Lingote de Ferro T1; Espada de Ferro T1;
- Costureiro: Couro Tratado T1; Túnica de Couro T1;
- Encantador: Anel Arcano T1;
- Alquimista: Tônico Menor T1;
- Coletador: Expedição de Minério de Ferro T1.

`recipe_divine_test_blade_t9` fica desabilitada no gameplay normal e existe
somente para testes do pity.

A cena `Crafting` apresenta profissões, nível, grau, Tier, estação, Foco, XP,
maestria, receitas, requisitos, materiais, duração, fila, tempo restante,
iniciar, cancelar, concluir/coletar e resultado. A cena `Battle` recebe acesso
ao painel. `CraftingPanelController` apenas encaminha comandos e renderiza o
estado retornado; não escolhe seed, chance, raridade, qualidade ou saída.

O protótipo local é criado somente sob `UNITY_EDITOR`, `DEVELOPMENT_BUILD` ou
`UNITY_INCLUDE_TESTS`. Em uma build de distribuição, o `GameManager` não o
inicializa, a fábrica e o construtor recusam instanciação e o painel informa
que o serviço autoritativo é necessário. A troca autoritativa de jogador
descarta fila, progresso, carteira e identidade locais do jogador anterior.

Jobs usam IDs internos `craft_<guid>`; assim, reiniciar o serviço local não
reinicia um contador nem reutiliza a chave lógica de saída
`jobId + outputIndex`. Antes de consumir reservas na conclusão, qualquer saída
já existente é validada contra jogador, definição, receita, transação e
proveniência do próprio job. Uma colisão estrangeira falha sem consumir os
materiais.

## Testes e validação

EditMode cobre thresholds, nove Tiers, cinco profissões/estações, receitas,
especialização/cooldown, fila, validações de início, reserva, cancelamento,
conclusão/idempotência, XP, redução de XP, qualidade, tabelas em basis points,
pity não elegível, soft pity, hard pity, simulação de 100 tentativas, separação
por profissão, reset e proveniência. As regressões adicionais cobrem IDs após
reinício, troca de jogador, atomicidade de timestamps, raridade da instância,
compatibilidade de `RefinedMaterial` e rejeição de proveniência ausente.

PlayMode cobre abertura do painel, seleção de profissão/receita, início, fila,
tempo simulado, conclusão e recebimento no inventário.

Resultados finais ficam em
`%TEMP%/IdleMedievalLegends-validation-task008-review`:

- importação/compilação incremental: código 0, sem `error CS` ou `warning CS`;
- EditMode: `Passed`, 203 executados, 203 aprovados, zero falhas/ignorados;
- PlayMode: `Passed`, 12 executados, 12 aprovados, zero falhas/ignorados;
- catálogo e cena `Crafting` foram gerados pelas APIs do Editor com código 0.

A compilação/importação final, as validações específicas de catálogo/cena e a
auditoria do diff são repetidas no fechamento da task; tentativas intermediárias
com falha não são contabilizadas como sucesso.

## Limitações e riscos

- Progresso runtime, carteira e jobs desta fatia são mantidos em memória e o
  protótipo nem sequer é inicializado em builds de distribuição; o cache legado
  de profissões ainda não foi migrado para todos os novos campos.
- A reserva registra a quantidade exata a consumir, mas deixa a pilha inteira
  indisponível enquanto o job está ativo. Uma implementação de produção deve
  fazer split transacional ou reservas parciais versionadas.
- Ferramentas e nodes são demonstrativos; não há desgaste completo nem árvore
  de talentos editável na UI.
- Afixos demonstrativos são IDs de proveniência; rolagem materializada de stats
  e orçamento de afixos permanece trabalho futuro.
- O relógio local é adequado somente ao protótipo. Produção deve usar timestamp
  e revisão do servidor e nunca confiar no relógio do aparelho.
- A transação local possui rollback do início, mas não substitui banco ACID,
  command deduplication, outbox, ledger e reconciliação.
- Não foram executados builds Android/iOS nem smoke tests em dispositivo.

## Ausência de commit

Nenhum commit foi criado automaticamente nesta task.

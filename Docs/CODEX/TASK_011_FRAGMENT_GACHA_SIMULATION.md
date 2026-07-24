# TASK 011 — Simulador de gacha por fragmentos

Data: 2026-07-24

Branch: `feat/fragment-gacha-simulator`

Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo e limite de autoridade

Esta task implementa uma bancada local para testar distribuição, pity, custo e
progressão antes de qualquer monetização. Não existem IAP, anúncios, loja,
Gemas reais nem comunicação com backend.

`DevelopmentGachaCurrency`, `GachaSimulator`, `FragmentWallet`, o conteúdo
demonstrativo e seus modelos são compilados somente sob:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
```

Uma compilação normal de produção não contém essa moeda nem uma autoridade local
de gacha. Resultados calculados aqui não podem ser aceitos como saldo, inventário,
pity ou recompensa oficial. Em produção, o servidor deverá gerar a seed, aplicar
as regras versionadas, debitar a moeda oficial e persistir o resultado de forma
transacional e idempotente.

## Banner demonstrativo

O banner `development_fragment_banner_001`, versão
`task_011_dev_rules_v1`, usa somente `DevelopmentGachaCurrency`.

| Entrada | Tipo | Raridade | Quantidade | Peso | Probabilidade-base |
|---|---|---:|---:|---:|---:|
| Paladino | fragmentos | Épica | 10 | 1.250 | 12,50% |
| Arqueira | fragmentos | Rara | 8 | 4.000 | 40,00% |
| Mago | fragmentos | Épica | 10 | 1.250 | 12,50% |
| Pó de desenvolvimento | auxiliar | Comum | 10 | 2.000 | 20,00% |
| Pó de desenvolvimento | auxiliar | Incomum | 5 | 1.500 | 15,00% |

Os pesos somam 10.000 e o sorteio usa inteiros. A probabilidade-base agregada é
20% Comum, 15% Incomum, 40% Rara e 25% Épica. Fragmentos representam 65% dos
resultados e três das cinco entradas. O Paladino é o único destaque; a garantia
de destaque fica desabilitada no banner principal porque o GDD não a especifica.

Essa tabela é deliberadamente uma hipótese econômica da bancada, não substitui a
tabela geral do GDD (42/30/17/8/2,5/0,5 por raridade). O catálogo demonstrativo
possui apenas uma Arqueira Rara e dois heróis Épicos; simulações futuras devem
comparar outros pools sem adulterar a raridade dos heróis.

Pull simples custa 100 unidades simuladas. O multi-pull de dez custa 900. Custos
são `long`; não há desconto, preço ou saldo em Gemas.

O modelo aceita `DirectHeroUnlock` como exceção configurável e inclui uma entrada
de exemplo desabilitada. O banner principal não contém desbloqueio direto.
Duplicata de desbloqueio direto pode ser convertida em fragmentos pela regra da
raridade. Dentro de um multi-pull, o primeiro desbloqueio direto avança a
propriedade provisória do lote e cópias posteriores já são convertidas. O estado
é consolidado somente após a operação válida. A conversão não progride o herói
automaticamente.

## Pool, validação e elegibilidade

`GachaProbabilityValidator` rejeita:

- banner ou pool vazio;
- ID, `entryId`, versão ou grupo de pity ausente;
- definição de herói inexistente;
- peso zero/negativo ou soma não positiva;
- fragmentos/custos inválidos;
- raridade persistida inválida;
- hard pity menor que soft pity;
- garantia de destaque sem entrada destacada qualificadora;
- datas invertidas;
- banner desabilitado, não iniciado ou expirado;
- request sem ID, seed explícita, timestamp/progresso válidos;
- quantidade diferente de um ou do multi-pull configurado.

O progresso mínimo é aplicado antes do sorteio. Se o filtro não deixar entrada
qualificadora para hard pity, a operação falha explicitamente.

## RNG e ordem do multi-pull

O RNG é uma implementação local determinística de SplitMix64. Ele não usa
`UnityEngine.Random`, `float` ou `double` como fonte do sorteio. A seleção
ponderada usa `long`, rejeição de viés modular e pesos inteiros.

O simulador exige seed explícita para reprodução de testes. Isso é uma capacidade
exclusiva da bancada: um cliente de produção nunca deve escolher ou conhecer a
seed autoritativa.

Um multi-pull cria um único RNG e avança a mesma sequência dez vezes. Cada
resultado registra `sequence` de 0 a 9 e recebe o estado de pity imediatamente
anterior e posterior. Não são dez requests independentes.

## Pity

O banner principal usa um trilho de alta raridade com qualificador Épico:

- soft pity inicia exatamente no 21º pull sem Épico;
- cada falha após o início adiciona 1.000 basis points de peso efetivo ao
  conjunto qualificante, de forma progressiva e proporcional aos pesos Épicos;
- hard pity aplica Épico ou superior exatamente no 30º pull;
- obter Épico ou superior zera `pullsSinceHighRarity`;
- o estado usa o grupo `development_epic_fragment_family`, permitindo
  compartilhamento entre banners equivalentes;
- total, revisão e garantia de destaque avançam em ordem por recompensa.

O bônus é calculado com inteiros escalados. `GachaPityState` contém
`bannerOrGroupId`, `pullsSinceHighRarity`, `featuredGuarantee`, `totalPulls` e
`revision`.

Decisão de escopo: o simulador usa um trilho qualificante configurável. O GDD
descreve garantias simultâneas de Raro/Épico e pities independentes para
Lendário/Mítico. Esses quatro trilhos não foram comprimidos em um contador
ambíguo; continuam como requisito explícito do backend/uma futura versão do
simulador.

Quando habilitada em outro banner, a garantia featured marca a perda de uma
recompensa alta não destacada e força a próxima recompensa alta a usar somente
entradas destacadas elegíveis. Resultados baixos não consomem a garantia.

## Idempotência e histórico

`GachaSimulator` mantém índice local por `requestId`. Repetir o mesmo payload:

- devolve as mesmas recompensas;
- informa replay idempotente;
- cobra zero;
- não avança pity;
- não credita fragmentos;
- não adiciona histórico.

Reutilizar o ID com banner, quantidade ou seed diferentes é rejeitado.
`playerProgress` e `logicalTimestamp` também fazem parte do payload idempotente
e não podem mudar em um replay.

Cada `GachaHistoryEntry` é serializável e contém request, banner, sequência,
recompensa, pity antes/depois, seed, versão de regras e timestamp lógico. O
histórico é cache/auditoria de desenvolvimento; produção precisa persistir
deduplicação e ledger no servidor.

## Fragmentos, desbloqueio e ascensão

`FragmentWallet` serializa `List<HeroFragmentBalance>`. Seu `Dictionary` existe
somente como índice reconstruído em memória após o `JsonUtility`; não há
`Dictionary` no JSON.

Recompensas de fragmento são agregadas por `heroDefinitionId` com aritmética
verificada. `BannerEligibilityRules.Unlock` é uma operação separada:

1. valida `HeroInstance`, raridade e custo da Task 004;
2. combina apenas para elegibilidade os fragmentos já presentes no herói e a
   carteira;
3. transfere da carteira somente o valor ainda necessário;
4. chama `HeroProgressionRules.Unlock`;
5. preserva qualquer excedente na carteira;
6. rejeita um segundo desbloqueio.

A ascensão segue o mesmo padrão e chama `HeroProgressionRules.Ascend`. Não há
consumo automático ao receber fragmentos.

## Simulação e métricas

`GachaSimulator.SimulateAggregate` não cria histórico ou recompensa individual.
Ele pré-filtra o pool uma vez e mantém apenas contadores, estado primitivo de
pity e intervalos de alta raridade necessários aos percentis.

`GachaSimulationReport` contém:

- pulls e seed;
- frequência por recompensa e raridade;
- média, máximo, variância e percentis P50/P90/P95/P99 dos intervalos até alta
  raridade;
- ativações de hard pity;
- taxa featured entre recompensas altas;
- total e média de fragmentos por herói;
- custo médio simulado por desbloqueio teórico.

O custo por desbloqueio divide o custo total (priorizando pacotes multi-pull)
pela soma fracionária de desbloqueios possível com os fragmentos gerados. É uma
métrica econômica de expectativa; não arredonda jogadores ou cria heróis.

## Ferramenta de Editor

Menu:

**Tools > Idle Medieval Legends > Economy > Run Gacha Simulation**

A janela mostra claramente o ambiente de desenvolvimento, banner, odds-base,
custos, saldo simulado, pull simples, multi-pull, pity, resultados, fragmentos e
botão de desbloqueio quando elegível. Ela aceita número de pulls e seed e exporta
JSON ou CSV para caminho escolhido.

`RunValidationSimulations` executa 10.000, 100.000 e 1.000.000 de pulls e escreve
JSON em:

```text
%TEMP%/IdleMedievalLegends-validation/gacha/
```

Resultados grandes não ficam no repositório. A pasta opcional
`/GachaSimulationResults/` também foi adicionada ao `.gitignore`.

## Resultados de validação

Seed comum: `11011`.

| Pulls | Média até Épico | Máximo | Hard pity | Featured | Custo/ desbloqueio |
|---:|---:|---:|---:|---:|---:|
| 10.000 | 4,042 | 25 | 0 | 50,08% | 952,13 |
| 100.000 | 4,030 | 28 | 0 | 49,80% | 943,23 |
| 1.000.000 | 3,997 | 30 | 1 | 49,86% | 943,83 |

Os testes estatísticos usam tolerância absoluta ampla (±1 ponto percentual para
uma amostra de 100 mil sem pity), evitando testes frágeis. O teste de um milhão
também confirma soma dos contadores, ausência de overflow e máximo limitado pelo
hard pity.

Validação executada no Unity `6000.5.4f1`:

- importação/compilação: código 0, sem `error CS` ou `warning CS`;
- testes específicos de gacha: 27 executados, 27 aprovados;
- suíte EditMode completa: 276 executados, 276 aprovados, zero falhas ou
  ignorados;
- simulações agregadas de 10 mil, 100 mil e 1 milhão: código 0 e três relatórios
  JSON gerados na pasta temporária;
- builds Android/iOS e smoke test em dispositivo: não executados.

## Limitações e requisitos de backend

- Não há saldo oficial, autenticação, attestation, ledger ou transação.
- O pity local é descartável e possui somente um trilho configurável.
- O request deduplica apenas durante a vida do objeto simulador.
- O timestamp é lógico e fornecido pela ferramenta; produção usa relógio do
  servidor.
- A seed explícita é insegura fora de testes.
- A configuração demonstrativa não é LiveOps nem catálogo remoto.
- Odds, pity e versão devem ser publicados claramente na UI final.
- O backend deve validar banner, progresso, propriedade, custos, duplicatas,
  concorrência e limites; gerar RNG/seed; persistir pity/histórico; debitar e
  creditar na mesma transação; registrar ledger/outbox; e devolver retries pelo
  mesmo `requestId`.
- Nenhuma monetização foi implementada.

## Ausência de commit

Nenhum commit é criado automaticamente por esta task.

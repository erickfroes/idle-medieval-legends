# TASK 005 — Simulador de batalha determinístico

Data: 2026-07-21

Branch: `feat/deterministic-combat-simulator`

Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo

Criar uma simulação de batalha síncrona em C# puro, sem cena ou apresentação,
que transforme snapshots de duas equipes em resultado, estados finais e um log
reproduzível. O resultado local é diagnóstico/previsão; não substitui a
validação futura do servidor.

## Modelo da simulação

- `BattleRequest`: atacante, defensor, seed positiva, configuração, ID opcional
  e versão obrigatória das regras;
- `BattleConfiguration`: limites, action gauge, ataque, variação, dano mínimo,
  crítico, acerto/evasão, Defesa e política de alvo;
- `BattleTeam`: lado e coleção defensiva de até cinco snapshots;
- `BattleUnit`: entrada imutável materializada antes do combate;
- `BattleUnitState`: cópia mutável privada ao fluxo da batalha;
- `CombatSnapshot`: visão imutável do estado atual/final;
- `BattleSimulator`: loop, término e emissão de eventos;
- `BattleResult`: outcome, vencedor opcional, contadores, motivo, eventos,
  snapshots finais e hash;
- `TurnOrderResolver`, `TargetSelector` e `DamageCalculator`: políticas
  separadas e testáveis;
- `DeterministicRandom`: único gerador pseudoaleatório aceito pelo simulador.

`BattleUnitFactory.FromHero` consulta `HeroPowerCalculator` uma única vez para
materializar Vida, Ataque, Defesa e Velocidade finais. Depois disso, o motor não
retém nem consulta `HeroInstance`, catálogo, equipamentos ou estado externo.

## Determinismo e RNG

O RNG implementa `xorshift64*` com estado privado e seed `long` positiva. A
sequência do algoritmo faz parte de `combat_rules_v1`; trocá-la exige nova
versão de regras. Não são usados `System.Random`, `UnityEngine.Random`, relógio,
frame rate, `MonoBehaviour`, cena ou estado global mutável.

Coleções que influenciam a resolução são ordenadas explicitamente por lado,
slot e `unitId`. O hash SHA-256 usa uma representação canônica com cultura
invariante de todos os eventos e snapshots. Ele serve para comparação local e
detecção de divergência, não como assinatura segura ou prova de servidor.

## Ordem de ação

O limiar padrão do action gauge é 1.000. Para cada unidade viva:

```text
TempoAtéAção = max(0, Limiar - GaugeAtual) / Velocidade
Avanço       = menor TempoAtéAção entre as unidades vivas
NovoGauge    = GaugeAtual + Velocidade × Avanço
```

Uma unidade elegível consome um limiar ao agir. Empates usam, nesta ordem:

1. maior gauge;
2. maior Velocidade;
3. atacante antes do defensor;
4. menor slot;
5. `unitId` ordinal.

Cada ataque básico corresponde a um turno na versão inicial. O tick lógico é o
número do turno e independe de tempo real.

## Alvo, acerto e dano

`ITargetSelector` permite substituir a política. `TargetSelector` oferece:

- `LowestSlot`: menor slot vivo e depois `unitId` ordinal;
- `Random`: índice entre alvos vivos usando somente `DeterministicRandom`.

Alvos mortos e unidades do mesmo lado são removidos antes da seleção.

```text
ChanceAcerto = clamp(Acurácia - Evasão, ChanceMínima, ChanceMáxima)
DanoBruto    = Ataque × MultiplicadorAtaque
DanoCrítico  = DanoBruto × MultiplicadorCrítico, quando o roll crítico acerta
DanoVariado  = DanoCrítico × uniform(95%, 105%)

M_nível(L)   = 1 + 0,065 × (L - 1) + 0,00035 × (L - 1)²
K(L)         = 400 × M_nível(L do alvo)
Redução      = clamp(Defesa / (Defesa + K(L)), 0, 0,75)
DanoFinal    = max(DanoMínimo, floor(DanoVariado × (1 - Redução)))
```

Configurações podem alterar todos os parâmetros. Valores não finitos,
negativos ou acima de `long` são rejeitados. O dano aplicado é limitado à vida
atual, portanto não gera vida negativa ou cura por overflow.

## Eventos e reprodução visual

`CombatEvent` é imutável e contém sequência, turno, tick lógico, tipo, fonte,
alvo, valor, crítico, vida anterior/posterior e uma lista ordenada de metadata.
São produzidos:

- `BattleStarted`;
- `TurnStarted`;
- `UnitSelected`;
- `BasicAttackStarted`;
- `AttackMissed`;
- `CriticalHit`;
- `DamageDealt`;
- `UnitDefeated`;
- `TurnEnded`;
- `BattleEnded`.

A apresentação futura deve consumir o log em ordem e animar seus efeitos sem
recalcular alvo ou dano. A sequência começa em zero, não possui lacunas e os
ticks nunca regridem.

## Limites e validações

- equipes não podem ser vazias nem ultrapassar cinco unidades;
- slots válidos ficam em `0..maximumTeamSize-1` e não se repetem no lado;
- `unitId` é único globalmente na batalha;
- vida inicial, ataque e velocidade devem ser positivos; Defesa pode ser zero;
- nível começa em 1; probabilidades ficam entre 0 e 1;
- seed deve ser positiva e `rulesVersion` não pode ser vazia;
- o simulador termina por eliminação ou pelo limite configurável de ações;
- atingir o limite produz `Draw`, sem vencedor, com motivo `action_limit` no
  resultado e no evento final.

## Runner de Editor

O menu **Tools > Idle Medieval Legends > Combat > Run Deterministic Demo
Battle** carrega os assets existentes e executa Paladino/Arqueira contra Mago.
Ele não cria nem carrega cena e imprime uma única linha de resumo. A entrada de
linha de comando repete o mesmo cenário três vezes e rejeita hashes divergentes.

Resultado observado durante o desenvolvimento:

```text
Seed=5005 | Vencedor=Attacker | Turnos=13 | Ações=13
Dano total=1251 | Derrotados=1
Hash=68bd4a055c6fd6b95e9fae68d035ee2c2fa50c308e4f65181189b9e2f60d2813
```

O mesmo hash foi observado nas três execuções consecutivas.

## Testes

Os testes EditMode cobrem:

- mesma seed produz resultado, eventos e hash idênticos;
- seeds diferentes alteram a variação observada;
- vida reduzida, acerto, miss, crítico, Defesa e dano mínimo;
- morte remove alvo da seleção e eliminação define o vencedor;
- draw por limite, desempate estável e ordem da Arqueira/Paladino;
- alvo morto nunca é selecionado;
- equipes/IDs/slots/seed/atributos inválidos;
- sequência sem lacunas, ticks ordenados e hash de 64 caracteres;
- snapshots de entrada permanecem inalterados;
- Paladino resiste mais ataques que Mago;
- os três heróis da demonstração e `BattleUnitFactory`.

Comandos de validação:

```powershell
& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -logFile $compileLog

& $unityEditor -batchmode -nographics -projectPath (Get-Location) `
  -runTests -testPlatform EditMode -testResults $results -logFile $testLog

& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -executeMethod IdleMedievalLegends.Editor.Combat.BattleDebugRunner.RunFromCommandLine `
  -logFile $battleLog
```

Resultados reais finais: compilação/importação com código 0; XML EditMode com
`124 total, 124 passed, 0 failed, 0 skipped`; runner de Editor com código 0 e o
mesmo hash nas três repetições. A validação estrutural do projeto terminou com
código 0 e apenas o aviso preexistente de `DefaultCompany`.

## Integração futura

Uma camada Application poderá montar `BattleRequest` a partir de snapshots
assinados pelo servidor e entregar `CombatEvent` a uma fila visual. Habilidades,
efeitos e IA devem entrar como novas políticas/eventos versionados, preservando
o motor puro e casos de replay das versões antigas.

## Limitações e não implementado

- apenas ataque básico; sem habilidades, mana, cooldown, buffs, cura, controle,
  reviver, elementos, formação avançada ou IA estratégica;
- não há animação, UI final, cena de batalha, rede, backend ou persistência;
- o hash é diagnóstico e não oferece autenticação;
- determinismo é garantido para esta implementação C# e versão de regras; uma
  implementação em outra linguagem deve reproduzir exatamente floats,
  arredondamento, RNG e ordenação;
- os números são sementes configuráveis e ainda exigem simulação de volume e
  telemetria.

# TASK 004 — Progressão, atributos e Poder dos heróis

Data: 2026-07-21

Branch: `feat/hero-progression-and-power`

Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo

Implementar um domínio puro, determinístico e independente de cena para a
instância, progressão, atributos derivados e métricas de Poder dos heróis. Os
resultados no cliente servem para apresentação e previsão; o servidor futuro
continua sendo autoridade sobre instâncias, moedas, fragmentos, equipamentos e
revisões persistidas.

## Arquitetura

`HeroInstance` é um snapshot serializável com ID imutável, ID da definição,
dono opcional, nível, XP, raridade, ascensão, fragmentos, referências de
equipamento, estado de desbloqueio, timestamp, revisão do servidor,
modificadores permanentes e cache de Poder. Campos persistidos são privados e
expostos como valores ou coleções somente leitura. Todas as transições em
`HeroProgressionRules` retornam uma nova instância, preservam identidade e
invalidam o cache quando alteram o cálculo.

`CombatBalanceTuning` centraliza curvas, limites, multiplicadores, custos e
parâmetros das métricas. A configuração passou da versão 2 para 3 por meio de
`CombatBalanceTuningMigration`; o asset existente foi migrado e salvo pela API
do Editor. Uma lista de overrides permite substituir pontos específicos das
curvas de XP e ouro sem manter uma tabela manual extensa.

Na migração, uma tabela v2 personalizada e não vazia de multiplicadores de
ascensão é preservada e define `maxAscensionLevel = Length - 1`. A nova tabela
de custos recebe o mesmo número de transições; custos ausentes usam a sequência
padrão e continuam por duplicação determinística quando a tabela antiga possui
mais níveis que o padrão.

`HeroPowerCalculator` recebe `HeroInstance`, `HeroDefinition`, tuning e bônus de
equipamento. `IHeroEquipmentModifierProvider` separa o cálculo do inventário
futuro; a implementação vazia é segura para fluxos sem equipamento. O cache
`CalculatedPower` nunca é lido pelas fórmulas.

`HeroPowerMetrics` agrega entradas já recalculadas. `HeroPowerService` resolve
a definição no `ContentCatalogLookup`, solicita os bônus ao provider e monta as
entradas de equipe/conta. Somas usam `checked`.

## Progressão e validações

- nível permitido: 1 até o máximo configurado (100 por padrão);
- XP por nível: `round(baseXp × nível^expoente)`, com base 100 e expoente 1,55;
- ouro previsto por nível: `round(baseGold × nível^expoente)`, com base 50 e
  expoente 1,25; a regra informa o custo, mas não debita carteira local;
- fragmentos de desbloqueio por raridade: 20, 30, 50, 80, 120 e 200;
- custos de ascensão 0→5: 20, 40, 80, 160 e 320;
- custos iniciais de promoção: 30, 50, 80, 120 e 200; Mítico não promove;
- ascensão permitida: 0 até 5;
- raridades aceitas: somente os valores persistidos `Common=0` a `Mythic=5`;
- ao combinar instância e definição, a raridade da instância não pode ser menor
  que a `InitialRarity` definida no catálogo;
- XP, fragmentos, timestamps, revisão e cache negativos são rejeitados;
- referências de equipamento vazias/duplicadas e modificadores permanentes com
  `sourceId` vazio/duplicado são rejeitados;
- IDs de instância duplicados são detectados antes da agregação;
- uma instância bloqueada não pode carregar nível, XP ou ascensão já aplicados.

Os custos são previsões determinísticas do domínio. Nenhuma operação concede
ou remove ouro/inventário como autoridade do cliente.

## Fórmulas

Para `x = L - 1`:

```text
M_nível(L) = 1 + 0,065x + 0,00035x²
```

Multiplicadores de raridade: `1,00; 1,08; 1,18; 1,31; 1,47; 1,66`.
Multiplicadores de ascensão: `1,00; 1,08; 1,18; 1,30; 1,44; 1,60`.

Vida, Ataque e Defesa fazem apenas um arredondamento, ao final:

```text
floor(((Base × M_nível × M_raridade × M_ascensão) + BônusFlat)
      × (1 + BônusPercentual))
```

Velocidade não recebe multiplicadores de progressão:

```text
clamp((VelocidadeBase + BônusFlat) × (1 + BônusPercentual), 60, 180)
```

As demais fórmulas são:

```text
K(L)              = 400 × M_nível(L)
ReduçãoDeDano     = clamp(Defesa / (Defesa + K(L)), 0, 0,75)
VidaEfetiva       = Vida / (1 - ReduçãoDeDano)
FatorVelocidade   = clamp((Velocidade / 100)^0,65, 0,75, 1,50)
ÍndiceOfensivo    = Ataque × FatorVelocidade
HeroPower         = round(3 × sqrt(VidaEfetiva × ÍndiceOfensivo))
```

Conversões finais para `long` e somas agregadas rejeitam overflow. O
arredondamento de Poder usa `MidpointRounding.AwayFromZero`.

## Métricas

- `HeroPower`: resultado recalculado de um único herói;
- `TeamPower`: soma somente os heróis desbloqueados da equipe ativa, limitada a
  cinco posições por padrão;
- `AccountPower`: soma de todos os heróis desbloqueados da conta, estejam ou não
  na equipe;
- `CompetitivePower`: `TeamPower + floor(15% × soma dos cinco reservas
  desbloqueados mais fortes)`;
- `SeasonPeakPower`: maior `CompetitivePower` observado na temporada; nunca
  diminui e rejeita regressão de revisão.

`CompetitivePower` não substitui `AccountPower`. O pico sazonal deve ser
persistido/confirmado pelo backend futuro para ser oficial.

## Conteúdo e ferramentas de demonstração

As definições da Task 003 permanecem com IDs estáveis:

- Paladino: 1400 Vida, 70 Ataque, 150 Defesa, 85 Velocidade — tanque;
- Arqueira: 900 Vida, 145 Ataque, 65 Defesa, 120 Velocidade — dano/velocidade e
  vida moderada;
- Mago: 850 Vida, 140 Ataque, 60 Defesa, 100 Velocidade — ataque alto, defesa
  baixa e velocidade média.

O catálogo foi regenerado por `ContentCatalogEditorTools`, sem edição manual de
YAML. `HeroPowerDebugComponent` é um componente opcional e somente de Editor:
recebe catálogo/tuning, parâmetros simulados e mostra base, multiplicadores,
bônus, atributos finais, redução, Vida Efetiva, fator, índice e Poder. O menu
**Balance > Upgrade Combat Balance Assets** persiste migrações de tuning.

## Testes

Os testes EditMode cobrem:

- multiplicadores dos níveis 1, 50 e 100, seis raridades e ascensões 0 a 5;
- limites de velocidade, teto de redução, Guerreiro-base e bônus de equipamento;
- cache ignorado/recalculado e provider externo de equipamentos;
- XP, custo de ouro, override de curva, desbloqueio, ascensão e promoção;
- níveis/fragmentos inválidos, referências duplicadas e IDs de instância;
- round-trip JsonUtility das listas de equipamentos e modificadores;
- round-trip e migração do JSON histórico de atributos e bônus de simulação;
- `TeamPower`, `AccountPower`, `CompetitivePower`, pico sazonal e overflow;
- perfis demonstrativos do Paladino, Arqueira e Mago;
- migração do tuning da versão 2 para 3.

Validação executada no Unity `6000.5.4f1`:

```powershell
& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -logFile $compileLog

& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -executeMethod IdleMedievalLegends.Editor.ContentCatalog.ContentCatalogEditorTools.GenerateDemoFromMenu `
  -logFile $catalogLog

& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -executeMethod IdleMedievalLegends.Editor.ContentCatalog.CombatBalanceEditorTools.UpgradeFromCommandLine `
  -logFile $balanceLog

& $unityEditor -batchmode -nographics -projectPath (Get-Location) `
  -runTests -testPlatform EditMode -testResults $results -logFile $testLog
```

A execução EditMode mais recente produziu XML com
`124 total, 124 passed, 0 failed, 0 skipped`. A tentativa anterior com `-quit`
encerrou antes do runner e não produziu XML; ela não foi contabilizada como
aprovação.

As validações de projeto, catálogo e Bootstrap também encerraram com código 0.
O catálogo reportou 3 heróis, 8 itens totais, 3 equipamentos, 5 materiais,
4 receitas, 0 erros e 0 avisos. O Bootstrap foi considerado válido. A validação
estrutural registrou somente o aviso preexistente de `DefaultCompany` em Player
Settings.

## Decisões

- reutilizar `Rarity` da Task 003, preservando os valores persistidos;
- snapshots imutáveis por API em vez de setters públicos em `HeroInstance`;
- representar modificadores serializáveis com `List<HeroPermanentModifier>`,
  sem `Dictionary` serializado pelo JsonUtility;
- manter velocidade fora dos multiplicadores de nível/raridade/ascensão, como
  especificado no GDD;
- usar `double` durante fórmulas e `long` para atributos inteiros e Poder;
- manter custos econômicos como resultado/previsão, sem autoridade local;
- separar a integração de equipamento por interface e não criar inventário.

## Limitações, riscos e próximos passos

- não há backend para confirmar custos, fragmentos, revisão ou pico sazonal;
- `ownerPlayerId` pode ser vazio em contextos sem dono, mas produção deverá
  exigir e validar o vínculo no servidor;
- curvas e custos são sementes configuráveis e ainda precisam de telemetria e
  balanceamento;
- não foram executados builds Android/iOS nem smoke tests em dispositivo;
- `HeroPowerDebugComponent` é diagnóstico, não UI final;
- não foram implementados batalha, habilidades, inventário, crafting runtime,
  gacha, mercado, backend ou progressão visual;
- nenhuma cena foi criada ou alterada para este sistema.

Próximo passo recomendado: uma task estreita para instâncias de equipamento e
resolução de afixos, implementando `IHeroEquipmentModifierProvider` sem tornar o
cliente autoridade econômica.

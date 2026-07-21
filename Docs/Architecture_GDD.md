# Idle Medieval Legends — Arquitetura e GDD de Balanceamento v2

Versão de referência: **v2 — Raridades e Profissões**  
Engine: **Unity / C#**  
Premissa de segurança: **cliente apresenta e solicita; servidor valida e grava**.

---

## 1. GDD simplificado de atributos

### 1.1 Objetivos de balanceamento

1. Heróis do mesmo nível, raridade e ascensão devem ficar dentro de uma faixa aproximada de **±12% de Poder**, antes de considerar habilidades especiais.
2. Vida e Ataque crescem de forma previsível; Defesa possui retorno decrescente; Velocidade é limitada e não escala diretamente com nível.
3. O Poder exibido deve acompanhar a capacidade real de combate e ser calculado com a mesma configuração no cliente e no servidor.
4. Buffs temporários de combate, bônus de evento e efeitos condicionais não entram no Poder persistente.
5. Todo arredondamento ocorre apenas no fim de cada etapa, para evitar diferenças entre telas e backend.

### 1.2 Valores-base sugeridos no nível 1

| Arquétipo | Vida | Ataque | Defesa | Velocidade | Poder aproximado |
|---|---:|---:|---:|---:|---:|
| Tanque | 1.400 | 70 | 150 | 85 | 1.045 |
| Guerreiro | 1.100 | 105 | 100 | 100 | 1.140 |
| Assassino | 800 | 145 | 65 | 120 | 1.169 |
| Mago | 850 | 140 | 60 | 100 | 1.110 |
| Suporte | 1.100 | 90 | 90 | 110 | 1.078 |

Esses números são sementes de balanceamento, não conteúdo final. Habilidades de cura, controle, invulnerabilidade, execução e reviver devem ser validadas por simulação de combate e, posteriormente, por telemetria.

### 1.3 Multiplicador de nível

Para nível `L` entre 1 e 100:

```text
M_nivel(L) = 1 + 0,065 × (L - 1) + 0,00035 × (L - 1)²
```

Referências:

| Nível | Multiplicador |
|---:|---:|
| 1 | 1,0000 |
| 50 | 5,02535 |
| 100 | 10,86535 |

A curva quadrática moderada cria aceleração perceptível sem depender de exponenciais difíceis de controlar no LiveOps.

### 1.4 Multiplicadores de raridade

O projeto utiliza seis raridades, em ordem crescente:

| Raridade | ID persistido | Multiplicador de atributos do herói | Função de design |
|---|---:|---:|---|
| Comum | 0 | 1,00 | Base acessível e material de progressão |
| Incomum | 1 | 1,08 | Primeiro salto perceptível de qualidade |
| Raro | 2 | 1,18 | Núcleo da coleção de médio prazo |
| Épico | 3 | 1,31 | Conteúdo de alto valor e kits mais especializados |
| Lendário | 4 | 1,47 | Objetivo de longo prazo |
| Mítico | 5 | 1,66 | Topo de coleção, extremamente escasso |

Esses multiplicadores afetam Vida, Ataque e Defesa do herói. Velocidade continua fora do multiplicador geral, porque pequenas variações nela têm impacto desproporcional na economia de turnos. A raridade Mítica não deve receber, além disso, habilidades objetivamente dominantes; o kit deve criar opções estratégicas, não invalidar todas as raridades anteriores.

**Migração obrigatória:** a v1 usava `Common=0`, `Rare=1`, `Epic=2`, `Legendary=3`. A v2 usa `Common=0`, `Uncommon=1`, `Rare=2`, `Epic=3`, `Legendary=4`, `Mythic=5`. Dados numéricos antigos precisam ser remapeados. Novos backends devem persistir IDs textuais estáveis (`common`, `uncommon`, etc.) ou registrar explicitamente a versão do schema.

### 1.5 Multiplicadores de ascensão

| Ascensão | Multiplicador |
|---:|---:|
| 0 | 1,00 |
| 1 | 1,08 |
| 2 | 1,18 |
| 3 | 1,30 |
| 4 | 1,44 |
| 5 | 1,60 |

O custo em fragmentos deve crescer mais rapidamente que o ganho de poder, para impedir que as últimas ascensões se tornem a única decisão economicamente válida.

### 1.6 Fórmula dos atributos finais

Para Vida, Ataque e Defesa:

```text
AtributoFinal = floor(
    (AtributoBase × M_nivel × M_raridade × M_ascensão + BônusFlat)
    × (1 + BônusPercentual)
)
```

Ordem obrigatória:

1. Base do herói.
2. Progressão de nível, raridade e ascensão.
3. Bônus planos de equipamento/permanentes.
4. Bônus percentuais.
5. Arredondamento para baixo.

Para Velocidade:

```text
VelocidadeFinal = clamp(
    (VelocidadeBase + BônusFlatVelocidade) × (1 + BônusPercentualVelocidade),
    60,
    180
)
```

Velocidade não recebe o multiplicador geral de nível. Caso recebesse, heróis de late game poderiam executar ações em uma frequência difícil de animar, balancear e sincronizar.

### 1.7 Mitigação por Defesa

```text
K(L) = 400 × M_nivel(L)

ReduçãoDeDano = clamp(
    Defesa / (Defesa + K(L)),
    0,
    0,75
)

DanoRecebido = DanoBruto × (1 - ReduçãoDeDano)
```

Características:

- Defesa nunca gera imunidade completa.
- O parâmetro `K(L)` cresce com o nível, preservando a relevância relativa da Defesa.
- O teto inicial de 75% impede combinações invulneráveis.

### 1.8 Fator de Velocidade para o Poder

```text
F_velocidade = clamp(
    (Velocidade / 100)^0,65,
    0,75,
    1,50
)
```

O expoente menor que 1 aplica retorno decrescente. A fórmula real de ataque da batalha deve permanecer coerente com essa aproximação; caso Velocidade reduza recargas, conjurações e ataques simultaneamente, seu peso deverá ser recalibrado.

### 1.9 Poder do herói

```text
VidaEfetiva = Vida / (1 - ReduçãoDeDano)
ÍndiceOfensivo = Ataque × F_velocidade

PoderHeroi = round(
    3 × sqrt(VidaEfetiva × ÍndiceOfensivo)
)
```

Por que média geométrica:

- Evita que um único atributo inflado domine toda a pontuação.
- Valoriza combinações coerentes de sobrevivência e dano.
- Faz o poder crescer aproximadamente na mesma proporção dos atributos quando todos escalam juntos.

Referência usando o Guerreiro-base:

| Configuração | Poder aproximado |
|---|---:|
| Nível 1, Comum, Ascensão 0 | 1.140 |
| Nível 50, Comum, Ascensão 0 | 5.724 |
| Nível 100, Comum, Ascensão 0 | 12.380 |
| Nível 100, Lendário, Ascensão 5 | 32.830 |
| Nível 100, Mítico, Ascensão 5 | 37.951 |

### 1.10 Poder total e poder competitivo

A definição literal do escopo é:

```text
PoderTotalConta = soma(PoderHeroi de todos os heróis desbloqueados)
```

Ela funciona para prestígio, conquistas e desbloqueios. Para o Coliseu, porém, somar toda a coleção cria três incentivos ruins:

- evitar desbloquear heróis fracos;
- deixar a reserva sem evolução;
- parar imediatamente antes do limite de uma liga.

Recomendação de produção:

```text
PoderEquipe = soma dos 5 heróis da defesa
PoderCompetitivoAtual = PoderEquipe + 0,15 × soma dos próximos 5 heróis mais fortes
PoderDeLiga = max(PicoSazonalDePoder, PoderCompetitivoAtual)
```

Caso seja obrigatório usar a soma de toda a conta, ainda aplique:

```text
PoderDeLiga = max(PicoSazonalDePoderTotal, PoderTotalContaAtual)
```

Assim, remover equipamentos ou desmontar a equipe não permite voltar a uma faixa inferior durante a temporada.

### 1.11 Regras de liga

- Promoção imediata ao ultrapassar o limite.
- Sem rebaixamento durante a temporada; reavaliação somente no reset.
- Dentro de cada faixa de poder, usar uma nota competitiva separada, como Elo/Glicko simplificado.
- Defesa é um snapshot assinado pelo servidor; alterações posteriores não mudam batalhas já iniciadas.
- Recompensas devem crescer continuamente entre ligas para reduzir o desejo de “acampar” abaixo de um limite.

### 1.12 Raridades no gacha por fragmentos

Distribuição inicial sugerida por invocação; os números são hipóteses para simulação econômica e testes, não valores imutáveis:

| Raridade do fragmento | Probabilidade-base | Fragmentos sugeridos para desbloqueio |
|---|---:|---:|
| Comum | 42,0% | 20 |
| Incomum | 30,0% | 30 |
| Raro | 17,0% | 50 |
| Épico | 8,0% | 80 |
| Lendário | 2,5% | 120 |
| Mítico | 0,5% | 200 |

Pities independentes e persistidos no servidor:

- a cada 10 invocações, pelo menos Raro;
- a cada 30, pelo menos Épico;
- Lendário com soft pity a partir da 61ª e hard pity na 80ª;
- Mítico com soft pity a partir da 181ª e hard pity na 240ª;
- obter um Mítico também satisfaz o pity Lendário, mas obter um Lendário não zera o pity Mítico;
- o pity deve acompanhar a família do banner e sobreviver à troca entre banners equivalentes;
- o cliente nunca escolhe raridade, seed, fragmento ou contador final.

A quantidade de fragmentos por pull pode variar por raridade, mas o valor esperado precisa ser modelado junto do custo de desbloqueio e ascensão. Odds, pity e versão da tabela devem ser exibidos de maneira clara na interface.

---

## 2. Sistema de profissões, crafting e economia P2P

### 2.1 Objetivo econômico

O sistema deve permitir dois estilos sem criar um bloqueio artificial:

1. **Jogador solo:** pode aprender e evoluir Ferreiro, Costureiro, Encantador, Alquimista e Coletador até o Tier 9. Nenhum equipamento funcional depende obrigatoriamente de outro jogador.
2. **Jogador comerciante/especialista:** progride mais rápido em uma profissão principal, produz com melhor eficiência e transforma tempo, receitas e qualidade em valor no mercado.

A utilidade do mercado vem de custo de oportunidade, capacidade diária, especialização, receitas descobertas e qualidade do resultado — não de proibir o jogador de aprender outras profissões. Essa abordagem é chamada de **especialização suave**.

### 2.2 Dois eixos independentes: Tier e raridade

- **Tier 1 a Tier 9:** representa geração material/tecnológica, dificuldade da região e orçamento-base do item.
- **Comum a Mítico:** representa qualidade dentro do mesmo Tier, quantidade de afixos, teto de aprimoramento e valor de mercado.

Um item T6 Comum pode ter orçamento-base superior ao de um T5 Lendário; a interface sempre deve exibir os dois eixos. Para equipamentos:

```text
M_tier(T) = 1,45^(T - 1)

OrçamentoDoItem =
    OrçamentoBaseDoSlot
    × M_tier
    × M_raridadeEquipamento
    × (1 + 0,035 × NívelDeAprimoramento)
```

| Tier | Multiplicador de orçamento | Faixa profissional |
|---:|---:|---|
| T1 | 1,000 | Aprendiz |
| T2 | 1,450 | Aprendiz |
| T3 | 2,103 | Proficiente |
| T4 | 3,049 | Proficiente |
| T5 | 4,421 | Mestre |
| T6 | 6,410 | Mestre |
| T7 | 9,294 | Grão-Mestre |
| T8 | 13,476 | Grão-Mestre |
| T9 | 19,541 | Deus |

A raridade de equipamento usa multiplicadores menores que a raridade do herói para evitar multiplicação excessiva de vantagem:

| Raridade | Multiplicador de orçamento | Afixos | Aprimoramento máximo |
|---|---:|---:|---:|
| Comum | 1,00 | 0 | +3 |
| Incomum | 1,06 | 1 | +5 |
| Raro | 1,14 | 2 | +7 |
| Épico | 1,24 | 3 | +10 |
| Lendário | 1,36 | 4 | +13 |
| Mítico | 1,50 | 5 | +15 |

Afixos devem consumir um orçamento total; não se deve somar cinco afixos completos a um item Mítico. O orçamento é dividido entre atributo principal e afixos secundários, mantendo o Poder calculado coerente.

### 2.3 Graus, níveis e desbloqueio dos nove Tiers

Cada profissão possui nível 1 a 100. O grau é derivado do nível, e os Tiers são liberados em marcos explícitos:

| Grau | Níveis | Tiers liberados | Marcos |
|---|---:|---|---|
| Aprendiz | 1–19 | T1–T2 | T1 no nível 1; T2 no 10 |
| Proficiente | 20–39 | T3–T4 | T3 no 20; T4 no 30 |
| Mestre | 40–63 | T5–T6 | T5 no 40; T6 no 52 |
| Grão-Mestre | 64–89 | T7–T8 | T7 no 64; T8 no 76 |
| Deus | 90–100 | T9 | T9 no 90; nível 100 maximiza domínio |

Além do nível, a estação correspondente precisa ter Tier suficiente. Um Ferreiro nível 64 com Forja T5 conhece T7, mas não inicia receita T7 até melhorar a Forja. Isso cria um sink de ouro e materiais sem apagar progresso.

XP sugerido para avançar do nível `L` ao próximo:

```text
XP_proximo(L) = round(100 × L^1,55)
```

Modificadores:

- profissão principal: +20% XP;
- primeira criação de uma receita: +100% XP naquela execução;
- receita no maior Tier liberado: 100% XP;
- um Tier abaixo: 65%; dois abaixo: 25%; três ou mais: 5%.

A penalidade impede maximizar uma profissão fabricando milhares de itens T1 baratos, mas não impede produzir itens antigos para o mercado.

### 2.4 Profissão principal sem bloquear o jogo solo

O jogador pode evoluir todas as profissões. Uma delas pode ser marcada como **profissão principal** e recebe:

- +20% de XP profissional;
- redução inicial de 15% no tempo de produção;
- +5 pontos no cálculo de qualidade;
- acesso a linhas de receitas de assinatura e cosméticos da profissão;
- um slot adicional de fila daquela profissão no grau Mestre.

A profissão principal não ignora nível, grau, Tier, estação ou diagrama. A troca de especialidade deve usar cooldown de sete dias e custo alto em ouro, não Gemas, para evitar vantagem competitiva comprada diretamente.

### 2.5 Responsabilidade de cada profissão

| Profissão | Refina | Produz | Principais compradores |
|---|---|---|---|
| Coletador | lotes brutos selecionados, peles tratadas inicialmente, ervas secas, cristais separados | expedições, mapas de recurso, caixas de matéria-prima | todas as profissões |
| Ferreiro | minérios em lingotes, rebites, lâminas e chapas | armas físicas, escudos, armaduras pesadas, ferramentas profissionais | guerreiros, tanques, demais artesãos |
| Costureiro | fibras em tecido, peles em couro, fios e enchimentos | armaduras leves/médias, mantos, capuzes, bolsas e componentes de empunhadura | magos, arqueiros, assassinos, ferreiros |
| Encantador | cristais e essências em pó, núcleos e runas | encantamentos, pedras de soquete, selos de reroll, desmontagem arcana | todos os usuários de equipamento |
| Alquimista | ervas e partes de monstros em extratos, óleos e solventes | poções, elixires, buffs, fluxos de forja, tinturas mágicas e catalisadores | combate e todas as profissões |

Famílias temáticas de material podem seguir: Ferro/Linho/Erva Selvagem/Cristal Opaco em T1; Aço e Lã em T2; Prataço e Tecido Rúnico em T3; Mithril em T4; Adamantita em T5; Aço Dracônico em T6; materiais Astrais em T7; Celestiais em T8; e Divinos em T9. Os nomes são conteúdo, enquanto Tier e tags são regras estáveis.

### 2.6 Matriz macro de conteúdo T1–T9 por profissão

A tabela abaixo define famílias de conteúdo, não IDs finais de catálogo. Cada linha representa o que o jogador passa a **coletar, refinar e fabricar** ao liberar um Tier. Receitas individuais continuam versionadas no backend.

| Tier | Tema material | Coletador | Ferreiro | Costureiro | Encantador | Alquimista |
|---:|---|---|---|---|---|---|
| T1 | Ferro, linho e ervas selvagens | expedições curtas; minério de ferro, fibras, couro cru, ervas e cristal opaco | lingotes de ferro, ferramentas simples, armas e armaduras pesadas básicas | fios de linho, couro simples, vestes leves, luvas e bolsas pequenas | pó arcano opaco, runas menores e desencanto de itens Comuns | poções menores, óleo de lâmina, solvente e fluxo de forja simples |
| T2 | Aço, lã e âmbar | rotas regionais, carvão, lã, peles tratáveis, raiz âmbar e cristais claros | aço, rebites reforçados, escudos, armas e conjuntos pesados T2 | tecido de lã, couro reforçado, capas, conjuntos médios e bolsas T2 | runas claras, primeiro soquete, selos de proteção e desencanto Incomum | tônicos, antídotos, óleo reforçado, fluxo T2 e bombas utilitárias |
| T3 | Prataço e tecido rúnico | mapas de recurso, prataço bruto, folha-da-lua e cristais arcanos | lingotes de prataço, armas especializadas, componentes de fecho e equipamento T3 | tecido rúnico, empunhaduras de couro, mantos e equipamento T3 | núcleos rúnicos, runas de atributo e pedras de soquete T3 | elixires estabilizadores, tinturas de mana e catalisadores T3 |
| T4 | Mithril e seda lunar | expedições de risco, mithril, seda lunar, ervas estelares e prismas | ligas de mithril, armas de precisão, armaduras pesadas e ferramentas T4 | seda lunar, couro sombrio, conjuntos leves/médios e componentes de arco | runas prismáticas, selos de reroll limitado e encantamentos de conjunto | poções superiores, óleos elementais, reagentes de reroll e fluxo T4 |
| T5 | Adamantita e materiais ancestrais | contratos de coleta, adamantita, couro de wyvern, seda ancestral e flor régia | placas de adamantita, armas épicas, escudos de função e ferramentas mestras | couro de wyvern, seda ancestral, bolsas de expedição e equipamentos épicos | núcleos antigos, encantamentos duplos e extração de essência Épica | elixires de raid, catalisadores de qualidade, resinas e solventes T5 |
| T6 | Aço dracônico e essência de fênix | caçadas de elite, minério dracônico, fibras de fênix e glândulas raras | aço dracônico, armas de chefe, armaduras pesadas e moldes avançados | couro dracônico, tecido de fênix, capas reativas e equipamentos T6 | runas dracônicas, soquetes avançados e selos de preservação de afixo | sangue alquímico, elixires de resistência, têmpera dracônica e catalisadores T6 |
| T7 | Astral e Vazio | expedições planares, minério astral, seda do Vazio, ervas etéreas e cristais siderais | liga astral, armas com identidade de build, armaduras e ferramentas de grão-mestre | trama do Vazio, couro etéreo, conjuntos especializados e mantos astrais | palavras rúnicas, encantamentos condicionais e núcleos astrais | elixires astrais, transmutação controlada e catalisadores de alta qualidade |
| T8 | Celestial e serafim | portais celestiais, minério celeste, fio serafim, lótus solar e cristal puro | liga celestial, equipamentos Lendários, componentes de relíquia e ferramentas T8 | trama serafim, couro celestial, conjuntos Lendários e cosméticos raros | selos celestiais, runas de conjunto e bloqueio parcial de afixo | elixires de ápice, essências celestes, fluxo perfeito e catalisadores T8 |
| T9 | Divino, destino e criação | expedições míticas, minério divino, fio do destino, lótus primordial e cristal do mundo | equipamentos Divinos T9, armas de assinatura e ferramentas Deus | vestes do destino, armaduras T9 e cosméticos de assinatura | runas divinas, palavras de poder, pedras míticas e selos de legado | Catalisador Divino, elixires primordiais e reagentes para crafting Mítico |

Regras para essa matriz:

- a profissão que aparece na coluna produz o componente com maior eficiência, mas o jogador pode evoluir todas as colunas na mesma conta;
- receitas T1–T2 ensinam a cadeia com baixa dependência; do T3 em diante, componentes cruzados criam demanda de mercado;
- cada família deve possuir pelo menos uma receita de refino, uma de uso próprio e uma saída negociável;
- equipamentos de assinatura no grau Deus não devem monopolizar o melhor poder: a exclusividade principal é visual, de eficiência ou de combinação de afixos;
- o arquivo `Examples/profession_recipe_families_t1_t9.example.csv` transforma esta matriz em linhas importáveis por ferramentas de conteúdo.

### 2.7 Maestria e identidade do artesão

A partir do grau Mestre, crafts dos dois maiores Tiers liberados também geram **XP de Maestria**. Ao preencher uma barra, a profissão recebe um ponto, com máximo inicial de 30 pontos ativos. O jogador distribui esses pontos em três ramos e pode redefini-los com ouro e cooldown de 72 horas:

| Ramo | Benefícios permitidos | Limites de segurança econômica |
|---|---|---|
| Eficiência | redução moderada de duração, chance de economizar material comum, tamanho de lote e recuperação de ferramenta | nunca reduz o custo de Foco abaixo de 50% nem duplica saídas únicas |
| Excelência | pontos de qualidade, melhor faixa de afixos e preservação parcial em reroll | não ignora o limite de raridade do Tier nem substitui Catalisador Divino/pity |
| Comércio | mais slots de comissão, histórico/reputação, menor taxa de anúncio em ouro e filtros avançados | não reduz a queima de 10% em Gemas e não permite autocompra |

Identidade sugerida por profissão no grau Deus:

- **Ferreiro:** escolhe uma escola de arma ou armadura para bônus de qualidade e cosméticos de assinatura;
- **Costureiro:** especializa um tipo de conjunto leve/médio e cria aparências raras;
- **Encantador:** domina uma família de palavras rúnicas, mantendo chances publicadas e auditáveis;
- **Alquimista:** melhora rendimento de lotes e duração de consumíveis sem vender buffs exclusivos de PvP;
- **Coletador:** escolhe biomas preferidos, reduz risco de expedição e encontra lotes de melhor qualidade.

Maestria aumenta eficiência e identidade, não cria conteúdo funcional impossível para quem joga solo. Bônus que alterem probabilidades devem aparecer na prévia da receita e ser resolvidos pelo servidor.

### 2.8 Dependências cruzadas progressivas

Para manter onboarding simples e criar mercado no mid/late game:

| Faixa | Dependência recomendada |
|---|---|
| T1–T2 | receita quase autossuficiente; no máximo componente opcional |
| T3–T4 | um componente obrigatório de outra profissão |
| T5–T6 | dois componentes cruzados |
| T7–T8 | dois ou três componentes, receitas/diagramas mais raros |
| T9 | três ou quatro componentes, mais Catalisador Divino para Mítico |

Exemplo de Espada T3: oito lingotes do Ferreiro, duas empunhaduras de couro do Costureiro e um núcleo rúnico do Encantador. O Alquimista fornece fluxo para aumentar rendimento no refino. O jogador solo consegue fabricar toda a cadeia, mas um especialista economiza dias de Foco e fila comprando componentes.

### 2.9 Foco artesanal, estações e filas

`Foco Artesanal` é uma capacidade compartilhada entre as cinco profissões:

- cap inicial: 100;
- recuperação contínua equivalente a 100 por dia;
- custos típicos por receita: T1=1, T2=1, T3=2, T4=3, T5=5, T6=8, T7=12, T8=18, T9=25;
- não pode ser comprado sem limite; eventos podem conceder pequenas quantidades vinculadas à conta;
- o servidor calcula regeneração usando seu próprio relógio.

Cada profissão tem uma estação T1–T9: Forja, Ateliê, Mesa Arcana, Laboratório e Acampamento de Expedição. Há duas filas-base compartilhadas; profissão principal Mestre recebe uma fila exclusiva adicional. Materiais são reservados ao iniciar o job e não podem ser vendidos, equipados, divididos ou consumidos até conclusão/cancelamento autoritativo.

### 2.10 Qualidade e rolagem de raridade

Crafting funcional nunca falha completamente. A receita sempre produz sua saída-base; habilidade altera a raridade. O backend calcula uma faixa de qualidade a partir de margem de nível, estação excedente, ferramenta, catalisador e profissão principal. Pesos iniciais em porcentagem:

| Faixa | Comum | Incomum | Raro | Épico | Lendário | Mítico |
|---|---:|---:|---:|---:|---:|---:|
| Padrão | 60,0 | 28,0 | 10,0 | 2,0 | 0 | 0 |
| Habilidoso | 40,0 | 34,0 | 19,0 | 6,5 | 0,5 | 0 |
| Especialista | 22,0 | 30,0 | 30,0 | 15,5 | 2,5 | 0 |
| Dominado | 8,0 | 18,0 | 33,0 | 32,0 | 8,5 | 0,5 |
| Divino | 2,0 | 8,0 | 26,0 | 35,0 | 28,0 | 1,0 |

Limites de raridade por Tier:

- T1–T2: no máximo Raro;
- T3–T4: no máximo Épico;
- T5–T8: no máximo Lendário;
- T9: Mítico somente para grau Deus com Catalisador Divino.

Pesos acima do limite são incorporados à maior raridade permitida. RNG, seed e resultado são exclusivamente do servidor. O cliente pode exibir probabilidades previstas, mas não envia `qualityScore` nem raridade desejada.

### 2.11 Pity de crafting Mítico

Cada profissão mantém um contador separado para crafts T9 elegíveis a Mítico:

- somente jobs T9, grau Deus e Catalisador Divino incrementam o contador;
- soft pity após 50 falhas: +0,05 ponto percentual por tentativa;
- hard pity na 100ª tentativa elegível;
- obter Mítico zera apenas o contador daquela profissão;
- contador, seed, catálogo e versão de balanceamento ficam no servidor.

O pity evita que um artesão altamente investido tenha uma sequência indefinida de azar e também cria demanda consistente por componentes T9.

### 2.12 Binding, reciclagem e sinks

- equipamento craftado nasce `UNBOUND` e pode ser anunciado;
- no primeiro uso/equipamento, vira `ACCOUNT_BOUND`;
- encantamento de alto nível também pode vincular o item;
- desmontar item vinculado devolve materiais vinculados, evitando lavagem de valor;
- aprimoramento consome ouro, material refinado e, nos níveis altos, duplicatas/essências;
- falha de aprimoramento não destrói o item, mas pode consumir materiais;
- diagramas normalmente são aprendidos e consumidos uma vez; duplicatas viram conhecimento/fragmentos de receita.

Essas regras retiram itens e materiais da circulação, limitam revenda infinita e mantêm demanda por produção nova.

### 2.13 Mercado: por que usar mesmo podendo jogar sozinho

O mercado oferece cinco ganhos que o solo não oferece simultaneamente:

1. economizar Foco e tempo de fila;
2. acessar receitas que outro jogador descobriu antes;
3. comprar uma raridade/rolagem específica em vez de aceitar RNG;
4. converter excedentes de uma profissão em componentes de outra;
5. contratar um especialista por comissão.

Categorias de busca: matéria-prima, material refinado, equipamento, consumível, encantamento, diagrama e ferramenta. Filtros obrigatórios: Tier, raridade, profissão, slot, atributo, binding e preço. A taxa de 10% em Gemas permanece; recomenda-se ainda pequena taxa de anúncio em ouro, não reembolsável, para evitar spam e criar sink de moeda soft.

### 2.14 Comissões de crafting

Além da venda de itens prontos, o mercado pode oferecer ordens de serviço:

1. comprador escolhe receita, quantidade e taxa de serviço;
2. ingredientes e Gemas são colocados em escrow;
3. um artesão elegível aceita;
4. o job usa habilidade, ferramenta e pity do artesão, mas a saída pertence ao comprador;
5. na conclusão, o artesão recebe a taxa líquida e 10% é queimado;
6. ambos recebem histórico auditável; o artesão recebe XP profissional.

Na primeira versão, o comprador aceita a tabela de probabilidade do artesão. Uma evolução posterior pode permitir “raridade mínima garantida” somente com selo/catalisador que transforme a garantia em custo econômico explícito.

### 2.15 Fluxo autoritativo de crafting

```text
StartCraft(recipeId, quantity, toolId, catalystId, requestId)
    ↓
validar profissão, nível, grau, Tier, estação, receita e Foco
    ↓
reservar exatamente os stacks/instâncias de ingrediente
    ↓
criar craft_job com catálogo e config versionados
    ↓ tempo do servidor
finalizar job de forma idempotente
    ↓
consumir reservas uma única vez
    ↓
rolar raridade/afixos no servidor
    ↓
criar item_instance + item_provenance + XP + pity + outbox
```

A chave lógica `(job_id, output_index)` deve ser única. Se um worker repetir a finalização após falha, ele encontra a saída já criada em vez de cunhar uma segunda cópia.

### 2.16 Métricas de economia

Monitorar por Tier e profissão:

- produção, consumo e estoque mediano de cada material;
- dias de oferta no mercado;
- preço mediano e dispersão por Tier/raridade;
- tempo até venda e taxa de anúncios expirados;
- Foco gerado versus gasto;
- participação de jogadores solo versus compradores/vendedores;
- concentração de receita nos 1%, 5% e 10% maiores vendedores;
- Gemas transferidas, queimadas e originadas por compra real;
- taxa de binding, desmontagem e aprimoramento;
- crafts por faixa de qualidade e distância até o pity Mítico.

Alertas sugeridos: inflação de preço acima de 20% semanal sem mudança de conteúdo, estoque mediano acima de 30 dias, profissão com menos de 10% da demanda das demais, ou mais de 40% das vendas concentradas no top 1%.

---

## 3. Arquitetura de salvamento, crafting e mercado

### 3.1 Regra central

O aplicativo Unity nunca pode ser autoridade para:

- saldo de gemas;
- propriedade ou criação de itens;
- resultado de gacha;
- recompensas offline;
- estado de anúncio de mercado;
- cálculo oficial de poder;
- relógio usado para energia, produção ou expiração.

O cliente envia apenas intenção:

```text
CreateListing(itemInstanceId, priceGems, requestId)
BuyListing(listingId, requestId)
ClaimOfflineRewards(requestId)
StartCraft(recipeId, quantity, toolId, catalystId, requestId)
CancelCraftJob(jobId, requestId)
SelectPrimaryProfession(professionId, requestId)
UpgradeProfessionStation(professionId, requestId)
CreateCraftingCommission(recipeId, quantity, serviceFeeGems, requestId)
```

O servidor autentica, carrega o estado atual, valida as regras, calcula o resultado e grava uma transação.

### 3.2 Fluxo de alto nível

```text
Unity Client
    ↓ autenticação + App/Device Attestation
Command API autoritativa
    ↓
Serviço de Domínio: Inventário / Mercado / Gacha / Crafting
    ↓
Banco transacional + Ledger imutável + Outbox
    ↓
Read models, notificações, analytics e reconciliação
```

### 3.3 Entidades recomendadas

#### `player_profiles`

| Campo | Tipo | Regra |
|---|---|---|
| `player_id` | string/UUID | PK |
| `account_power` | int64 | calculado pelo servidor |
| `season_peak_power` | int64 | nunca diminui na temporada |
| `balance_config_version` | int | versão usada no cálculo |
| `last_offline_claim_at` | timestamp | relógio do servidor |
| `revision` | int64 | controle de concorrência |

#### `hero_instances`

| Campo | Tipo | Regra |
|---|---|---|
| `hero_instance_id` | UUID | PK |
| `owner_player_id` | string | FK |
| `hero_definition_id` | string | catálogo |
| `level` | int | validado |
| `rarity` | enum | catálogo/progressão |
| `ascension` | int | validado por fragmentos |
| `equipped_item_ids` | array | somente referências válidas |
| `computed_power` | int64 | cache calculado no servidor |
| `version` | int64 | compare-and-swap |

#### `item_instances`

| Campo | Tipo | Regra |
|---|---|---|
| `item_instance_id` | UUIDv7/ULID | PK global e imutável |
| `definition_id` | string | item do catálogo |
| `owner_player_id` | string | proprietário atual |
| `kind` | enum | material, refinado, equipamento, consumível etc. |
| `tier` | 1..9 | geração do item |
| `rarity` | enum/string | Comum a Mítico |
| `quantity` | int64 | 1 para equipamento único |
| `state` | enum | `OWNED`, `EQUIPPED`, `ESCROW`, `RESERVED`, `CONSUMED`, `DESTROYED` |
| `binding` | enum | `UNBOUND`, `ACCOUNT`, `HERO` |
| `listing_id` | nullable | obrigatório em `ESCROW` |
| `reservation_id` | nullable | obrigatório em `RESERVED`; aponta para job/comissão |
| `source_profession` | enum | profissão que originou o item |
| `recipe_id` | nullable | receita usada |
| `crafted_by_player_id` | nullable | artesão |
| `origin_transaction_id` | string | job/drop/transação que criou a instância |
| `parent_instance_id` | nullable | auditoria de split/merge de stacks |
| `quality_score_bps` | int | 0..10.000 |
| `enhancement_level` | int | validado pela raridade |
| `roll_seed_hash` | string | auditoria sem revelar a seed |
| `rolled_stats` | JSON | valores já materializados |
| `version` | int64 | concorrência otimista |
| `created_at`, `updated_at` | timestamp | servidor |

Invariantes:

- um `item_instance_id` existe em apenas um documento/linha;
- um item só possui um proprietário lógico;
- item em `ESCROW` não pode ser equipado, consumido, desmontado ou anunciado novamente;
- item em `RESERVED` pertence a exatamente um job/comissão e não pode ser negociado;
- split/merge de stack cria histórico de proveniência e conserva a soma das quantidades;
- item `DESTROYED` nunca volta ao inventário.

#### `wallets`

| Campo | Tipo | Regra |
|---|---|---|
| `player_id` | string | PK |
| `gems_available` | int64 | nunca negativo |
| `gems_held` | int64 | reservas temporárias |
| `gold` | int64 | nunca negativo |
| `revision` | int64 | concorrência |

É recomendável separar internamente as origens de gemas: compradas, ganhas, promocionais e recebidas do mercado. Isso facilita chargebacks, análise de fraude e políticas de gasto.

#### `market_listings`

| Campo | Tipo | Regra |
|---|---|---|
| `listing_id` | UUID | PK |
| `item_instance_id` | UUID | único enquanto ativo/reservado |
| `seller_player_id` | string | proprietário original |
| `buyer_player_id` | nullable | preenchido na venda |
| `price_gems` | int64 | inteiro; mínimo sugerido 10 |
| `fee_basis_points` | int | 1.000 = 10% |
| `fee_gems` | int64 | calculado no servidor |
| `seller_net_gems` | int64 | preço - taxa |
| `status` | enum | `PENDING`, `ACTIVE`, `RESERVED`, `SOLD`, `CANCELLED`, `EXPIRED`, `FAILED` |
| `expires_at` | timestamp | servidor |
| `version` | int64 | concorrência |

No Firestore, use `activeMarketListings/{item_instance_id}` como trava/read model do anúncio atual e mantenha o histórico completo em `marketListings/{listing_id}`. Em SQL, use índice único parcial para estados ativos.

#### `wallet_ledger`

Ledger append-only:

| Campo | Tipo |
|---|---|
| `entry_id` | UUID |
| `transaction_id` | UUID |
| `player_id` | string ou `SYSTEM_BURN` |
| `currency_id` | string |
| `delta` | int64 assinado |
| `reason` | enum |
| `counterparty_id` | nullable |
| `balance_after` | int64 |
| `request_id` | string |
| `created_at` | timestamp |

Nunca editar nem apagar entradas. Correções devem ser novas entradas compensatórias.

#### `command_deduplication`

| Campo | Tipo | Regra |
|---|---|---|
| `scope_key` | `playerId:requestId` | PK |
| `command_type` | string | valida reuso indevido |
| `status` | enum | `PROCESSING`, `COMPLETED`, `FAILED_RETRYABLE` |
| `result_json` | JSON | retorna o mesmo resultado em retries |
| `expires_at` | timestamp | retenção configurável |

#### `profession_progress`

| Campo | Tipo | Regra |
|---|---|---|
| `player_id`, `profession_id` | chave composta | uma linha por profissão |
| `level` | int | 1..100 |
| `total_experience` | int64 | nunca diminui, salvo correção auditada |
| `rank` | enum | cache derivado do nível |
| `max_unlocked_tier` | int | cache derivado |
| `station_tier` | int | 1..9 |
| `crafts_completed` | int64 | telemetria/maestria |
| `mastery_points` | int | progressão de receita |
| `mythic_pity_counter` | int | por profissão |
| `version` | int64 | concorrência |

#### `recipe_unlocks`

Chave única `(player_id, recipe_id)`, contendo fonte do desbloqueio, diagrama consumido, timestamp, temporada e versão. Uma repetição do mesmo comando devolve o desbloqueio anterior e não consome outro diagrama.

#### `craft_jobs` e `craft_transactions`

`craft_jobs` mantém `job_id`, jogador, receita, quantidade, estado, `reservation_id`, catálogo/config versionados, ferramenta, catalisador, início, conclusão e IDs de saída. `craft_transactions` é o registro imutável da finalização. Deve existir unicidade em `(job_id, output_index)`.

#### `item_provenance`

Registro append-only ligando cada saída ao job, receita, artesão, versões de catálogo/balanceamento, entradas consumidas, ferramenta, catalisador e hash da seed. É a principal evidência contra duplicação, rollback seletivo e criação administrativa não auditada.

#### `crafting_commissions`

| Campo | Tipo | Regra |
|---|---|---|
| `commission_id` | UUID | PK |
| `buyer_player_id` | string | dono dos materiais/saída |
| `crafter_player_id` | nullable | preenchido ao aceitar |
| `recipe_id`, `quantity` | dados da ordem | imutáveis após abertura |
| `ingredient_reservation_id` | string | escrow dos materiais |
| `service_fee_gems` | int64 | mantida em `gems_held` |
| `fee_bps` | int | 1.000 |
| `status` | enum | `OPEN`, `ACCEPTED`, `CRAFTING`, `COMPLETED`, `CANCELLED`, `EXPIRED`, `FAILED` |
| `job_id` | nullable | job criado pelo aceite |
| `expires_at`, `version` | servidor | concorrência |

### 3.4 Taxa de mercado sem ponto flutuante

```text
feeBps = 1000
fee = ceil(price × feeBps / 10000)
sellerNet = price - fee
```

Exemplos:

| Preço | Taxa | Líquido do vendedor |
|---:|---:|---:|
| 10 | 1 | 9 |
| 99 | 10 | 89 |
| 100 | 10 | 90 |
| 1.001 | 101 | 900 |

A gema queimada gera uma entrada para `SYSTEM_BURN`, mas não é creditada em uma carteira gastável.

### 3.5 Criação de anúncio

Transação autoritativa:

1. Verificar `requestId`; devolver resultado anterior se já processado.
2. Ler item e anúncio ativo por `item_instance_id`.
3. Validar proprietário, `UNBOUND`, `AVAILABLE`, não equipado e quantidade válida.
4. Validar preço mínimo/máximo e limites de anúncios do vendedor.
5. Criar anúncio `ACTIVE`.
6. Alterar item para `ESCROW`, vinculando `listing_id` e incrementando `version`.
7. Escrever evento de auditoria/outbox.
8. Confirmar tudo ou nada.

### 3.6 Compra de anúncio

Uma única transação lógica deve:

1. Deduplicar `requestId`.
2. Ler anúncio, item, carteira do comprador e carteira do vendedor.
3. Confirmar `ACTIVE`, não expirado e comprador diferente do vendedor.
4. Confirmar item em `ESCROW` com o mesmo `listing_id`.
5. Confirmar saldo suficiente.
6. Calcular taxa no servidor.
7. Debitar o preço integral do comprador.
8. Creditar o líquido ao vendedor.
9. Registrar a queima de 10%.
10. Transferir a propriedade do item ao comprador e voltar seu estado para `OWNED`.
11. Marcar anúncio como `SOLD`.
12. Criar transação, ledger e outbox.
13. Confirmar tudo ou nada.

Push notification, e-mail e analytics não devem ocorrer dentro da função transacional, porque ela pode ser repetida. A transação cria um evento na outbox; um worker o publica após o commit.

### 3.7 Cancelamento e expiração

- Cancelamento exige vendedor correto e anúncio `ACTIVE`.
- O item retorna de `ESCROW` para `OWNED` na mesma transação.
- Expiração usa um job servidor, idempotente, seguindo a mesma regra.
- Anúncio `RESERVED` não pode ser cancelado até concluir ou expirar a reserva.

### 3.8 Reconciliação automática

Executar periodicamente:

- item em `ESCROW` sem anúncio ativo;
- anúncio ativo cujo item não está em `ESCROW`;
- anúncio vendido sem transação correspondente;
- ledger cujo saldo derivado difere da carteira materializada;
- comando preso em `PROCESSING` além do timeout;
- transferência externa em estado pendente.

O reconciliador não deve “inventar” o resultado. Ele lê o histórico e aplica compensação determinística, registrando tudo no ledger.

### 3.9 Firebase/Firestore

Estrutura sugerida:

```text
/players/{playerId}
/players/{playerId}/heroes/{heroInstanceId}
/players/{playerId}/professions/{professionId}
/players/{playerId}/recipeUnlocks/{recipeId}
/players/{playerId}/craftJobs/{jobId}
/items/{itemInstanceId}
/itemProvenance/{itemInstanceId}
/wallets/{playerId}
/craftingCatalog/{recipeId}
/craftingCommissions/{commissionId}
/activeMarketListings/{itemInstanceId}
/marketListings/{listingId}
/marketTransactions/{transactionId}
/craftTransactions/{transactionId}
/commandDedup/{playerId_requestId}
/walletLedger/{entryId}
/craftingLedger/{entryId}
/outbox/{eventId}
```

As escritas de coleções autoritativas devem ser negadas ao cliente. Cloud Functions/Cloud Run com Admin SDK realizam as operações e usam IAM. App Check adiciona atestação do aplicativo/dispositivo, mas não substitui autenticação nem validações de domínio.

### 3.10 PlayFab

Uso recomendado:

- catálogo e definições: Economy v2 Catalog;
- itens do jogador: Inventory v2, com uma pilha exclusiva por equipamento craftado;
- `StackId` ou ID externo como referência de instância;
- coleção `default` para posse e coleção `market_escrow` para itens anunciados;
- `IdempotencyId`, ETag e histórico de transações em todas as operações suportadas;
- CloudScript/Azure Function para comandos autoritativos.

A liquidação P2P envolve pelo menos comprador, vendedor, item em escrow e queima. Operações atômicas de inventário de um único jogador/coleção não tornam automaticamente todo esse fluxo multi-entidade atômico. Há duas opções seguras:

1. **Recomendada:** ledger e mercado em banco ACID próprio; PlayFab permanece como identidade, catálogo, recibos e/ou espelho de inventário.
2. **PlayFab predominante:** saga durável com estados, idempotência, compensações e reconciliador. O usuário pode ver `PENDING`, mas nunca um item duplicado.

### 3.11 Unity Cloud Save/Cloud Code

- Player Data `Protected` é apropriado para estado legível pelo jogador, mas gravável apenas pelo servidor.
- Game Data pode guardar anúncios/read models públicos, mantendo escrita do lado servidor.
- Cloud Code executa regras autoritativas.
- Write locks evitam sobrescrever uma versão concorrente.
- Para uma liquidação envolvendo várias entidades, ainda é necessário modelar transação/saga e reconciliação; um write lock isolado não substitui um ledger transacional.

### 3.12 JSON local de cache e migração v2

O cache local agora contém inventário e profissões, ambos descartáveis. Um exemplo completo está em `Examples/player_cache.example.json`. Estrutura resumida:

```json
{
  "schemaVersion": 2,
  "playerId": "player_01HXYZ",
  "inventory": {
    "schemaVersion": 2,
    "serverRevision": 1948,
    "items": [
      {
        "instanceId": "item_01HABC",
        "definitionId": "sword_silversteel_t3",
        "rarity": 3,
        "tier": 3,
        "sourceProfession": 1,
        "recipeId": "blacksmith_sword_t3",
        "originTransactionId": "craft_tx_01J001",
        "qualityScoreBasisPoints": 8610,
        "serverVersion": 8
      }
    ]
  },
  "professions": {
    "schemaVersion": 2,
    "serverRevision": 955,
    "primaryProfession": 1,
    "focusAvailable": 68,
    "focusCap": 100,
    "professions": [],
    "recipeUnlocks": [],
    "activeJobs": []
  }
}
```

O loader migra cache v1 para v2 adicionando uma progressão vazia de profissões. Entretanto, raridades numéricas de heróis persistidas fora desse arquivo precisam da migração explícita `0→0`, `1→2`, `2→3`, `3→4`. Não incluir segredos, chaves privadas ou HMAC compartilhado no cliente.

---

## 4. Organização dos scripts

```text
Assets/_Game/Scripts/
├── Application/
│   └── GameManager.cs
├── Config/
│   ├── CombatBalanceConfigAsset.cs
│   └── CraftingBalanceConfigAsset.cs
├── Domain/
│   ├── Common/
│   │   └── ProgressionTypes.cs
│   ├── Combat/
│   │   └── HeroPowerCalculator.cs
│   ├── Crafting/
│   │   ├── CraftingModels.cs
│   │   ├── CraftingRules.cs
│   │   ├── PlayerProfessions.cs
│   │   └── ProfessionProgression.cs
│   ├── Equipment/
│   │   └── EquipmentBudgetCalculator.cs
│   ├── Inventory/
│   │   ├── InventoryModels.cs
│   │   └── PlayerInventory.cs
│   └── Market/
│       └── MarketMath.cs
└── Infrastructure/
    ├── Backend/
    │   ├── CraftingCommandDtos.cs
    │   └── MarketCommandDtos.cs
    └── Save/
        ├── GameSaveData.cs
        ├── PlayerStateRepositoryBehaviour.cs
        └── LocalJsonPlayerStateRepository.cs
```

Princípios aplicados:

- `GameManager` coordena ciclo de vida; não contém regras de mercado ou combate.
- `HeroPowerCalculator`, `EquipmentBudgetCalculator`, `ProfessionProgression` e `CraftingRules` são puros e reutilizáveis no backend.
- `PlayerInventory` não oferece método público para criar/remover itens arbitrariamente; aplica snapshots do servidor.
- `PlayerProfessions` aplica XP, receitas, Foco e jobs somente por snapshot autoritativo.
- Tier e raridade usam enums compartilhados; migrações são obrigatórias quando IDs persistidos mudam.
- Persistência usa porta abstrata, permitindo trocar JSON local por PlayFab/Firebase.
- DTOs de mercado enviam intenção e `requestId`, nunca resultado calculado pelo cliente.
- A taxa usa inteiros e basis points.

### Instalação rápida

1. Copiar a pasta `Assets/_Game` para o projeto Unity.
2. Criar um GameObject `GameManager` na cena inicial.
3. Adicionar `GameManager` e `LocalJsonPlayerStateRepository` ao mesmo GameObject.
4. Arrastar o repositório para o campo `Cached State Repository`.
5. Criar os assets `Combat Balance Config` e `Crafting Balance Config` no menu de balanceamento.
6. Importar o catálogo exemplo apenas como referência; o catálogo oficial deve ser versionado no backend.
7. Substituir o repositório local por um adaptador de backend antes de qualquer teste econômico real.

### Testes mínimos obrigatórios

- dois pedidos simultâneos comprando o mesmo anúncio: somente um vence;
- retry do mesmo `requestId`: resultado idêntico, sem novo débito;
- queda após débito e antes da entrega: saga/reconciliador conclui ou compensa;
- vendedor tenta equipar/desmontar item em escrow: rejeitado;
- cliente altera JSON local e reinicia: servidor restaura o estado correto;
- relógio do aparelho avança uma semana: recompensa offline não muda;
- item duplicado no snapshot: cliente rejeita o snapshot;
- snapshot antigo chega depois de um novo: `serverRevision` impede regressão;
- preço 10, 99, 100 e 1.001: taxa inteira correta;
- jogador remove equipamento para cair de liga: pico sazonal impede rebaixamento;
- níveis 1, 10, 20, 40, 64 e 90 liberam os graus/Tiers esperados;
- duas finalizações concorrentes do mesmo `craft_job`: somente um conjunto de saídas é criado;
- ingrediente reservado para crafting não pode ser listado nem consumido;
- split/merge de stack conserva quantidade e proveniência;
- T9 sem grau Deus ou sem Catalisador Divino nunca produz Mítico;
- pesos de raridade sempre somam 10.000 basis points após aplicar o cap;
- profissão principal reduz duração sem ignorar nível, grau, Tier ou estação;
- a 51ª tentativa elegível recebe o primeiro bônus de soft pity e a 100ª é Mítica;
- retry de comissão não paga o artesão nem queima a taxa duas vezes;
- cache v1 abre como v2, mas o bootstrap do servidor substitui seus defaults.

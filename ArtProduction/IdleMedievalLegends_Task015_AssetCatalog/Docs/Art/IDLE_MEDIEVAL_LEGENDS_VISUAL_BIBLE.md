# Idle Medieval Legends — Visual Bible

**Documento:** Fonte oficial de verdade visual  
**Task:** 014 — Direção artística e Visual Bible  
**Versão:** 1.0  
**Status:** Direção-base aprovada para pré-produção  
**Projeto:** Idle Medieval Legends  
**Engine:** Unity 6, URP  
**Plataforma prioritária:** Android e iOS  
**Pipeline 3D previsto:** Meshy → revisão/ajuste → Unity  

> Esta Visual Bible define a linguagem visual que deve ser seguida por conceitos, modelos 3D, texturas, personagens, monstros, equipamentos, ambientes, estações de crafting, interface e materiais promocionais. Quando uma geração de IA divergir deste documento, este documento prevalece.

---

## 1. Objetivo

O projeto deve apresentar uma fantasia medieval heroica estilizada, legível em tela pequena e viável para produção em grande volume. O estilo precisa sustentar:

- batalhas idle com vários personagens simultâneos;
- heróis colecionáveis;
- progressão por nove Tiers;
- seis raridades;
- dezenas de famílias de equipamentos;
- cinco profissões de crafting;
- inimigos comuns, elites, mid-bosses e chefes;
- produção acelerada com IA sem perder consistência;
- desempenho adequado em dispositivos móveis intermediários.

A arte não deve competir com a leitura do gameplay. Silhueta, função e hierarquia visual têm prioridade sobre microdetalhes.

---

## 2. Declaração oficial do estilo

### 2.1 Nome da direção artística

**Idle Medieval Legends — Stylized Heroic Fantasy**

### 2.2 Descrição em português

Fantasia medieval heroica estilizada para mobile, com personagens de proporções levemente exageradas, silhuetas fortes, geometria low-poly suavizada, bordas chanfradas, materiais PBR pintados à mão, detalhes concentrados em pontos focais e paleta terrosa contrastada por magia saturada. O gameplay é apresentado em câmera elevada 3/4, com leitura clara mesmo em escala reduzida.

### 2.3 Descrição-base em inglês para ferramentas de geração

```text
Premium stylized heroic medieval fantasy for a mobile idle RPG,
soft low-poly geometry, clean beveled forms, strong readable silhouette,
slightly exaggerated heroic proportions, hand-painted PBR materials,
controlled surface detail, earthy medieval palette with saturated magical
accents, optimized for an elevated three-quarter gameplay camera.
```

### 2.4 O estilo não é

- fotorrealista;
- anime cel-shaded puro;
- chibi extremo;
- voxel ou semelhante a Minecraft;
- low-poly facetado excessivamente simples;
- dark fantasy grotesco e permanentemente sem cor;
- medieval historicamente rigoroso;
- carregado de microdetalhes invisíveis no mobile.

---

## 3. Pilares visuais

### 3.1 Legibilidade mobile

Todo elemento principal deve ser reconhecível rapidamente em uma tela pequena. A leitura deve depender de:

1. silhueta;
2. proporção;
3. cor dominante;
4. arma ou ferramenta;
5. pose;
6. VFX de função;
7. detalhes secundários.

Microdetalhes nunca devem ser necessários para identificar classe, facção ou perigo.

### 3.2 Fantasia heroica acessível

O mundo pode conter ameaça, morte e forças infernais, mas deve permanecer visualmente acessível, colecionável e comercialmente adequado a um RPG mobile amplo. Horror gráfico, gore e sexualização explícita não fazem parte da direção.

### 3.3 Silhueta primeiro

Cada herói, inimigo, chefe, arma e estação deve ser reconhecível em silhueta preta. Se dois assets da mesma categoria forem indistinguíveis sem textura, a forma precisa ser revisada.

### 3.4 Progressão visível

O jogador deve perceber visualmente a evolução entre Tiers e raridades. Tier e raridade são eixos diferentes e nunca devem ser tratados como sinônimos.

### 3.5 Produção escalável

O estilo deve permitir reutilização inteligente de:

- skeletons;
- shaders;
- famílias de materiais;
- módulos de cenário;
- bases de armas;
- efeitos de raridade;
- animações por arquétipo.

A reutilização não pode eliminar a identidade das unidades mais importantes.

### 3.6 Hierarquia de atenção

Na tela de batalha, a ordem visual desejada é:

1. heróis e chefes;
2. ataques e habilidades importantes;
3. inimigos comuns;
4. recompensas e objetivos;
5. cenário;
6. decoração secundária.

---

## 4. Tom do mundo e âncora narrativa visual

A primeira campanha apresenta três grandes arcos visuais conectados.

### 4.1 Arco I — Os Clãs Goblins

Goblins saqueiam a fronteira e escavam ruínas antigas em busca de relíquias. O Goblin King reuniu tribos antes rivais e utiliza um fragmento de coroa necromântica para fortalecer seu exército.

**Tom:** aventura, ameaça crescente, madeira, ferro, couro, verde musgo, tochas quentes e engenharia improvisada.

### 4.2 Arco II — A Corte dos Ossos

O fragmento roubado desperta uma necrópole esquecida. O Lich reorganiza os mortos em uma força militar e converte energia vital em poder ritual.

**Tom:** ruínas frias, pedra funerária, osso, metal envelhecido, azul espectral, violeta arcano e névoa controlada.

### 4.3 Arco III — A Legião Infernal

Os rituais do Lich abrem uma passagem para uma fortaleza infernal. O Demon Lord atravessa o véu acompanhado por imps, cavaleiros e nobres demoníacos.

**Tom:** basalto, ferro negro, bronze queimado, lava, vermelho, laranja, magenta infernal e energia escura.

Essa âncora orienta a arte, mas não substitui o roteiro narrativo completo.

---

## 5. Escala, orientação e câmera

### 5.1 Convenções técnicas

```text
1 Unity unit = 1 metro
Eixo vertical = Y
Frente do personagem = +Z
Pivô de personagem = centro entre os pés no chão
Pivô de prop = centro da base
Pivô de arma = centro funcional da empunhadura
```

### 5.2 Câmera de batalha

Configuração inicial a validar no Unity:

- perspectiva elevada em 3/4;
- inclinação aproximada de 28° a 35°;
- FOV aproximado de 28 a 35;
- pouca distorção;
- personagens inteiros visíveis;
- espaços claros entre as silhuetas;
- cenário com contraste inferior ao das unidades.

### 5.3 Câmera de inventário

- FOV de 30 a 40;
- fundo neutro escuro ou médio;
- luz principal, preenchimento e recorte;
- rotação lenta e controlada;
- sem pós-processamento que altere a cor real do item.

### 5.4 Câmera de props isolados

- perspectiva 3/4 elevada;
- sombra curta;
- asset centralizado;
- fundo neutro;
- escala relativa consistente dentro da mesma categoria.

---

## 6. Proporções e anatomia

### 6.1 Humanos e humanoides jogáveis

| Categoria | Proporção recomendada |
|---|---:|
| Herói humano padrão | 6,75 cabeças |
| Tanque robusto | 6,25–6,5 cabeças |
| Herói ágil | 6,75–7 cabeças |
| Mago ou suporte | 6,5–6,75 cabeças |
| Mãos | 10% maiores que a proporção realista |
| Armas | 10%–20% maiores que a escala realista |
| Ombreiras | ampliação moderada conforme a classe |

### 6.2 Monstros

| Tipo | Proporção recomendada |
|---|---:|
| Goblin comum | 4,5–5 cabeças |
| Hobgoblin | 5,5–6 cabeças |
| Esqueleto | 6–6,5 cabeças |
| Ghoul | 5,5–6,25 cabeças, postura curvada |
| Imp | 4–4,75 cabeças |
| Demônio humanoide | 6,5–7,5 cabeças |
| Mid-boss | 15%–30% maior que unidade comum equivalente |
| Final boss | 35%–70% maior, conforme arena e câmera |

### 6.3 Regras de anatomia para geração e rig

- A-pose oficial para humanoides;
- braços afastados do torso;
- pernas claramente separadas;
- mãos visíveis;
- dedos simplificados, mas não fundidos quando forem relevantes;
- capa afastada das pernas;
- cabelo sem fundir permanentemente com ombros;
- armas e escudos separados do corpo;
- sem pedestal ou base incorporada;
- frente orientada para +Z;
- pés alinhados ao chão.

---

## 7. Linguagem de formas

| Arquétipo | Formas dominantes | Sensação |
|---|---|---|
| Tanque/Paladino | quadrados, retângulos, arcos largos | estabilidade e proteção |
| Guerreiro | triângulos largos e diagonais fortes | pressão e força |
| Arqueiro/Ranger | curvas, diagonais longas, formas estreitas | alcance e agilidade |
| Mago | círculos, formas verticais e tecidos amplos | conhecimento e poder |
| Xamã | círculos irregulares, totens e formas naturais | ritual e suporte |
| Assassino | triângulos finos e assimetria | perigo e velocidade |
| Morto-vivo | formas quebradas, verticais secas e vazios | decadência e rigidez |
| Demônio | pontas, curvas agressivas e assimetria | ameaça sobrenatural |
| Chefe | combinação exclusiva e ponto focal dominante | autoridade |

A linguagem deve aparecer no corpo, arma, roupa, pose, ícone e VFX.

---

## 8. Estratégia de modularidade dos heróis

### 8.1 Decisão para o MVP

```text
Modelo completo por herói ou skin
+ arma separada
+ escudo separado
+ acessório limitado
+ aura e VFX separados
```

No MVP:

- peitoral não será trocado individualmente no modelo;
- luvas, botas e elmos não serão sistemas modulares completos;
- os itens continuarão existindo mecanicamente no inventário;
- conjuntos importantes poderão desbloquear uma skin completa;
- armas e escudos poderão refletir o equipamento real;
- a modularidade total será reavaliada somente após o vertical slice.

Essa decisão reduz clipping, incompatibilidade de skinning e retrabalho com modelos gerados por IA.

---

## 9. Heróis iniciais

### 9.1 Paladino — Tanque

**Silhueta:** larga, pesada e triangular.  
**Formas:** quadrados e arcos protetores.  
**Elementos:** armadura de placas, escudo grande, espada de uma mão, manto curto.  
**Paleta:** azul medieval, marfim, aço e dourado seletivo.  
**Ponto focal:** símbolo sagrado no escudo.  
**Emissão:** dourada discreta em runa ou gema.  
**Proibido:** ombreiras que ocultem a cabeça, escudo maior que o corpo inteiro, excesso de ouro.

### 9.2 Arqueira — Dano físico

**Silhueta:** estreita, diagonal e assimétrica.  
**Formas:** curvas longas e triângulos leves.  
**Elementos:** arco recurvo estilizado, aljava, couro em camadas, capa curta assimétrica.  
**Paleta:** verde floresta, couro castanho, bege e metal fosco.  
**Ponto focal:** arco e capuz/cabelo.  
**Emissão:** ausente no visual-base; efeitos vêm das habilidades.  
**Proibido:** arco fino e frágil, acessórios pequenos demais, sexualização da armadura.

### 9.3 Mago — Dano mágico

**Silhueta:** vertical, ampla na base e centrada no cajado.  
**Formas:** círculos, losangos e tecidos largos.  
**Elementos:** cajado separado, manto em camadas, cinto com foco arcano.  
**Paleta:** azul profundo, grafite, bronze e ciano arcano.  
**Ponto focal:** cristal do cajado.  
**Emissão:** ciano controlado no cristal e em duas ou três runas.  
**Proibido:** partes flutuantes excessivas, tecido colado às pernas, emissão em toda a roupa.

Assassino e Clérigo permanecem arquétipos futuros e não fazem parte do pacote obrigatório do primeiro vertical slice.

---

## 10. Estrutura comum das facções inimigas

Cada facção usa uma progressão funcional semelhante, sem exigir que tenha exatamente o mesmo número de unidades:

1. criatura básica;
2. combatente corpo a corpo;
3. atacante à distância;
4. conjurador ou suporte;
5. mid-boss;
6. elite corpo a corpo ou monstro especial;
7. elite à distância;
8. conjurador avançado ou controlador;
9. final boss.

A facção dos mortos-vivos possui uma unidade adicional porque o Ghoul representa uma família anatômica distinta dos esqueletos.

---

## 11. Facção I — Clãs Goblins

### 11.1 Identidade visual

- madeira improvisada;
- ferro reaproveitado;
- couro irregular;
- cordas e remendos;
- verde oliva e verde amarelado;
- laranja de tochas;
- tecnologia simples, armadilhas e mineração;
- assimetria e construção funcional.

### 11.2 Hierarquia oficial

| Ordem | ID estável | Nome em inglês | Nome em português | Categoria | Função de combate | Direção visual |
|---:|---|---|---|---|---|---|
| 1 | `enemy_goblin_scavenger_001` | Goblin Scavenger | Goblin Saqueador | Comum | melee básico e coleta | pequeno, mochila, faca curta, postura desconfiada |
| 2 | `enemy_goblin_warrior_001` | Goblin Warrior | Guerreiro Goblin | Comum | melee frontal | espada curta, escudo de madeira e proteção improvisada |
| 3 | `enemy_goblin_archer_001` | Goblin Archer | Arqueiro Goblin | Comum | dano à distância | arco curto, aljava grande e silhueta inclinada |
| 4 | `enemy_goblin_shaman_001` | Goblin Shaman | Xamã Goblin | Comum/Especial | magia, cura e buffs | cajado-totem, penas, ossos e energia verde-amarela |
| 5 | `boss_goblin_chieftain_001` | Goblin Chieftain | Chefe Goblin | Mid-boss | comandante/bruiser | maior, coroa tribal, machado largo e troféus |
| 6 | `enemy_hobgoblin_knight_001` | Hobgoblin Knight | Cavaleiro Hobgoblin | Elite | tanque e controle | goblinoide maior, armadura disciplinada, escudo metálico |
| 7 | `enemy_goblin_ranger_001` | Goblin Ranger | Patrulheiro Goblin | Elite | precisão, armadilhas e mobilidade | arco longo, capa curta, bolsas e armadilhas visíveis |
| 8 | `enemy_goblin_high_shaman_001` | Goblin High Shaman | Alto Xamã Goblin | Elite | magia avançada, invocação e suporte | máscara ritual, cajado maior, aura controlada e totens |
| 9 | `boss_goblin_king_001` | Goblin King | Rei Goblin | Final boss | comandante multifase | armadura real saqueada, coroa, arma de assinatura e relíquia necromântica |

### 11.3 Refinamentos de nomenclatura

- `Goblin Chief` torna-se **Goblin Chieftain**, termo mais natural para líder tribal;
- `Goblin Mage` torna-se **Goblin Shaman**, mais coerente com a identidade ritual da facção;
- o conjurador avançado torna-se **Goblin High Shaman**;
- `Goblin Knight` torna-se **Hobgoblin Knight**, diferenciando uma casta militar maior e mais disciplinada;
- Goblin Archer e Goblin Ranger permanecem distintos: o Archer é tropa comum; o Ranger é elite móvel com armadilhas.

### 11.4 Silhueta do final boss

O Goblin King deve ser reconhecido por:

- coroa irregular feita de metais saqueados;
- barriga/torso largo sem cair em caricatura cômica extrema;
- arma desproporcional, porém funcional;
- fragmento de coroa necromântica como foco visual;
- combinação de verde, bronze escuro, vermelho real e energia violeta discreta.

---

## 12. Facção II — Corte dos Ossos

### 12.1 Identidade visual

- osso envelhecido e não branco puro;
- ferro oxidado;
- tecido funerário;
- pedra fria;
- azul espectral e violeta arcano;
- movimentos rígidos em esqueletos;
- movimentos predatórios e curvados em ghouls;
- hierarquia militar preservada após a morte.

### 12.2 Hierarquia oficial

| Ordem | ID estável | Nome em inglês | Nome em português | Categoria | Função de combate | Direção visual |
|---:|---|---|---|---|---|---|
| 1 | `enemy_restless_skeleton_001` | Restless Skeleton | Esqueleto Inquieto | Comum | melee básico | ossos incompletos, arma quebrada e tecido funerário |
| 2 | `enemy_skeleton_soldier_001` | Skeleton Soldier | Soldado Esqueleto | Comum | melee disciplinado | elmo antigo, espada, escudo e postura militar |
| 3 | `enemy_skeleton_archer_001` | Skeleton Archer | Arqueiro Esqueleto | Comum | dano à distância | arco antigo, aljava, ombros estreitos e postura fixa |
| 4 | `enemy_bone_acolyte_001` | Bone Acolyte | Acólito Ósseo | Comum/Especial | magia inicial e debuffs | manto funerário, bastão, runas e chama espectral |
| 5 | `boss_bone_knight_001` | Bone Knight | Cavaleiro Ósseo | Mid-boss | tanque, contra-ataque e comando | armadura pesada incompleta, espada longa e brasão quebrado |
| 6 | `enemy_ghoul_reaver_001` | Ghoul Reaver | Carniçal Dilacerador | Elite/Monstro | melee rápido, sangramento e salto | corpo curvado, garras grandes, pele cinza e correntes rompidas |
| 7 | `enemy_death_knight_001` | Death Knight | Cavaleiro da Morte | Elite | melee pesado e aura de medo | armadura negra completa, espada rúnica e capa deteriorada |
| 8 | `enemy_grave_ranger_001` | Grave Ranger | Patrulheiro da Tumba | Elite | precisão, veneno e marcação | arco reforçado, capuz rasgado, flechas espectrais e ossos mais escuros |
| 9 | `enemy_bone_archmage_001` | Bone Archmage | Arquimago Ósseo | Elite | área, invocação e controle | coroa arcana, cajado elevado e múltiplos focos espectrais limitados |
| 10 | `boss_lich_001` | Lich | Lich | Final boss | necromancia multifase | corpo esquelético nobre, filactério, manto amplo e coroa de ossos |

### 12.3 Refinamentos de nomenclatura

- a grafia correta é **Skeleton**, não `Eskeleton`;
- a grafia correta é **Ghoul**, não `Goul`;
- `Skeleton Mage` torna-se **Bone Acolyte**, adequado a um conjurador inicial;
- `Skeleton Knight` torna-se **Bone Knight** como mid-boss;
- `Skeleton Grand-Knight` torna-se **Death Knight**, nome mais reconhecível e distinto;
- `Skeleton Ranger` torna-se **Grave Ranger**, evitando redundância nominal com Skeleton Archer;
- `Skeleton Archmage` torna-se **Bone Archmage**;
- o Lich permanece como chefe final e autoridade necromântica da facção.

### 12.4 Regras anatômicas

- esqueletos devem ter ossos grossos o suficiente para leitura mobile;
- costelas podem ser simplificadas;
- não modelar dezenas de ossos pequenos sem função visual;
- juntas precisam permitir rig estável;
- ghouls não reutilizam diretamente o skeleton de esqueletos;
- o Lich deve manter silhueta humanoide para rig, mas pode usar peças flutuantes limitadas como elementos separados.

---

## 13. Facção III — Legião Infernal

### 13.1 Identidade visual

- basalto;
- ferro negro;
- bronze queimado;
- couro escuro;
- chifres;
- vermelho, laranja, magenta e violeta;
- emissão localizada em olhos, runas, fendas e armas;
- formas pontiagudas e assimétricas;
- hierarquia militar e aristocrática.

### 13.2 Hierarquia oficial

| Ordem | ID estável | Nome em inglês | Nome em português | Categoria | Função de combate | Direção visual |
|---:|---|---|---|---|---|---|
| 1 | `enemy_imp_001` | Imp | Diabrete | Comum | assédio, ataque rápido e fogo fraco | pequeno, asas curtas opcionais, chifres e mãos grandes |
| 2 | `enemy_imp_legionary_001` | Imp Legionary | Legionário Diabrete | Comum | melee e formação | lança curta, escudo de ferro negro e elmo infernal |
| 3 | `enemy_imp_skirmisher_001` | Imp Skirmisher | Escaramuçador Diabrete | Comum | projéteis, mobilidade e evasão | funda, besta leve ou dardos de fogo; corpo inclinado |
| 4 | `enemy_imp_pyromancer_001` | Imp Pyromancer | Piromante Diabrete | Comum/Especial | magia de fogo e área | cajado curto, mãos incandescentes e capuz queimado |
| 5 | `boss_incubus_warden_001` | Incubus Warden | Guardião Íncubo | Mid-boss | controle, charme e duelo | humanoide elegante, armadura infernal, asas contidas e lâmina curva |
| 6 | `enemy_demon_knight_001` | Demon Knight | Cavaleiro Demoníaco | Elite | tanque, investida e aura | armadura pesada, chifres, espada de duas mãos e fogo interno |
| 7 | `enemy_demon_ranger_001` | Demon Ranger | Patrulheiro Demoníaco | Elite | dano à distância e marcação | arco infernal, capa queimada e flechas energizadas |
| 8 | `enemy_succubus_enchantress_001` | Succubus Enchantress | Encantadora Súcubo | Elite | controle mental, debuffs e magia | silhueta leve, asas, foco arcano e vestimenta não explícita |
| 9 | `boss_demon_lord_001` | Demon Lord | Lorde Demônio | Final boss | chefe multifase, invocação e destruição | enorme, coroa de chifres, armadura de basalto e núcleo infernal |

### 13.3 Refinamentos de nomenclatura

- `Imp Soldier` torna-se **Imp Legionary**, reforçando a natureza militar da facção;
- `Imp Ranger` torna-se **Imp Skirmisher**, pois imps funcionam melhor como tropas móveis e irregulares;
- `Imp Mage` torna-se **Imp Pyromancer**, ligando a unidade ao fogo infernal;
- Incubus permanece como mid-boss, reinterpretado como **Incubus Warden**, duelista e controlador;
- Succubus permanece como elite de controle, com o título **Succubus Enchantress**;
- Demon Knight, Demon Ranger e Demon Lord permanecem como progressão superior.

### 13.4 Direção de conteúdo para Incubus e Succubus

Incubus e Succubus são tratados como nobres demoníacos de controle e encantamento, sem sexualização explícita. A leitura deve vir de:

- elegância ameaçadora;
- postura confiante;
- asas e chifres;
- magia de controle;
- contraste entre tecidos escuros e metal infernal;
- silhueta clara e apropriada ao público amplo.

Evitar lingerie, nudez, anatomia exageradamente sexualizada ou poses promocionais incompatíveis com combate.

---

## 14. Correspondência funcional entre facções

| Função | Goblins | Mortos-vivos | Demônios |
|---|---|---|---|
| Básico | Goblin Scavenger | Restless Skeleton | Imp |
| Melee comum | Goblin Warrior | Skeleton Soldier | Imp Legionary |
| Ranged comum | Goblin Archer | Skeleton Archer | Imp Skirmisher |
| Caster comum | Goblin Shaman | Bone Acolyte | Imp Pyromancer |
| Mid-boss | Goblin Chieftain | Bone Knight | Incubus Warden |
| Elite melee | Hobgoblin Knight | Death Knight/Ghoul Reaver | Demon Knight |
| Elite ranged | Goblin Ranger | Grave Ranger | Demon Ranger |
| Elite caster/control | Goblin High Shaman | Bone Archmage | Succubus Enchantress |
| Final boss | Goblin King | Lich | Demon Lord |

A correspondência orienta dificuldade e leitura, mas não obriga habilidades idênticas.

---

## 15. Raridades

### 15.1 Paleta oficial inicial

| Raridade | Cor principal | Hex | Regra visual |
|---|---|---|---|
| Comum | cinza aço | `#A7ADB2` | construção funcional, sem emissão |
| Incomum | verde | `#5DBB63` | acabamento melhor e um detalhe secundário |
| Raro | azul | `#3D8EFF` | gema ou runa, metal mais polido |
| Épico | roxo | `#A24CE3` | ornamentação moderada e emissão baixa |
| Lendário | laranja-dourado | `#F3A62A` | silhueta de prestígio, dourado seletivo e aura curta |
| Mítico | vermelho-iridescente | `#E74B3C` + gradiente | material impossível, assinatura própria e VFX controlado |

### 15.2 Matriz de alterações

| Raridade | Geometria | Material | Emissão | VFX |
|---|---|---|---|---|
| Comum | base | simples | nenhuma | nenhum |
| Incomum | detalhe pequeno | acabamento superior | nenhuma | nenhum |
| Raro | uma gema/runa | polido | muito baixa | brilho ocasional |
| Épico | ornamento moderado | material especial | baixa | partículas discretas |
| Lendário | silhueta de prestígio | dourado seletivo | média localizada | aura curta |
| Mítico | assinatura exclusiva | material sobrenatural | controlada | efeito animado próprio |

### 15.3 Limites

- raridade não muda a tecnologia-base do Tier;
- emissão nunca cobre toda a superfície;
- partículas não podem esconder a silhueta;
- Mítico não significa “tudo neon”;
- diferenças devem ser legíveis também por forma, ornamento e moldura, não apenas por cor.

---

## 16. Tiers de equipamento

| Tier | Linguagem visual | Materiais-base sugeridos |
|---:|---|---|
| T1 | simples, artesanal e funcional | ferro, madeira, couro cru, linho |
| T2 | construção profissional | aço, couro tratado, lã e bronze |
| T3 | primeiras runas e acabamento de guilda | aço refinado, prataço, tecido rúnico |
| T4 | leveza e refinamento mágico | mithril, seda élfica, cristal espiritual |
| T5 | ancestral, robusto e ornamentado | adamantita, couro ancestral, essência maior |
| T6 | agressivo e orgânico | aço dracônico, couro dracônico, fênix |
| T7 | astral e não convencional | oricalco, seda astral, núcleo do vazio |
| T8 | celestial, simétrico e luminoso | etério, trama serafim, cristal celestial |
| T9 | divino, singular e de assinatura | metal primordial, trama do destino, coração da criação |

### 16.1 Regra Tier × raridade

```text
Tier altera:
- tecnologia;
- material estrutural;
- processo de fabricação;
- silhueta-base;
- complexidade construtiva.

Raridade altera:
- qualidade;
- acabamento;
- ornamentação;
- afixos visuais;
- emissão;
- VFX.
```

Uma espada T1 Mítica continua sendo uma obra-prima de tecnologia T1. Ela não deve parecer uma arma divina T9.

---

## 17. Biblioteca inicial de cores

### 17.1 Base medieval

| Uso | Hex |
|---|---|
| Carvão profundo | `#151A1D` |
| Ferro frio | `#58636A` |
| Madeira escura | `#5B3B27` |
| Couro médio | `#7A4C2D` |
| Pergaminho | `#D7C49A` |
| Linho | `#C8BFA9` |
| Verde musgo | `#536A3B` |
| Azul medieval | `#315A7D` |

### 17.2 Magias

| Escola | Hex |
|---|---|
| Arcano | `#25A7FF` |
| Natureza | `#72C94A` |
| Sagrado | `#F4C95D` |
| Sombra | `#7547C7` |
| Fogo | `#F06432` |
| Gelo | `#67D6E8` |
| Veneno | `#A2D729` |
| Caos | `#D83B51` |
| Necromancia | `#68C7C1` + `#7B5BC7` |
| Infernal | `#FF5A2F` + `#B4253A` |

O cenário usa saturação baixa a média. Heróis usam saturação média. Habilidades e recompensas raras podem usar saturação alta em áreas controladas.

---

## 18. Materiais PBR estilizados

### 18.1 Regras gerais

- albedo limpo, sem ruído fotográfico excessivo;
- roughness claramente distinto entre tecido, couro, madeira e metal;
- metallic usado apenas onde fisicamente coerente;
- desgaste concentrado em bordas e áreas de contato;
- oclusão pintada com moderação;
- gradientes amplos para leitura;
- detalhes menores sugeridos pela textura, não por geometria excessiva;
- emissão limitada a runas, gemas, olhos, gumes e fissuras mágicas.

### 18.2 Famílias

**Ferro e aço:** cinza azulado, bordas claras, roughness médio.  
**Bronze e ouro:** quente, brilho seletivo, sem aparência plástica.  
**Couro:** roughness alto, variação de tom e costuras grandes.  
**Tecido:** gradientes suaves, dobras largas e pouco brilho.  
**Madeira:** veios simplificados e grandes, sem fotografia literal.  
**Pedra:** planos amplos, fraturas legíveis e musgo localizado.  
**Osso:** marfim envelhecido, áreas amareladas e cavidades mais escuras.  
**Cristal:** cor saturada, núcleo mais claro e emissão moderada.  
**Infernal:** basalto escuro com fissuras incandescentes localizadas.  
**Celestial:** marfim, ouro pálido e luz limpa, evitando branco estourado.

---

## 19. Ambientes iniciais

### 19.1 Bioma 1 — Acampamento e Mina Goblin

**Módulos:** piso de terra/pedra, parede escavada, viga, ponte, trilho, carrinho, jaula, torre improvisada, tocha e portão.  
**Cores:** marrom, verde musgo, ferro, laranja quente.  
**Luz:** quente e irregular, com áreas de sombra legíveis.  
**Densidade:** moderada; objetos não podem esconder combatentes.

### 19.2 Bioma 2 — Cripta e Necrópole

**Módulos:** piso funerário, parede de cripta, arco, sarcófago, ossário, grade, vela espectral, altar e portal ritual.  
**Cores:** pedra fria, osso, azul espectral e violeta.  
**Luz:** fria, direcional e com névoa discreta.  
**Densidade:** menor no centro da arena; ornamentação nas bordas.

### 19.3 Bioma 3 — Fortaleza Infernal

**Módulos:** basalto, muralha, ponte, corrente, braseiro, grade, plataforma, canal de lava, portal e trono.  
**Cores:** preto, ferro queimado, vermelho, laranja e magenta.  
**Luz:** forte contraste, porém sem perder detalhes nas sombras.  
**Densidade:** formas grandes e menos objetos pequenos.

---

## 20. Estações de crafting

| Profissão | Estação | Forma dominante | Elementos obrigatórios |
|---|---|---|---|
| Ferreiro | Forja | quadrada e pesada | fornalha, bigorna, fole, rack e brasa |
| Costureiro | Ateliê | horizontal e organizada | bancada, manequim, rolos de tecido, couro e ferramentas |
| Encantador | Mesa Arcana | circular e vertical | mesa rúnica, cristais, livros e foco flutuante limitado |
| Alquimista | Laboratório | irregular e orgânica | caldeirão, frascos, tubos, ervas e fogo controlado |
| Coletador | Acampamento de Expedição | triangular e robusta | mochila, mapas, caixas, ferramentas e pequeno abrigo |

Cada estação deve ter uma silhueta reconhecível e uma área clara para animação de trabalho.

---

## 21. Iluminação

### 21.1 Personagens

- luz principal suave;
- preenchimento suficiente para ler o lado em sombra;
- rim light discreta para separar do cenário;
- sombras sem preto absoluto;
- efeitos mágicos não substituem a iluminação-base.

### 21.2 Presets iniciais

| Preset | Temperatura | Cor secundária | Característica |
|---|---|---|---|
| Vila | quente | azul suave | acolhedor e heroico |
| Mina Goblin | quente localizada | verde/marrom | improvisado e perigoso |
| Cripta | fria | violeta | sobrenatural e silenciosa |
| Fortaleza Infernal | quente intensa | magenta escuro | opressiva e energética |
| Inventário | neutra | rim ajustável | leitura fiel do asset |
| Gacha | teatral | cor da raridade | foco no personagem/recompensa |

---

## 22. Orçamentos técnicos preliminares

Os valores serão refinados na Task 015.

| Categoria | LOD0 sugerido | Textura sugerida |
|---|---:|---:|
| Herói jogável | 5.000–8.000 triângulos | 1K–2K |
| Inimigo comum | 3.000–5.000 | 1K |
| Elite/Mid-boss | 5.000–9.000 | 1K–2K |
| Final boss | 8.000–15.000 | 2K |
| Arma | 1.000–3.000 | 512–1K |
| Material coletável | 300–1.000 | 512 |
| Prop comum | 300–2.000 | 512–1K |
| Estação | 4.000–10.000 | 1K–2K |
| Módulo de ambiente | 1.000–5.000 | 1K |

### 22.1 LODs

- LOD0: asset principal;
- LOD1: aproximadamente 50% dos triângulos;
- LOD2: aproximadamente 20%–25%;
- ocultação ou impostor para elementos distantes quando necessário.

---

## 23. Regras-base para Meshy

### 23.1 Prompt-base positivo

```text
Premium stylized heroic medieval fantasy game asset for a mobile idle RPG,
soft low-poly game-ready geometry, clean beveled forms, strong readable
silhouette, controlled chunky details, hand-painted stylized PBR materials,
clear material separation, optimized for an elevated three-quarter camera,
clean topology intent, isolated asset, consistent scale.
```

### 23.2 Prompt negativo-base

```text
photorealistic, extreme chibi, voxel art, Minecraft style, flat unlit
materials, excessive microdetails, noisy photo textures, ultra-thin ornaments,
fragile geometry, background scene, pedestal, text, letters, logo, watermark,
floating debris, disconnected accidental parts, extra limbs, merged hands,
asymmetrical anatomy errors, weapon fused to hand, full-surface emission
```

### 23.3 Personagens humanoides

Adicionar:

```text
neutral A-pose, facing +Z, arms separated from torso, legs separated,
feet flat on the ground, hands visible, weapon and shield generated as separate
assets, cape separated from legs, suitable for humanoid rigging
```

### 23.4 Props

Adicionar:

```text
single isolated object, centered base pivot intent, no environment, no stand,
solid readable shapes, no unnecessary internal geometry
```

---

## 24. Faça e não faça

### 24.1 Faça

- use formas grandes;
- preserve silhueta clara;
- diferencie materiais;
- concentre detalhes no rosto, torso e arma;
- use bevels moderados;
- exagere levemente armas e mãos;
- limite focos de emissão;
- teste em tamanho reduzido;
- mantenha escala e pivô consistentes;
- use assimetria apenas com propósito.

### 24.2 Não faça

- fotorealismo;
- microdetalhes generalizados;
- ornamentos finíssimos;
- emissão em toda a superfície;
- dezenas de partículas permanentes;
- armas fundidas às mãos;
- capas coladas às pernas;
- bases incorporadas;
- textos ou logos;
- sexualização explícita;
- gore;
- cenário mais contrastado que os personagens;
- variações que só mudem a cor sem alterar acabamento ou ornamento.

---

## 25. Referências visuais do projeto

### ART_REF_001 — Style Overview

Arquivo:

```text
Art/References/ART_REF_001_STYLE_OVERVIEW.png
```

**Uso:** visão macro de estilo, personagens, raridades, Tiers, ambientes e estações.  
**Status:** referência conceitual aprovada com ressalvas.  
**Ressalvas:** textos e nomes presentes na imagem não são fonte de dados; Assassino, Clérigo, Orc e Senhor Demônio ilustrado não representam automaticamente conteúdo aprovado. O roster oficial é o definido neste documento.

---

## 26. Nomenclatura de assets

### 26.1 Padrão

```text
<categoria>_<facção-ou-família>_<nome>_<variante>_<número>
```

Exemplos:

```text
enemy_goblin_warrior_001
boss_goblin_king_001
enemy_bone_archmage_001
boss_lich_001
enemy_imp_pyromancer_001
boss_demon_lord_001
weapon_iron_sword_t1_001
station_blacksmith_t1_001
```

### 26.2 Status de referência

- `DRAFT` — exploração;
- `REVIEW` — aguardando aprovação;
- `APPROVED` — fonte válida;
- `DEPRECATED` — não usar em novos assets;
- `REPLACED` — substituída por versão indicada.

---

## 27. Processo de aprovação

Cada asset visual deve passar por:

1. validação do ID e função;
2. validação da silhueta;
3. validação de proporção e escala;
4. validação da facção;
5. validação de Tier e raridade;
6. validação de materiais;
7. validação do orçamento técnico;
8. teste em câmera de gameplay;
9. teste em fundo claro e escuro;
10. aprovação e versionamento.

Um render bonito em close não é suficiente. O asset deve funcionar em gameplay.

---

## 28. Entregáveis concluídos pela Task 014

- direção artística oficial;
- pilares visuais;
- proporções e linguagem de formas;
- estratégia de modularidade;
- direção dos três heróis iniciais;
- roster refinado de Goblins;
- roster refinado de Mortos-vivos;
- roster refinado de Demônios;
- correspondência funcional entre facções;
- paletas-base e mágicas;
- sistema visual de raridades;
- sistema visual de Tiers;
- materiais PBR estilizados;
- direção dos três primeiros biomas;
- direção das cinco estações;
- câmera e iluminação iniciais;
- orçamento técnico preliminar;
- regras-base para Meshy;
- critérios de aprovação.

---

## 29. Limites da Task 014

Ainda pertencem às próximas Tasks:

### Task 015 — Catálogo mestre e orçamento técnico

- inventário completo de assets;
- prioridade;
- dependências;
- orçamento final individual;
- resolução e LOD por asset;
- caminhos no Unity;
- status de produção.

### Task 016 — Personagens, inimigos e chefes

- turnarounds separados;
- fichas faciais;
- vistas frontal, lateral e traseira;
- armas separadas;
- variantes elite;
- folhas de escala;
- prompts individuais de conceito.

### Task 017 — Equipamentos, Tiers e raridades

- família completa T1–T9;
- matriz de seis raridades;
- regras por slot;
- skins e armas de assinatura.

### Task 018 — Ambientes e estações

- kits modulares;
- medidas finais;
- mapas de montagem;
- props por bioma;
- upgrades visuais T1–T9 das estações.

### Task 019 — Pacote de prompts Meshy

- prompt de geometria por asset;
- prompt de textura;
- prompt negativo;
- instruções de remesh;
- rigging;
- exportação;
- checklist de pós-processamento.

---

## 30. Critérios de aceite

A Task 014 é considerada concluída quando:

- este documento estiver versionado no repositório;
- o roster oficial não possuir IDs duplicados;
- todas as unidades tiverem função e silhueta definidas;
- Tier e raridade estiverem separados conceitualmente;
- a estratégia de modularidade estiver registrada;
- paleta e materiais estiverem documentados;
- três biomas possuírem direção clara;
- cinco estações possuírem linguagem visual;
- regras de Meshy e exemplos proibidos estiverem registrados;
- a Task 015 puder montar o catálogo sem inventar uma nova direção artística.

---

## 31. Decisões vinculantes

1. O estilo oficial é **Stylized Heroic Fantasy**.
2. O MVP usa herói completo por skin, com arma e escudo separados.
3. Tier e raridade são dimensões visuais independentes.
4. Goblin Chieftain, Bone Knight e Incubus Warden são os mid-bosses iniciais.
5. Goblin King, Lich e Demon Lord são os final bosses dos três primeiros arcos.
6. Incubus e Succubus serão apresentados como controladores infernais, sem sexualização explícita.
7. `Eskeleton` e `Goul` são grafias inválidas; usar `Skeleton` e `Ghoul`.
8. Assets devem ser aprovados em câmera de gameplay, não apenas por render de close.
9. A imagem de visão geral é referência de clima e estilo, não catálogo autoritativo.
10. IDs textuais deste documento devem ser preservados ou migrados explicitamente.

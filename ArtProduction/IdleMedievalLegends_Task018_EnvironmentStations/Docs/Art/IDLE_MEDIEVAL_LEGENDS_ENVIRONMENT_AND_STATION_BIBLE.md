# Idle Medieval Legends — Environment & Crafting Station Bible

Versão: **1.0 — Task 018**  
Fonte superior: `IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md`  
Escopo: três biomas modulares, 60 assets de ambiente e 25 estações profissionais.

## 1. Objetivo

Este documento transforma a direção visual do projeto em regras de produção reproduzíveis para Meshy, Blender e Unity. Ele não autoriza criar cenas inteiras como uma única malha. Cada bioma deve ser montado a partir de módulos isolados, com escala, pivô, grade, materiais e limites técnicos consistentes.

## 2. Decisões vinculantes

1. O estilo é **Stylized Heroic Fantasy**, low-poly premium, PBR pintado à mão e legível em câmera elevada 3/4.
2. A grade estrutural comum é de **4 m**.
3. `1 Unity unit = 1 metro`, eixo vertical `Y`, frente significativa em `+Z`.
4. Pivô de módulos e estações fica no centro da base em `Y=0`.
5. Piso, parede, canto, trilho, ponte e canal devem respeitar limites exatos de encaixe.
6. Fogo, fumaça, névoa, lava, energia espectral, portais, levitação e runas animadas são camadas do Unity.
7. Texturas não podem conter iluminação direcional assada.
8. Ambientes completos não devem ser gerados no Meshy como uma única malha.
9. O centro das arenas deve permanecer mais limpo que as bordas.
10. Toda estação deve ser identificável pela silhueta, sem depender de UI.

## 3. Resumo de produção

| Categoria | Quantidade |
|---|---:|
| Módulos e props de ambiente | 60 |
| Estações profissionais | 25 |
| Total de assets 3D especificados | 85 |
| Prompts por asset | 3 |
| Prompts individuais | 255 |
| Prompts de visão geral | 8 |
| Total de prompts | 263 |

Distribuição dos ambientes:

| Bioma | Assets |
|---|---:|
| Acampamento e Mina Goblin | 20 |
| Cripta e Necrópole | 20 |
| Fortaleza Infernal | 20 |

## 4. Sistema modular

### 4.1 Grade

- célula-base: `4 m × 4 m`;
- meio passo permitido na composição: `2 m`;
- ajuste fino para props: incrementos de `0,25 m`;
- paredes padrão: `4 m` de largura e aproximadamente `3–4 m` de altura;
- passagem comum: aproximadamente `2,2 m` de largura por `3 m` de altura;
- peças de 8 m devem ser múltiplos exatos da grade;
- nenhuma decoração pode cruzar o plano de encaixe e produzir interpenetração inevitável.

### 4.2 Pivô e orientação

- base central em `Y=0`;
- peças de parede com frente em `+Z`;
- conexões nomeadas `SNAP_N`, `SNAP_E`, `SNAP_S`, `SNAP_W`;
- a origem não deve ficar na extremidade, salvo uma exceção documentada;
- objetos com porta, grade ou tampa usam partes separadas quando animados.

### 4.3 Modularidade visual

Os encaixes precisam ser geometricamente exatos, mas não devem formar linhas artificiais óbvias. Para quebrar repetição:

- usar decals e vertex color no Unity;
- alternar props pequenos;
- rotacionar elementos não direcionais;
- usar duas ou três variações de material;
- manter a malha estrutural principal estável.

Não criar variações que alterem o footprint ou os planos de snap.

## 5. Bioma I — Acampamento e Mina Goblin

**Narrativa:** Uma mina tomada por tribos goblins que combinam escavação rudimentar, madeira reaproveitada, ferragens roubadas e engenharia improvisada.  
**Formas:** irregular triangles, leaning supports, asymmetrical silhouettes, chunky improvised construction.  
**Materiais:** dark rough timber, packed earth, chipped gray stone, raw iron, rope, patched hide and moss.  
**Paleta:** `#5B3B27 #536A3B #58636A #7A4C2D #D47A2A`.  
**Luz:** warm localized torchlight with readable soft shadows and restrained moss-green ambient bounce.

### Regras

- construção improvisada, mas funcional;
- suportes grossos e legíveis;
- assimetria controlada;
- ferragens e cordas superdimensionadas para leitura mobile;
- objetos não podem parecer miniaturas de cenário realista;
- evitar excesso de lixo visual no centro da arena;
- o trono do Rei Goblin e o portão são os pontos de maior prestígio.

### Kit

O kit contém 20 assets: pisos, paredes, canto, suporte, arco, trilhos, ponte, portão, torre, tenda, trono e props de mineração/acampamento.

## 6. Bioma II — Cripta e Necrópole

**Narrativa:** Arquitetura funerária antiga reativada por necromancia, com pedra fria, ossos organizados, ferro envelhecido e focos espectrais controlados.  
**Formas:** vertical arches, repeated funerary geometry, controlled symmetry, narrow silhouettes and eroded ceremonial forms.  
**Materiais:** cold blue-gray stone, aged bone, oxidized dark iron, worn funerary cloth and pale necromantic crystal.  
**Paleta:** `#4C5963 #C9C1A8 #26313A #5A4B78 #6ECBE5`.  
**Luz:** cold directional light, restrained violet accents, pale spectral focal points and subtle low fog.

### Regras

- arquitetura mais organizada que o acampamento Goblin;
- ossos devem parecer antigos, estilizados e não gráficos;
- esqueletos completos não devem ser repetidos como decoração em todo módulo;
- símbolos podem ser abstratos, sem texto legível;
- energia espectral deve funcionar mesmo desligada, preservando o desenho do prop;
- centro de batalha limpo, ornamentação concentrada em paredes e bordas.

### Kit

O kit contém 20 assets: piso, paredes, canto, arco, coluna, porta, grades, sarcófago, altar, portal, trono, ossário, cristais e props funerários.

## 7. Bioma III — Fortaleza Infernal

**Narrativa:** Uma cidadela militar demoníaca de basalto, ferro queimado e canais de energia ígnea, construída em volumes grandes e opressivos.  
**Formas:** massive wedges, downward spikes, heavy arches, broad platforms, controlled demonic asymmetry and monumental silhouettes.  
**Materiais:** black basalt, burned iron, dark brass, obsidian, charred stone and restrained infernal crystal.  
**Paleta:** `#17191C #4B2725 #8E2F2B #E2602D #C63A78`.  
**Luz:** high-contrast orange-red light with dark magenta secondary accents, preserving readable shadow detail.

### Regras

- volumes grandes, militares e opressivos;
- menos props pequenos que nos outros biomas;
- espinhos grossos e controlados;
- preto não pode eliminar o detalhe da silhueta;
- lava é material/VFX separado dentro de canais vazios;
- arcos, portões, trono e arena usam escala monumental;
- evitar transformar toda superfície em rachadura emissiva.

### Kit

O kit contém 20 assets: piso, paredes, canto, arco, pilares, plataforma, ponte, arena, canais de lava, portal, portão, trono e props infernais.

## 8. Estações de crafting

Cada profissão possui cinco marcos visuais: `T1`, `T3`, `T5`, `T7` e `T9`. Os Tiers intermediários são progressão sistêmica e podem usar a estação do marco anterior até que exista uma decisão de conteúdo diferente.

| Profissão | Estação | Forma | Componente de leitura principal |
|---|---|---|---|
| Ferreiro | Forja | square, heavy and grounded | furnace |
| Costureiro | Ateliê | horizontal, organized and layered | workbench |
| Encantador | Mesa Arcana | circular, vertical and centered | runic table |
| Alquimista | Laboratório | irregular, organic and clustered | cauldron |
| Coletador | Acampamento de Expedição | triangular, robust and portable | small shelter |

### 8.1 Evolução por marco

| Tier | Grau | Regra visual |
|---:|---|---|
| T1 | Aprendiz / Fronteira | small, practical, handmade, compact and visibly assembled from common local materials |
| T3 | Proficiente / Rúnico | reinforced professional station with clearer organization and first visible runic support |
| T5 | Mestre / Ancestral | prestigious workshop with ancestral construction, denser tool language and a stronger central silhouette |
| T7 | Grão-Mestre / Astral | advanced station integrating astral materials, controlled levitation and precise magical engineering |
| T9 | Deus / Criação | iconic endgame station with primordial construction, fate or creation motifs and an unmistakable silhouette |

### 8.2 Continuidade da família

Uma estação T9 deve ser reconhecida como evolução da estação T1. Manter:

- mesmo foco funcional;
- mesma direção de interação;
- área de trabalho equivalente;
- forma dominante da profissão;
- componentes obrigatórios;
- sockets com nomes estáveis.

A evolução acrescenta construção, materiais, precisão e energia; não substitui a profissão por outra fantasia.

## 9. Sockets obrigatórios

### Ambientes

- `SNAP_GRID_BASE`;
- `SNAP_N/E/S/W` para módulos;
- `INTERACT_PRIMARY` para portas, portais, tronos, altares, cofres ou carrinhos;
- `VFX_FLAME`, `VFX_PORTAL_CORE`, `VFX_CRYSTAL_CORE`, `VFX_LAVA` quando aplicável;
- `CAMERA_FOCUS` em hero props;
- `AUDIO_SOURCE` em objetos com som localizado.

### Estações

- `INTERACT_WORK`;
- socket específico da profissão;
- `OUTPUT_SPAWN`;
- `CAMERA_FOCUS`;
- `AUDIO_WORK`;
- `VFX_PRIMARY`;
- `VFX_SECONDARY`.

Sockets são transforms vazios no Blender/Unity, não geometria visível.

## 10. Colisão e navegação

- preferir BoxCollider e combinações simples;
- MeshCollider somente quando indispensável e, de preferência, em malha simplificada separada;
- portas e grades têm collider animável separado;
- props não devem criar pequenos bloqueios invisíveis;
- arena precisa de corredor de navegação legível;
- efeitos de lava não substituem a área lógica de dano;
- plataformas devem possuir bordas claras para gameplay.

## 11. Materiais e textura

- usar PBR estilizado, não textura fotográfica;
- concentrar desgaste em bordas e pontos de uso;
- manter texel density consistente dentro do kit;
- albedo sem iluminação direcional;
- emissão em máscara separada;
- usar material compartilhado por bioma sempre que possível;
- props menores podem compartilhar atlas;
- hero props podem receber material adicional quando justificado.

## 12. LOD e performance

- LOD0 conforme o catálogo;
- LOD1 em aproximadamente 50%;
- LOD2 em aproximadamente 20–25%;
- props muito pequenos podem usar apenas LOD0/LOD1;
- efeitos animados usam malha simples e shader;
- occlusion culling e batching são configurados no Unity;
- não gerar geometria interna invisível.

## 13. Fluxo Meshy → Unity

1. Aprovar concept sheet.
2. Gerar a peça isolada pelo método indicado.
3. Avaliar forma antes da textura.
4. Remesh no orçamento.
5. Unwrap UV após Remesh quando necessário.
6. Aplicar textura PBR sem luz assada.
7. Exportar FBX.
8. Corrigir escala, pivô, snap e sockets no Blender/Unity.
9. Criar materiais URP.
10. Adicionar colliders e LODGroup.
11. Criar prefab.
12. Testar encaixe e leitura em dispositivo mobile.

## 14. Critérios de aprovação

Um asset é aprovado somente quando:

- corresponde ao ID e à ficha;
- mantém escala e pivô;
- respeita a grade quando modular;
- não possui elementos desconectados acidentais;
- não contém VFX assado;
- diferencia materiais por albedo e roughness;
- permanece legível na câmera 3/4;
- atende ao orçamento de triângulos;
- possui collider planejado;
- encaixa com as peças relacionadas;
- não contém texto, logo ou watermark;
- foi testado com efeitos desligados.

## 15. Fora do escopo

A Task 018 não gera os modelos finais, não cria cenas completas, não instala SDK do Meshy, não produz VFX definitivos e não implementa scripts de crafting. Ela fornece especificação, prompts, matrizes e contrato de integração.

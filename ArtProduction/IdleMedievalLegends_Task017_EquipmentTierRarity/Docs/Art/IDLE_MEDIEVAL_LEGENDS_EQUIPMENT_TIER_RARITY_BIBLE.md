# Idle Medieval Legends — Equipment, Tier and Rarity Visual Bible

Versão: **Task 017 / v1.0**  
Escopo: **18 famílias principais × 9 Tiers × 6 raridades**  
Fontes superiores: `IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md`, Task 015 e Task 016.

---

## 1. Objetivo

Transformar Tier, raridade, profissão e slot em uma linguagem visual reproduzível para conceito, Meshy, texturização PBR e Unity. Esta Task gera **162 especificações de malha-base** e **972 variantes visuais lógicas**, sem exigir 972 malhas independentes.

## 2. Decisão central: Tier não é raridade

**Tier altera** tecnologia, material, construção, silhueta-base, orçamento estrutural e linguagem mágica.  
**Raridade altera** qualidade, acabamento, quantidade de afixos visuais, ornamentos, emissão e VFX.

Uma espada T1 Mítica continua sendo uma obra-prima extraordinária da tecnologia T1; ela não pode parecer uma arma divina T9. Uma espada T9 Comum continua usando materiais e construção T9, porém sem acabamento excepcional.

## 3. Estratégia de reutilização

1. Existe uma malha-base por `família + Tier`.
2. Comum e Incomum reutilizam integralmente a malha-base.
3. Raro pode adicionar um socket compacto de gema/runa.
4. Épico pode adicionar um conjunto pequeno de ornamentos.
5. Lendário usa attachments de prestígio, sem ampliar a silhueta em mais de 10%.
6. Mítico usa um attachment assinatura ou variação controlada de lâmina, cabeça, aro ou núcleo.
7. Emissão e partículas ficam em materiais/VFX Unity, não incorporadas como geometria caótica.
8. As variantes devem compartilhar UV e materiais sempre que possível.

## 4. Política de armaduras no MVP

As peças de armadura são produzidas como **props de inventário e referência de skin**. No MVP, elas não serão automaticamente conformadas e skinnadas individualmente ao corpo dos heróis. O visual corporal usa skins completas nos marcos T1, T3, T5, T7 e T9.

Isso evita clipping, incompatibilidade de skeleton, pesos de skin inconsistentes e explosão combinatória. Armas, escudos, arcos e cajados permanecem props visíveis separados.

## 5. Famílias principais

| Família | Slot | Profissão | Método | LOD0 | Textura |
|---|---|---|---|---:|---:|
| Espada de Uma Mão | Main Hand | Blacksmith | Text to 3D | 1,200–2,800 | 1024 |
| Escudo | Off Hand | Blacksmith | Text to 3D | 1,500–3,500 | 1024 |
| Arco Longo | Main Hand | Blacksmith + Tailor | Text to 3D | 1,000–2,400 | 1024 |
| Cajado | Main Hand | Enchanter + Blacksmith | Text to 3D | 1,400–3,400 | 1024 |
| Anel | Ring | Enchanter | Text to 3D | 300–800 | 512 |
| Amuleto | Amulet | Enchanter + Tailor | Text to 3D | 500–1,300 | 512 |
| Elmo Pesado | Head | Blacksmith | Image to 3D | 1,400–3,000 | 1024 |
| Peitoral Pesado | Chest | Blacksmith | Image to 3D | 2,500–5,000 | 1024 |
| Manoplas Pesadas | Hands | Blacksmith | Image to 3D | 900–2,200 | 1024 |
| Botas Pesadas | Feet | Blacksmith | Image to 3D | 1,100–2,400 | 1024 |
| Capuz Leve | Head | Tailor | Image to 3D | 900–2,000 | 1024 |
| Armadura Leve | Chest | Tailor | Image to 3D | 1,800–3,800 | 1024 |
| Luvas Leves | Hands | Tailor | Image to 3D | 700–1,700 | 1024 |
| Botas Leves | Feet | Tailor | Image to 3D | 900–2,000 | 1024 |
| Capuz Arcano | Head | Tailor + Enchanter | Image to 3D | 900–2,100 | 1024 |
| Manto Arcano | Chest | Tailor + Enchanter | Image to 3D | 2,200–4,500 | 1024 |
| Luvas Arcanas | Hands | Tailor + Enchanter | Image to 3D | 700–1,700 | 1024 |
| Botas Arcanas | Feet | Tailor + Enchanter | Image to 3D | 900–2,000 | 1024 |

## 6. Linguagem dos nove Tiers

| Tier | Nome | Metal | Tecido | Arcano | Forma | Raridade natural máxima |
|---:|---|---|---|---|---|---|
| T1 | Fronteira | ferro bruto e bronze simples | linho e lã áspera | quartzo pálido e runas quase ausentes | functional, compact, simple construction, visible fasteners | Rare |
| T2 | Ofício | ferro temperado e aço baixo carbono | lã tecida e linho reforçado | cristal menor e inscrições discretas | cleaner craftsmanship, reinforced edges, balanced proportions | Rare |
| T3 | Rúnico | aço polido e prataço | trama rúnica inicial | safira, núcleo rúnico e gravações visíveis | first intentional rune channels, stronger silhouettes, one focal inset | Epic |
| T4 | Mithril | mithril claro e prata élfica | seda refinada | cristal espiritual | lighter refined construction, elegant curves, disciplined ornament | Epic |
| T5 | Ancestral | adamantita e bronze ancestral | tecido rúnico denso | cristal antigo e glifos profundos | heavier prestige construction, carved ancestry motifs, broad protected cores | Legendary |
| T6 | Dracônico | aço dracônico e obsidiana metálica | trama de fênix | cristal prismático e calor interno | aggressive organic plates, scale rhythms, controlled heat channels | Legendary |
| T7 | Astral | oricalco e liga astral | seda astral | núcleo astral e energia violeta/ciano | unconventional celestial geometry, small suspended protected pieces, visible energy core | Legendary |
| T8 | Celestial | etério e liga celestial | trama celestial | prisma divino e luz sagrada | symmetric sacred construction, luminous inlays, elevated but robust silhouettes | Mythic |
| T9 | Criação | metal primordial e liga divina | trama do destino | coração da criação e energia iridescente | signature construction, impossible but readable material, one defining creation motif | Mythic |

A raridade natural máxima é uma recomendação de drop/crafting comum. Catalisadores, pity, receitas assinatura ou eventos podem ultrapassá-la conforme o GDD.

## 7. Linguagem das seis raridades

| Raridade | Geometria | Emissão | VFX | Afixos |
|---|---|---|---|---:|
| Comum | same base mesh; no extra attachment | none | none | 0 |
| Incomum | same base mesh; one small reinforced trim or stamped mark | none | none | 1 |
| Raro | base mesh plus one compact gem, rune plate or reinforced ornament | very low; one small rune or gem only | occasional inventory sparkle only | 2 |
| Épico | base mesh plus one medium ornament attachment and more developed rune channels | low and localized | subtle orbiting mote or short pulse in inspection | 3 |
| Lendário | prestige attachment set; silhouette may expand by at most 10 percent | medium but limited to maximum two zones | short aura close to asset; no silhouette obstruction | 4 |
| Mítico | signature attachment or controlled alternate head/blade/crest; preserve tier family identity | controlled animated core, edge or rune network; never whole-surface neon | signature localized effect and slow material motion | 5 + assinatura |

## 8. Matriz Tier × raridade

A combinação segue esta ordem:

```text
Malha e material tecnológico do Tier
→ acabamento da raridade
→ afixos visuais
→ emissão localizada
→ VFX separado
```

Nunca se deve aplicar primeiro uma estética genérica de raridade e depois tentar encaixar o Tier.

## 9. Profissões e autoria visual

- **Ferreiro:** armas metálicas, escudos, armaduras pesadas, rebites e estruturas.
- **Costureiro:** couros, tecidos, armaduras leves, mantos e estruturas flexíveis.
- **Encantador:** joias, núcleos, runas, sockets, raridade visual e VFX de inspeção.
- **Alquimista:** óleos, corantes, vernizes, catalisadores e tratamentos de superfície.
- **Coletador:** identidade e qualidade visual da matéria-prima bruta; não define sozinho a forma final.

Itens de Tier alto exibem colaboração entre profissões, mas devem manter um autor visual dominante.

## 10. Padrão técnico

- 1 Unity unit = 1 metro.
- Y para cima.
- Frente +Z quando aplicável.
- Armas: pivô no centro da empunhadura.
- Escudos: pivô na pega traseira.
- Joias: pivô geométrico ou alça superior.
- Props pareados: pivô central entre as duas peças.
- FBX para Unity.
- URP Lit ou shader especializado aprovado.
- LOD1: aproximadamente 50% de LOD0.
- LOD2: aproximadamente 25% de LOD0.
- Sem pedestal, fundo, texto, logo ou partes desconectadas sem função.

## 11. Sockets de raridade

Cada base pode preparar sockets opcionais:

```text
rarity_socket_primary
rarity_socket_secondary
gem_socket_primary
rune_plane_primary
vfx_anchor_core
vfx_anchor_edge
```

Esses sockets não precisam existir fisicamente em itens simples. Devem ser adicionados apenas quando a família realmente usa attachments.

## 12. Regras de emissão

- Comum e Incomum: nenhuma.
- Raro: um ponto muito pequeno.
- Épico: até duas linhas/runas discretas.
- Lendário: até duas zonas localizadas.
- Mítico: um núcleo, fio, gume ou rede controlada; nunca a superfície inteira.
- O objeto deve continuar legível com emissão desligada.

## 13. Regras para ícones de inventário

Cada prefab deve funcionar em câmera de estúdio 3/4, com fundo neutro e rotação controlada. O reconhecimento deve depender de silhueta, não de partículas. Props pareados devem ser compostos como um único display coerente.

## 14. Convenção de IDs

```text
equipment_<family>_t<NN>_base
equipment_<family>_t<NN>_<rarity>
mat_equipment_<family>_t<NN>_<rarity>
prefab_equipment_<family>_t<NN>_<rarity>
```

Exemplo:

```text
equipment_sword_1h_t06_base
equipment_sword_1h_t06_legendary
mat_equipment_sword_1h_t06_legendary
```

## 15. Fluxo de produção

```text
Concept sheet do base Tier
→ aprovação da silhueta
→ Meshy base geometry
→ Remesh e UV
→ textura Common
→ validação do material do Tier
→ variantes Uncommon/Rare/Epic/Legendary/Mythic
→ attachments controlados
→ URP/VFX
→ prefab e LODGroup
→ captura de ícone
→ QA mobile
```

## 16. Critérios de aceite

Um conjunto `família + Tier` é aprovado quando:

1. O Tier é reconhecível sem moldura de UI.
2. As seis raridades pertencem à mesma família.
3. Comum não parece inacabado.
4. Mítico não vira um objeto inteiramente neon.
5. A silhueta permanece legível em miniatura.
6. Escala e pivô estão corretos.
7. Não existem peças frágeis ou flutuantes sem função.
8. O número de materiais respeita o orçamento.
9. LODs preservam a silhueta.
10. A variante Mítica ainda comunica claramente o Tier de origem.

## 17. Entregas geradas

- 18 famílias.
- 9 Tiers.
- 162 linhas de produção base.
- 972 variantes lógicas de raridade.
- 162 prompts de concept sheet.
- 162 prompts de geometria Meshy.
- 162 prompts com as seis variantes de textura.

## 18. Fora do escopo

- Skinning modular automático de armaduras.
- Geração dos modelos finais.
- VFX definitivos.
- Ícones 2D finais.
- Balanceamento numérico de atributos.
- Famílias adicionais como machado, lança, adaga e besta; serão expansão posterior.

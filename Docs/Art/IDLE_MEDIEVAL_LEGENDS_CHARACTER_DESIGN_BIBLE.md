# Idle Medieval Legends — Character, Enemy and Boss Design Bible

Versão: **Task 016 / v1.0**  
Escopo: **três heróis iniciais + facção completa dos Clãs Goblins**  
Fonte superior: `IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md`

---

## 1. Objetivo

Converter a direção macro da Task 014 e o catálogo técnico da Task 015 em fichas reproduzíveis para conceito, Meshy, rig, animação e integração no Unity.

Esta Task fixa **identidade, anatomia, silhueta, paleta, materiais e separação de peças**. Ela não aprova automaticamente a primeira imagem ou a primeira malha gerada. Cada asset ainda passa pelo processo `Draft → Under Review → Approved`.

## 2. Decisões vinculantes

1. O MVP usa modelos completos por herói, com armas e escudos separados.
2. Todas as unidades humanoides são entregues em A-pose, frente +Z e pivô entre os pés.
3. Silhueta e leitura em 128 px têm prioridade sobre microdetalhes.
4. Goblins são ameaçadores e funcionais, não mascotes infantis.
5. A facção evolui visualmente de improvisação para disciplina, ritual e realeza corrompida.
6. Armas, escudos, cajados, arcos, coroas importantes e relíquias não podem nascer fundidos ao corpo.
7. Emissão é um recurso de hierarquia; não substitui material, forma ou cor.
8. Sexualização de armadura não faz parte da direção visual.

## 3. Relação visual entre os heróis

| Herói | Forma | Peso visual | Ponto focal | Movimento esperado |
|---|---|---|---|---|
| Paladino | quadrados e arcos | pesado e estável | escudo sagrado | bloqueios e ataques curtos |
| Arqueira | curvas e diagonais | leve e assimétrico | arco/capuz | deslocamento e liberação rápida |
| Mago | círculos e verticais | amplo na base | cristal do cajado | gestos largos e casts controlados |

## 4. Progressão visual dos Clãs Goblins

| Camada | Unidades | Regra visual |
|---|---|---|
| Comuns | Saqueador, Guerreiro, Arqueiro | materiais improvisados, assimetria, pouco acabamento |
| Especial | Xamã | materiais naturais, ritual simples, emissão mínima |
| Mid-boss | Chefe | volume maior, troféus e autoridade tribal |
| Elites | Hobgoblin, Patrulheiro, Alto Xamã | especialização clara, construção mais competente |
| Final boss | Rei Goblin | realeza saqueada, arma assinatura e corrupção necromântica |

## 5. Matriz de diferenciação

| Unidade | Altura | Arma dominante | Elemento secundário | Leitura instantânea |
|---|---:|---|---|---|
| Paladino | 1.90 m | one-handed sword | sacred sun-and-bastion emblem on shield and chest clasp | wide triangular silhouette anchored by shield; head always visible between moderate pauldrons |
| Arqueira | 1.76 m | stylized recurve bow | bow profile and hood/hair shape | narrow diagonal silhouette with asymmetric short cape and strong bow curve |
| Mago | 1.82 m | tall arcane staff | faceted cyan staff crystal framed by a bronze ring | vertical silhouette, broad layered robe base, staff as dominant parallel line |
| Goblin Saqueador | 1.25 m | scrap knife | backpack silhouette and alert oversized ears | small irregular silhouette with oversized patched backpack and short knife |
| Guerreiro Goblin | 1.30 m | goblin cleaver | asymmetric shield and cleaver pairing | wide shield-side silhouette with short cleaver profile and patched armor blocks |
| Arqueiro Goblin | 1.25 m | shortbow | shortbow and tall quiver silhouette | large shortbow curve, oversized quiver and inclined hood/ear profile |
| Xamã Goblin | 1.25 m | shaman totem staff | staff totem face and controlled green-yellow magical core | vertical totem staff, irregular feather crown and hanging ritual shapes |
| Chefe Goblin | 1.58 m | wide chieftain axe | wide axe head and crown/trophy shoulder line | broad asymmetrical trophy silhouette with oversized axe and tribal crown band |
| Cavaleiro Hobgoblin | 1.85 m | hobgoblin sword | large tower shield and upright disciplined posture | tower shield rectangle, disciplined helmet line and upright military stance |
| Patrulheiro Goblin | 1.35 m | goblin longbow | longbow and trap satchel silhouette | longbow curve, short shoulder cape and visible trap satchel shapes |
| Alto Xamã Goblin | 1.42 m | large forked high-shaman totem | mask and split-color totem core showing lime spirit energy touched by violet corruption | large ritual mask, tall forked totem and two controlled hanging charm clusters |
| Rei Goblin | 1.78 m | signature king blade | necromantic crown fragment framed by tarnished gold and royal red | irregular stolen-metal crown, royal shoulder mass, signature blade and necromantic crown fragment above chest or belt |

## 6. Fichas individuais

### 6.1 Paladino — `hero_paladin_001`

**Nome em inglês:** Paladin  
**Facção/categoria:** Heroes / Playable Hero  
**Função:** Tank / protector  
**Escala:** 1.90 m  
**Proporção:** 6.4 cabeças  
**Rig:** `rig_hero_standard_humanoid`  
**Animações:** `anim_hero_tank_humanoid`  
**LOD0:** 5,000–8,000 triângulos  
**Textura:** 2048×2048  
**Método recomendado:** Multi-view Image to 3D

**Função narrativa**

A frontier guardian who turns faith into discipline. His equipment was forged for rescue and endurance rather than ceremonial display.

**Anatomia e postura**

robust heroic human male; broad shoulders; thick torso; stable stance.

**Silhueta e formas**

wide triangular silhouette anchored by shield; head always visible between moderate pauldrons. Linguagem dominante: squares, rectangles and broad protective arcs.

**Roupa/armadura**

full stylized plate armor with layered chest plate, medium pauldrons, reinforced boots, short split mantle, visible face, no full helmet in base skin.

**Paleta e materiais**

Paleta: medieval blue #315A7D; ivory #D7C49A; cold steel #58636A; selective gold #D8A84E. Materiais: brushed cold steel, painted blue plate, ivory cloth, dark brown leather, restrained polished gold.

**Ponto focal e emissão**

Ponto focal: sacred sun-and-bastion emblem on shield and chest clasp. Emissão: small warm-gold rune on shield, maximum two emissive areas.

**Peças separadas**

body; sword; shield; optional mantle physics proxy. Props: one-handed sword; large rounded kite shield; both separate; optional short back mantle.

**Sockets necessários**

`hand_r_weapon; hand_l_shield; back_weapon; chest_fx; head_fx; feet_fx`

**Critérios de aprovação**

Recognizable as tank at 128 px; head unobstructed; shield does not hide the entire torso; sword and shield detachable; limbs deform cleanly; no thin floating filigree; palette matches approved swatches.

**Prompt de conceito**

`Prompts/ConceptArt/hero_paladin_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/hero_paladin_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/hero_paladin_001_texture.txt`

### 6.2 Arqueira — `hero_archer_001`

**Nome em inglês:** Archer  
**Facção/categoria:** Heroes / Playable Hero  
**Função:** Physical ranged damage  
**Escala:** 1.76 m  
**Proporção:** 6.9 cabeças  
**Rig:** `rig_hero_standard_humanoid`  
**Animações:** `anim_hero_ranged_humanoid`  
**LOD0:** 5,000–8,000 triângulos  
**Textura:** 2048×2048  
**Método recomendado:** Multi-view Image to 3D

**Função narrativa**

A border scout who reads terrain before enemies realize they have been seen. Every strap and pouch has a practical use.

**Anatomia e postura**

athletic heroic human female; long limbs; practical build; balanced stance.

**Silhueta e formas**

narrow diagonal silhouette with asymmetric short cape and strong bow curve. Linguagem dominante: long curves, controlled diagonals and light triangular layers.

**Roupa/armadura**

practical layered leather armor, fitted but non-sexualized, bracers, reinforced boots, short asymmetric hooded cape, clear arm movement.

**Paleta e materiais**

Paleta: forest green #536A3B; leather brown #7A4C2D; linen beige #C8BFA9; dull steel #58636A. Materiais: layered matte leather, woven linen, dark wood, dull steel buckles, restrained feathers.

**Ponto focal e emissão**

Ponto focal: bow profile and hood/hair shape. Emissão: none in base model; ability VFX remains separate.

**Peças separadas**

body; bow; quiver; grouped arrow set; optional hood-down hair variant. Props: stylized recurve bow; separate quiver; arrows as grouped prop; small utility knife optional.

**Sockets necessários**

`hand_l_bow; hand_r_arrow; back_quiver; hip_tool; chest_fx; feet_fx`

**Critérios de aprovação**

Recognizable as ranged hero at 128 px; bow is thick enough for mobile silhouette; shoulders and elbows have clear deformation space; quiver does not intersect cape; outfit remains practical and non-sexualized.

**Prompt de conceito**

`Prompts/ConceptArt/hero_archer_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/hero_archer_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/hero_archer_001_texture.txt`

### 6.3 Mago — `hero_mage_001`

**Nome em inglês:** Mage  
**Facção/categoria:** Heroes / Playable Hero  
**Função:** Arcane area damage  
**Escala:** 1.82 m  
**Proporção:** 6.6 cabeças  
**Rig:** `rig_hero_standard_humanoid`  
**Animações:** `anim_hero_caster_humanoid`  
**LOD0:** 5,000–8,000 triângulos  
**Textura:** 2048×2048  
**Método recomendado:** Multi-view Image to 3D

**Função narrativa**

A disciplined scholar of practical battlefield magic. His robes are designed for travel and controlled casting, not court ceremony.

**Anatomia e postura**

mature heroic human male; lean frame; calm upright posture; expressive hands.

**Silhueta e formas**

vertical silhouette, broad layered robe base, staff as dominant parallel line. Linguagem dominante: circles, diamonds, vertical panels and broad cloth shapes.

**Roupa/armadura**

layered knee-length arcane coat over split robe panels, reinforced boots, wide belt with focus, high collar below jaw, cloth separated from legs.

**Paleta e materiais**

Paleta: deep blue #233A5E; graphite #2F343B; bronze #A56A3A; arcane cyan #25A7FF. Materiais: heavy matte fabric, layered woven cloth, aged bronze, dark leather belts, translucent cyan crystal.

**Ponto focal e emissão**

Ponto focal: faceted cyan staff crystal framed by a bronze ring. Emissão: staff crystal plus two or three small rune marks only.

**Peças separadas**

body; staff; belt focus; optional spellbook. Props: tall arcane staff; belt focus crystal; optional closed spellbook prop.

**Sockets necessários**

`hand_r_staff; hand_l_focus; back_staff; chest_fx; hand_fx_l; hand_fx_r`

**Critérios de aprovação**

Readable caster silhouette at 128 px; staff crystal is clear focal point; cloth has rig-safe splits; emission remains under 10% of visible surface; no floating pieces required to understand the model.

**Prompt de conceito**

`Prompts/ConceptArt/hero_mage_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/hero_mage_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/hero_mage_001_texture.txt`

### 6.4 Goblin Saqueador — `enemy_goblin_scavenger_001`

**Nome em inglês:** Goblin Scavenger  
**Facção/categoria:** Goblin Clans / Common Enemy  
**Função:** Basic melee / resource thief  
**Escala:** 1.25 m  
**Proporção:** 4.7 cabeças  
**Rig:** `rig_goblin_small_humanoid`  
**Animações:** `anim_enemy_goblin_melee`  
**LOD0:** 3,000–5,000 triângulos  
**Textura:** 1024×1024  
**Método recomendado:** Image to 3D

**Função narrativa**

The lowest-ranked clan member, sent ahead to steal tools, food and anything that can be sold or repurposed.

**Anatomia e postura**

small wiry goblin; long forearms; large ears; slightly hunched but riggable biped.

**Silhueta e formas**

small irregular silhouette with oversized patched backpack and short knife. Linguagem dominante: irregular triangles, sacks and crooked rectangles.

**Roupa/armadura**

simple patched tunic, rope belt, one shoulder pad made from scrap, open sandals or wrapped feet.

**Paleta e materiais**

Paleta: olive skin #6E7C3C; dirty tan #A88C5A; leather brown #6A4128; rust iron #7B4A32. Materiais: patched cloth, coarse sacks, splintered wood, worn leather, rusty scrap iron.

**Ponto focal e emissão**

Ponto focal: backpack silhouette and alert oversized ears. Emissão: none.

**Peças separadas**

body; scrap knife; backpack optional as separate skinned/accessory part. Props: scrap knife; patched backpack; loose stolen trinket bundle.

**Sockets necessários**

`hand_r_weapon; back_pack; hip_loot; head_fx; feet_fx`

**Critérios de aprovação**

Reads as lowest-tier scavenger; remains threatening rather than adorable; backpack and ears create silhouette; feet and hands are riggable; no prop intersects elbow range.

**Prompt de conceito**

`Prompts/ConceptArt/enemy_goblin_scavenger_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/enemy_goblin_scavenger_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/enemy_goblin_scavenger_001_texture.txt`

### 6.5 Guerreiro Goblin — `enemy_goblin_warrior_001`

**Nome em inglês:** Goblin Warrior  
**Facção/categoria:** Goblin Clans / Common Enemy  
**Função:** Frontline melee  
**Escala:** 1.30 m  
**Proporção:** 4.8 cabeças  
**Rig:** `rig_goblin_small_humanoid`  
**Animações:** `anim_enemy_goblin_melee`  
**LOD0:** 3,000–5,000 triângulos  
**Textura:** 1024×1024  
**Método recomendado:** Image to 3D

**Função narrativa**

A clan fighter equipped with whatever the mines and raids provide. He values intimidation and durability over balance.

**Anatomia e postura**

compact muscular goblin; thick forearms; stable forward stance.

**Silhueta e formas**

wide shield-side silhouette with short cleaver profile and patched armor blocks. Linguagem dominante: broad triangles, crooked squares and shield arcs.

**Roupa/armadura**

improvised chest plate, one heavy shoulder guard, padded loin tunic, wrapped shins, crude helmet cap optional but face visible.

**Paleta e materiais**

Paleta: olive skin #68773A; dark brown #4F3425; dirty red #8A3F2C; rust iron #7B4A32. Materiais: wood plank shield, scrap iron plates, cracked leather, coarse red cloth.

**Ponto focal e emissão**

Ponto focal: asymmetric shield and cleaver pairing. Emissão: none.

**Peças separadas**

body; cleaver; wooden shield. Props: goblin cleaver; wooden round shield reinforced with scrap metal.

**Sockets necessários**

`hand_r_weapon; hand_l_shield; back_weapon; head_fx; feet_fx`

**Critérios de aprovação**

Clearly stronger than Scavenger but below elites; shield side remains readable; armor is improvised; face and ears visible; weapon sockets align with animation set.

**Prompt de conceito**

`Prompts/ConceptArt/enemy_goblin_warrior_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/enemy_goblin_warrior_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/enemy_goblin_warrior_001_texture.txt`

### 6.6 Arqueiro Goblin — `enemy_goblin_archer_001`

**Nome em inglês:** Goblin Archer  
**Facção/categoria:** Goblin Clans / Common Enemy  
**Função:** Basic ranged damage  
**Escala:** 1.25 m  
**Proporção:** 4.8 cabeças  
**Rig:** `rig_goblin_small_humanoid`  
**Animações:** `anim_enemy_goblin_ranged`  
**LOD0:** 3,000–5,000 triângulos  
**Textura:** 1024×1024  
**Método recomendado:** Image to 3D

**Função narrativa**

Poorly trained but numerous, these archers rely on volleys, elevated ledges and cheap arrows.

**Anatomia e postura**

lean small goblin; narrow shoulders; long arms; forward-leaning but rig-safe posture.

**Silhueta e formas**

large shortbow curve, oversized quiver and inclined hood/ear profile. Linguagem dominante: curves, thin diagonals and compact triangular cloth.

**Roupa/armadura**

light jerkin, one short hood panel between ears, forearm wrap, utility pouch, no heavy armor.

**Paleta e materiais**

Paleta: yellow-olive skin #768342; faded green #5E6A37; leather #70472C; pale wood #9A7749. Materiais: rough leather, faded cloth, bent wood, rope, dull iron arrowheads.

**Ponto focal e emissão**

Ponto focal: shortbow and tall quiver silhouette. Emissão: none.

**Peças separadas**

body; shortbow; quiver; grouped arrows. Props: shortbow; oversized quiver; grouped arrows.

**Sockets necessários**

`hand_l_bow; hand_r_arrow; back_quiver; hip_pouch; head_fx`

**Critérios de aprovação**

Distinct from Ranger through lower complexity and shortbow scale; clear draw-arm space; quiver avoids shoulder clipping; remains readable at battle camera distance.

**Prompt de conceito**

`Prompts/ConceptArt/enemy_goblin_archer_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/enemy_goblin_archer_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/enemy_goblin_archer_001_texture.txt`

### 6.7 Xamã Goblin — `enemy_goblin_shaman_001`

**Nome em inglês:** Goblin Shaman  
**Facção/categoria:** Goblin Clans / Special Enemy  
**Função:** Caster / healer / buffer  
**Escala:** 1.25 m  
**Proporção:** 4.7 cabeças  
**Rig:** `rig_goblin_small_humanoid`  
**Animações:** `anim_enemy_goblin_caster`  
**LOD0:** 3,500–5,500 triângulos  
**Textura:** 1024×1024  
**Método recomendado:** Image to 3D

**Função narrativa**

A clan healer and omen-reader who channels fungi, cave spirits and ancestral superstition into real battlefield magic.

**Anatomia e postura**

thin older goblin; slightly bowed neck; long expressive hands; riggable legs.

**Silhueta e formas**

vertical totem staff, irregular feather crown and hanging ritual shapes. Linguagem dominante: irregular circles, crooked verticals and natural totem forms.

**Roupa/armadura**

layered hide shawl, ritual belt, small bone charms, feather cluster that does not obscure ears or rig.

**Paleta e materiais**

Paleta: moss green skin #65783A; ochre #B28A42; bone #C9B98F; ritual lime #A2D729. Materiais: bone, carved wood, feathers, rough hide, gourds, woven cords.

**Ponto focal e emissão**

Ponto focal: staff totem face and controlled green-yellow magical core. Emissão: one small lime core in staff plus subtle eye/rune accents.

**Peças separadas**

body; totem staff; belt gourd; optional feather accessory. Props: shaman totem staff; small charm gourd; optional belt pouch.

**Sockets necessários**

`hand_r_staff; hand_l_focus; back_totem; head_fx; hand_fx_l; hand_fx_r`

**Critérios de aprovação**

Caster role readable without VFX; staff remains primary focal point; accessories do not create fragile geometry; silhouette differs from High Shaman through lower height and simpler maskless head.

**Prompt de conceito**

`Prompts/ConceptArt/enemy_goblin_shaman_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/enemy_goblin_shaman_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/enemy_goblin_shaman_001_texture.txt`

### 6.8 Chefe Goblin — `boss_goblin_chieftain_001`

**Nome em inglês:** Goblin Chieftain  
**Facção/categoria:** Goblin Clans / Mid-Boss  
**Função:** Commander / bruiser  
**Escala:** 1.58 m  
**Proporção:** 5.2 cabeças  
**Rig:** `rig_goblin_boss_humanoid`  
**Animações:** `anim_boss_goblin_chieftain`  
**LOD0:** 6,000–9,000 triângulos  
**Textura:** 2048×2048  
**Método recomendado:** Multi-view Image to 3D

**Função narrativa**

The mine-camp commander who keeps rival goblins obedient through strength, trophies and the promise of the King’s favor.

**Anatomia e postura**

large broad goblin; thick neck and torso; powerful arms; boss-scale hands.

**Silhueta e formas**

broad asymmetrical trophy silhouette with oversized axe and tribal crown band. Linguagem dominante: heavy triangles, broken crowns, trophy spikes and broad leather masses.

**Roupa/armadura**

reinforced scrap cuirass, fur shoulder, trophy belt, short war skirt, irregular tribal crown band, scars stylized rather than graphic.

**Paleta e materiais**

Paleta: dark olive skin #55652F; burnt red #923D2C; blackened iron #3E4142; bone #C9B98F. Materiais: blackened scrap iron, thick leather, fur patches, bone trophies, chipped painted wood.

**Ponto focal e emissão**

Ponto focal: wide axe head and crown/trophy shoulder line. Emissão: none or a tiny ember-like war charm only.

**Peças separadas**

body; wide axe; trophy chain/banner; optional crown band. Props: wide chieftain axe; trophy chain or banner fragment.

**Sockets necessários**

`hand_r_weapon; back_banner; waist_trophy; chest_fx; head_fx; feet_fx`

**Critérios de aprovação**

Immediately reads as mid-boss and commander; at least 20% larger than common goblins; limbs remain animation-friendly; trophies are chunky and limited; does not visually surpass Goblin King.

**Prompt de conceito**

`Prompts/ConceptArt/boss_goblin_chieftain_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/boss_goblin_chieftain_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/boss_goblin_chieftain_001_texture.txt`

### 6.9 Cavaleiro Hobgoblin — `enemy_hobgoblin_knight_001`

**Nome em inglês:** Hobgoblin Knight  
**Facção/categoria:** Goblin Clans / Elite Enemy  
**Função:** Tank / control  
**Escala:** 1.85 m  
**Proporção:** 5.8 cabeças  
**Rig:** `rig_hobgoblin_large_humanoid`  
**Animações:** `anim_enemy_humanoid_melee`  
**LOD0:** 5,000–8,000 triângulos  
**Textura:** 1024×1024  
**Método recomendado:** Multi-view Image to 3D

**Função narrativa**

Unlike chaotic goblins, hobgoblins drill as soldiers. They form the King’s reliable defensive line.

**Anatomia e postura**

tall disciplined hobgoblin; broad shoulders; long powerful legs; less hunched than goblins.

**Silhueta e formas**

tower shield rectangle, disciplined helmet line and upright military stance. Linguagem dominante: rectangles, shield slabs, controlled angular plates.

**Roupa/armadura**

organized heavy armor assembled from captured human pieces, standardized red tabard, open-face helmet, reinforced greaves.

**Paleta e materiais**

Paleta: deep olive skin #59652F; blackened steel #3E4142; muted red #7B3428; brass #A06B35. Materiais: blackened steel plates, battered brass, thick leather, dense military cloth.

**Ponto focal e emissão**

Ponto focal: large tower shield and upright disciplined posture. Emissão: none.

**Peças separadas**

body; sword; tower shield. Props: hobgoblin sword; tower shield.

**Sockets necessários**

`hand_r_weapon; hand_l_shield; back_weapon; head_fx; feet_fx`

**Critérios de aprovação**

Clearly a different military caste; taller than heroes’ waistline and common goblins; armor appears standardized; tower shield can animate without leg collision; face remains goblinoid.

**Prompt de conceito**

`Prompts/ConceptArt/enemy_hobgoblin_knight_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/enemy_hobgoblin_knight_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/enemy_hobgoblin_knight_001_texture.txt`

### 6.10 Patrulheiro Goblin — `enemy_goblin_ranger_001`

**Nome em inglês:** Goblin Ranger  
**Facção/categoria:** Goblin Clans / Elite Enemy  
**Função:** Precision ranged / traps / mobility  
**Escala:** 1.35 m  
**Proporção:** 5.0 cabeças  
**Rig:** `rig_goblin_small_humanoid`  
**Animações:** `anim_enemy_goblin_ranged`  
**LOD0:** 5,000–8,000 triângulos  
**Textura:** 1024×1024  
**Método recomendado:** Multi-view Image to 3D

**Função narrativa**

Veteran trackers who patrol hidden shafts and ambush routes. They use fewer arrows and far better aim than common archers.

**Anatomia e postura**

lean experienced goblin; slightly taller than archer; balanced and alert.

**Silhueta e formas**

longbow curve, short shoulder cape and visible trap satchel shapes. Linguagem dominante: long diagonals, curved bow limbs and controlled asymmetry.

**Roupa/armadura**

layered ranger jerkin, short shoulder cape, bracers, knee protection, compact trap satchels and rope coil.

**Paleta e materiais**

Paleta: dark moss #465A32; leather #5E3C28; charcoal #343A37; amber accent #C79038. Materiais: treated leather, dark cloth, flexible wood, bronze trap components, rope.

**Ponto focal e emissão**

Ponto focal: longbow and trap satchel silhouette. Emissão: none; trap VFX separate.

**Peças separadas**

body; longbow; compact quiver; trap satchel. Props: goblin longbow; trap satchel; compact quiver.

**Sockets necessários**

`hand_l_bow; hand_r_arrow; back_quiver; hip_trap_l; hip_trap_r; head_fx`

**Critérios de aprovação**

Instantly distinguishable from Archer by longbow, darker palette and trap shapes; elite without looking magical; satchels do not block hip or leg animation.

**Prompt de conceito**

`Prompts/ConceptArt/enemy_goblin_ranger_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/enemy_goblin_ranger_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/enemy_goblin_ranger_001_texture.txt`

### 6.11 Alto Xamã Goblin — `enemy_goblin_high_shaman_001`

**Nome em inglês:** Goblin High Shaman  
**Facção/categoria:** Goblin Clans / Elite Enemy  
**Função:** Advanced caster / summoner / support  
**Escala:** 1.42 m  
**Proporção:** 5.0 cabeças  
**Rig:** `rig_goblin_boss_humanoid`  
**Animações:** `anim_enemy_goblin_caster`  
**LOD0:** 5,000–8,000 triângulos  
**Textura:** 1024×1024  
**Método recomendado:** Multi-view Image to 3D

**Função narrativa**

The King’s spiritual adviser, increasingly influenced by the necromantic crown fragment. He translates whispers into ritual power.

**Anatomia e postura**

tall thin goblin elder; straight ceremonial posture; long expressive arms.

**Silhueta e formas**

large ritual mask, tall forked totem and two controlled hanging charm clusters. Linguagem dominante: concentric ritual circles, forked verticals and asymmetric natural forms.

**Roupa/armadura**

layered ritual mantle above knees, carved mask exposing eyes and ears, two charm clusters, reinforced ceremonial belt.

**Paleta e materiais**

Paleta: dark moss skin #53652F; ochre #B28A42; bone #C9B98F; violet corruption #7547C7; lime spirit #A2D729. Materiais: carved wood, lacquered ritual mask, bone, feathers, woven cords, crystal fragments.

**Ponto focal e emissão**

Ponto focal: mask and split-color totem core showing lime spirit energy touched by violet corruption. Emissão: limited lime and violet accents on totem core, mask eyes remain mostly dark.

**Peças separadas**

body; large totem; optional detachable mask; charm cluster accessories. Props: large forked high-shaman totem; ritual mask detachable if pipeline allows.

**Sockets necessários**

`hand_r_staff; hand_l_focus; back_totem; mask_socket; head_fx; hand_fx_l; hand_fx_r`

**Critérios de aprovação**

Reads as advanced caster and narrative corruption bridge; clearly more formal than basic Shaman; emission stays controlled; mask and charms remain chunky, rig-safe and culturally fictional.

**Prompt de conceito**

`Prompts/ConceptArt/enemy_goblin_high_shaman_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/enemy_goblin_high_shaman_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/enemy_goblin_high_shaman_001_texture.txt`

### 6.12 Rei Goblin — `boss_goblin_king_001`

**Nome em inglês:** Goblin King  
**Facção/categoria:** Goblin Clans / Final Boss  
**Função:** Multi-phase commander / bruiser / summoner  
**Escala:** 1.78 m  
**Proporção:** 5.7 cabeças  
**Rig:** `rig_goblin_boss_humanoid`  
**Animações:** `anim_boss_goblin_king`  
**LOD0:** 8,000–15,000 triângulos  
**Textura:** 2048×2048  
**Método recomendado:** Multi-view Image to 3D

**Função narrativa**

The first goblin to unite the frontier clans. His authority comes from strategy and conquest, but the stolen crown fragment is changing both his kingdom and his body.

**Anatomia e postura**

massive regal goblin; broad torso; powerful legs; commanding upright posture; not comically obese.

**Silhueta e formas**

irregular stolen-metal crown, royal shoulder mass, signature blade and necromantic crown fragment above chest or belt. Linguagem dominante: dominant crown triangles, broad royal arcs, heavy blade diagonal and one controlled supernatural focal shape.

**Roupa/armadura**

asymmetric royal armor assembled from conquered human and goblin pieces, broad but head-safe pauldrons, short split royal cloak, heavy belt shrine holding crown fragment.

**Paleta e materiais**

Paleta: dark green skin #46552A; black bronze #4A3A2A; royal red #8F2F35; tarnished gold #B8873E; corruption violet #7547C7. Materiais: stolen royal plate, tarnished gold, black bronze, heavy red cloth, dark leather, necromantic crystal.

**Ponto focal e emissão**

Ponto focal: necromantic crown fragment framed by tarnished gold and royal red. Emissão: violet only on crown fragment and thin veins on signature blade during phase change.

**Peças separadas**

body; king blade; crown; crown fragment; optional command banner. Props: signature king blade; irregular crown; necromantic crown fragment; optional command banner.

**Sockets necessários**

`hand_r_weapon; crown_socket; chest_relic; back_banner; head_fx; chest_fx; feet_fx`

**Critérios de aprovação**

Dominates all goblin silhouettes without becoming an orc; 35–45% larger than common goblins; crown, blade and relic remain distinct; supports multi-phase animation; emission communicates corruption without erasing material readability.

**Prompt de conceito**

`Prompts/ConceptArt/boss_goblin_king_001_concept_sheet.txt`

**Prompt Meshy**

`Prompts/Meshy/boss_goblin_king_001_meshy_character.txt`

**Prompt de textura**

`Prompts/Texture/boss_goblin_king_001_texture.txt`

## 7. Ordem de produção recomendada

### Lote A — prova de estilo e pipeline

1. Paladino.
2. Goblin Saqueador.
3. Goblin Guerreiro.
4. Arqueira.
5. Mago.

Esse lote valida corpo humano, goblin pequeno, armadura pesada, couro, tecido, armas separadas e retarget básico.

### Lote B — combate ranged e caster

1. Goblin Arqueiro.
2. Goblin Xamã.
3. Goblin Patrulheiro.
4. Alto Xamã.

### Lote C — elites e bosses

1. Chefe Goblin.
2. Cavaleiro Hobgoblin.
3. Rei Goblin.

## 8. Processo de aprovação por personagem

1. Aprovar o concept sheet multi-view.
2. Aprovar silhueta preta em tamanho mobile.
3. Aprovar armas e acessórios separados.
4. Gerar 3D sem textura e revisar anatomia.
5. Executar Remesh no orçamento.
6. Corrigir topologia e UV quando necessário.
7. Texturizar conforme prompt aprovado.
8. Aplicar rig da família.
9. Testar animações extremas.
10. Importar em cena de batalha com câmera real.
11. Aprovar LODs e material URP.

## 9. Reprovação automática

- arma fundida à mão;
- roupa ou capa fundida entre as pernas;
- diferença relevante de roupa entre vistas do concept sheet;
- silhueta ilegível em 128 px;
- rosto escondido sem decisão explícita;
- emissão em mais de 15% da superfície visível;
- ornamentos ultrafinos;
- excesso de materiais;
- personagem comum visualmente mais complexo que boss;
- goblin com leitura infantil ou cômica extrema;
- anatomia incapaz de receber o rig definido.

## 10. Entregáveis da Task 016

- 12 fichas estruturadas;
- 36 prompts: conceito, malha e textura;
- CSV de produção;
- padrão de rig e sockets;
- prompt Codex para integração;
- template reutilizável;
- validação automática e manifesto.

## 11. Fora do escopo

- fichas completas dos mortos-vivos e demônios;
- geração/aprovação das imagens finais;
- malhas 3D finais;
- animações produzidas;
- equipamentos T1–T9;
- ambientes e estações;
- VFX finais.

## 12. Próxima etapa

A Task 017 deve formalizar equipamentos, Tiers e raridades. Em paralelo, o Lote A desta Task pode entrar em geração de concept art e Meshy.

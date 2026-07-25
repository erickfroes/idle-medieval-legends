# Idle Medieval Legends — Padrão de Rig, Sockets e Separação de Peças

Versão: **Task 016 / v1.0**

## 1. Convenções

```text
1 Unity unit = 1 metro
Y = vertical
+Z = frente
Pivô = centro entre os pés no chão
Pose de entrega = A-pose neutra
```

## 2. Famílias de rig

| Perfil | Uso | Observação |
|---|---|---|
| `rig_hero_standard_humanoid` | Paladino, Arqueira e Mago | Unity Humanoid; proporções humanas estilizadas compartilhadas |
| `rig_goblin_small_humanoid` | Saqueador, Guerreiro, Arqueiro, Xamã e Patrulheiro | Esqueleto bípede curto; braços mais longos; retarget apenas dentro da família |
| `rig_goblin_boss_humanoid` | Chefe, Alto Xamã e Rei | Base goblin ampliada; permite mãos maiores e torso mais largo |
| `rig_hobgoblin_large_humanoid` | Cavaleiro Hobgoblin | Estrutura mais próxima de humano, porém com cabeça, mãos e pernas goblinoides |

Não se deve forçar retarget perfeito entre heróis humanos e goblins pequenos. As bibliotecas podem compartilhar intenção e timing, mas precisam de correção por família.

## 3. Sockets mínimos

```text
root
pelvis
spine_01
spine_02
chest
neck
head
hand_r_weapon
hand_l_weapon
hand_l_shield
back_weapon
back_quiver
back_pack
hip_tool
hip_trap_l
hip_trap_r
chest_fx
head_fx
hand_fx_l
hand_fx_r
feet_fx
```

Sockets opcionais são criados somente quando usados por um asset aprovado. Não criar dezenas de transforms vazios sem necessidade.

## 4. Peças separadas

Devem permanecer separadas do corpo:

- armas;
- escudos;
- cajados;
- arcos;
- aljavas quando houver troca visual;
- coroas e máscaras quando houver animação ou variação;
- relíquias de chefe;
- banners;
- VFX e auras.

Capas curtas podem ser skinned ao corpo ou usar uma malha secundária. Capas longas não são permitidas no MVP.

## 5. Regras de deformação

- ombros não podem atravessar peitorais em 90° de elevação;
- cotovelos precisam de loops limpos;
- joelhos precisam dobrar sem colapsar botas;
- dedos podem ser simplificados, mas polegar deve permitir empunhadura;
- o pescoço deve girar sem colidir com ombreiras ou gola;
- o tecido entre as pernas deve ser dividido;
- mochilas e aljavas não podem bloquear toda a rotação do ombro;
- máscaras e coroas precisam acompanhar a cabeça sem penetrar orelhas.

## 6. Política de root motion

O vertical slice usa deslocamento controlado pelo jogo. Animações de combate devem ser entregues preferencialmente **in-place**, com versão root-motion apenas quando um ataque específico exigir avanço medido. Todo clipe deve registrar o frame lógico de impacto.

## 7. Eventos de animação

Nomes sugeridos:

```text
attack_windup_start
attack_release
projectile_spawn
hit_frame
cast_release
footstep_l
footstep_r
death_commit
phase_change
```

Eventos não resolvem regras de combate. Eles sincronizam a reprodução visual com o `CombatEvent` já calculado.

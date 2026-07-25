# Idle Medieval Legends — Modular Environment Standard

## Grade e unidades

- 1 Unity unit = 1 metro.
- Grade estrutural: 4m.
- Meio módulo permitido: 2m quando explicitamente catalogado.
- Props livres: incrementos de 0,25m.
- Pivô padrão: centro da base, Y=0.
- Frente significativa: +Z.

## Tipos de peça

### Tile

- footprint exato de 4m × 4m;
- bordas sem pedras salientes que impeçam o encaixe;
- variação superficial concentrada longe da borda;
- collider plano ou caixa rasa.

### Wall

- 4m de largura;
- espessura consistente dentro do kit;
- plano de encaixe limpo nas laterais;
- ornamentação concentrada no centro ou em sockets de variante.

### Corner

- ângulo exato de 90°;
- usa as mesmas espessuras da parede reta;
- deve encaixar sem duplicar colunas ou molduras.

### Arch, gate e door

- passagem comum próxima de 2,2m × 3m;
- parte móvel separada quando necessária;
- collider separado da moldura;
- frente e verso tratados.

### Bridge, track e channel

- comprimento múltiplo de 4m;
- conexão plana;
- largura de gameplay explícita;
- rails, ropes ou banks não atravessam o plano de snap.

## Nomes de snap

```text
SNAP_GRID_BASE
SNAP_N
SNAP_E
SNAP_S
SNAP_W
SNAP_UP
SNAP_DOWN
```

## Materiais compartilhados

Cada bioma deve preferir uma biblioteca de cinco famílias de material. Variantes usam tint, masks, decals e vertex color antes de criar materiais completamente novos.

## Repetição

Combater repetição com:

- decals;
- props opcionais;
- rotação de peças não direcionais;
- vertex color;
- duas ou três variações de textura;
- iluminação e VFX no Unity.

Não combater repetição alterando a dimensão do módulo.

## Unity prefab

Estrutura sugerida:

```text
PrefabRoot
├── Visual_LOD0
├── Visual_LOD1
├── Visual_LOD2
├── Colliders
├── Sockets
├── VFX_Anchors
└── Audio_Anchors
```

## Validação mínima

- escala;
- bounds;
- pivot;
- orientação;
- snap;
- material slots;
- triângulos;
- UV;
- collider;
- LOD;
- ausência de VFX assado;
- teste de quatro peças adjacentes.

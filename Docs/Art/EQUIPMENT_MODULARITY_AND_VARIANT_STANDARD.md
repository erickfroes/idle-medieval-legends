# Equipment Modularity and Variant Standard

## 1. Objetivo

Evitar que 18 famílias × 9 Tiers × 6 raridades resultem em 972 malhas totalmente independentes.

## 2. Camadas de um item

```text
BaseMesh (família + Tier)
BaseMaterialSet (Tier)
RarityMaterialVariant
OptionalRarityAttachmentSet
OptionalRarityVFX
InventoryIcon
Prefab
```

## 3. Reutilização obrigatória

- Common/Uncommon: 100% da mesma geometria.
- Rare: preferir material/runa; no máximo um attachment compacto.
- Epic: no máximo dois attachments.
- Legendary: attachments de prestígio com crescimento máximo de 10% da silhueta.
- Mythic: uma alteração assinatura; não redesenhar todo o objeto salvo item narrativo excepcional.

## 4. Armadura

No MVP, elmos, peitorais, luvas e botas são props de inventário. A representação equipada usa skins completas. Não tentar adaptar malhas geradas independentemente a todos os corpos.

## 5. Materiais

Cada base deve buscar um atlas ou conjunto compartilhável por família/Tier. O limite normal é 2 materiais; 3 somente quando metal/tecido/arcano realmente precisarem de shaders diferentes.

## 6. Prefab

O prefab lógico de raridade referencia:

- mesma malha-base;
- material de raridade;
- attachments opcionais;
- VFX opcional;
- metadata de Tier/raridade;
- pivô/sockets;
- LODGroup.

## 7. Exceção Mítica

Uma malha Mítica inteiramente própria só é permitida para item assinatura narrativo, receita sazonal ou recompensa única aprovada. Deve usar outro asset ID e não substituir silenciosamente a variante padrão.

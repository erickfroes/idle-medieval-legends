# Idle Medieval Legends — Asset Production Standard

Version: **Task 015 / v1.0**  
Source of truth: `Docs/Art/IDLE_MEDIEVAL_LEGENDS_VISUAL_BIBLE.md`

## 1. Purpose

This document converts the Visual Bible into measurable production rules for Meshy, Blender and Unity. The spreadsheet is the operational catalog; this document explains how its fields must be interpreted.

## 2. Catalog scope

The catalog contains **458 production records** and **907 expected outputs/variants**. It includes 3D models, portraits, UI, animation sets, VFX and shared material libraries. Only records marked `meshy_eligible = Yes` or `Partial` belong to the Meshy pipeline.

## 3. Binding rules

1. `asset_id` is stable, lowercase, ASCII and never reused.
2. A renamed display name does not rename an approved ID.
3. Equipment rarity does not multiply base geometry by default; one Tier family supports six material/ornament treatments.
4. Heroes use full-body skins with separate weapons/shields for the MVP.
5. Characters must use the shared skeleton assigned in `animation_set` or document a justified exception.
6. FBX is the default export for Unity. Texture files stay beside the source export and are copied into controlled Unity folders.
7. AI output is never approved directly. It must pass silhouette, topology, UV, pivot, scale, material, rig and performance checks.

## 4. Meshy workflow

```text
Approved concept/reference
→ Text/Image/Multi-view generation
→ select geometry
→ Remesh to the catalog target
→ UV inspection
→ stylized PBR texturing
→ rig/animation when applicable
→ FBX export
→ Unity import staging
→ material conversion to URP
→ collider/LOD/prefab
→ device profiling
→ approval
```

Meshy documents Remesh as the step used to control topology and target polygon count; its general mobile recommendation is under roughly 10K faces, so final bosses above that range are explicit profiling exceptions rather than defaults. Meshy also recommends FBX for Unity game pipelines.

## 5. Production phases

- **P0 / Vertical Slice:** minimum playable set; must be completed before mass production.
- **P1 / MVP:** complete first commercial loop and primary faction.
- **P2 / Season 1:** broader content and advanced Tiers.
- **P3 / Post-MVP:** T8–T9 prestige content and nonessential variations.

## 6. Technical acceptance

Every 3D asset must satisfy:

- correct real-world scale (`1 Unity unit = 1 meter`);
- Y up and forward orientation consistent with the project import preset;
- approved pivot;
- no accidental internal geometry;
- no detached floating artifacts unless intentionally socketed;
- valid normals and UVs;
- texture maps defined in the catalog;
- material slot count at or below the catalog limit;
- LOD0 within the min/max budget or a documented exception;
- LOD1 and LOD2 ratios within ±10% of target;
- collider strategy implemented;
- prefab path and import path respected;
- readable silhouette at battle camera and 3x speed.

## 7. Rarity strategy

Equipment families have six expected rarity treatments, but this does not mean six unrelated meshes. Base policy:

- Common/Uncommon: material and small trim differences;
- Rare: one rune/gem and modest polish;
- Epic: controlled ornament overlay and low emission;
- Legendary: prestige silhouette overlay and localized aura;
- Mythic: signature overlay/material/VFX, still preserving the Tier technology.

## 8. LOD policy

- LOD0: catalog min/max range;
- LOD1: normally 50% of the LOD0 midpoint;
- LOD2: normally 20–25%;
- tiny inventory-only props may omit LODs when profiling confirms no benefit;
- bosses above 10K triangles require a recorded on-device test.

## 9. File naming

```text
Source concept: CONCEPT_<asset_id>_v###.png
Meshy source: MESHY_<asset_id>_v###.fbx
Texture: T_<asset_id>_<map>_v###.png
Material: MAT_<asset_id>_<variant>.mat
Prefab: PF_<asset_id>.prefab
Animation: ANIM_<set>_<clip>.anim
```

## 10. Status flow

```text
Planned → Brief Ready → Concept Approved → Generating → Cleanup → Unity Staging → QA → Approved
                                                       ↘ Rework
```

Only `Approved` assets may enter production builds.

## 11. Sources

- Meshy Remesh: https://docs.meshy.ai/en/webapp/guides/3d-model/remesh
- Meshy game assets workflow: https://docs.meshy.ai/en/webapp/guides/use-cases/game-assets
- Meshy export formats: https://docs.meshy.ai/en/webapp/guides/platform/export-formats
- Meshy Unity plugin: https://docs.meshy.ai/en/webapp/plugins/unity/introduction

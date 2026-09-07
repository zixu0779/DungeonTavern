# Current art workflow

This short reference replaces the retired tile-production and environment-handoff
instructions. GameDesignNote contains world canon; this file describes current
presentation and asset organization, not future feature commitments.

## Presentation

- Oblique 2.5D dungeon tavern; pixel-art textures/sprites coexist with 3D props.
- Current Game camera: orthographic, pitch 45 degrees, four yaw headings
  45/135/225/315; Q/E switches by 90 degrees over 0.22 seconds. No mouse orbit.
  Exploration size is 4.5; dialogue temporarily reframes the speakers.
- Main scene output is serialized at 1920x1080. Inspect actual Game View;
  arbitrary Scene View zoom is not a runtime comparison.
- Ordinary model materials currently use URP/Unlit to show authored texture
  colors without lighting darkening. Special effect materials remain separate.
  Neutral ambient light plus local warm/cool lights is a future proposal, not
  the current implemented replacement. Unlit models do not receive that lighting.
- Keep underground architecture readable: restrained blue-grey stone and warm
  wood, with enough contrast to see masonry, silhouettes and interactions.
- B1 uses cutaway walls and a flat stone backdrop at wall-top height. Preserve
  authored stair/gate geometry, travel triggers and collision support.
- The playable protagonist uses a 3D Humanoid model with idle, walk and cup
  interaction animation. The protagonist uses KayKit Mage, Eve uses Rogue, and customers use Barbarian.
  The previous protagonist sources and a pre-replacement prefab remain local.
  Protagonist/customer scale is uniform, calibrated to shoulders above the counter.
  Original sprite children are inactive for rollback. Seating uses three imported
  clips; protagonist drinking uses an Animator curve driving hand IK and cup tilt.

## Source and runtime assets

- `ArtSource/Props/Concepts`: existing environment/prop designs, including walls
  and stairs as well as furniture.
- `ArtSource/Props/AIGenerated`: original generated object models and textures.
- `ArtSource/Characters/Concepts`: new character reference designs.
- `ArtSource/Characters/AIGenerated`: generated character meshes, textures and rigged sources.
- Unity-ready runtime assets live under `Assets/DungeonTavern`; moving external
  source folders does not require moving those imported assets.
- Keep each character's design separate. For rigging candidates, prefer neutral
  A/T poses with separate limbs and empty hands; assess topology before animation.

## Editing rules

- Preserve user-authored positions, pivots, sprite slicing and material tints
  outside the requested scope. For animation-only edits, change animation keys,
  not base transforms.
- Keep Unity GUIDs stable when moving imported assets; use AssetDatabase.
- Preserve original concept/model content during organizational moves.
- Respect current per-asset import settings. Ground texture imports currently
  use 32 PPU, Multiple sprites, Bilinear, no mipmaps and no compression via
  `DungeonTavernGroundTextureDefaults`; do not reapply old universal Point rules.
- Legacy Tilemap/Palette tooling is retired. Do not regenerate it from old docs.
- Review color, silhouettes, collision and animation in context. A successful
  compile or unchanged file hash is not a visual acceptance test.

## Retained editor tools

- `DemoServiceCheck`: Play Mode six-wave arrivals, automatic cup activation and NPC facing checks.
- `NpcPlacementCheck`: seat-route audits and Eve doorway traversal checks.
- `TavernNavigationBake`: bake current physical obstacles with NPC body clearance.
- `OpeningVaultCheck`: Play Mode opening input-lock and Bar Prefab vault checks.
- `CharacterActionCheck`: repeatable Play Mode drinking/seating checks and close-ups.
- `ScenePortalGizmos`: visible bounds for placing teleport trigger walls.
- `PrototypeFloorEdgeClipperWindow`: apply/restore selected floor-edge cuts.
- `DungeonTavernGroundTextureDefaults`: active Ground PNG import policy.

Completed one-off migration, repair, screenshot and validation scripts were
removed in the 2026-09-18 cleanup. They are not runtime dependencies.

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
  Character direction now follows the current KayKit-style proportions, silhouettes,
  and simple low-poly forms; the previous protagonist concept is no longer the
  production target. Its source files and pre-replacement prefab remain local.
  Protagonist/customer scale is uniform, calibrated to shoulders above the counter.
  Original sprite children are inactive for rollback. Seating uses three imported
  clips; protagonist drinking uses an Animator curve driving hand IK and cup tilt.

## Local wall cutouts

- `DialogueOcclusionFader` uses sphere casts every 0.1 seconds and smooth world-space
  view-segment capsule masks with triplanar noise and independent fade transitions, based on Brendan Sullivan's public breakdown:
  https://www.artofsully.com/projects/WXVnyD . This is an original Unity implementation;
  no downloadable source from the author was found.
- Runtime wall material copies preserve base texture/tint/UVs. Native Pixel Face
  retains its atlas mapping. Fixed world-space noise breaks up the cut edge.
- Only architecture under Walls, Walls_Stone and StairRearEnclosure participates.
  Floors and collisions remain unchanged; renderer objects are never hidden.
- Hollow wall slabs use depth-correct reconstructed cut surfaces, bounded by each
  mesh's back faces and local bounds. The original exterior texture is never used
  as an interior fill. Brick cores use crushed brick/lime mortar; stone cores use
  grey mineral aggregate. Textures live in Walls/Sections/Resources and use fixed
  world-space scale. This is a rendering cap, not a change to mesh or collision.
- Non-wall props (including stairs and moving door leaves) keep their normal
  occlusion. Stone door frames remain eligible. Deeply concave/disconnected meshes
  require separate wall slabs to guarantee an exact internal section.
- The camera component's Enable Sections toggle disables caps independently of
  the cutout. Both textures and the cap shader are included in runtime resources.
- Disable the camera's DialogueOcclusionFader to restore authored materials.

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

## Interface presentation

- Runtime UI uses separate overlay canvases with dark iron panels, copper borders,
  warm gold emphasis and parchment speech bubbles. Chinese text uses bundled
  Noto Sans CJK SC. See [UI design](UIDesign.md) for layout and display order.

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

- `DemoServiceCheck`: Play Mode six-wave arrivals and NPC facing checks.
- `DemoFlowPlayCheck`: startup skip, queue facing, cup demand, entrance camera/door/sign sequence and floor support checks.
- `NpcPlacementCheck`: seat-route audits and Eve doorway traversal checks.
- `TavernNavigationBake`: bake current physical obstacles with NPC body clearance.
- `OpeningVaultCheck`: Play Mode opening input-lock and Bar Prefab vault checks.
- `CharacterActionCheck`: repeatable Play Mode drinking/seating checks and close-ups.
- `ScenePortalGizmos`: visible bounds for placing teleport trigger walls.
- `PrototypeFloorEdgeClipperWindow`: apply/restore selected floor-edge cuts.
- `DungeonTavernGroundTextureDefaults`: active Ground PNG import policy.

Completed one-off migration, repair, screenshot and validation scripts were
removed in the 2026-09-18 cleanup. They are not runtime dependencies.

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

- `DialogueOcclusionFader` checks the current camera direction every 0.1 seconds,
  and every frame during Q/E turns. Narrow ankle probes (6 cm radius) activate
  when the feet are just becoming occluded; the body probe is 12 cm. Stable root-relative
  samples avoid animation footstep jitter and broad lower-body proximity triggers.
  Only walls in front of the actor initiate a cut. Rotation openings use a 0.12-second
  transition, so the change is visible during the turn. The world-space cutout follows
  Brendan Sullivan's breakdown: https://www.artofsully.com/projects/WXVnyD .
  This is a local Unity implementation, not downloaded author source.
- Exploration protects the protagonist; dialogue merges both speakers' channels.
  Locomotion uses stable full-body dimensions, with damped position/radius and
  world-anchored noise at two scales projected along the view direction. The broad
  transition band leaves separate fragments and holes; it uses the same field through
  the wall depth, so section filling does not heal those holes. Full-body actions adapt their bounds.
- Architecture under Walls, Walls_Stone, StairRearEnclosure and StoneGates,
  including B1 moving stone gates, participates. Closed small stone door frames/leaves
  share the same cutout field as adjacent walls, without requiring their own probe hit.
  Their original geometry and surface mapping remain; inferred volume caps are disabled
  on these door meshes to avoid filling or distorting the arch.
  Near an open or moving small door, the actor cutout closes for passage and resumes
  once the door is closed. Signs, stairs and other props do
  not. Floors, collisions and shadows remain intact; no whole renderer is hidden.
- Interior sections use the wall's own front-face texture, tint and UV mapping.
  Low-poly slabs/door frames intersect actual mesh triangles to respect mitres
  and doorway openings; higher-poly stone slabs use their local volume bounds.
  Deeply concave high-poly meshes still require separately authored solid slabs.
- Authoring face components preserve unrelated material parameters. Moving doors
  update their section transforms. Disabling DialogueOcclusionFader restores the
  original materials and property blocks; Enable Sections only controls filling.
- UnderWallFloor in both scenes extends the existing floor appearance beneath
  wall footprints, slightly below the original floor to prevent z-fighting. These
  visual surfaces add no colliders and do not fill the open stairwell. F1 extensions
  are clipped to the surveyed concave exterior wall outline, inset 8 mm to avoid
  exposed pixels outside straight and diagonal wall faces.
- B1 `Environment/StairArchConnection_Trial` is a removable stone arch tunnel behind
  the original stair arch. Its sides and curved roof remain opaque when surrounding
  walls dissolve; it adds no collision and leaves stair/wall transforms unchanged.
- Opening yaw is 315 degrees. The protagonist starts beside the control core,
  face down with relaxed asymmetric arms; Prone/WakeUp clips are editable Unity
  assets, while original action FBX files are retained. The right hand rests beside
  the head, with separated relaxed feet. `Tools > Characters > Preview Opening Pose`
  previews Prone in B1 without saving posed bones; Stop Opening Pose Preview restores
  the authored transforms. Wake-up transition refinement is pending pose acceptance.

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

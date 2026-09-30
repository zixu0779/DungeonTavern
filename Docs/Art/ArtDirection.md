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
  interaction animation. The protagonist uses the approved generated design B; Eve uses her generated elf Humanoid model, and customers use Barbarian.
  Character style now targets the rounded proportions and simple forms of the
  ThirdParty Mage, Barbarian and Rogue references. The protagonist concept has
  been revised accordingly; the current runtime model still uses the earlier B design. Their character models are
  temporary placeholders and must be redesigned for the final game. Protagonist concept B is now selected in
  `ArtSource/Characters/Concepts/Protagonist/concept.png`, replacing the old concept image;
  the obsolete first-design model and pre-replacement prefab have been removed.
  Current third-party characters remain under `Assets/DungeonTavern/Art/Characters/ThirdParty`
  without an extra vendor directory; their source license is retained.
  The protagonist and Eve keep uniform scale, with Mage/Rogue as the relative size references. On 2026-10-06, all runtime character visuals were enlarged by 8 percent. Idle starts at each generated model's original standing height; the former scene lift offsets are removed.
  Retired 2D character sheets and their inactive sprite references have been removed. Seating uses three imported
  clips; protagonist drinking uses an Animator curve driving hand IK and cup tilt.

## Local wall cutouts

- `DialogueOcclusionFader` checks the current camera direction every 0.1 seconds,
  and every frame during Q/E turns. Narrow ankle probes (6 cm radius) activate
  when the feet are just becoming occluded; the body probe is 12 cm. Stable root-relative
  samples avoid animation footstep jitter and broad lower-body proximity triggers.
  Every probe collects the `WallCutoutGroup` parents of all wall hits between
  the actor and camera. Actual hits plus short movement-predicted hits permit a local cut, with a 0.16-second
  hit grace period. While actually occluded, movement prediction extends probes by
  0.3 seconds (maximum 0.65 m), clipped to character-width collision clearance.
  Predicted hits require an actual view ray intersection; valid look-ahead is retained
  while stopped under occlusion. Initial and portal reveals prepare the cutout under
  the loading overlay before the image fades in. Group membership fades over 0.22 seconds using
  depth-consistent stippling inside the shared footprint, without resizing it.
  Other groups remain opaque even
  inside the shared mask. No wall-facing or actor-depth-plane heuristic is used.
  Openings use a 0.6-second transition, including during camera rotation. The world-space cutout follows
  Brendan Sullivan's breakdown: https://www.artofsully.com/projects/WXVnyD .
  This is a local Unity implementation, not downloaded author source.
- Exploration protects the protagonist; dialogue merges both speakers' channels.
  Locomotion uses stable full-body dimensions, with damped position/radius and
  world-anchored noise at two scales projected along the view direction. The broad
  transition band leaves separate fragments and holes; it uses the same field through
  the wall depth, so section filling does not heal those holes. Full-body actions adapt their bounds.
- Architecture under Walls, Walls_Stone, StairRearEnclosure and StoneGates,
  including B1 moving stone gates, participates. Small stone door frames
  share their wall group's eligibility and local cutout field without requiring their
  own probe hit. Groups are authored as parents in the scene Hierarchy, split at
  corners; B1 stone gates belong to EastWall. Dialogue uses the union of both actors'
  hit groups. A group permits a local hole, never whole-wall hiding.
  Their original geometry and surface mapping remain; inferred volume caps are disabled
  on these door meshes to avoid filling or distorting the arch.
  Small hinged door leaves stay opaque in every state; only the fixed frames
  share wall cutouts. Opening, closing and fully open doors never suppress the
  surrounding wall cut. Signs, stairs, floor collision and shadows remain unchanged.
- Interior sections use the wall's own front-face texture, tint and UV mapping.
  Low-poly slabs/door frames intersect actual mesh triangles to respect mitres
  and doorway openings; higher-poly stone slabs use their local volume bounds.
  Deeply concave high-poly meshes still require separately authored solid slabs.
- Grouped brick walls reuse their front texture on unassigned end faces. Uncut
  groups retain their original back surfaces instead of entering section filling.
- Authoring face components preserve unrelated material parameters. Moving doors
  update their section transforms. Disabling DialogueOcclusionFader restores the
  original materials and property blocks; Enable Sections only controls filling.
- UnderWallFloor in both scenes extends the existing floor appearance beneath
  wall footprints, slightly below the original floor to prevent z-fighting. These
  visual surfaces add no colliders and do not fill the open stairwell. F1 extensions
  are clipped to the surveyed concave exterior wall outline, inset 8 mm to avoid
  exposed pixels outside straight and diagonal wall faces.
- B1 stair masonry sample uses large dressed arch stones and coping stones, with
  separate top/front/side material values. The rear wall ends at z=22.77 and joins
  a short south return instead of exposing a free-standing wall end. Perpendicular
  returns keep separate WallCutoutGroups. Existing stair placement and arch clearance
  remain unchanged. Dressed opening assets live under `Walls/StoneWall/StairPassageSample`.
- All B1 rubble walls use the approved top/front/side values. Coping stones are
  0.30 m thick and cover the union of wall tops, including corner pillars, without
  overlapping faces. Rollout meshes and local materials live under
  `Walls/StoneWall/B1StoneMasonry`. B1 stair fog uses separate bounded materials
  confined to the exterior passage; F1 fog materials retain their original behavior.
- Opening yaw is 315 degrees. The protagonist starts beside the control core,
  face down with relaxed asymmetric arms; Prone/WakeUp clips are editable Unity
  assets, while original action FBX files are retained. The right hand rests beside
  the head, with separated relaxed feet. `Tools > Characters > Preview Opening Pose`
  previews Prone in B1 without saving posed bones; Stop Opening Pose Preview restores
  the authored transforms. WakeUp and GetUp use continuous two-hand ground support, leg recovery and standing
  settlement. The B1 player start is authored at (40.62213, 0.08, 17.68641).

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

- B1 ascending stone stair uses `Stair_Stone_B1_NoArch.fbx` through its existing
  prefab: upper arch removed and pillar footprints capped. Original FBX is retained
  in the same model folder. The B1 scene uses `Stair_InsideWallFootprint.asset`,
  derived from the arch-free mesh, to remove the exterior overhang at the rear
  return wall. Authored scene placement and materials are unchanged.

## Character references and Hyper3D

- Each character has one folder under `ArtSource/Characters/Concepts/<Name>/`.
  Keep the approved `concept.png` and separate `front.png`, `left.png`, `back.png`.
  Each direction file contains one character only; do not upload a combined sheet.
- Before modeling, check matching identity, proportions, costume, pose and anatomical
  left/right across the three images. A left view shows the character's left side.
- Use the service's multi-view workflow and explicit direction fields. The official
  Rodin Gen-2.5 REST API accepts ordered `images` with `image_label=["F","L","B"]`.
  The currently exposed MCP lacks `image_label`; filenames or prompt descriptions
  alone are not an equivalent direction setting. Verify support before spending credits.
- User authorization covers uploading these project character references to Hyper3D
  for requested modeling. Proceed without repetitive confirmation within that scope;
  this does not override tool approval or security restrictions.
- Do not save discarded iterations, standalone design explanations or prompt logs.
  Keep adopted images and usable production assets; temporary checks stay outside the project.

## Protagonist Unity integration (2026-10-02)

- Runtime model, Unlit material, 2K shaded texture, independent controller and prefab: `Assets/DungeonTavern/Art/Characters/Protagonist`. Editable rig stays in `ArtSource/Characters/AIGenerated/Protagonist/Protagonist_Rig.blend`.
- B1 `Player/CharacterModel` now uses this Humanoid model. Player movement, interaction, collision and story components are preserved; the cup anchor points to the new right hand.
- `ProtagonistCapeMotion` reproduces the front-cloak thigh lift after Humanoid animation; Blender drivers are not imported. This is bone-driven cloth, not cloth collision simulation. Extreme-pose surface stretching remains a visual limitation.
- ThirdParty model/animation assets retain their contents and GUIDs. The new controller reuses existing clips without overwriting their sources.
- `Tools > Characters > Check Protagonist Rig` checks avatar, cape lift/rest and skin references. Opening/vault and drink/seating functional checks passed; the final opening report includes an unrelated remote WebSocket connection failure. This does not establish full four-view or wall/counter clipping acceptance.

- Final character visuals must be newly designed to match the adopted protagonist. ThirdParty models are temporary/reference-only; their silhouettes are not a guarantee of final stylistic consistency. Rogue assets are grouped under `ThirdParty/Rogue`. Eve currently uses only Idle/Walk; unused seating states and their standalone `Seating.fbx` source have been removed.
- The protagonist controller uses its own `Protagonist/Idle.anim` with reduced arm abduction. Idle-based empty-hand/holding states share this base pose; hand IK remains responsible for the held cup. Original third-party clips remain unchanged.

## Eve Unity integration (2026-10-05)

- Runtime FBX, Unlit shaded texture/material, independent controller, adapted Walk and prefab are under `Assets/DungeonTavern/Art/Characters/Eve`; original GLBs and `Eve_Rig.blend` remain in her AIGenerated folder.
- Tavern_Main replaces only Eve's visual child. Skeleton hierarchy and Idle are reused from the protagonist, while joint placement, skin weights and Walk stride/sole height are adapted for Eve.
- Avatar/skin/pose checks and Play Mode navigation to the opening guide passed. Apron hem weights now follow the underlying skirt; final close-up appearance remains subject to user acceptance; no new service actions are implied.

## Character size baseline (2026-10-06)

- Use approximately 2.3 world units of standing visual height, with a 5 percent allowance, as the initial integration target for ordinary adult character models. Preserve intended relative stature; unusually tall/short species need individual review.
- Measure from the soles to the normal head/hair silhouette in the original standing pose. Oversized hats, horns or carried props should not force the body to shrink; compare shoulders and body size as well in those cases.
- Scale uniformly on the runtime visual root, keep soles on the actor floor plane, and match Idle's initial height to the original model pose. Do not resize colliders or gameplay interaction distances solely to match visual scale.
- Verify beside the bar and other characters with the actual exploration camera. For Idle, judge visible motion in that camera, not only close-up animation previews; the current target is roughly 3 screen pixels of upper-body rise at 1080p with planted feet.

## Bar proportion review (2026-10-06)

- Review the existing bar directly; do not add a separate blockout. Tavern_Main's bar visual root is temporarily compressed vertically from a 1.10 to a 0.95 countertop height, with width/depth preserved. This height is pending user acceptance, not the final authored proportion.
- After size acceptance, confirm a more regular modular design using a corner piece and repeatable straight sections. Avoid generating the entire L-shaped bar as one model. Do not start new model generation before those confirmations.
- Dialogue portraits now frame the head silhouette consistently rather than fitting each character's full-body bounds; baked skinned geometry must not receive its renderer scale a second time.

- Follow-up review: the user clarified that each L-shaped counter arm should be one floor tile deep (1.00 world unit), not shorter in overall length. The prior X shortening is restored; both arms are narrowed in a separate review mesh while preserving the outer span and 0.95 height. Original FBX remains intact. Bar collision boxes, staff-gate positions and countertop props follow the revised footprint. CupDispenser remains uniformly reduced by 10 percent with its base at the 0.95 tabletop. Visual dimensions remain under review; navigation has been rebaked for the revised footprint and gates.

- Gate review: both leaves now measure 1.50 world units along their passage direction. The whole bar shifts +0.4072 on world X while its long span remains 13.3019; the short arm extends +0.0670 on world Z. The east hinge/post stay fixed, the north hinge/post follow the rightward translation. Passage blockers/triggers follow the gate width adjustment; countertop devices follow the bar. Isolated close/open/close state checks passed; navigation has been rebaked for this layout.

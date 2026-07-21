# Dungeon Tavern — Game Design Bible

This document is the durable source of truth for confirmed game and world design. Items under **Open questions** are intentionally undecided and must not be implemented as final systems without explicit confirmation.

## Confirmed world canon

- The game is a pixel-art RPG about operating a tavern inside a dungeon.
- The tavern is located in a secluded corner of one dungeon floor, not at the dungeon entrance.
- The opening may show a character entering the dungeon, but that sequence belongs to a separate intro scene.
- The player is the tavern owner. Handling tavern affairs reveals the world and advances the story.
- The tavern treats all creatures equally, including adventurers and native dungeon creatures.
- There is one shared public entrance, one shared bar, and mixed seating. Public areas are never segregated by species or faction.

## Tavern peace rules

- Hostile magic fails within the tavern.
- Weapons continue to exist and function physically; they are not magically removed.
- A creature that initiates an attack is magically bound before it can cause harm.
- Severe offenders are permanently refused entry.
- Staff and regular patrons treat these rules as common knowledge.

The implementation details, edge cases, and gameplay presentation of these rules remain undecided.

## Areas and progression

Initially available:

- Main hall
- Bar
- Mixed seating
- One public entrance
- Kitchen
- Storage

Potential later unlocks, driven by story or achievements:

- Performance area
- Lodging area
- Quiet private rooms

The tavern has no conventional building-upgrade ladder. Progression may expose or open additional physical areas, but must not silently become a generic upgrade economy.

## Confirmed visual direction

- Pixel-art presentation.
- The game uses an oblique top-down view, not a completely vertical overhead view.
- Walls must show readable vertical faces or height. They must not be represented only as flat floor boundaries.
- Furniture and architecture must share the same oblique perspective; fully overhead props are not suitable for final scene dressing.
- Dungeon and underground atmosphere: dark, enclosed, and subdued rather than bright outdoor fantasy.
- Architecture should read darker than loose props while preserving enough local contrast for cracks, masonry, and material texture to remain legible.
- Cainos and CraftPix Tavern assets may be combined through project palettes organized by scene function rather than source pack.
- The curated assets, slicing data, Tile assets, and Palettes currently present in the project are the working baseline. Do not delete, reslice, recolor, regenerate, or reorder material outside the explicit scope of a task.

Detailed visual rules are maintained in `Docs/Art/ArtDirection.md`. Sprite slicing and Palette workflow rules are maintained in `Docs/Art/TileAuthoringGuide.md`.

## Open questions

The following are not design commitments:

- Exact core gameplay loop and daily structure
- RPG attributes, skills, progression, and failure states
- Event generation and resolution rules
- NPC schedules, AI, relationships, and reputation
- Detailed narrative structure and quest model
- Technical implementation of the no-fighting rules
- Exact conditions and ordering for opening expansion areas
- Camera-follow, collision, navigation, and interaction architecture
- Final internal room boundaries, furniture arrangement, and circulation measurements for the current tavern layout
- Final lighting and post-processing configuration

## Current scene status and contract

- `Assets/Scenes/Tavern/Tavern_Main.unity` is the authoritative 2.5D work scene.
- The approved presentation uses an orthographic camera pitched 45 degrees, with
  camera-relative cardinal headings at 45, 135, 225, and 315 degrees.
- WASD moves the player and Q/E rotates the view by 90 degrees.
- `Assets/Scenes/Backup/Legacy2D/Tavern_Main_2D_Backup.unity` preserves the former
  2D Tilemap scene as a read-only fallback.
- Runtime 2.5D assets live under `Assets/DungeonTavern/Tavern25D`.
- Legacy 2D Palettes and Tile assets are archived under
  `Assets/DungeonTavern/Archive/Legacy2D`.
- Exact internal geometry, circulation, furniture arrangement, and gameplay systems
  remain pending review in the authoritative 2.5D scene.

Stable marker names for future integrations:

- `SpawnPoints`: `PlayerStart`, `GuestEntry`, `StaffKitchen`, `StaffStorage`
- `AreaMarkers`: `Hall`, `Bar`, `MixedSeating`, `Kitchen`, `Storage`
- `LockedAreaMarkers`: `Performance`, `Lodging`, `QuietRooms`

## Change log

- 2026-07-16: Created the durable design bible and recorded the confirmed tavern setting, peace rules, initial/locked areas, visual direction, and intentionally open gameplay questions.
- 2026-07-20: Confirmed the oblique top-down perspective, made `Tavern_ReadabilityPrototype` the authoritative work scene, reclassified `Tavern_Main` as the superseded greybox, and linked the dedicated art-direction and tile-authoring documents.
- 2026-07-20: Configured the authoritative scene camera for pixel-perfect rendering at 32 PPU with a 640×360 reference resolution.
- 2026-07-27: Promoted the user-corrected `Tavern_Main` back to the authoritative 2D
  work scene and defined the rotation prototype as a separate derived experiment.
- 2026-07-28: Approved the 2.5D orthographic presentation, promoted it to
  `Tavern_Main`, preserved the former 2D scene as `Tavern_Main_2D_Backup`, and
  archived 2D-only Palette and Tile authoring assets.

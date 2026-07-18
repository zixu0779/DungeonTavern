# Dungeon Tavern — Environment Art Handoff

This is the active production handoff for environment-art work. Read it together with
`Docs/Art/ArtDirection.md` and `Docs/Art/TileAuthoringGuide.md`. It records task state and
priority, not permanent game canon.

## Current priority

Current active task:

1. Produce an aged wooden tavern-floor tileset.

The `Walls_interior.png` adjustment task is complete. Do not reopen or revise the wall
assets unless the user explicitly puts them back in scope.

## Task 1 — Walls_interior

**Status: completed.**

The source atlas is:

`Assets/DungeonTavern/Art/Environment/Walls/Walls_interior.png`

Required approach:

- Inspect the user's current manual Sprite slicing before changing the image or metadata.
- Treat the atlas as a mixed assembly sheet, not a uniform grid. Identify pieces by semantic
  role: wall top, visible face, straight segment, corner, end cap, pillar/support, arch/door,
  shadow, and decorative transition.
- Preserve the oblique top-down perspective. A wall must expose a readable vertical face;
  do not build the scene from flat top strips alone.
- Do not assume every Sprite under `Walls` represents the same direction or can be repeated.
- Do not rotate or mirror perspective-sensitive pieces without visual review.
- First produce a marked-up analysis or a candidate image. Do not overwrite the formal source,
  reslice it, regenerate Tile assets, or reorganize the Wall Palette without explicit approval.
- If the source lacks required structural pieces, new images may be generated in the same style,
  but they must be delivered as candidates before replacing or supplementing formal assets.
- Any generated wall pieces must use hard pixel edges, Point filtering, 32 PPU, no mipmaps,
  no lossy compression, and the project's dark blue-grey dungeon palette.

Initial review questions to answer from the asset itself:

- Which current Sprites are actually usable for front-facing wall faces in the chosen camera view?
- Which pieces form coherent left/right corners, ends, pillars, and door openings?
- Are side-facing walls missing or merely sliced incorrectly?
- Which pieces have shadows that must remain inside their Sprite rectangle?
- What minimal new pieces are required to build the tavern boundary without perspective errors?

### Diagonal wall production status

- `Assets/DungeonTavern/Art/Environment/Walls/Walls_Diagonal.png` is the
  formal supplementary atlas for `1:1`, `2:1`, and `3:2` slopes in both directions.
  It contains six continuous series of ten pieces (60 Sprites total), with no start,
  end, straight-wall interface, or corner pieces.
- Each piece is 16 pixels wide and preserves a 46-pixel wall cross-section. The
  authoritative pattern is sampled from the ten original no-window wall pieces in
  `Walls_interior.png`, kept in source order for both Up and Down series. The
  generator changes only the diagonal geometry; it does not invent or mirror wall
  decoration, and screenshot grid lines are excluded.
- The `3:2` Up/Down masters use one continuous rational `2:3` staircase phase
  across all 160 pixels. The phase must not restart at 16-pixel Sprite boundaries;
  doing so creates visibly uneven adjacent short or long stair steps.
- `Walls_Diagonal_Preview.png` provides dark- and checker-background review at
  `720×533`.
- The atlas is authored and imported at `16 PPU` to match the
  current `Walls_interior` pixel grid. This is a compatibility exception, not a
  change to the project's long-term `32 PPU` environment target.
- Detailed slot ordering and asset rules are recorded in
  `Docs/Art/WallsDiagonalLayout.md`.
- The 60 Sprite slices have 60 matching semantic Tile assets under
  `Assets/DungeonTavern/Art/TileAssets/Dungeon_Walls`. They are installed in the Wall
  Palette at X `42..51`, using Y `7/6`, `4/3`, and `1/0` for Up/Down slope pairs.
  The obsolete 42-piece connector kit has been removed. Scenes remain unchanged.

### Vertical wall and orthogonal connector status

- `Assets/DungeonTavern/Art/Environment/Walls/Walls_Vertical_Connections.png`
  is the formal supplementary atlas for vertical wall tops and horizontal-to-
  vertical transitions.
- The atlas contains 55 Sprites: two repeatable vertical bodies, four regular
  connector directions with ten wall-pattern variants each, two shifted-strip
  specials, ten texture-matched third-Tile specials, and the user-authored
  `Walls_HV_RightToDown_04_Special`. The first twelve specials are `16×16`;
  the user-authored Sprite is `16×47`.
- Every third-Tile special overlays the complete left four columns of the first
  `16×16` Tile from `RightToUp_03` (the fourth fifth-row variant). Its base
  final row stays deleted outside
  those columns, while the overlay's final row remains visible.
- The atlas is `176×321`; every Sprite is exactly 16 pixels wide. The taller
  connector bounds may extend vertically but must never enlarge the Palette
  cell width.
- A vertical body is a transparent `16×16` cell containing a four-pixel wall
  top: two light pixels enclosed by one brown pixel on each side. Brick joints
  run horizontally at zero-based rows `2/6/10/14`, preserving the approved
  body pattern without placing a joint on the top or bottom endpoint rows. It
  does not contain a rotated or compressed 46-pixel wall face.
- Downward connectors do not add a brown cross-line at the wall-top junction.
  Their vertical top reaches through the wall and adds one four-pixel-wide
  brown bottom line, while its inner brown edge is omitted for the first two
  junction pixels. Upward connectors are also 47 pixels high: one enclosed
  white-brick row above a
  four-pixel-wide, 46-pixel extension sampled from the corresponding horizontal
  wall texture. The first two extension rows clear the specified unwanted
  texture-side line and bridge the true inside edge; the adjacent horizontal
  Tile supplies the rest of the wall face.
- The kit uses `16 PPU`, Point filtering, no mipmaps, and no compression to
  match the current `Walls_interior` and `Walls_Diagonal` compatibility grid.
- All 55 matching Tile assets are under
  `Assets/DungeonTavern/Art/TileAssets/Dungeon_Walls`.
- `Walls_HV_RightToDown_04_Special` is placed at Wall Palette cell `(68,16)`,
  immediately right of the managed `RightToDown` row. Its pixels occupy
  top-left rect `x=56, y=274, 16×47`; its Unity
  bottom-origin Sprite rect is `x=56, y=0, 16×47`. Extending the canvas adds
  +17 to every older Sprite rect Y without moving their pixels.
- The Wall Palette appends the kit at X `57..66`; exact rows and naming are
  documented in `Docs/Art/WallsVerticalConnectionsLayout.md`.
- The Wall Palette uses manual cell sizing (`GridPalette.cellSizing = 100`).
  Do not replace it with automatic sizing: tall wall Sprites would cause all
  earlier Palette content to appear globally shrunken in the Tile Palette.
- The authoritative scene remains unchanged. Placement into the reserved
  vertical-wall gaps is a separate scene-painting step.

## Task 2 — Aged wooden tavern floor

**Status: active, requirements pending.**

The user has confirmed only the task name and priority: produce aged wooden flooring for
the dungeon tavern. Detailed requirements have not yet been provided and will be supplied
incrementally in conversation.

Until those requirements are confirmed:

- Do not generate images, choose a plank layout, define Tile counts, create transitions,
  import assets, slice Sprites, create Tile assets, or modify a Palette.
- Do not infer a final color palette, damage density, adjacency system, plank direction,
  plank dimensions, or relationship to the stone floor.
- It is acceptable to inspect existing project art when the user asks, but report findings
  separately from recommendations and do not treat recommendations as approved requirements.
- Once the user provides a concrete requirement, implement only the approved scope and use
  candidate review gates before replacing or formalizing assets.
- When image production begins, author the wooden-floor Tiles and atlas directly at their final
  pixel dimensions. Do not generate a larger image and compress or downsample it into the
  production asset.

## Ground status — do not reopen implicitly

The formal stone sources are:

- `Assets/DungeonTavern/Art/Environment/Ground/Ground_Cracked_Seamless.png`
- `Assets/DungeonTavern/Art/Environment/Ground/Ground_Cracked_Autotile.png`

`Ground_Cracked_Seamless.png` currently contains 64 complete 32×32 Tiles. Each Tile has mandatory
bottom and right mortar edges, compatible top/left lit edges, Point filtering, 32 PPU, no mipmaps,
and no compression. Its formal GUID is `9e7cb8cefaad845b7bd8552e9fedbd17`.

`Ground_Cracked_Seamless_EdgeFix_Candidate.png` is retained as a rollback copy. Do not delete it or
resume Ground editing unless the user explicitly puts Ground back in scope.

## Scope and review gates

- Preserve all user-authored slicing and Palette edits outside the named task.
- Candidate images must be shown and approved before they replace formal images.
- Do not modify Ground, Walls, Decoration, furniture, scenes, or existing Palettes while
  producing the wooden floor unless the user explicitly includes them in the task.
- At each approval gate, verify image dimensions, hard pixel edges, Alpha behavior, 32 PPU, Point
  filtering, mipmaps, compression, slicing, GUID stability, Tile references, and Palette references.

## Suggested start for the next conversation

Read `AGENTS.md`, `Docs/GameDesign/GameDesignBible.md`, `Docs/Art/ArtDirection.md`,
`Docs/Art/TileAuthoringGuide.md`, and this file. The active task is aged wooden tavern
flooring, but its detailed requirements are intentionally pending. Summarize the known
project context, make no asset changes, and wait for the user's first concrete requirement.

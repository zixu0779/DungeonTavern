# Dungeon Tavern — Environment Art Handoff

This is the active production handoff for environment-art work. Read it together with
`Docs/Art/ArtDirection.md` and `Docs/Art/TileAuthoringGuide.md`. It records task state and
priority, not permanent game canon.

## Current priority

Work in this order unless the user explicitly changes it:

1. Review and adjust `Assets/DungeonTavern/Art/Environment/Walls/Walls_interior.png`.
2. Produce a new aged wooden tavern-floor tileset.

Do not begin the wooden floor before the wall direction has been reviewed far enough to
establish a compatible perspective, palette, pixel density, and material contrast.

## Task 1 — Walls_interior

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

## Task 2 — Aged wooden tavern floor

Create this as a new material family; do not overwrite the formal stone-floor sources.

Target language:

- Old subterranean tavern wood: dark, desaturated brown with slight warm variation.
- Readable plank separation at 100% scale, restrained grain, wear, dents, stains, and occasional
  repaired boards; avoid bright clean fantasy-inn flooring.
- The floor remains mostly top-facing and must fit the same oblique environment presentation.
- Use 32×32 complete Tiles at 32 PPU with Point filtering, no mipmaps, and no lossy compression.
- Design adjacency deliberately: plank courses and seams must continue across compatible edges.
- Include enough intact variants to avoid noisy damage everywhere. Keep cracks, missing chips,
  stains, and repairs as controlled variants rather than the universal base texture.
- Generate candidates and practical tiled previews before formal import, slicing, Tile creation,
  or Palette placement.

Before generation, confirm from the wall work:

- final plank direction and approximate plank width;
- value contrast against stone walls and furniture;
- whether a stone perimeter or threshold transition Tile is required;
- whether the first delivery is a freely mixed set, an interface-coded set, or both.

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
- Do not modify Ground, Decoration, furniture, scenes, or unrelated Palettes while working on Walls.
- Do not modify Walls while producing the wooden floor unless a separately approved transition Tile
  requires both sources.
- At each approval gate, verify image dimensions, hard pixel edges, Alpha behavior, 32 PPU, Point
  filtering, mipmaps, compression, slicing, GUID stability, Tile references, and Palette references.

## Suggested start for the next conversation

Read `AGENTS.md`, `Docs/GameDesign/GameDesignBible.md`, `Docs/Art/ArtDirection.md`,
`Docs/Art/TileAuthoringGuide.md`, and this file. Then inspect `Walls_interior.png`, its `.meta`, the
current Wall Tile assets, and the Wall Palette. Report the semantic role of each existing slice and
identify missing pieces before changing any asset.

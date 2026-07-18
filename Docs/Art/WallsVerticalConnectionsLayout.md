# Vertical Wall and Orthogonal Connector Layout

The formal supplementary atlas is:

`Assets/DungeonTavern/Art/Environment/Walls/Walls_Vertical_Connections.png`

It is a `176×321` RGBA atlas imported as Multiple Sprites at `16 PPU`, Point
filtering, no mipmaps, and no compression. The 16 PPU setting is the same
compatibility exception used by `Walls_interior.png` and `Walls_Diagonal.png`.

## Sprite set

There are 55 Sprites and 55 matching Tile assets. The original managed Palette
block still contains 54 entries; placement of the user-authored Tile is kept
separate from that managed block.

- `Walls_Vertical_Right`
- `Walls_Vertical_Left`
- `Walls_HV_LeftToDown_00..09`
- `Walls_HV_RightToDown_00..09`
- `Walls_HV_LeftToUp_00..09`
- `Walls_HV_RightToUp_00..09`
- `Walls_HV_Special_LeftDownStripToLeft`
- `Walls_HV_Special_RightDownStripToRight`
- `Walls_HV_Special_RightDownThirdTileShort_00..09`
- `Walls_HV_RightToDown_04_Special` (user-authored supplementary Sprite)

Each vertical body occupies a transparent `16×16` cell. Its visible wall top is
four pixels wide: two light pixels enclosed by a one-pixel brown line on each
side. Internal brick joints are horizontal but never occur on the first or last
row. Within each 16-pixel body they occur at zero-based rows `2`, `6`, `10`,
and `14`, preserving the previously approved body pattern. `Right`
places the strip at columns `12..15`; `Left` places it at columns `0..3`.

Connector variants preserve the ten no-window wall textures from
`Walls_interior` in their original left-to-right order. All connectors are
`16×47`. Downward connectors continue the four-pixel vertical top through the
46-pixel wall and add one bottom pixel drawn as a four-pixel horizontal brown
line; the inner brown side line is omitted for the first two junction pixels
to avoid doubling the horizontal wall edge.
Upward connectors use a single top row containing two white pixels enclosed by
the two brown side lines, followed by a four-pixel-wide 46-pixel face
extension. In the `LeftToUp` row, local column 2 is cleared on extension rows
2–3; the mirrored row clears its corresponding local column. The true inside
edge is bridged to the cap. The adjacent horizontal Tile provides the rest of
the wall face.

The special set contains twelve `16×16` Tiles across three use cases:

- the top Tile of `LeftToDown_00`, containing only its right strip, moved to
  local columns `0..3`; source row 4 is deleted and the remaining 15 rows are
  bottom-aligned;
- the top Tile of `RightToDown_00`, containing only its left strip, moved to
  local columns `12..15`, with the same row deletion and bottom alignment;
- ten texture-matched third top-down Tiles corresponding to every
  `RightToDown_00..09`. Their final row is deleted and the remaining 15 rows
  are top-aligned. Because the 47-pixel source uses a `24/47` pivot, this third
  Tile begins at zero-based source row `31`, not `32`. The complete left four
  columns of the first top-down Tile
  from `RightToUp_03` (the fifth preview row's fourth variant) are then
  overlaid on every special. The base remains deleted on its final row, but the
  overlay's own final-row pixels remain visible.

The atlas was extended downward by 17 pixels for
`Walls_HV_RightToDown_04_Special`. Its rect is `x=56`, bottom-origin `y=0`,
`16×47`, with pivot `0.5, 24/47`. Because Unity stores Sprite rect Y from the
bottom, every earlier Sprite rect receives a +17 Y compensation while its
top-left pixel position remains unchanged. The generator preserves this
user-authored pixel block rather than recreating it.

## Palette layout

The kit is appended to the right of the previous Wall Palette content, starting
at X `57`.

- `(57,25)` / `(58,25)`: vertical Right / Left
- X `57..66`, Y `20`: Left to Down
- X `57..66`, Y `16`: Right to Down
- `(68,16)`: user-authored `Walls_HV_RightToDown_04_Special`
- X `57..66`, Y `11`: Left to Up
- X `57..66`, Y `6`: Right to Up
- X `57..66`, Y `1`: the ten third-Tile special variants
- `(57,-1)` / `(58,-1)`: the two shifted-strip special Tiles

The separated rows leave enough room for the 47-pixel-high Sprite
bounds without visually overlapping adjacent families.

The Wall Palette must retain `GridPalette.cellSizing = 100` (Manual). Automatic
cell sizing can use the tallest supplementary Sprite as its display cell and
make every pre-existing wall Tile appear artificially reduced in the Tile
Palette window.

## Usage

- Use the connector whose horizontal-wall direction and vertical travel match
  the intended path.
- Continue a vertical run with the corresponding `Right` or `Left` body Tile.
- Connector suffixes `00..09` follow the same wall-texture sequence as the
  horizontal wall family.
- Do not rotate or mirror these perspective-sensitive pieces.

The generator is `Tools/GenerateVerticalWallConnections.py`; the independent
validator is `Tools/ValidateVerticalWallConnections.py`.

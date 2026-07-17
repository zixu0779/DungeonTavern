# Diagonal Wall Layout

`Walls_Diagonal.png` is the formal supplementary atlas for continuous diagonal wall
construction. Its Sprite slices have matching Tile assets in the `Dungeon_Walls`
Tile folder. The atlas deliberately contains no starts, ends, straight-wall
interfaces, or corners.

## Compatibility settings

- Atlas size: `352×533` RGBA.
- Import: Sprite Multiple, `16 PPU`, Point filtering, no mipmaps, no compression.
- `16 PPU` is an explicit compatibility exception for the current
  `Walls_interior.png`, whose existing wall widths and slices are authored on a
  16-pixel grid. It does not replace the project's long-term `32 PPU` environment
  target.
- Alpha is binary (`0` or `255`) and all pixels are hard-edged.

## Source wall pattern

The authoritative wall pattern is the ten unscreened `16×46` pieces from the
`Walls_interior.png` row at Unity rectangles `x=0..159`, `y=98..143`, retained in
their original left-to-right order. Together they form one `160×46` master. Every
diagonal series maps this exact master onto new geometry; no regular brick pattern,
mirrored decoration, grid overlay, or newly invented ornament is introduced.

## Atlas organization and slicing

The three slope bands begin at top-left image Y coordinates `8`, `230`, and `372`.
Within each band, Up begins at X `8` and Down at X `184`; each connected master is
160 pixels wide. Each master is sliced into ten 16-pixel-wide Sprites in order.

- `1:1`: each piece rises 16 pixels and is sliced as `16×62`.
- `2:1`: each piece rises 8 pixels and is sliced as `16×54`.
- `3:2`: rises follow `11,10,11` phases, continued as
  `11,10,11,11,10,11,11,10,11,11`; slices are `16×57` or `16×56`.
  These values describe only the Sprite-boundary displacement. Pixel columns use
  one continuous `floor((2x+1)/3)` phase across the complete 160-pixel master, so
  the one- and two-pixel stair runs alternate without restarting at slice edges.

Names use `Walls_Diagonal_<1x1|2x1|3x2>_<Up|Down>_<00-09>`.
Up and Down mean rising or falling when read from left to right. Both directions
retain the same original wall-pattern order and differ only in diagonal geometry.
Custom pivots compensate for sub-cell displacement against these Tilemap paths:

- `1:1`: `0,1,2,3,4,5,6,7,8,9`
- `2:1`: `0,0,1,1,2,2,3,3,4,4`
- `3:2`: `0,0,1,2,2,3,4,4,5,6`

Down uses the corresponding negative Y values. Series are used in numeric order;
the `09→00` wrap preserves the opaque 46-pixel structural span.

## Wall Palette layout

The six series occupy X `42..51` in `Dungeon_Walls`, one ten-piece series per row:

- Y `7` / `6`: `1:1` Up / Down
- Y `4` / `3`: `2:1` Up / Down
- Y `1` / `0`: `3:2` Up / Down

This leaves two empty columns after the previous content and one empty row between
slope groups. Existing Palette cells outside this block remain unchanged.

## Review preview

`Walls_Diagonal_Preview.png` is `720×533`: the formal atlas is shown once on a dark
background and once on a checkerboard. Review at 100% or integer nearest-neighbour
scales to inspect the top trim, wall motifs, lower rubble, base, and binary-alpha
edges without interpolation.

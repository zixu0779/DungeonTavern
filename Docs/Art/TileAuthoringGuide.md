# Dungeon Tavern — Tile Authoring Guide

This document records the project workflow for Sprite slicing, Tile assets, Palettes, and Tilemap painting.

## Scope and preservation

- Work only on the images, slicing data, Tile assets, Palettes, and scenes explicitly named by the task.
- Preserve unrelated art and user edits. Do not normalize every atlas merely because one atlas needs repair.
- Preserve formal asset paths and GUIDs when replacing approved image pixels, unless a migration is explicitly requested.
- Candidate images must not replace formal images until they have been reviewed and approved.

## Final-size authoring rule

- Author every pixel-art Tile and atlas directly at its final production dimensions.
- A `32×32` Tile must be created and edited on a `32×32` pixel canvas. An `8×8` atlas of those
  Tiles must be authored as the final `256×256` image rather than produced by shrinking a larger
  image.
- Never use “draw large, then downscale” as the production workflow. It is prohibited even when
  the final resize uses nearest-neighbour sampling, because the original pixel decisions and
  interface geometry were made at the wrong scale.
- High-resolution images may be used as concepts or style references only. Reconstruct the final
  asset natively, pixel by pixel or Tile by Tile, at the target dimensions.
- Nearest-neighbour upscaling is permitted for `400%` inspection images and other previews, but
  preview dimensions must never be written back as the formal source.
- If a task explicitly requires converting an existing differently sized source, preserve the
  original, output a candidate, document the conversion, and perform pixel-level review before
  formal replacement.
- Before approval, verify important details at native scale: 1–2 pixel cracks, mortar or plank
  seams, damage silhouettes, repairs, Alpha edges, and all cross-Tile interface coordinates.

## Sprite slicing

- Manual slicing is supported and preferred when automatic slicing merges pieces, clips shadows, or fails to represent the atlas structure.
- New rectangular Sprite regions may be added manually when the source contains usable material not covered by existing rectangles.
- Use Auto slicing only where it produces correct independent pieces.
- Use Grid slicing only where the source was authored on a reliable regular grid.
- Mixed atlases should be divided by semantic groups rather than forced through one global Grid operation.
- A Sprite rectangle is always rectangular. Preserve necessary shadows inside the rectangle; transparent pixels may represent an irregular silhouette.
- Do not expect Palette painting to crop an unsliced Sprite dynamically. If a partial piece must be painted independently, it needs its own Sprite region.
- Keep Point filtering, integer pixel scaling, appropriate PPU, no mipmaps, and no lossy texture compression for pixel-art Tile sources.
- Environment Tiles currently use `32 PPU`; the authoritative camera uses a `640×360` pixel-perfect reference resolution.

## Palette organization

- Organize Palettes by scene function, not by Cainos/CraftPix source package.
- Keep categories visually separated by one empty row.
- Within a category, preserve the source atlas's spatial arrangement when that arrangement communicates adjacency, direction, animation, or assembly.
- Do not sort a manually authored atlas only by generated Sprite number.
- Keep compatible crack connectors adjacent in the Palette where their relationship can be understood visually.
- Keep four corner-damage pieces assembled as a visible large-hole example where space allows.
- Wall pieces should be grouped by structural role and direction, not merely by filename or atlas membership.
- Avoid overlapping or tightly interleaved category blocks that make individual Tile roles difficult to identify.

## Current Ground Palette contract

- Ground Tiles are grouped as intact/old stone, cracks, damage, repairs, and mixed variants.
- Categories are separated by blank rows.
- Seamless connector Tiles retain meaningful spatial relationships.
- Four-corner damage Tiles include an assembled large-hole layout.
- `Ground_Cracked_Seamless.png` and `Ground_Cracked_Autotile.png` are formal sources; their approved Sprite slicing and Tile references must not be silently regenerated.

## Current scene and legacy Tilemap status

The authoritative work scene is the 2.5D
`Assets/Scenes/Tavern/Tavern_Main.unity`.

- The 2.5D scene consumes source Sprites, wall Prefabs, materials, and meshes rather
  than painting its final environment through a Tile Palette.
- The former 2D scene, Palettes, and Tile wrappers have been retired. The source
  atlases that remain in active art folders are retained because the 2.5D scene
  and Prefabs still consume them directly.
- Restore a Tilemap workflow only when explicitly requested; do not treat the old
  Archive or scene-backup paths as available sources.

## Validation before handoff

- Confirm that each newly created pixel-art source was authored at final native dimensions and
  was not downsampled from a larger generated image.
- Confirm formal image dimensions, Sprite count, PPU, Point filtering, mipmap state, and compression settings.
- Check for missing Sprite or Tile references and red cells in every modified Palette.
- Verify category spacing and assembly examples at normal Palette zoom.
- Paint a small test area to check seams, perspective, sorting order, transparency, and repetition before filling the full scene.
- Confirm Unity compilation and Console health after editor tooling or import-setting changes.

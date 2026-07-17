# Dungeon Tavern — Environment Art Direction

This document defines the stable visual language for environment art. It supplements `Docs/GameDesign/GameDesignBible.md`; it does not define gameplay systems.

## View and perspective

- The game is top-down pixel art viewed from an oblique angle, not from a perfectly vertical camera.
- Floors are seen mostly from above, while walls must expose a readable vertical face and a sense of height.
- Wall tops, wall faces, corners, pillars, arches, and door openings are distinct construction pieces. A sprite being stored under `Walls` does not mean it can be repeated in every direction.
- Furniture must use the same oblique perspective as the environment. Fully overhead tables, shelves, counters, or other props must be corrected, replaced, or kept out of final scene dressing.
- Do not rotate or mirror perspective-sensitive sprites merely to manufacture a missing direction unless the result has been visually reviewed.

## Mood and lighting language

- The tavern belongs to a secluded corner of a dungeon floor. It should feel enclosed, old, and subterranean, not like a bright outdoor fantasy inn.
- The base environment uses dark, desaturated blue-grey stone. Slightly warmer local materials may be used for mortar, aged wood, repairs, firelight, and inhabited areas.
- Architecture should generally be darker than movable props, but darkness must not erase masonry, cracks, silhouettes, or usable-space boundaries.
- Avoid uniform grey darkening. Preserve local hue variation and material separation so the scene remains readable.
- Grass, bright outdoor greenery, and obvious surface-world terrain language do not belong in the ordinary tavern floor treatment.
- Final 2D lighting and post-processing remain undecided. Do not bake a temporary lighting experiment into the permanent palette colors without approval.

## Pixel-perfect presentation

- The authoritative scene uses the URP `PixelPerfectCamera` component.
- Assets PPU is `32` and the reference resolution is `640×360` (16:9).
- Upscale Render Texture and Pixel Snapping are enabled; Stretch Fill is disabled.
- Pixel Perfect Camera Filter Mode must be `Point`, not `RetroAA`; RetroAA performs a bilinear final blit and visibly softens Tile edges.
- Pixel-art quality must be judged in Game View at the reference resolution or an integer multiple such as `1280×720` or `1920×1080`.
- Arbitrary Scene View zoom levels may look uneven and are not the final rendering reference.

## Stone floor language

- The formal Ground baseline is `Ground_Cracked_Seamless.png` and `Ground_Cracked_Autotile.png`.
- Stone should look old rather than newly manufactured: retain restrained stains, wear, uneven color, and slight edge variation.
- Mortar and brick boundaries must remain readable at 100% display scale. Global darkening must not make the small-brick structure disappear.
- Cracks must be visibly distinct from ordinary straight mortar lines. Their path should be irregular, with controlled 1–2 pixel weight rather than a uniform black stroke.
- Damage holes are volume loss, not merely thick cracks. Their interiors use deeper stone and rubble colors rather than pure black.
- Hole interiors may contain two or three large, low-contrast, irregular depth regions. Avoid gradients, concentric circles, scattered single-pixel noise, and obvious repeated stamps.
- Corner-damage variants may combine across four Tiles into one larger irregular hole. Their shared outline and internal depth interfaces must connect exactly.
- Repairs should be sparse and plausible: usually one or two differently colored aged stones aligned with the masonry, not identical central patches repeated across many Tiles.
- Seamless crack and damage Tiles should be placed through compatible spatial relationships; they are not assumed to be arbitrary noise variants.

## Walls and architecture

- Construct walls by semantic role: top surface, visible face, inner/outer corner, end cap, pillar, doorway, and transition.
- Before using a wall Sprite, inspect what direction and structural role it depicts. Do not treat an entire atlas as a single repeatable wall strip.
- A usable wall must communicate both the walkable floor boundary and the vertical obstruction. If only a top strip is visible, the result is too close to a vertical overhead view.
- Wall faces should be dark enough to establish depth but retain readable joints, chips, and material texture.
- Door openings and sealed future areas should be expressed through coherent wall construction, not by placing an unrelated decorative block over a gap.

## Furniture and decoration

- Props should support the shared tavern used by creatures of different sizes without creating species- or faction-exclusive visual zones.
- Keep silhouettes readable against architecture. Avoid props whose value and hue merge completely into the floor or wall behind them.
- Decorations must not introduce outdoor grass or lighting assumptions that conflict with the underground setting.
- Perspective correctness takes priority over retaining a source-pack asset. An incompatible prop may be replaced or redrawn when explicitly approved.

## Review criteria

Review environment art at both 100% and enlarged nearest-neighbour scale:

- At 100%, walls, floor structure, cracks, damage, doors, and prop silhouettes must be immediately readable.
- At enlarged scale, pixels must remain hard-edged, with no anti-aliasing, interpolation blur, compression artifacts, or accidental semi-transparent fringes.
- Repetition, disconnected seams, incompatible perspectives, overly black damage, and lost masonry outlines are blocking issues rather than optional polish.

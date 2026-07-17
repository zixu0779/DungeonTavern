# DungeonTavern project guidance

Before changing gameplay, scenes, narrative, or art direction, read
`Docs/GameDesign/GameDesignBible.md`.

Before changing environment art, sprites, palettes, tilemaps, or scene presentation,
also read `Docs/Art/ArtDirection.md` and `Docs/Art/TileAuthoringGuide.md`.

Before starting or continuing the current environment-art production tasks, also read
`Docs/Art/EnvironmentArtHandoff.md` for task priority, current asset state, and review gates.

## Stable constraints

- The tavern occupies a secluded corner of a dungeon floor. It is not the dungeon entrance.
- The dungeon-entry animation belongs to a separate opening scene.
- The tavern has one shared public entrance and serves every creature equally.
- Adventurers and dungeon creatures share the hall, bar, and mixed seating area. Do not create faction-only public zones.
- Hostile magic fails inside the tavern. An active attacker is bound before harm is dealt. Severe offenders are permanently refused entry. Ordinary weapons still physically exist.
- Initially available areas are the hall, bar, mixed seating, kitchen, and storage.
- Performance, lodging, and quiet private-room areas unlock later through story or achievements.
- Do not introduce a conventional building-upgrade system.
- The core gameplay loop, RPG systems, event structure, and detailed narrative systems remain undecided. Never promote a provisional idea into implementation without explicit user confirmation.

## Maintenance

- Record newly confirmed design truths in `Docs/GameDesign/GameDesignBible.md`.
- Keep this file concise; only copy rules here when they must constrain every future task.
- Preserve existing user assets and project changes unless deletion or replacement is explicitly requested.
- Treat `Assets/Scenes/Tavern/Tavern_ReadabilityPrototype.unity` as the current authoritative work scene.
- Treat `Assets/Scenes/Tavern/Tavern_Main.unity` as the superseded greybox until an explicitly approved milestone replaces it.

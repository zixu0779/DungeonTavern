# Dungeon Tavern — Game Design Bible

This document is the durable source of truth for confirmed game and world design. Items under **Open questions** are intentionally undecided and must not be implemented as final systems without explicit confirmation.

## Confirmed world canon

- The game is a pixel-art RPG about operating a tavern inside a dungeon.
- The tavern is located in a secluded corner of one dungeon floor, not at the dungeon entrance.
- The opening may show a character entering the dungeon, but that sequence belongs to a separate intro scene.
- The player is the tavern owner. Handling tavern affairs reveals the world and advances the story.
- The 2.5D narrative presentation has three confirmed forms: rare opening/memory
  cinematics may use voice-over; short in-world reactions use head bubbles;
  branch-bearing owner/NPC conversations use a close dialogue view with the owner
  left, NPC right, and dialogue/choices at the bottom of the screen.
- Optional observation and information questions return to their dialogue hub;
  each hub has a separately motivated progression choice, and meaningful choices
  either converge intentionally or persist a later consequence.
- NPC-initiated conversations use active approach: the NPC follows the player
  until entering a broad 1-1.5 tile trigger range, keeps at least roughly half a
  tile of personal space, then stops player movement and begins the close view.
- The close dialogue camera frames the owner on the left and the NPC on the
  right. World head bubbles persist until replaced by another bubble or closed
  by the start of a formal dialogue.
- Customers settle at the counter-side settlement point. Concurrent customers
  form a spaced queue and advance when the customer ahead finishes.
- NPC movement inside the tavern uses walkable-route navigation rather than
  direct movement toward a target. An NPC-initiated conversation requires both
  a complete reachable route within conversation range and an unobstructed
  line between the speakers, so walls cannot trigger dialogue through them.
- The lightweight service loop includes an explicit order cycle: a seated
  customer states an order in a head bubble, the player records it, prepares the
  matching item at its service point, delivers it, and later handles settlement.
- Day 1 uses a mechanical hanging rope on the wall at `(35.5, 0, 22)` as the
  open/close control. Eve walks to the adjacent guide position before presenting
  its bubble, and pulling the rope automatically changes the public-entrance sign.
- The tavern treats all creatures equally, including adventurers and native dungeon creatures.
- There is one shared public entrance, one shared bar, and mixed seating. Public areas are never segregated by species or faction.
- The owner created the tavern and its peace rules after a long first-floor conflict between adventurers and dungeon residents. All conventional routes to deeper floors are sealed; the sole controlled route is beneath the tavern. The route is open to any species that meets its peace conditions, although deep residents use it most often.
- The principal seal combines layered wards, rare "anchor crystals", a control core, and a past Wish-like reality change. The owner can no longer cast Wish normally. A later emergency reconfiguration locked the route and cost the owner memories tied to the sealing incident, while leaving their ordinary knowledge and protective strength intact.
- The antagonist bypassed the seal's purpose check with one unique relic found
  in a small ruin in the dungeon's deeper levels. Mira's black stone fragment is
  a different remnant originating from the same ruin, not a broken piece of the
  unique relic and not a second equivalent relic. Their shared origin may be
  established through related magic, material, or markings without revealing
  the unique relic's full identity or function during Chapter 1.
- The seal chamber is one level below storage. After the opening cinematic, the
  player begins prone in that chamber, regains control by standing and returning
  to storage, then meets Eve when she hears movement from the hall.

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

- The B1 seal room is authored as `Assets/Scenes/SealRoom/SealRoom_B1.unity`, a separate additive content scene roughly one third the footprint of the tavern. The player is authored in B1 and becomes persistent at runtime before B1 is unloaded; `Tavern_Main` retains the camera, business systems, and narrative state. Stair travel uses a short fade.

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

## Confirmed Chapter 1 character and consequence constraints

- Nox is a neutral-to-evil supplier who buys, stores, and resells dungeon
  materials, not an innocent hired courier. He may personally deliver valuable
  or sensitive orders as part of his own trade. He commonly supplies deep-level
  materials to other buyers, but was not the tavern's established deep-material
  supplier. The owner deliberately ordered the suspicious shipment from Nox
  while investigating newly circulating deep-level materials: if Nox could
  provide fresh goods under an old-stock story, the order would expose a route
  worth tracing. Thus "old order" means an order placed before the owner's
  disappearance, not that the goods are truly old.
  Nox knows the shipment is suspicious and uses the old-stock claim as cover.
  He values profit and plausible deniability over the victims or legality of a
  transaction. Repeated questioning must expose evasiveness through wording or
  low-cost visible action text. He is not required to be the mastermind and
  should not freely confess everything he knows.
- Mira may either retain the black fragment or entrust it to the tavern. The
  choice must persist and trigger different branch content beyond the current
  three-day slice. The exact advantages, risks, and outcomes of either custody
  choice remain to be designed.
- Mira leaves the tavern immediately after her Day 2 conversation, regardless
  of the later Nox branch. After completing his transaction, Nox stays for a
  drink and creates another service round unless questioning has pushed him into
  visibly flustered, stammering answers. Sweating or looking away alone does not
  make him leave; once he stammers, he completes the transaction and departs.
- Peaceful passage does not prohibit ordinary exchange between the two sides of
  the seal: legitimately gathered or traded deep-level materials may circulate.
  The suspicious Chapter 1 shipment instead contains material that can only be
  taken by killing protected peaceful deep creatures, making its fresh condition
  evidence of a prohibited hunt rather than evidence that all deep trade is illicit.
- Cross-seal shipments use a **passage declaration** (`通行申报单`):
  the traveller declares the time, cargo, purpose, and planned storage location;
  after approval, the control core stamps a passage number onto the declaration.
  This declaration is distinct from the tavern's own order and inventory records.

## Change log

- 2026-07-16: Created the durable design bible and recorded the confirmed tavern setting, peace rules, initial/locked areas, visual direction, and intentionally open gameplay questions.
- 2026-07-20: Confirmed the oblique top-down perspective, made `Tavern_ReadabilityPrototype` the authoritative work scene, reclassified `Tavern_Main` as the superseded greybox, and linked the dedicated art-direction and tile-authoring documents.
- 2026-07-20: Configured the authoritative scene camera for pixel-perfect rendering at 32 PPU with a 640×360 reference resolution.
- 2026-07-27: Promoted the user-corrected `Tavern_Main` back to the authoritative 2D
  work scene and defined the rotation prototype as a separate derived experiment.
- 2026-07-28: Approved the 2.5D orthographic presentation, promoted it to
  `Tavern_Main`, preserved the former 2D scene as `Tavern_Main_2D_Backup`, and
  archived 2D-only Palette and Tile authoring assets.
- 2026-08-28: Confirmed the sealed deeper-route background and the Chapter 1 narrative premise: after waking with sealing-incident memories missing, the owner reopens the tavern to investigate through returning guests and surviving evidence.
- 2026-08-29: Confirmed the three-form 2.5D narrative presentation contract:
  opening/memory voice-over cinematics, in-world head bubbles, and close owner/NPC
  dialogue with bottom-screen choices.
- 2026-08-29: Confirmed dialogue-choice flow rule: optional observation and
  information choices do not force scene progression; progression is a separate,
  motivated choice unless alternatives intentionally converge or branch.
- 2026-08-29: Confirmed Day 1 opening flow: player-controlled exit from the
  seal chamber to storage, Eve's return-key conversation, an in-world tavern
  opening button at the bar, then Bran's arrival and active approach to settle.
- 2026-08-29: Confirmed Day 1 closing flow: Eve handles bar, stock, and empty
  tables during service; when she judges the day complete, she returns to the
  wall switch and the player presses it to end the business day.
- 2026-08-30: Confirmed that the unique purpose-deception relic and Mira's black
  fragment are separate remnants from the same small deep-level ruin; Nox is a
  knowingly evasive neutral-to-evil merchant; and custody of the fragment
  creates a persistent post-Chapter-1 branch.
- 2026-09-02: Confirmed that the player is authored in the B1 seal-room scene,
  while cross-scene camera, HUD, and narrative systems bind to the persistent
  player after additive loading.
- 2026-09-02: Confirmed active NPC-to-player dialogue approach distances,
  persistent head bubbles, left-owner/right-NPC close framing, and queued
  counter-side customer settlement.
- 2026-09-02: Confirmed that a pursuing NPC does not cross scene portals with
  the player, but waits at that area's entrance and resumes pursuit when the
  player returns; NPCs use the same automatic doors as the player. Close
  dialogue uses one scrollable transcript inside a single background, keeps
  per-conversation history, clears it when a new conversation begins, and
  places plain-text choices at the end of that transcript.
- 2026-09-02: Confirmed that any NPC actively approaching the player to begin a
  conversation displays its upcoming first spoken line in a persistent overhead
  bubble until the close dialogue starts.
- 2026-09-02: Confirmed close-dialogue occlusion handling: choose the least
  obstructed nearby camera yaw first, then temporarily fade only remaining
  occluding renderers and restore their original materials when dialogue ends.
- 2026-09-03: Confirmed NavMesh-based tavern NPC movement and wall-safe dialogue
  triggering based on complete route distance plus a final line-of-sight check.
- 2026-09-03: Confirmed the first lightweight customer-order cycle: visible
  request, player order acceptance, preparation, delivery, and settlement.
- 2026-09-03: Chose the mechanical hanging-rope presentation for the Day 1
  business switch, including Eve's in-world guidance and the entrance sign state.
- 2026-09-04: Confirmed the counter menu/order presentation: customers visit the
  menu first, then queue along the long counter and turn outward at the inset
  staff gate; the menu groups pending customers by item, shows their portraits,
  quantities, and prices, removes an order on service, and the HUD persistently
  displays tavern money. The counter uses one unified-color prefab with two inset
  automatic staff gates instead of overlapping light/dark geometry.

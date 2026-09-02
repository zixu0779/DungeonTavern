# Dungeon Tavern — Game Design Notes

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
- Customers queue at the menu before ordering, then move to reserved seats.
  They receive food, eat, and settle in place before leaving; there is no
  separate settlement queue.
- NPC movement inside the tavern uses walkable-route navigation rather than
  direct movement toward a target. An NPC-initiated conversation requires both
  a complete reachable route within conversation range and an unobstructed
  line between the speakers, so walls cannot trigger dialogue through them.
- The lightweight service loop is menu queue, order confirmation, reserved
  seating, preparation and delivery, eating, and settlement. An order may have
  multiple dishes and quantities; delivered portions can be eaten while the
  customer waits for the rest. Settlement requires all portions to be served
  and consumed. Seat reservation and physical arrival are separate.
- Day 1 uses the placed FloorLever model as the open/close control, replacing
  the hanging-rope placeholder. Eve walks to the adjacent guide position before
  presenting its bubble; operating the lever changes the public-entrance sign.
- The cup dispenser activates on the first interaction and presents a floating
  wooden cup; the next interaction takes the empty cup and deactivates the dispenser.
  The barrel fills only a held empty cup. Chests offer an action matching their
  current state and close automatically when the player moves away.
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

## Confirmed character identities

- Eve is a female elf.
- Mira is a female tiefling and an appraiser. Her former adventuring party
  subjected her to racial prejudice while relying on her expertise. Her refusal
  to falsify appraisals brought that prejudice into the open and precipitated
  her departure; she retains her professional integrity and agency.
- Bran's species remains undecided between an orc and a suitable underground
  people such as grey dwarves. The protagonist's detailed appearance remains
  a design proposal, not a locked character model.

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
- Existing environment sprites and imported 3D models coexist. Preserve current
  source textures, sprite slicing, meshes and materials outside an explicit edit.
  Legacy Tilemap/Palette production is retired.

Current visual and source-directory guidance: `Docs/Art/ArtDirection.md`.

## Open questions

The following are not design commitments:

- Expansion of the existing lightweight service loop and daily structure
- RPG attributes, skills, progression, and failure states
- Event generation and resolution rules
- NPC schedules, AI, relationships, and reputation
- Detailed narrative structure and quest model
- Technical implementation of the no-fighting rules
- Exact conditions and ordering for opening expansion areas
- Future changes to the existing camera, collision, navigation, and interaction systems
- Final internal room boundaries, furniture arrangement, and circulation measurements for the current tavern layout
- Final lighting and post-processing configuration

## Current scene status and contract

- The chest lid opens around its rear hinge. The tavern sign has two stable
  states and independently authored opening and closing animations. The current
  TavernSign prefab includes the user-adjusted mechanism housing. Preserve its
  authored poses and pivots when editing animation keys.

- The generated L-shaped bar, its two staff gate leaves, and fixed hinge posts
  form one reusable bar Prefab. Staff gates swing around vertical hinges and
  retain independent automatic opening and closing; they no longer lift upward.
  `Tavern_Main` uses `Assets/DungeonTavern/Art/Models/Bar/Bar.prefab`; the old
  `Bar_Unified` placeholder Prefab and its dedicated material have been removed.

- Hierarchy cleanup: the bar sits directly under `Tavern_Main/Environment/Bar`;
  automatic kitchen/storage door triggers belong under `Environment/Walls/Doors`,
  and the walkable floor collider belongs under `Environment/Floor`.
  `Gameplay` is a child of `Tavern_Main`; live host services are named `Runtime`.
  The obsolete greybox table, foundation group, legacy marker, inactive test
  customer, unused storage-entry marker, and unused closing marker are removed.
- Storage stair travel now connects F1 and B1 in both directions. Day 1 narrative
  is enabled and uses the current `B1Return_StoneStairArrival` as its storage
  arrival reference. The storage-return gate runs only while that host content
  is active, preventing overlapping B1 coordinates from starting Eve's scene.
  Business dependencies are validated when the day begins, after F1 is visible.


- The B1 seal room is authored as `Assets/Scenes/SealRoom/SealRoom_B1.unity`, a separate additive content scene roughly one third the footprint of the tavern. The player is authored in B1 and becomes persistent at runtime before B1 is unloaded; `Tavern_Main` retains the camera, business systems, and narrative state. Stair travel uses a short fade.
- The current formal build enters through `Tavern_Main`, which keeps the screen
  black until the initial B1 content and player placement are ready, then fades
  in. In the Unity Editor only, playing directly from `SealRoom_B1` temporarily
  loads the `Tavern_Main` camera and persistent systems additively for preview;
  this editor convenience path is excluded from player builds.
- While B1 is active, the additive `Tavern_Main` host keeps only its persistent
  systems and camera active; its environment, characters, and gameplay content
  are hidden. Those content roots are restored when the player enters the tavern.
- Initial loading preserves the player position and rotation authored in B1;
  it must not overwrite them with fixed coordinates. Stair travel resolves an
  arrival Transform in the destination scene after loading and uses its current
  world position and rotation. Moving an arrival object updates travel without
  editing code; renaming or reparenting it requires updating the portal's path.

- `Assets/Scenes/Tavern/Tavern_Main.unity` is the authoritative 2.5D work scene.
- The current camera is orthographic, pitched 45 degrees, with four fixed yaw
  headings (45, 135, 225, 315). Q/E rotates 90 degrees with a 0.22-second eased
  transition. Mouse orbit is disabled. WASD moves the player.
- The serialized output resolution is 1920x1080. Exploration orthographic size
  is 4.5; dialogue has its own closer framing. Ordinary 3D scene materials use
  Unlit to retain authored colors. A future softly lit setup is proposed only.
- Characters currently remain billboard sprites. The protagonist's 3D model,
  skeleton and expanded action library have not yet been produced.
- Only Day 1 is wired to the current narrative controller. Days 2 and 3 exist
  in Ink and are not yet complete playable scene flows. The opening cinematic
  is a placeholder; prone-to-standing animation is still planned.
- F1/B1 stair travel is connected; the unfinished F2 entrance is temporarily
  blocked by collision. The existing cup, barrel, lever and chest interactions
  use F and state-dependent prompts.
- Active runtime assets live directly under `Assets/DungeonTavern`, organized as
  `Art`, `Gameplay`, `Narrative`, `Rendering`, and `Scripts`; the redundant
  `Tavern25D` compatibility layer is retired.
- The retired 2D scene, Palettes, and Tile wrappers are no longer retained in the
  active project. Their former Archive and scene-backup folders must not become
  dependencies of the 2.5D workflow.
- Exact internal geometry, circulation, furniture arrangement, and gameplay systems
  remain pending review in the authoritative 2.5D scene.

Integration markers must be checked in the current scene rather than copied
from old layouts. F1 narrative arrival currently references the authored
`B1Return_StoneStairArrival` beneath the descending storage stair.

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

## Documentation maintenance

- 2026-09-18: Reconciled this document with current scene configuration and
  runtime scripts. Removed obsolete chronological layout instructions and
  duplicate service-flow descriptions. Design intentions (including waking
  prone) remain separate from implementation status above.
- Source assets: `ArtSource/Props/Concepts` and `ArtSource/Props/AIGenerated`.
  New character source work belongs under `ArtSource/Characters`.
- Proposed animation work is tracked in `Docs/Characters/CharacterActionPlan.md`;
  it is not evidence that those animations already exist.

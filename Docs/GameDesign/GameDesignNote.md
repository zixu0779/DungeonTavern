# Dungeon Tavern — Game Design Notes

This document is the durable source of truth for confirmed game and world design. Items under **Open questions** are intentionally undecided and must not be implemented as final systems without explicit confirmation.

## Confirmed world canon

- The game is a pixel-art RPG about operating a tavern inside a dungeon.
- The tavern is located in a secluded corner of one dungeon floor, not at the dungeon entrance.
- The opening may show a character entering the dungeon, but that sequence belongs to a separate intro scene.
- The player is the tavern owner. Handling tavern affairs reveals the world and advances the story.
- The 2.5D narrative presentation has three confirmed forms: rare opening/memory
  cinematics may use voice-over; short in-world reactions use head bubbles;
  branch-bearing owner/NPC conversations use a close dialogue view with dialogue/choices at the bottom of the screen.
- Optional observation and information questions return to their dialogue hub;
  each hub has a separately motivated progression choice, and meaningful choices
  either converge intentionally or persist a later consequence.
- NPC-initiated conversations separate the 4.2 metre trigger range from the
  2.7 metre speaking distance (current spacing review: doubled from 1.35). Walls block triggering; marked counters do not.
  On triggering, lock the player's position, turn them toward the NPC and start
  the close camera transition. The NPC continues along a walkable route, including
  detours through the counter gates, until within speaking distance with a clear
  line between actors. Only then start the formal dialogue. A stalled approach
  releases the player instead of starting dialogue from too far away.
- The close dialogue camera uses one of the two views perpendicular to the speaker
  pair. Compare 18 visibility rays (nine per actor) at the two hypothetical camera
  poses; choose fewer obstructed rays, breaking ties by shorter rotation. Do not
  search nonperpendicular angles. Actor left/right ordering may swap to improve visibility. World head bubbles persist until replaced by another bubble or closed
  by the start of a formal dialogue.
- Customers queue at the menu before ordering, then move to reserved seats.
  They receive food, eat, and settle in place before leaving; there is no
  separate settlement queue.
- Customers walk at the player's configured movement speed; Eve walks 8% faster.
- NPC movement inside the tavern uses walkable-route navigation rather than
  direct movement toward a target. An NPC-initiated conversation requires a clear line between speakers except
  for marked counters. The NPC may detour around the counter during approach;
  walls cannot trigger dialogue through them.
- Seated customers occupy the actual stool surface. Navigation approach points
  are separate from sitting poses; seated bodies leave the approach aisle clear.
  Seat facing uses the actual tabletop mesh center and axes, not the stool distribution.
  Round-table seats face the table center; long-table seats face inward perpendicular
  to the table edge. Guests turn before sitting, retain facing through service and
  standing up, and preserve seated animation across floor visibility changes.
  Customer interaction measures the visible body within 2.4 metres, prioritizes
  nearby guests in front of the player, and rejects blocked lines through walls.
- Tableware disappears automatically after settlement; manual tableware collection is not part of the current demo.
- The owner may close at any point during service. With guests present or future arrivals still scheduled, an
  exclamation bubble announces temporary closure; the owner turns toward the hall
  and remains free to move and interact. All guests start leaving
  together with randomized symbol complaints, and the entrance closes only after
  they leave. This uses the player view, cancels unpaid orders without revenue,
  and preserves unspawned waves for reopening; it does not complete Bran's story.
  Turning toward the hall is eased and yields to movement input. Normal closure
  requires an empty tavern, no further arrivals, and Eve's closing-time reminder;
  it does not trigger the owner's announcement. Eve gives that reminder only
  after service and the Day 1 story gate are complete, while the tavern is open.
- UI uses independent layered overlay canvases with a shared dark iron, copper
  and parchment theme. See `Docs/Art/UIDesign.md`; follow-up work is tracked in
  `Docs/GameDesign/NextDevelopmentPlan.md`.
- Management HUD (coin/customer icons and values), M and the physical ledger
  unlock after the first Eve conversation reaches the opening-lever gate. The
  HUD plays one staggered fade/slide entrance; changing floors does not replay it.
- Dialogue shows the current line and separate choices, with model portraits.
  Full dialogue history is available from dialogue review and the pause menu.
  Spoken text omits only its enclosing quotation marks; history retains the
  original line. Speaker plaques fit and center the speaker name. Dialogue choices are centered,
  unnumbered and mouse-only; their history retains original text. Choices are
  fixed-position, without scrolling. Multiple choices accompany the preceding
  line; a lone choice becomes the protagonist's action/speech in the dialogue
  box and continues without a choice button.
- Dialogue camera yaw is perpendicular to the character pair, selecting between
  two opposing headings. Choose fewer blocked rays out of 18, then shorter rotation
  for a tie. Orthographic sight lines exclude triggers and other characters.
  Dialogue framing eases over 1.3 seconds. Remaining architectural occlusion uses
  local world-space cutout channels toward the camera, not whole-wall fading.
  Eligibility belongs to complete wall faces grouped in the Hierarchy, including
  their door frames and leaves. Actual occlusion starts the shared cutout. While occluded, probes also sample
  the next 0.3 seconds of movement (at most 0.65 m) to admit approaching walls.
  Prediction is limited by physical wall clearance and retained when stopped at
  a still-occluded corner. Loading reveals a prepared cutout instead of its startup animation.
  New groups fade into the existing footprint over 0.22 seconds without growing
  a separate opening. Unhit groups stay intact; clear space cannot start prediction.
  Corners separate wall groups, and dialogue combines both speakers' hit groups.
  Exploration follows the
  player, using a spherecast that also triggers at narrow doorways; dialogue joins
  both speakers into a continuous opening sized for their full silhouettes. NPC
  cutouts only exist during dialogue. Floors, colliders and interaction blocking
  remain unchanged. Masks move smoothly with stable locomotion dimensions. Cut interiors
  reuse the wall surface texture and colour. B1 stone gates participate; small stone doors retain their original rendering.
  Small hinged door leaves remain opaque in every state; fixed frames share wall
  cutouts, and door state never pauses the surrounding wall dissolution;
  stairs and other non-wall props remain opaque. Rendering sections do not alter wall collision. The F1/B1 floor extends beneath
  walls to support the cutaway visually; stairwell openings remain clear.
  The protagonist smoothly turns toward the speaker over 0.35–1 second, according to
  turn angle. Temporary-closing turns use the same speed and yield to movement.
- Lightweight guidance announces B1/F1 on entry and tracks first successful
  awakening, exit, lever, menu, cup, filling, serving and settlement actions. A pending
  action offers a guide button beside the pause menu from the start of the game,
  until all eight first-time actions have been completed. After 25 actionable seconds
  it breathes with warm gold light on a three-second cycle, retaining a visible minimum glow. Clicking toggles the card and route together; between actionable steps it shows
  a quiet waiting message without a route.
  Unfinished F1 guidance persists in B1, targeting the return stairs until F1 resumes.
  Pause, dialogue, ledger and travel do not consume this delay. Completed steps
  do not repeat during the run, including actions completed before their hints.
  No quest log, rewards or additional progression system is introduced.
- The pause menu freezes simulation and input, offers disabled save/load entries,
  current control instructions, and an exit confirmation. The tavern menu has
  a dish-summary tab with pending quantities and customer counts and an orders
  tab whose detailed content is not implemented yet.
- The lightweight service loop is menu queue, order confirmation, reserved
  seating, preparation and delivery, eating, and settlement. An order may have
  multiple dishes and quantities; delivered portions can be eaten while the
  customer waits for the rest. Settlement requires all portions to be served
  and consumed. Seat reservation and physical arrival are separate.
- Day 1 uses the placed FloorLever model as the open/close control, replacing
  the hanging-rope placeholder. Eve walks to the adjacent guide position before
  presenting its bubble. Normal opening and end-of-day closing temporarily lock player control,
  pans the camera to the public entrance, opens/closes the door, plays the full
  corresponding sign animation, then pans back and restores control. Closing stops
  new admissions immediately; opening starts service after the mechanism finishes.
  W/A/S/D or Q/E during the entrance shot returns the camera to the player early;
  the door and sign finish their sequence. The interrupted return uses the same
  duration as the outward pan, following the moving player throughout. Closure before the closing-time reminder follows the
  temporary-closure flow above without an entrance shot.
- The cup dispenser tracks missing cups for current drink orders rather than new-order
  events. Delivered cups cover delivered portions until settlement; a held empty/full
  cup covers one unserved drink. While a deficit remains, the halo stays active and
  each collected cup is replaced by another floating cup. It powers down only when
  the deficit reaches zero. Ordinary demo menu orders use a filled cup as a temporary substitute for any
  portion, so their food portions also request cups. Authored non-proxy food orders
  do not request cups. Shared portions are counted once; personal portions receive
  substitute delivery before shared portions.
  The barrel fills only a held empty cup. Chests offer an action matching their
  current state and close automatically when the player moves away. During lid
  animation the prompt retains its previous open/close action until motion ends.
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

- On 2026-10-01, the user selected protagonist concept B: compact low-poly
  proportions, dark hair with a grey forelock, blue-grey travel clothing and an
  asymmetric torn black cloak with the hood destroyed. The current concept is
  `ArtSource/Characters/Concepts/Protagonist/concept.png`. The model is now imported and replaces the B1 player visual; core action checks passed, while full visual clipping acceptance remains pending.

- The protagonist went out to investigate the truth and was ambushed. Their
  opening appearance, waking prone in the seal chamber, retains damaged travel
  clothing and a torn black cloak, not tavern workwear. The hood is completely
  destroyed, with at most cloth remnants remaining, exposing the head and face.
  The departure memory may still show the intact cloak concealing their face.
- Eve is a female elf.
- On 2026-10-05, the user broadly accepted Eve's compact character concept:
  chestnut bun, green eyes, cream blouse, moss-green work dress, short apron
  and a small belt-mounted ledger. The user approved slightly longer upward/backward
  pointed ears and a simple copper leaf clasp on the green hair band; the revised
  `ArtSource/Characters/Concepts/Eve/concept.png` was accepted for three-view production.
  This accessory does not establish wider elven cultural lore. Separate `front.png`,
  `left.png` and `back.png` references are now prepared in the same folder for review;
  all three use an elevated A pose, with arms approximately 45 degrees away from
  the torso. Preserve the concept's sleeve length and round hands with minimal
  exposed wrist; do not lengthen bare forearms. The ledger retains the concept's book-to-body
  size; do not shrink it to hide it from the back. It sits snugly on her anatomical
  left-front waist, within the body silhouette and fully hidden from the back.
  The leaf clasp remains on her anatomical left. The user supplied the generated Eve GLBs on 2026-10-05. The shaded model now has
  a fitted Humanoid rig and replaces the Rogue visual in Tavern_Main. Original GLBs
  and the editable Eve_Rig.blend are retained. Idle is adapted to Eve with subtle upper-body breathing and stable legs;
  Eve has her own reduced-stride, sole-calibrated Walk clip. The avatar and pose
  samples passed; Play Mode guidance reached its destination with a complete path
  and no Console errors. The apron hem weights were matched to the adjacent skirt surface; final close-up appearance remains subject to user acceptance.
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
  are hidden. Runtime customers belong beneath the tavern CustomerPoints content
  root, so they hide and pause with it. Arrival scheduling pauses while the tavern
  content is inactive; returning restores customers with their orders and seat
  reservations intact. Those content roots are restored when the player enters the tavern.
- Initial loading preserves the player position and rotation authored in B1;
  it must not overwrite them with fixed coordinates. Stair travel resolves an
  arrival Transform in the destination scene after loading and uses its current
  world position and rotation. Moving an arrival object updates travel without
  editing code; renaming or reparenting it requires updating the portal's path.

- `Assets/Scenes/Tavern/Tavern_Main.unity` is the authoritative 2.5D work scene.
- The current camera is orthographic, pitched 45 degrees, with four fixed yaw
  headings (45, 135, 225, 315). Q/E rotates 90 degrees with a 0.22-second eased
  transition. Mouse orbit is disabled. WASD moves the player.
- Demo camera shortcut: C follows the current menu-queue head through ordering
  and travel to its seat, then returns to the previous player follow target one
  second after the seated animation reaches SeatedIdle. C again cancels; an empty
  queue leaves the camera unchanged. Dialogue or loss of the customer restores
  the previous target. This changes presentation only, not customer behavior.
  Switching to the customer and returning use a 0.65-second eased camera pan;
  cancelling mid-pan starts the return from the current camera position.
- The serialized output resolution is 1920x1080. Exploration orthographic size
  is 4.5; dialogue has its own closer framing. Ordinary 3D scene materials use
  Unlit to retain authored colors. A future softly lit setup is proposed only.
- The playable protagonist uses the generated design B Humanoid rig with movement-driven Idle
  and Walk, plus a right-arm pickup/holding layer and a right-hand cup anchor.
  Eve uses the KayKit Rogue and customers use the KayKit Barbarian. The obsolete first-design protagonist assets have been removed. Current Mage/Rogue/Barbarian resources are preserved under `Assets/DungeonTavern/Art/Characters/ThirdParty`; importing the new protagonist must not overwrite them.
  Models are uniformly scaled so protagonist/customer shoulders sit above the counter.
  Customers use SitDown, SeatedIdle and StandUp at seated service positions.
  The protagonist also has seating clips and a callable drink presentation;
  player chair interactions and drink consumption rules are not introduced.
  Final NPC appearances and story-specific animations remain pending.
- Only Day 1 is wired to the current narrative controller. Days 2 and 3 exist
  in Ink and are not yet complete playable scene flows. The current Demo skips the
  opening cinematic text and starts with the prone player; movement input starts
  the face-down, two-hand-supported floor-to-standing animation. The later Eve narrative remains enabled.
  Movement and F interaction stay locked until standing. Space vaulting drives
  a full-body animation with hand contact alongside the collision-controlled arc.
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

## Customer ordering presentation (2026-09-18)

- Customers physically reach the queue in front of the placed menu and face toward the preceding queue position when stopped; the head faces the menu. Solo guests order when first in line. Parties queue individually, then fan out
  to distinct reachable positions beside the menu when their leader reaches the head.
  All members start choosing after everyone reaches their position; thinking is `...`.
- The customer then visibly displays the ordered item icon and quantity before leaving the menu queue for their seat. The order remains visible while travelling and waiting for service.
- After delivery, eating is communicated without descriptive text using the item icon and decreasing progress. After eating finishes, the bubble reads `结账`; after payment the customer leaves.

## Customer seating (2026-09-18)

- Solitary solo guests prefer an empty long table, then an empty small round
  table, then a shared long-table seat whose immediate neighbours and opposite
  seat are empty, then another available long-table seat, then an empty large
  round table. If none qualify, they use a reserved standing position in the hall.
- Sociable solo guests use the same table priorities, but do not rank personal
  space within an occupied long table. Solo guests never join occupied round tables.
- Parties contain 2–4 people, randomly chosen, and exclusively reserve one empty
  large round table. Unused stools remain unavailable to other parties and solo
  guests until the last reserved member releases their seat. Reservations count
  while customers are still in the menu queue.
- Ordinary arrivals favour solitary guests over sociable guests, and sociable
  guests over parties. Party arrivals are excluded when no eligible large round
  table is empty. Inspector weights are tunable; their initial 6:3:1 values are
  implementation defaults, not a locked design ratio.
- Parties order together but retain individual personal orders and settlement.
  Each member can order at most two personal dishes and one drink; the whole party
  can order at most two shared dishes. Shared dishes appear in every member bubble,
  accept delivery through any member, and are consumed and charged once.
- Confirmed dishes occupy the upper bubble area, with animated upward confirmation;
  the lower area shows thinking, the current dish, or a shared proposal/vote.
  Members think independently and finish their current confirmation before joining
  a shared proposal. The proposer holds dish + question mark; others briefly show
  a question mark, then independently accept or rarely reject. A single rejection
  rejects the proposal; unanimity adds it to everyone's confirmed area. All wait
  until ordering is finished before departing for their seats together.
- The first-day authored Bran arrival stays a single customer. Additional ordinary
  arrivals are configured separately from the story schedule.

### One-day demo arrivals

- Bran and ordinary guests may enter during the same service period. Ordinary
  guests arrive in exactly six waves, in addition to Bran. Waves attempt admission
  every six seconds; no seventh ordinary wave is generated.
- The first three ordinary waves are a party, a sociable guest, and a solitary
  guest. Later waves use the configured weights. A full seating area or obstructed
  entrance defers the wave until space is available; there is no five-person cap.
- The lever can interrupt service at any time using the temporary-closure flow
  above. Personal bills remain individual; shared dishes are charged once to the party's first member during normal service.

### Demo menu and temporary serving

- Shared dishes: 铁锅洞菇炖肉, 炭烤穴猪拼盘. Personal dishes: 盐焗岩薯, 黑麦根面包, 酸渍洞蕨.
- Drinks: 深窖麦芽酒, 幽菇淡艾尔, 余烬蜂蜜酒. Ordinary demo guests choose from these;
  Bran retains his authored drink order and story settlement.
- Until food preparation and distinct drinks are implemented, one filled cup serves
  one pending portion. The prompt names the actual ordered dish. Menu counts, cup
  demand and settlement count shared portions only once.
- The cup-dispenser guide uses a reachable floor endpoint for pathfinding and a
  separate fixed visual target on the real dispenser; an elevated final connector
  joins them without routing the player onto the counter.

The demo menu prices and geometric dish icons are prototype tuning/presentation assets, not final economy balance.

- 2026-10-02: Protagonist design B is imported as an independent Humanoid prefab and replaces only B1 Player/CharacterModel. Existing gameplay and third-party source assets are preserved. Front-cloak bone follow is implemented in Unity; full cloth collision is not.

- 2026-10-03: Third-party character models are temporary placeholders and visual references only. They must not appear as final character visuals; Eve, customers and other characters require new designs consistent with the adopted protagonist style. This does not require replacing their reusable animation sources.

- 2026-10-05: Standing Idle should retain subtle upper-body breathing, with slight head and arm follow, rather than being completely frozen. It should show a small upper-body rise visible in the normal gameplay camera, without whole-body bobbing, repeated knee bends, or side-to-side weight shifts. The protagonist and Eve use their original model foot spacing as the standing reference; standing action endpoints should match that stance.

- 2026-10-05: Idle must start at the original model standing height without a downward jump. In-game protagonist/Eve sizes should align with the ThirdParty Mage/Rogue placeholder references using uniform scaling.

- 2026-10-06: Enlarge current runtime character visuals uniformly by 8 percent while preserving relative proportions. Use the ordinary-character size baseline in ArtDirection.md for later imports; Idle must be visibly alive at the normal gameplay camera distance.

- 2026-10-06: Bar redesign proceeds by first compressing the existing bar in place for size review, then confirming a modular corner/straight-section design before model generation. The user has confirmed the current scene dimensions, including 0.95 countertop height, one-tile depth and 1.50 gate widths. Proceed to modular concept design; model generation follows design approval.

- 2026-10-06: The bar counter depth means the width of each L-shaped arm: one existing floor tile (1 world unit), preserving the overall arm lengths.

- 2026-10-06: The bar uses three modular part types: a repeating straight section cut at panel midpoints, a corner extended by half a panel on both arms, and a finished terminal mirrored for left/right use. Keep assembled panels square and join without overlapping framing.

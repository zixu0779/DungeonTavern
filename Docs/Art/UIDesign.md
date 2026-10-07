# DungeonTavern UI

**English** | [简体中文](UIDesign.zh-CN.md)

## Visual specification

Dark iron panels, thin aged-copper borders, cut corners, warm-gold emphasis, and parchment bubbles. Management information stays near screen edges, leaving the center for characters and action. Resource displays and control hints take layout inspiration from Lost Castle 2 without copying its assets.

- Panel: #1B1E1F; deep background: #131619; aged copper: #8F663B.
- Body text: #F0E3C4; emphasis: #E6B361; secondary text: #A6A89C.
- Bubbles: #E6D1A3 with dark-brown text. Drinks and food use dedicated geometric icons rather than emoji fonts.
- Chinese font: bundled Noto Sans CJK SC; license at `Assets/DungeonTavern/Art/UI/OFL.txt`.
- Body 24–28, secondary hints 19–22, headings 34–38, in 1920×1080 reference-canvas units.
- Buttons show hover, pressed, and disabled states. Close, back, and confirm actions are distinct.

## Display layers

The independent runtime root `TavernUI` persists across F1/B1. Objects use Unity UI Layer 5 and Screen Space Overlay canvases with explicit sortingOrder. CanvasScaler adapts to screen size; edge UI respects the safe area.

| Canvas | Order | Content |
|---|---:|---|
| GameOutput | -100 | Game RenderTexture, aspect-preserving with black borders |
| WorldBubbles | 10 | Speech, customer orders, eating progress |
| HUD | 20 | Top-left menu, top-right coins/guest count, bottom-right interaction/carried-item hints, location and first-action guidance |
| Ledger | 40 | Overlay, dish overview and order-details tabs |
| Dialogue | 60 | Dialogue, choices, continue control, opening subtitles |
| Transition | 80 | Startup and floor-transition blackout |
| Pause | 100 | Pause overlay, menu, controls |
| Confirmation | 110 | Exit confirmation |

Bubbles project into the displayed game-image rectangle and remain readable through walls. They hide when off-camera or when their actor is hidden. Decoration and text do not intercept clicks; windows and overlays do. Pause and ledger block world input, but the ledger does not pause business simulation. Ink controls dialogue continuation and branching; pause blocks dialogue advancement.

## Window content

- The ledger shows Day 1, business state, dish icons, prices, pending portions, and ordering-customer counts. Order details remain an empty state. Business states distinguish not opened, preparing to open, open, closing, and finished for the day. Reopening on the same day does not increment the day.
- The dialogue strip removes only paired outer quotation marks extracted after the speaker prefix; history keeps the original text. Nameplates fit their names. Only the current line appears in the strip, with a model-rendered portrait on the right. Narrow centered choice buttons sit above it, without outer quotes or numbering; selection is mouse-only. A lone choice becomes the protagonist's action/speech in the main box, without a button. Multiple choices appear alongside the preceding line. Choice positions are fixed and do not scroll. Review opens the entire current-session history, pauses, and returns to the same line without advancing the story.
- The top-left menu icon has an Esc tooltip. Pause provides save, load, controls, exit, and dialogue history. Save/load are disabled; exit requires confirmation.

## Implementation entry points

`Scripts/UI/TavernUI.cs` manages views and layers; `TavernUiTheme.cs` defines type, colors, and controls; `TavernPanelGraphic.cs` draws scalable borders. Business, story, and pause state remain in their controllers. UI does not mutate orders or narrative progress.

Reference: [Lost Castle 2 official page](https://store.steampowered.com/app/2445690/Lost_Castle_2/)

## Management HUD entrance

The top-right HUD uses coin and two-person icons with values and explanatory tooltips. It unlocks together with M and the physical ledger when the first Eve conversation reaches the opening-lever gate.

- 0.00–0.55 s: panel moves 12 units left into place and fades from 0 to 1 with ease-out cubic.
- 0.18–0.58 s: coin icon/value fade in and move up 4 units.
- 0.30–0.70 s: guest icon/count enter the same way.
- About 0.8 s overall, without scale bounce, number rolling, or looping flashes. Runs only on first unlock. Pause/dialogue hiding suspends the entrance; floor changes do not unlock it again.

## Location and first-action guidance

- After startup and F1/B1 travel, show the location at top center: 0.4 s fade-in and 8-unit downward movement, 2.4 s hold, 0.6 s fade-out. Centered location text does not overlap the top-left expanded guide.
- The guide entry is available from the start. Accepting WASD completes awakening guidance and advances to exit guidance.
- Its button sits right of the top-left menu. The card opens below, using a left gold line and left-aligned text to distinguish it from the location plaque. Body size is 23; footer size is 17 in grey with compact key boxes.
- Clear the old location hint when travel starts. Show the new hint from transparency when travel completes, without restoring the old hint.
- Exit, lever, ledger, cup pickup, filling, serving, and settlement each track first success, including actions performed before the hint. Completed hints do not repeat in that run.
- The quiet guide button remains until all eight first actions finish. After 25 actionable seconds, add a warm-gold glow fading along its cut-corner outline, not a solid rectangle. The tooltip reads `旅途指引`. Its 3 s breathing cycle retains visible gold at minimum brightness. Clicking toggles card and route together; timeout does not auto-expand. Button, card, and route fade over 0.22 s with smoothstep. Awakening has no route. Completing an action closes its card but retains the entry; unavailable actions show a waiting message. Timing is configurable in TavernGuidance; pause, story, ledger, and travel do not count.
- Show one applicable target at a time. Settlement and actions appropriate to the carried item take priority; do not request a cup without cup demand.
- The target uses a low-floating diamond rune, cyan-blue halo, fading ground shaft, and ground spot. Project world positions onto an independent UI layer, readable through occluders without changing scene lighting.
- The route is a continuous soft line 0.28 m above the ground, smoothing corners within path clearance. Its start follows the player each frame; normal movement trims the traversed section. Replan only after deviation exceeds 0.85 m and movement exceeds 1 m, no more often than every 1.5 s.
- F1 uses NavMesh; B1 plans across physically supported floors and steps. Keep the target actor locked until inapplicable and retain the first reachable ground endpoint as the destination marker. B1 physical search is spread across frames with about a 2 ms budget. Standing still or following the path does not replan. Never draw a straight route through walls if no path is found. Entering B1 preserves unfinished tavern guidance, timer, and expanded state, temporarily routing to the return stairs; F1 restores the original target.
- Completion survives floor changes. With no save system, restarting the game resets it.

New windows, choices, interaction/carried-item hints, and bubbles use 0.22 s smoothstep fades. Updating text inside an existing frame does not replay the entrance. Location and management HUD retain their own entrance animations.

Design previews and test screenshots stay outside the project; retain runtime code, fonts, and this specification inside it.

Dialogue framing uses 1.3 s smoothstep position/rotation/zoom. Camera interpolation itself does not change wall materials; the separate occlusion system handles wall cutouts. If input interrupts the opening shot, return over the same duration as the outward pan (currently 1.3 s), following the moving protagonist.

## Group-order bubbles

Confirmed dishes occupy a separate upper area; the lower area shows thinking, confirmation, shared proposals, and votes. Icons rise and return to normal size over 0.45 s into the upper area. The proposer holds dish + question mark; members finish their current confirmation before voting with a check or cross. Acceptance adds the same shared-dish icon to every member.

The dispenser marker targets the real prop. Its ground-route endpoint remains reachable, with a final upward connector.

Multiple pending portions use a compact icon grid; single portions retain their quantity style. Group bubbles avoid overlap and connect to heads with thin lines.

Group-order state dwell times are increased by 25% for readable thinking, personal confirmation, and shared voting. The upward icon animation remains 0.45 s.

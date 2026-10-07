# Character animation status and production plan

**English** | [简体中文](CharacterActionPlan.zh-CN.md)

Basis: GameDesignNote, Chapter01/Day01–03 Ink, and the current dispenser/barrel/lever/chest/service interactions. This is an animation plan, not authorization for new gameplay or evidence that proposed clips exist. Unity currently connects Day 1 only. Day 2/3 Ink can inform future actions but is not integrated gameplay.

Audit date: 2026-10-01. Overall priorities and cross-system acceptance are in the [development plan](../GameDesign/NextDevelopmentPlan.md).

## Protagonist model completion (2026-10-05)

- **Completed and accepted:** Left sleeve shape, elbow-weight transition, and pale cuff repairs, preserving the approved shoulder-cloak structure. Source, Unity model, and animation previews are synchronized.
- Model/rig repairs can enter cleanup with no known blocking modeling work. Service, mechanism, and dialogue actions remain separate future work.
- **Closed; user chose to skip runtime recheck:** End-of-rise foot IK jumps and repeated Idle switching were corrected. Full Editor state-chain sampling passed; Play Mode was not rerun and this is no longer a pending acceptance item.
- Cleanup removed eight one-off pose repair/reconstruction scripts and metadata plus an unreferenced emissive texture. Keep final models, original GLB, editable Blender sources, current animations, and production preview tools. Filling, delivery, and tray carrying were outside this repair round.

## Eve integration (2026-10-05)

- Keep original GLBs; editable rig: `ArtSource/Characters/AIGenerated/Eve/Eve_Rig.blend`; runtime assets: `Assets/DungeonTavern/Art/Characters/Eve`.
- Reuse protagonist skeleton hierarchy, re-rig at Eve's joints, and attach the ledger to the waist. Eve's independent Walk has a reduced leg lift and sole-calibrated height; protagonist clips are unchanged.
- Replace Eve's appearance in Tavern_Main while preserving story/movement components. Avatar, skin, and four-view Idle/Walk samples passed; Play Mode lever guidance reported `ready=True / PathComplete` with no Console errors.
- Apron hem weights follow the adjacent skirt. Sampled walking overlap improved; close-up appearance was accepted on 2026-10-06. No sitting, key handoff, or service gestures were added.
- Rig repair makes soles/uppers follow foot bones rigidly, blending boot tops into shins. Blender, FBX, Walk landing height, and scene visual offset are synchronized. Four-view Idle/Walk and boot-rigidity checks passed; close-up acceptance was completed on 2026-10-06.

## Protagonist deceleration and stopping (2026-10-05)

- Walk cadence follows actual horizontal displacement speed, including deceleration. Remove extra stop-speed smoothing and shorten Walk → Idle to 0.08 s. Only Walk timing changes.
- Gradual deceleration and sudden stops at three gait phases passed. Actual Play Mode entered Idle about 0.082 s after key release, without Console errors. This proves synchronization/transitions, not foot locking on every terrain.

## Stance, breathing, and runtime size (2026-10-06)

- Idle starts with hips/soles at original-model height and original foot spacing. GetUp/StandUp endings and SitDown start use that stance.
- Breathing is authored directly in each protagonist/Eve `Idle.anim`: 4.5 s cycle, peak upper-body displacement 0.032, native Humanoid SpineTDOF with calibrated hip compensation. Asset and controller previews agree. The old runtime breathing component is removed; other basic clips explicitly zero that channel and Walk sole height is recalibrated.
- With the exploration camera (45°, orthographic size 4.5, 1920×1080), protagonist/Eve head rise is about 3.25/3.39 pixels with stationary feet. Two controller cycles and transition to Walk passed. These are isolated/controller checks; final in-game appearance was accepted on 2026-10-06.
- Protagonist, Eve, and runtime customer visuals are enlarged uniformly by 8%, preserving relative proportions. Protagonist/Eve standing visible heights are 2.257/2.347. ThirdParty sources are unchanged; customer scaling is on the `Customer_Test` visual instance.

## Current action audit

Character and mechanism animations are counted separately. An F interaction working does not prove a matching character animation exists.

| Action | Status |
|---|---|
| Protagonist empty-hand Idle/Walk | Integrated; mover drives turning, without a separate turn-in-place clip |
| Pickup, holding, holding-walk | Integrated; pickup still transfers immediately on successful interaction, not on a contact frame |
| Protagonist drinking | Drink uses right-arm IK and cup-tilt curves; callable PlayDrink, without inventory changes or a new drinking key |
| Protagonist sit/hold/stand | Three Animator clips and callable SetSeated; no F chair interaction |
| Eve/customer Idle/Walk | Eve has her elf Humanoid with adapted Idle/Walk; customers remain Barbarian, preserving ThirdParty sources |
| Eve seating | Not required; unused states and standalone Seating.fbx removed without affecting protagonist/customer seating |
| Customer sit/hold/stand | Integrated with waiting/eating/settlement; aligned to stools, Animator pose retained across floor visibility, standing guests stay upright; seats release and navigation resumes after standing |
| Stairs | Reuse Walk; no dedicated stair/foot-placement animation |
| Filling, cup/food delivery | Functional interaction exists; faucet alignment, handoff, and tray animations are missing |
| Lever, chest, ledger | Mechanism/UI works; character lever/lid/viewing actions are missing |
| Eating, customer drinking/payment | Timers/settlement exist; customer tableware/cup and eating/payment animations are missing |
| Dialogue, pointing, key handoff | Dialogue/guidance exists; dedicated gestures/handoff are missing |
| Opening prone/wake/rise/settle | Integrated; face-down asymmetric relaxed arms, first WASD starts rise, control returns when standing; editable Prone/WakeUp retained with original action FBX |
| Bar vault | Space drives full-body action, contact IK, landing and control restoration |

**Movement, holding, drinking/seating, prone rise, and vaulting exist; complete service presentation does not.** Remaining work includes tray carrying/delivery, customer eating, mechanism operation, and dialogue gestures. Tableware disappears after settlement; manual collection is not planned now.

In Edit Mode with B1 open, `Tools > Characters > Preview Opening Pose` previews Prone; `Stop Opening Pose Preview` restores authored transforms. Preview does not save posed bones. Runtime remains driven by Prone.

Opening sequence: Prone → WakeUp → GetUp → Idle. Hands draw inward and plant, both arms support the torso, legs recover and push, then the actor settles. Arms spread naturally when prone. WakeUp lasts 1.2 s; GetUp lasts 2.75 s. Boundaries share the same pose; controls return after standing. `Tools > Characters > Test Opening Rise` starts from the main scene, waits for loading, and checks input lock, stationary rise, control return, and vaulting.

### Validation entry points

The demo skips opening text. First WASD starts the rise; movement/F remain locked until standing. Near a vaultable counter, move toward it and press Space.

`Tools > Characters > Check Opening and Vault` in a fresh Play Mode checks the opening lock and real Bar Prefab landing. It advances the rise; restart Play Mode to experience the opening again.

After the protagonist loads, `Tools > Characters > Check Drink and Seating` checks drinking, cup stowing, seated hold, knee bend, and standing. Temporary items and movement locks restore afterward. Output goes to the system temporary directory: `character-action-check.txt`, `character-drink.png`, and `character-seated.png`. This is automated animation validation, not a full manual service playthrough.

The protagonist CharacterModel's CharacterModelMotion component menu also previews drinking/sitting/standing. Drinking requires a filled held cup. Seating preview needs open space and does not find or reserve a chair.

Latest opening checks passed continuity, input locking, stationary rise, and control return; hand contact and naturalness were accepted. The report also captured a Unity AI plugin networking error, so the entire report was not all-green.

The batches below are an action catalog including polishing of implemented actions. Use the status table; do not treat whole batches as unfinished.

## Batch 1: protagonist basics and existing interactions

| Group | Actions | Use |
|---|---|---|
| Locomotion | Empty-hand Idle, Walk, stop-turn | All scenes; separate world movement from the four camera headings |
| Stairs | Up/down steps | F1/B1/storage; initially reuse Walk with cadence adjustment, later add foot placement |
| Carrying | One-hand cup Idle/Walk | Share empty/full cup clips; liquid changes separately |
| Pickup/place | Reach, retract, hand over/place | Floating cup, serving, keys, small props; hand targets adapt height |
| Filling | Move cup under faucet, hold, retract | Align cup and spout; second-hand faucet operation depends on final interaction |
| Mechanism | Separate lever actions in each direction | Open/close; hand follows lever arc rather than a generic reach |
| Chest | Bend, lift/close lid, straighten | Two clips synchronized with lid state; automatic close needs no character action |
| Inspect | Look at menu/prop, pause | Read orders; do not invent a handheld book for a fixed menu |
| Dialogue | Turn, listen, speak, subtle nod | Eve/guest close dialogue; avoid exaggerated looping gestures |
| Vault | Support, cross, recover | Space already connects contact, crossing, and landing |

## Batch 2: Day 1 and complete service presentation

- Protagonist: prone → support → rise → settle; first movement input triggers it, with no sliding before completion.
- Eve hands over a key; protagonist takes and grips it, relaxing after the memory. Reuse generic handoff; add gripping.
- Eve approaches, stops, turns to guide, and points to the lever, synchronized with navigation arrival.
- Service: carry tray Idle/Walk, place plates/bowls; order recording/accounting may share writing actions.
- Guests: move into position, sit, wait, lift cup, eat, put tableware down, rise, pay, leave.
- Payment handoff can reuse exchange actions; no separate cashier gameplay is required.
- Eve background work: wipe tables, organize the bar, count/carry stock. Start with presentation loops, not a new cleanliness system.

## Batch 3: Day 2/3 and close-ups during later integration

- Mira: place fragment, hand over crystal; protagonist picks up, inspects, senses magic, briefly loses focus and recovers.
- Nox: hand over declaration, wipe sweat, avert gaze; protagonist reads it, compares inventory records, and stores papers.
- Eve: two-hand box Idle/Walk, set down, seal; protagonist opens and inspects seal/contents.
- Core investigation: hold crystal, align and insert into slot, retract, operate/read records.
- Reactions: thinking, confusion, surprise, fatigue, prioritizing small head/upper-body actions reusable with Idle/speech.
- Memories: hammering, yielding a seat/moving luggage, passing salt, serving stew. Produce only for required shots, including hand close-ups if sufficient.

## Shared production requirements

1. Use current 3D skeletal characters; inspect held objects, feet, and occlusion before batch production.
2. Give hands grip anchors, cups/trays/boxes grip references, and faucets/levers/lids hand targets; do not attach cups only to the actor root.
3. Combine lower-body movement with upper-body holding. Empty/full cups need not duplicate Walk; two-hand boxes need their own pose.
4. Pickup, filling, delivery, and key transfer occur on contact frames. Cancellation/interruption must preserve ownership without duplication/loss.
5. Choose root motion or CharacterController as the displacement source, not both. Align position/facing before interaction clips.
6. Check each group from four camera headings, empty/holding, near walls/tables, and on stairs. Camera turns must not change world facing or swing a cup to the other side.

Priority acceptance loop: order → deficit-driven dispenser activation → pickup → holding-walk → fill → serve → empty hand. The functional chain exists; filling/delivery contact animation remains unfinished.

For seated preview, expand `Assets/DungeonTavern/Art/Characters/ThirdParty/Barbarian/Barbarian.fbx`, select SeatedIdle, and play the Inspector preview. At runtime select CharacterModel and observe SitDown → SeatedIdle → StandUp in Animator. `Tools > Demo Flow > Test Customer Presentation` checks seated loops, disable/restore, all seat facings, and interaction occlusion. It restarts Play Mode and exits afterward.

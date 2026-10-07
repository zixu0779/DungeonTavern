# Development and fixes

**English** | [简体中文](NextDevelopmentPlan.zh-CN.md)

## Next: basement environment and control core redesign

- Improve the B1 basement layout, reviewing the control core against character size, circulation, and surrounding space.
- The current control core is too bulky. A complete redesign is confirmed; scaling or patching the current model is not the final solution.
- Confirm layout, functional boundaries, and dimensions before making a new concept. After design approval, proceed to views, modeling, and Unity replacement. The exact form is still undecided.

## Priority fixes

| Item | Remaining work | Acceptance |
|---|---|---|
| Hat brim clipping | Address wide brims near walls, corners, and narrow doors; assess shape, pose, or local avoidance | Reduce clipping while preserving normal traversal |
| Dissolve-edge flicker during dialogue camera turns | Reproduce continuous flicker during slow turns; distinguish cutout projection, noise sampling, and wall-group changes | Preserve slow turns and necessary occlusion handling; stable edges and smooth transitions |

## Further production

Only unfinished work is listed here. Production order follows the next agreed goal.

| Item | Next step and scope |
|---|---|
| Remaining character designs and models | Bran, Mira, Nox, and ordinary guests. Follow the approved protagonist/Eve style: concept approval, individual views, then models and rigs. See the [character brief](../Characters/CharacterDesignBrief.md) for identities and open questions |
| Service and interaction animation | Add pickup contact frames, filling, cup/food delivery, tray carrying, lever and lid operation, ledger viewing, customer eating/payment, and dialogue gestures. Start with pickup → fill → deliver; see the [action plan](../Characters/CharacterActionPlan.md) |
| Exterior transition space | Build a small space outside the tavern within the dungeon, connecting the building to guest arrival/departure points; this is not the dungeon entrance |
| Ledger order-details tab | Extend the existing tab and empty state with personal orders, shared portions, and order status |
| Day 2 and Day 3 integration | Ink scripts exist but are not connected to runtime flow. Add the required characters, scene events, props, and actions alongside integration |

## Open or deferred

| Item | Current boundary |
|---|---|
| Food and drink assets | Filled cups still stand in for food portions. Confirm the scope of dish models, preparation, and drink making first |
| NPC AI expansion | Continue using the current state machine and navigation; a new framework or autonomous behavior is undecided |
| Save/load | Deferred; the demo has no saves, and persistence scope and format are undecided |
| Final lighting | Open for discussion. Keep current Unlit presentation; ambient and local lighting are not confirmed |

## Retained scope boundaries

- Protagonist drinking and seating have previews and callable animation; drinking rules and chair interactions have not been added.
- F1/B1 travel works both ways. F2 is not built and its entrance remains blocked.
- Tableware disappears after settlement; manual collection is not planned for the current demo.
- The opening text cinematic remains skipped.

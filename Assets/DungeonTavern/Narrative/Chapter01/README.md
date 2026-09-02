# Chapter 01 Ink scripts

Open `Chapter01.ink` in Inky. It is the only entry file; the three day files
and `_Variables.ink` are included automatically.

## Ink entry and continuation points

- `start`: chapter opening.
- `day01_after_service`: Day 1 settlement conversation with Bran.
- `day02_mira_settlement`: call after Day 2 service for Mira completes.
- `day03_after_service`: call after Day 3 service completes.

The current Unity controller connects Day 1. Days 2 and 3 are available in Ink
for preview; their complete scene flows are not yet connected.

Unity owns scene movement, camera, interaction triggers, customer scheduling,
and service-state transitions. The Ink files contain dialogue, choices,
narrative facts, and memory branches only. `//` comments document scene and
presentation intent; they are ignored by Inky and Unity.

Inky Player preview uses the default `start` divert. Choose the visible
`(Inky 预览)` simulation options to skip physical play sections and continue
through the chapter.

## Current fact keys

- `eve_key_memory_count`
- `eve_closure_question_count`
- `bran_hammer_question_count`
- `bran_war_question_count`
- `bran_return_question_count`
- `bran_day3_forge_question_stage`
- `mira_team_question_count`
- `mira_relic_memory_count`
- `fragment_custody`
- `nox_source_question_count`
- `nox_trace_pressure`
- `nox_trace_accusation_asked`
- `nox_final_confirmation_asked`
- `nox_became_flustered`
- `material_trace_examined`

Memory counters stop after two viewings. Optional question counters stop after
three answers and control the repeat-question wording. Bran's question facts
unlock his corresponding Day 3 fragments. Mira's relic-memory counter contributes to the Day 3 deduction branches; her
team-answer counter controls repeated questions about her departure. Nox's
transaction records belong to the main flow; the optional material-trace check
preserves an additional investigation clue.
`fragment_custody` preserves whether Mira or the tavern holds the black fragment
for a later branch. Nox's trace pressure remembers whether the crate inspection
followed two or three source answers, while the accusation flag makes the
post-inspection question one-time.

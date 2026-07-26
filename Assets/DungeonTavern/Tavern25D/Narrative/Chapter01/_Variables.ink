// Narrative state only. Count variables are used where a question/recollection
// may be available twice with different wording, then must disappear.

VAR eve_key_memory_count = 0
VAR eve_closure_question_count = 0

VAR bran_hammer_question_count = 0
VAR bran_war_question_count = 0
VAR bran_return_question_count = 0
VAR bran_day3_forge_question_stage = 0

VAR mira_team_question_count = 0
VAR mira_relic_memory_count = 0
VAR fragment_custody = "undecided"
VAR nox_source_question_count = 0
VAR nox_trace_pressure = 0
VAR nox_trace_accusation_asked = false
VAR nox_final_confirmation_asked = false
VAR nox_became_flustered = false
VAR material_trace_examined = false

// External save/checkpoint sentinel. Unity may use it to avoid replaying the
// chapter-end event after loading a completed save.
VAR chapter01_complete = false

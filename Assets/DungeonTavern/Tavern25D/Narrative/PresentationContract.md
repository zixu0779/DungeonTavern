# Narrative presentation contract

Ink determines story state, branches, and player choices. It does not imply that
every prose line becomes an on-screen text box in the 2.5D game.

Use `//` comments to record non-text production notes. Comments are ignored by
Ink and never appear in Inky preview or in the compiled story.

Use full-width brackets such as `【诺克斯移开视线。】` when a small action must
be visible to the player but does not justify a dedicated pixel animation. This
text is part of the dialogue presentation, unlike a `// [动作]` production note.

Spoken owner lines use `你：“……”`. Internal thoughts use `你：『……』`; the
different brackets keep them unambiguous in Inky's plain-text preview. Unity may
add a different text style, but styling is supplementary rather than required.

## Production comments

- `// [回忆过场]`: opening or memory-only montage. It may
  use voice-over and temporarily take control of the camera.
- `// [表现]`: close dialogue view. The owner is framed on
  the left and the named NPC on the right; the bottom panel shows lines and
  choices.
- `// [表现]`: a short, interruptible head bubble in the
  normal 2.5D play view. Use for a character reacting, greeting, or muttering;
  do not use it for branch-heavy conversations.
- `// [场景]`, `// [动作]`, `// [镜头]`, `// [Unity流程]`: scene-direction notes
  for future Unity implementation. They are not commands and Inky does not
  execute them.

All dialogue branches that may affect facts use close dialogue in Unity. A
bubble line may establish a scene but must not contain a player choice. Only
opening and memory knots may use cinematics/voice-over.

## Choice-flow rule

- A pure observation or information question is optional. After it resolves,
  return to the current dialogue hub and hide that option with a fact flag.
- Every dialogue hub must include a clearly motivated option that advances to
  the next scene, service phase, or plot beat; optional questions must never be
  required to unlock it.
- Two choices that both advance may converge, or they may set different facts
  for later consequences. A choice must not auto-advance merely because the
  player examined an object or asked for background information.
- Optional questions may be asked at most three times. The first answer is
  normal; the second repeats essentially the same answer while showing mild
  surprise at being asked again; the third repeats the same answer while more
  clearly acknowledging the repeated questioning. Do not add new story facts
  merely because a question was repeated. The option disappears after the
  third. Repeated question text must sound like natural speech and clearly
  retain the original subject; avoid mechanical prefixes such as "ask again"
  or "confirm again". A deliberate follow-up question may still develop its
  own answer instead of repeating the previous one.
- Optional memory replays are separate from questions: they may be triggered at
  most twice, then disappear.

### Chapter 1 choice audit

- Eve, Day 1: two optional facts (key memory and closure information) return to
  a hub; “今天先开门吧” is the sole progress option.
- Bran, Day 1: ask about the hammer or end the conversation courteously; both
  are deliberate ways to conclude his visit and converge on closing time.
- Mira, Day 2: questions about her former team and up to two relic-memory
  observations return to the dialogue hub. Either custody decision advances to
  Nox's delivery without making the optional material mandatory.
- Nox's goods, Day 2: source questions and a one-time residue inspection return
  to the delivery hub. The inspection becomes available only after two source
  questions and unlocks one non-repeatable accusation about the supposedly old
  goods. Both responses depend on whether inspection began after the second or
  third source answer. Sealed custody and the transaction records are now part
  of the required flow rather than a separate accept/refuse branch.

## Memory-combination rule

Memory scenes are not a one-flag-per-character reward. A character may have
multiple conversation facts. A primary memory can require a named combination
of main facts; independent secondary facts or secondary combinations unlock
additional, smaller scenes. The player may leave at any time, so missed scenes
remain genuine gaps rather than mandatory checklist content.

Chapter 1 applies this to Bran and the Day 2 investigation. Asking about Bran's
hammer unlocks a memory when the same hammer returns on Day 3; learning why he
stopped making weapons adds a character-specific line to that memory. Asking why
he returned unlocks a separate shared-table memory. On Day 3, Nox's source
answers, the crate's seal residue, Mira's stone magic, and Nox's final admission
each add only the memory or inference directly supported by that clue.

## Chapter 1 mapping

- Opening and each `memory_*` knot: cinematic voice-over.
- Eve's reopening conversation, Bran's post-service conversation, Mira's
  relic conversation, Nox's transaction, and the Day 3 conclusion: close
  dialogue.
- Routine greetings and service prompts: head bubbles in the regular tavern
  view. Day 3 storage and seal investigation stays in the normal 2.5D view with
  textual inspection results; it does not introduce bespoke item or log UIs.

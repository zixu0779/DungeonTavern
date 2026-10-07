# Character design brief

**English** | [简体中文](CharacterDesignBrief.zh-CN.md)

Compiled: 2026-10-01.

This document collects recovered appearance proposals for later concept art and modeling. **Compilation is not approval.** Confirmed identities and story facts follow [GameDesignNote](../GameDesign/GameDesignNote.md); current presentation follows [ArtDirection](../Art/ArtDirection.md); animation needs follow [CharacterActionPlan](CharacterActionPlan.md).

Historical source: appearance proposals, species changes, and opening-costume corrections in [Import models into Unity](thread://01a0a8bb-86e4-7ff2-9e67-8be29eac9bc2?hostId=local). This is a synthesis, not a verbatim transcript. The conversation link is local context, not a public GitHub resource.

## Shared constraints

- Use the accepted KayKit-inspired proportions, clear silhouettes, and simple low-poly forms. Earlier four-head or six-to-seven-head proposals are not the current shared proportion standard.
- The protagonist uses design B; Eve has her new elf model. Barbarian remains a customer placeholder, not a confirmed final costume or appearance.
- Characters must work in oblique exploration, close dialogue, sitting, cup holding, and service. Judge recognition in the actual Game View, not only front-view concepts.
- The protagonist's selected B direction (2026-10-01) uses compact proportions, grey-black short hair with a pale forelock, blue-grey travel clothing, and an asymmetric damaged black cloak. The concept is `ArtSource/Characters/Concepts/Protagonist/concept.png`; the editable rig is `ArtSource/Characters/AIGenerated/Protagonist/Protagonist_Rig.blend`. The Unity model is integrated and obsolete first-design assets are removed.
- Produce each character separately. Modeling references use neutral A/T poses, separated limbs, and empty hands. Show occupational props separately rather than permanently attaching every prop to the hands.

## Protagonist / tavern owner

**Confirmed:** Ambushed while investigating the truth, the owner wakes prone in the B1 seal room. The opening uses damaged travel clothing and a torn black cloak. The hood is destroyed, with at most scraps remaining, exposing the head and face. Departure memories may retain an intact face-concealing cloak. Do not use tavern workwear for the opening.

**Selected appearance B:** Human appearance, short grey-black hair with a pale streak, simple face and compact low-poly proportions; blue-grey travel clothes, belt and small pouch, dark trousers, worn brown boots, and an asymmetric damaged black cloak. Use the current concept as the appearance reference. Exact age and origins remain open. Directional references, the replacement model, and basic action integration are complete; large-motion/environment clipping still needs visual acceptance.

**Superseded opening proposal:** Cream rolled-sleeve shirt, charcoal waistcoat, wine-red half apron, and the pale-forelock/red-apron/old-key combination were an early owner proposal. They are no longer the opening-model basis; possible later workwear is also unconfirmed.

## Eve / attendant

**Confirmed:** Female elf familiar with tavern operations.

**Approved appearance (2026-10-05):** Green eyes and copper leaf clasp; chestnut hair in a bun secured with a dark-green band, exposing clear elf ears; cream blouse, moss-green work dress, short apron, and a small waist ledger. Practical clothing supports carrying boxes and trays and wiping tables.

Recognition centers on the bun, moss green, and ledger. The early human proposal is superseded by her elf identity. Rogue is reference-only; the scene uses Eve's new model.

## Bran / old blacksmith

**Identity direction:** A craftsman shaped by war who wants a quiet life. Species is not final; an orc or suitable underground people such as grey dwarves remain options.

**Earlier appearance proposal, unconfirmed:** Broad shoulders and heavy hands, short grey-brown hair, divided beard, indigo workwear, worn leather waist protection, and an ordinary-sized old hammer at the belt. A later grey-dwarf variant proposed limestone skin and a short grey-white beard.

Focus on a broad silhouette, work-worn details, and the old hammer. It is first a long-used tool, not an exaggerated weapon replacing personal history. Finalize grey-dwarf colors and stocky proportions only after species approval; do not import another work's faction or personality assumptions.

## Mira / appraiser

**Confirmed:** Female tiefling and appraiser. Her former adventuring party depended on her expertise while harboring racial prejudice. Refusing to falsify an appraisal precipitated the split. Preserve professional integrity and agency.

**Earlier appearance proposal, unconfirmed:** Practical grey-violet short coat, dark trousers, and a small cross-body appraisal case with rigid compartments. The tiefling variant proposed small swept-back horns, dark copper-red or grey-violet skin, and a simple tail. Short silver-grey hair belonged to the initial elf proposal; carrying it into the tiefling design is undecided.

Focus on horns, grey-violet clothing, and the case: a working professional who travels. Earlier proposals avoided large robes and tall hats to support sitting and handoffs. Horn/tail size needs animation checks. The former elf identity is obsolete.

## Nox / material merchant

**Earlier appearance proposal, unconfirmed:** Middle-aged human man with a somewhat rounded build, swept-back hair and short beard; ochre waistcoat, dark-brown short coat, documents in a chest pocket, and a money pouch at the belt.

Focus on rounded form, ochre, and paperwork. Normally polished and practiced; tension appears through wiping sweat and avoiding eye contact rather than overt villain styling. Species, age, and face in this historical proposal are not confirmed canon.

## Ordinary guest candidates

These are historical production proposals, not finished characters or a fixed arrival list.

| Candidate | Appearance and occupation direction |
|---|---|
| Human man | Young gatherer, ordinary build, light clothing and old backpack |
| Human woman | Experienced shield bearer, short hair, faded blue cloak and light armor; dark skin, short curls, and round shield appeared in an early draft but are not final |
| Lizardfolk | Grey-cyan scales, short snout, ordinary upright porter/traveler; sand-colored sleeveless workwear, shoulder strap, and short tail were proposed and need seating checks |
| Orc | Ordinary height, sturdy underground artisan rather than a giant warrior; earlier gatherer variant had grey-green skin, small tusks, dark-red scarf, and gathering bag; occupation/costume remain open |
| Gnome | Tool merchant or repairer for later expansion; not simply a uniformly shrunken dwarf |

An early goblin repairer proposal used olive skin, large ears, brick-red vest, chest goggles, and a tool belt. A later proposal replaced it with a gnome; the final roster is unconfirmed. All guests share the hall, bar, and seating without species/faction segregation.

## Mage appearance and hat questions

On 2026-10-01, the user confirmed that the wide-brim hat then belonged to the protagonist's temporary model, while a mage identity should still exist. The final mage character and reuse of the current Mage appearance remain undecided.

Treat two issues separately: brim clipping near walls, and poor face recognition from the exploration angle even without the hat. A smaller brim does not solve every face-readability issue.

**Discussion only, not approved implementation:** Establish the hat silhouette; assess shortening or turning up the front brim and narrowing the sides while preserving the crown; then assess local avoidance for remaining clipping. Validate exploration recognition and close-dialogue expression separately. This does not authorize camera, collider, hat-mesh, or pose changes.

## Remaining decisions

The protagonist and Eve have current approved designs and integrated models. Bran, Mira, Nox, and ordinary guests still need final designs. Store concepts per character. The protagonist keeps concept.png and separate front.png, left.png, back.png; do not retain comparison sheets, standalone design explanations, or prompt logs.

1. Shared proportions and distinct silhouettes for the protagonist, Eve, Bran, Mira, and Nox within the KayKit-inspired direction.
2. Bran's final species and still-provisional hair, skin, costumes, and colors.
3. The final mage's identity, hat, and clothing; do not assume the placeholder Mage is the final protagonist.
4. Recognition at exploration distance versus expression in dialogue and portraits.

After approval, update design/art documents and proceed to concept production.

2026-10-03 confirmation: ThirdParty models are temporary/reference-only and must not remain as final visuals. Customers and later characters need designs matching the adopted protagonist. This restriction concerns appearance, not reuse of animation sources.

2026-10-03 protagonist revision: Use Mage, Barbarian, and Rogue as references for rounder large heads, short compact bodies, simple faces, and broad color areas, reducing spiky hair, muscularity, and tiny tears. Retain the white forelock, blue-grey clothes, and asymmetric black shoulder cloak. Concepts/Protagonist keeps the round-hand, short-leg concept and matching views. At that revision point, AIGenerated and Unity models still predated it; concept changes alone did not prove model synchronization.

Current protagonist reference: the round-hand, short-leg version without narrowed boots or an elongated torso has been restored. Existing front/left/back match it. Stop forcing exact ThirdParty dimensions; continue using their rounded masses, simple faces, and colors as references. Match later characters to the approved protagonist and replace placeholders individually. The workflow is concept approval before orthographic views.

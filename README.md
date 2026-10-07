<div align="center">

# DungeonTavern · 地下酒馆

**English** | [简体中文](README.zh-CN.md)

**In a quiet corner of the dungeon, serve adventurers—and their adversaries.**

A 2.5D Unity prototype where tavern management and conversations with guests unfold the story.

![Unity](https://img.shields.io/badge/Unity-6000.5.3f1-222222?logo=unity&logoColor=white)
![Rendering](https://img.shields.io/badge/Rendering-URP-8B6845)
![Narrative](https://img.shields.io/badge/Narrative-Ink-5B7162)
![Status](https://img.shields.io/badge/Status-Prototype-C28A45)

[Features](#features) · [Getting started](#getting-started) · [Roadmap](Docs/GameDesign/NextDevelopmentPlan.md) · [Documentation](Docs/README.md)

</div>

<p align="center">
  <img src="Docs/Images/opening.png" width="960" alt="Day 1 opening: the protagonist and control core in the underground seal room">
  <br><sub>Day 1 opening · Screenshot from the current game</sub>
</p>

## Features

The tavern occupies a secluded corner inside the dungeon. Adventurers and dungeon creatures share its hall, bar, and seating. As its owner, you learn about the world through everyday service and conversations with your guests.

| Run the tavern | Meet the guests | Explore the space |
|---|---|---|
| Open, take orders, serve, settle bills, and close | Ink branching dialogue, follow-up questions, and memory clues | Isometric camera, occlusion handling, and underground floor transitions |
| Solo and group arrivals, queues, and seating | Close-up conversations, portraits, and dialogue history | Modular bar, interactive props, and NPC navigation |

**Current scope:** The Day 1 flow is integrated. Day 2 and Day 3 Ink scripts can be previewed, but their full scene flows are not yet integrated. The demo does not support saving; some service animations and other characters are still in development.

## Characters and art

The protagonist and Eve have been integrated into the game. The images below are character concepts showing the design direction, **not gameplay screenshots**.

<table>
  <tr>
    <td align="center" width="50%"><img src="ArtSource/Characters/Concepts/Protagonist/concept.png" width="360" alt="Tavern owner character concept"><br><b>Tavern owner</b></td>
    <td align="center" width="50%"><img src="ArtSource/Characters/Concepts/Eve/concept.png" width="360" alt="Eve character concept"><br><b>Eve · Tavern attendant</b></td>
  </tr>
</table>

Characters follow a concept → individual views → modeling and rigging workflow. The current environment uses Unlit materials to display texture colors. Models, textures, and Blender source files are retained in `ArtSource`.

## Getting started

Requires **Unity Hub, Unity 6000.5.3f1, Git, and Git LFS**. The first import requires an internet connection to download dependencies.

```bash
git lfs install
git clone https://github.com/zixu0779/DungeonTavern.git
cd DungeonTavern
git lfs pull
```

Repository access follows its GitHub settings. After cloning:

1. Add the project in Unity Hub and open it with `6000.5.3f1`.
2. Wait for package resolution, asset import, and script compilation to finish.
3. Open `Assets/Scenes/Tavern/Tavern_Main.unity`.
4. Enter Play Mode and follow the in-game prompts.

> If models or textures are missing, check that Git LFS downloaded the actual files rather than text pointers. `ProjectSettings/ProjectVersion.txt` is the source of truth for the Unity version.

## Technology and structure

| Technology | Use in this project |
|---|---|
| Unity 6 / C# | Character controls, interactions, and tavern operations |
| URP | 2.5D presentation and occlusion effects |
| Ink | Dialogue, choices, narrative facts, and memory branches |
| AI Navigation | NPC movement and pathfinding |
| Input System / UGUI | Input and game interfaces |

```text
Assets/
├── DungeonTavern/
│   ├── Art/                 # Characters, environments, props, UI, and art shaders
│   ├── Gameplay/            # Prefabs, animation, interaction materials, navigation, transitions
│   ├── Narrative/           # Ink sources, compiled assets, and presentation contracts
│   ├── Rendering/           # URP renderer settings, wall cutaway and occlusion shaders
│   └── Scripts/
│       ├── Player/          # Movement, animation, and carried items
│       ├── Interaction/     # Interaction detection and targets
│       ├── Tavern/          # Business days, orders, arrivals, seating, and service
│       ├── Narrative/       # Story flow and NPC dialogue integration
│       ├── Presentation/    # Cameras and visual presentation
│       ├── UI/              # HUD, menus, guidance, and dialogue portraits
│       └── World/           # Scene loading, floor transitions, navigation, doors, environment
├── Scenes/                  # Tavern main scene and underground SealRoom
├── Settings/                # Rendering and build configuration
└── Editor/                  # Importers, authoring tools, and project checks
ArtSource/                   # Concepts, generated models, Blender, and third-party source assets
Docs/                        # Design contracts, production guidelines, and development plans
Packages/                    # Package manifest and dependency lock file
ProjectSettings/             # Unity project settings
```

## Roadmap and documentation

The next priority is improving the basement environment and redesigning the control core. Further work includes character and service animations, the space outside the entrance, order details, and later story integration. Remaining tasks and known issues are maintained in the [development plan](Docs/GameDesign/NextDevelopmentPlan.md).

- [Game design](Docs/GameDesign/GameDesignNote.md): World rules, confirmed gameplay, and scope boundaries.
- [Art direction](Docs/Art/ArtDirection.md): Visual style and asset production requirements.
- [Character action plan](Docs/Characters/CharacterActionPlan.md): Current support, validation entry points, and remaining animations.
- [Documentation index](Docs/README.md): UI, customer flow, character, and narrative documents.

The project overview and all Markdown documents under `Docs` have English and Simplified Chinese versions. Use the language links at the top of each document. Documentation translation does not add English localization to the game.

## Resources and licensing

Thanks to the following resource creators. Third-party assets retain their own licenses and are not covered by a single project-wide license.

| Resource | Author / source | License and records |
|---|---|---|
| KayKit Adventurers Character Pack | Kay Lousberg / KayKit | [CC0, bundled license](ArtSource/Characters/ThirdParty/KayKitAdventurers/LICENSE.txt) |
| Universal Animation Library — Standard | Quaternius | [CC0](ArtSource/Characters/ThirdParty/Quaternius/LICENSE) · [Source record](ArtSource/Characters/ThirdParty/Quaternius/SOURCE.txt) |
| UI font `TavernSans.otf` | Font asset included in the project | [Bundled SIL Open Font License 1.1](Assets/DungeonTavern/Art/UI/OFL.txt); the original font name and author still need to be documented |

Character concepts and some prop models were produced with AI assistance, followed by manual work, rigging, and texture adjustments. Source files are in `ArtSource`. These assets do not automatically acquire a CC0 license by sharing a repository with CC0 resources.

The table lists resources with existing local source or license records; it is not a complete licensing audit. Sources and redistribution permissions for other environment assets still need to be documented. Ink, Unity packages, and development tools follow their respective bundled licenses.

No unified open-source license has been designated for the project's own code and art. The repository as a whole is not declared to be MIT, CC0, or covered by another single license.

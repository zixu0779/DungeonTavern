<div align="center">

# DungeonTavern · 地下酒馆

[English](README.md) | **简体中文**

**在地下城的一角，招待冒险者，也招待他们的对手。**

一款以酒馆经营与角色对话推进故事的 2.5D Unity 原型。

![Unity](https://img.shields.io/badge/Unity-6000.5.3f1-222222?logo=unity&logoColor=white)
![Rendering](https://img.shields.io/badge/Rendering-URP-8B6845)
![Narrative](https://img.shields.io/badge/Narrative-Ink-5B7162)
![Status](https://img.shields.io/badge/Status-Prototype-C28A45)

[项目特色](#项目特色) · [快速开始](#快速开始) · [开发路线](Docs/GameDesign/NextDevelopmentPlan.md) · [项目文档](Docs/README.zh-CN.md)

</div>

<p align="center">
  <img src="Docs/Images/opening.png" width="960" alt="第一日开场：地下封印室中的主角与控制核心">
  <br><sub>第一日开场 · 当前版本实机画面</sub>
</p>

## 项目特色

地下酒馆藏在地下城内部，冒险者与地下城生物共用大厅、吧台和座位。玩家作为老板，在日常服务与客人的交谈中逐步了解这个世界。

| 经营酒馆 | 认识来客 | 探索空间 |
|---|---|---|
| 开店、接单、送餐、结账与打烊 | Ink 分支对话、信息追问与记忆线索 | 斜俯视镜头、遮挡消除与地下层切换 |
| 独行与结伴客流、排队和座位分配 | 近景对话、人物立绘与对白回顾 | 模块化吧台、交互道具与 NPC 寻路 |

**当前范围：** 第一日流程已接入；第二、三日 Ink 文本可预览，完整场景流程尚未接入。当前 Demo 不支持存档，部分服务动作和其他人物仍在制作中。

## 角色与美术

主角与伊芙已经完成模型接入。以下为角色 Concept，展示设计方向，**不是游戏实机截图**。

<table>
  <tr>
    <td align="center" width="50%"><img src="ArtSource/Characters/Concepts/Protagonist/concept.png" width="360" alt="酒馆老板角色 Concept"><br><b>酒馆老板</b></td>
    <td align="center" width="50%"><img src="ArtSource/Characters/Concepts/Eve/concept.png" width="360" alt="伊芙角色 Concept"><br><b>伊芙 · 店员</b></td>
  </tr>
</table>

角色采用 Concept → 独立视图 → 模型与绑定的制作流程。当前环境以 Unlit 材质呈现贴图颜色，模型、贴图和 Blender 源文件保留在 `ArtSource`。

## 快速开始

需要 **Unity Hub、Unity 6000.5.3f1、Git 和 Git LFS**，首次导入需要网络下载依赖。

```bash
git lfs install
git clone https://github.com/zixu0779/DungeonTavern.git
cd DungeonTavern
git lfs pull
```

仓库访问权限以 GitHub 设置为准。克隆后：

1. 在 Unity Hub 中添加项目，使用 `6000.5.3f1` 打开。
2. 等待包解析、资源导入和脚本编译完成。
3. 打开 `Assets/Scenes/Tavern/Tavern_Main.unity`。
4. 进入 Play Mode，按游戏内提示开始。

> 模型或贴图缺失时，先确认 Git LFS 文件已经下载，而不是仅有文本指针。Unity 版本以 `ProjectSettings/ProjectVersion.txt` 为准。

## 技术与结构

| 技术 | 项目用途 |
|---|---|
| Unity 6 / C# | 角色控制、交互与经营流程 |
| URP | 2.5D 场景呈现与遮挡效果 |
| Ink | 对话、选择、叙事事实与记忆分支 |
| AI Navigation | NPC 移动与场景寻路 |
| Input System / UGUI | 输入与游戏界面 |

```text
Assets/
├── DungeonTavern/
│   ├── Art/                 # 角色、环境、道具模型、UI 与美术 Shader
│   ├── Gameplay/            # 预制体、动画、交互材质、导航与场景切换资源
│   ├── Narrative/           # Ink 剧情源文件、编译资源与呈现约定
│   ├── Rendering/           # URP 渲染器配置、墙体切口与遮挡 Shader
│   └── Scripts/
│       ├── Player/          # 主角移动、动作与持物
│       ├── Interaction/     # 交互检测与交互对象
│       ├── Tavern/          # 营业日、订单、客流、座位与服务流程
│       ├── Narrative/       # 剧情驱动与 NPC 对话衔接
│       ├── Presentation/    # 镜头与场景表现
│       ├── UI/              # HUD、菜单、引导与对话立绘
│       └── World/           # 场景加载、楼层传送、寻路、门与环境
├── Scenes/                  # Tavern 主场景与 SealRoom 地下层
├── Settings/                # 渲染配置与构建配置
└── Editor/                  # 资源导入、制作工具与综合检查
ArtSource/                   # Concept、生成模型、Blender 与第三方源资产
Docs/                        # 设计约定、制作规范和开发计划
Packages/                    # 包声明与依赖锁文件
ProjectSettings/             # Unity 项目设置
```

## 开发路线与文档

下一步优先优化地下室场景并重新设计控制核心；其后还有人物与服务动作、门外过渡空间、订单明细和后续剧情接入。剩余工作与已知问题统一记录在[开发计划](Docs/GameDesign/NextDevelopmentPlan.md)。

- [游戏设计约定](Docs/GameDesign/GameDesignNote.md)：世界观、已确认玩法与范围边界。
- [美术规范](Docs/Art/ArtDirection.md)：画面表现与资产制作要求。
- [人物动作计划](Docs/Characters/CharacterActionPlan.md)：现状、验证入口与待制作动作。
- [完整文档索引](Docs/README.zh-CN.md)：UI、客流、角色和剧情文档。

项目概览和文档索引提供中英文版本，详细设计与实现文档目前以中文维护。文档翻译不代表游戏内已支持英文。

## 资源与许可

感谢以下资源作者。第三方资源保留各自许可，不适用统一的项目授权。

| 资源 | 作者 / 来源 | 许可与记录 |
|---|---|---|
| KayKit Adventurers Character Pack | Kay Lousberg / KayKit | [CC0，随包许可](ArtSource/Characters/ThirdParty/KayKitAdventurers/LICENSE.txt) |
| Universal Animation Library — Standard | Quaternius | [CC0](ArtSource/Characters/ThirdParty/Quaternius/LICENSE) · [来源记录](ArtSource/Characters/ThirdParty/Quaternius/SOURCE.txt) |
| UI 字体 `TavernSans.otf` | 项目内字体资源 | [随附 SIL Open Font License 1.1](Assets/DungeonTavern/Art/UI/OFL.txt)；原字体名称与作者信息尚待补齐 |

角色 Concept 和部分道具模型采用 AI 辅助制作，包含生成后的人工作业、绑定与贴图调整，源文件位于 `ArtSource`。这些资产不因与 CC0 资源同处仓库而自动获得 CC0 授权。

以上列出已有本地来源或许可记录的资源，并非完整授权审计。其余环境素材的来源及再分发许可仍需补齐；Ink、Unity 包和开发工具遵循各自随包许可证。

项目自有代码与美术尚未指定统一开源许可证，当前不声明整仓库为 MIT、CC0 或其他统一授权。

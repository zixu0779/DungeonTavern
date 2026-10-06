<div align="center">

# DungeonTavern · 地下酒馆

**在地下城的一角，招待冒险者，也招待他们的对手。**

一款以酒馆经营与角色对话推进故事的 2.5D Unity 原型。

![Unity](https://img.shields.io/badge/Unity-6000.5.3f1-222222?logo=unity&logoColor=white)
![Rendering](https://img.shields.io/badge/Rendering-URP-8B6845)
![Narrative](https://img.shields.io/badge/Narrative-Ink-5B7162)
![Status](https://img.shields.io/badge/Status-Prototype-C28A45)

[项目特色](#项目特色) · [快速开始](#快速开始) · [开发路线](Docs/GameDesign/NextDevelopmentPlan.md) · [项目文档](Docs/README.md)

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
├── DungeonTavern/    # 游戏代码与运行资源
├── Scenes/          # 酒馆和地下层场景
└── Editor/          # 项目编辑器与验证工具
ArtSource/           # Concept、模型和美术制作源文件
Docs/                # 设计约定、制作规范和开发计划
Packages/            # 包声明与依赖锁文件
ProjectSettings/     # Unity 项目设置
```

<details>
<summary><b>依赖与本地开发工具</b></summary>

`Packages/manifest.json` 声明直接依赖，`Packages/packages-lock.json` 记录解析后的依赖和 Git 提交。两者都纳入版本管理；锁文件由 Unity 更新。

Unity Skills 按[官方 README](https://github.com/Besty0728/Unity-Skills#-quick-start)通过 Git URL 安装，源码缓存在 `Library/PackageCache`，用于 AI 操作编辑器。需要此功能时，在 `Window > UnitySkills` 启动服务，再通过 AI Config 配置使用的 AI 工具。

`Library`、`Temp`、`Logs`、`UserSettings`、`Builds` 和 IDE 自动生成的工程文件不提交。个人窗口布局与偏好保留在 `UserSettings`；不要在 Unity 运行时删除缓存目录。

</details>

## 开发路线与文档

剩余工作集中于人物与服务动作、门外过渡空间、订单明细和后续剧情接入；已知问题与验收状态统一记录在[开发计划](Docs/GameDesign/NextDevelopmentPlan.md)。

- [游戏设计约定](Docs/GameDesign/GameDesignNote.md)：世界观、已确认玩法与范围边界。
- [美术规范](Docs/Art/ArtDirection.md)：画面表现与资产制作要求。
- [人物动作计划](Docs/Characters/CharacterActionPlan.md)：现状、验证入口与待制作动作。
- [完整文档索引](Docs/README.md)：UI、客流、角色和剧情文档。

## 资源与许可

项目使用第三方资源，使用与再分发须遵循各资源附带的许可证。仓库未声明统一开源许可证，不应将代码、美术源文件和第三方资源视为同一授权范围。

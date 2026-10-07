# 当前美术工作流程

[English](ArtDirection.md) | **简体中文**

本参考替代已退役的图块生产与环境交接说明。世界观以 GameDesignNote 为准；本文记录当前表现和资源组织，不将未来提案视为功能承诺。

## 画面表现

- 斜俯视 2.5D 地下酒馆，像素纹理/精灵与 3D 道具共存。
- 当前 Game 相机为正交、俯角 45°，朝向固定为 45/135/225/315°；Q/E 以 0.22 秒过渡旋转 90°，不支持鼠标自由环绕。探索正交尺寸为 4.5，对话临时调整构图。
- 主场景序列化输出为 1920×1080。验收应看实际 Game View，任意 Scene View 缩放不能作为运行效果对比。
- 普通模型采用 URP/Unlit，以保留贴图原色；特效材质单独处理。中性环境光与局部冷暖灯光仍是未来提案，未替代当前方案；Unlit 模型不接收这些光照。
- 地下建筑保持可读：克制的蓝灰石材、温暖木材，以及足够辨认砌缝、轮廓和交互的局部对比。
- B1 使用切墙和位于墙顶高度的平面石质背景；保留楼梯/门洞几何、传送触发器与碰撞支撑。
- 主角使用 3D Humanoid，已有 Idle、Walk 和杯子交互动画；采用已认可的生成设计 B，伊芙采用独立精灵 Humanoid，顾客仍为 Barbarian 占位。当前风格参考 ThirdParty Mage、Barbarian、Rogue 的圆润比例和简洁体块。主角 Concept 已沿此方向修订，但当前运行模型仍基于较早的 B 设计；第三方顾客外观是临时占位，最终需要重新设计。
- 当前主角 Concept 为 `ArtSource/Characters/Concepts/Protagonist/concept.png`，旧 Concept、首版废弃模型和替换前预制体已移除。第三方人物位于 `Assets/DungeonTavern/Art/Characters/ThirdParty`，不额外套供应商目录，保留来源许可。
- 主角与伊芙保持等比缩放，以 Mage/Rogue 作相对尺寸参考。2026-10-06 全部运行人物外观增大 8%；Idle 从原模型站立高度开始，旧场景抬高偏移已移除。
- 退役的 2D 角色图集及闲置引用已清理。坐姿使用三个导入片段；主角饮用由 Animator 曲线驱动手部 IK 和杯子倾斜。

## 局部墙体切口

- `DialogueOcclusionFader` 每 0.1 秒检测当前相机方向，Q/E 旋转期间逐帧检测。脚踝探针半径 6 cm，在脚部刚开始遮挡时激活；身体探针为 12 cm。稳定的根节点相对采样避免脚步抖动和过宽的下身接近触发。
- 每个探针收集人物与相机之间所有墙体命中的 `WallCutoutGroup` 父级。实际命中与短距离运动预测共同允许局部开孔，命中宽限为 0.16 秒。只有已发生遮挡时才向前预测 0.3 秒，最远 0.65 m，并按人物宽度的碰撞余量裁剪。预测命中必须真正与视线相交；在仍遮挡的位置停下时保留有效前瞻。
- 初始加载和传送在黑幕下准备切口，再淡入画面。墙组成员以 0.22 秒、深度一致的点状过渡加入共享范围，不单独放大孔洞。其他墙组即使处于同一遮罩内也保持完整。不使用墙体朝向或人物深度平面启发式。
- 开孔过渡为 0.6 秒，相机旋转时也一样。世界空间切口参考 Brendan Sullivan 的演示：https://www.artofsully.com/projects/WXVnyD 。这是本地 Unity 实现，并非下载作者源码。
- 探索保护主角，对话合并双方通道。移动使用稳定全身尺寸，位置/半径阻尼，以及沿视线投影的两级世界锚定噪声。宽过渡带保留碎片与孔洞，贯穿墙深使用同一噪声场，截面填充不得补回这些孔洞；全身动作按动作调整边界。
- Walls、Walls_Stone、StairRearEnclosure、StoneGates 下的建筑参与，包括 B1 活动石门。小石门固定门框共享所属墙组的资格和噪声场，不必独立命中。墙组在 Hierarchy 中按整面墙设父级、墙角拆组；B1 石门属于 EastWall。对话使用双方命中墙组并集。墙组允许局部孔洞，不能整墙隐藏。
- 门体保留原几何与表面映射；这些门网格禁用推断体积封盖，避免填平或扭曲拱形。小型铰链门扇在所有状态保持不透明，只有固定门框参与切口；开门、关门及全开都不能暂停周围墙体切口。招牌、楼梯、地面碰撞和阴影保持原状。
- 截面复用墙体正面纹理、颜色和 UV。低面数板/门框与真实网格三角形求交，以保留斜接和门洞；高面数石板使用局部体积边界。深凹高面数网格仍需分开制作实体板。
- 分组砖墙未指定的端面复用正面纹理；未切墙组保留原背面，不参与截面填充。
- 面组件保留无关材质参数，移动门更新截面变换。禁用 DialogueOcclusionFader 会恢复原材质和属性块；Enable Sections 只控制截面填充。
- 两场景的 UnderWallFloor 将原地面外观延伸到墙下，略低于原地面以避免 z-fighting；不新增碰撞，不填楼梯井。F1 延伸按实测凹形外墙轮廓裁剪，内缩 8 mm，避免直墙/斜墙外漏像素。
- B1 楼梯砌体样例采用大块修整拱石和压顶，顶/正/侧面分别设材质值。后墙在 z=22.77 结束并接短南向回墙，不露独立墙端。垂直回墙保持独立 WallCutoutGroup；现有楼梯位置和拱下净空不变。门洞资源位于 `Walls/StoneWall/StairPassageSample`。
- 所有 B1 毛石墙使用已认可的顶/正/侧值。压顶厚 0.30 m，覆盖含角柱在内的墙顶并集，面不重叠。网格与本地材质位于 `Walls/StoneWall/B1StoneMasonry`。B1 楼梯雾使用独立有界材质，限制在外部通道；F1 雾行为不变。
- 开场朝向 315°，主角伏地于控制核心旁，双臂放松且不对称，右手在头旁，双脚分开。Prone/WakeUp 为可编辑 Unity 资源，原动作 FBX 保留。`Tools > Characters > Preview Opening Pose` 在 B1 预览而不保存骨骼姿态；Stop Opening Pose Preview 恢复制作变换。WakeUp/GetUp 连续呈现双手撑地、收腿和站稳。B1 主角起点为 (40.62213, 0.08, 17.68641)。

## 源文件与运行资源

- `ArtSource/Props/Concepts`：环境/道具设计，包括墙、楼梯、家具。
- `ArtSource/Props/AIGenerated`：原始生成道具模型与纹理。
- `ArtSource/Characters/Concepts`：人物设计参考。
- `ArtSource/Characters/AIGenerated`：生成人物网格、纹理与绑定源。
- Unity 运行资源位于 `Assets/DungeonTavern`；移动外部源目录不代表要移动已导入资源。
- 人物设计分别保存。绑定候选采用中立 A/T 姿态、四肢分离和空手，先检查拓扑再制作动画。

## 界面表现

运行 UI 使用独立 Overlay Canvas、暗铁面板、铜边、暖金强调与羊皮纸气泡。中文使用随包 Noto Sans CJK SC；布局和层级见 [UI 规范](UIDesign.zh-CN.md)。

## 编辑规则

- 保留请求范围外的用户位置、枢轴、精灵切片和材质色调。只改动画时改关键帧，不改基础变换。
- 移动导入资源使用 AssetDatabase，保持 Unity GUID。
- 目录整理保留原始 Concept/模型内容。
- 尊重每项资源的导入设置。当前地面纹理由 `DungeonTavernGroundTextureDefaults` 使用 32 PPU、Multiple、Bilinear、无 mipmap、无压缩；不要重新套用旧的统一 Point 规则。
- Tilemap/Palette 工具链已退役，不依据旧文档重新生成。
- 在实际场景检查颜色、轮廓、碰撞和动作。编译成功或文件哈希不变不等于视觉验收。

## 保留的编辑器工具

- `DemoServiceCheck`：Play Mode 六波客流与 NPC 朝向。
- `DemoFlowPlayCheck`：开场跳过、排队朝向、缺杯量、入口镜头/门/招牌顺序及楼层支撑。
- `NpcPlacementCheck`：座位路径及伊芙过门。
- `TavernNavigationBake`：按 NPC 身体净空烘焙当前物理障碍。
- `OpeningVaultCheck`：开场输入锁和 Bar Prefab 翻越。
- `CharacterActionCheck`：可重复的饮用/坐姿检查和近景。
- `ScenePortalGizmos`：传送触发墙可视边界。
- `PrototypeFloorEdgeClipperWindow`：应用/恢复选中地面边缘裁切。
- `DungeonTavernGroundTextureDefaults`：Ground PNG 导入策略。

已完成的一次性迁移、修复、截图和验证脚本在 2026-09-18 清理中移除，不是运行依赖。

B1 上行石梯通过原预制体使用 `Stair_Stone_B1_NoArch.fbx`：上拱已去掉、柱脚封口，同目录保留原 FBX。B1 场景采用源自无拱网格的 `Stair_InsideWallFootprint.asset`，去掉后回墙外的悬挑；场景位置和材质不变。

## 人物参考与 Hyper3D

- 每名角色在 `ArtSource/Characters/Concepts/<Name>/` 有独立目录，保留 concept.png 和独立 front.png、left.png、back.png；每张仅一名人物，不上传拼合图。
- 建模前核对三图身份、比例、服装、姿态和解剖学左右；左视图显示人物左侧。
- 使用服务的多视图流程和显式方向字段。Rodin Gen-2.5 REST API 的有序 images 使用 `image_label=["F","L","B"]`；当前暴露的 MCP 缺少 image_label，文件名或提示词不等于方向参数。花费额度前确认支持情况。
- 用户对所请求人物建模已授权上传项目参考图到 Hyper3D，不在同一范围重复确认；仍遵守工具审批和安全限制。
- 不保留废弃迭代、独立设计解释或提示词日志；只保留采纳图片与可用制作资源，临时检查放项目外。

## 主角 Unity 接入（2026-10-02）

- 运行模型、Unlit 材质、2K 明暗纹理、独立控制器和预制体位于 `Assets/DungeonTavern/Art/Characters/Protagonist`；可编辑绑定在 `ArtSource/Characters/AIGenerated/Protagonist/Protagonist_Rig.blend`。
- B1 `Player/CharacterModel` 使用该 Humanoid；移动、交互、碰撞、剧情组件保留，杯子锚点改指新右手。
- `ProtagonistCapeMotion` 在 Humanoid 动画后复现前披肩随大腿抬起；Blender 驱动器不导入。这是骨骼驱动而非布料碰撞模拟，极端姿态的表面拉伸仍有限制。
- ThirdParty 模型/动画保留内容与 GUID，新控制器复用既有片段，不覆盖来源。
- `Tools > Characters > Check Protagonist Rig` 检查 Avatar、披肩抬起/回落和蒙皮引用。开场/翻越、饮用/坐姿功能检查通过；最终开场报告含无关远程 WebSocket 连接错误，不代表四视角或墙/吧台穿模已完整验收。
- 最终人物外观需重新设计并匹配已采纳主角。ThirdParty 仅作临时/参考，不能保证最终风格一致。Rogue 位于 `ThirdParty/Rogue`；伊芙仅使用 Idle/Walk，闲置坐姿状态与独立 Seating.fbx 已移除。
- 主角控制器使用独立 `Protagonist/Idle.anim`，手臂外展减少；空手/持物状态共享基础姿态，手部 IK 控制杯子，第三方原片段不变。

## 伊芙 Unity 接入（2026-10-05）

- FBX、Unlit 明暗纹理/材质、独立控制器、适配 Walk 和预制体位于 `Assets/DungeonTavern/Art/Characters/Eve`；原 GLB 和 Eve_Rig.blend 留在 AIGenerated。
- Tavern_Main 只替换伊芙外观子节点；复用主角骨架层级和 Idle 基础，按伊芙调整关节、权重、步幅和脚底高度。
- Avatar/蒙皮/姿态及 Play Mode 开店引导寻路通过；围裙下沿权重跟随底层裙面，近景已于 2026-10-06 验收。没有因此新增服务动作。

## 人物尺寸基准（2026-10-06）

- 普通成年角色初始接入以约 2.3 世界单位可见站立高度、允许 5% 偏差为基准；保留相对身高，特殊高矮种族另行评估。
- 从原始站姿脚底量至正常头发/头部轮廓。大帽子、角、持物不应迫使身体缩小，必要时同时比较肩部和身体尺寸。
- 在运行外观根节点等比缩放，脚底贴演员地面，Idle 初始高度匹配原模型。不得仅为匹配外观尺寸改变碰撞体或交互距离。
- 在实际探索相机下与吧台和其他人物一起检查。Idle 判断正常镜头可见动作，不只看近景；当前目标为 1080p 上半身约 3 像素起伏，脚底固定。

## 吧台比例评审（2026-10-06）

以下保留比例评审经过；最终运行资源以本节末尾的模块化替换结果为准。

- 原吧台直接在场景评审，不新增灰盒；外观根曾从台高 1.10 压至 0.95，长宽不变。用户已确认高度和占地，作为模块化设计约束。
- 尺寸确认后，以转角与可重复直段设计，不把整个 L 形一次生成；新建模需先通过相应设计确认。
- 对话立绘按头部轮廓统一构图，不再按全身范围；烘焙蒙皮几何不得重复应用 Renderer 缩放。
- 后续澄清：每条 L 臂深度为一块地砖（1.00 世界单位），不是缩短总长度。先前 X 缩短已还原，独立评审网格收窄两臂但保留外跨度及 0.95 高度；原 FBX 保留。碰撞盒、员工门和台面道具随占地更新。取杯器保持等比缩小 10%，底部在 0.95 台面；尺寸已确认并重烘焙寻路。
- 员工门沿通道方向长 1.50。整个吧台沿世界 X 移 +0.4072，长臂跨度仍为 13.3019，短臂世界 Z 延长 +0.0670；东铰链/立柱固定，北铰链/立柱随右移。阻挡/触发器随门宽，道具随吧台。隔离关/开/关检查通过，寻路已重烘焙。
- 模块严格三类：直段在面板中线切，连接两端各留半板，分隔柱位于段内；转角两臂各延长半板；终端有半板连接端与完整外端，另一端镜像使用。拼合装饰板须为正方形，Concept 无文字。保留已确认占地，建模前按两条跨度解决板距/端余量，禁止重叠边框或拉伸方板。当前 Concept：`ArtSource/Props/Concepts/Bar/Bar_Modular_Concept.png`。
- 三件比例修正：保留终端半板比例，所有连接半板净宽:高为 1:2，转角延长为半个净方板高度。直段/转角深度必须达到已确认的 1.00、总高 0.95，不能像薄隔板；这些是物体尺寸，不是透视图测量比例。
- 已用于拼合模型的尺寸正投影参考：`ArtSource/Props/Concepts/Bar/Bar_Modular_Orthographic.svg` 及 PNG 使用统一顶/正/侧比例。高 0.95、深 1.00；方板净边 0.723611、半板 0.361806；直段节距 0.823611、终端长 0.461806、转角外跨度 1.309539。转角 + 14 直段 + 终端长 13.3019；另一臂转角 + 5 直段 + 镜像终端长 5.8894；共享转角在每臂长度中各计一次。当前拼合模型遵循此尺寸，员工门 diffuse 为木色参考。
- 端面修正：直段连接截面与终端外端均为 1.00×0.95 整板，没有框条或凹板。
- `Bar_Modular_Concept.png` 为三件外观图；`Bar_Modular_ThreeViews.png` 行为顶/正/侧、列为直/转角/终端，两者在 `ArtSource/Props/Concepts/Bar`。精确尺寸以 SVG 为准，栅格透视不覆盖尺寸。修正三件拼在 `ArtSource/Props/Blender/Bar_Modular.blend`；运行网格、材质和预制体位于 `Assets/DungeonTavern/Art/Models/Bar/Bar_Modular`。Tavern_Main 保留原吧台变换和交互对象使用替换模型；旧 L 形模型与一格深评审网格已移除。

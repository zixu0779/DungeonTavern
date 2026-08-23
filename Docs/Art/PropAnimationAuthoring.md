# Chest 与 TavernSign 动画调整

Prefab：
- `Assets/DungeonTavern/Art/Models/Chest/Chest.prefab`
- `Assets/DungeonTavern/Art/Models/TavernSign/TavernSign.prefab`

动画位于各自文件夹下的 `Animations`。FloorLever 本轮未修改。

## 不运行游戏，直接预览和调整

1. 双击打开 Prefab，选中根对象 Chest 或 TavernSign。
2. 打开 `Window > Animation > Animation`。
3. 在动画下拉框选择 `Chest_Opening` / `Chest_Closing`，或
   `TavernSign_Opening` / `TavernSign_Closing`。
4. 打开 Preview，拖动时间轴或播放；预览不会保存为 Prefab 的静止姿态。
5. 修改动画时点击红色 Record，选择 LidPivot 或 BoardPivot，在指定时间
   修改 Transform，Unity 会记录关键帧。完成后关闭 Record 和 Preview。

Chest 当前绕 LidPivot 的局部 X 轴开启 25°；修改角度时需同步调整 Opening
末帧、Closing 首帧，以及 Chest_Open 的静止关键帧。

TavernSign 的两段运动是独立的示范路径，不保证符合最终滑槽的几何约束。
Opening 当前 1.25 秒，Closing 当前 1.1 秒，各自记录 BoardPivot 的位置和旋转。
保持三条端点约束：
- Opening 首帧 = Closing 末帧 = TavernSign_Closed。
- Opening 末帧 = Closing 首帧 = TavernSign_Open。
- 只编辑 BoardPivot，固定外框不参与动画。

若要修改 Pivot 的轴位置，先退出动画预览，将子模型暂移至根节点，移动 Pivot，
再把模型放回；这能保留模型的世界位置。已有动画记录了轴的位置，改轴后也需
同步修改对应位置曲线。

## 运行时切换

根对象上的 TwoStateProp 的 Open 复选框表示目标状态：勾选打开，取消关闭。
运行时可在该组件的菜单调用 Open (Play Mode) / Close (Play Mode)，或由代码调用
`SetOpen(true)` / `SetOpen(false)`。

状态机为 Closed → Opening → Open → Closing → Closed。若动画过程中改变目标，
当前动作先完成，再执行另一个方向，不会在滑槽中途跳到另一段动画。

本轮仅提供道具动画，没有新增 F 键交互、开箱物品逻辑，也没有连接营业剧情或
修改现有营业开关。Scene 中尚未放置这两个组合时，可先用 Prefab 动画预览。

## 检查

菜单 `Tools > Dungeon Tavern > Props > Check Chest And Sign Animations` 检查
当前 25° / 180° 示范端点、端点连续性、固定件不动，以及 Animator 双向状态切换。
角度设计改变后，应同步调整检查中的预期角度。

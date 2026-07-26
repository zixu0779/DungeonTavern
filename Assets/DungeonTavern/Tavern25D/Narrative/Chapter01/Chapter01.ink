INCLUDE _Variables.ink
INCLUDE Day01_Reopening.ink
INCLUDE Day02_AnomalousGoods.ink
INCLUDE Day03_RemainingClues.ink

// The top-level divert makes this the default entry for Inky's Player view.
-> start

// First playable chapter entry point.
// Unity should begin with ChoosePathString("start") and resume the named
// *_after_service knots after the BusinessDayController reports completion.

=== start ===
-> prologue

=== prologue ===
// [表现] 开场过场；Unity 播放画面与少量画外音，结束后交还玩家控制。
// [场景] 封印室。

冰冷的石地贴着背脊。

控制核心悬在房间中央，黯得像一块熄灭的炭。它周围的界锚晶槽全是空的；通往更深处的门紧闭，没有留下任何可以撬开的缝隙。

你记得这间酒馆属于自己。
你记得这里不能伤人。
你记得有一扇门绝不能打开。

除此之外，记忆像被人从书页上整齐地裁掉了。

有一句话在你脑海里回响：

“继续营业。留意来客。别轻信表象。”
-> day01_open

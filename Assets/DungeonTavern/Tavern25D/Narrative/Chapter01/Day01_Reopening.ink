=== day01_open ===
// [场景] 第一日开始：主角趴在封印室地面；任意移动输入令其起身。
// [Unity流程] 玩家恢复控制，走出封印室、到达上层储藏室后进入 day01_eve_arrives。
+ [(Inky 预览) 模拟主角起身，离开封印室并回到储藏室。]
    -> day01_eve_arrives

=== day01_eve_arrives ===
// [场景] 伊芙从大厅循声进入储藏室。
// [表现] 主角左、伊芙右的近景对话。
伊芙：“你终于回来了。”

伊芙：“我听见储藏室有动静，就过来看看。”

// [动作] 伊芙把一直代为保管的钥匙还给主角。
伊芙：“这是你的钥匙。我一直替你收着。”

* [接过钥匙。]
    伊芙：“你以前只交代过一句：‘如果我没回来，就把酒馆关了。’所以这些日子，我没再开门。”
    -> day01_eve_conversation

=== day01_eve_conversation ===
+ { eve_key_memory_count == 0 } [握紧钥匙。]
    ~ eve_key_memory_count = eve_key_memory_count + 1
    // [回忆过场] 酒馆关闭当日；可使用短画面与画外音。
    黑斗篷遮住了你的脸。你对伊芙说：“我不回来，就关门。”

    伊芙：“钥匙太凉了吗？你的手一直没松开。”
    -> day01_eve_conversation

+ { eve_key_memory_count == 1 } [再握紧一次钥匙。]
    ~ eve_key_memory_count = eve_key_memory_count + 1
    // [回忆过场] 同一段关闭酒馆的记忆再次闪过；此后不再重复触发。
    黑斗篷遮住了你的脸。你对伊芙说：“我不回来，就关门。”

    伊芙：“要不要先把钥匙收好？”
    -> day01_eve_conversation

+ { eve_closure_question_count == 0 } [“酒馆是什么时候关的门？”]
    ~ eve_closure_question_count = eve_closure_question_count + 1
    伊芙：“从你失踪那天起，一天也没开过。”
    -> day01_eve_conversation

+ { eve_closure_question_count == 1 } [“你还记得酒馆是哪一天关门的吗？”]
    ~ eve_closure_question_count = eve_closure_question_count + 1
    伊芙：“我记得。就是从你失踪那天起，酒馆就一天也没开过。”
    -> day01_eve_conversation

+ { eve_closure_question_count == 2 } [“不好意思，酒馆是哪一天关门的来着？”]
    ~ eve_closure_question_count = eve_closure_question_count + 1
    伊芙：“你已经问过两次了。就是从你失踪那天起，酒馆就一天也没开过。”
    -> day01_eve_conversation

* [“先把酒馆开起来吧。”]
    伊芙：“好，先开半天，让大家知道这间酒馆还在。”
    -> day01_prepare

=== day01_prepare ===
// [表现] 延续伊芙的近景对话。
伊芙：“厨房和储藏室一直都在收拾，今天就能用。”

伊芙：“酒馆规矩还是原来那样，对吧？”

你：“酒馆规矩？”

伊芙：“这里不问来处，也不准伤人。谁先动手，符文会先把谁按住。”

// [场景] 伊芙走到吧台旁墙上的营业吊绳。
// [表现] 伊芙头顶气泡。
伊芙：“拉一下这根绳子，然后我们重新开始营业吧。”

// [Unity流程] 玩家拉下营业吊绳后进入 day01_open_tavern。
+ [(Inky 预览) 拉下营业吊绳。]
    -> day01_open_tavern

=== day01_open_tavern ===
// [场景] 酒馆开门；布兰从公共入口进入。
// [表现] 布兰头顶气泡。
布兰：“门口的牌子终于翻回来了。我还以为酒馆再也不开门了。”

// [场景] 伊芙在营业期间负责吧台、清点存货和收拾空桌。
// [Unity流程] 开始第一日营业；完成服务后进入 day01_after_service。
+ [(Inky 预览) 模拟完成第一日营业。]
    -> day01_after_service

=== day01_after_service ===
// [场景] 布兰离开座位，主动走向主角结账。
// [表现] 主角左、布兰右的近景对话。
布兰：“老板，结账。”

* [替布兰结账。]
    布兰：“谢了。上次坐在这里，已经是很久以前的事了。”
    -> day01_bran_conversation

=== day01_bran_conversation ===
+ { bran_hammer_question_count == 0 } [那把铁锤……？]
    ~ bran_hammer_question_count = bran_hammer_question_count + 1
    布兰：“替人打武器、补甲、修攻城器。它跟我在战场上待得比谁都久。”
    -> day01_bran_conversation

+ { bran_hammer_question_count == 1 } [能不能再说一下这铁锤的事情？]
    ~ bran_hammer_question_count = bran_hammer_question_count + 1
    布兰：“嗯？以前我替人打武器、补甲、修攻城器。这铁锤跟我在战场上待得比谁都久。”
    -> day01_bran_conversation

+ { bran_hammer_question_count == 2 } [不好意思，能不能再说一下这铁锤的事情？]
    ~ bran_hammer_question_count = bran_hammer_question_count + 1
    布兰：“你已经问过两次了。我以前替人打武器、补甲、修攻城器。这铁锤跟我在战场上待得比谁都久。”
    -> day01_bran_conversation

+ { bran_hammer_question_count > 0 } { bran_war_question_count == 0 } [你后来为什么不再替人铸武器？]
    ~ bran_war_question_count = bran_war_question_count + 1
    布兰：“战争停了，活下来的人却没停。后来我不想再替谁把下一场仗打得更久。”
    -> day01_bran_conversation

+ { bran_hammer_question_count > 0 } { bran_war_question_count == 1 } [那段日子，还会让你想起什么吗？]
    ~ bran_war_question_count = bran_war_question_count + 1
    布兰：“会。但我不想每次想起，都只记得炉火和惨叫。”
    -> day01_bran_conversation

+ { bran_return_question_count == 0 } [你为什么还会回来？] //改！
    ~ bran_return_question_count = bran_return_question_count + 1
    布兰：“因为这里以前让人能坐下来吃完一顿饭，不必先问坐在对面的是谁。”
    -> day01_bran_conversation

+ { bran_return_question_count == 1 } [能不能再说一次，你为什么还会回来？]
    ~ bran_return_question_count = bran_return_question_count + 1
    布兰：“因为这里以前让人能坐下来吃完一顿饭，不必先问坐在对面的是谁。”
    -> day01_bran_conversation

+ { bran_return_question_count == 2 } [最后问一次，你为什么还会回来？]
    ~ bran_return_question_count = bran_return_question_count + 1
    布兰：“好吧，这是最后一次。因为这里以前让人能坐下来吃完一顿饭，不必先问坐在对面的是谁。”
    -> day01_bran_conversation

+ [结完账，送布兰出门。]
    布兰：“会再来的。能让人安静吃完一餐的地方不多。”
    // [场景] 布兰离开酒馆。
    -> day01_close

=== day01_close ===
// [场景] 伊芙收完账本和桌面，回到吧台旁墙上的开关旁。
// [表现] 伊芙头顶气泡。
伊芙：“今天差不多了，就到这里吧。”

// [Unity流程] 如果玩家长时间没有按下开关，用下面的催促气泡替换原气泡。
// 伊芙：“一会儿我还要收拾厨房。今天差不多了，就到这里吧。”
// [Unity流程] 玩家按下开关后，门口的牌子由机关自动切换，结束第一日并进入 day02_open。
+ [(Inky 预览) 按下开关，结束第一日营业。]
    -> day02_open

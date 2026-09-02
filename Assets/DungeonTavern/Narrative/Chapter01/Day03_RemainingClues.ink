=== day03_open ===
// [场景] 第三日上午。伊芙完成开门前的准备，站在吧台旁的营业拉杆附近。
// [表现] 伊芙头顶气泡。
伊芙：“先营业吧，有客人来了。”

// [Unity流程] 玩家拨动营业拉杆后，门口的牌子由机关自动切换。
+ [(Inky 预览) 拨动拉杆，开始第三日营业。]
    -> day03_mira_arrives

=== day03_mira_arrives ===
// [场景] 米拉在开门后不久进入酒馆，手里带着一枚颜色很淡的晶石。米拉主动来找主角。
// [表现] 主角左、米拉右的近景对话。
米拉：“昨天回去以后，我翻了翻以前没处理完的委托，找到了一样你留下的东西。”

米拉：“你失踪前把这枚晶石交给我，只让我检查它有没有沾上奇怪的魔力。”

【米拉把晶石放到桌上。】

* [拿起晶石。]
    -> memory_mira_anchor_crystal

=== memory_mira_anchor_crystal ===
// [回忆过场] 主角失踪前把同一枚晶石交给米拉；只展示二人的手和晶石。
你：“这次不用估价。你帮我看看，这块晶石里有没有不属于它的魔力。”

米拉：“嗯哼，如果我找到了呢？”

你：“那就直接来找我。”

-> day03_after_mira_memory

=== day03_after_mira_memory ===
// [表现] 回到主角与米拉的近景对话。
米拉：“我当时没发现别的魔力。可你一直没有回来取，就替你收着了。”

伊芙：“这是界锚晶。控制核心的晶槽全空了，所以一直没有亮。”

伊芙：“这一枚没法做太多事情，但也许能让控制核心暂时亮起来。先收好吧。”

米拉：“东西送到了。正好，我也留下吃点东西。”

-> day03_service_start

=== day03_service_start ===
// [场景] 米拉入座；布兰和其他普通客人随后从公共入口进入。
// [表现] 布兰头顶气泡。
布兰：“前天那顿不错。今天再给我来份热的。”

// [Unity流程] 开始第三日营业；完成对米拉、布兰及其他客人的服务后进入 day03_after_service。
+ [(Inky 预览) 模拟完成第三日营业。]
    -> day03_after_service

=== day03_after_service ===
// [场景] 米拉结账后离开酒馆；布兰留在座位等待结账，玩家走近并交互后进入对话。
// [表现] 主角左、布兰右的近景对话。
布兰：“老板，结账。”

* [替布兰结账。]
    { bran_hammer_question_count > 0:
        布兰：“前天问了我那么多铁锤的事，今天怎么还盯着它看？”
        -> day03_bran_conversation
    - else:
        布兰：“今天也不错。看来这间酒馆是真的重新开起来了。”
        -> day03_bran_return_gate
    }

=== day03_bran_conversation ===
+ { bran_day3_forge_question_stage == 0 } [“我以前有没有找你做过什么东西？”]
    ~ bran_day3_forge_question_stage = 1
    布兰：“做过。一座黑钢基座。你不肯说装在哪里，只说按图纸上的尺寸做，绝对不能差。”
    
    布兰：“怎么，是出什么问题了吗？”
    
    你：『黑钢基座……是封印室的那个吗』
    -> day03_bran_conversation

+ { bran_day3_forge_question_stage == 1 } [“黑钢基座上面有几个槽位对吧，你还记得吗？”]
    ~ bran_day3_forge_question_stage = 2
    布兰：“当然。五个槽位都是照你带来的那枚浅色晶石留的。怎么，晶石装不上？”
    -> memory_bran_hammer

* [“没什么。你坐一会儿再走吧。”]
    -> day03_bran_return_gate

=== memory_bran_hammer ===
// [回忆过场] 同一把铁锤敲打着用于固定封印构件的黑铁支架；只展示手、铁锤和支架。
火星落在尚未冷却的黑铁上。

布兰：“再说一遍，这东西不是武器？”

你：“不是。它只负责把一扇门守住。”

{ bran_war_question_count > 0:
    布兰：“那就好。我的锤子也该做点不会让战争更久的东西。”
}

-> day03_bran_return_gate

=== day03_bran_return_gate ===
{ bran_return_question_count > 0:
    -> day03_bran_return_trigger
- else:
    -> day03_bran_leaves
}

=== day03_bran_leaves ===
布兰：“不用了，明天有空，我还会来。”
// [场景] 布兰离开酒馆。
-> day03_eve_investigation

=== day03_bran_return_trigger ===
布兰：“哈哈，不用了。”

布兰：“前天和今天，没人赶我换位置，也没人盘问我的来历。看来这里确实没变。”
-> memory_bran_return

=== memory_bran_return ===
// [回忆过场] 旧酒馆内，布兰与不同种族的客人坐在同一张长桌旁。
布兰站在长桌旁。一个披着冒险者斗篷的客人把长凳上的行李挪开，给他让出位置。

布兰看了对方一眼，随后坐下。

你把一碗刚出锅的炖菜放到布兰面前：“趁热。”

长桌对面的人把盐罐推了过来。

-> day03_bran_leaves_after_memory

=== day03_bran_leaves_after_memory ===
布兰：“明天有空，我还会来。”
// [场景] 布兰离开酒馆。
-> day03_eve_investigation

=== day03_eve_investigation ===
// [场景] 其他客人陆续离开。伊芙从吧台后取出第二天留下的通行申报单和旧进货簿。
// [表现] 主角左、伊芙右的近景对话。
伊芙：“客人走得差不多了。昨天留下的货、申报单和旧进货簿都在储藏室。”

伊芙：“你不是想查这批货吗？现在没人打扰了。”

* [去储藏室检查货物和记录。]
    -> day03_storage_investigation

=== day03_storage_investigation ===
// [场景] 玩家进入储藏室；木箱仍单独封存，通行申报单和旧进货簿放在旁边。
// [表现] 正常2.5D视角；检查结果通过文字呈现，不制作单独的物品界面。
+ [(Inky 预览) 打开木箱，检查里面的深层生物材料。]
    你：『这些腺囊只能从活体上剖取吧，残留的气味还很新，应该是最近才收集到的。』
    -> day03_compare_records

=== day03_compare_records ===
 + [对照通行申报单和旧进货簿。]
    你：『订单确实是我以前留下的，没问题。“查货源”……我得看看还有没有别的线索。』

    你：『申报单末尾有一串通行编号。』
    -> day03_eve_explains_core

=== day03_eve_explains_core ===
// [表现] 主角左、伊芙右的近景对话。
伊芙：“这是封印留下的通行编号。要查它，得用封印室里的控制核心。”

你：“控制核心啊，希望米拉送来的界锚晶还有用。”

伊芙：“只能查记录，估计干不了别的事情，要控制封印肯定是不行的。”

* [带上界锚晶和通行编号，前往封印室。]
    -> day03_go_to_seal_chamber

=== day03_go_to_seal_chamber ===
// [Unity流程] 玩家进入储藏室下方的封印室，走到暗淡的控制核心前。
+ [(Inky 预览) 前往封印室。]
    -> day03_restore_core

=== day03_restore_core ===
+ [把界锚晶装入控制核心。]
    // [场景] 控制核心暂时亮起；主封印仍然关闭。
    -> day03_read_passage_record

=== day03_read_passage_record ===
+ { !passage_record_checked } [查询通行申报单上的通行编号。]
    ~ passage_record_checked = true
    // [表现] 查询结果使用普通文字呈现，不制作复杂日志界面。
    【通行目的：运输已订购货物。判定：符合条件，并无恶意企图。允许通过。】
    -> day03_read_passage_record

* [离开封印室，返回大厅。]
    -> day03_conditional_clues

=== day03_conditional_clues ===
// [表现] 主角头顶气泡。
你：“……先理一理思路。”

你：『这么大量、又这么新鲜的腺囊，只能从数量庞大的尸体上取下来。』

{ passage_record_checked || nox_source_question_count == 3:
    你：『带着非和平的目的却成功通过了封印！有什么手段，竟然能避开控制核心的检查？』
}

{ mira_relic_memory_count > 0:
    你：『米拉带来的黑色石片……它的魔力，控制核心也曾沾染过。难道控制核心也有同源的部件吗？还是说有同源的东西影响过控制核心。』
}

{ mira_relic_memory_count > 0 && (passage_record_checked || nox_source_question_count == 3):
    你：『嗯……我大概明白了，是这个同源的东西影响了控制核心，掩盖了非和平的通行目的，让那伙人能够进入深层，然后屠杀生物，拿到这些腺囊。』
}

-> day03_return_to_hall

=== day03_return_to_hall ===
// [Unity流程] 玩家从封印室返回大厅；伊芙在营业拉杆旁等待。
+ [(Inky 预览) 返回大厅。]
    -> day03_eve_conclusion

=== day03_eve_conclusion ===
// [表现] 主角左、伊芙右的近景对话。
伊芙：“查到什么了吗？”

{ mira_relic_memory_count > 0 && (passage_record_checked || nox_source_question_count == 3):
    你：“货不是旧库存，是从大量被猎杀的深层生物身上新取下来的。我推测，大概是有人用和黑色石片同源的东西掩盖了非和平的目的，然后穿过封印去下层猎杀了他们。”

    伊芙：“所以他们再把素材带回上层，伪装成旧库存，借着稀有资源的名头高价卖给别人。”

    你：“只是推断，还得找到那件同源的东西，或者找到他们。”
- else:
    { passage_record_checked || nox_source_question_count == 3:
        你：“货不是旧库存，是从大量被猎杀的深层生物身上新取下来的。有人带着非和平的目的，却还是通过了封印。”

        伊芙：“封印为什么会允许？”

        你：“还不清楚。我得继续查下去。”
    - else:
        { mira_relic_memory_count > 0:
            你：“货不是旧库存，是从大量被猎杀的深层生物身上新取下来的。还有那个黑色石片的魔力，曾经控制核心也沾染过。但我不知道，是这股魔力影响了控制核心，还是控制核心以前装过同样来源的部件。”
            你：“可是这都说不通，二者之间有什么联系吗？我觉着是我漏掉了一些线索。”

            伊芙：“那就先把石片和这箱货都收好。”
        - else:
            你：“货不是旧库存，是从大量被猎杀的深层生物身上新取下来的。其他的线索还不够。”

            伊芙：“至少我们知道，这箱货不能流进市场。”
        }
    }
}

伊芙：“那明天还开门吗？”

你：“开。酒馆开着，线索才会继续找上门。”

伊芙：“好。那今天就先到这里。”

// [Unity流程] 玩家拨动营业拉杆后，门口的牌子由机关自动切换，结束第三日并完成第一章。
+ [(Inky 预览) 拨动拉杆，结束第三日营业。]
    ~ chapter01_complete = true
    -> END

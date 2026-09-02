=== day02_open ===
// [场景] 第二日上午。伊芙已经完成开门前的准备，站在吧台旁的营业拉杆附近。
// [表现] 伊芙头顶气泡。
伊芙：“东西都准备好了。你准备好，我们就开门。”

// [Unity流程] 玩家拨动营业拉杆后，门口的牌子由机关自动切换，进入 day02_service_start。
+ [(Inky 预览) 拨动拉杆，开始第二日营业。]
    -> day02_service_start

=== day02_service_start ===
// [场景] 米拉是一名提夫林鉴定师，带着随身鉴定箱从公共入口进入酒馆。
// [表现] 米拉头顶气泡。
米拉：“看来传言是真的。这里重新开门了。”

// [表现] 伊芙头顶气泡。
伊芙：“空位很多，随便坐吧。”

// [Unity流程] 米拉作为普通客人入座。完成对她的服务后进入 day02_mira_settlement。
+ [(Inky 预览) 模拟完成对米拉的服务。]
    -> day02_mira_settlement

=== day02_mira_settlement ===
// [场景] 米拉用餐结束，带着鉴定箱留在座位等待结账；玩家走近并交互后进入对话。
// [表现] 主角左、米拉右的近景对话。
米拉：“老板，结账。”

* [替米拉结账。]
    米拉：“还有一件私事。你要是不忙，我想请你看样东西。”
    -> day02_mira_relic

=== day02_mira_relic ===
// [动作] 米拉把黑色石片放到桌上。
米拉：“你知道的，我原来跟着一支冒险队。这是那支队伍带回来的东西。”

米拉：“他们嫌我的鉴定结果卖不上价，最后把它当废料丢给了我。”

米拉：“那趟委托我没跟着去。委托记录只说，他们去了深层一处没有名字的小遗迹。”

-> day02_mira_conversation

=== day02_mira_conversation ===
+ { mira_team_question_count == 0 } [“你的意思是，你离开了那支队伍？”]
    ~ mira_team_question_count = mira_team_question_count + 1
    米拉：“嗯。他们需要我鉴定东西，却一直拿我的角和血统说事。我原以为把活做好就够了。”

    米拉：“后来他们要我给不值钱的东西开高价鉴定书。我不肯，他们就说，提夫林果然不值得信任。”

    米拉：“那就找别人吧。我可不愿拿自己的名字替假货担保。”
    -> day02_mira_conversation

+ { mira_team_question_count == 1 } [“能再说说你离开队伍的原因吗？”]
    ~ mira_team_question_count = mira_team_question_count + 1
    米拉：“他们一直介意我是提夫林，只是用得上我的时候不提。我拒绝伪造鉴定书以后，他们就把话都说出来了。我不想再留下。”
    -> day02_mira_conversation

+ { mira_team_question_count == 2 } [“不好意思，你当时为什么离开队伍来着？”]
    ~ mira_team_question_count = mira_team_question_count + 1
    米拉：“我已经说过两次了。他们嫌我的血统，却还想借我的名字给假货担保，想都别想！”
    -> day02_mira_conversation

+ { mira_relic_memory_count == 0 } [感受黑色石片上的魔力。]
    ~ mira_relic_memory_count = mira_relic_memory_count + 1
    你：『我好像在哪里见过类似的魔力。』

    // [回忆过场] 主角曾在封印前察觉过同源魔力；不展示遗物本体或使用者。
    你站在封印前。控制核心周围萦绕着一缕奇怪的魔力。

    你：『有一股奇怪的魔力，但封印状态没问题。』

    米拉：“你的脸色不太好。认得它？”
    -> day02_mira_conversation

+ { mira_relic_memory_count == 1 } [再感受一次石片上的魔力。]
    ~ mira_relic_memory_count = mira_relic_memory_count + 1
    你：『这种魔力……我确实在哪里遇到过。』

    // [回忆过场] 同一段封印异常的记忆再次闪过；此后不再重复触发。
    你站在封印前。控制核心周围萦绕着一缕奇怪的魔力。

    你：『有一股奇怪的魔力，但封印状态没问题。』

    米拉：“还是先放下吧。”
    -> day02_mira_conversation

* [“我会暂时保管它，等查清楚再还给你。”]
    ~ fragment_custody = "tavern"
    米拉：“好。至少在这里，不会有人急着把它卖掉。”
    -> day02_nox_notice

* [“先由你保管。别再让原来的队伍接触它。”]
    ~ fragment_custody = "mira"
    米拉：“明白。我会把它封回鉴定箱。”
    -> day02_nox_notice

=== day02_nox_notice ===
// [场景] 米拉离开酒馆。
// [场景] 伊芙从公共入口附近叫住主角；经营地下素材收购与转卖的商人诺克斯带着一只封闭木箱等在门内。
// [表现] 伊芙头顶气泡。
伊芙：“老板，诺克斯来了。他带着你以前订的一批货。”

// [Unity流程] 玩家靠近诺克斯后进入 day02_nox_arrives。
+ [(Inky 预览) 走到公共入口，与诺克斯交谈。]
    -> day02_nox_arrives

=== day02_nox_arrives ===
// [表现] 主角左、诺克斯右的近景对话。
诺克斯：“你们之前在我这里订过一批深层生物材料，订金也付了。”

诺克斯：“后来酒馆一直关着，货就压在我的仓库里。现在重新开门，我就来把这笔生意做完。”

【诺克斯递来一张通行申报单。】

* [查看通行申报单。]
    你：『申报时间在我醒来之前，通行目的为“货物交易”，看上去没什么问题。不过这申报单是交到哪里的？』

    诺克斯：“货是我从别人手里收的，再按你说的卖给酒馆，不过卖家用的假名。”
    -> day02_nox_conversation

=== day02_nox_conversation ===
+ { nox_source_question_count == 0 } [“这批货是从哪里来的？”]
    ~ nox_source_question_count = nox_source_question_count + 1
    诺克斯：“送进仓库的人说是深层旧库存。我没见过卖家，只核对了封条和预付款。”
    -> day02_nox_conversation

+ { nox_source_question_count == 1 } [“能不能再说一下，这批货是从哪里来的？”]
    ~ nox_source_question_count = nox_source_question_count + 1
    诺克斯：“哎，就是送货的人说的，这是深层生物材料的旧库存。我没见过卖家，只核对了封条和预付款。”
    -> day02_nox_conversation

+ { nox_source_question_count == 2 && !material_trace_examined } [“不好意思，这批货的来路是什么来着？”]
    ~ nox_source_question_count = nox_source_question_count + 1
    【诺克斯擦了擦汗，目光从木箱上移开。】
    诺克斯：“我已经说过两次了，这是深层生物材料。没办法，也只能是旧库存嘛。”
    
    诺克斯：“毕竟自从大封印之后，再想进入深层，都要申报审核。没人能以非和平目的通行，自然也不存在新的生物材料了。你们应该最了解这个才对。”
    -> memory_nox_order

+ { nox_source_question_count >= 1 && !material_trace_examined } [检查木箱封口处的魔力残留。]
    ~ material_trace_examined = true
    ~ nox_trace_pressure = nox_source_question_count
    【你检查了木箱的封蜡、箱缝和附着其上的魔力痕迹。】
    你：“这不是长期存放留下的痕迹，它不久前才经过封印运到你仓库。”

    { nox_trace_pressure == 2:
        【诺克斯擦了擦汗，目光从木箱上移开。】
        诺克斯：“路线干不干净，不在我的收货条件里。单据能对上，我就把货送到该去的地方。”
    - else:
        ~ nox_became_flustered = true
        诺克斯：“这个嘛……路线干不干净，不在我的收货条件里，对吧？你看……这单据能对上，我就送货。”
    }
    -> day02_nox_conversation

+ { material_trace_examined && !nox_trace_accusation_asked } [“既然是旧库存，为什么会有这么新的封印残留？”]
    ~ nox_trace_accusation_asked = true
    ~ nox_became_flustered = true
        诺克斯：“嗯……我也看不出货算不算旧。就是……买卖都已经谈好了，我也没必要纠这点细节。”
    -> day02_nox_conversation

+ { nox_trace_accusation_asked && !nox_final_confirmation_asked } [“你确定自己没有看出别的问题？”]
    ~ nox_final_confirmation_asked = true
        诺克斯：“卖家……他出手很阔，催得也急。不过做这一行，催得急的人多了。”
    -> day02_nox_conversation

* [“没啥问题，那货我就收下了，通行申报单也放我这。”]
    { nox_became_flustered:
        诺克斯：“行。凭据、申报单和箱子都给你。仓储和送货的费用照单结清，我们就两不相欠。”
    - else:
        诺克斯：“当然。凭据和申报单都在这儿，箱子也交给你。仓储和送货的费用按单子结就行。”
    }

    // [动作] 伊芙把木箱移入储藏室单独封存。
    -> day02_nox_after_transaction 
    
=== memory_nox_order ===
// [回忆过场] 主角失踪前向诺克斯下单；只呈现二人的局部轮廓和桌上的订单。
你：“最近有一批流进市场的深层生物素材，你碰上了就替我留一箱。”

诺克斯：“行，只要你订金给够。”

你：“记得要求他们通行申报单，然后登记货物来源，等货到了就一起拿过来。”
-> day02_nox_conversation


=== day02_nox_after_transaction ===
{ nox_became_flustered:
    * [结清费用。]
        诺克斯：“嗯……都交清了，没问题。那我还有别的生意，先走一步。”
        // [场景] 诺克斯离开酒馆。
        -> day02_after_nox
- else:
    诺克斯：“既然货已经送到了，再给我来杯酒吧。”
    -> day02_nox_service
}

=== day02_nox_service ===
// [场景] 诺克斯作为客人入座，酒馆继续营业。
// [Unity流程] 完成对诺克斯的服务后进入 day02_nox_settlement。
+ [(Inky 预览) 模拟完成对诺克斯的服务。]
    -> day02_nox_settlement

=== day02_nox_settlement ===
// [场景] 诺克斯喝完酒后留在座位等待结账；玩家走近并交互后进入对话。
// [表现] 主角左、诺克斯右的近景对话。
诺克斯：“酒钱也一起结了吧。”

* [替诺克斯结账。]
    诺克斯：“酒还是以前的味道。下次有生意，再来找我。”
    // [场景] 诺克斯离开酒馆。
    -> day02_after_nox


=== day02_after_nox ===
// [场景] 诺克斯离开后，伊芙拿出主角失踪前留下的旧进货簿。
// [表现] 主角左、伊芙右的近景对话。
伊芙：“申报单上的日期，和这本进货簿里的一笔记录对得上。”

伊芙：“但那笔记录被你自己划掉了。”

【伊芙把旧进货簿交给你。】

* [查看旧进货簿。]
    你：『被划掉的记录旁边，写着“查货源”三个字。』
    -> day02_store_ledger

=== day02_store_ledger ===
* [把旧进货簿收好。]
    你：“先收好，别让其他人碰这些记录。”
    伊芙：“好。我会和今天新的分开放。”
    -> day02_close

=== day02_close ===
// [场景] 伊芙处理完吧台和账目，回到吧台旁的营业拉杆附近。
// [表现] 伊芙头顶气泡。
伊芙：“今天差不多了，就到这里吧。”

// [Unity流程] 如果玩家长时间没有拨动拉杆，用下面的催促气泡替换原气泡。
// 伊芙：“一会儿我还要把旧进货簿锁起来。今天差不多了，就到这里吧。”
// [Unity流程] 玩家拨动拉杆后，门口的牌子由机关自动切换，结束第二日并进入 day03_open。
+ [(Inky 预览) 拨动拉杆，结束第二日营业。]
    -> day03_open

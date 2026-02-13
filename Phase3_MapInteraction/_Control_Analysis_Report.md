# 小球运动控制分析报告

## 一、当前控制链路全景

```
键盘A/D → Input.GetAxis(sensitivity=50,gravity=50,snap=true)→ rawInput *50 = inputVelocity
         → _posX += inputVelocity * dt
         → 管道壁反弹检测(±3.5)
         → transform.position直接赋值
```

## 二、发现的核心问题

### 问题1：发声器贴墙放置，但碰撞检测用AABB扩展球半径
- `CalcWallX` 把所有发声器贴在`TRACK_HALF_WIDTH(3.5) -0.075` = **3.425** 处
- `ManualCollisionCheck` 用 `bounds.Expand(playerRadius*2)` =扩展0.8
- 发声器scale=(0.15, 1.5, 5.0)，BoxCollider bounds X范围 = 3.35~3.5
- 扩展后X范围 = **2.55~4.3**
- 玩家只要X>2.55就会触发碰撞 → **碰撞区域太大，几乎占管道宽度的27%**
- 左右两侧碰撞区重叠区域：左侧到-2.55，右侧从2.55 → **中间只有5.1的安全区**
- 管道全宽7.0，安全区5.1 → 两侧各0.95的"死亡区"

### 问题2：碰撞触发DoBounce后冲量被立即清零
- DoBounce调用 `ApplyLateralImpulse(dir*8)` 设置 `_impulseVelocity`
- 但下一帧如果玩家仍在按键，`hasInput=true` → `_impulseVelocity=0`
- **发声器反弹完全无效**，玩家感觉碰到发声器没有任何反馈

### 问题3：管道壁反弹后立即被输入覆盖
- 撞墙设置 `_impulseVelocity =±15`
- 下一帧玩家按键 → 冲量清零 → 反弹无效
- 玩家持续按向墙壁方向 → 每帧撞墙→反弹→清零→再撞墙 → **卡在墙上抖动**

### 问题4：发声器Z长度=5.0，碰撞窗口过长
- 发声器scale.z=5.0，在SCROLL_SPEED=30下对应 5/30=0.167秒的碰撞窗口
- 玩家在这0.167秒内任何时刻进入X范围都会触发
- 但发声器视觉上是一面薄墙，玩家预期是"擦过"而非"长时间接触"

### 问题5：双重碰撞检测可能重复触发
- OnTriggerEnter + ManualCollisionCheck 每帧都检测
- 虽然isJudged防止重复，但ManualCollisionCheck在Update中每帧调用
- 如果DoBounce在同一帧被OnTriggerEnter和ManualCollision同时触发前执行，可能有竞态

## 三、优化建议

### A. 碰撞区域修正（最关键）
- ManualCollisionCheck 的 `Expand(playerRadius*2)` 过大
- 应改为 `Expand(playerRadius)` 或直接用球心到bounds的距离检测
- 或者：不扩展bounds，改用 `bounds.SqrDistance(playerPos) < r*r`

### B. 冲量系统重设计
- 反弹冲量不应被输入清零，应该是**叠加**关系
- 改为：`totalVelocity = inputVelocity + impulseVelocity`，冲量独立衰减
- 去掉 `if(hasInput) impulse=0` 的逻辑

### C. 管道壁处理改为硬夹紧+速度归零
- 撞墙不反弹，直接Clamp + 清零冲量
- 避免卡墙抖动

### D. 发声器碰撞改为点检测
- 不用AABB扩展，改为检测球心到发声器平面的距离
- 碰撞判定更精确，符合视觉预期

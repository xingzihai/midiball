# 阶段三：地图生成与交互 - 总规划

## 阶段目标
根据MIDI解析数据在管道两侧生成发声器（方块占位符），实现玩家与发声器的撞击判定和撞墙反弹逻辑。

## 职责边界
本阶段涉及：
- `Map/` 模块：MapGenerator（发声器生成 + 对象池）
- `Gameplay/` 模块：扩展 PlayerController（撞击判定 + 撞墙反弹触发事件）
- `_Core/` 模块：可能需要补充常量

**不涉及：** 自动球、召唤/惩罚状态机、视觉特效（仅用占位方块）

## 技术要点
1. **发声器位置**：由 NoteData.xPosition 决定X坐标，NoteData.timeInSeconds * SCROLL_SPEED 决定Z坐标
2. **对象池**：发声器数量可能很大，使用简单对象池避免运行时GC
3. **判定逻辑**：基于距离检测（HIT_RADIUS=0.5），非物理碰撞
4. **判定时机**：每帧检查当前songTime附近的音符，玩家X位置与音符X位置的距离
5. **撞墙反弹**：PlayerController已有边界反弹，本阶段补充Miss事件触发
6. **事件触发**：命中->EventBus.OnNoteHit，未命中->EventBus.OnNoteMiss

## 目录结构变更
```
Assets/Scripts/
├── Map/
│   └── MapGenerator.cs       # [新增] 发声器生成+对象池
├── Gameplay/
│   ├── PlayerController.cs   # [修改] 添加音符判定逻辑
│   └── NoteJudge.cs          # [新增] 判定系统（独立模块）
```

## 交付标准
1. Play后管道两侧出现方块，位置与MIDI音符对应
2. 玩家球体接近方块时触发Hit判定，方块高亮/消失
3. 玩家未命中时触发Miss事件
4. Console输出Hit/Miss日志
5. 方块随玩家前进逐渐出现在视野中

## 子阶段划分
- **3A**: MapGenerator — 发声器生成与对象池
- **3B**: NoteJudge — 判定系统
- **3C**:集成验证 + 编辑器场景配置更新

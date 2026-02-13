# 阶段四：游戏循环与进阶机制 - 总规划

## 阶段目标
根据技术规格书 Phase 4 要求，实现：
1. **AutoBot 自动球系统** — 代表伴奏轨道的自动飞行球体
2. **GameStateManager 状态机** — 召唤(Summon)与惩罚(Penalty)逻辑
3. **Curved World 视觉弯曲**（本阶段仅做Shader占位，完整美术留Phase5）

## 职责边界
本阶段涉及：
- `Gameplay/AutoBotController.cs` [新增] — 自动球插值运动逻辑
- `Gameplay/GameStateManager.cs` [新增] — 召唤/惩罚状态机 + 分数管理
- `Visuals/WorldCurve.cs` [新增] — 弯曲世界Shader控制器（占位）
- `_Core/EventBus.cs` [可能微调] — 补充自动球相关事件
- `Audio/IAudioConductor.cs` [微调] — 接口暴露MuteTrack/UnmuteTrack
- `Editor/Phase4SceneSetup.cs` [新增] — 编辑器一键配置

**不涉及：**粒子特效、UI界面、结算流程（Phase5）

## 技术要点

### 4A: AutoBot 自动球
- 每个AutoBot绑定一个TrackType（Drums/Bass/Chords，Melody由玩家控制）
- 读取SongData中对应轨道的NoteData数组
- 使用二分查找定位当前songTime对应的音符索引
- 在"当前音符位置"和"下一音符位置"之间Lerp插值，确保100%命中
- 视觉：小球体，颜色区分轨道，半透明
- 碰到发声器时触发对应轨道的音效（复用ToneGenerator）

### 4B: GameStateManager 状态机
- 监听EventBus.OnComboChanged：
  - combo >= COMBO_TO_UNLOCK(5)且存在未解锁轨道 → 触发召唤
  - 召唤：实例化AutoBot + EventBus.OnTrackUnlocked + AudioConductor.UnmuteTrack
- 监听EventBus.OnNoteMiss：
  - 连续miss >= MISS_TO_PENALTY(5) 且存在已解锁轨道 → 触发惩罚
  - 惩罚：销毁最高级AutoBot + EventBus.OnTrackLost + AudioConductor.MuteTrack
- 解锁顺序：Drums → Bass → Chords
- 惩罚顺序：Chords → Bass → Drums（逆序剥离）
- 分数计算：Hit +100*comboMultiplier，Miss不扣分

### 4C: IAudioConductor接口扩展
- 添加MuteTrack/UnmuteTrack到接口，使GameStateManager可通过ServiceLocator调用

### 4D: 集成验证 + 编辑器配置
- Phase4SceneSetup一键创建GameStateManager空物体
- 验证：连续Hit5次后自动球出现，连续Miss 5次后自动球消失

## 目录结构变更
```
Assets/Scripts/
├── Gameplay/
│   ├── AutoBotController.cs   # [新增] 自动球插值飞行
│   └── GameStateManager.cs    # [新增] 召唤/惩罚状态机
├── Visuals/
│   └── WorldCurve.cs          # [新增] 弯曲世界占位
├── Editor/
│   └── Phase4SceneSetup.cs    # [新增] 编辑器配置
```

## 交付标准
1. 连续Hit 5个音符后，自动球（小球体）出现并沿对应轨道自动飞行
2. 自动球100%命中对应轨道的发声器位置
3. 连续Miss 5次后，最后解锁的自动球被销毁
4. AudioConductor的MuteTrack/UnmuteTrack被正确调用
5. Console输出召唤/惩罚日志
6. 分数通过EventBus.OnScoreChanged广播

## 子阶段划分
- **4A**: IAudioConductor接口扩展 + AutoBotController
- **4B**: GameStateManager 状态机
- **4C**: WorldCurve占位 + Phase4SceneSetup + 集成验证

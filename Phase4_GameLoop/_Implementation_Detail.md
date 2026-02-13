# 阶段四：实现细节

## 4A: IAudioConductor接口扩展 + AutoBotController

### IAudioConductor.cs 修改
- 添加 `void MuteTrack(TrackType track)` 和 `void UnmuteTrack(TrackType track)` 到接口
- AudioConductor已有实现，只需接口声明补齐

### AutoBotController.cs 设计
- 挂载到运行时动态创建的球体上
- 构造时接收：TrackType + NoteData[] (对应轨道音符)
- 核心逻辑（Update每帧）：
  1. 获取当前songTime
  2. 二分查找找到当前时间对应的音符索引`currentIdx`
  3. 取`notes[currentIdx]` 和 `notes[currentIdx+1]` 的位置
  4. 计算插值因子 `t = (songTime - curNote.time) / (nextNote.time - curNote.time)`
  5. X坐标 = Lerp(curX, nextX, t)，Z坐标 = songTime * SCROLL_SPEED
  6. Y固定（与玩家同高或略偏移）
- 视觉：半透明彩色球体，轨道颜色映射：
  - Drums =橙色(1, 0.6, 0)
  - Bass = 蓝色(0.2, 0.5, 1)
  - Chords =紫色(0.8, 0.3, 1)
- 球体半径 = 0.25（比玩家小）

### 使用方式
GameStateManager调用 `AutoBotController.Create(TrackType, NoteData[], Transform parent)` 静态工厂方法

## 4B: GameStateManager 状态机

### 数据结构
```
_unlockOrder = { Drums, Bass, Chords }  // 解锁顺序
_unlockedTracks = List<TrackType>       // 已解锁列表
_autoBots = Dictionary<TrackType, AutoBotController>  // 活跃自动球
_consecutiveMisses = int                // 连续Miss计数
_score = int
```

### 事件订阅
- OnEnable: 订阅 OnComboChanged, OnNoteHit, OnNoteMiss
- OnDisable: 取消订阅

### 召唤逻辑 (OnComboChanged回调)
```
if combo >= COMBO_TO_UNLOCK &&有未解锁轨道:
    nextTrack = _unlockOrder中第一个未解锁的
    创建AutoBot(nextTrack)
    _unlockedTracks.Add(nextTrack)
    conductor.UnmuteTrack(nextTrack)
    EventBus.OnTrackUnlocked(nextTrack)重置combo（通过NoteJudge）
```

### 惩罚逻辑 (OnNoteMiss回调)
```
_consecutiveMisses++
if _consecutiveMisses >= MISS_TO_PENALTY && _unlockedTracks.Count > 0:
    lastTrack = _unlockedTracks中最后一个
    销毁AutoBot(lastTrack)
    _unlockedTracks.Remove(lastTrack)
    conductor.MuteTrack(lastTrack)
    EventBus.OnTrackLost(lastTrack)
    _consecutiveMisses = 0
```

### 分数逻辑 (OnNoteHit回调)
```
comboMultiplier = 1 + unlockedTracks.Count  // 解锁越多倍率越高
_score += 100 * comboMultiplier
EventBus.OnScoreChanged(_score)
_consecutiveMisses = 0// Hit重置连续Miss
```

## 4C: WorldCurve占位 + Phase4SceneSetup

### WorldCurve.cs
- 占位脚本，预留Curved World Shader参数控制接口
- 暴露 `curveStrength` 参数，Phase5接入实际Shader

### Phase4SceneSetup.cs
- 编辑器菜单：StarPipe/Setup Phase4 Scene
- 创建 GameStateManager 空物体
- 确保场景中已有 AudioConductor、MapGenerator、NoteJudge、PlayerController

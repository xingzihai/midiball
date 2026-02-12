# 阶段一：细节实现规划

## 1A: _Core 模块

### EventBus.cs
- 静态类，使用 C# 委托/Action 实现发布-订阅模式
- 事件定义（来自规格书）：
  - `OnNoteHit(NoteType type, Vector3 position)` — 击中音符
  - `OnNoteMiss(Vector3 position)` — 未击中音符
  - `OnTrackUnlocked(TrackType track)` — 召唤成功，解锁音轨
  - `OnTrackLost(TrackType track)` — 惩罚触发，静音音轨
  - `OnComboChanged(int combo)` — 连击数变化
  - `OnGameStateChanged(GameState state)` — 游戏状态切换
- 提供 Subscribe/Unsubscribe/Trigger三组方法
- 提供 Clear() 用于场景切换时清理

### GameConstants.cs
- 静态类，只读常量
- 关键常量：
  - `TRACK_HALF_WIDTH = 5.0f` — X轴范围[-5,5]
  - `SCROLL_SPEED` — Z轴滚动速度（待调参）
  - `HIT_RADIUS = 0.5f` — 判定半径
  - `COMBO_TO_UNLOCK = 5` — 召唤所需连续命中
  - `MISS_TO_PENALTY = 5` — 惩罚触发连续失误数
  - `FORWARD_SPEED` — 玩家前进速度（待调参）

### ServiceLocator.cs
- 静态类，字典存储 `Type -> object` 映射
- 方法：`Register<T>(T instance)`, `Get<T>()`, `Reset()`
- 用于运行时注入IAudioConductor 等接口

## 1B: 数据结构

### NoteData.cs（放在 Audio/ 下）
- `NoteData` 结构体(struct)：
  - `double timeInSeconds` — 音符触发时间（秒）
  - `int midiNote` — MIDI音高(0-127)
  - `float xPosition` — 映射后的X坐标[-5,5]
  - `float duration` — 持续时长（秒）
  - `TrackType track` — 所属轨道- `NoteType noteType` — 普通/特殊（召唤触发器）
- `TrackType` 枚举：Melody, Drums, Bass, Chords
- `NoteType` 枚举：Normal, Special
- `GameState` 枚举：Loading, Playing, Paused, GameOver

## 1C: MidiParser

### MidiParser.cs（放在 Audio/ 下）
- 使用 Melanchall.DryWetMidi 库
- 核心方法：`List<NoteData> Parse(string midiFilePath)`
- 解析流程：
  1. 读取 .mid 文件 -> MidiFile 对象
  2. 获取 TempoMap
  3. 遍历每个 Track 的 Notes
  4. 将MIDI tick 转换为秒（通过 TempoMap）
  5. 将 MIDI 音高映射到 X 坐标：`x = Mathf.Lerp(-5f, 5f, midiNote / 127f)`
  6. 根据 Track 索引分配 TrackType
  7. 返回按时间排序的 NoteData 列表
- 验证方式：编写 EditorWindow 或 MonoBehaviour 测试脚本，在控制台打印解析结果

## DryWetMidi 集成方式
- 从 NuGet 下载 Melanchall.DryWetMidi 包
- 提取 DLL 文件放入 `Assets/Plugins/DryWetMidi/`
- 无需 Assembly Definition，Unity 会自动引用 Plugins 下的 DLL

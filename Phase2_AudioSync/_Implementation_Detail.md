# 阶段二：细节实现规划

## 2A: IAudioConductor 接口+ AudioConductor

### IAudioConductor.cs（Audio/下）
- 接口定义，供其他模块通过 ServiceLocator 获取
- 属性：
  - `double SongTime` — 当前歌曲时间（秒）
  - `bool IsPlaying` — 是否正在播放
  - `SongData CurrentSongData` — 当前加载的歌曲数据
- 方法：
  - `void LoadSong(string midiFileName)` — 加载MIDI并准备音频
  - `void Play()` — 开始播放
  - `void Pause()` / `void Resume()` — 暂停/恢复
  - `void Stop()` — 停止

### AudioConductor.cs（Audio/下）
- MonoBehaviour，实现 IAudioConductor
- 核心时间机制：
  - `dspStartTime`：调用 Play() 时记录 `AudioSettings.dspTime`
  - `SongTime = AudioSettings.dspTime - dspStartTime`（暂停时冻结）
- 分轨播放（StemsManager职责暂内联）：
  - Inspector暴露 4 个 AudioClip 槽位（Melody/Drums/Bass/Chords）
  - Awake 时创建 4 个子AudioSource
  - Play() 时使用 `PlayScheduled(dspStartTime)` 确保同步
  - 提供 `MuteTrack(TrackType)` / `UnmuteTrack(TrackType)`
- Awake 中注册到 ServiceLocator：`ServiceLocator.Register<IAudioConductor>(this)`
- 静默模式：无音频文件时仍以 dspTime 驱动SongTime
- MIDI 加载：调用 MidiParser.Parse()缓存 SongData

## 2B: PlayerController

### PlayerController.cs（Gameplay/下）
- MonoBehaviour，挂载到玩家球体
- 依赖：通过 ServiceLocator.Get<IAudioConductor>() 获取 songTime
- Z轴移动（时间驱动，非物理）：
  - `transform.position.z = (float)conductor.SongTime * GameConstants.SCROLL_SPEED`
  - 不使用 Translate，直接设置 Z 坐标，确保零漂移
- X轴移动（输入驱动，运动学模拟）：
  - `velocityX` 内部状态变量
  - 输入：`Input.GetAxis("Horizontal")` ->加速度`GameConstants.LATERAL_ACCEL`
  - 每帧：`velocityX += input * LATERAL_ACCEL * Time.deltaTime`
  - 位移：`x += velocityX * Time.deltaTime`
  - 阻尼：无输入时 velocityX 自然衰减（乘以 damping 系数）
- 边界反弹：
  - 当 `|x| >= TRACK_HALF_WIDTH` 时：
    - `velocityX = -velocityX`（完全弹性反弹）
    - `x = Clamp` 到边界内
- Update 中组合Z 和 X，设置 `transform.position`

## 2C: 集成验证

### 场景配置
- 创建空GameObject "AudioConductor"，挂载 AudioConductor 脚本
- 创建 Sphere "Player"，挂载 PlayerController 脚本
- Main Camera 暂时手动跟随或固定位置观察
- Inspector 配置 MIDI 文件名（test.mid）
- 音频文件暂时可选（静默模式也能验证移动）

### 验证项
1. 按 Play 后 Player沿 Z 轴匀速前进
2. A/D 键控制左右移动，到达边界自动反弹
3. Console 输出 songTime 递增，无跳变
4. 暂停/恢复后 songTime 和位置一致

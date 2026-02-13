# 阶段二：音频同步与基础移动 - 总规划

## 阶段目标
实现音频时间驱动的游戏核心循环：AudioConductor 管理 dspTime 同步播放，PlayerController 实现基于 dspTime 的 Z 轴移动和基于输入的 X 轴反弹物理。

## 职责边界
本阶段涉及：
- `Audio/` 模块：AudioConductor（dspTime 时间基准 + 分轨播放管理）
- `Gameplay/` 模块：PlayerController（运动学物理 + 边界反弹）

**不涉及：** 地图生成、音符判定、自动球、视觉特效

## 技术要点
1. **唯一时间基准**：`AudioSettings.dspTime`，严禁使用 `Time.time`
2. **Z轴位置公式**：`zPos = (float)(songTime) * GameConstants.SCROLL_SPEED`
3. **运动学模拟**：不使用 Rigidbody，手动计算位移和反弹
4. **模块通信**：PlayerController 通过 ServiceLocator 获取 IAudioConductor 接口读取 songTime

## 目录结构变更
```
Assets/Scripts/
├── Audio/
│   ├── IAudioConductor.cs    # [新增] 音频控制器接口
│   ├── AudioConductor.cs     # [新增] dspTime同步 + 分轨管理
│   ├── MidiParser.cs         # [已有]
│   └── NoteData.cs           # [已有]
├── Gameplay/
│   └── PlayerController.cs   # [新增] 玩家运动学控制
```

## 交付标准
1. AudioConductor 能加载并同步播放多条音轨（WAV分轨）
2. songTime 基于 dspTime，精度稳定无漂移
3. PlayerController Z轴位置严格跟随 songTime
4. X轴输入响应流畅，边界反弹物理正确
5. 在无音频文件时也能以 dspTime 驱动前进（静默模式）

## 子阶段划分
- **2A**: IAudioConductor 接口 + AudioConductor 实现
- **2B**: PlayerController 运动学物理
- **2C**: 集成验证（挂载到场景，运行测试）

#阶段一：核心引擎与数据层- 总规划

## 阶段目标
搭建项目基础架构，完成核心数据层和事件通信系统，验证MIDI解析能力。

## 职责边界
本阶段仅涉及：
- `_Core/`模块：事件总线、全局常量、服务定位器
- `Audio/` 模块（仅数据层）：MIDI解析器、音符数据结构
- Unity工程基础配置

**不涉及：** 音频播放、玩家控制、地图生成、视觉效果

## 技术选型
- Unity 2022LTS / Unity 6
- C# (.NET Standard 2.1)
- Melanchall.DryWetMidi（NuGet -> Unity手动导入DLL）

## 目录结构
```
Assets/Scripts/
├── _Core/
│   ├── EventBus.cs          # 静态事件总线
│   ├── GameConstants.cs     # 全局常量
│   └── ServiceLocator.cs    # 服务定位器
├── Audio/
│   ├── NoteData.cs          # 音符数据结构体
│   └── MidiParser.cs        # MIDI文件解析器
├── Map/     # (本阶段空)
├── Gameplay/                # (本阶段空)
└── Visuals/                 # (本阶段空)
```

## 模块间通信规则
- 所有模块仅可读取 `_Core/` 中的公共接口
- 模块间严禁直接引用，必须通过 `EventBus` 通信
- `ServiceLocator` 用于运行时获取接口实例

## 交付标准
1. EventBus 能正确注册/触发/注销事件
2. GameConstants 包含规格书中所有常量定义
3. MidiParser 能读取 .mid 文件并输出音符列表（控制台验证）
4. NoteData 结构体能完整表达音符信息（时间、音高、轨道、持续时长）

## 子阶段划分
- **1A**: _Core 模块（EventBus + GameConstants + ServiceLocator）
- **1B**: 数据结构（NoteData + TrackType枚举）
- **1C**: MidiParser 实现与验证

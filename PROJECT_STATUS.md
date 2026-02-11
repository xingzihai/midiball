# MidiMarble项目技术状态

## 当前进度：阶段一✅ | 阶段二✅ | 阶段2.5✅

## 项目结构
```
midiball/
├── encoder/                # Python后端 - MIDI→.mdbl转换
│   ├── main.py               # CLI入口 (50行)
│   ├── midi_parser.py        # MIDI解析+多乐器映射 (107行)
│   ├── path_planner.py       # 路径规划：交替横竖+回溯+动态速度+分光镜 (190行)
│   ├── chord_grouper.py      # 和弦分组+汇光镜预计算 (55行) [2.5新建]
│   ├── collision_detector.py # 空间网格哈希碰撞检测 (80行)
│   ├── mdbl_writer.py        # .mdbl输出+type/chordNotes (58行)
│   ├── generate_test_midi.py # 测试MIDI生成器
│   └── requirements.txt      # mido, numpy
├── player/                   # TypeScript前端 - .mdbl播放器
│   ├── src/
│   │   ├── main.ts           # 应用入口 (85行)
│   │   ├── renderer.ts       # Pixi.js渲染+SPLITTER/MERGER标记 (185行)
│   │   ├── camera.ts         # 摄像机跟随+动态zoom (52行)
│   │   ├── audio-engine.ts   # Tone.js音频+和弦播放 (70行)
│   │   ├── mdbl-loader.ts    # 数据加载 (30行)
│   │   └── types.ts          # 类型定义+type/chordNotes (55行)
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   └── vite.config.ts
├── Phase1_MVP/               # 阶段一文档
├── Phase2_SmartAlgo/         # 阶段二文档
├── Phase2.5_SplitterMerger/  # 阶段2.5文档 [新建]
│   ├── _Phase_Master.md
│   └── _Implementation_Detail.md
└── 项目技术规格书（必读）.md
```

## 快速启动命令
```bash
# Encoder: MIDI转.mdbl（阶段2.5：交替横竖+分光镜/汇光镜）
cd encoder && python main.py ../testmidi/时间煮雨.mid -o ../player/public/shijianzhuyu.mdbl

# Player: 启动开发服务器
npm run dev --prefix player
```

## 阶段2.5新增功能

### Encoder
- **交替横竖正交法线**：偶数墙横放(法线0,±1)，奇数墙竖放(法线±1,0)
- **三级放置策略**：正交优先→随机方向重试(30次)→智能强制放置(28候选)
- **和弦分组**：chord_grouper.py，时间差<50ms归为和弦组
- **分光镜(SPLITTER)**：和弦组第一个音符，携带chordNotes数组
- **汇光镜(MERGER)**：和弦组后第一个满足间距的单音符
- **零碰撞**：978墙col=0，回溯14670次，耗时12.9s

### Player
- **和弦播放**：SPLITTER触发时同时播放所有chordNotes
- **视觉标记**：SPLITTER菱形边框(金色)，MERGER圆形边框(绿色)
- **光晕区分**：SPLITTER菱形光晕，MERGER圆形光晕

## 当前参数
| 模块 | 参数 | 值 |
|------|------|-----|
| Encoder | V_MIN/V_MAX | 150/600 px/s |
| Encoder | NPS_WINDOW | 2.0s |
| Encoder | MIN_WALL_DIST | 25px |
| Encoder | MIN_DISTANCE | 25px |
| Encoder | MAX_RETRIES | 30 |
| Encoder | BT_PER_WALL | 15 |
| Encoder | CHORD_THRESHOLD_MS | 50ms |
| Player | ZOOM_MIN/MAX | 0.5/1.6 |
| Player | DENSITY_WINDOW | 2000ms |
| Player | GLOW_FADE_MS | 600ms |

## 依赖版本
- Python: 3.11, mido 1.3.3, numpy 1.26.4
- Node: pixi.js 7.3.2, tone 14.7.77, vite 5.0.12, typescript 5.3.3

## 阶段三待实现
1. **和弦拓扑处理**：编码器端为每个子球计算独立路径（SPLITTER→各音符墙→MERGER汇合）
2. **多球并发渲染**：播放器端渲染多个子球同时运动
3. **摄像机包围盒缩放**：多球模式下自动缩小视野包含所有子球
4. **粒子特效增强**：撞击溅射粒子

# MidiMarble项目技术状态

## 当前进度：阶段一✅ 完成 | 阶段二 ✅ 完成

## 项目结构
```
midiball/
├── encoder/# Python后端 - MIDI→.mdbl转换
│   ├── main.py# CLI入口 (42行)
│   ├── midi_parser.py        # MIDI解析+多乐器映射 (107行)
│   ├── path_planner.py       # 路径规划：回溯+动态速度 (123行)
│   ├── collision_detector.py # 碰撞检测工具 (80行) [新建]
│   ├── mdbl_writer.py        # .mdbl输出+多乐器 (58行)
│   ├── generate_test_midi.py # 测试MIDI生成器
│   └── requirements.txt      # mido, numpy
├── player/                   # TypeScript前端 - .mdbl播放器
│   ├── src/
│   │   ├── main.ts           # 应用入口 (85行)
│   │   ├── renderer.ts       # Pixi.js渲染+密度缩放 (170行)
│   │   ├── camera.ts         # 摄像机跟随+动态zoom (52行)
│   │   ├── audio-engine.ts   # Tone.js音频 (65行)
│   │   ├── mdbl-loader.ts    # 数据加载 (30行)
│   │   └── types.ts          # 类型定义 (50行)
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   └── vite.config.ts
├── Phase1_MVP/               # 阶段一文档
├── Phase2_SmartAlgo/         # 阶段二文档
│   ├── _Phase_Master.md
│   ├── _Encoder_Detail.md
│   └── _Player_Detail.md
└── 项目技术规格书（必读）.md
```

## 快速启动命令
```bash
# Encoder: MIDI转.mdbl（阶段二：回溯+动态速度）
cd encoder && python main.py ../testmidi/时间煮雨.mid -o ../player/public/shijianzhuyu.mdbl

# Player: 启动开发服务器
npm run dev --prefix player
```

## 阶段二新增功能

### Encoder
- **回溯算法**：碰撞检测(射线-线段+最小间距)+回滚重试，全局上限500次
- **动态速度**：NPS滑动窗口→smoothstep映射→三点平均平滑，V_MIN=150~V_MAX=600px/s
- **多乐器支持**：MIDI通道→instrumentId映射，16色调色板
- **碰撞检测模块**：collision_detector.py（射线-线段相交、墙体间距、线段交叉）

### Player
- **动态缩放**：camera.ts增加zoom lerp，根据事件密度自动调整(0.4~1.8)
- **密度计算**：renderer.ts统计±2s窗口内事件数→zoom映射
- **多乐器颜色**：墙体激活后显示对应乐器颜色

## 当前参数
| 模块 | 参数 | 值 |
|------|------|-----|
| Encoder | V_MIN/V_MAX | 150/600 px/s |
| Encoder | NPS_WINDOW | 2.0s |
| Encoder | MIN_WALL_DIST | 20px |
| Encoder | MAX_RETRIES | 40 |
| Encoder | MAX_TOTAL_BT | 500 |
| Player | ZOOM_MIN/MAX | 0.4/1.8 |
| Player | ZOOM_LERP | 0.04|
| Player | DENSITY_WINDOW | 2000ms |

## 依赖版本
- Python: 3.11, mido 1.3.3, numpy 1.26.4
- Node: pixi.js 7.3.2, tone 14.7.77, vite 5.0.12, typescript 5.3.3

## 阶段三待实现
1. **和弦分裂**：Splitter/Merger几何计算
2. **多球并发渲染**：摄像机包围盒自动缩放
3. **粒子特效增强**：撞击溅射粒子

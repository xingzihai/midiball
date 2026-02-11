#阶段一：原型验证 (MVP) - "单球跑通"总规划

## 1. 阶段目标
实现最简单的单旋律MIDI解析，固定速度，无防碰撞。前端能读取坐标、渲染墙体、小球按轨迹运动、撞墙发声。

## 2. 模块职责划分

### 2.1 Encoder（Python后端）
- **输入**：标准 .mid 文件（单旋律）
- **输出**：.mdbl 文件（JSON格式）
- **职责**：
  - 解析MIDI文件，提取音符事件（note_on/note_off、时间、音高、力度）
  - 固定速度下计算小球飞行距离
  - 简单角度分配（无回溯，允许重叠）
  - 生成墙体坐标、旋转角度、时间戳
  - 输出符合.mdbl规范的JSON文件

### 2.2 Player（TypeScript + Pixi.js 前端）
- **输入**：.mdbl 文件
- **输出**：Canvas动画 + 音频播放
- **职责**：
  - 解析.mdbl JSON
  - Canvas渲染：黑色墙体、小球运动轨迹
  - 小球按时间轴线性插值运动
  - 撞墙瞬间触发音频（Tone.js）
  - 基础摄像机跟随（锁定小球）

## 3. 技术选型

### Encoder
- Python 3.10+
- mido：MIDI文件解析
- numpy：向量计算
- json：输出.mdbl

### Player
- TypeScript
- Pixi.js：2D渲染
- Tone.js：Web Audio调度
- Vite：构建工具

## 4. 目录结构

```
midiball/
├── encoder/
│   ├── main.py# 入口，CLI接口
│   ├── midi_parser.py       # MIDI解析模块
│   ├── path_planner.py      # 路径规划（MVP：简单角度分配）
│   ├── mdbl_writer.py       # .mdbl文件输出
│   └── requirements.txt
├── player/
│   ├── src/
│   │   ├── main.ts          # 入口
│   │   ├── mdbl-loader.ts   # .mdbl文件加载与解析
│   │   ├── renderer.ts      # Pixi.js渲染引擎
│   │   ├── camera.ts        # 摄像机系统
│   │   ├── audio-engine.ts  # Tone.js音频引擎
│   │   └── types.ts         # TypeScript类型定义
│   ├── index.html
│   ├── package.json
│   └── tsconfig.json
├── Phase1_MVP/
│   ├── _Phase_Master.md     # 本文件
│   ├── _Encoder_Detail.md   # Encoder细节规划
│   └── _Player_Detail.md    # Player细节规划
└── 项目技术规格书（必读）.md
```

## 5. 数据流

```
.mid文件 → [midi_parser] → 音符事件列表 → [path_planner] → 墙体几何数据 → [mdbl_writer] → .mdbl文件
.mdbl文件 → [mdbl-loader] → 时间轴数据 → [renderer + camera] → 视觉输出
                → [audio-engine] → 音频输出
```

## 6. MVP交付标准
-✅ 能加载一个简单的单旋律MIDI文件（如"小星星"）
- ✅ Encoder输出合法的.mdbl文件
- ✅ Player能渲染墙体和小球运动
- ✅ 撞墙时发出对应音高的声音
- ✅ 基础摄像机跟随小球

## 7. 阶段一完成记录

### 完成日期：2026-02-11

### 已实现功能
- Encoder：MIDI解析 →路径规划（固定速度、随机反射角）→ .mdbl输出
- Player：Pixi.js渲染（墙体+小球+拖尾）、Tone.js音频调度、摄像机跟随、播放/暂停/重播
- 测试通过：小星星(14音符) + 时间煮雨(1866音符)

### Bug修复记录
- 墙体/球体尺寸优化：WALL_LENGTH 60→30, BALL_RADIUS 8→5
- 速度计算修复：移除MIN_DISTANCE干扰，严格按时间计算距离
- 重播功能：添加needsRestart状态标记，完整重置流程

### 当前参数
- Encoder: BALL_SPEED=400px/s, WALL_LENGTH=30px
- Player: WALL_LENGTH=30, WALL_THICK=3, BALL_RADIUS=5, TRAIL_LENGTH=15

### 已知限制（阶段二解决）
- 无防碰撞（墙体可能重叠）
- 固定速度（无动态变速）
- 单乐器（所有音符同色）
- 无和弦分裂支持

### 移交给阶段二的关键信息
- Encoder入口：`python encoder/main.py input.mid -o output.mdbl`
- Player启动：`npm run dev --prefix player`
- 核心数据流：.mid → midi_parser → path_planner → mdbl_writer → .mdbl → Player
- 阶段二需修改的文件：path_planner.py（回溯算法+动态速度）、renderer.ts（摄像机缩放）

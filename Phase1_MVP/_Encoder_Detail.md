# Encoder 细节实现规划 (MVP阶段)

## 1. midi_parser.py - MIDI解析模块

### 职责
将.mid文件解析为结构化的音符事件列表。

### 输入/输出
- **输入**：.mid文件路径
- **输出**：`List[NoteEvent]`，按绝对时间排序

### 数据结构
```python
NoteEvent = {
    "time_ms": float,      # 绝对时间（毫秒）
    "note": int,           # MIDI音高(0-127)
    "velocity": int,       # 力度 (0-127)
    "channel": int,        # 通道号
    "duration_ms": float   # 音符持续时长
}
```

### 实现要点
- 使用 `mido` 库的 `MidiFile` 加载文件
- 遍历所有track，将delta time转换为绝对时间（毫秒）
- 配对note_on/note_off 事件，计算duration
- 按 time_ms 排序输出
- MVP阶段只处理第一个有音符的轨道（单旋律）

### 使用方式
```
python encoder/main.py input.mid -o output.mdbl
```

---

## 2. path_planner.py - 路径规划模块

### 职责
将音符事件列表转换为墙体几何数据（坐标、角度）。

### 核心参数（MVP固定值）
- `BALL_SPEED = 300` px/s（固定速度）
- `WALL_LENGTH = 60` px（墙体长度）
- `START_POS = (0, 0)`（起始位置）
- `START_DIR = (1, 0)`（初始方向：向右）

### 算法流程
1. 从起始位置和方向开始
2. 对每个音符事件：
   a. 计算与上一个音符的时间差 `dt`
   b. 计算飞行距离 `d = BALL_SPEED * dt / 1000`
   c.沿当前方向前进距离 `d`，得到墙体中心坐标
   d. 随机生成墙体法线角度（限制范围避免回弹过于极端）
   e. 根据入射方向和墙体法线，计算反射方向作为新的运动方向
   f. 记录墙体数据（坐标、旋转角、音符信息、时间戳）
3. 输出墙体列表

### 反射计算
- 入射向量 `v_in`，墙体法线 `n`
- 反射公式：`v_out = v_in - 2 * dot(v_in, n) * n`
- MVP阶段墙体角度采用简单策略：在入射方向基础上偏转30°~150°范围内随机取值

### 输出数据结构
```python
WallData = {
    "id": int,
    "time": float,         # 撞击时间(ms)
    "pos": {"x": float, "y": float},
    "rotation": float,     # 墙体旋转角(度)
    "note": int,
    "velocity": int,
    "instrumentId": int
}
```

---

## 3. mdbl_writer.py - .mdbl文件输出模块

### 职责
将墙体数据和元信息组装为.mdbl JSON文件。

### 输出格式
严格遵循技术规格书第3节定义的JSON结构：
- `meta`：标题、BPM、总时长、地图边界
- `assets`：乐器列表（MVP只有一个默认乐器）
- `timeline`：墙体事件列表（按时间排序）
- 新增 `ballPath`：小球关键帧列表，供前端插值

### ballPath 结构
```json
{
  "ballPath": [
    {"time": 0, "x": 0, "y": 0},
    {"time": 1000, "x": 300, "y": 0},
    ...
  ]
}
```
前端通过相邻关键帧线性插值即可得到任意时刻的小球位置。

### 地图边界计算
遍历所有墙体坐标，取min/max并加padding。

---

## 4. main.py - CLI入口

### 职责
解析命令行参数，串联三个模块。

### 使用方式
```bash
python encoder/main.py input.mid -o output.mdbl
```

### 流程
1. argparse解析参数（输入文件、输出路径）
2. 调用 midi_parser 解析MIDI
3. 调用 path_planner 生成几何数据
4. 调用 mdbl_writer 输出文件
5. 打印统计信息（音符数、地图尺寸等）

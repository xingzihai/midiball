# Encoder 子球路径规划 - 实现细节

## 概述
新增 `encoder/child_path_planner.py` 模块，负责在SPLITTER→MERGER区间为每个和弦子音符规划独立路径。

## 核心算法: 扇形展开-飞行-收束汇合

### 1. 输入参数
- `splitter_pos`: SPLITTER墙体位置 (numpy array)
- `splitter_dir`: 主球到达SPLITTER时的飞行方向
- `chord_notes`: 和弦音符列表 (含note, velocity, instrumentId)
- `merger_pos`: MERGER墙体位置
- `merger_time`: MERGER时间戳(ms)
- `splitter_time`: SPLITTER时间戳(ms)
- `grid`: 主SpatialGrid引用（子墙体需加入防碰撞）

### 2. 扇形角度分配
```
N = len(chord_notes)
总扇形角 = min(π/2, N * π/8)  # 最大90度扇形
base_angle = atan2(splitter_dir.y, splitter_dir.x)
angles[i] = base_angle + 总扇形角 * (i/(N-1) - 0.5)  # 均匀分布，N=1时直行
```

### 3. 子球路径生成流程（每个子球独立）
```
for each child_note, angle:1. 从splitter_pos出发，沿angle方向飞行
  2. 在飞行距离处放置一面子墙体（该音符对应的墙）
  3. 子墙体碰撞检测：与主grid中已有墙体+其他子墙体检测
  4. 反射后计算新方向
  5. 从子墙体出发，计算到merger_pos的方向，放置最终引导墙
  6. 生成关键帧序列: [splitter_pos →子墙体pos → merger_pos]
```

### 4. 时间分配
```
total_dt = merger_time - splitter_time
子墙体撞击时间 = splitter_time + total_dt * 0.5  # 中间时刻撞墙
各子球速度 = 路径总长度 / total_dt  # 保证同时到达merger
```

### 5. 汇合约束
- 所有子球必须在merger_time到达merger_pos
- 最后一段路径（子墙体→merger）的方向由几何关系直接确定
- 如果子墙体放置失败，使用强制放置（选择最远离已有墙的位置）

### 6. 子墙体ID编号
- 子墙体ID从`base_id = splitter_wall_id * 100 + 1` 开始
- 避免与主墙体ID冲突

## 文件修改清单

### child_path_planner.py (新建, ~200行)
- `plan_child_paths(splitter_pos, splitter_dir, chord_notes, merger_pos, splitter_time, merger_time, grid)` →返回 childPaths 列表
- 内部函数: `_plan_single_child()`, `_find_child_wall_pos()`, `_calc_merge_direction()`

### path_planner.py (修改)
- `_plan_with_groups()` 中SPLITTER处理逻辑:
  - 找到对应的MERGER组索引和位置
  - 调用 `plan_child_paths()` 获取子路径
  - 将子墙体加入主grid
  - 将childPaths附加到SPLITTER墙体数据中

### mdbl_writer.py (修改)
- `_calc_bounds()` 需遍历childPaths中的坐标
- childPaths已在wall字典中，无需额外处理（JSON序列化自动包含）

### main.py (修改)
- 统计并打印子墙体总数

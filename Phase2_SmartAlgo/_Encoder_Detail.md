# 阶段二 Encoder 细节规划

## 1. collision_detector.py（新建，约80行）

### 1.1 核心函数
- `ray_intersects_segment(ray_origin, ray_dir, ray_len, seg_start, seg_end)` → bool
  - 射线与线段的2D相交检测，使用参数化方程求解
  - 返回是否在有效范围内相交
- `check_path_collision(origin, direction, distance, existing_walls, wall_length)` → bool
  - 遍历所有已有墙体，检查新路径射线是否与任何墙体线段相交
  - 同时检查新墙体位置是否与已有墙体过近（最小间距检查）
- `walls_too_close(pos1, pos2, min_dist)` → bool
  - 两墙中心距离小于min_dist则判定过近

### 1.2 使用方式
```python
from collision_detector import check_path_collision
# 在path_planner中每次生成新墙前调用
if not check_path_collision(pos, direction, distance, existing_walls, WALL_LENGTH):
    accept()
```

## 2. path_planner.py（重写，约250行）

### 2.1 新增常量
```
V_MIN = 150.0       # 最低速度 px/s（高密度区）
V_MAX = 600.0       # 最高速度 px/s（低密度区）
NPS_WINDOW = 2.0    # NPS计算窗口(秒)
NPS_MAX = 8.0       # NPS饱和值
MAX_RETRIES = 50    # 单墙最大重试次数
BACKTRACK_DEPTH = 3# 回溯深度
MIN_WALL_DIST = 25.0 # 墙体最小间距(px)
```

### 2.2 动态速度模块
- `_calc_nps(notes, current_index, window_sec)` → float
  - 以当前音符为中心，统计前后window_sec/2范围内的音符数- 返回 NPS = count / window_sec
- `_nps_to_speed(nps)` → float
  - smoothstep映射：`t = clamp(nps/NPS_MAX, 0, 1)`
  - `speed = V_MAX - (V_MAX - V_MIN) * (3t² - 2t³)`
- `_build_speed_curve(notes)` → List[float]
  - 为每个音符预计算速度值
  - 使用三点滑动平均平滑速度曲线

### 2.3 回溯算法
- `plan_paths(notes)` 主函数重写逻辑：
  1. 预计算速度曲线
  2. 维护状态栈 `stack = [(pos, direction, wall, ball_kf), ...]`
  3. 对每个音符：
     a. 用动态速度计算距离
     b. 尝试生成墙体角度（最多MAX_RETRIES次）
     c. 每次尝试调用collision_detector检查
     d. 若全部重试失败 → 回溯：弹出栈顶BACKTRACK_DEPTH个节点，重新生成
     e. 若回溯也失败（极端情况）→ 强制放置并打印警告
  4. 返回 (walls, ball_path)

### 2.4 辅助函数（保留并优化）
- `_generate_wall_normal()` — 不变
- `_reflect()` — 不变

## 3. midi_parser.py（小改，增加约30行）

### 3.1 新增函数
- `_build_instrument_map(notes)` → List[Dict]
  - 扫描所有音符的channel字段
  - 为每个唯一channel分配：id, name(GM标准名), color(预定义调色板)
-预定义调色板（16色，对应MIDI 16通道）：
  ```python
  CHANNEL_COLORS = ['#4FC3F7','#FF7043','#66BB6A','#AB47BC','#FFA726','#EC407A','#26C6DA','#D4E157',
                    '#8D6E63','#78909C','#5C6BC0','#29B6F6',
                    '#EF5350','#9CCC65','#FFCA28','#BDBDBD']
  ```

### 3.2 修改点
- `parse_midi()` 返回值新增第三项instruments：`return notes, meta, instruments`
- 每个note增加 `instrumentId` 字段（channel在instruments列表中的索引）

## 4. mdbl_writer.py（小改，增加约5行）

### 4.1 修改点
- `write_mdbl()` 新增参数 `instruments: List[Dict]`
- `assets.instruments` 使用传入的instruments而非硬编码

## 5. main.py（小改，增加约3行）

### 5.1 修改点
- 接收 `parse_midi()` 返回的instruments
- 传递给 `write_mdbl()`
- 打印乐器数量信息

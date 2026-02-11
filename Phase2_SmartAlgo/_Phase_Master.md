#阶段二：智能算法介入- "避障与变速" 总规划

## 1. 阶段目标
引入回溯算法解决路径重叠问题；引入动态速度计算使小球运动有快慢变化；增加摄像机动态缩放；支持多乐器颜色区分。

## 2. 交付物
可以生成复杂、不重叠、速度有快慢变化的地图，摄像机智能跟随且有缩放效果，不同乐器通道用不同颜色渲染。

## 3. 模块修改清单

### 3.1 Encoder 修改（Python后端）

#### 3.1.1 collision_detector.py（新建）
- 职责：射线-线段碰撞检测工具函数
- 核心函数：`ray_intersects_wall(origin, direction, distance, wall)` → bool
- 用于回溯算法中判断新路径是否与已有墙体相交

#### 3.1.2 path_planner.py（重写）
- 引入回溯算法：生成墙体角度时进行碰撞检测，碰撞则回滚N-1节点重试
- 引入动态速度：
  - 计算滑动窗口内的NPS（Notes Per Second）
  - 映射函数：`v(nps) = V_MAX - (V_MAX - V_MIN) * sigmoid(nps)`
  - 贝塞尔插值平滑速度变化曲线
- 最大回溯深度限制（防止死循环）
- 参数：MAX_RETRIES=50, BACKTRACK_DEPTH=3, V_MIN=150, V_MAX=600

#### 3.1.3 midi_parser.py（小改）
- 保留channel字段（已有）
- 新增：根据channel生成instrumentId映射
- 输出meta中增加instruments列表（通道→乐器名+颜色）

#### 3.1.4 mdbl_writer.py（小改）
- assets.instruments 从midi_parser的通道映射中读取，不再硬编码单乐器

#### 3.1.5 main.py（小改）
- 传递instruments信息到mdbl_writer

### 3.2 Player 修改（TypeScript前端）

#### 3.2.1 camera.ts（增强）
- 新增动态缩放（zoom）功能
- 根据小球速度/密度区域自动调整scale
- 平滑缩放过渡（lerp）
- 新增zoom参数：ZOOM_MIN=0.3, ZOOM_MAX=2.0, ZOOM_LERP=0.05

#### 3.2.2 renderer.ts（小改）
- 墙体颜色根据instrumentId从assets读取（已有逻辑，确认多乐器兼容）
- 将zoom信息传递给camera

#### 3.2.3 types.ts（无需修改）
- 现有类型定义已兼容多乐器和zoom

## 4. 目录结构变更
```
encoder/
├── main.py                # 小改：传递instruments
├── midi_parser.py         # 小改：多乐器映射
├── path_planner.py        # 重写：回溯+动态速度
├── collision_detector.py  # 新建：碰撞检测工具
├── mdbl_writer.py         # 小改：多乐器assets
└── requirements.txt       # 不变

player/src/
├── camera.ts              # 增强：动态缩放
├── renderer.ts            # 小改：zoom集成
└── (其余文件不变)
```

## 5. 实施顺序
1. 新建 collision_detector.py（碰撞检测基础设施）
2. 重写 path_planner.py（回溯算法+动态速度，核心难点）
3. 修改 midi_parser.py（多乐器通道映射）
4. 修改 mdbl_writer.py + main.py（多乐器输出）
5. 增强 camera.ts（动态缩放）
6. 修改 renderer.ts（zoom集成）
7. 测试：用时间煮雨.mid验证无碰撞+变速+多色

## 6. 关键算法说明

### 回溯算法伪代码
```
for each note[i]:
    for retry in range(MAX_RETRIES):
        angle = random_angle()
        new_wall = compute_wall(pos, direction, angle, distance)
        if not collides_with_existing(new_wall, walls[:i]):
            accept(new_wall)
            breakelse:
        backtrack(depth=BACKTRACK_DEPTH)  # 回滚最近N个墙，重新生成
```

### 动态速度映射
```
NPS = count_notes_in_window(t, window=2s) / window
speed = V_MAX - (V_MAX - V_MIN) * smoothstep(NPS / NPS_MAX)
```
使用贝塞尔曲线在相邻速度值之间插值，避免突变。

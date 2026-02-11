# 阶段2.5 细节实现规划 ✅ 已完成

## 1. chord_grouper.py（新建）✅

### 职责
和弦分组预处理 + 汇光镜目标预计算。

### 实际接口
- `group_notes(notes, threshold_ms=50) -> list[dict]`
  - 输出：`{ notes, is_chord, representative, time_ms }`
- `compute_merger_targets(groups, speeds, min_dist) -> set[int]`
  - 从和弦组后向后扫描，找第一个间距≥min_dist的单音符组索引

### 分组规则
- 遍历notes，时间差<threshold归入同组，组内>1个音符则is_chord=True

## 2. path_planner.py（重构）✅

### 参数变更（实际值）
| 参数 | 原值 | 新值 |
|------|------|------|
| MIN_WALL_DIST | 15px | 25px |
| MIN_DISTANCE | 12px | 25px |
| MAX_RETRIES | 40 | 30 |
| BT_PER_WALL | — | 15 |
| CHORD_THRESHOLD_MS | — | 50ms |

### 核心重构：交替横竖正交法线
- `_get_ortho_normals(d, idx)`：偶数idx优先[0,±1]横墙，奇数idx优先[±1,0]竖墙
- 按与飞行方向点积排序，优先选反射效果最好的法线

### 三级放置策略
1.正交优先：沿当前方向飞行，尝试4个正交法线
2. 随机方向重试（30次）：偏转±60°方向+正交法线
3. 智能强制放置：28种候选（4距离×7角度），选最远离已有墙的位置

### plan_paths流程
```
notes → group_notes() → groups
groups → _build_group_speed_curve() → speeds
groups + speeds → compute_merger_targets() → merger_targets
遍历groups: is_chord→SPLITTER, in merger_targets→MERGER, else→WALL
```

## 3. mdbl_writer.py ✅
- timeline事件已包含type和chordNotes字段，JSON序列化自动处理

## 4. main.py ✅
- 打印和弦组统计、SPLITTER/MERGER数量

## 5. types.ts ✅
```typescript
type?: 'WALL' | 'SPLITTER' | 'MERGER'
chordNotes?: { note: number; velocity: number; instrumentId: number }[]
```

## 6. audio-engine.ts ✅
- `_playNote`：SPLITTER时遍历chordNotes同时triggerAttackRelease

## 7. renderer.ts ✅
- SPLITTER：菱形边框(金色0xFFD700) + 菱形光晕
- MERGER：圆形边框(绿色0x00FFAA) + 圆形光晕
- 激活动画：白色闪烁→乐器颜色+光晕淡出（600ms）

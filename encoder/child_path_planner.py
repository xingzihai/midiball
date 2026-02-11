"""子球路径规划器- SPLITTER处为每个和弦音符生成独立路径到MERGER汇合"""
import numpy as np
from collision_detector import check_wall_segment_collision_grid, check_path_collision_grid

WALL_LENGTH = 30.0
MIN_WALL_DIST = 25.0
MIN_CHILD_DIST = 15.0  # 子墙体间最小间距（紧凑布局）
CHILD_TRAIL_RATIO = 0.08  # 子墙体撞击时间比例（尽快发声，接近同时）


def plan_child_paths(splitter_pos, splitter_dir, chord_notes, merger_pos,
                     splitter_time, merger_time, splitter_id, grid):
    """为SPLITTER处的和弦音符规划子球路径
    Args:
        splitter_pos: SPLITTER位置 np.array([x,y])
        splitter_dir: 主球到达SPLITTER时的飞行方向 np.array([dx,dy])
        chord_notes: 和弦附加音符列表 [{note, velocity, instrumentId}, ...]
        merger_pos: MERGER位置 np.array([x,y])
        splitter_time: SPLITTER时间(ms)
        merger_time: MERGER时间(ms)
        splitter_id: SPLITTER墙体ID(用于子墙体编号)
        grid: 主SpatialGrid引用
    Returns:
        list of childPath dicts, 每个包含 {note, velocity, instrumentId, walls, keyframes, mergerTime}
    """
    n = len(chord_notes)
    if n == 0 or merger_time <= splitter_time:
        return []
    total_dt = merger_time - splitter_time
    # 扇形角度分配：均匀分布在base_angle两侧
    base_angle = np.arctan2(splitter_dir[1], splitter_dir[0])
    fan_half = min(np.pi / 4, n * np.pi / 16)  # 最大45度半扇形
    angles = _distribute_angles(base_angle, fan_half, n)
    child_paths = []
    for i, cn in enumerate(chord_notes):
        cp = _plan_single_child(
            splitter_pos, angles[i], cn, merger_pos,
            splitter_time, total_dt, splitter_id, i, grid)
        child_paths.append(cp)
    return child_paths


def _distribute_angles(base, half_fan, n):
    """均匀分配扇形角度"""
    if n == 1:
        return [base]
    return [base + half_fan * (2 * i / (n - 1) - 1) for i in range(n)]


def _plan_single_child(sp_pos, angle, note_info, mg_pos,sp_time, total_dt, sp_id, child_idx, grid):
    """规划单个子球路径: SPLITTER →子墙体 → MERGER
    核心思路: 子球沿angle方向飞行一段距离放置子墙体，然后从子墙体直飞MERGER"""
    fly_dir = np.array([np.cos(angle), np.sin(angle)])
    # 子墙体到SPLITTER的距离：根据总距离的30%-50%
    sp_to_mg = np.linalg.norm(mg_pos - sp_pos)
    child_dist = max(MIN_CHILD_DIST, sp_to_mg * 0.35)
    # 尝试放置子墙体
    child_wall_pos = _find_child_wall_pos(sp_pos, fly_dir, child_dist, grid)
    # 子墙体法线：朝向飞行方向的反方向（正面迎击）
    wall_normal = -fly_dir
    wall_rot = np.degrees(np.arctan2(wall_normal[1], wall_normal[0])) - 90.0
    # 时间分配
    hit_time = sp_time + total_dt * CHILD_TRAIL_RATIO
    child_wall_id = sp_id * 100 + child_idx + 1
    wall_data = {
        'id': child_wall_id,
        'time': round(hit_time, 2),
        'type': 'CHILD_WALL',
        'pos': {'x': round(float(child_wall_pos[0]), 2),'y': round(float(child_wall_pos[1]), 2)},
        'rotation': round(float(wall_rot), 2),
        'note': note_info['note'],
        'velocity': note_info['velocity'],
        'instrumentId': note_info.get('instrumentId', 0)
    }
    # 将子墙体加入主grid防碰撞
    grid.add(wall_data)
    # 关键帧: splitter_pos → child_wall_pos → merger_pos
    keyframes = [
        {'time': round(sp_time, 2),
         'x': round(float(sp_pos[0]), 2), 'y': round(float(sp_pos[1]), 2)},
        {'time': round(hit_time, 2),
         'x': round(float(child_wall_pos[0]), 2), 'y': round(float(child_wall_pos[1]), 2)},
        {'time': round(sp_time + total_dt, 2),
         'x': round(float(mg_pos[0]), 2), 'y': round(float(mg_pos[1]), 2)}
    ]
    return {
        'note': note_info['note'],
        'velocity': note_info['velocity'],
        'instrumentId': note_info.get('instrumentId', 0),
        'walls': [wall_data],
        'keyframes': keyframes,
        'mergerTime': round(sp_time + total_dt, 2)
    }


def _find_child_wall_pos(origin, direction, target_dist, grid):
    """寻找子墙体放置位置，避免与已有墙体碰撞
    先尝试目标距离，碰撞则微调方向和距离"""
    # 直接尝试
    pos = origin + direction * target_dist
    if not _check_nearby_collision(pos, grid):
        return pos
    # 微调：尝试不同距离和角度偏移
    base_angle = np.arctan2(direction[1], direction[0])
    for d_mult in [0.8, 1.2, 0.6, 1.5]:
        for a_off in [0, 0.15, -0.15,0.3, -0.3]:
            a = base_angle + a_off
            d = np.array([np.cos(a), np.sin(a)])
            p = origin + d * target_dist * d_mult
            if not _check_nearby_collision(p, grid):
                return p
    # 强制放置：选择最远离已有墙的位置
    best_pos, best_dist = pos, 0
    for a_off in np.linspace(-np.pi / 3, np.pi / 3, 12):
        a = base_angle + a_off
        d = np.array([np.cos(a), np.sin(a)])
        p = origin + d * target_dist
        md = _min_nearby_dist(p, grid)
        if md > best_dist:
            best_dist = md
            best_pos = p
    return best_pos


def _check_nearby_collision(pos, grid):
    """检查位置附近是否有过近的墙体"""
    near = grid.query_near(pos[0], pos[1])
    for idx in near:
        w = grid.walls[idx]
        wp = np.array([w['pos']['x'], w['pos']['y']])
        if np.linalg.norm(pos - wp) < MIN_WALL_DIST:
            return True
    return False


def _min_nearby_dist(pos, grid):
    """计算到最近墙体的距离"""
    near = grid.query_near(pos[0], pos[1])
    if not near:
        return 999.0
    md = 999.0
    for idx in near:
        w = grid.walls[idx]
        wp = np.array([w['pos']['x'], w['pos']['y']])
        d = np.linalg.norm(pos - wp)
        if d < md:
            md = d
    return md

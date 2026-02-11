"""路径规划 - 回溯+动态速度+空间网格加速"""
import numpy as np
import random
import time
import bisect
from collision_detector import SpatialGrid, check_path_collision_grid, check_wall_segment_collision_grid

V_MIN, V_MAX = 150.0, 600.0
NPS_WINDOW, NPS_MAX = 2.0, 8.0
WALL_LENGTH, MIN_WALL_DIST, MIN_DISTANCE = 30.0, 15.0, 12.0
MAX_RETRIES, MAX_TOTAL_BT = 40, 3000


def plan_paths(notes):
    if not notes:
        return [], [{'time': 0, 'x': 0, 'y': 0}]
    speeds = _build_speed_curve(notes)
    states = []
    grid = SpatialGrid()
    ball_path = [{'time': 0, 'x': 0.0, 'y': 0.0}]
    pos = np.array([0.0, 0.0])
    d = np.array([1.0, 0.0])
    tbt, i = 0, 0
    t0 = time.time()
    while i < len(notes):
        n = notes[i]
        dt = n['time_ms'] if i == 0 else n['time_ms'] - notes[i-1]['time_ms']
        dist = max(speeds[i] * dt / 1000.0, MIN_DISTANCE)
        r = _try_place(pos, d, dist, i, n, grid, states, ball_path)
        if r is not None:
            pos, d = r
            i += 1
            if i % 500 == 0:
                print(f'  progress: {i}/{len(notes)}, {time.time()-t0:.1f}s')
            continue
        if tbt < MAX_TOTAL_BT and len(grid.walls) > 0:
            op, od = states.pop()
            grid.remove_last()
            ball_path.pop()
            pos = op.copy()
            a2 = np.arctan2(od[1], od[0]) + random.uniform(-np.pi/2, np.pi/2)
            d = np.array([np.cos(a2), np.sin(a2)])
            i = len(states)
            tbt += 1
            continue
        # 强制放置
        wn = _gen_normal(d)
        wr = np.degrees(np.arctan2(wn[1], wn[0])) - 90.0
        wp = pos + d * dist
        nd = _reflect(d, wn)
        states.append((pos.copy(), d.copy()))
        wall = _mk_wall(i, n, wp, wr)
        grid.add(wall)
        ball_path.append(_mk_kf(n['time_ms'], wp))
        pos, d = wp.copy(), nd
        i += 1
    elapsed = time.time() - t0
    cc = _count_col(grid.walls)
    print(f'  walls={len(grid.walls)} col={cc} bt={tbt} time={elapsed:.1f}s')
    return grid.walls, ball_path


def _try_place(pos, d, dist, idx, note, grid, states, bp):
    for _ in range(MAX_RETRIES):
        wn = _gen_normal(d)
        wr = np.degrees(np.arctan2(wn[1], wn[0])) - 90.0
        wp = pos + d * dist
        col = check_path_collision_grid(pos, d, dist, grid, WALL_LENGTH, MIN_WALL_DIST)
        if not col:
            col = check_wall_segment_collision_grid(wp, wr, grid, WALL_LENGTH)
        if not col:
            nd = _reflect(d, wn)
            states.append((pos.copy(), d.copy()))
            wall = _mk_wall(idx, note, wp, wr)
            grid.add(wall)
            bp.append(_mk_kf(note['time_ms'], wp))
            return wp.copy(), nd
    return None


# 预排序时间数组+二分查找优化NPS计算
def _build_speed_curve(notes):
    times = [n['time_ms'] for n in notes]
    raw = []
    hw = NPS_WINDOW * 500# 半窗口(ms)
    for i in range(len(notes)):
        tc = times[i]
        lo = bisect.bisect_left(times, tc - hw)
        hi = bisect.bisect_right(times, tc + hw)
        nps = (hi - lo) / NPS_WINDOW
        t = min(nps / NPS_MAX, 1.0)
        s = t * t * (3.0 - 2.0 * t)
        raw.append(V_MAX - (V_MAX - V_MIN) * s)
    if len(raw) <= 2: return raw
    sm = [raw[0]]
    for i in range(1, len(raw) - 1):
        sm.append((raw[i-1] + raw[i] + raw[i+1]) / 3.0)
    sm.append(raw[-1])
    return sm


def _gen_normal(d):
    a2 = np.arctan2(d[1], d[0])
    na = a2 + np.pi + random.uniform(-np.pi/3, np.pi/3)
    n = np.array([np.cos(na), np.sin(na)])
    return n / np.linalg.norm(n)

def _reflect(v, n):
    n = n / np.linalg.norm(n)
    r = v - 2 * np.dot(v, n) * n
    nm = np.linalg.norm(r)
    return r / nm if nm > 0 else np.array([1.0, 0.0])

def _mk_wall(idx, note, pos, rot):
    return {
        'id': idx + 1, 'time': round(note['time_ms'], 2),
        'pos': {'x': round(float(pos[0]), 2), 'y': round(float(pos[1]), 2)},
        'rotation': round(float(rot), 2), 'note': note['note'],
        'velocity': note['velocity'], 'instrumentId': note.get('instrumentId', 0)
    }

def _mk_kf(t, pos):
    return {'time': round(t, 2), 'x': round(float(pos[0]), 2), 'y': round(float(pos[1]), 2)}

def _count_col(walls):
    c = 0
    for i in range(1, len(walls)):
        wp = np.array([walls[i]['pos']['x'], walls[i]['pos']['y']])
        for j in range(i):
            wp2 = np.array([walls[j]['pos']['x'], walls[j]['pos']['y']])
            if np.linalg.norm(wp - wp2) < MIN_WALL_DIST * 0.8:
                c += 1
                break
    return c

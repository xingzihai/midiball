"""路径规划 - 回溯+动态速度+和弦分光镜/汇光镜+交替横竖墙"""
import numpy as np
import random
import time
import bisect
from collision_detector import SpatialGrid, check_path_collision_grid, check_wall_segment_collision_grid
from chord_grouper import group_notes, compute_merger_targets
from child_path_planner import plan_child_paths

V_MIN, V_MAX = 150.0, 600.0
NPS_WINDOW, NPS_MAX = 2.0, 8.0
WALL_LENGTH = 30.0
MIN_WALL_DIST = 25.0
MIN_DISTANCE = 25.0
MAX_RETRIES, BT_PER_WALL = 30, 15
CHORD_THRESHOLD_MS = 50.0
VIRTUAL_MERGE_MS = 500.0
_SQ2 = np.sqrt(2) / 2


def plan_paths(notes):
    if not notes:
        return [], [{'time': 0, 'x': 0, 'y': 0}]
    groups = group_notes(notes, CHORD_THRESHOLD_MS)
    cc = sum(1 for g in groups if g['is_chord'])
    print(f'和弦分组: {len(groups)}组, 其中和弦组{cc}个')
    speeds = _build_group_speed_curve(groups, notes)
    mergers = compute_merger_targets(groups, speeds, MIN_WALL_DIST)
    print(f'  汇光镜目标: {len(mergers)}个')
    return _plan_with_groups(groups, speeds, mergers)


def _plan_with_groups(groups, speeds, merger_targets):
    grid = SpatialGrid()
    bp = [{'time': 0, 'x': 0.0, 'y': 0.0}]
    pos = np.array([0.0, 0.0])
    d = np.array([_SQ2, _SQ2])
    tbt, widx, i = 0, 0, 0
    sp_to_mg = _build_splitter_merger_map(groups, merger_targets)
    splitter_dirs = {}
    t0 = time.time()
    while i < len(groups):
        g = groups[i]
        rep = g['representative']
        pt = groups[i - 1]['time_ms'] if i > 0 else 0
        dist = max(speeds[i] * (g['time_ms'] - pt) / 1000.0, MIN_DISTANCE)
        wtype = 'SPLITTER' if g['is_chord'] else ('MERGER' if i in merger_targets else 'WALL')
        cn = None
        if g['is_chord'] and len(g['notes']) > 1:
            cn = [{'note': n['note'], 'velocity': n['velocity'],
                   'instrumentId': n.get('instrumentId', 0)} for n in g['notes'][1:]]
        r = _try_place(pos, d, dist, widx, rep, grid, bp, wtype, cn)
        if r:
            pos, d = r
            if wtype == 'SPLITTER' and cn:
                mg_idx = sp_to_mg.get(i, -1)
                splitter_dirs[widx] = (d.copy(), i, mg_idx, cn)
            widx += 1
            i += 1
            if i % 200 == 0:
                print(f'  progress: {i}/{len(groups)}, {time.time()-t0:.1f}s')
            continue
        max_bt = max(len(groups) * BT_PER_WALL, 200)
        if tbt < max_bt and len(grid.walls) > 0:
            grid.remove_last()
            bp.pop()
            widx = len(grid.walls)
            i = widx
            if i > 0:
                wp = grid.walls[-1]['pos']
                pos = np.array([wp['x'], wp['y']])
            else:
                pos = np.array([0.0, 0.0])
            a = np.arctan2(d[1], d[0]) + random.uniform(-np.pi/2, np.pi/2)
            d = np.array([np.cos(a), np.sin(a)])
            tbt += 1
            continue
        pos, d = _force_place_smart(pos, d, dist, widx, rep, grid, bp, wtype, cn)
        widx += 1
        i += 1
        # 直接扫描grid.walls找所有SPLITTER生成子路径（避免回溯导致splitter_dirs失效）
    child_wall_count = _generate_all_child_paths_scan(grid, bp, speeds)
    elapsed = time.time() - t0
    col = _count_col(grid.walls)
    sp = sum(1 for w in grid.walls if w.get('type') == 'SPLITTER')
    mg = sum(1 for w in grid.walls if w.get('type') == 'MERGER')
    print(f'  walls={len(grid.walls)} col={col} bt={tbt} splitters={sp} mergers={mg} '
          f'child_walls={child_wall_count} time={elapsed:.1f}s')
    return grid.walls, bp


def _generate_all_child_paths_scan(grid, bp, speeds):
    """扫描grid.walls找所有SPLITTER，从ballPath推算飞行方向，生成子路径"""
    total = 0
    walls = grid.walls
    n = len(walls)
    for widx in range(n):
        w = walls[widx]
        if w.get('type') != 'SPLITTER' or not w.get('chordNotes'):
            continue
        sp_pos = np.array([w['pos']['x'], w['pos']['y']])
        sp_time = w['time']
        sp_id = w['id']
        cn = w['chordNotes']
        # 从ballPath推算飞行方向：前一个关键帧→当前位置
        fly_dir = np.array([_SQ2, _SQ2])
        for ki in range(len(bp) - 1):
            if abs(bp[ki + 1]['time'] - sp_time) < 1:
                prev = np.array([bp[ki]['x'], bp[ki]['y']])
                diff = sp_pos - prev
                norm = np.linalg.norm(diff)
                if norm > 1:
                    fly_dir = diff / norm
                break
        # 找下一个MERGER作为汇合点
        mg_time = sp_time + VIRTUAL_MERGE_MS
        mg_pos = _estimate_merger_pos(sp_pos, fly_dir, 300.0, VIRTUAL_MERGE_MS)
        for j in range(widx + 1, min(widx + 50, n)):
            if walls[j].get('type') == 'MERGER':
                mg_time = walls[j]['time']
                mg_pos = np.array([walls[j]['pos']['x'], walls[j]['pos']['y']])
                break
        dt = mg_time - sp_time
        if dt <= 0:
            continue
        child_paths = plan_child_paths(
            sp_pos, fly_dir, cn, mg_pos, sp_time, mg_time, sp_id, grid)
        if child_paths:
            w['childPaths'] = child_paths
            total += sum(len(cp['walls']) for cp in child_paths)
    return total


def _build_splitter_merger_map(groups, merger_targets):
    sp_mg = {}
    for i, g in enumerate(groups):
        if not g['is_chord']:
            continue
        for j in range(i + 1, len(groups)):
            if j in merger_targets:
                sp_mg[i] = j
                break
    return sp_mg


def _estimate_merger_pos(sp_pos, direction, speed, dt):
    dist = speed * dt / 1000.0
    return sp_pos + direction * dist


def _try_place(pos, d, dist, idx, note, grid, bp, wtype, cn):
    wp = pos + d * dist
    pcol = check_path_collision_grid(pos, d, dist, grid, WALL_LENGTH, MIN_WALL_DIST)
    if not pcol:
        for wn in _get_ortho_normals(d, idx):
            wr = np.degrees(np.arctan2(wn[1], wn[0])) - 90.0
            if not check_wall_segment_collision_grid(wp, wr, grid, WALL_LENGTH):
                nd = _reflect(d, wn)
                grid.add(_mk_wall(idx, note, wp, wr, wtype, cn))
                bp.append(_mk_kf(note['time_ms'], wp))
                return wp.copy(), nd
    for _ in range(MAX_RETRIES):
        a = np.arctan2(d[1], d[0]) + random.uniform(-np.pi/3, np.pi/3)
        td = np.array([np.cos(a), np.sin(a)])
        twp = pos + td * dist
        if check_path_collision_grid(pos, td, dist, grid, WALL_LENGTH, MIN_WALL_DIST):
            continue
        for wn in _get_ortho_normals(td, idx):
            wr = np.degrees(np.arctan2(wn[1], wn[0])) - 90.0
            if not check_wall_segment_collision_grid(twp, wr, grid, WALL_LENGTH):
                nd = _reflect(td, wn)
                grid.add(_mk_wall(idx, note, twp, wr, wtype, cn))
                bp.append(_mk_kf(note['time_ms'], twp))
                return twp.copy(), nd
    return None


def _force_place_smart(pos, d, dist, idx, note, grid, bp, wtype, cn):
    best_pos, best_rot, best_min_d = None, 0, -1
    best_wn = _get_ortho_normals(d, idx)[0]
    for mult in [1.0, 1.5, 2.0, 0.7]:
        for angle_off in [0, 0.3, -0.3, 0.6, -0.6, 1.0, -1.0]:
            a = np.arctan2(d[1], d[0]) + angle_off
            td = np.array([np.cos(a), np.sin(a)])
            twp = pos + td * dist * mult
            wn = _get_ortho_normals(td, idx)[0]
            wr = np.degrees(np.arctan2(wn[1], wn[0])) - 90.0
            md = _min_wall_dist(twp, grid)
            if md > best_min_d:
                best_min_d = md
                best_pos = twp.copy()
                best_rot = wr
                best_wn = wn
    grid.add(_mk_wall(idx, note, best_pos, best_rot, wtype, cn))
    bp.append(_mk_kf(note['time_ms'], best_pos))
    return best_pos, _reflect(d, best_wn)


def _min_wall_dist(pos, grid):
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


def _get_ortho_normals(d, idx=0):
    if idx % 2 == 0:
        pri = [np.array([0.0, 1.0]), np.array([0.0, -1.0])]
        sec = [np.array([1.0, 0.0]), np.array([-1.0, 0.0])]
    else:
        pri = [np.array([1.0, 0.0]), np.array([-1.0, 0.0])]
        sec = [np.array([0.0, 1.0]), np.array([0.0, -1.0])]
    pri.sort(key=lambda n: -abs(np.dot(d, n)))
    sec.sort(key=lambda n: -abs(np.dot(d, n)))
    return pri + sec


def _reflect(v, n):
    n = n / np.linalg.norm(n)
    r = v - 2 * np.dot(v, n) * n
    angle = np.arctan2(r[1], r[0]) + random.uniform(-0.26, 0.26)
    return np.array([np.cos(angle), np.sin(angle)])


def _build_group_speed_curve(groups, notes):
    at = [n['time_ms'] for n in notes]
    hw = NPS_WINDOW * 500
    raw = []
    for g in groups:
        tc = g['time_ms']
        lo = bisect.bisect_left(at, tc - hw)
        hi = bisect.bisect_right(at, tc + hw)
        nps = (hi - lo) / NPS_WINDOW
        t = min(nps / NPS_MAX, 1.0)
        s = t * t * (3.0 - 2.0 * t)
        raw.append(V_MAX - (V_MAX - V_MIN) * s)
    if len(raw) <= 2:
        return raw
    sm = [raw[0]]
    for i in range(1, len(raw) - 1):
        sm.append((raw[i-1] + raw[i] + raw[i+1]) / 3.0)
    sm.append(raw[-1])
    return sm


def _mk_wall(idx, note, pos, rot, wtype='WALL', cn=None):
    w = {'id': idx + 1, 'time': round(note['time_ms'], 2), 'type': wtype,
         'pos': {'x': round(float(pos[0]), 2), 'y': round(float(pos[1]), 2)},
         'rotation': round(float(rot), 2), 'note': note['note'],
         'velocity': note['velocity'], 'instrumentId': note.get('instrumentId', 0)}
    if cn:
        w['chordNotes'] = cn
    return w


def _mk_kf(t, pos):
    return {'time': round(t, 2), 'x': round(float(pos[0]), 2), 'y': round(float(pos[1]), 2)}


def _count_col(walls):
    c = 0
    for i in range(1, len(walls)):
        wp = np.array([walls[i]['pos']['x'], walls[i]['pos']['y']])
        for j in range(i):
            wp2 = np.array([walls[j]['pos']['x'], walls[j]['pos']['y']])
            if np.linalg.norm(wp - wp2) < MIN_WALL_DIST * 0.8:
                c += 1; break
    return c

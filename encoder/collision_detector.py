"""碰撞检测模块 - 空间网格哈希加速的2D碰撞检测"""
import numpy as np
from typing import List, Dict, Tuple, Set
from collections import defaultdict

GRID_SIZE = 50.0  # 网格单元大小(px)，略大于墙体长度+间距


class SpatialGrid:
    """空间哈希网格，O(1)查询邻近墙体"""
    def __init__(self, cell_size: float = GRID_SIZE):
        self.cs = cell_size
        self.cells: Dict[Tuple[int, int], List[int]] = defaultdict(list)
        self.walls: List[Dict] = []

    def _key(self, x: float, y: float) -> Tuple[int, int]:
        return (int(np.floor(x / self.cs)), int(np.floor(y / self.cs)))

    def add(self, wall: Dict):
        """添加墙体到网格"""
        idx = len(self.walls)
        self.walls.append(wall)
        cx, cy = wall['pos']['x'], wall['pos']['y']
        #墙体可能跨越多个格子，注册到中心及相邻格子
        gx, gy = self._key(cx, cy)
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                self.cells[(gx + dx, gy + dy)].append(idx)

    def remove_last(self):
        """移除最后添加的墙体（用于回溯）"""
        if not self.walls:
            return
        idx = len(self.walls) - 1
        wall = self.walls.pop()
        cx, cy = wall['pos']['x'], wall['pos']['y']
        gx, gy = self._key(cx, cy)
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                k = (gx + dx, gy + dy)
                if k in self.cells and idx in self.cells[k]:
                    self.cells[k].remove(idx)

    def query_near(self, x: float, y: float) -> List[int]:
        """查询(x,y)附近的墙体索引"""
        gx, gy = self._key(x, y)
        seen: Set[int] = set()
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for idx in self.cells.get((gx + dx, gy + dy), []):
                    seen.add(idx)
        return list(seen)

    def query_path(self, ox: float, oy: float, ex: float, ey: float) -> List[int]:
        """查询射线路径经过的所有格子中的墙体索引"""
        seen: Set[int] = set()
        # 采样路径上的几个点
        dx, dy = ex - ox, ey - oy
        steps = max(int(np.sqrt(dx*dx + dy*dy) / self.cs) + 2, 3)
        for s in range(steps + 1):
            t = s / steps
            px, py = ox + dx * t, oy + dy * t
            gx, gy = self._key(px, py)
            for ddx in (-1, 0, 1):
                for ddy in (-1, 0, 1):
                    for idx in self.cells.get((gx + ddx, gy + ddy), []):
                        seen.add(idx)
        return list(seen)


def ray_intersects_segment(
    ray_o: np.ndarray, ray_d: np.ndarray, ray_len: float,
    seg_a: np.ndarray, seg_b: np.ndarray, margin: float = 2.0
) -> bool:
    """射线与线段相交检测（参数化方程法）"""
    seg_d = seg_b - seg_a
    det = ray_d[0] * seg_d[1] - ray_d[1] * seg_d[0]
    if abs(det) < 1e-10:
        return False
    diff = seg_a - ray_o
    t = (diff[0] * seg_d[1] - diff[1] * seg_d[0]) / det
    u = (diff[0] * ray_d[1] - diff[1] * ray_d[0]) / det
    return (-margin < t < ray_len + margin) and (-0.05 < u < 1.05)


def wall_to_segment(wall: Dict, wl: float) -> Tuple[np.ndarray, np.ndarray]:
    """墙体→线段端点"""
    cx, cy = wall['pos']['x'], wall['pos']['y']
    rad = np.radians(wall['rotation'])
    h = wl / 2
    dx, dy = np.cos(rad) * h, np.sin(rad) * h
    return np.array([cx - dx, cy - dy]), np.array([cx + dx, cy + dy])


def check_path_collision_grid(
    origin: np.ndarray, direction: np.ndarray, distance: float,
    grid: SpatialGrid, wall_length: float, min_dist: float
) -> bool:
    """用空间网格加速的路径碰撞检测"""
    new_pos = origin + direction * distance
    # 只查询路径附近的墙
    candidates = grid.query_path(origin[0], origin[1], new_pos[0], new_pos[1])
    for idx in candidates:
        wall = grid.walls[idx]
        sa, sb = wall_to_segment(wall, wall_length)
        if ray_intersects_segment(origin, direction, distance, sa, sb):
            return True
        wp = np.array([wall['pos']['x'], wall['pos']['y']])
        if np.linalg.norm(new_pos - wp) < min_dist:
            return True
    return False


def check_wall_segment_collision_grid(
    new_pos: np.ndarray, new_rot: float,
    grid: SpatialGrid, wall_length: float
) -> bool:
    """用空间网格加速的墙体线段交叉检测"""
    nw = {'pos': {'x': float(new_pos[0]), 'y': float(new_pos[1])}, 'rotation': new_rot}
    na, nb = wall_to_segment(nw, wall_length)
    nd = nb - na
    nl = np.linalg.norm(nd)
    if nl < 1e-10:
        return False
    ndn = nd / nl
    candidates = grid.query_near(new_pos[0], new_pos[1])
    for idx in candidates:
        sa, sb = wall_to_segment(grid.walls[idx], wall_length)
        if ray_intersects_segment(na, ndn, nl, sa, sb, margin=0.5):
            return True
    return False

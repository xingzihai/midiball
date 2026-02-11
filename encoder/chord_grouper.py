"""和弦分组预处理器- 将时间相近的音符归为和弦组"""
from typing import List, Dict

CHORD_THRESHOLD_MS = 50.0  # 和弦判定阈值：音符间隔<50ms视为同一和弦


def group_notes(notes: List[Dict], threshold_ms: float = CHORD_THRESHOLD_MS) -> List[Dict]:
    """将音符按时间邻近度分组
    
    Args:
        notes: 按time_ms排序的音符列表
        threshold_ms: 和弦判定时间阈值(ms)
    Returns:
        groups列表，每组: {notes, is_chord, representative, time_ms}
    """
    if not notes:
        return []
    groups = []
    cur = [notes[0]]
    for i in range(1, len(notes)):
        # 与组内最后一个音符的时间差 < 阈值则归入同组
        if notes[i]['time_ms'] - cur[-1]['time_ms'] < threshold_ms:
            cur.append(notes[i])
        else:
            groups.append(_emit_group(cur))
            cur = [notes[i]]
    groups.append(_emit_group(cur))
    return groups


def _emit_group(notes: List[Dict]) -> Dict:
    """构建分组结构"""
    return {
        'notes': notes,
        'is_chord': len(notes) > 1,
        'representative': notes[0],# 代表音符（用于放置墙体）
        'time_ms': notes[0]['time_ms']
    }


def compute_merger_targets(groups: List[Dict], speeds: List[float],
                           min_dist: float = 30.0) -> set:
    """预计算汇光镜目标索引
    
    扫描每个和弦组，找到其后第一个满足间距≥min_dist的非和弦组，标记为MERGER。
    
    Args:
        groups: 分组列表
        speeds: 每个group对应的速度值（与groups等长）
        min_dist: 最小间距要求(px)
    Returns:
        需要标记为MERGER的group索引集合
    """
    merger_set = set()
    for i, g in enumerate(groups):
        if not g['is_chord']:
            continue
        # 从和弦组的下一个开始向后扫描
        for j in range(i + 1, len(groups)):
            dt = groups[j]['time_ms'] - groups[i]['time_ms']
            # 用目标组的速度估算物理距离
            dist = speeds[j] * dt / 1000.0 if j < len(speeds) else min_dist
            if dist >= min_dist and not groups[j]['is_chord']:
                merger_set.add(j)
                break
    return merger_set

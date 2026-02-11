"""MDBL文件输出模块 - 将墙体数据和元信息组装为.mdbl JSON文件"""
import json
from typing import List, Dict

PADDING = 200  # 地图边界padding(px)


def write_mdbl(walls: List[Dict], ball_path: List[Dict],
               meta: dict, instruments: List[Dict],
               output_path: str) -> None:
    """组装并输出.mdbl文件
    
    Args:
        walls: 墙体数据列表
        ball_path: 小球关键帧列表
        meta: MIDI元信息(title, bpm, total_time)
        instruments: 乐器列表(id, name, color)
        output_path: 输出文件路径
    """
    bounds = _calc_bounds(walls, ball_path)
    # 输出时去掉instruments中的channel字段（前端不需要）
    clean_instruments = [
        {'id': inst['id'], 'name': inst['name'], 'color': inst['color']}
        for inst in instruments
    ]
    mdbl = {
        'meta': {
            'title': meta.get('title', 'Untitled'),
            'bpm': meta.get('bpm', 120),
            'totalTime': meta.get('total_time', 0),
            'mapBounds': bounds
        },
        'assets': {
            'instruments': clean_instruments
        },
        'timeline': walls,
        'ballPath': ball_path
    }
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(mdbl, f, indent=2, ensure_ascii=False)


def _calc_bounds(walls: List[Dict], ball_path: List[Dict]) -> Dict:
    """计算地图边界(遍历所有坐标取min/max + padding)"""
    all_x, all_y = [0.0], [0.0]
    for w in walls:
        all_x.append(w['pos']['x'])
        all_y.append(w['pos']['y'])
    for p in ball_path:
        all_x.append(p['x'])
        all_y.append(p['y'])
    return {
        'minX': round(min(all_x) - PADDING, 2),
        'maxX': round(max(all_x) + PADDING, 2),
        'minY': round(min(all_y) - PADDING, 2),
        'maxY': round(max(all_y) + PADDING, 2)
    }

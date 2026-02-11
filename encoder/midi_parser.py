"""MIDI解析模块 - 将.mid文件转换为结构化音符事件列表（含多乐器映射）"""
import mido
from typing import List, Dict, Tuple

# 16通道预定义调色板
CHANNEL_COLORS = [
    '#4FC3F7', '#FF7043', '#66BB6A', '#AB47BC',
    '#FFA726', '#EC407A', '#26C6DA', '#D4E157',
    '#8D6E63', '#78909C', '#5C6BC0', '#29B6F6',
    '#EF5350', '#9CCC65', '#FFCA28', '#BDBDBD'
]
CHANNEL_NAMES = [
    'Piano', 'Piano2', 'Keys', 'Organ',
    'Guitar', 'Bass', 'Strings', 'Ensemble',
    'Brass', 'Reed', 'Pipe', 'Synth Lead',
    'Synth Pad', 'FX', 'Ethnic', 'Percussion'
]


def parse_midi(filepath: str) -> Tuple[List[Dict], dict, List[Dict]]:
    """解析MIDI文件，返回(音符事件列表, 元信息, 乐器列表)
    
    Args:
        filepath: .mid文件路径
    Returns:
        (notes, meta, instruments)
    """
    mid = mido.MidiFile(filepath)
    ticks_per_beat = mid.ticks_per_beat
    
    # 提取BPM（从第一个set_tempo事件，默认120BPM）
    tempo = 500000  # 默认120BPM
    for track in mid.tracks:
        for msg in track:
            if msg.type == 'set_tempo':
                tempo = msg.tempo
                break
    bpm = mido.tempo2bpm(tempo)
    
    # 收集所有轨道的音符事件
    notes = []
    for track in mid.tracks:
        notes += _extract_notes_from_track(track, ticks_per_beat, tempo)
    notes.sort(key=lambda n: n['time_ms'])
    
    # 构建乐器映射并为每个音符分配instrumentId
    instruments = _build_instrument_map(notes)
    channel_to_id = {inst['channel']: inst['id'] for inst in instruments}
    for n in notes:
        n['instrumentId'] = channel_to_id.get(n['channel'], 0)
    
    total_time = notes[-1]['time_ms'] + notes[-1]['duration_ms'] if notes else 0
    meta = {
        'title': _extract_title(mid),
        'bpm': round(bpm, 2),
        'total_time': round(total_time, 2)
    }
    return notes, meta, instruments


def _build_instrument_map(notes: List[Dict]) -> List[Dict]:
    """根据音符的channel字段构建乐器列表"""
    channels = sorted(set(n['channel'] for n in notes))
    instruments = []
    for idx, ch in enumerate(channels):
        instruments.append({
            'id': idx,
            'channel': ch,
            'name': CHANNEL_NAMES[ch %16],
            'color': CHANNEL_COLORS[ch % 16]
        })
    return instruments


def _extract_notes_from_track(track, ticks_per_beat: int, tempo: int) -> List[Dict]:
    """从单个轨道提取音符，配对note_on/note_off"""
    notes = []
    pending = {}  # pending[note_number] = [(abs_tick, velocity, channel), ...]
    abs_tick = 0
    for msg in track:
        abs_tick += msg.time
        if msg.type == 'note_on' and msg.velocity > 0:
            key = msg.note
            if key not in pending:
                pending[key] = []
            pending[key].append((abs_tick, msg.velocity, msg.channel))
        elif msg.type == 'note_off' or (msg.type == 'note_on' and msg.velocity == 0):
            key = msg.note
            if key in pending and pending[key]:
                on_tick, velocity, channel = pending[key].pop(0)
                on_ms = mido.tick2second(on_tick, ticks_per_beat, tempo) * 1000
                off_ms = mido.tick2second(abs_tick, ticks_per_beat, tempo) * 1000
                notes.append({
                    'time_ms': round(on_ms, 2),
                    'note': msg.note,
                    'velocity': velocity,
                    'channel': channel,
                    'duration_ms': round(off_ms - on_ms, 2)
                })
                if not pending[key]:
                    del pending[key]
    return notes


def _extract_title(mid: mido.MidiFile) -> str:
    """尝试从MIDI元数据提取曲名"""
    for track in mid.tracks:
        for msg in track:
            if msg.type == 'track_name' and msg.name.strip():
                return msg.name.strip()
    return "Untitled"

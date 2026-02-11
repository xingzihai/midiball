"""MidiMarble Encoder - MIDI转.mdbl文件的CLI工具"""
import argparse
import os
import sys
from midi_parser import parse_midi
from path_planner import plan_paths
from mdbl_writer import write_mdbl


def main():
    parser = argparse.ArgumentParser(description='MidiMarble Encoder: MIDI → .mdbl')
    parser.add_argument('input', help='输入的.mid文件路径')
    parser.add_argument('-o', '--output', default=None, help='输出的.mdbl文件路径')
    args = parser.parse_args()
    
    if not os.path.exists(args.input):
        print(f'错误:文件不存在 - {args.input}')
        sys.exit(1)
    
    output = args.output or os.path.splitext(args.input)[0] + '.mdbl'
    
    print(f'[1/3] 解析MIDI文件: {args.input}')
    notes, meta, instruments = parse_midi(args.input)
    print(f'      找到 {len(notes)} 个音符, BPM={meta["bpm"]}, '
          f'时长={meta["total_time"]:.0f}ms, 乐器={len(instruments)}个')
    
    if not notes:
        print('错误: MIDI文件中未找到任何音符')
        sys.exit(1)
    
    print(f'[2/3] 规划路径（回溯+动态速度+和弦分光镜）...')
    walls, ball_path = plan_paths(notes)
    splitters = sum(1 for w in walls if w.get('type') == 'SPLITTER')
    mergers = sum(1 for w in walls if w.get('type') == 'MERGER')
    child_walls = sum(len(cp['walls']) for w in walls
                      for cp in w.get('childPaths', []))
    print(f'      生成 {len(walls)} 面墙体, {len(ball_path)} 个路径关键帧')
    print(f'      分光镜={splitters}, 汇光镜={mergers}, 子墙体={child_walls}')
    
    print(f'[3/3] 输出文件: {output}')
    write_mdbl(walls, ball_path, meta, instruments, output)
    file_size = os.path.getsize(output)
    print(f'\n完成! 文件大小: {file_size / 1024:.1f} KB')


if __name__ == '__main__':
    main()

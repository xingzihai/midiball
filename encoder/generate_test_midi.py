"""生成测试用MIDI文件- 小星星旋律"""
import mido

mid = mido.MidiFile()
track = mido.MidiTrack()
mid.tracks.append(track)

track.append(mido.MetaMessage('track_name', name='Twinkle Twinkle', time=0))
track.append(mido.MetaMessage('set_tempo', tempo=mido.bpm2tempo(120), time=0))

# 小星星旋律: C C G G A A G - F F E E D D C
# MIDI音高: 60 60 67 67 69 69 67 - 65 65 64 64 62 62 60
notes = [60, 60, 67, 67, 69, 69, 67, 0, 65, 65, 64, 64, 62, 62, 60]
ticks_per_note = 480  # 每个音符一拍

for n in notes:
    if n == 0:  # 休止符
        track.append(mido.Message('note_on', note=60, velocity=0, time=ticks_per_note))
        continue
    track.append(mido.Message('note_on', note=n, velocity=80, time=0))
    track.append(mido.Message('note_off', note=n, velocity=0, time=ticks_per_note))

mid.save('test_twinkle.mid')
print('已生成 test_twinkle.mid')

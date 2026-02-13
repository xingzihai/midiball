// NoteData.cs — 音符数据结构，MIDI解析后的标准化数据格式
using StarPipe.Core;

namespace StarPipe.Audio
{
    /// <summary>
    /// 单个音符的完整信息，由MidiParser生成，供Map和Gameplay消费
    /// </summary>
    public struct NoteData
    {
        public double timeInSeconds;  // 音符触发时间（秒，基于TempoMap）
        public int midiNote;          // MIDI音高0-127
        public float xPosition;       // 映射后X坐标 [-5, 5]
        public float duration;        // 持续时长（秒）
        public TrackType track;       // 所属轨道
        public NoteType noteType;     // 普通/特殊

        public NoteData(double time, int note, float x, float dur, TrackType trk, NoteType type)
        {
            timeInSeconds = time;
            midiNote = note;
            xPosition = x;
            duration = dur;
            track = trk;
            noteType = type;
        }

        public override string ToString()
        {
            return $"[{track}] t={timeInSeconds:F3}s note={midiNote} x={xPosition:F2} dur={duration:F3}s {noteType}";
        }
    }

    /// <summary>
    /// 一首歌曲的完整解析结果
    /// </summary>
    public class SongData
    {
        public string songName;
        public float bpm;
        public double totalDuration;       // 歌曲总时长（秒）
        public NoteData[] allNotes;        // 所有音符（按时间排序）
        public NoteData[] melodyNotes;     // 主旋律轨道
        public NoteData[] drumsNotes;      // 鼓组轨道
        public NoteData[] bassNotes;       // 贝斯轨道
        public NoteData[] chordsNotes;     // 和弦轨道
    }
}

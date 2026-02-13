// MidiParser.cs — MIDI文件解析器，将.mid转换为NoteData列表
// 依赖：Melanchall.DryWetMidi (DLL放在Assets/Plugins/DryWetMidi/)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StarPipe.Core;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace StarPipe.Audio
{
    public static class MidiParser
    {
        private static readonly TrackType[] TrackMap = {
            TrackType.Melody, TrackType.Drums, TrackType.Bass, TrackType.Chords
        };

        /// <summary>解析MIDI文件，返回完整的SongData</summary>
        public static SongData Parse(string filePath)
        {
            var midiFile = MidiFile.Read(filePath);
            var tempoMap = midiFile.GetTempoMap();
            var allNotes = new List<NoteData>();

            var trackChunks = midiFile.GetTrackChunks().ToArray();
            for (int i = 0; i < trackChunks.Length; i++)
            {
                var trackType = i < TrackMap.Length ? TrackMap[i] : TrackType.Melody;
                var notes = trackChunks[i].GetNotes();
                foreach (var note in notes)
                {
                    double timeSec = note.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                    double durSec = note.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                    int midiNote = note.NoteNumber;
                    float xPos = GameConstants.MidiNoteToX(midiNote);
                    allNotes.Add(new NoteData(
                        timeSec, midiNote, xPos, (float)durSec, trackType, NoteType.Normal
                    ));
                }
            }
            allNotes.Sort((a, b) => a.timeInSeconds.CompareTo(b.timeInSeconds));

            float bpm = 120f;
            var tempos = tempoMap.GetTempoChanges().ToArray();
            if (tempos.Length > 0) bpm = (float)tempos[0].Value.BeatsPerMinute;

            double totalDur = allNotes.Count > 0
                ? allNotes[allNotes.Count - 1].timeInSeconds + allNotes[allNotes.Count - 1].duration
                : 0;

            var noteArray = allNotes.ToArray();
            var songData = BuildSongData(
                System.IO.Path.GetFileNameWithoutExtension(filePath), bpm, totalDur, noteArray);

            Debug.Log($"[MidiParser] 解析完成: {songData.songName} | BPM={bpm:F1} | " +
                      $"总音符={noteArray.Length} | 时长={totalDur:F2}s");
            return songData;
        }

        /// <summary>程序化生成测试音符（当MIDI音符不足时使用）</summary>
        /// <param name="count">生成音符总数</param>
        /// <param name="bpm">节拍速度</param>
        public static SongData GenerateTestNotes(int count = 1000, float bpm = 120f)
        {
            var allNotes = new List<NoteData>(count);
            float beatInterval = 60f / bpm; // 每拍间隔（秒）
            // 每半拍放一个音符，交替左右分布
            float noteSpacing = beatInterval * 0.5f;
            var rng = new System.Random(42); // 固定种子保证可复现

            for (int i = 0; i < count; i++)
            {
                double timeSec = i * noteSpacing;
                // 随机MIDI音高40~90，覆盖左右两侧
                int midiNote = rng.Next(40, 91);
                float xPos = GameConstants.MidiNoteToX(midiNote);
                float dur = noteSpacing * 0.8f;
                //轮流分配轨道
                var track = (TrackType)(i % 4);
                allNotes.Add(new NoteData(timeSec, midiNote, xPos, dur, track, NoteType.Normal));
            }

            double totalDur = count * noteSpacing;
            var noteArray = allNotes.ToArray();
            var songData = BuildSongData("ProceduralTest", bpm, totalDur, noteArray);

            Debug.Log($"[MidiParser] 程序化生成完成: {count}个音符 | BPM={bpm} | 时长={totalDur:F1}s");
            return songData;
        }

        /// <summary>构建SongData并按轨道分组</summary>
        private static SongData BuildSongData(string name, float bpm, double totalDur, NoteData[] notes)
        {
            return new SongData
            {
                songName = name,
                bpm = bpm,
                totalDuration = totalDur,
                allNotes = notes,
                melodyNotes = notes.Where(n => n.track == TrackType.Melody).ToArray(),
                drumsNotes = notes.Where(n => n.track == TrackType.Drums).ToArray(),
                bassNotes = notes.Where(n => n.track == TrackType.Bass).ToArray(),
                chordsNotes = notes.Where(n => n.track == TrackType.Chords).ToArray()
            };
        }
    }
}

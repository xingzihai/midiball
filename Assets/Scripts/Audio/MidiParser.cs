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
        //轨道索引映射规则：Track0=Melody, Track1=Drums, Track2=Bass, Track3=Chords
        private static readonly TrackType[] TrackMap = {
            TrackType.Melody, TrackType.Drums, TrackType.Bass, TrackType.Chords
        };

        /// <summary>
        /// 解析MIDI文件，返回完整的SongData
        /// </summary>
        /// <param name="filePath">MIDI文件的完整路径</param>
        public static SongData Parse(string filePath)
        {
            var midiFile = MidiFile.Read(filePath);
            var tempoMap = midiFile.GetTempoMap();
            var allNotes = new List<NoteData>();

            // 按chunk(轨道)遍历
            var trackChunks = midiFile.GetTrackChunks().ToArray();
            for (int i = 0; i < trackChunks.Length; i++)
            {
                var trackType = i < TrackMap.Length ? TrackMap[i] : TrackType.Melody;
                var notes = trackChunks[i].GetNotes();

                foreach (var note in notes)
                {
                    // tick ->秒
                    double timeSec = note.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                    double durSec = note.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                    int midiNote = note.NoteNumber;
                    float xPos = GameConstants.MidiNoteToX(midiNote);

                    // 默认Normal，后续可根据特定规则标记Special
                    var noteType = NoteType.Normal;

                    allNotes.Add(new NoteData(
                        timeSec, midiNote, xPos, (float)durSec, trackType, noteType
                    ));
                }
            }

            // 按时间排序
            allNotes.Sort((a, b) => a.timeInSeconds.CompareTo(b.timeInSeconds));

            // 提取BPM（取第一个Tempo事件）
            float bpm = 120f; // 默认值
            var tempos = tempoMap.GetTempoChanges().ToArray();
            if (tempos.Length > 0)bpm = (float)tempos[0].Value.BeatsPerMinute;

            // 计算总时长
            double totalDur = allNotes.Count > 0
                ? allNotes[allNotes.Count - 1].timeInSeconds + allNotes[allNotes.Count - 1].duration
                : 0;

            // 按轨道分组
            var noteArray = allNotes.ToArray();
            var songData = new SongData
            {
                songName = System.IO.Path.GetFileNameWithoutExtension(filePath),
                bpm = bpm,
                totalDuration = totalDur,
                allNotes = noteArray,
                melodyNotes = noteArray.Where(n => n.track == TrackType.Melody).ToArray(),
                drumsNotes = noteArray.Where(n => n.track == TrackType.Drums).ToArray(),
                bassNotes = noteArray.Where(n => n.track == TrackType.Bass).ToArray(),
                chordsNotes = noteArray.Where(n => n.track == TrackType.Chords).ToArray()
            };

            Debug.Log($"[MidiParser] 解析完成: {songData.songName} | BPM={bpm:F1} | " +
                      $"总音符={noteArray.Length} | 时长={totalDur:F2}s | " +
                      $"Melody={songData.melodyNotes.Length} Drums={songData.drumsNotes.Length} " +
                      $"Bass={songData.bassNotes.Length} Chords={songData.chordsNotes.Length}");

            return songData;
        }
    }
}

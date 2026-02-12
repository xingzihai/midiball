// MidiParserTest.cs — 编辑器测试脚本，验证MIDI解析功能
// 使用方式：挂载到场景空物体，在Inspector中填入MIDI路径，点击Play
using UnityEngine;
using StarPipe.Audio;

namespace StarPipe.Audio
{
    public class MidiParserTest : MonoBehaviour
    {
        [Header("MIDI文件路径（相对于Assets/Resources/MIDI/）")]
        [SerializeField] private string midiFileName = "test.mid";

        [Header("打印前N个音符（0=全部）")]
        [SerializeField] private int printCount = 20;

        void Start()
        {
            string fullPath = System.IO.Path.Combine(
                Application.dataPath, "Resources", "MIDI", midiFileName);

            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError($"[MidiParserTest] 文件不存在: {fullPath}");
                return;
            }

            Debug.Log($"[MidiParserTest] 开始解析: {fullPath}");
            var songData = MidiParser.Parse(fullPath);

            // 打印摘要
            Debug.Log($"---歌曲: {songData.songName} ---");
            Debug.Log($"BPM: {songData.bpm:F1} | 总时长: {songData.totalDuration:F2}s");
            Debug.Log($"总音符: {songData.allNotes.Length}");

            // 打印前N个音符详情
            int count = printCount <= 0 ? songData.allNotes.Length : Mathf.Min(printCount, songData.allNotes.Length);
            for (int i = 0; i < count; i++)
            {
                Debug.Log($"  [{i}] {songData.allNotes[i]}");
            }
        }
    }
}

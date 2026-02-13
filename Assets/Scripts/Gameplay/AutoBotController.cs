// AutoBotController.cs — 自动球插值飞行，100%命中对应轨道音符
// 由GameStateManager通过静态工厂Create()创建，销毁时调用Destroy
using UnityEngine;
using StarPipe.Core;
using StarPipe.Audio;

namespace StarPipe.Gameplay
{
    public class AutoBotController : MonoBehaviour
    {
        public TrackType Track { get; private set; }

        private NoteData[] _notes;
        private IAudioConductor _conductor;
        private int _currentIdx;
        private float _yOffset;
        private Renderer _renderer;
        private AudioSource _sfx;

        //轨道颜色映射
        private static readonly Color ColorDrums= new Color(1f, 0.6f, 0f, 0.7f);
        private static readonly Color ColorBass   = new Color(0.2f, 0.5f, 1f, 0.7f);
        private static readonly Color ColorChords = new Color(0.8f, 0.3f, 1f, 0.7f);

        /// <summary>静态工厂：创建自动球并初始化</summary>
        public static AutoBotController Create(TrackType track, NoteData[] notes, Transform parent)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"AutoBot_{track}";
            sphere.transform.SetParent(parent);
            sphere.transform.localScale = Vector3.one * 0.5f;
            // 移除碰撞体，自动球不参与物理
            var col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var bot = sphere.AddComponent<AutoBotController>();
            bot.Init(track, notes);
            return bot;
        }

        private void Init(TrackType track, NoteData[] notes)
        {
            Track = track;
            _notes = notes;
            _currentIdx = 0;
            // Y偏移：不同轨道略微错开避免重叠
            _yOffset = track switch
            {
                TrackType.Drums=> 0.3f,
                TrackType.Bass   => 0.6f,
                TrackType.Chords => 0.9f,
                _ => 0.5f
            };
            // 设置半透明材质颜色
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                var mat = _renderer.material;
                mat.color = GetTrackColor(track);
                // 简易半透明（Standard Shader Fade模式需运行时设置）
                mat.SetFloat("_Mode", 2f); // Fademat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            // 音效源
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.volume = 0.3f;

            if (!ServiceLocator.Has<IAudioConductor>()) return;
            _conductor = ServiceLocator.Get<IAudioConductor>();
            Debug.Log($"[AutoBot] {track} 创建完成 | 音符数={notes.Length}");
        }

        void Update()
        {
            if (_conductor == null || _notes == null || _notes.Length == 0) return;
            if (!_conductor.IsPlaying) return;

            float songTime = (float)_conductor.SongTime;
            // 二分查找当前音符索引
            _currentIdx = FindNoteIndex(songTime);
            // 计算插值位置
            Vector3 pos = CalcPosition(songTime);
            transform.position = pos;
            // 检查是否刚到达音符位置（触发音效）
            CheckNoteArrival(songTime);
        }

        /// <summary>二分查找：找到songTime所在的音符区间起始索引</summary>
        private int FindNoteIndex(float songTime)
        {
            int lo = 0, hi = _notes.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if ((float)_notes[mid].timeInSeconds <= songTime) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }

        /// <summary>在当前音符和下一音符之间Lerp插值</summary>
        private Vector3 CalcPosition(float songTime)
        {
            float z = songTime * GameConstants.SCROLL_SPEED;
            if (_currentIdx >= _notes.Length - 1){
                // 最后一个音符，停在其X位置
                float lastX = _notes[_notes.Length - 1].xPosition;
                return new Vector3(lastX, _yOffset, z);
            }
            var cur = _notes[_currentIdx];
            var next = _notes[_currentIdx + 1];
            float curTime = (float)cur.timeInSeconds;
            float nextTime = (float)next.timeInSeconds;
            float interval = nextTime - curTime;
            // 防除零
            float t = interval > 0.001f ? (songTime - curTime) / interval : 1f;
            t = Mathf.Clamp01(t);
            float x = Mathf.Lerp(cur.xPosition, next.xPosition, t);
            return new Vector3(x, _yOffset, z);
        }

        private int _lastPlayedIdx = -1;
        /// <summary>到达音符位置时播放轻微音效</summary>
        private void CheckNoteArrival(float songTime)
        {
            if (_currentIdx == _lastPlayedIdx) return;
            float noteTime = (float)_notes[_currentIdx].timeInSeconds;
            // 在音符时间±0.05s内视为到达
            if (Mathf.Abs(songTime - noteTime) < 0.05f)
            {
                _lastPlayedIdx = _currentIdx;
                PlayBotTone(_notes[_currentIdx].midiNote);
            }
        }

        private void PlayBotTone(int midiNote)
        {
            if (_sfx == null) return;
            // 频率基于MIDI音高，音量较低不抢主旋律
            float freq = 220f + midiNote * 3f;
            _sfx.clip = ToneGenerator.CreateTone(freq, 0.05f, 0.2f);
            _sfx.Play();
        }

        private static Color GetTrackColor(TrackType t) => t switch
        {
            TrackType.Drums  => ColorDrums,
            TrackType.Bass   => ColorBass,
            TrackType.Chords => ColorChords,
            _ => Color.white
        };
    }
}
